using RfoModel;
using TorConfigParser;
using System.Net;
using System.Net.Sockets;
using Interface.Network;
using ScanOutputModel;
using System.Diagnostics;

namespace TorAdvScan
{
    public class TorScan : INetwork
    {
        public string FilePath{set;get;}
        public IPAddress host{set;get;}
        public int port{set;get;}
        public string password{set;get;} = string.Empty;
        public string Target{set;get;} = string.Empty;
        public int Timeout{set;get;}
        public string JsonFilePath{set;get;} = string.Empty;
        public string WordlistPath{set;get;} = string.Empty;
        public HttpClient client{set;get;}
        public TorScan(string file_path)
        {
            FilePath = file_path;
            var handler = new SocketsHttpHandler
            {
              Proxy = new WebProxy("socks://127.0.0.1:9050"),
              UseProxy = true
            };
            client = new HttpClient(handler);
        }
        public async Task Rotate()
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(host, port);
            using NetworkStream stream = client.GetStream();
            using StreamReader streamReader = new StreamReader(stream);
            using StreamWriter streamWriter = new StreamWriter(stream)
            {
                AutoFlush = true,
                NewLine = "\r\n"
            };
            await streamWriter.WriteAsync($"AUTHENTICATE {password}");
            string authResponse = await streamReader.ReadLineAsync();
            if(authResponse == null || !authResponse.StartsWith("250"))
                throw new Exception("AUTH failed unfortunately");
            await streamWriter.WriteAsync("SIGNAL NEWNYM");
            string authNymResponse = await streamReader.ReadLineAsync();
            if(authNymResponse == null || !authNymResponse.StartsWith("250"))
                throw new Exception($"Seems like we have caught an error while changing the tracks of the tors :- {authNymResponse}");
            Console.WriteLine("Circuits changed successfully continue the scans");
        }
        public async Task<string[]> processWordlist()
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
                var result = await client.GetAsync(subTarget, cts.Token);
                sw.Stop();
                scan.StatusCode = (int)result.StatusCode;
                scan.Headers = result.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
                scan.Message = result.ReasonPhrase;
                scan.LatencyMS = sw.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                sw.Stop();
                scan.Message = ex.Message;
                scan.StatusCode = 0;
                scan.Headers = new Dictionary<string, string>();
                scan.LatencyMS = sw.ElapsedMilliseconds;
                throw new Exception($"The scan Exception is this whichh you get from this :- {ex.Message}");
            }
            return scan;
        }
    }
}