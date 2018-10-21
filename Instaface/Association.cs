namespace Instaface
{
    public class Association : GraphNode
    {
        public string Type { get;set; }
        public int From { get; set; }
        public int To { get; set; }
    }
}
