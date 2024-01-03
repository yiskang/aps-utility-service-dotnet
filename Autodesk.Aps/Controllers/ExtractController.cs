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
using System.IO;
using System.IO.Compression;
using Autodesk.Forge;
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
                    return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage("Invalid URN parameter", (int)HttpStatusCode.Forbidden));

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
                    return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage("Invalid objectId parameter", (int)HttpStatusCode.Forbidden));

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    var token = this.tokenService.InternalToken;
                    accessToken = token.AccessToken;
                }

                var decodedObjectId = System.Web.HttpUtility.UrlDecode(objectId);
                var fileList = await CompositeDesignExtractUtil.ListContents2(decodedObjectId, accessToken);
                return Ok(fileList);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }

        /// <summary>
        /// Extract a file from a composite Revit Cloud Worksharing design (i.e. ZIP package)
        /// </summary>
        [HttpPost("{objectId}/objects")]
        public async Task<IActionResult> GetFileFromCompositeDesign([FromRoute] string objectId, [FromQuery] string accessToken, [FromBody] CompositeDesignExtractEntry data, [FromQuery] string projectId, [FromQuery] string folderUrn, [FromQuery] bool uploadToDocs = false, [FromQuery] bool renameConflict = false)
        {
            try
            {
                var filename = data.Name;
                var filenameWhenConflict = data.Name;

                if (string.IsNullOrWhiteSpace(objectId))
                    return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage("Invalid `objectId` parameter", (int)HttpStatusCode.Forbidden));

                if (string.IsNullOrWhiteSpace(filename))
                    return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage("Invalid `data.name` parameter", (int)HttpStatusCode.Forbidden));

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    var token = this.tokenService.InternalToken;
                    accessToken = token.AccessToken;
                }

                if (uploadToDocs == true)
                {
                    if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(folderUrn))
                        return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage("Invalid `projectId` or `folderUrn` parameter when `uploadToDocs` is true", (int)HttpStatusCode.Forbidden));

                    var itemInfo = await DataManagementUtil.GetItemInfoAsync(projectId, folderUrn, filename, accessToken);
                    if (!string.IsNullOrWhiteSpace(itemInfo.Id) && (itemInfo.IsCloudModel == true))
                    {
                        if (renameConflict == false)
                        {
                            return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage("Invalid `folderUrn` parameter when `renameConflict` is false. Uploading extracted file to the same folder where the original file locates will cause data corruption.", (int)HttpStatusCode.Forbidden));
                        }
                        else
                        {
                            var filenameWithoutExt = Path.GetFileNameWithoutExtension(filename);
                            var fileExt = Path.GetExtension(filename);
                            filenameWhenConflict = $"{filenameWithoutExt}-extracted{fileExt}";
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(projectId) || !string.IsNullOrWhiteSpace(folderUrn))
                        return StatusCode((int)HttpStatusCode.BadRequest, new ErrorMessage("When specifying `projectId` and `folderUrn` parameter with `uploadToDocs=false` or without `uploadToDocs=true`, this file won't be uploaded to Docs.", (int)HttpStatusCode.BadRequest));
                }

                var decodedObjectId = System.Web.HttpUtility.UrlDecode(objectId);
                string fileExtractedPath = null;
                try
                {
                    fileExtractedPath = await CompositeDesignExtractUtil.ExtractFile(decodedObjectId, filename, data.Size, data.Offset, data.CompressedSize, accessToken);
                    if (!System.IO.File.Exists(fileExtractedPath))
                        throw new InvalidOperationException($"Failed to extract {filename} from the zip");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.Message);
                    return StatusCode((int)HttpStatusCode.BadRequest, new ErrorMessage($"Failed to extract {filename} from the composite Revit Cloud Worksharing design", (int)HttpStatusCode.BadRequest));
                }

                var fileStream = new FileStream(fileExtractedPath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);

                if (uploadToDocs == false)
                {
                    var cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = filename,
                        Inline = false,
                    };
                    Response.Headers.Add("Content-Disposition", cd.ToString());
                    var mimeType = MimeMapping.MimeUtility.GetMimeMapping(fileExtractedPath);

                    return File(fileStream, mimeType);
                }
                else
                {
                    using (MemoryStream fileMemoryStream = new MemoryStream())
                    {
                        byte[] bytes = new byte[fileStream.Length];
                        fileStream.Read(bytes, 0, (int)fileStream.Length);
                        fileMemoryStream.Write(bytes, 0, (int)fileStream.Length);

                        try
                        {
                            var objectFilename = renameConflict == true ? filenameWhenConflict : filename;
                            var storageObjectId = await DataManagementUtil.CreateFileStorage(projectId, folderUrn, objectFilename, accessToken);
                            var uploadedObjectInfo = await DataManagementUtil.UploadFileAsync(storageObjectId, fileMemoryStream, accessToken);
                            var fileInfoInDocs = await DataManagementUtil.CreateFileItemOrAppendVersionAsync(projectId, folderUrn, uploadedObjectInfo.ObjectId, objectFilename, accessToken);

                            await fileStream.DisposeAsync();

                            return Ok(fileInfoInDocs);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Trace.WriteLine(ex);
                            return StatusCode((int)HttpStatusCode.Forbidden, new ErrorMessage("Failed to upload file to Docs", (int)HttpStatusCode.Forbidden));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}