using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Topshelf.HostConfigurators;

namespace AcklenAvenue.Poller
{
    public class PollerBuilder
    {
        const string Default = "Default";
        Func<object, Exception, Exception> _onException = (o, exception) => exception;
        Action<object, string> _onLogDebug = (o, s) => { };
        Action<object, string> _onLogInfo = (o, s) => { };
        Action<object, string> _onLogWarning = (o, s) => { };

        public PollerBuilder()
        {
            ConcreteTasks = new Dictionary<string, TaskAdapter>();
            ServiceDescription = Default;
            ServiceDisplayName = Default;
            ServiceName = Default;
            OveridedServiceConfiguration = x => { };
            ContainerConfiguration = builder => { };
        }

        protected string ServiceDescription { get; private set; }
        protected string ServiceDisplayName { get; private set; }
        protected string ServiceName { get; private set; }
        protected Action<HostConfigurator> OveridedServiceConfiguration { get; private set; }
        protected Dictionary<string, TaskAdapter> ConcreteTasks { get; }
        protected Action<ContainerBuilder> ContainerConfiguration { get; private set; }

        public PollerBuilder OnException(Func<object, Exception, Exception> handler)
        {
            _onException = handler;
            return this;
        }

        public PollerBuilder OnLogInfo(Action<object, string> handler)
        {
            _onLogInfo = handler;
            return this;
        }

        public PollerBuilder OnLogWarning(Action<object, string> handler)
        {
            _onLogWarning = handler;
            return this;
        }

        public PollerBuilder OnLogDebug(Action<object, string> handler)
        {
            _onLogDebug = handler;
            return this;
        }

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

        public PollerBuilder WithTask<TTask>(string taskName, string taskDescription, int intervalInSeconds)
            where TTask : class, ITask
        {
            if (ConcreteTasks.Keys.All(s => s != taskName))
            {
                ConcreteTasks.Add(
                    taskName, new TaskAdapter(typeof (TTask), taskName, taskDescription, intervalInSeconds));
            }
            else
            {
                throw new Exception(string.Format("The task {0} has already registered", taskName));
            }
            return this;
        }

        public PollerBuilder RegisterComponents(Action<ContainerBuilder> containerConfiguration)
        {
            ContainerConfiguration = containerConfiguration;
            return this;
        }

        public IPoller Build()
        {
            return new Poller(
                ConcreteTasks,
                ContainerConfiguration,
                OveridedServiceConfiguration,
                ServiceDescription,
                ServiceDisplayName,
                ServiceName,
                _onException, _onLogDebug, _onLogInfo, _onLogWarning);
        }
    }
}