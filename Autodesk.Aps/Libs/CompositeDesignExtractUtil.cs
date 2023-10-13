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

// Forge Extractor
// Based on https://forge.autodesk.com/blog/forge-svf-extractor-nodejs

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;
using Autodesk.Forge;
using Autodesk.Forge.Model;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;

namespace Autodesk.Aps.Libs
{
    public static class CompositeDesignExtractUtil
    {
        public class ObjectInfo
        {
            public string BucketKey { get; set; }
            public string ObjectKey { get; set; }
        }

        public static ObjectInfo ExtractObjectInfo(string objectId)
        {
            var result = objectId.Replace("urn:adsk.objects:os.object:", string.Empty).Split('/');
            var bucketKey = result[0];
            var objectKey = result[1];

            return new ObjectInfo
            {
                BucketKey = bucketKey,
                ObjectKey = objectKey
            };
        }

        public static async Task<RestResponse> FetchFileByRangeAsync(string url, int offset, int length)
        {
            var client = new RestClient();
            var request = new RestRequest(url, Method.Get);
            var rangeBytes = $"bytes={offset}-{offset + length}";
            request.AddHeader("Range", rangeBytes);

            var response = await client.ExecuteAsync(request);
            return response;
        }

        /// <summary>
        /// Create a temporary ZIP file
        /// </summary>
        /// <param name="fileDownloadURL">The S3 download URL for the composite design (i.e. ZIP)</param>
        /// <param name="fileFullSize">The ZIP package file size</param>
        /// <param name="fileOffset">The bytes offset for that target file (ZIP entry)</param>
        /// <param name="fileSize">The compressedSize in bytes for that target file (ZIP entry)</param>
        /// <returns></returns>
        private static async Task<string> CreateTempZipFileAsync(string fileDownloadURL, int fileFullSize, int fileOffset = 0, int? fileSize = null)
        {
            int chunkSize = 4 * 1024; // only need 16k bytes of data
            int zipHeaderOffset = 128;
            var downloadURL = fileDownloadURL;
            RestResponse fileResponse = null;

            try
            {
                // Fetch ZIP header and footer
                int footerOffset = Convert.ToInt32(fileFullSize - chunkSize);

                var zipHeaderResponse = await FetchFileByRangeAsync(downloadURL, 0, chunkSize);
                var zipFooterResponse = await FetchFileByRangeAsync(downloadURL, footerOffset, chunkSize);

                if (fileSize.HasValue)
                    fileResponse = await FetchFileByRangeAsync(downloadURL, fileOffset, fileSize.Value + zipHeaderOffset);

                var zipFilename = "tmp-" + Guid.NewGuid() + ".zip";
                string zipPath = Path.Combine(Directory.GetCurrentDirectory(), "tmp", zipFilename);

                // Combine ZIP hear and footer data bytes
                var data = new byte[fileFullSize];
                Array.Copy(zipHeaderResponse.RawBytes, 0, data, 0, zipHeaderResponse.RawBytes.Length);

                if (fileResponse != null)
                    Array.Copy(fileResponse.RawBytes, 0, data, fileOffset, fileResponse.RawBytes.Length);

                Array.Copy(zipFooterResponse.RawBytes, 0, data, footerOffset, zipFooterResponse.RawBytes.Length);

                using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
                await fs.WriteAsync(data);

                return zipPath;
            }
            catch (Exception ex)
            {
                var message = $"Cannot create zip: {ex.Message}";
                System.Diagnostics.Trace.WriteLine(message);
                throw new InvalidOperationException(message, ex);
            }
        }

        public async static Task<dynamic> GetFileDownloadUrl(string objectId, string accessToken)
        {
            var objectInfo = ExtractObjectInfo(objectId);
            // Get object download url via OSS Direct-S3 API
            var objectsApi = new ObjectsApi();
            objectsApi.Configuration.AccessToken = accessToken;

            List<PostBatchSignedS3DownloadPayloadItem> items = new List<PostBatchSignedS3DownloadPayloadItem>()
            {
                new PostBatchSignedS3DownloadPayloadItem(objectInfo.ObjectKey)
            };

            PostBatchSignedS3DownloadPayload payload = new PostBatchSignedS3DownloadPayload(items);

            dynamic response = await objectsApi.getS3DownloadURLsAsync(
                objectInfo.BucketKey,
                payload
            );

            return response;
        }

        /// <summary>
        /// https://github.com/wallabyway/bim360-zip-extract/blob/master/server.js#L49C18-L49C31
        /// https://github.com/wallabyway/bim360-zip-extract/blob/master/server.js#L155
        /// </summary>
        public async static Task<List<Models.ZipArchiveEntry>> ListContents(string objectId, string accessToken)
        {
            dynamic response = await GetFileDownloadUrl(objectId, accessToken);
            var objectInfo = ExtractObjectInfo(objectId);
            var target = response["results"][objectInfo.ObjectKey];

            // Fetch ZIP header and footer
            var downloadURL = Convert.ToString(target.url);
            var fileSize = Convert.ToInt32(target.size);

            string zipPath = await CreateTempZipFileAsync(downloadURL, fileSize);

            try
            {
                // Read ZIP entry data for file lists
                using var zipFileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
                using ZipArchive zip = new ZipArchive(zipFileStream);
                return zip.Entries.Select(entry => new Models.ZipArchiveEntry
                {
                    Comment = entry.Comment,
                    CompressedSize = entry.CompressedLength,
                    Crc32 = entry.Crc32,
                    ExternalAttributes = entry.ExternalAttributes,
                    FullName = entry.FullName,
                    LastWriteTime = entry.LastWriteTime,
                    Size = entry.Length,
                    Name = entry.Name
                }).ToList();
            }
            catch (Exception ex)
            {
                var message = $"Cannot read zip: {ex.Message}";
                System.Diagnostics.Trace.WriteLine(message);
                throw new InvalidOperationException(message, ex);
            }
        }

        /// <summary>
        /// https://github.com/wallabyway/bim360-zip-extract/blob/master/server.js#L49C18-L49C31
        /// https://github.com/wallabyway/bim360-zip-extract/blob/master/server.js#L155
        /// Change ZIP function
        /// </summary>
        public async static Task<List<ZipEntry>> ListContents2(string objectId, string accessToken)
        {
            dynamic response = await GetFileDownloadUrl(objectId, accessToken);
            var objectInfo = ExtractObjectInfo(objectId);
            var target = response["results"][objectInfo.ObjectKey];

            // Fetch ZIP header and footer
            var downloadURL = Convert.ToString(target.url);
            var fileSize = Convert.ToInt32(target.size);

            string zipPath = await CreateTempZipFileAsync(downloadURL, fileSize);

            try
            {
                // Read ZIP entry data for file lists
                using var zipFileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
                using ICSharpCode.SharpZipLib.Zip.ZipFile zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(zipFileStream);
                var entries = new List<ZipEntry>();
                foreach (ZipEntry entry in zip)
                {
                    if (!entry.IsFile) continue;

                    entries.Add(entry);
                }
                return entries;
            }
            catch (Exception ex)
            {
                var message = $"Cannot read zip: {ex.Message}";
                System.Diagnostics.Trace.WriteLine(message);
                throw new InvalidOperationException(message, ex);
            }
        }

        /// <summary>
        /// https://github.com/wallabyway/bim360-zip-extract/blob/master/server.js#L212
        /// </summary>
        public async static Task<string> ExtractFile(string objectId, string filename, int fileSize, int offset, int compressedFileSize, string accessToken)
        {
            dynamic response = await GetFileDownloadUrl(objectId, accessToken);
            var objectInfo = ExtractObjectInfo(objectId);
            var target = response["results"][objectInfo.ObjectKey];

            // Fetch ZIP header and footer
            var downloadURL = Convert.ToString(target.url);
            var fullFileSize = Convert.ToInt32(target.size);

            // now, fetch the exact bytes from bim360, and write to our temp file
            //var megabyteToDownload = Math.Round((decimal)compressedFileSize / 100000) / 10;

            // var logMsg = $"(downloading {megabyteToDownload} MB) {filename} , zip offset: {offset}";
            // System.Diagnostics.Trace.WriteLine(logMsg);

            try
            {
                string zipPath = await CreateTempZipFileAsync(downloadURL, fullFileSize, offset, compressedFileSize);
                string fileExtractedPath = Path.Combine(Directory.GetCurrentDirectory(), "tmp", filename);

                try
                {
                    // Read ZIP entry data for file lists
                    using var zipFileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
                    using (ICSharpCode.SharpZipLib.Zip.ZipFile zip = new ICSharpCode.SharpZipLib.Zip.ZipFile(zipFileStream))
                    {
                        var fileEntry = zip.GetEntry(filename);
                        if (fileEntry == null)
                            throw new InvalidDataException($"Cannot find the `{filename}` in the ZIP");

                        var zipInputStream = zip.GetInputStream(fileEntry);
                        using (var streamWriter = File.Create(fileExtractedPath))
                        {
                            int size = fileSize;
                            byte[] buffer = new byte[size];

                            while ((size = zipInputStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                streamWriter.Write(buffer, 0, size);
                            }
                        }

                        return fileExtractedPath;
                    }
                }
                catch (Exception ex)
                {
                    var message = $"Cannot extract `{filename}` from zip: {ex.Message}";
                    System.Diagnostics.Trace.WriteLine(message);
                    throw new InvalidOperationException(message, ex);
                }
            }
            catch (Exception ex)
            {
                var message = $"Cannot create a temp zip for extracting`{filename}`: {ex.Message}";
                System.Diagnostics.Trace.WriteLine(message);
                throw new InvalidOperationException(message, ex);
            }
        }
    }
}