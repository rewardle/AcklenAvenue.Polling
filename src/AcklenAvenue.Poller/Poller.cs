using System;
using System.Collections.Generic;

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
        readonly Dictionary<string, TaskAdapter> _concreteTask;

        readonly Action<ContainerBuilder> _containerConfiguration;

        readonly ILog _log = LogManager.GetLogger(typeof(Job));

        readonly Action<HostConfigurator> _overidedServiceConfiguration;

        readonly string _serviceDescription;

        readonly string _serviceDisplayName;

        readonly string _serviceName;

        IContainer _container;

        public Poller(
            Dictionary<string, TaskAdapter> concreteTask,
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
            _container = ConfigureContainer().Build();
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
            svc.UsingQuartzJobFactory(() => _container.Resolve<PollerAutofacJobFactory>());

            foreach (var taskAdapter in _concreteTask)
            {
                string name = taskAdapter.Key;
                TaskAdapter adapter = taskAdapter.Value;
                string taskDescription = adapter.TaskDescription;
                svc.ScheduleQuartzJob(
                    q =>
                        {
                            q.WithJob(
                                JobBuilder.Create<Job>().WithIdentity(name).WithDescription(taskDescription).Build);
                            q.AddTrigger(
                                () =>
                                TriggerBuilder.Create()
                                              .WithSchedule(
                                                  SimpleScheduleBuilder.RepeatSecondlyForever(adapter.IntervalInSeconds))
                                              .Build());
                        });
            }
        }

        ContainerBuilder ConfigureContainer()
        {
            var cb = new ContainerBuilder();
            cb.RegisterModule(new QuartzAutofacFactoryModule());
            cb.Register(c => new PollerAutofacJobFactory(c.Resolve<ILifetimeScope>(), "Poller.Job"))
              .AsSelf()
              .As<IJobFactory>()
              .SingleInstance();

            _containerConfiguration(cb);
            cb.RegisterType<JobsManager>().AsSelf();
            foreach (var taskAdapter in _concreteTask)
            {
                TaskAdapter task = taskAdapter.Value;

                cb.RegisterType(task.Type).Named<ITask>(task.TaskName);

                cb.Register(context => new Job(context.ResolveNamed<ITask>(task.TaskName)))
                  .Named<Job>(task.TaskName)
                  .AsSelf()
                  .InstancePerLifetimeScope();
            }

            return cb;
        }
    }
}