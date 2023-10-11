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
using System.Net;

namespace Autodesk.Aps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExtractController : ControllerBase
    {
        readonly ApsTokenService tokenService;
        readonly string folderRootToSave;

        public ExtractController(ApsTokenService tokenService)
        {
            this.tokenService = tokenService;
            this.folderRootToSave = Path.Combine(Directory.GetCurrentDirectory(), "bubbles");
        }

        /// <summary>
        /// Extract SVF derivative files from APS Model Derivative service.
        /// </summary>
        [HttpGet("{urn}/derivatives")]
        public async Task<IActionResult> GetDerivatives([FromRoute] string urn, [FromQuery] string accessToken, [FromQuery] bool deleteAfterDownload = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(urn))
                    return StatusCode((int)HttpStatusCode.Forbidden, "Invalid URN parameter");

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    var token = this.tokenService.InternalToken;
                    accessToken = token.AccessToken;
                }
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }

        
            string folderToSave = Path.Combine(this.folderRootToSave, urn);

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
                            string pathToSave = Path.Combine(folderToSave, "output", resource.LocalPath);
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
                    var manifest = await derivativeApi.GetManifestAsync(urn);
                    var manifestJson = manifest.ToString();
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
                var zipPath = Path.Combine(this.folderRootToSave, zipFilename);
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

        /// <summary>
        /// Extract file list from a composite Revit Cloud Worksharing design (i.e. ZIP package)
        /// </summary>
        [HttpGet("{objectId}/objects:list")]
        public async Task<IActionResult> GetFileListFromCompositeDesign([FromRoute] string objectId, [FromQuery] string accessToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(objectId))
                    return StatusCode((int)HttpStatusCode.Forbidden, "Invalid objectId parameter");

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    var token = this.tokenService.InternalToken;
                    accessToken = token.AccessToken;
                }

                var decodedObjectId = System.Web.HttpUtility.UrlDecode(objectId);
                var fileList = await CompositeDesignExtractUtil.ListContents(decodedObjectId, accessToken);
                return Ok(fileList);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}