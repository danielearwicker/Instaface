using System;

namespace InstafaceCmd
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Newtonsoft.Json.Linq;

    public class Program
    {
        private const string GraphServer = "graphserver";
        private const string TimelineServer = "timelineserver";

        public static void Main(string[] args)
        {
            StartContainer(args[0], GraphServer, "001");
            StartContainer(args[0], GraphServer, "002");
            StartContainer(args[0], GraphServer, "003");
            StartContainer(args[0], GraphServer, "004");
            StartContainer(args[0], GraphServer, "005");
            StartContainer(args[0], TimelineServer, "001");
        }

        private static void StartContainer(string configPath, string type, string instance)
        {
            var config = JObject.Parse(File.ReadAllText(configPath));

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

            if (type == GraphServer)
            {
                env["ConnectionStrings__DefaultConnection"] = mysql;
                env["Consensus__Self"] = $"http://{containerGroupName}.{azureRegion.Name}.azurecontainer.io";
            }

            var existing = azure.ContainerGroups.ListByResourceGroup(resourceGroupName)
                                .FirstOrDefault(g => g.Name == containerGroupName);
            if (existing != null)
            {
                azure.ContainerGroups.DeleteById(existing.Id);
            }

            // Create the container group
            azure.ContainerGroups.Define(containerGroupName)
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
        }
    }
}
