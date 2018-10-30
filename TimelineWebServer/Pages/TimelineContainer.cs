namespace TimelineWebServer.Pages
{
    using System.Collections.Generic;
    using System.Linq;

    public class TimelineContainer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public IReadOnlyCollection<TimelineItem> Posted { get; set; }
        public IReadOnlyCollection<TimelineItem> Liked { get; set; }

        public IEnumerable<TimelineItem> Items => (Posted ?? Empty).Concat(Liked ?? Empty).OrderBy(i => i.Linked);


        private static readonly IEnumerable<TimelineItem> Empty = Enumerable.Empty<TimelineItem>();
    }
}