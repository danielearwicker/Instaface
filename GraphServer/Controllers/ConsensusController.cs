namespace GraphServer.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Instaface.Consensus;

    [Route("api/[controller]")]
    [ApiController]
    public class ConsensusController : ControllerBase, IConsensusApi
    {
        private readonly IConsensusNode _consensusNode;

        public ConsensusController(IConsensusNode consensusNode)
        {
            _consensusNode = consensusNode;
        }
        
        [HttpPost("heartbeat/{term}/from/{from}")]
        public async Task<HeartbeatResponse> PostHeartbeat(int term, string from, CancellationToken cancellation)
        {
            return new HeartbeatResponse
            {
                Confirmed = await _consensusNode.HandleHeartbeat(term, Encoding.UTF8.GetString(Convert.FromBase64String(from)))
            };
        }
    }
}
