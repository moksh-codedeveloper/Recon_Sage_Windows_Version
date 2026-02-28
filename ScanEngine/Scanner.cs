using ScanOutputModel;
using ScanInterface;
namespace ScannerCore
{
    public class Scanner
    {
        public string Target { set; get; } = string.Empty;
        public int Timeout { set; get; }
        public int Concurrency { set; get; }
        public string WordlistPath { set; get; } = string.Empty;

        public Scanner(string _target, int _concurrency, int _timeout, string _wordlistpath)
        {
            Target = _target;
            Concurrency = _concurrency;
            Timeout = _timeout;
            WordlistPath = _wordlistpath;
        }

        public async Task<string[]> wordlistProcess()
        {
            return await File.ReadAllLinesAsync(WordlistPath);
        }
        public async Task<MainScanOutput> ExecuteScan()
        {
            MainScanOutput mainScan = new MainScanOutput();
            var normal = new NormalScan(Target, Concurrency, Timeout);
            string[] wordlist = await wordlistProcess();
            var semaphore = new SemaphoreSlim(Concurrency);
            var tasks = wordlist.Select(async domain =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var result = await normal.SendAsync(domain:domain);
                    lock (mainScan)
                    {
                        mainScan.Result.Add(result);
                    }
                } finally
                {
                    semaphore.Release();
                }
            });
            await Task.WhenAll(tasks);
            return mainScan;
        }
    }
}