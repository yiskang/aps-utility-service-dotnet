/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
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
//
// Forge Proxy Server dotNetCore
// by Eason Kang - Autodesk Developer Network (ADN)
//
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

            var uri = new Uri(UriHelper.BuildAbsolute(opts.Scheme, opts.Host, "/authentication/v1/authenticate"));
            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = HttpMethod.Post;

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", opts.ClientId),
                new KeyValuePair<string, string>("client_secret", opts.ClientSecret),
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
