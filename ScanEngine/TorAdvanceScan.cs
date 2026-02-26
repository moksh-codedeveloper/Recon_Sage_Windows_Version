using System.Net;
using System.Net.Sockets;
using Interface.Network;
using ScanOutputModel;
using System.Diagnostics;

namespace TorAdvScan
{
    public class TorScan : INetwork, IDisposable
    {
        public string FilePath { get; set; }
        public IPAddress Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int Timeout { get; set; }
        public string JsonFilePath { get; set; } = string.Empty;
        public string WordlistPath { get; set; } = string.Empty;

        private readonly HttpClient _client;
        private readonly SemaphoreSlim _rotateLock = new(1, 1);
        private static readonly HashSet<int> BlockedCodes = new()
        {
            401, 403, 406, 429, 451, 503
        };

        public TorScan(string filePath)
        {
            FilePath = filePath;

            var handler = new SocketsHttpHandler
            {
                Proxy = new WebProxy("socks5://127.0.0.1:9050"),
                UseProxy = true
            };

            _client = new HttpClient(handler);
        }

        public async Task Rotate()
        {
            using TcpClient torControlClient = new TcpClient();
            await torControlClient.ConnectAsync(Host, Port);

            using NetworkStream stream = torControlClient.GetStream();
            using StreamReader reader = new StreamReader(stream);
            using StreamWriter writer = new StreamWriter(stream)
            {
                AutoFlush = true,
                NewLine = "\r\n"
            };

            await writer.WriteLineAsync($"AUTHENTICATE {Password}");
            string? authResponse = await reader.ReadLineAsync();

            if (authResponse == null || !authResponse.StartsWith("250"))
                throw new Exception("Tor AUTH failed.");

            await writer.WriteLineAsync("SIGNAL NEWNYM");
            string? nymResponse = await reader.ReadLineAsync();

            if (nymResponse == null || !nymResponse.StartsWith("250"))
                throw new Exception($"Tor NEWNYM failed: {nymResponse}");

            Console.WriteLine("Tor circuit rotated successfully.");
        }

        public async Task<string[]> ProcessWordlist()
        {
            return await File.ReadAllLinesAsync(FilePath);
        }

        public async Task<ScanOutput> SendAsync(string domain)
        {
            ScanOutput scan = new ScanOutput();
            string subTarget = Target + domain;
            scan.Target = subTarget;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));
            var sw = Stopwatch.StartNew();

            try
            {
                var response = await _client.GetAsync(subTarget, cts.Token);
                sw.Stop();

                scan.StatusCode = (int)response.StatusCode;
                scan.Headers = response.Headers
                                       .ToDictionary(h => h.Key, h => string.Join(",", h.Value));
                scan.Message = response.ReasonPhrase;
                scan.LatencyMS = sw.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                sw.Stop();

                scan.StatusCode = 0;
                scan.Headers = new Dictionary<string, string>();
                scan.Message = ex.Message;
                scan.LatencyMS = sw.ElapsedMilliseconds;

                return scan; // DO NOT throw — keep scan resilient
            }

            return scan;
        }

        public async Task<MainScanOutput> ExecuteScan()
        {
            string[] wordlist = await ProcessWordlist();

            using var semaphore = new SemaphoreSlim(10);
            MainScanOutput mainScanOutput = new MainScanOutput();
            object resultLock = new object();

            var tasks = wordlist.Select(async domain =>
            {
                await semaphore.WaitAsync();

                try
                {
                    var result = await SendAsync(domain);

                    lock (resultLock)
                    {
                        mainScanOutput.Result.Add(result);
                    }

                    if (IsBlocked(result.StatusCode))
                    {
                        await _rotateLock.WaitAsync();
                        try
                        {
                            await Rotate();
                            await Task.Delay(10000);
                        }
                        finally
                        {
                            _rotateLock.Release();
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            return mainScanOutput;
        }

        public bool IsBlocked(int statusCode)
        {
            return BlockedCodes.Contains(statusCode);
        }

        public void Dispose()
        {
            _client?.Dispose();
            _rotateLock?.Dispose();
        }
    }
}