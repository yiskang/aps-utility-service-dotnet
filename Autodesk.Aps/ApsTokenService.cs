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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Aps.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Autodesk.Aps
{
    public class ApsTokenService
    {
        public HttpClient Client { get; private set; }

        public ApsServiceOptions Options { get; private set; }

        private ApsToken token;

        private ApsToken internalToken;

        public ApsTokenService(IOptions<ApsServiceOptions> options)
        {
            Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
            Options = options.Value;
        }

        public ApsToken Token
        {
            get
            {
                if (token == null || DateTime.Now >= token.Expiration)
                {
                    var opts = this.Options;
                    token = this.FetchToken(opts.Scope).Result;
                    token.Expiration = DateTime.Now.AddSeconds(token.ExpiresIn - 600);
                }
                return token;
            }
        }

        public ApsToken InternalToken
        {
            get
            {
                if (internalToken == null || DateTime.Now >= internalToken.Expiration)
                {
                    var opts = this.Options;
                    internalToken = this.FetchToken(opts.InternalScope).Result;
                    internalToken.Expiration = DateTime.Now.AddSeconds(internalToken.ExpiresIn - 600);
                }
                return internalToken;
            }
        }

        internal async Task<ApsToken> FetchToken(string scopes)
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
                new KeyValuePair<string, string>("scope", scopes),
            });
            requestMessage.Content = content;

            var result = await Client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            var json = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ApsToken>(json);
        }
    }
}
