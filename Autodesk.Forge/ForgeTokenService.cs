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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Forge.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Autodesk.Forge
{
    public class ForgeTokenService
    {
        public HttpClient Client { get; private set; }

        public ForgeProxyOptions Options { get; private set; }

        private ForgeToken token;

        private DateTime expiration;

        public ForgeTokenService(IOptions<ForgeProxyOptions> options)
        {
            Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
            Options = options.Value;
        }

        public ForgeToken Token
        {
            get
            {
                if (token == null || DateTime.Now >= expiration)
                {
                    token = this.FetchToken().Result;
                    expiration = DateTime.Now.AddSeconds(token.ExpiresIn - 600);
                }
                return token;
            }
        }

        internal async Task<ForgeToken> FetchToken()
        {
            var requestMessage = new HttpRequestMessage();

            var opts = this.Options;

            var uri = new Uri(UriHelper.BuildAbsolute(opts.Scheme, opts.Host, "/authentication/v2/token"));
            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = HttpMethod.Post;

            var encodedKey = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                            .GetBytes(opts.ClientId + ":" + opts.ClientSecret));

            requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encodedKey);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("scope", opts.Scope),
            });
            requestMessage.Content = content;

            var result = await Client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var json = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ForgeToken>(json);
        }
    }
}
