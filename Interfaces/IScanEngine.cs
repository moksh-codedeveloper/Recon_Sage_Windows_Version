using ScanOutputModel;

namespace Interface.ScanEngine{
    public interface IScanEngine
    {
        Task<ScanOutput> ExecuteAsync();
    }
}   