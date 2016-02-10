using System;
using System.Collections.Generic;

using Autofac;
using Autofac.Extras.Quartz;

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

        readonly Func<object, Exception, Exception> _onException;

        readonly Action<object, string> _onLogDebug;

        readonly Action<object, string> _onLogInfo;

        readonly Action<object, string> _onLogWarning;

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
            string serviceName,
            Func<object, Exception, Exception> onException,
            Action<object, string> onLogInfo,
            Action<object, string> onLogDebug,
            Action<object, string> onLogWarning)
        {
            _concreteTask = concreteTask;
            _containerConfiguration = containerConfiguration;
            _overidedServiceConfiguration = overidedServiceConfiguration;
            _serviceDescription = serviceDescription;
            _serviceDisplayName = serviceDisplayName;
            _serviceName = serviceName;
            _onException = onException;
            _onLogInfo = onLogInfo;
            _onLogDebug = onLogDebug;
            _onLogWarning = onLogWarning;
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
                if (adapter.IntervalInSeconds > 0)
                {
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
                else
                {
                    svc.ScheduleQuartzJob(
                    q =>
                    {
                        q.WithJob(
                            JobBuilder.Create<Job>().WithIdentity(name).WithDescription(taskDescription).Build);
                        q.AddTrigger(
                            () =>
                            TriggerBuilder.Create()
                                          .WithSimpleSchedule(s =>
                                                s.RepeatForever())
                                          .Build());
                    });
                }
                
            }
        }

        ContainerBuilder ConfigureContainer()
        {
            var cb = new ContainerBuilder();
            cb.RegisterModule(new QuartzAutofacFactoryModule());
            cb.Register(
                c =>
                new PollerAutofacJobFactory(
                    c.Resolve<ILifetimeScope>(), "Poller.Job", _onLogDebug, _onLogInfo, _onLogWarning))
              .AsSelf()
              .As<IJobFactory>()
              .SingleInstance();

            _containerConfiguration(cb);
            cb.RegisterType<JobsManager>().AsSelf();
            foreach (var taskAdapter in _concreteTask)
            {
                TaskAdapter task = taskAdapter.Value;

                cb.RegisterType(task.Type).Named<ITask>(task.TaskName);

                cb.Register(
                    context =>
                        {
                            try
                            {
                                var resolveNamed = context.ResolveNamed<ITask>(task.TaskName);
                                return new Job(resolveNamed, _onException);
                            }
                            catch (Exception ex)
                            {
                                _onException(
                                    this,
                                    new Exception(
                                        string.Format(
                                            "There was an error in the inicialization of the job: {0}; error message:{1}",task.TaskName, ex.Message),
                                        ex));
                                throw;
                            }
                        }).Named<Job>(task.TaskName).AsSelf().InstancePerLifetimeScope();
            }

            return cb;
        }
    }
}