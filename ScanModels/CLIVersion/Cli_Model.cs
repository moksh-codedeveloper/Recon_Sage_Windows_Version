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
                        if (!int.TryParse(args[++i], out int concurrency))
                            throw new Exception("Invalid concurrency");
                        options.Concurrency = concurrency;
                        break;

                    case "--timeout":
                        if (!int.TryParse(args[++i], out int timeout))
                            throw new Exception("Invalid timeout");
                        options.Timeout = timeout;
                        break;

                    case "--json":
                        options.JsonFilePath = args[++i];
                        break;
                    case "--wordlist":
                        options.WordlistPath = args[++i];
                        break;
                }
            }
            return options;
        }
    }
}
