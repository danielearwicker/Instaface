using System;

namespace InstafaceCmd
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Newtonsoft.Json.Linq;
    using StackExchange.Redis;

    public class Program
    {
        private const string GraphServer = "graphserver";
        private const string TimelineServer = "timelineserver";

        public static void Main(string[] args)
        {
            var config = JObject.Parse(File.ReadAllText(args[0]));

            if (args.Length == 3 && args[1] == "/client")
            {
                DeployFolderToStorage(config, args[2]);
                return;
            }

            if (args.Length == 1)
            {
                args = args.Concat(new[] {GraphServer, TimelineServer}).ToArray();
            }

            if (args.Contains(GraphServer))
            {
                StartContainer(config, GraphServer, "001");
                StartContainer(config, GraphServer, "002");
                StartContainer(config, GraphServer, "003");
                StartContainer(config, GraphServer, "004");
                StartContainer(config, GraphServer, "005");
            }

            if (args.Contains(TimelineServer))
            {
                StartContainer(config, TimelineServer, "001");
            }
        }

        private static void StartContainer(JObject config, string type, string instance)
        {
            var clientId = config["clientId"].Value<string>();
            var clientSecret = config["clientSecret"].Value<string>();
            var tenantId = config["tenantId"].Value<string>();
            var subscriptionId = config["subscriptionId"].Value<string>();

            var redis = Environment.GetEnvironmentVariable("Redis:ConnectionString");
            var mysql = Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");
            
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(
                clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);

            var azure = Microsoft.Azure.Management.Fluent.Azure
                                 .Configure()
                                 .Authenticate(credentials)
                                 .WithSubscription(subscriptionId);

            var resourceGroupName = "instaface";
                
            var resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            var azureRegion = resGroup.Region;

            var containerGroupName = $"{resourceGroupName}-{type}-{instance}";
            var containerImage = $"danielearwicker/instaface-{type}";

            var env = new Dictionary<string, string>
            {                
                ["Redis__ConnectionString"] = redis
            };

            var selfId = Guid.NewGuid();

            if (type == GraphServer)
            {
                env["ConnectionStrings__DefaultConnection"] = mysql;
                env["Consensus__Self"] = $"{selfId}";
            }

            var redisConnection = ConnectionMultiplexer.Connect(redis);
            var redisDb = redisConnection.GetDatabase();

            redisDb.KeyDelete($"consensus:ip:{selfId}");
            
            var existing = azure.ContainerGroups.ListByResourceGroup(resourceGroupName)
                                .FirstOrDefault(g => g.Name == containerGroupName);
            if (existing != null)
            {
                Console.WriteLine($"Deleting existing {containerGroupName}");
                azure.ContainerGroups.DeleteById(existing.Id);
            }

            Console.WriteLine($"Creating {containerGroupName}");

            // Create the container group
            var def = azure.ContainerGroups.Define(containerGroupName)
                 .WithRegion(azureRegion)
                 .WithExistingResourceGroup(resourceGroupName)
                 .WithLinux()
                 .WithPublicImageRegistryOnly()
                 .WithoutVolume()
                 .DefineContainerInstance(containerGroupName + "-1")
                 .WithImage(containerImage)
                 .WithExternalTcpPort(80)
                 .WithCpuCoreCount(1.0)
                 .WithMemorySizeInGB(1)
                 .WithEnvironmentVariables(env)
                 .Attach()
                 .WithDnsPrefix(containerGroupName)
                 .Create();
            
            redisDb.StringSet($"consensus:ip:{selfId}", $"http://{def.IPAddress}");
        }

        private static void DeployFolderToStorage(JObject config, string fromFolder)
        {
            var conStr = config["storageAccount"].Value<string>();
            var account = CloudStorageAccount.Parse(conStr);
            var client = account.CreateCloudBlobClient();

            var web = client.GetContainerReference("$web");

            var contentTypes = new FileExtensionContentTypeProvider();
            
            foreach (var file in Directory.EnumerateFiles(fromFolder, "*", new EnumerationOptions { RecurseSubdirectories = true }))
            {
                var path = file.Substring(fromFolder.Length + 1).Replace('\\', '/');
                var blob = web.GetBlockBlobReference(path);

                if (!contentTypes.TryGetContentType(file, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                blob.Properties.ContentType = contentType;
                blob.UploadFromFileAsync(file).Wait();
            }
        }
    }
}
