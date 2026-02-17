using System.Text.Json;
using System.IO;
using ScanModels.Waf_Scanner;
using System.Reflection.Metadata;

namespace ScanModels.WafAnalyser
{
    public class WafAnalyser
    {
        public string JsonFilePath { init; get; } = string.Empty;
        public List<int> WafCodes = new() { 403, 406, 429, 400, 301, 302, 503, 504, 499, 999, 990 };
        public WafAnalyser(string jsonFilePath)
        {
            JsonFilePath = jsonFilePath;
        }
        public async Task<List<ScanOutput>> LoadFile()
        {
            string jsonString = await File.ReadAllTextAsync(JsonFilePath);
            return JsonSerializer.Deserialize<List<ScanOutput>>(
     jsonString,
     new JsonSerializerOptions
     {
         PropertyNameCaseInsensitive = true
     }
 ) ?? new List<ScanOutput>();

        }
        public List<double> LatencySpikeAnalysis(List<double> latencyList)
        {
            List<double> SpikedLatency = new();
            double Average = latencyList.Average();
            double variance = latencyList.Select(x => Math.Pow(x - Average, 2)).Average();
            double stDev = Math.Sqrt(variance);
            double thresholds = Average + (2 * stDev);
            foreach (var lat in latencyList)
            {
                if (lat > thresholds)
                {
                    SpikedLatency.Add(lat);
                }
            }
            return SpikedLatency;
        }
        public async Task<WafAnalyserOutput> AnayseOutput()
        {
            List<double> IncreasedLatency = new();
            List<double> DecreasedLatency = new();
            List<ScanOutput> loadedFile = await LoadFile();
            var StatusCodeList = loadedFile.Select(x => x.StatusCode).ToList();
            List<int> DetectedCodes = new();
            List<int> OtherVendorSpecificCodes = new();
            int SusCodePattern = 0;
            foreach (var codes in StatusCodeList)
            {
                if (WafCodes.Contains(codes))
                {
                    DetectedCodes.Add(codes);
                }
                if (codes >= 900)
                {
                    OtherVendorSpecificCodes.Add(codes);
                }
            }
            for (int i = 1; i < StatusCodeList.Count - 1; i++)
            {
                if (StatusCodeList[i] >= 200 && StatusCodeList[i - 1] >= 400)
                    SusCodePattern++;

                if (StatusCodeList[i + 1] >= 400 && StatusCodeList[i] >= 200)
                    SusCodePattern++;
            }

            // Extraction Part 
            var LatencyList = loadedFile.Select(x => x.LatencyMS).ToList();
            var SpikedLatency = LatencySpikeAnalysis(LatencyList);
            for (int i = 0; i < LatencyList.Count - 1; i++)
            {
                if (LatencyList[i + 1] > LatencyList[i])
                {
                    IncreasedLatency.Add(LatencyList[i + 1]);
                }
                else if (LatencyList[i + 1] < LatencyList[i])
                {
                    DecreasedLatency.Add(LatencyList[i + 1]);
                }
            }
            WafAnalyserOutput outputData = new WafAnalyserOutput();
            var TargetList = loadedFile.Select(x => x.Target).ToList();
            var HashResponseList = loadedFile.Select(x => x.ResponseHash).ToList();
            var MessageList = loadedFile.Select(x => x.Message).ToList();
            var Headers = loadedFile.Where(x => x.Headers != null).Select(x => x.Headers!).ToList();
            outputData.ListOfTarget = TargetList;
            outputData.StatusCodeList = StatusCodeList;
            outputData.LatencyList = LatencyList;
            outputData.LatencyInreasing = IncreasedLatency;
            outputData.LatencyDecreasing = DecreasedLatency;
            outputData.SpikedLatency = SpikedLatency;
            outputData.HashResponseList = HashResponseList;
            outputData.OtherVendorSpecificCodes = OtherVendorSpecificCodes;
            outputData.SusCodePattern = SusCodePattern;
            outputData.Message = MessageList;
            outputData.Headers = Headers;
            outputData.DetectedStatusCodes = DetectedCodes;
            return outputData;
        }
    }

    public class WafAnalyserOutput
    {
        public List<string> ListOfTarget { set; get; } = new();
        public List<double> LatencyList { set; get; } = new();
        public List<double> SpikedLatency { set; get; } = new();
        public List<double> LatencyInreasing { set; get; } = new();
        public List<double> LatencyDecreasing { set; get; } = new();
        public List<int> StatusCodeList { set; get; } = new();
        public List<int> DetectedStatusCodes { set; get; } = new();
        public List<string> HashResponseList { set; get; } = new();
        public List<int> OtherVendorSpecificCodes { set; get; } = new();
        public int SusCodePattern { set; get; }
        public List<Dictionary<string, string>> Headers { set; get; } = new();
        public List<string> Message { set; get; } = new();
    }
}