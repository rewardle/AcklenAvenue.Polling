using Quartz;

namespace AcklenAvenue.Poller
{
    public class JobsManager
    {
        readonly IScheduler _scheduler;

        public JobsManager(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void Start()
        {
            _scheduler.Start();
        }

        public void Stop()
        {
            _scheduler.Shutdown(true);
        }
    }
}