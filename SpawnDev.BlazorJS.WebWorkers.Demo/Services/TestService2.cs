namespace SpawnDev.BlazorJS.WebWorkers.Demo.Services
{

    public interface ITestService2
    {
        Task<string> GetId();
    }
    public class TestService2 : ITestService2
    {
        public string Id { get; set; }
        public TestService2(string id)
        {
            Id = id;
        }
        public Task<string> GetId() => Task.FromResult(Id);
    }
    public class TestService3<T> : ITestService2
    {
        public T Value { get; set; }
        public string Id { get; set; }
        public TestService3(string id)
        {
            Id = id;
        }
        public Task<string> GetId() => Task.FromResult(Id);
    }
}
