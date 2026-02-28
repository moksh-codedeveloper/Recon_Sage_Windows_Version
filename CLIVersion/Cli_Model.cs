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
                        string jsonFilePath = args[++i];
                        if(!jsonFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            throw new Exception("The file should be the json strictly not the  other  files");
                        options.JsonFilePath = jsonFilePath;
                        break;
                    case "--wordlist":
                        string wordlistFilePath = args[++i];
                        if(!wordlistFilePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                            throw new Exception("The file should be text strictly not the  other  files");
                        options.WordlistPath = wordlistFilePath;
                        break;
                }
            }
            return options;
        }
    }
}
