using System;
using Quartz;

namespace AcklenAvenue.Poller
{
    public class Job : IJob
    {
        readonly Action<object, Exception> _exceptionLogger;
        readonly ITask _task;

        public Job(ITask task, Action<object, Exception> exceptionLogger)
        {
            _task = task;
            _exceptionLogger = exceptionLogger;
        }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                _task.Execute();
            }
            catch (Exception ex)
            {
                var jobExecutionException = new JobExecutionException("Something awful happened", ex, false);
                _exceptionLogger(_task, jobExecutionException);
                throw jobExecutionException;
            }
        }
    }
}