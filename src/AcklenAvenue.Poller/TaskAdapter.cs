using System;

namespace AcklenAvenue.Poller
{
    public class TaskAdapter
    {
        public Type Type { get; set; }

        public string TaskName { get; set; }

        public string TaskDescription { get; set; }

        public int IntervalInSeconds { get; set; }

        public TaskAdapter(Type type, string taskName, string taskDescription, int intervalInSeconds)
        {
            Type = type;
            TaskName = taskName;
            TaskDescription = taskDescription;
            IntervalInSeconds = intervalInSeconds;
        }
    }
}