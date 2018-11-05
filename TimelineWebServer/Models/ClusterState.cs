namespace TimelineWebServer.Models
{
    using System.Collections.Generic;

    public class ClusterState
    {
        public string Leader { get; set; }
        public IReadOnlyList<string> Followers { get; set; }
        public IReadOnlyList<string> Unreachable { get; set; }
        public IReadOnlyList<string> Unplugged { get; set; }
    }
}
