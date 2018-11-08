namespace Instaface
{
    using System;
    using Newtonsoft.Json.Linq;

    public class GraphNode
    {
        public int Id { get;set; }
        public DateTime Created { get;set; }
        public JObject Attributes { get;set; }
    }
}
