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
    public class Poller : IPoller
    {
        readonly TaskAdapter _concreteTask;

        readonly Action<ContainerBuilder> _containerConfiguration;

        readonly ILog _log = LogManager.GetLogger(typeof(Job));

        readonly Action<HostConfigurator> _overidedServiceConfiguration;

        readonly string _serviceDescription;

        readonly string _serviceDisplayName;

        readonly string _serviceName;

        IContainer _container;

        public Poller(
            TaskAdapter concreteTask,
            Action<ContainerBuilder> containerConfiguration,
            Action<HostConfigurator> overidedServiceConfiguration,
            string serviceDescription,
            string serviceDisplayName,
            string serviceName)
        {
            _concreteTask = concreteTask;
            _containerConfiguration = containerConfiguration;
            _overidedServiceConfiguration = overidedServiceConfiguration;
            _serviceDescription = serviceDescription;
            _serviceDisplayName = serviceDisplayName;
            _serviceName = serviceName;
        }

        public void Start()
        {
            _container = ConfiguraContainer().Build();
            HostFactory.Run(
                x =>
                    {
                        x.RunAsLocalService();

                        _overidedServiceConfiguration(x);

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

                        x.SetDescription(_serviceDescription);
                        x.SetDisplayName(_serviceDisplayName);
                        x.SetServiceName(_serviceName);
                        x.UseLog4Net();
                    });
        }

        public void ConfigureBackgroundJobs(ServiceConfigurator<JobsManager> svc)
        {
            svc.UsingQuartzJobFactory(() => _container.Resolve<IJobFactory>());
            svc.ScheduleQuartzJob(
                q =>
                    {
                        q.WithJob(
                            JobBuilder.Create<Job>()
                                      .WithIdentity(_concreteTask.TaskName)
                                      .WithDescription(_concreteTask.TaskDescription)
                                      .Build);
                        q.AddTrigger(
                            () =>
                            TriggerBuilder.Create()
                                          .WithSchedule(
                                              SimpleScheduleBuilder.RepeatSecondlyForever(
                                                  _concreteTask.IntervalInSeconds))
                                          .Build());
                    });
        }

        ContainerBuilder ConfiguraContainer()
        {
            var cb = new ContainerBuilder();
            cb.RegisterModule(new QuartzAutofacFactoryModule());

            cb.Register(context => new Job(context.ResolveNamed<ITask>(_concreteTask.TaskName)))
             .AsSelf()
              .InstancePerLifetimeScope();

            cb.RegisterType<JobsManager>().AsSelf();

            cb.RegisterType(_concreteTask.Type).Named<ITask>(_concreteTask.TaskName);
            _containerConfiguration(cb);

            return cb;
        }
    }
}