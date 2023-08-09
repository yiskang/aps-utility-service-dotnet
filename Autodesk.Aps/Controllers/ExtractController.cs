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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Autodesk.Aps.Models;
using Autodesk.Aps.Libs;
using RestSharp;
using System.IO;
using System.IO.Compression;
using Autodesk.Forge;
using Newtonsoft.Json;

namespace Autodesk.Aps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExtractController : ControllerBase
    {
        [HttpGet("{urn}/derivatives")]
        public async Task<IActionResult> GetDerivatives([FromRoute] string urn, [FromQuery] string accessToken, [FromQuery] bool deleteAfterDownload = true)
        {
            string folderToSave = Path.Combine(Directory.GetCurrentDirectory(), "tmp", urn);

            if (!Directory.Exists(folderToSave))
            {
                List<DerivativeExtractUtil.Resource> resourcesToDownload = await DerivativeExtractUtil.ExtractSVFAsync(urn, accessToken);

                System.Diagnostics.Trace.WriteLine($"Downloading svf derivative files for urn: `{urn}` ...");

                foreach (DerivativeExtractUtil.Resource resource in resourcesToDownload)
                {
                    System.Diagnostics.Trace.WriteLine($"Downloading `{resource.LocalPath}` ...");

                    try
                    {
                        var response = await DerivativeExtractUtil.DownloadDerivativeAsync(resource.RemotePath, accessToken);

                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        {
                            // something went wrong with this file...
                            throw new InvalidOperationException($"Error downloading `{resource.FileName}`: `{response.StatusCode}`");
                            // any other action?
                        }
                        else
                        {
                            // combine with selected local path
                            string pathToSave = Path.Combine(folderToSave, resource.LocalPath);
                            if (System.IO.File.Exists(pathToSave))
                                continue;

                            try
                            {
                                // ensure local dir exists
                                Directory.CreateDirectory(Path.GetDirectoryName(pathToSave));
                                // save file
                                if (resource.LocalPath.IndexOf(".gz") > -1)
                                {
                                    using var fs = new FileStream(pathToSave, FileMode.CreateNew);
                                    using var gzip = new GZipStream(fs, CompressionMode.Compress);
                                    await gzip.WriteAsync(response.RawBytes, 0, response.RawBytes.Length);
                                }
                                else
                                {
                                    System.IO.File.WriteAllBytes(pathToSave, response.RawBytes);
                                }
                            }
                            catch
                            {
                                throw new InvalidOperationException($"Error saving `{resource.FileName}`");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.WriteLine(ex.Message);
                    }
                }

                try
                {
                    var derivativeApi = new DerivativesApi();
                    derivativeApi.Configuration.AccessToken = accessToken;

                    // get the manifest for the URN
                    dynamic manifest = await derivativeApi.GetManifestAsync(urn);
                    var manifestJson = JsonConvert.SerializeObject(manifest);
                    System.IO.File.WriteAllText(Path.Combine(folderToSave, "manifest.json"), manifestJson);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    return NotFound($"Failed to save manifest file to json");
                }
            }

            try
            {
                var zipFilename = $"extracted-derivative-{urn}.zip";
                var zipPath = Path.Combine(Directory.GetCurrentDirectory(), "tmp", zipFilename);
                if (!System.IO.File.Exists(zipPath))
                    ZipFile.CreateFromDirectory(folderToSave, zipPath);

                var cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = zipFilename,
                    Inline = false,
                };

                Response.Headers.Add("Content-Disposition", cd.ToString());
                var zipFileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);

                if (deleteAfterDownload)
                {
                    bool recursivelyDelete = Directory.GetFiles(folderToSave).Length > 0 ? true : false;
                    Directory.Delete(folderToSave, recursivelyDelete);
                }


                return File(zipFileStream, "application/zip");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
                return NotFound($"Failed to cerate zip for extracted derivatives");
            }
        }
    }
}