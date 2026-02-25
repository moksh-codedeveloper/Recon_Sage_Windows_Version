using System.Net;
using System.Net.Sockets;

namespace TorConfigParser
{
    public class Parser
    {
        public string _filepath { set; get; } = string.Empty;
        public Parser(string filepath)
        {
            _filepath = filepath;
        }
        public IPAddress _host { set; get; }
        public int _port { set; get; }
        public string password {set;get;} = string.Empty;
        public string Target {set;get;} = string.Empty;
        public int Timeout{set;get;}
        public string JsonFilePath{set;get;} = string.Empty;
        public string WordlistPath{set;get;} = string.Empty;

        public Dictionary<string, string> Parse()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (_filepath == "config.rfo")
            {
                throw new Exception("The file you have passed doesn't seem to have name config.rfo so please name it and pass it proper file");
            }

            var line = File.ReadAllLines(_filepath);
            foreach (var words in line)
            {
                if (string.IsNullOrWhiteSpace(words) || words.StartsWith("#"))
                    continue;
                var parts = words.Split("=", 2);
                if (parts.Length == 2)
                {
                    data[parts[0].Trim()] = parts[1].Trim();
                }
            }
            List<string> requiredData = new() { "host", "port", "password", "target", "tiemout", "json_file_path", "wordlist_path" };
            foreach (var keys in requiredData)
                if (!data.ContainsKey(keys))
                    throw new Exception("you it seems like you have passed the wrong file which was not supposed to be passed here please pass config.rfo");

            if (!int.TryParse(data["port"], out int port))
                throw new Exception("Invalid concurrency value.");

            if (!int.TryParse(data["timeout"], out int timeout) || timeout <= 0)
                throw new Exception("The provided timeout value is not acceptable here so yeah please pass something legit");

            if (data["password"] != "" || string.IsNullOrWhiteSpace(data["password"]) || data["password"].Length == 0)
                throw new Exception("Please pass a valid password");

            if (!Uri.TryCreate(data["target"], UriKind.Absolute, out Uri? target) || (target.Scheme != Uri.UriSchemeHttp && target.Scheme != Uri.UriSchemeHttps))
                throw new Exception("Target has broken http url or  it is not Http Url please pass a proper url of https or http");
            bool valid = IPAddress.TryParse(data["host"], out IPAddress ip);
            if (!valid)
                throw new Exception("There is something wrong with the ip you passed here in host so yeah please verify");
            // FILE EXTENSION CHECKS
            if (!data["wordlist_path"].EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Wordlist must be .txt file.");

            if (!data["json_file_path"].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Output file must be .json.");
            return data;
        }
        public async Task Rotate()
        {
            Dictionary<string, string> parsedData = Parse();
            _host = IPAddress.Parse(parsedData["host"]);
            _port = int.Parse(parsedData["port"]);
            password = parsedData["password"];

            using TcpClient client = new TcpClient();

            await client.ConnectAsync(_host, _port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);
            using StreamWriter writer = new StreamWriter(stream)
            {
                AutoFlush = true,
                NewLine = "\r\n"
            };
            await writer.WriteLineAsync($"AUTHENTICATE {password}");
            string authResponse = await reader.ReadLineAsync();
            if (authResponse == null || !authResponse.StartsWith("250"))
            {
                throw new Exception("Auth error : the password authentication failed with the wrong password");
            }
            await writer.WriteLineAsync("SIGNAL NEWNYM");
            string newNymResponse = await reader.ReadLineAsync();
            if (newNymResponse == null || newNymResponse.StartsWith("250"))
            {
                throw new Exception($"NEWNYM failed : {newNymResponse}");
            }
        }
    }
}