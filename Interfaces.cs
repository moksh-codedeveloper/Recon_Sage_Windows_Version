using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO.Pipelines;
using Interface.Network;
using ScanOutputModel;

namespace ScanInterface
{
    public class NormalScan : INetwork
    {
        public string Target { set; get; } = string.Empty;
        public int Concurrency { set; get; }
        public int Timeout { set; get; }
        private readonly HttpClient _client;
        private readonly SemaphoreSlim _concurrency;
        public NormalScan(string target, int concurrency, int timeout)
        {
            Target = target;
            Concurrency = concurrency;
            Timeout = timeout;
            _client = new HttpClient();
            _concurrency = new SemaphoreSlim(Concurrency);
        }

        public async Task<ScanOutput> SendAsync()
        {
            ScanOutput scanOutputModel = new ScanOutput();
            scanOutputModel.Target = Target;
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await _client.GetAsync(Target, cts.Token);
                sw.Stop();
                var Latency = sw.ElapsedMilliseconds;
                var Message = result.ReasonPhrase;
                Dictionary<string, string> Header = result.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
                scanOutputModel.LatencyMS = Latency;
                scanOutputModel.StatusCode = (int)result.StatusCode;
                scanOutputModel.Message = Message;
                scanOutputModel.Headers = Header;
            }
            catch (Exception ex)
            {
                Console.WriteLine("This is the Exception which keeps coming..... :- ", ex.Message);
            }
            return scanOutputModel;
        }
    }
}