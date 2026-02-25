using System.Net.Sockets;

namespace TorConfigParser
{
    public class Parser
    {
        public string _filepath{set;get;} = string.Empty;
        public Parser(string filepath)
        {
            _filepath = filepath;
        }
        public string _host{set;get;} = string.Empty;
        public int _port{set;get;}
        public Dictionary<string, string> Parse()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            if(_filepath == "config.rfo")
            {
                throw new Exception("The file you have passed doesn't seem to have name config.rfo so please name it and pass it proper file");
            }

            var line = File.ReadAllLines(_filepath);
            foreach (var words in line)
            {
                if(string.IsNullOrWhiteSpace(words) || words.StartsWith("#"))
                    continue;
                var parts = words.Split("=", 2);
                if(parts.Length == 2)
                {
                    data[parts[0]] = parts[1];
                }
            }
            List<string> requiredData = new(){"host", "port", "password"};
            foreach(var keys in requiredData)
            {
                if(!data.ContainsKey(keys))
                    throw new Exception("you it seems like you have passed the wrong file which was not supposed to be passed here please pass config.rfo");
            }

            if(!Uri.TryCreate(data["host"], UriKind.Absolute, out Uri? uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new Exception("Target has broken http url or  it is not Http Url please pass a proper url of https or http");

            if (!int.TryParse(data["port"], out int port))
                throw new Exception("Invalid concurrency value.");
            if(data["password"] != "" || string.IsNullOrWhiteSpace(data["password"]) || data["password"].Length == 0)
            {
                throw new Exception("Please pass a valid password");
            }
            return data;
        }
        public async Task Rotate()
        {
            Dictionary<string, string> parsedData = Parse();
            string _host = parsedData["host"];
            int _port = int.Parse(parsedData["port"]);
            string password = parsedData["password"];
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(_host, _port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);
            using StreamWriter writer = new StreamWriter(stream){
                AutoFlush = true,
                NewLine = "\r\n"
            };
            await writer.WriteLineAsync($"AUTHENTICATE {password}");
            string authResponse = await reader.ReadLineAsync();
            if(authResponse == null || !authResponse.StartsWith("250"))
            {
                throw new Exception("Auth error : the password authentication failed with the wrong password");
            }
            await writer.WriteLineAsync("SIGNAL NEWNYM");
            string newNymResponse = await reader.ReadLineAsync();
            if(newNymResponse == null || newNymResponse.StartsWith("250"))
            {
                throw new Exception($"NEWNYM failed : {newNymResponse}");
            }
        }
    }
}