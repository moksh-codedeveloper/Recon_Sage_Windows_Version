using AppEngine;

public class Program
{
    static async Task Main(string[] args)
    {
        AppEngineModel app = new AppEngineModel(Args:args);
        await app.Run();
    }
}