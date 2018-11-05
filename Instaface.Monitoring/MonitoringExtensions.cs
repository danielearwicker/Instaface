namespace Instaface.Monitoring
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Converters;

    public static class MonitoringExtensions
    {
        public static IApplicationBuilder UseMonitoring(this IApplicationBuilder app)
        {
            return app.UseSignalR(options =>
            {
                options.MapHub<MonitoringEventsHub>("/monitoring");
            });
        }

        public static IServiceCollection AddMonitoring(this IServiceCollection services, IConfiguration config)
        {
            var redisConnectionString = config.GetSection("Redis").GetValue<string>("ConnectionString");

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException("Missing configuration Redis:ConnectionString");
            }

            services.AddSignalR()
                    .AddRedis(redisConnectionString)
                    .AddJsonProtocol(options =>
                    {
                        options.PayloadSerializerSettings.Converters.Add(new StringEnumConverter(false));
                    });

            return services.AddSingleton<IMonitoringEvents, MonitoringEvents>(); 
        }
    }
}