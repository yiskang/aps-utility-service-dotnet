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
using System.Linq;
using System.Threading.Tasks;
using RestSharp;
using Autodesk.Forge;
using Autodesk.Forge.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Autodesk.Aps.Libs
{
    public static class DataManagementUtil
    {
        public class ObjectInfo
        {
            public string BucketKey { get; set; }
            public string ObjectKey { get; set; }
        }

        public class FileInfoInDocs
        {
            public string ProjectId { get; set; }
            public string FolderUrn { get; set; }
            public string ItemId { get; set; }
            public string VersionId { get; set; }
        }

        public static ObjectInfo ExtractObjectInfo(string objectId)
        {
            var result = System.Text.RegularExpressions.Regex.Match(objectId, ".*:.*:(.*)/(.*)");
            var bucketKey = result.Groups[1].Value; ;
            var objectKey = result.Groups[2].Value;

            return new ObjectInfo
            {
                BucketKey = bucketKey,
                ObjectKey = objectKey
            };
        }

        public async static Task<dynamic> GetFileDownloadUrl(string objectId, string accessToken)
        {
            var objectInfo = DataManagementUtil.ExtractObjectInfo(objectId);
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

        public static async Task<RestResponse> FetchFileByRangeAsync(string url, int offset, int length)
        {
            var client = new RestClient();
            var request = new RestRequest(url, Method.Get);
            var rangeBytes = $"bytes={offset}-{offset + length}";
            request.AddHeader("Range", rangeBytes);

            var response = await client.ExecuteAsync(request);
            return response;
        }

        public async static Task<string> CreateFileStorage(string projectId, string folderUrn, string filename, string accessToken)
        {
            ProjectsApi projectsApi = new ProjectsApi();
            projectsApi.Configuration.AccessToken = accessToken;

            var storageRelData = new StorageRelationshipsTargetData(StorageRelationshipsTargetData.TypeEnum.Folders, folderUrn);
            var storageTarget = new CreateStorageDataRelationshipsTarget(storageRelData);
            var storageRel = new CreateStorageDataRelationships(storageTarget);
            var attributes = new BaseAttributesExtensionObject(string.Empty, string.Empty, new JsonApiLink(string.Empty), null);
            var storageAtt = new CreateStorageDataAttributes(filename, attributes);
            var storageData = new CreateStorageData(CreateStorageData.TypeEnum.Objects, storageAtt, storageRel);
            var storage = new CreateStorage(new JsonApiVersionJsonapi(JsonApiVersionJsonapi.VersionEnum._0), storageData);

            try
            {
                var res = await projectsApi.PostStorageAsync(projectId, storage);
                string id = res.data.id;
                return id;
            }
            catch (Forge.Client.ApiException ex)
            {
                throw ex;
            }
        }

        public async static Task<ObjectDetails> UploadFileAsync(string objectId, MemoryStream fileMemoryStream, string accessToken)
        {
            var objectInfo = ExtractObjectInfo(objectId);
            // Get object upload url via OSS Direct-S3 API
            var objectsApi = new ObjectsApi();
            objectsApi.Configuration.AccessToken = accessToken;

            var payload = new List<UploadItemDesc> {
                new UploadItemDesc(objectInfo.ObjectKey, fileMemoryStream)
            };

            var results = await objectsApi.uploadResources(
                objectInfo.BucketKey,
                payload
            );

            if (results[0].Error)
            {
                throw new Exception(results[0].completed.ToString());
            }

            var json = results[0].completed.ToJson();
            return json.ToObject<ObjectDetails>();
        }

        public async static Task<FileInfoInDocs> CreateFileItemOrAppendVersionAsync(string projectId, string folderUrn, string objectId, string filename, string accessToken)
        {
            ItemsApi itemsApi = new ItemsApi();
            itemsApi.Configuration.AccessToken = accessToken;
            var itemBody = new CreateItem
            (
                new JsonApiVersionJsonapi
                (
                    JsonApiVersionJsonapi.VersionEnum._0
                ),
                new CreateItemData
                (
                    CreateItemData.TypeEnum.Items,
                    new CreateItemDataAttributes
                    (
                        DisplayName: filename,
                        new BaseAttributesExtensionObject
                        (
                            Type: "items:autodesk.bim360:File",
                            Version: "1.0"
                        )
                    ),
                    new CreateItemDataRelationships
                    (
                        new CreateItemDataRelationshipsTip
                        (
                            new CreateItemDataRelationshipsTipData
                            (
                                CreateItemDataRelationshipsTipData.TypeEnum.Versions,
                                CreateItemDataRelationshipsTipData.IdEnum._1
                            )
                        ),
                        new CreateStorageDataRelationshipsTarget
                        (
                            new StorageRelationshipsTargetData
                            (
                                StorageRelationshipsTargetData.TypeEnum.Folders,
                                Id: folderUrn
                            )
                        )
                    )
                ),
                new List<CreateItemIncluded>
                {
                    new CreateItemIncluded
                    (
                        CreateItemIncluded.TypeEnum.Versions,
                        CreateItemIncluded.IdEnum._1,
                        new CreateStorageDataAttributes
                        (
                            filename,
                            new BaseAttributesExtensionObject
                            (
                                Type:"versions:autodesk.bim360:File",
                                Version:"1.0"
                            )
                        ),
                        new CreateItemRelationships(
                            new CreateItemRelationshipsStorage
                            (
                                new CreateItemRelationshipsStorageData
                                (
                                    CreateItemRelationshipsStorageData.TypeEnum.Objects,
                                    objectId
                                )
                            )
                        )
                    )
                }
            );

            string itemId = "";
            string versionId = "";
            try
            {
                DynamicJsonResponse postItemJsonResponse = await itemsApi.PostItemAsync(projectId, itemBody);
                var uploadItem = postItemJsonResponse.ToObject<ItemCreated>();
                Console.WriteLine("Attributes of uploaded BIM 360 file");
                Console.WriteLine($"\n\t{uploadItem.Data.Attributes.ToJson()}");
                itemId = uploadItem.Data.Id;
                versionId = uploadItem.Data.Relationships.Tip.Data.Id;
            }
            catch (Forge.Client.ApiException ex)
            {
                //we met a conflict
                dynamic errorContent = JsonConvert.DeserializeObject<JObject>(ex.ErrorContent);
                if (errorContent.Errors?[0].Status == "409")//Conflict
                {
                    //Get ItemId of our file
                    itemId = await GetItemIdAsync(projectId, folderUrn, filename, accessToken);

                    //Lets create a new version
                    versionId = await UpdateVersionAsync(projectId, itemId, objectId, filename, accessToken);
                }
            }

            if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(versionId))
            {
                throw new InvalidOperationException("Failed to Create/Append file version");
            }

            var fileInfo = new FileInfoInDocs
            {
                ProjectId = projectId,
                FolderUrn = folderUrn,
                ItemId = itemId,
                VersionId = versionId
            };

            return fileInfo;
        }

        public async static Task<string> GetItemIdAsync(string projectId, string folderUrn, string filename, string accessToken)
        {
            FoldersApi foldersApi = new FoldersApi();
            foldersApi.Configuration.AccessToken = accessToken;
            DynamicDictionaryItems itemList = await GetFolderItems(projectId, folderUrn, accessToken);
            var item = itemList.Cast<KeyValuePair<string, dynamic>>().FirstOrDefault(item => item.Value.attributes.displayName.Equals(filename, StringComparison.OrdinalIgnoreCase));
            return item.Value?.Id;
        }

        private static async Task<string> UpdateVersionAsync(string projectId, string itemId, string objectId, string filename, string accessToken)
        {
            var versionsApi = new VersionsApi();
            versionsApi.Configuration.AccessToken = accessToken;

            var relationships = new CreateVersionDataRelationships
            (
                new CreateVersionDataRelationshipsItem
                (
                    new CreateVersionDataRelationshipsItemData
                    (
                        CreateVersionDataRelationshipsItemData.TypeEnum.Items,
                        itemId
                    )
                ),
                new CreateItemRelationshipsStorage
                (
                    new CreateItemRelationshipsStorageData
                    (
                        CreateItemRelationshipsStorageData.TypeEnum.Objects,
                        objectId
                    )
                )
            );
            var createVersion = new CreateVersion
            (
                new JsonApiVersionJsonapi
                (
                    JsonApiVersionJsonapi.VersionEnum._0
                ),
                new CreateVersionData
                (
                    CreateVersionData.TypeEnum.Versions,
                    new CreateStorageDataAttributes
                    (
                        filename,
                        new BaseAttributesExtensionObject
                        (
                            "versions:autodesk.bim360:File",
                            "1.0",
                            new JsonApiLink(string.Empty),
                            null
                        )
                    ),
                    relationships
                )
            );

            dynamic versionResponse = await versionsApi.PostVersionAsync(projectId, createVersion);
            var versionId = versionResponse.data.id;
            return versionId;
        }

        private async static Task<DynamicDictionaryItems> GetFolderItems(string projectId, string folderId, string accessToken)
        {
            var foldersApi = new FoldersApi();
            foldersApi.Configuration.AccessToken = accessToken;
            dynamic folderContents = await foldersApi.GetFolderContentsAsync(projectId,
                                                       folderId,
                                                       filterType: new List<string>() { "items" },
                                                       filterExtensionType: new List<string>() { "items:autodesk.bim360:File" }
            );

            var folderData = new DynamicDictionaryItems(folderContents.data);

            return folderData;
        }
    }
}