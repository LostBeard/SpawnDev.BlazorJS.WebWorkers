namespace SpawnDev.BlazorJS.WebWorkers.Demo.Services
{
    public interface ITestService
    {
        string Id { get; set; }
        string Key { get; set; }
        Task<string> GetId();
    }
    public class TestService : ITestService
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Key { get; set; } = "";
        public async Task<string> GetId()
        {
            return Id;
        }
        public TestService()
        {
            Console.WriteLine($"TestService() **************************");
        }
        public TestService(string key)
        {
            Console.WriteLine($"TestService({key}) **************************");
            Key = key;
        }
    }
}
