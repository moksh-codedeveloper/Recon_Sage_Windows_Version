namespace Interface.Network
{
    public interface INetwork
    {
        Task<List<Object>> SendAsync();
    }
}