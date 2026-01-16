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
            WafScan wafScanner = new WafScan(args: args);
            var results = await wafScanner.MainScan();
            Console.WriteLine("[+] WAF Scan Completed. Generating Analysis Report...");
            await DataExporter.ExportToJsonAsync(results, wafScanner.JsonFilePath);
            Console.WriteLine("[+] WAF Scan Report Generated.");
            var analyser = new WafAnalyser(jsonFilePath:wafScanner.JsonFilePath);
            Console.WriteLine("[+] Analyzing WAF Scan Results...");
            var analysisResults = await analyser.AnayseOutput();
            Console.WriteLine("[+] WAF Analysis Report Generated.");
            await DataExporter.ExportToJsonAsync(analysisResults, "Waf_Analysis_Report.json");
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
