namespace Instaface.Consensus
{
    public class ConsensusStateSnapshot
    {
        public ConsensusMode Mode { get; }
        public string Leader { get; }
        public int Term { get; }

        public ConsensusStateSnapshot(ConsensusMode mode, string leader, int term)
        {
            Mode = mode;
            Leader = leader;
            Term = term;
        }
    }    
}
