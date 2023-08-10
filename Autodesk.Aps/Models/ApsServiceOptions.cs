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

using Microsoft.AspNetCore.Http;

namespace Autodesk.Aps.Models
{
    /// <summary>
    /// Shared Proxy Options
    /// </summary>
    public class ApsServiceOptions
    {
        private static HostString HOST = new HostString("developer.api.autodesk.com");

        /// <summary>
        /// Proxy uri
        /// </summary>
        public string ProxyUri { get; set; } = "aps-proxy";

        /// <summary>
        /// Autodesk APS client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Autodesk APS client secret
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Autodesk APS OAuth scope
        /// </summary>
        public string Scope { get; set; } = "viewables:read";

        /// <summary>
        /// Autodesk APS OAuth scope for non-proxy services
        /// </summary>
        public string InternalScope { get; set; } = "data:read";

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
