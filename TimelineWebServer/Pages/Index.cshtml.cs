using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TimelineWebServer.Pages
{
    using System;
    using System.Diagnostics;
    using Instaface;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Refit;

    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }        
    }

    public class TimelineItem
    {
        public DateTime Linked { get; set; }

        public IReadOnlyCollection<Person> PostedBy { get; set; }
        public IReadOnlyCollection<Person> LikedBy { get; set; }

        public string Text { get; set; }
    }

    public class TimelineContainer
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public IReadOnlyCollection<TimelineItem> Posted { get; set; }
        public IReadOnlyCollection<TimelineItem> Liked { get; set; }

        public IEnumerable<TimelineItem> Items => (Posted ?? Empty).Concat(Liked ?? Empty).OrderBy(i => i.Linked);

        private static readonly IEnumerable<TimelineItem> Empty = Enumerable.Empty<TimelineItem>();
    }

    public class TimelineModel : PageModel
    {
        public int Id { get; set; }

        public TimelineContainer Data { get; private set; }
        
        public double Duration { get; private set; }

        public async Task OnGet()
        {
            Id = int.Parse(Request.Query["id"].FirstOrDefault() ?? "1");
            
            var query = GraphClient.Query("http://192.168.1.10:6542");

            var started = new Stopwatch();
            started.Start();
            
            var mapped = await query.Query(new QueryRequest
            {
                Entities = new[] { Id },

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

            Duration = started.ElapsedMilliseconds;

            Data = JsonConvert.DeserializeObject<TimelineContainer[]>(mapped.ToString()).FirstOrDefault();            
        }
    }
}