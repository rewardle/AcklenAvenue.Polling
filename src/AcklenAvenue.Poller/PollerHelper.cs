namespace AcklenAvenue.Poller
{
    public static class PollerHelper
    {
        public static PollerBuilder WithMultipleThreadsPerTask<TTask>(this PollerBuilder pollerBuilder, string taskName,
            string taskDescription,
            int intervalInSeconds, int numberOfThreads)
            where TTask : class, ITask
        {
            for (var i = 0; i < numberOfThreads; i++)
            {
                pollerBuilder.WithTask<TTask>(string.Format("{0}-{1}", taskName, i), taskDescription, intervalInSeconds);
            }

            return pollerBuilder;
        }
    }
}