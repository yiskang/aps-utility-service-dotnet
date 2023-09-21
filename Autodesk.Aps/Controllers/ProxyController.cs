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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using AspNetCore.Proxy.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using AspNetCore.Proxy;
using Autodesk.Aps.Models;
using Microsoft.Extensions.Hosting;

namespace Autodesk.Aps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase
    {
        readonly ApsTokenService tokenService;
        readonly ApsServiceOptions apsProxyConfig;
        readonly HttpProxyOptions httpProxyOptions;

        public ProxyController(ApsTokenService tokenService, IOptions<ApsServiceOptions> apsOpts)
        {
            this.tokenService = tokenService;
            this.apsProxyConfig = apsOpts.Value;
            this.httpProxyOptions = HttpProxyOptionsBuilder.Instance
                            .WithBeforeSend((context, message) =>
                            {
                                var token = this.tokenService.Token;

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
        }

        [Route("{**rest}")]
        public Task ProxyCatchAll(string rest)
        {
            var apsConfig = this.apsProxyConfig;

            HostString host = rest.Contains("manifest") ? apsConfig.Host : apsConfig.DerivativeHost;
            var apsURL = UriHelper.BuildAbsolute(apsConfig.Scheme, host);

            var queries = this.Request.QueryString;
            if (queries.HasValue)
            {
                return this.HttpProxyAsync($"{apsURL}{rest}{queries.Value}", this.httpProxyOptions);
            }
            return this.HttpProxyAsync($"{apsURL}{rest}", this.httpProxyOptions);
        }
    }
}