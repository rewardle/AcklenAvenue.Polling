using System;

using Autofac;

using Topshelf.HostConfigurators;

namespace AcklenAvenue.Poller
{
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

        public PollerBuilder WithTask<TTask>(string taskName, string taskDescription, int intervalInSeconds)
            where TTask : class, ITask
        {
            ConcreteTask = new TaskAdapter(typeof(TTask), taskName, taskDescription, intervalInSeconds);
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
                ConcreteTask,
                ContainerConfiguration,
                OveridedServiceConfiguration,
                ServiceDescription,
                ServiceDisplayName,
                ServiceName);
        }
    }
}