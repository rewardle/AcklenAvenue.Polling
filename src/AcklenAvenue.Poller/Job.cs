using System;



using Quartz;

using log4net;

namespace AcklenAvenue.Poller
{
    [DisallowConcurrentExecution]
    public class Job : IJob
    {
        readonly Func<object, Exception, Exception> _exceptionHandler;
        readonly ITask _task;

        public Job(ITask task, Func<object, Exception, Exception> exceptionHandler)
        {
            _task = task;
            _exceptionHandler = exceptionHandler;
        }

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                _task.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION IN {2}: {0} - {1}", ex.Message, ex.GetType().Name, context.JobDetail.Key);
                var handledException = _exceptionHandler(_task, ex);
                throw new JobExecutionException("Something awful happened", handledException, false);
            }
        }
    }
}