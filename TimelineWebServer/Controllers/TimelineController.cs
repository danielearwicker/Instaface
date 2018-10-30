namespace TimelineWebServer.Controllers
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Instaface;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;

    [Route("api/[controller]")]
    [ApiController]
    public class TimelineController : ControllerBase
    {                
        private readonly IClusterConfig _cluster;

        public TimelineController(IClusterConfig cluster)
        {
            _cluster = cluster;
        }
        
        [HttpGet("{userId}")]
        public async Task<JObject> GetTimeline(int userId)
        {
            var query = _cluster.Query();

            var started = new Stopwatch();
            started.Start();

            var mapped = await query.Query(new QueryRequest
            {
                Entities = new[] { userId },

                Template = new JObject
                {
                    ["posted"] = new JObject
                    {
                        ["likedby"] = null
                    },
                    ["liked"] = new JObject
                    {
                        ["postedby"] = null
                    }
                }
            });
            
            mapped["overallTime"] = started.Elapsed.TotalMilliseconds;
            return mapped;
        }        
    }
}
