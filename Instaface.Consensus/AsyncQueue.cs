namespace Instaface.Consensus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class AsyncQueue<T>
    {
        private readonly Queue<T> _messages = new Queue<T>();
        private TaskCompletionSource<bool> _waiter;
        private readonly object _locker = new object();

        public Task WhenReadable
        {
            get
            {
                lock (_locker)
                {
                    if (_messages.Count != 0) return Task.CompletedTask;
                    if (_waiter == null) _waiter = new TaskCompletionSource<bool>();
                    return _waiter.Task;
                }
            }
        }

        public bool TryRead(out T message)
        {
            lock (_locker)
            {
                if (_messages.Count == 0)
                {
                    message = default(T);
                    return false;
                }

                message = _messages.Dequeue();
                return true;
            }
        }

        public void Write(T message)
        {
            TaskCompletionSource<bool> waiter;

            lock (_locker)
            {
                _messages.Enqueue(message);

                waiter = _waiter;
                _waiter = null;
            }

            waiter?.SetResult(true);
        }
    }
}