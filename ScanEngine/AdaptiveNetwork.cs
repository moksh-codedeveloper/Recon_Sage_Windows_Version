using Interface.Network;
using ScanOutputModel;

namespace Adapter
{
    public class InterfaceGoverner : INetwork
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
            Console.WriteLine("[+]Normal scanning started.....");
            var result = await _normal.SendAsync(domain);
            if (IsBlocked(result))
            {
                Console.WriteLine("[+]Tor scanning activated.......");
                return await _tor.SendAsync(domain);
            }
            return result;
        }

        private bool IsBlocked(ScanOutput result)
        {
            return result.StatusCode == 403 ||
                   result.StatusCode == 429 ||
                   result.StatusCode == 503;
        }
    }
}