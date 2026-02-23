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
        public bool TorScan{set;get;}
        public bool NormalScan{set;get;}
        public bool AdaptiveSwitch{set;get;}

        public Scanner(string _target, int _concurrency, int _timeout, string _wordlistpath, bool tor_scan, bool normal_scan, bool adaptive_switch)
        {
            Target = _target;
            Concurrency = _concurrency;
            Timeout = _timeout;
            WordlistPath = _wordlistpath;
            TorScan = tor_scan;
            NormalScan = normal_scan;
            AdaptiveSwitch = adaptive_switch;
        }

        public async Task<string[]> wordlistProcess()
        {
            return await File.ReadAllLinesAsync(WordlistPath);
        }

        public async Task<MainScanOutput> AdaptiveSwitcher()
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
        public async Task<MainScanOutput> Normal_Scan()
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
        public async Task<MainScanOutput> Tor_Scan()
        {
            MainScanOutput mainScan = new MainScanOutput();
            var tor = new TorNetwork(Target, Timeout);
            string[] wordlist = await wordlistProcess();
            var semaphore = new SemaphoreSlim(Concurrency);
            var tasks = wordlist.Select(async domain =>
            {
               await semaphore.WaitAsync();
                try
                {
                    var result = await tor.SendAsync(domain);
                    lock (mainScan)
                    {
                        mainScan.Result.Add(result);
                    }
                }
                finally
                {
                    semaphore.Release();
                } 
            });
            await Task.WhenAll(tasks);
            return mainScan;
        }
        public async Task<MainScanOutput> ExecuteScan()
        {
            if(NormalScan == true && TorScan == true && AdaptiveSwitch == true)
            {
                MainScanOutput normalScan = await Normal_Scan();
                return normalScan;
            } else if(TorScan == true && NormalScan == false && AdaptiveSwitch == false)
            {
                MainScanOutput torScan = await Tor_Scan();
                return torScan;
            }
            else if(AdaptiveSwitch == true && NormalScan == false && TorScan == false)
            {
                MainScanOutput adaptiveSwitch = await AdaptiveSwitcher();
                return adaptiveSwitch;
            }
            return new MainScanOutput();
        }
    }
}