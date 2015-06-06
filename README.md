# AcklenAvenue.Polling

## Installation:

```
install-package AcklenAvenue.Polling
```

## Example usage:
```
public class SampleQueueWorker
    {
        Poller _poller;

        public void Start()
        {
            var queue = new SomeExampleQueue();
            var dispatcher = new SomeExampleDispatcher();

            _poller = new Poller("command queue",
                () =>
                {
                    //get an item from the queue
                    var queueItem = queue.Pull();

                    //dispatch the item
                    dispatcher.Dispatch(queueItem);

                },
                5000, // poll every 5 seconds
                true // is background thread
                );

            _poller.Start();
        }

        public void Stop()
        {
            _poller.Stop();
        }        
    }
```
