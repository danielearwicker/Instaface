namespace Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Instaface;
    using Instaface.Monitoring;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using Microsoft.Extensions.Configuration;

    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static IServiceProvider GetServices(string[] args)
        {
            var splitArgs = from a in args
                            let eq = a.IndexOf('=')
                            where eq != -1
                            select KeyValuePair.Create(a.Substring(0, eq), a.Substring(eq + 1));

            var config = new ConfigurationBuilder()
                         .AddEnvironmentVariables()
                         .AddInMemoryCollection(splitArgs)
                         .Build();

            var services = new ServiceCollection()
                           .AddSingleton<IConfiguration>(config)
                           .AddMonitoring(config)
                           .AddLogging()
                           .AddSingleton(config)
                           .AddMemoryCache()
                           .AddSingleton<IRedis, Redis>()
                           .AddSingleton<IClusterConfig, ClusterConfig>();

            return services.BuildServiceProvider();
        }

        private static async Task MainAsync(string[] args)
        {
            var services = GetServices(args);
            var cluster = services.GetRequiredService<IClusterConfig>();

            Console.WriteLine("Waiting for configuration to refresh...");
            await cluster.WaitForFollowers();
            
            var random = new Random();

            for (; ; )
            {
                var read = cluster.Read();
                var write = cluster.Write();

                Console.WriteLine("Selecting random friends");
                var friends = await read.GetRandomEntities("user", random.Next(20) + 5);

                Console.WriteLine("Creating user");
                var self = await write.CreateEntity("user", new JObject
                {
                    ["firstName"] = Pick(random, FakeData.FirstNames),
                    ["lastName"] = Pick(random, FakeData.LastNames)
                });

                Console.WriteLine($"Linking to {friends.Count} friends");
                foreach (var friend in friends)
                {
                    await write.CreateAssociation(self, friend, "friend", "friend");
                }

                var activity = random.Next(30) + 5;
                Console.WriteLine("Get status updates");
                var statuses = await read.GetRandomEntities("status", activity);
                var s = 0;

                Console.WriteLine($"Performing {activity} actions");
                for (var a = 0; a < activity; a++)
                {
                    if (random.Next(2) == 0)
                    {                                                
                        if (s < statuses.Count)
                        {
                            Console.WriteLine("Liking status update");
                            await write.CreateAssociation(self, statuses.Skip(s++).First(), "liked", "likedby");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Posting a status update");
                        var text = Pick(random, FakeData.DeepThoughts);
                        var status = await write.CreateEntity("status", new JObject {["text"] = text});
                        await write.CreateAssociation(self, status, "posted", "postedby");
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
