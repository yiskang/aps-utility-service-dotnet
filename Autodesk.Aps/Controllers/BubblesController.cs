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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Autodesk.Aps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BubblesController : ControllerBase
    {
        readonly string bubblesRootFolder;
        public BubblesController()
        {
            this.bubblesRootFolder = Path.Combine(Directory.GetCurrentDirectory(), "bubbles");
        }

        [Route("derivativeservice/v2/manifest/{urn}")]
        [Route("modelderivative/v2/designdata/{urn}/manifest")]
        public async Task<IActionResult> GetLocalManifest(string urn)
        {
            string bubblesFolder = Path.Combine(this.bubblesRootFolder, urn);
            if (!System.IO.Directory.Exists(bubblesFolder))
                return NotFound();

            string manifestPath = Path.Combine(bubblesFolder, "manifest.json");
            if (!System.IO.File.Exists(manifestPath))
                return NotFound();

            var mimeType = MimeMapping.MimeUtility.GetMimeMapping(manifestPath);
            var manifestJsonStream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read);
            return File(manifestJsonStream, mimeType);
        }

        [Route("derivativeservice/v2/derivatives/{**rest}")]
        [Route("modelderivative/v2/designdata/{urn}/manifest/{**rest}")]
        public async Task<IActionResult> GetLocalDerivatives([FromRoute] string urn, [FromRoute] string rest)
        {
            string derivativeUrn = Uri.UnescapeDataString(rest);
            var result = derivativeUrn.Split(':');
            var bubbleUrn = result.Last();

            result = bubbleUrn.Split('/');
            if (string.IsNullOrWhiteSpace(urn))
                urn = result.First();

            string bubblesFolder = Path.Combine(this.bubblesRootFolder, urn);
            if (!System.IO.Directory.Exists(bubblesFolder))
                return NotFound();

            var fileUrn = string.Join('/', result.Skip(1));
            string filePath = Path.Combine(bubblesFolder, fileUrn);
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var mimeType = MimeMapping.MimeUtility.GetMimeMapping(filePath);
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, mimeType);
        }
    }
}