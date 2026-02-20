using ScannerCore;
using ScanModels.CLIVersion;
using ScanOutputModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppEngine
{
    public class App
    {
        public string Target { set; get; } = string.Empty;
        public int Concurrency { set; get; }
        public int Timeout { set; get; }
        public string JsonFilePath { set; get; } = string.Empty;
        public string WordlistPath { set; get; } = string.Empty;

        public async Task RunScan(string[] args)
        {
            var cliEngine = new CLIMainEngine().ProcessCLiArgs(args: args);
            Console.WriteLine("Arguements processing done now starting the scan.....");
            Target = cliEngine.Target;
            Concurrency = cliEngine.Concurrency;
            Timeout = cliEngine.Timeout;
            JsonFilePath = cliEngine.JsonFilePath;
            WordlistPath = cliEngine.WordlistPath;
            Scanner scanner = new Scanner(Target, Concurrency, Timeout, WordlistPath);
            MainScanOutput mainScan = await scanner.ExecuteScan();
            PrintToConsole(mainScan);
            await WriteToJsonAsync(mainScan, JsonFilePath);
        }
        public async Task WriteToJsonAsync(MainScanOutput mainOutput, string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(mainOutput, options);

            await File.WriteAllTextAsync(filePath, json);

            Console.WriteLine($"JSON output written to: {filePath}");
        }
        public void PrintToConsole(MainScanOutput mainOutput)
        {
            Console.WriteLine("========== ReconSage Scan Results ==========\n");

            foreach (var result in mainOutput.Result)
            {
                Console.WriteLine("--------------------------------------------");
                Console.WriteLine($"Target     : {result.Target}");
                Console.WriteLine($"StatusCode : {result.StatusCode}");
                Console.WriteLine($"Latency    : {result.LatencyMS} ms");
                Console.WriteLine($"Message    : {result.Message}");

                if (result.Headers.Count > 0)
                {
                    Console.WriteLine("Headers:");
                    foreach (var header in result.Headers)
                    {
                        Console.WriteLine($"   {header.Key} : {header.Value}");
                    }
                }
            }
            Console.WriteLine("\n============================================");
        }
    }
}