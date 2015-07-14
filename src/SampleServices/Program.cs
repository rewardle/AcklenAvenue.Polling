using System;
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
                .RegisterComponents(builder =>
                    {
                        //builder.RegisterType<FakeServie>().As<IFakeService>()
                    })
                .WithTask<ExampleTask>("HOla", "Description", 2)
                .WithTask<OtherTask>("dd", "Description", 4)
                .OnException((sender, ex) =>
                             {
                                 Console.WriteLine("EXCEPTION: " + ex.Message);
                                 return ex;
                             })
                .OnLogDebug((sender, message) => Console.WriteLine("DEBUG: " + message))
                .OnLogInfo((sender, message) => Console.WriteLine("INFO: " + message))
                .OnLogWarning((sender, message) => Console.WriteLine("WARNING: " + message))
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

    public class OtherTask : ITask
    {
        readonly IFakeService _fakeService;

        public OtherTask(IFakeService fakeService)
        {
            _fakeService = fakeService;
        }

        public void Execute()
        {
            throw new Exception("test");
        }
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