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

using Microsoft.AspNetCore.Http;

namespace Autodesk.Forge.Models
{
    /// <summary>
    /// Shared Proxy Options
    /// </summary>
    public class ForgeProxyOptions
    {
        private static HostString HOST = new HostString("developer.api.autodesk.com");

        /// <summary>
        /// Proxy uri
        /// </summary>
        public string ProxyUri { get; set; } = "forge-proxy";

        /// <summary>
        /// Autodesk Forge client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Autodesk Forge client secret
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Autodesk Forge OAuth scope
        /// </summary>
        public string Scope { get; set; } = "viewables:read";

        /// <summary>
        /// Destination uri scheme
        /// </summary>
        public string Scheme
        {
            get
            {
                return "https";
            }
        }

        /// <summary>
        /// Destination uri host
        /// </summary>
        public HostString Host
        {
            get
            {
                return HOST;
            }
        }
    }
}
