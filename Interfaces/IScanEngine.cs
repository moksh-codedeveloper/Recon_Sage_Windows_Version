namespace Interface.ScanEngine{
    public interface IScanEngine
    {
        Task<List<Object>> ExecuteAsync();
    }
}   