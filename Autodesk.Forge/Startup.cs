//
// Copyright (c) Autodesk, Inc. All rights reserved
// Copyright (c) .NET Foundation. All rights reserved.
//
// Licensed under the Apache License, Version 2.0.
// See LICENSE in the project root for license information.
// 
// Forge Proxy Server dotNetCore
// by Eason Kang - Autodesk Developer Network (ADN)
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AspNetCore.Proxy;
using Autodesk.Forge.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Autodesk.Forge
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            Environment = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IHostingEnvironment Environment { get; set; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services.Configure<ForgeProxyOptions>(configureOptions =>
            {
                var clientId = Configuration.GetSection("Credentials:ClientId").Value;
                var clientSecret = Configuration.GetSection("Credentials:ClientSecret").Value;
                var scope = Configuration.GetSection("Credentials:Scope").Value;

                configureOptions.ClientId = clientId;
                configureOptions.ClientSecret = clientSecret;
                configureOptions.Scope = scope;
            });

            services.AddSingleton<ForgeTokenService>();
            services.AddProxies();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IOptions<ForgeProxyOptions> forgeOpts)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
                builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
            );

            var forgeConfig = forgeOpts.Value;
            var forgeURL = UriHelper.BuildAbsolute(forgeConfig.Scheme, forgeConfig.Host);
            var proxyOpts = ProxyOptions.Instance
                            .WithBeforeSend((context, message) =>
                            {
                                var proxyService = context.RequestServices.GetRequiredService<ForgeTokenService>();
                                var token = proxyService.Token;

                                // Set something that is needed for the downstream endpoint.
                                message.Headers.Add("X-Forwarded-Host", context.Request.Host.Host);
                                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                                context.Response.Headers.Remove("Content-Type");
                                context.Response.Headers.Append("Content-Type", "application/json; chartset=utf-8");

                                return Task.CompletedTask;
                            })
                            .WithHandleFailure(async (context, exception) =>
                            {
                                // Return a custom error response.
                                context.Response.StatusCode = 403;
                                var result = new
                                {
                                    message = "Request cannot be proxyed",
                                    resason = exception.ToString()
                                };
                                await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                            });

            var proxyPrefix = forgeConfig.ProxyUri + "/{**catchall}";
            app.UseProxy(proxyPrefix, (context, args) => {
                var queries = context.Request.QueryString;
                if (queries.HasValue)
                {
                    return $"{forgeURL}/{args["catchall"]}{queries.Value}";
                }
                return $"{forgeURL}/{args["catchall"]}";
            }, proxyOpts);

            app.UseMvcWithDefaultRoute();
        }
    }
}
