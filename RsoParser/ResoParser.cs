using System.ComponentModel;
using System.Numerics;
using ResoModel;

namespace ResoParser
{
    public class Parser
    {
        public string RsoFilePath { set; get; } = string.Empty;
        public Parser(string filepath)
        {
            RsoFilePath = filepath;
        }

        public Dictionary<string, string> Parse()
        {
            Dictionary<string, string> data = new();

            foreach (var line in File.ReadAllLines(RsoFilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split("=", 2);
                if (parts.Length == 2)
                {
                    data[parts[0].Trim()] = parts[1].Trim();
                }
            }

            // REQUIRED KEYS CHECK
            string[] requiredKeys = { "target", "concurrency", "timeout", "json_file_path", "wordlist_path" };

            foreach (var key in requiredKeys)
            {
                if (!data.ContainsKey(key))
                    throw new Exception($"Missing required key: {key}");
            }
            // TARGET validation
            if(!Uri.TryCreate(data["target"], UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new Exception("Target has broken http url or  it is not Http Url please pass a proper url of https or http");
            }
            // NUMBER VALIDATION
            if (!int.TryParse(data["concurrency"], out int concurrency) || concurrency <= 0)
                throw new Exception("Invalid concurrency value.");

            if (!int.TryParse(data["timeout"], out int timeout) || timeout <= 0)
                throw new Exception("Invalid timeout value.");
            if(!bool.TryParse(data["tor_scan"], out bool tor_scan))
                throw new Exception("ERROR in parsing the values of tor_scan in bool please pass true or false for if you want it or not");
            if(!bool.TryParse(data["normal_scan"], out bool normal_scan))
                throw new Exception("ERROR in parsing the values of normal_scan in bool please pass true or false for if you want it or not");
            if(!bool.TryParse(data["tor_scan"], out bool adaptive_switch))
                throw new Exception("ERROR in parsing the values normal_scan in bool please pass true or false for if you want it or not");

            // FILE EXTENSION CHECKS
            if (!data["wordlist_path"].EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Wordlist must be .txt file.");

            if (!data["json_file_path"].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Output file must be .json.");
            return data;
        }

        public RModel ParseDictToObject(Dictionary<string, string> data)
        {
            return new RModel
            {
                Target = data["target"],
                Concurrency = int.Parse(data["concurrency"]),
                Timeout = int.Parse(data["timeout"]),
                JsonFilePath = data["json_file_path"],
                WordlistPath = data["wordlist_path"],
                TorScan = bool.Parse(data["tor_scan"]),
                NormalScan = bool.Parse(data["normal_scan"]),
                AdaptiveSwitch = bool.Parse(data["adaptive_switch"])
            };
        }
    }
}