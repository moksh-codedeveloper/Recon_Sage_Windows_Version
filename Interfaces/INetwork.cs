using ScanOutputModel;

namespace Interface.Network
{
    public interface INetwork
    {
        Task<ScanOutput> SendAsync(string Domain);
    }
}