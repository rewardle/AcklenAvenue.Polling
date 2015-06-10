using System;

using Common.Logging;

using Quartz;

namespace AcklenAvenue.Poller
{
    public class Job : IJob
    {
        readonly ILog _log = LogManager.GetLogger(typeof(Job));

        readonly ITask _task;

        public Job(ITask task)
        {
            _task = task;
        }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                _task.Execute();
            }
            catch (Exception ex)
            {
                throw new JobExecutionException("Something awful happened", ex, false);
                ;
            }
        }
    }
}