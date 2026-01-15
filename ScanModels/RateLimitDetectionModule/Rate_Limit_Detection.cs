using System;
using System.IO;
using System.Runtime.CompilerServices;
using ScanModels.CLIVersion;
using ScanModels.DirBruteScan;

namespace ScanModels.RateLimitModule
{
    public class RateLimitModule
    {
        public string[] Args { init; get; }
        public CLIMainEngine CliEngine { init; get; }
        public string Target { set; get; } = string.Empty;
        public int Concurrency { set; get; }
        public int Timeout { set; get; }
        public string WordlistPath { set; get; } = string.Empty;
        public string JsonFilePath { set; get; } = string.Empty;
        public double LatencyMS { set; get; }
        public  List<int> detectedStatusCode{set;get;} = new();

        public List<int> RateLimitCode = new(){429, 420, 402, 403, 503};
        public RateLimitModule(string[] args)
        {
            Args =  args;
            CliEngine = new CLIMainEngine().ProcessCLiArgs(args: args);
            Target = CliEngine.Target;
            Concurrency = CliEngine.Concurrency;
            Timeout = CliEngine.Timeout;
            WordlistPath = CliEngine.WordlistPath;
            JsonFilePath = CliEngine.JsonFilePath;
        }

        public string[] ProcessWordlist()
        {
            var wordlist = File.ReadAllLines(WordlistPath);
            return wordlist.Length > 20 ? wordlist.Take(20).ToArray() : wordlist;
        }
        public async Task<RateLimitDetectionOutputModel> mainScan()
        {
            List<Task<DirScanEngine.ScanOutputModel>> tasks = new();
            string[] wordlist = ProcessWordlist();
            DirScanEngine scanEngine = new DirScanEngine(Args: Args);
            foreach (var word in wordlist)
            {
                tasks.Add(scanEngine.Scan(word));
            }
            var results = await Task.WhenAll(tasks);

            List<Dictionary<string, string>> headers =
            results
                   .Where(x => x.headers is not null)
                   .Select(x => x.headers!)
                   .ToList();
            var LatencyMS = results.Select(x => x.latencyms).ToList();
            var StatusCode = results.Select(x => x.statuscode).ToList();
            RateLimitDetectionOutputModel limitDetectionOutputModel = new RateLimitDetectionOutputModel();

            List<double> SpikedLatency = new();
            double mean = LatencyMS.Average();
            double variance = LatencyMS.Select(x => Math.Pow(x - mean, 2)).Average();
            double stdev = Math.Sqrt(variance);
            double thresholds = mean + (2 * stdev);
            int increasingLatCount = 0;
            int decreasingLatCount = 0;
            foreach(var lat in LatencyMS)
            {
                bool isSpiked = lat > thresholds;
                if (isSpiked)
                {
                    SpikedLatency.Add(lat);
                }
            }
            foreach(var codes in StatusCode)
            {
                if (RateLimitCode.Contains(codes))
                {
                    detectedStatusCode.Add(codes);
                }
            }
            for (int i = 1; i < LatencyMS.Count(); i++)
            {
                var lat = LatencyMS[i];
                var prevLat = LatencyMS[i - 1];
                bool isIncreasing = lat > prevLat;
                bool isDecreasing = lat < prevLat;
                if (isIncreasing)
                {
                    increasingLatCount++;
                }
                else if (isDecreasing)
                {
                    decreasingLatCount++;
                }
            }
            limitDetectionOutputModel.LatencyMS = LatencyMS;
            limitDetectionOutputModel.StatusCode = StatusCode;
            limitDetectionOutputModel.SpikedLatencyMS = SpikedLatency;
            limitDetectionOutputModel.headers = headers;
            limitDetectionOutputModel.decreasingTrendCount = decreasingLatCount;
            limitDetectionOutputModel.increasingTrendCount = increasingLatCount;
            limitDetectionOutputModel.DetectedStatusCodeList = detectedStatusCode;
            limitDetectionOutputModel.Target = results.Select(x => x.target).ToList();
            return limitDetectionOutputModel;
        }
    }

    public class RateLimitDetectionOutputModel
    {
        public List<string> Target { set; get; } = new();
        public List<double> LatencyMS { set; get; } = new();
        public List<int> StatusCode { set; get; } = new();
        public List<int> DetectedStatusCodeList { set; get; } = new();
        public List<double> SpikedLatencyMS { set; get; } = new();
        public List<Dictionary<string, string>> headers { set; get; } = new();
        public int increasingTrendCount { set; get; }
        public int decreasingTrendCount { set; get; }
    }
}