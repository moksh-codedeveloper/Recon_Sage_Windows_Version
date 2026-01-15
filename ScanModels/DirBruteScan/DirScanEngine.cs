using System;
using System.Diagnostics;
using ScanModels.CLIVersion;


namespace ScanModels.DirBruteScan
{
    public class DirScanEngine
    {
        public CLIMainEngine clioption { get; set; }

        public string Target { get; }
        public int Concurrency { set; get; }
        public int Timeout { set; get; }
        public string JsonFilePath { get; set; }
        public string WordlistPath { get; set; }

        private readonly SemaphoreSlim _concurrency;
        private readonly HttpClient _client;

        public DirScanEngine(string[] Args)
        {
            clioption = new CLIMainEngine().ProcessCLiArgs(Args);

            Target = clioption.Target;
            Concurrency = clioption.Concurrency;
            Timeout = clioption.Timeout;
            JsonFilePath = clioption.JsonFilePath;
            WordlistPath = clioption.WordlistPath;

            _concurrency = new SemaphoreSlim(Concurrency);
            _client = new HttpClient();
        }

        public class ScanOutputModel
        {
            public string message { get; set; } = string.Empty;
            public string target { get; set; } = string.Empty;
            public double latencyms { get; set; }
            public Dictionary<string, string>? headers { get; set; }
            public int statuscode { get; set; }
        }

        public string[] ProcessWordlist()
        {
            return File.ReadAllLines(WordlistPath);
        }

        public async Task<ScanOutputModel> Scan(string domain)
        {
            await _concurrency.WaitAsync();

            var data = new ScanOutputModel
            {
                headers = new Dictionary<string, string>()
            };

            string subtarget = Target + domain;
            var sw = Stopwatch.StartNew();

            try
            {
                using var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(Timeout)
                );

                var result = await _client.GetAsync(subtarget, cts.Token);

                sw.Stop();

                data.target = subtarget;
                data.statuscode = (int)result.StatusCode;
                data.latencyms = sw.Elapsed.TotalMilliseconds;
                data.headers = result.Headers.ToDictionary(
                    h => h.Key,
                    h => string.Join(", ", h.Value)
                );
                data.message = "OK";
            }
            catch (TaskCanceledException)
            {
                sw.Stop();

                data.target = subtarget;
                data.statuscode = 0;
                data.latencyms = sw.Elapsed.TotalMilliseconds;
                data.message = "Timeout";
            }
            catch (HttpRequestException)
            {
                sw.Stop();
                data.target = subtarget;
                data.statuscode = 0;
                data.latencyms = sw.Elapsed.TotalMilliseconds;
                data.message = "Network error";
            }
            catch (Exception ex)
            {
                sw.Stop();
                data.target = subtarget;
                data.statuscode = 0;
                data.latencyms = sw.Elapsed.TotalMilliseconds;
                data.message = ex.Message;
            }
            finally
            {
                _concurrency.Release();
            }

            return data;
        }
    }
}