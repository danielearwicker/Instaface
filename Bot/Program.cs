using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Instaface;
using Newtonsoft.Json.Linq;

namespace Bot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            var data = GraphClient.Data(args[0]);
            
            var random = new Random();

            for (; ; )
            {
                Console.WriteLine("Selecting random friends");
                var friends = await data.GetRandomEntities("user", random.Next(20) + 5);

                Console.WriteLine("Creating user");
                var self = await data.CreateEntity("user", new JObject
                {
                    ["firstName"] = Pick(random, FakeData.FirstNames),
                    ["lastName"] = Pick(random, FakeData.LastNames)
                });

                Console.WriteLine($"Linking to {friends.Count} friends");
                foreach (var friend in friends)
                {
                    await data.CreateAssociation(self, friend, "friend", "friend");
                }

                var activity = random.Next(30) + 5;
                Console.WriteLine("Get status updates");
                var statuses = await data.GetRandomEntities("status", activity);
                var s = 0;

                Console.WriteLine($"Performing {activity} actions");
                for (var a = 0; a < activity; a++)
                {
                    if (random.Next(2) == 0)
                    {                                                
                        if (s < statuses.Count)
                        {
                            Console.WriteLine("Liking status update");
                            await data.CreateAssociation(self, statuses.Skip(s++).First(), "liked", "likedby");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Posting a status update");
                        var text = Pick(random, FakeData.DeepThoughts);
                        var status = await data.CreateEntity("status", new JObject {["text"] = text});
                        await data.CreateAssociation(self, status, "posted", "postedby");
                    }                    
                }
            }
        }

        private static string Pick(Random random, IReadOnlyList<string> options)
        {
            return options[random.Next(0, options.Count)];
        }
    }
}
