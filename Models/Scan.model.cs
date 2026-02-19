namespace ScanOutputModel
{
    public class ScanOutput
    {
        public string Target { set; get; } = string.Empty;
        public double LatencyMS { set; get; }
        public string Message { set; get; } = string.Empty;
        public Dictionary<string, string> Headers { set; get; } = new Dictionary<string, string>();
        public int StatusCode { set; get; }
    }

    public class MainScanOutput
    {
        public List<ScanOutput> Result{set;get;} = new();
    }
}