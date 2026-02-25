using System.Diagnostics;
using System.Net;

namespace TcpCliWrapper
{
    public class TcpCli
    {
        public IPAddress Host { set; get; }
        public int Port { set; get; }
        public int Timeout { set; get; }
        public string Target { set; get; } = string.Empty;
        public string WordlistPath { set; get; } = string.Empty;
        public string JsonFilePath { set; get; } = string.Empty;
        public string Password { set; get; } = string.Empty;
        public void argsProcess(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--target":
                        Target = args[++i];
                        break;
                    case "--timeout":
                        if (!int.TryParse(args[++i], out int timeout))
                            throw new Exception("Actually do you know that the port are suppose to be the integers if you forgot then its reminder");
                        Timeout = timeout;
                        break;
                    case "--host":
                        if(!IPAddress.TryParse(args[++i], out IPAddress ip))
                            throw new Exception("Are you newbie who don't know that the host are supposed to be the ip address not the other value");
                        Host = ip;
                        break;
                    case "--port":
                        if (!int.TryParse(args[++i], out int port))
                            throw new Exception("Actually do you know that the port are suppose to be the integers if you forgot then its reminder");
                        Port = port;
                        break;
                    case "--password":
                        string pass = args[++i];
                        if (string.IsNullOrWhiteSpace(pass))
                            throw new Exception("You have null as your pass i don't buy it");
                        Password = pass;
                        break;
                    case "--json":
                        string filepath = args[++i];
                        if (!filepath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(filepath))
                            throw new Exception("You serriously don't know what the json is huhh go home kid and remove this tool seriously");
                        JsonFilePath = args[++i];
                        break;
                    case "--wordlist":
                        string wordlistpath = args[++i];
                        if (!wordlistpath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(wordlistpath))
                            throw new Exception("You serriously don't know what the wordlist is huhh go home kid and remove this tool seriously");
                        WordlistPath = wordlistpath;
                        break;
                }
            }
        }
    }
}