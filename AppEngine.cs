using System.IO;
using System.Text.Json;
using ScanModels.DirBruteScan;
using ScanModels.WarmupModel;
using ScanModels.RateLimitModule;
using ScanModels.Waf_Scanner;
using ScanModels.WafAnalyser;

namespace AppEngine
{
    public sealed class AppEngineModel
    {
        public string[] args { get; init; }
        public AppEngineModel(string[] Args)
        {
            args = Args.Length > 0
            ? Args
            : throw new ArgumentException("No CLI arguments provided");
        }
        public async Task DirScan()
        {
            DirScanEngine scanEngine = new DirScanEngine(args);
            List<Task<DirScanEngine.ScanOutputModel>> tasks = new();
            string[] wordlists = scanEngine.ProcessWordlist();
            foreach (var words in wordlists)
            {
                tasks.Add(scanEngine.Scan(words));
            }
            var results = await Task.WhenAll(tasks);
            await DataExporter.ExportToJsonAsync(results, "DirScan");
            Console.WriteLine("[+]The scan is complete and results are written in the Json file");
        }
        public async Task WarmScan()
        {
            WarmupScan warmupScan = new WarmupScan(Args: args);
            var results = await warmupScan.MainScan();
            await DataExporter.ExportToJsonAsync(results, warmupScan.JsonFilePath);
        }
        public async Task RateLimitScan()
        {
            RateLimitModule rateLimit = new RateLimitModule(args: args);
            var results = await rateLimit.mainScan();
            await DataExporter.ExportToJsonAsync(results, rateLimit.JsonFilePath);
        }
        public async Task WafScan()
        {
            WafScan scan = new WafScan(args);
            var results = await scan.MainScan();

            await DataExporter.ExportToJsonAsync(results, scan.JsonFilePath);
            Console.WriteLine("[+] Scan is done lets start analysis of json file");

            WafAnalyser analyser = new WafAnalyser(scan.JsonFilePath);
            var scanFile = await analyser.AnayseOutput();

            Console.WriteLine("\n[0] Vendor Specific Status Codes");
            if (scanFile.OtherVendorSpecificCodes.Count == 0)
                Console.WriteLine("\t[-] None detected");

            foreach (var code in scanFile.OtherVendorSpecificCodes)
                Console.WriteLine($"\t[-] {code}");

            Console.WriteLine("\n[0.1] WAF Related Status Codes");
            if (scanFile.DetectedStatusCodes.Count == 0)
                Console.WriteLine("\t[-] None detected");

            foreach (var code in scanFile.DetectedStatusCodes.Distinct())
                Console.WriteLine($"\t[-] {code}");

            Console.WriteLine("\n[2] Latency Analysis");

            foreach (var lat in scanFile.LatencyInreasing)
                Console.WriteLine($"[2.1] Increased -> {lat}ms");

            foreach (var lat in scanFile.LatencyDecreasing)
                Console.WriteLine($"[2.2] Decreased -> {lat}ms");

            foreach (var lat in scanFile.SpikedLatency)
                Console.WriteLine($"[2.3] Spike -> {lat}ms");

            Console.WriteLine("\n[3] Status Code Distribution");

            var groupedCodes = scanFile.StatusCodeList
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count());

            foreach (var g in groupedCodes)
                Console.WriteLine($"\t[-] {g.Key} => {g.Count()} times");

            Console.WriteLine("\n[4] Response Hash Analysis");

            var hashGroups = scanFile.HashResponseList
                .Where(h => !string.IsNullOrWhiteSpace(h))
                .GroupBy(h => h)
                .ToList();

            foreach (var g in hashGroups)
                Console.WriteLine($"\t[-] {g.Key[..12]}... => {g.Count()} times");

            if (hashGroups.Count > 1)
                Console.WriteLine("\n[!!] ANOMALY: Multiple different response bodies detected");
            else
                Console.WriteLine("\n[+] Stable response body detected");
        }

        public async Task Run()
        {
            switch (args[0])
            {
                case "--dir":
                    await DirScan();
                    break;
                case "--warmup":
                    await WarmScan();
                    break;
                case "--waf":
                    await WafScan();
                    break;
                case "--rate-limit":
                    await RateLimitScan();
                    break;
            }
        }
    }
}

public static class DataExporter
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task ExportToJsonAsync<T>(T data, string jsonFilePath)
    {
        ArgumentNullException.ThrowIfNull(data);

        string finalPath = ResolveFilePath(jsonFilePath);

        await using FileStream createStream = new(
            finalPath,
            FileMode.CreateNew,   // guarantees no overwrite
            FileAccess.Write,
            FileShare.None
        );

        await JsonSerializer.SerializeAsync(createStream, data, _options);

        Console.WriteLine($"[+] JSON Report Generated: {finalPath}");
    }
    private static string ResolveFilePath(string originalPath)
    {
        if (!File.Exists(originalPath))
            return originalPath;

        string directory = Path.GetDirectoryName(originalPath) ?? "";
        string filename = Path.GetFileNameWithoutExtension(originalPath);
        string extension = Path.GetExtension(originalPath);

        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        string versionedName = $"{filename}_{timestamp}{extension}";
        return Path.Combine(directory, versionedName);
    }
}
