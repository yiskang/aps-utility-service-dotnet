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
