using AcklenAvenue.Poller;

using Autofac;

using Topshelf;

namespace SampleServices
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = new PollerBuilder();

            x.SetDescription("Rewardle's Points Commands Service")
             .SetDisplayName("PointsCommands")
             .SetServiceName("PointsCommands")
             .OverideServiceConfiguration(d => d.RunAsLocalService())
             .RegisterComponents(builder => builder.RegisterType<FakeServie>().As<IFakeService>())
             .WithTask<ExampleTask>("ExampleName", "Description", 2)
             .Build()
             .Start();
        }
    }

    public interface IFakeService
    {
    }

    public class FakeServie : IFakeService
    {
    }

    public class ExampleTask : ITask
    {
        readonly IFakeService _fakeService;

        public ExampleTask(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }
        
        public void Execute()
        {
        }
    }
}