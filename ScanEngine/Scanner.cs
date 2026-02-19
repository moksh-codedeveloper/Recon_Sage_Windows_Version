using Interface.Network;
using ScanOutputModel;
using Adapter;
using ScanInterface;
namespace ScannerCore
{
    public class Scanner
    {
        public string Target { set; get; } = string.Empty;
        public int Timeout { set; get; }
        public int Concurrency { set; get; }
        public string WordlistPath { set; get; } = string.Empty;
        public string JsonPath { set; get; } = string.Empty;
        public string JsonFileName { set; get; } = string.Empty;

        public Scanner(string _target, int _concurrency, int _timeout, string _wordlistpath, string _jsonFileName, string _jsonFilePath)
        {
            Target = _target;
            Concurrency = _concurrency;
            Timeout = _timeout;
            WordlistPath = _wordlistpath;
            JsonPath = _jsonFilePath;
            JsonFileName = _jsonFileName;
        }

        public async Task<string[]> wordlistProcess()
        {
            return await File.ReadAllLinesAsync(WordlistPath);
        }

        public async Task<MainScanOutput> ExecuteScan()
        {
            var normal = new NormalScan(Target, Concurrency, Timeout);
            var tor = new TorNetwork(Target, Timeout);

            INetwork network = new InterfaceGoverner(normal, tor);

            string[] wordlist = await wordlistProcess();

            var mainScanOutput = new MainScanOutput();
            var semaphore = new SemaphoreSlim(Concurrency);

            var tasks = wordlist.Select(async domain =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var result = await network.SendAsync(domain);
                    lock (mainScanOutput)
                    {
                        mainScanOutput.Result.Add(result);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            return mainScanOutput;
        }

    }
}