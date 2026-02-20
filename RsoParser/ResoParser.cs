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

            // NUMBER VALIDATION
            if (!int.TryParse(data["concurrency"], out int concurrency) || concurrency <= 0)
                throw new Exception("Invalid concurrency value.");

            if (!int.TryParse(data["timeout"], out int timeout) || timeout <= 0)
                throw new Exception("Invalid timeout value.");

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
                Target = data.GetValueOrDefault("target"),
                Concurrency = int.Parse(data.GetValueOrDefault("concurrency")),
                Timeout = int.Parse(data.GetValueOrDefault("timeout")),
                JsonFilePath = data.GetValueOrDefault("json_file_path"),
                WordlistPath = data.GetValueOrDefault("wordlist_path")
            };
        }
    }
}