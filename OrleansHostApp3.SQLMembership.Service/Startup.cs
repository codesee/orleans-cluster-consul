﻿using Library;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;
using System;
using System.Net;
using System.Runtime;

namespace OrleansHostApp3.SQLMembership.Service
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureOrleans(services);
            services.AddMvc();
        }

        public void ConfigureOrleans(IServiceCollection services)
        {
            services.Configure<SiloOptions>(Configuration);

            services.AddSingleton<IGrainFactoryResolver, GrainFactoryResolver>();

            services.AddSingleton<SiloHost>(provider =>
            {
                var options = provider.GetService<IOptions<SiloOptions>>()?.Value;
                return SiloFactory.InitializeSilo(options.DeploymentId, 
                    options.Port, 
                    options.ProxyPort,
                    clusterConfig => {
                        clusterConfig.Globals.SetGlobals(GlobalConfiguration.LivenessProviderType.SqlServer,
                            "Data Source=.\\SQLExpress; Database=Orleans; Trusted_Connection=True;", 
                            null);
                    });
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole().AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            StartSilo(app);
            app.UseMvcWithDefaultRoute();
        }

        public void StartSilo(IApplicationBuilder app)
        {
            var siloHost = app.ApplicationServices.GetRequiredService<SiloHost>();
            if (!siloHost.IsStarted)
            {
                throw new SystemException(String.Format("Failed to start Orleans silo '{0}' as a {1} node", siloHost.Name, siloHost.Type));
            }
        }
    }
}
