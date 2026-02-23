namespace RSFParser
{
    public class RFParser
    {
        public bool _adaptive_switching{set;get;}
        public bool _normal_scan{set;get;}
        public bool _tor_scan{set;get;}
        public string FilePath{set;get;} = string.Empty;
        public string ProxyPort{set;get;} = string.Empty;
        public string ProxyIP{set;get;} = string.Empty;
        public RFParser(bool tor_scan, bool adaptive_switch, bool normal_scan, string file_path, string  Proxy_Port, string Proxy_IP)
        {
            FilePath = file_path;
            _adaptive_switching = adaptive_switch;
            _tor_scan = tor_scan;
            _normal_scan = normal_scan;
            ProxyPort = Proxy_Port;
            ProxyIP = Proxy_IP;
        }

        public Dictionary<string, string> Parse()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            foreach(var line in File.ReadAllLines(FilePath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                var parts = line.Split("=" ,2);
                if(parts.Length == 2)
                {
                    data[parts[0].Trim()] = parts[1].Trim();
                }
            }

            string[] requiredFields = {"adaptive_switch", "tor_scan", "normal_scan", "proxy_port", "proxy_ip"};
            foreach(var keys in requiredFields)
            {
                if (!data.ContainsKey(keys))
                {
                    throw new Exception("The required keys are not present please pass them as they are necessary for the use cases");
                }
            }
            return data;
        }
    }
}