using System;

using Autofac;
using Autofac.Extras.Quartz;

using Common.Logging;

using Quartz;
using Quartz.Spi;

using Topshelf;
using Topshelf.Autofac;
using Topshelf.HostConfigurators;
using Topshelf.Quartz;
using Topshelf.ServiceConfigurators;

namespace AcklenAvenue.Poller
{
    class Program
    {
        static IContainer _container;

        static readonly ILog s_log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            _container = ConfigureContainer(new ContainerBuilder()).Build();

            HostFactory.Run(
                x =>
                    {
                        x.UseAutofacContainer(_container);
                        x.Service<JobsManager>(
                            s =>
                                {
                                    s.ConstructUsingAutofacContainer();
                                    s.WhenStarted(tc => tc.Start());
                                    s.WhenStopped(
                                        tc =>
                                            {
                                                tc.Stop();
                                                _container.Dispose();
                                            });
                                    ConfigureBackgroundJobs(s);
                                });

                        x.RunAsLocalSystem();

                        x.SetDescription("Rewardle's Points Commands Service");
                        x.SetDisplayName("PointsCommands");
                        x.SetServiceName("PointsCommands");
                    });
        }

        static void ConfigureBackgroundJobs(ServiceConfigurator<JobsManager> svc)
        {
            svc.UsingQuartzJobFactory(() => _container.Resolve<IJobFactory>());
            svc.ScheduleQuartzJob(
                q =>
                    {
                        q.WithJob(
                            JobBuilder.Create().OfType()
                        //.WithIdentity("CommandsHandler", "Commands").Build);
                        q.AddTrigger(
                            () =>
                            TriggerBuilder.Create().WithSchedule(SimpleScheduleBuilder.RepeatSecondlyForever(2)).Build());
                    });
        }

        internal static ContainerBuilder ConfigureContainer(ContainerBuilder cb)
        {
            cb.RegisterModule(new QuartzAutofacFactoryModule());
            cb.RegisterModule(new QuartzAutofacJobsModule(typeof(CommandHandlerJob).Assembly));

            RegisterComponents(cb);
            return cb;
        }

        internal static void RegisterComponents(ContainerBuilder cb)
        {
            cb.RegisterType<JobsManager>().AsSelf();
            RunBootstrapperTasks(cb);
        }

        static void RunBootstrapperTasks(ContainerBuilder builder)
        {
        }
    }

    public class JobsManager
    {
        public void Start()
        {
        }

        public void Stop()
        {
        }
    }

    public class PollerBuilder
    {
        const string Default = "Default";

        public PollerBuilder()
        {
            ServiceDescription = Default;
            ServiceDisplayName = Default;
            ServiceName = Default;
            OveridedServiceConfiguration = x => { };
            ContainerConfiguration = builder => { };
        }

        protected string ServiceDescription { get; set; }

        protected string ServiceDisplayName { get; set; }

        protected string ServiceName { get; set; }

        protected Action<HostConfigurator> OveridedServiceConfiguration { get; set; }

        protected TaskAdapter ConcreteTask { get; set; }

        protected Action<ContainerBuilder> ContainerConfiguration { get; set; }

        public PollerBuilder SetDescription(string serviceDescription)
        {
            ServiceDescription = serviceDescription;
            return this;
        }

        public PollerBuilder SetDisplayName(string serviceDisplayName)
        {
            ServiceDisplayName = serviceDisplayName;
            return this;
        }

        public PollerBuilder SetServiceName(string serviceName)
        {
            ServiceName = serviceName;
            return this;
        }

        public PollerBuilder OverideServiceConfiguration(Action<HostConfigurator> overideConfiguration)
        {
            OveridedServiceConfiguration = overideConfiguration;
            return this;
        }

        public PollerBuilder WitTask<TTask>(string taskName, string taskDescription, int intervalInSeconds)
            where TTask : class, ITask
        {
            ConcreteTask = new TaskAdapter(typeof(TTask), taskName, taskDescription, intervalInSeconds);
            return this;
        }

        public PollerBuilder ConfigureContainer(Action<ContainerBuilder> containerConfiguration)
        {
            ContainerConfiguration = containerConfiguration;
            return this;
        }

        public IPoller Build()
        {
            return new Poller();
        }
    }

    public interface IPoller
    {
        void Start();
    }

    public class Poller : IPoller
    {
        public void Start()
        {
        }
    }
}