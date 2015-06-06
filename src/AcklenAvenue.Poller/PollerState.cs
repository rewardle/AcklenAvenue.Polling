namespace AcklenAvenue.Poller
{
    public enum PollerState
    {
        Unstarted = 0,
        Running = 1,
        StopRequested = 2,
        Stopped = 3,
        PauseRequested = 4,
        Paused = 5,
    }
}