namespace ResoModel
{
    public class RModel
    {
        public string Target{set;get;} = string.Empty;
        public int Concurrency{set;get;}
        public int Timeout{set;get;} 
        public string JsonFilePath{set;get;} = string.Empty;
        public string WordlistPath{set;get;} = string.Empty;
        public bool TorScan{set;get;}
        public bool NormalScan{set;get;}
        public bool AdaptiveSwitch{set;get;}
    }
}