namespace TimelineWebServer.Pages
{
    using System.Collections.Generic;

    public class ResultWrapper
    {
        public Stats Stats { get; set; }

        public IReadOnlyCollection<TimelineContainer> Entities { get; set; }
    }
}