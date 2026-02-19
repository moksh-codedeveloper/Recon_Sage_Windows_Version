using Interface.Network;
using ScanOutputModel;

namespace Adapter
{
    public class InterfaceGoverner:INetwork
    {
        private readonly INetwork _normal;
        private readonly INetwork _tor;
        public InterfaceGoverner(INetwork normal, INetwork tor)
        {
            _normal = normal;
            _tor = tor;
        }

        public async Task<ScanOutput> SendAsync(string domain)
        {
            var result = await _normal.SendAsync(domain);
            return result;
        }
    }
}