namespace TimelineWebServer.Pages
{
    using System;
    using System.Collections.Generic;

    public class TimelineItem
    {
        public DateTime Linked { get; set; }

        public IReadOnlyCollection<Person> PostedBy { get; set; }
        public IReadOnlyCollection<Person> LikedBy { get; set; }

        public string Text { get; set; }
    }
}