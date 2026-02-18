namespace ScanOutputModel
{
    public class ScanOutput
    {
        public string Target{set;get;} = string.Empty;
        public List<double> LatencyMS{set;get;} = new();
        public List<string> ListMessage{set;get;} = new();
        public List<Dictionary<string, string>?> Headers{set;get;} = new();
        public List<int> StatusCodeList{set;get;} = new();
        public List<string> TraversedDirectory{set;get;} = new();

    }
}