﻿namespace Instaface.Consensus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IConsensusNode
    {        
        Task Execute(CancellationToken cancellation);

        Task<bool> HandleHeartbeat(int term, string from);        
    }

    public interface IConsensusInternals
    {
        IConsensusConfig Config { get; }

        int BeginCandidacy();
        void EndCandidacy();
        void BecomeLeader();
        bool RespondToHeartbeat();

        Task HeartbeatReceived { get; }

        Task<ICollection<Task<bool?>>> SendHeartbeat(CancellationToken cancellation);

        Random Random { get; }
    }

    public interface IConsensusConfig
    {
        string Self { get; }

        int ElectionPeriodMin { get; }
        int ElectionPeriodMax { get; }
        int HeartbeatPeriod { get; }

        Action<string> Log { get; }

        void PublishLeader();
        void PublishFollower(string leader);
    }
    
    public class ConsensusNode : IConsensusNode, IConsensusInternals
    {                
        private readonly object _locker = new object();

        private readonly AsyncQueue<HeartbeatMessage> _heartbeats = new AsyncQueue<HeartbeatMessage>();
        private ConsensusStateSnapshot _state = new ConsensusStateSnapshot(ConsensusModes.Follower, null, 0);
        
        public ConsensusNode(IConsensusTransport transport, IConsensusConfig config)
        {
            Transport = transport;
            Config = config;
        }

        public IConsensusTransport Transport { get; }
        public Task HeartbeatReceived => _heartbeats.WhenReadable;
        public IConsensusConfig Config { get; }
        
        public ConsensusStateSnapshot State
        {
            get
            {
                lock (_locker)
                {
                    return _state;
                }
            }
        }

        public int BeginCandidacy()
        {
            lock (_locker)
            {
                if (_state.Mode == ConsensusModes.Follower)
                {
                    _state = new ConsensusStateSnapshot(ConsensusModes.Candidate, Config.Self, _state.Term + 1);
                    Config.Log($"standing for election in term {_state.Term}");
                    return _state.Term;
                }

                return -1;
            }
        }

        public void EndCandidacy()
        {
            lock (_locker)
            {
                if (_state.Mode == ConsensusModes.Candidate)
                {
                    Config.Log($"abandoning candidacy {_state.Term}");
                    _state = new ConsensusStateSnapshot(ConsensusModes.Follower, null, _state.Term);
                }
            }
        }

        public void BecomeLeader()
        {
            lock (_locker)
            {
                if (_state.Mode == ConsensusModes.Candidate)
                {
                    Config.Log($"becoming leader in term {_state.Term}");
                    _state = new ConsensusStateSnapshot(ConsensusModes.Leader, Config.Self, _state.Term);

                    Config.PublishLeader();
                }
            }
        }

        public Task<ICollection<Task<bool?>>> SendHeartbeat(CancellationToken cancellation)
        {
            int term;

            lock (_locker)
            {
                if (_state.Mode == ConsensusModes.Follower)
                {
                    return null;
                }

                term = _state.Term;
            }

            return Transport.SendHeartbeat(Config.Self, term, cancellation);
        }

        public Random Random { get; } = new Random();

        public Task<bool> HandleHeartbeat(int term, string sender)
        {
            var message = new HeartbeatMessage(sender, term);
            _heartbeats.Write(message);
            return message.Result;            
        }

        public bool RespondToHeartbeat()
        {
            if (!_heartbeats.TryRead(out var message)) return false;
            var result = InterpretHeartbeat(message);
            message.SetResult(result);
            return result;
        }

        public bool InterpretHeartbeat(HeartbeatMessage message)
        {
            lock (_locker)
            {
                if (message.Term < _state.Term)
                {
                    Config.Log($"in {_state.Term}, so rejects {message}");
                    return false;
                }

                if (_state.Term == message.Term)
                {
                    if (_state.Leader == message.From)
                    {
                        return true;
                    }

                    if (_state.Leader != null)
                    {
                        Config.Log($"already voted for {_state.Leader}:{message.Term} so rejects {message}");
                        return false;
                    }
                }

                Config.Log($"votes for {message}");
                _state = new ConsensusStateSnapshot(ConsensusModes.Follower, message.From, message.Term);
                Config.PublishFollower(message.From);
                return true;
            }
        }

        public async Task Execute(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                try
                {
                    await State.Mode(this);
                }
                catch (Exception x)
                {
                    Config.Log($"{x.Message}");
                }
            }
        }        
    }
}
