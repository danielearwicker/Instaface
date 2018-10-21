using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GraphServer
{
    using Instaface;
    using Instaface.Caching;
    using Instaface.Db;
    
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter(true));
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services.AddSingleton(Configuration);

            services.AddSingleton<IDataConnection, DataConnection>();
            services.AddSingleton<IGraphData, GraphDataDb>();
            services.AddSingleton<IGraphCache, GraphCache>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            app.ApplicationServices.GetRequiredService<IDataConnection>().Setup().Wait();
            ((GraphDataDb)app.ApplicationServices.GetRequiredService<IGraphData>()).Init().Wait();
        }
    }
}
