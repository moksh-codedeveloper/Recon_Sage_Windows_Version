using System;
using System.Collections.Generic;

namespace ScanModels.CLIVersion
{
    public class CLIMainEngine
    {
        public string Target { set; get; } = string.Empty;
        public int Concurrency { set; get; }
        public int Timeout { set; get; }
        public string JsonFilePath { set; get; } = string.Empty;
        public string  WordlistPath{set; get;} = string.Empty;
        public bool TorScan{set;get;}
        public bool NormalScan{set;get;}
        public bool AdaptiveSwitch{set;get;}
        public CLIMainEngine ProcessCLiArgs(string[] args)
        {
            CLIMainEngine options = new CLIMainEngine();
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--target":
                        options.Target = args[++i];
                        break;

                    case "--concurrency":
                        if (!int.TryParse(args[++i], out int concurrency) || concurrency <= 0)
                            throw new Exception("Invalid concurrency");
                        options.Concurrency = concurrency;
                        break;

                    case "--timeout":
                        if (!int.TryParse(args[++i], out int timeout) || timeout <= 0)
                            throw new Exception("Invalid timeout");
                        options.Timeout = timeout;
                        break;

                    case "--json":
                        options.JsonFilePath = args[++i];
                        break;
                    case "--wordlist":
                        options.WordlistPath = args[++i];
                        break;
                    case "--tor-scan":
                        if(!bool.TryParse(args[++i], out bool tor_scan))
                            throw new Exception("Invalid only the boolean values for tor_scan");
                        options.TorScan = tor_scan;
                        break;
                    case "--normal-scan":
                        if(!bool.TryParse(args[++i], out bool normal_scan))
                            throw new Exception("Invalid only the boolean values for normal_scan");
                        options.NormalScan = normal_scan;
                        break;
                    case "--adaptive-scan":
                        if(!bool.TryParse(args[++i], out bool adaptive_switch))
                            throw new Exception("Invalid only the boolean values for adaptive_switch");
                        options.AdaptiveSwitch = adaptive_switch;
                        break;
                }
            }
            return options;
        }
    }
}
