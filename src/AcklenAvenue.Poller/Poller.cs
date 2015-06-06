﻿using System;
using System.Threading;
using log4net;

namespace AcklenAvenue.Poller
{
    public class Poller
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Poller));

        private readonly Action _action;
        private readonly int _pollingInterval;
        private readonly Thread _processingThread;
        private readonly AutoResetEvent _stopEvent;
        private readonly ManualResetEventSlim _pauseEvent;
        private readonly object _syncLock = new object();
        private PollerState _pollerState;

        public Poller(string pollerName, Action action, int pollingInterval, bool isBackground)
        {
            _action = action;
            _pollingInterval = pollingInterval;

            _stopEvent = new AutoResetEvent(false);
            _pauseEvent = new ManualResetEventSlim(false);
            _processingThread = new Thread(DoWork) { IsBackground = isBackground, Name = pollerName };

            _pollerState = PollerState.Unstarted;
        }

        public void Start()
        {
            _pollerState = PollerState.Running;
            _processingThread.Start();
        }

        public void Start(int dueTime)
        {
            new Timer(o => Start(), null, dueTime, Timeout.Infinite);
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                if (_pollerState != PollerState.Running && _pollerState != PollerState.PauseRequested)
                    Log.WarnFormat("Requested STOP on {0} poller state.", _pollerState);

                _pollerState = PollerState.StopRequested;
                _stopEvent.Set();
                _pauseEvent.Set();
            }
        }

        public void Pause()
        {
            lock (_syncLock)
            {
                if (_pollerState != PollerState.Running)
                    Log.WarnFormat("Requested PAUSE on {0} poller state.", _pollerState);

                _pauseEvent.Reset();
                _pollerState = PollerState.PauseRequested;
            }
        }

        public void Continue()
        {
            lock (_syncLock)
            {
                if (_pollerState == PollerState.PauseRequested)
                    _pollerState = PollerState.Running; // applicable if job is long running or no new poll was needed since pause requested
                else if (_pollerState != PollerState.Paused)
                    Log.WarnFormat("Requested CONTINUE on {0} poller state.", _pollerState);
                _pauseEvent.Set();
            }
        }

        private void DoWork()
        {
            while (_pollerState == PollerState.Running)
            {
                try
                {
                    Console.WriteLine("polling");
                    _action();
                }
                catch (Exception ex)
                {
                    Log.Error(Thread.CurrentThread.Name + "failed.", ex);
                }
                finally
                {
                    if (_stopEvent.WaitOne(_pollingInterval))
                    {
                        if (_pollerState == PollerState.StopRequested)
                            _pollerState = PollerState.Stopped;
                    }

                    if (_pollerState == PollerState.PauseRequested)
                    {
                        _pollerState = PollerState.Paused;
                        _pauseEvent.Wait();
                        // Continue only if we are still in Pause mode and not StopRequested
                        if (_pollerState == PollerState.Paused)
                            _pollerState = PollerState.Running;
                    }
                }
            }
            Log.Debug("Exiting: " + Thread.CurrentThread.Name);
        }
    }
}