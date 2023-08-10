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
        /// https://github.com/wallabyway/bim360-zip-extract/blob/master/server.js#L49C18-L49C31
        /// https://github.com/wallabyway/bim360-zip-extract/blob/master/server.js#L155
        /// </summary>
        public async static Task<List<Models.ZipArchiveEntry>> ListContents(string objectId, string accessToken)
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

            var target = response["results"][objectInfo.ObjectKey];

            // Fetch ZIP header and footer
            int chunkSize = 4 * 1024; // only need 16k bytes of data
            var downloadURL = target.url;
            var fileSize = target.size;
            int footerOffset = Convert.ToInt32(fileSize - chunkSize);

            var zipHeaderResponse = await FetchFileByRangeAsync(downloadURL, 0, chunkSize);
            var zipFooterResponse = await FetchFileByRangeAsync(downloadURL, footerOffset, chunkSize);

            var zipFilename = "tmp-" + Guid.NewGuid() + ".zip";
            string zipPath = Path.Combine(Directory.GetCurrentDirectory(), "tmp", zipFilename);

            try
            {
                // Combine ZIP hear and footer data bytes
                var data = new byte[fileSize];
                Array.Copy(zipHeaderResponse.RawBytes, 0, data, 0, zipHeaderResponse.RawBytes.Length);
                Array.Copy(zipFooterResponse.RawBytes, 0, data, footerOffset, zipFooterResponse.RawBytes.Length);

                using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
                await fs.WriteAsync(data);
            }
            catch (Exception ex)
            {
                var message = $"Cannot create zip: {ex.Message}";
                System.Diagnostics.Trace.WriteLine(message);
                throw new InvalidOperationException(message);
            }

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
                throw new InvalidOperationException(message);
            }
        }
    }
}