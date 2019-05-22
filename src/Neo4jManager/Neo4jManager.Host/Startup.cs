using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Neo4jManager.ServiceInterface;
using Neo4jManager.ServiceModel;
using ServiceStack;
using ServiceStack.Api.Swagger;
using ServiceStack.Configuration;

namespace Neo4jManager.Host
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseServiceStack(new AppHost(env));

            app.Run(context =>
            {
                context.Response.Redirect("/metadata");
                return Task.FromResult(0);
            });
        }
    }
    
   public class AppHost : AppHostBase
    {
        public AppHost(IHostingEnvironment hostingEnvironment) : base("Neo4jManager", typeof(DeploymentService).Assembly)
        {
            var versions = File.ReadAllText(Path.Combine(hostingEnvironment.ContentRootPath, "versions.json"))
                .FromJson<IEnumerable<Version>>()
                .ToJsv();

            AppSettings = new MultiAppSettingsBuilder()
                .AddEnvironmentalVariables()
                .AddDictionarySettings(new Dictionary<string, string>
                {
                    { AppSettingsKeys.Versions, versions }
                })
                .Build();
        }

        public override void Configure(Container container)
        {
            container.RegisterAutoWiredAs<FileCopy, IFileCopy>();
            container.Register<INeo4jManagerConfig>(c => new Neo4jManagerConfig
            {
                Neo4jBasePath = @"c:\Neo4jManager",
                StartBoltPort = 7691,
                StartHttpPort = 7401
            }).ReusedWithin(ReuseScope.None);
            container.RegisterAutoWiredAs<ZuluJavaResolver, IJavaResolver>().ReusedWithin(ReuseScope.None);
            container.RegisterAutoWiredAs<Neo4jInstanceFactory, INeo4jInstanceFactory>().ReusedWithin(ReuseScope.None);
            container.RegisterAutoWiredAs<Neo4jDeploymentsPool, INeo4jDeploymentsPool>().ReusedWithin(ReuseScope.Container);

            ConfigurePlugins();
            ConfigureMappers();
        }

        private void ConfigurePlugins()
        {
            Plugins.Add(new CancellableRequestsFeature());
            Plugins.Add(new SwaggerFeature());
            Plugins.Add(new CorsFeature());
        }

        private void ConfigureMappers()
        {
            AutoMapping.RegisterConverter<KeyValuePair<string, INeo4jInstance>, Deployment>(kvp => new Deployment
            {
                Id = kvp.Key, 
                DataPath = kvp.Value.DataPath,
                Endpoints = kvp.Value.Endpoints.ConvertTo<Endpoints>(),
                Version = kvp.Value.Version.ConvertTo<Version>(),
                Status = kvp.Value.Status.ToString()
            });
        }
    }
}
