/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Developer Advocacy and Support
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using Autodesk.Aps.Models;
using dotenv.net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Autodesk.Aps
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            // READ APS credentials from .dev or app settings
            DotEnv.Load();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();

            services.Configure<ApsProxyOptions>(configureOptions =>
            {
                var envConfiguration = DotEnv.Read();
                var clientId = Configuration.GetSection("Credentials:ClientId").Value ?? envConfiguration["APS_CLIENT_ID"];
                var clientSecret = Configuration.GetSection("Credentials:ClientSecret").Value ?? envConfiguration["APS_CLIENT_ID"];
                var scope = Configuration.GetSection("Credentials:Scope").Value;

                configureOptions.ClientId = string.IsNullOrEmpty(clientId) ? Environment.GetEnvironmentVariable("APS_CLIENT_ID") : clientId;
                configureOptions.ClientSecret = string.IsNullOrEmpty(clientSecret) ? Environment.GetEnvironmentVariable("APS_CLIENT_SECRET") : clientSecret;
                configureOptions.Scope = scope;
            });

            services.AddSingleton<ApsTokenService>();
            services.AddProxies();
            services.AddMvc().AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<ApsProxyOptions> apsOpts)
        {
             if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
                builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(origin => true) // allow any origin
                    .AllowCredentials()
            );

            var apsConfig = apsOpts.Value;
            var apsURL = UriHelper.BuildAbsolute(apsConfig.Scheme, apsConfig.Host);
            var proxyOpts = HttpProxyOptionsBuilder.Instance
                            .WithBeforeSend((context, message) =>
                            {
                                var proxyService = context.RequestServices.GetRequiredService<ApsTokenService>();
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
                                    message = "Request cannot be proxied",
                                    reason = exception.ToString()
                                };
                                await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                            }).Build();

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            var proxyPrefix = apsConfig.ProxyUri + "/{**catchall}";

            app.UseProxies(proxies =>
            {
                proxies.Map(proxyPrefix, proxy =>
                {
                    proxy.UseHttp((context, args) =>
                    {
                        var queries = context.Request.QueryString;
                        if (queries.HasValue)
                        {
                            return $"{apsURL}/{args["catchall"]}{queries.Value}";
                        }
                        return $"{apsURL}/{args["catchall"]}";
                    },
                    builder =>
                    {
                        builder.WithBeforeSend((context, message) =>
                        {
                            var proxyService = context.RequestServices.GetRequiredService<ApsTokenService>();
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
                                reason = exception.ToString()
                            };
                            await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                        });
                    });
                });
            });
        }
    }
}
