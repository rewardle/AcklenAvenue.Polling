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

        public PollerBuilder()
        {
            ConcreteTasks = new Dictionary<string, TaskAdapter>();
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

        protected Dictionary<string, TaskAdapter> ConcreteTasks { get; set; }

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

        public PollerBuilder WithTask<TTask>(string taskName, string taskDescription, int intervalInSeconds)
            where TTask : class, ITask
        {
            if (ConcreteTasks.Keys.All(s => s != taskName))
            {
                ConcreteTasks.Add(
                    taskName, new TaskAdapter(typeof(TTask), taskName, taskDescription, intervalInSeconds));
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
                ServiceName);
        }
    }
}