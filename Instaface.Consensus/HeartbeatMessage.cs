namespace Instaface.Consensus
{
    using System.Threading.Tasks;

    public class HeartbeatMessage
    {
        private readonly TaskCompletionSource<bool> _result = new TaskCompletionSource<bool>();

        public HeartbeatMessage(string from, int term)
        {
            From = from;
            Term = term;
        }

        public string From { get; }
        public int Term { get; }

        public Task<bool> Result => _result.Task;

        public void SetResult(bool result) => _result.SetResult(result);

        public override string ToString()
        {
            return $"{From}:{Term}";
        }
    }
}