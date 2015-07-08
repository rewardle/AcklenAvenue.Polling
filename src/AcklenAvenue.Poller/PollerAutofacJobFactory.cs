﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Autofac;
using Quartz;
using Quartz.Spi;

namespace AcklenAvenue.Poller
{
    public class PollerAutofacJobFactory : IJobFactory, IDisposable
    {
        readonly ILifetimeScope _lifetimeScope;

        readonly ConcurrentDictionary<object, JobTrackingInfo> _runningJobs =
            new ConcurrentDictionary<object, JobTrackingInfo>();

        readonly string _scopeName;
        readonly Action<object, string> _logDebug;
        readonly Action<object, string> _logInfo;
        readonly Action<object, string> _logWarning;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PollerAutofacJobFactory" /> class.
        /// </summary>
        /// <param name="lifetimeScope">The lifetime scope.</param>
        /// <param name="scopeName">Name of the scope.</param>
        /// <param name="logDebug">Log handler for debug information.</param>
        /// <param name="logInfo">Log handler for info logs.</param>
        /// <param name="logWarning">Log handler for warning information.</param>
        public PollerAutofacJobFactory(ILifetimeScope lifetimeScope, string scopeName, Action<object, string> logDebug, Action<object, string> logInfo, Action<object, string> logWarning)
        {
            if (lifetimeScope == null) throw new ArgumentNullException("lifetimeScope");
            if (scopeName == null) throw new ArgumentNullException("scopeName");
            _lifetimeScope = lifetimeScope;
            _scopeName = scopeName;
            _logDebug = logDebug;
            _logInfo = logInfo;
            _logWarning = logWarning;
        }

        internal ConcurrentDictionary<object, JobTrackingInfo> RunningJobs
        {
            get { return _runningJobs; }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            KeyValuePair<object, JobTrackingInfo>[] runningJobs = RunningJobs.ToArray();
            RunningJobs.Clear();

            if (runningJobs.Length > 0)
            {
                _logInfo(this, string.Format("Cleaned {0} scopes for running jobs", runningJobs.Length));
            }
        }

        /// <summary>
        ///     Called by the scheduler at the time of the trigger firing, in order to
        ///     produce a <see cref="T:Quartz.IJob" /> instance on which to call Execute.
        /// </summary>
        /// <remarks>
        ///     It should be extremely rare for this method to throw an exception -
        ///     basically only the the case where there is no way at all to instantiate
        ///     and prepare the Job for execution.  When the exception is thrown, the
        ///     Scheduler will move all triggers associated with the Job into the
        ///     <see cref="F:Quartz.TriggerState.Error" /> state, which will require human
        ///     intervention (e.g. an application restart after fixing whatever
        ///     configuration problem led to the issue wih instantiating the Job.
        /// </remarks>
        /// <param name="bundle">
        ///     The TriggerFiredBundle from which the <see cref="T:Quartz.IJobDetail" />
        ///     and other info relating to the trigger firing can be obtained.
        /// </param>
        /// <param name="scheduler">a handle to the scheduler that is about to execute the job</param>
        /// <throws>SchedulerException if there is a problem instantiating the Job. </throws>
        /// <returns>
        ///     the newly instantiated Job
        /// </returns>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            if (bundle == null) throw new ArgumentNullException("bundle");
            if (scheduler == null) throw new ArgumentNullException("scheduler");

            Type jobType = bundle.JobDetail.JobType;

            string jobName = bundle.JobDetail.Key.Name;
            ILifetimeScope nestedScope = _lifetimeScope.BeginLifetimeScope(_scopeName);

            IJob newJob = null;
            try
            {
                newJob = (IJob) nestedScope.ResolveNamed(jobName, jobType);
                var jobTrackingInfo = new JobTrackingInfo(nestedScope);
                RunningJobs[newJob] = jobTrackingInfo;

                _logDebug(newJob,
                    string.Format(CultureInfo.InvariantCulture, "Scope 0x{0:x} associated with Job 0x{1:x}",
                        jobTrackingInfo.Scope.GetHashCode(), newJob.GetHashCode()));
                
                nestedScope = null;
            }
            catch (Exception ex)
            {
                if (nestedScope != null)
                {
                    DisposeScope(newJob, nestedScope);
                }
                throw new SchedulerConfigException(string.Format(CultureInfo.InvariantCulture,
                    "Failed to instantiate Job '{0}' of type '{1}'",
                    bundle.JobDetail.Key, bundle.JobDetail.JobType), ex);
            }
            return newJob;
        }

        /// <summary>
        ///     Allows the the job factory to destroy/cleanup the job if needed.
        /// </summary>
        public void ReturnJob(IJob job)
        {
            JobTrackingInfo trackingInfo;
            if (!RunningJobs.TryRemove(job, out trackingInfo))
            {
                _logWarning(job, string.Format("Tracking info for job 0x{0:x} not found", job.GetHashCode()));
            }

            DisposeScope(job, trackingInfo.Scope);
        }

        void DisposeScope(IJob job, ILifetimeScope lifetimeScope)
        {
            _logDebug(job, string.Format("Disposing Scope 0x{0:x} for Job 0x{1:x}",
                lifetimeScope != null ? lifetimeScope.GetHashCode() : 0,
                job != null ? job.GetHashCode() : 0));
   
            if (lifetimeScope != null)
                lifetimeScope.Dispose();
        }

        #region Job data

        internal sealed class JobTrackingInfo
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
            /// </summary>
            public JobTrackingInfo(ILifetimeScope scope)
            {
                Scope = scope;
            }

            public ILifetimeScope Scope { get; private set; }
        }

        #endregion Job data
    }
}