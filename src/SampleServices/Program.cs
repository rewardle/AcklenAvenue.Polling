using System;
using System.Threading;
using System.Threading.Tasks;
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
                                        builder.RegisterType<FakeServie>().As<IFakeService>();
                                    })
                .WithMultipleThreadsPerTask<ExampleTask>("HOla", "Description", 1, 3)
                //.WithTask<OtherTask>("dd", "Description", 4)
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
            var taskFactory = new TaskFactory();
            var task = taskFactory.StartNew(() =>
                                            {
                                                Console.WriteLine("Start task ");

                                                Thread.Sleep(10000);
                                                Console.Write("End Task");
                                            });

            Task.WaitAll(new[] {task});
        }
    }
}