using System;
using ScanModels.CLIVersion;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace ScanModels.Waf_Scanner
{
    public class WafScan
    {
        public string[] Args { init; get; }
        public string Target = string.Empty;
        public SemaphoreSlim _concurrency;
        public int Timeout;
        public string WordlistPath = string.Empty;
        public string JsonFilePath = string.Empty;
        public int Concurrency;
        public HttpClient _client;
        public CLIMainEngine cliEngine;
        public WafScan(string[] args)
        {
            Args = args;
            cliEngine = new CLIMainEngine().ProcessCLiArgs(args: Args);
            Target = cliEngine.Target;
            WordlistPath = cliEngine.WordlistPath;
            JsonFilePath = cliEngine.JsonFilePath;
            _concurrency = new SemaphoreSlim(Concurrency);
            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("ReconSage/1.0");
        }
        public async Task<ScanOutput> Scan(string domain)
        {
            var subTarget = string.Empty;
            await _concurrency.WaitAsync();
            if (Target.EndsWith("/"))
            {
                subTarget = Target + domain;
            }
            else
            {
                subTarget = Target + "/" + domain;
            }
            var sw = Stopwatch.StartNew();
            var dataOutput = new ScanOutput();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));
                var result = await _client.GetAsync(subTarget, cts.Token);
                sw.Stop();
                var StatusCode = (int)result.StatusCode;
                string ResponseBody = await result.Content.ReadAsStringAsync();
                byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(ResponseBody));
                string responseHex = Convert.ToHexString(hash);
                dataOutput.Target = subTarget;
                dataOutput.StatusCode = StatusCode;
                dataOutput.ResponseHash = responseHex;
                dataOutput.LatencyMS = sw.ElapsedMilliseconds;
                dataOutput.Headers = result.Headers
                .Concat(result.Content.Headers)
                .ToDictionary(
                    h => h.Key,
                    h => string.Join(", ", h.Value)
                );
                dataOutput.Message = result?.ReasonPhrase;
            }
            catch (Exception ex)
            {
                dataOutput.Message = ex.Message;
                dataOutput.Target = subTarget;
                dataOutput.StatusCode = -1;
                dataOutput.LatencyMS = sw.ElapsedMilliseconds;
                dataOutput.ResponseHash = "";
                dataOutput.Headers = new Dictionary<string, string>();
            }
            finally
            {
                _concurrency.Release();
            }
            return dataOutput;
        }
        public string[] ProcessWordlist()
        {
            var wordlist = File.ReadAllLines(WordlistPath);
            return wordlist.Length > 20 ? wordlist.Take(20).ToArray() : wordlist;
        }
        public async Task<List<ScanOutput>> MainScan()
        {
            Console.WriteLine("[+] Starting WAF Detection Scan......");
            string[] wordlist = ProcessWordlist();
            List<Task<ScanOutput>> scanOutputs = new();
            foreach(var words in wordlist)
            {
                scanOutputs.Add(Scan(words));
            }

            var results = await Task.WhenAll(scanOutputs);
            return results.ToList();
        }
    }
    public class ScanOutput
    {
        public string Target { set; get; } = string.Empty;
        public int StatusCode { set; get; }
        public Dictionary<string, string>? Headers { set; get; } = new Dictionary<string, string>();
        public string ResponseHash { set; get; } = string.Empty;
        public double LatencyMS { set; get; }
        public string Message { set; get; } = string.Empty;
    }
}