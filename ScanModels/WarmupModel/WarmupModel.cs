using ScanModels.DirBruteScan;
using ScanModels.CLIVersion;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;

namespace ScanModels.WarmupModel
{
    public class WarmUpScanOutput
    {
        public int Concurrency{set;get;}
        public int Timeout{set;get;}
        public List<int> SecurityStatusCodes{set;get;} = new();
        public int WafHits{get; set;}
        public int RLHits{set; get;}
        public int IncreasingLatency{set;get;}
        public int DecreasingLatency{set;get;}
        public List<double> SpikedLatency{set;get;} = new();
    }
    public class WarmupScanArchitecture
    {
        public string Target { set; get; } = string.Empty;
        public int Concurrency { set; get; }
        public int Timeout { set; get; }
        public double LatencyMs{set; get;}
        public int StatusCode{set; get;}
    }
    public class WarmupScan
    {
        public readonly HttpClient _client;
        public string Target{set; get;} = string.Empty;
        public int Concurrency{set; get;}
        public int Timeout{set; get;}
        public readonly SemaphoreSlim _concurrency;
        public string WordlistPath{set;get;} = string.Empty;
        public string JsonFilePath{set;get;} =  string.Empty;
        public WarmupScan(string[] Args)
        {
           CLIMainEngine processArgs = new CLIMainEngine().ProcessCLiArgs(args: Args);
            _client = new HttpClient();
            Target = processArgs.Target;
            Timeout = processArgs.Timeout;
            WordlistPath = processArgs.WordlistPath;
            Concurrency = processArgs.Concurrency;
            JsonFilePath = processArgs.JsonFilePath;
            _concurrency = new SemaphoreSlim(Concurrency);
        }
        public string[] processWordlist()
        {
            return File.ReadAllLines(WordlistPath).Length > 10 ? File.ReadAllLines(WordlistPath).Take(10).ToArray() : File.ReadAllLines(WordlistPath);
        }
        public async Task<WarmupScanArchitecture> Scan(string domain)
        {
            await _concurrency.WaitAsync();
            WarmupScanArchitecture scanArchitecture = new WarmupScanArchitecture();
            string Subtarget = Target + domain;
            var sw = Stopwatch.StartNew();
            try
            {
                using var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(Timeout)
                );
                var result = await _client.GetAsync(Subtarget, cts.Token);
                sw.Stop();
                scanArchitecture.Target = Subtarget;
                scanArchitecture.Concurrency = Concurrency;
                scanArchitecture.Timeout = Timeout;
                scanArchitecture.StatusCode = (int)result.StatusCode;
                scanArchitecture.LatencyMs = sw.Elapsed.TotalMilliseconds;
                return scanArchitecture;
            }
            catch (Exception ex)
            {
                scanArchitecture.Target = Subtarget;
                scanArchitecture.StatusCode = 0;
                scanArchitecture.Concurrency = 0;
                scanArchitecture.Timeout = 0;
                scanArchitecture.LatencyMs = 0.000;
                Console.WriteLine($"There is error coming up from the scan here it is{ex}");
                return scanArchitecture;
            }
            finally
            {
                _concurrency.Release();
            }
        }

        public async Task<WarmUpScanOutput> MainScan()
        {
            string[] wordlist = processWordlist();
            List<Task<WarmupScanArchitecture>> tasks  = new();
            foreach(var words in wordlist)
            {
                tasks.Add(Scan(domain:words));
            }
            var results = await Task.WhenAll(tasks);
            var allStatusCodesList = results.Select(x => x.StatusCode).ToArray();
            var allLatencyList = results.Select(x => x.LatencyMs).ToArray();
            int Concurrency = results.Select(x => x.Concurrency).First();
            int Timeout = results.Select(x => x.Timeout).First();
            var speedTuningModel = new SpeedTuning(
                statusCodeList:allStatusCodesList,
                latencyList:allLatencyList,
                Concurrency: Concurrency,
                Timeout:Timeout
            );
            SpeedTuningOutputModel outputReading = speedTuningModel.CalculateSpeedUsingCodes();
            WarmUpScanOutput outputData = new WarmUpScanOutput();
            outputData.Concurrency = outputReading.SuggestedConcurrency;
            outputData.Timeout = outputReading.SuggestedTimemout;
            outputData.DecreasingLatency = outputReading.DecreasingLatency;
            outputData.IncreasingLatency = outputReading.IncreasingLatency;
            outputData.SpikedLatency = outputReading.SpikedLatency;
            outputData.SecurityStatusCodes = outputReading.WafRateLimitedCodes;
            outputData.WafHits = outputReading.wafHit;
            outputData.RLHits = outputReading.rateLimtedHits;
            return outputData;
        }
    }
}