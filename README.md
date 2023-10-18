
![.NET](https://img.shields.io/badge/.NET-7.0-blue.svg)
![Platforms](https://img.shields.io/badge/platform-windows%20%7C%20osx%20%7C%20linux-lightgray.svg)
[![License](http://img.shields.io/:license-mit-blue.svg)](http://opensource.org/licenses/MIT)

# Autodesk APS Utility Service

## Overview

This sample is demonstrating the following in a set of Web API:

1. A proxy server with [AspNetCore.Proxy](https://github.com/twitchax/AspNetCore.Proxy) to forward all requests from the APS Viewer to this backend without showing access token in your viewer frontend for viewing SVF model (not SVF2).
2. A utility service of the below by single API call
   - Extract file list from composite [Revit Cloud Workshariong model](https://aps.autodesk.com/blog/make-composite-revit-design-work-design-automation-api-revit) that `attributes.extension.isCompositeDesign` is true in the response of Data Management API. (This idea is from https://github.com/wallabyway/bim360-zip-extract)
   - Extract a file from composite [Revit Cloud Workshariong model](https://aps.autodesk.com/blog/make-composite-revit-design-work-design-automation-api-revit) that `attributes.extension.isCompositeDesign` is true in the response of Data Management API. (This idea is from https://github.com/wallabyway/bim360-zip-extract)
   - Extract/download SVF model files from APS Model Derivative service.
3. A Web server serves extracted/download SVF model files so that we don't need to override the `Autodesk.Viewing.endpoint.getItemApi` like the blog post [Consume AEC data with SVFs on your own server](https://aps.autodesk.com/blog/consume-aec-data-svfs-your-own-server).

## Requirements

* .net 7.0 or later

<a name="setup"></a>

## Setup

1. Download this repo anywhere you want
2. Execute 'dotnet restore', this command will download and install the required node modules automatically for you. <br />

   ```bash
   dotnet restore
   ```
3. Run the server <br />

   ```bash
   dotnet build
   dotnet run
   ```

<a name="UseOfTheSample"></a>

## Use of the sample

<details>
   <summary>Configure viewer to load online models through proxy service</summary>

   1. Configure viewer endpoint
   2. Initialize your viewer app in this way:

      ```JavaScript
      const options = {
         env: 'AutodeskProduction',
         accessToken: 'eyJhbGciOiJIUzI1NiIsImtpZCI6Imp3dF9zeW1tZXRyaWNfa2V5X2RldiJ9.eyJjbGllbnRfaWQiOiJjWTFqcm1rQXhPSVptbnNsOVhYN0puVURtVEVETGNGeCIsImV4cCI6MTQ4NzU2NzgwMSwic2NvcGUiOlsiZGF0YTpyZWFkIl0sImF1ZCI6Imh0dHBzOi8vYXV0b2Rlc2suY29tL2F1ZC9qd3RleHAzMCIsImp0aSI6InJZcEZZTURyemtMOWZ1ZFdKSVVlVkxucGNWT29BTDg0dFpKbXlmZ29ORW1MakF0YVVtWktRWU1lYUR2UGlnNGsifQ.uzNexXCeu4efGPKGGhHdKxoJDXHAzLb28B2nSjrq_ys' //!<<< Pass a expired token to avoid initializing auth issue on the APS Viewer v7.x
      };

      Autodesk.Viewing.Initializer( options, () => {
         // Change derivative endpoint to Proxy endpoint
         Autodesk.Viewing.endpoint.setEndpointAndApi('http://127.0.0.1:5000/api/proxy', 'derivativeV2');

         Autodesk.Viewing.Document.load(documentId, onDocumentLoadSuccess, onDocumentLoadFailure);
      });
      ```

      **Note.** To use this with enterprise WebProxy, please follow the instructions of [Route At Startup with Custom HttpClientHandler](https://github.com/twitchax/AspNetCore.Proxy/tree/333a0115ed7aec4dde7679c97bfb9e0593a0aeca#route-at-startup-with-custom-httpclienthandler) from [AspNetCore.Proxy](https://github.com/twitchax/AspNetCore.Proxy) to add a custom `HttpClientHandler` with proxy settings and specify custom http client name in [Autodesk.Aps/Controllers/ProxyController.cs](Autodesk.Aps/Controllers/ProxyController.cs#L47) by using `HttpProxyOptionsBuilder.Instance.WithHttpClientName(...)`. For example,

      ```Csharp
      // In ConfigureServices function of Startup.cs
      services.AddHttpClient("MyEnterpriseWebProxyClient")
         .ConfigurePrimaryHttpMessageHandler(() => {
            var webProxyHandler = new HttpClientHandler() {
               Proxy = new WebProxy("http://proxyerverurl:8099"),
               //...
            };

            return webProxyHandler;
         });

      // In constructor of ProxyController.cs
      this.httpProxyOptions = HttpProxyOptionsBuilder.Instance
                              .WithHttpClientName("MyEnterpriseWebProxyClient") //!<<< Add this line
                              .WithBeforeSend( ... )
                              .WithHandleFailure( ... )
                              .build();
      ```
</details>

<details>
   <summary>Extract file list from composite Revit Cloud Workshariong model, which will be downloaded as a ZIP package</summary>
   
   - Call Web API of this sample like this way
   - Note. The composite Revit file's objectId is `urn:adsk.objects:os.object:wip.dm.prod/977d69b1-43e7-40fa-8ece-6ec4602892f3.rvt`, and then encode it as URL-safe string: `urn%3Aadsk.objects%3Aos.object%3Awip.dm.prod%2F977d69b1-43e7-40fa-8ece-6ec4602892f3.rvt`.

      ```bash
      curl --location 'http://127.0.0.1:5000/api/extract/urn%3Aadsk.objects%3Aos.object%3Awip.dm.prod%2F977d69b1-43e7-40fa-8ece-6ec4602892f3.rvt/objects:list'
      ```

      Afterward, we can get the API response for the ZIP file content like the below:
      ```json
      [
         {
            "hasCrc": true,
            "isCrypted": false,
            "isUnicodeText": false,
            "flags": 0,
            "zipFileIndex": 0,
            "offset": 0,
            "externalFileAttributes": 0,
            "versionMadeBy": 20,
            "isDOSEntry": true,
            "hostSystem": 0,
            "version": 20,
            "canDecompress": true,
            "localHeaderRequiresZip64": false,
            "centralHeaderRequiresZip64": false,
            "dateTime": "2023-09-21T00:00:00+08:00",
            "name": "Link.rvt",
            "size": 5406720,
            "compressedSize": 5407550,
            "crc": 678220089,
            "compressionMethod": 8,
            "extraData": null,
            "aesKeySize": 0,
            "comment": null,
            "isDirectory": false,
            "isFile": true
         },
         {
            "hasCrc": true,
            "isCrypted": false,
            "isUnicodeText": false,
            "flags": 0,
            "zipFileIndex": 1,
            "offset": 5407588,
            "externalFileAttributes": 0,
            "versionMadeBy": 20,
            "isDOSEntry": true,
            "hostSystem": 0,
            "version": 20,
            "canDecompress": true,
            "localHeaderRequiresZip64": false,
            "centralHeaderRequiresZip64": false,
            "dateTime": "2023-09-21T00:00:00+08:00",
            "name": "Host.rvt",
            "size": 5398528,
            "compressedSize": 5399353,
            "crc": 3893408371,
            "compressionMethod": 8,
            "extraData": null,
            "aesKeySize": 0,
            "comment": null,
            "isDirectory": false,
            "isFile": true
         }
      ]
      ```
</details>

<details>
   <summary>Extract a file from composite Revit Cloud Workshariong model, which will be downloaded as a ZIP package</summary>
   
   - Call Web API of this sample like this way
   - Note 1. The composite Revit file's objectId is `urn:adsk.objects:os.object:wip.dm.prod/977d69b1-43e7-40fa-8ece-6ec4602892f3.rvt`, and then encode it as URL-safe string: `urn%3Aadsk.objects%3Aos.object%3Awip.dm.prod%2F977d69b1-43e7-40fa-8ece-6ec4602892f3.rvt`.

      ```bash
      curl --location 'http://127.0.0.1:5000/api/extract/urn%3Aadsk.objects%3Aos.object%3Awip.dm.prod%2F977d69b1-43e7-40fa-8ece-6ec4602892f3.rvt/objects' \
            --header 'Content-Type: application/json' \
            --data '{
               "name": "Host.rvt",        //!<<< the name of the target file from the response of extracting file list above
               "size": 5398528,           //!<<< the actual file size of the target from the response of extracting file list above
               "compressedSize": 5399353, //!<<< the compressed file size in the zip of the target from the response of extracting file list above
               "offset": 5407588          //!<<< the offset in zip central header of the target from the response of extracting file list above
            }'
      ```

      Afterward, it will extract and return the file from the zip.

      What if you want to upload the file to Docs after extrating it from the composite design, the API call will become the below:

      ```bash
      curl --location 'http://127.0.0.1:5000/api/extract/urn%3Aadsk.objects%3Aos.object%3Awip.dm.prod%2F977d69b1-43e7-40fa-8ece-6ec4602892f3.rvt/objects?uploadToDocs=true&renameConflict=true&projectId=b.2efa3b98-b895-4380-a820-2b638edd50eaf&folderUrn=urn:adsk.wipprod:fs.folder:co.mgS-lb-BThaTdHnhiN_mbA' \
            --header 'Content-Type: application/json' \
            --data '{
               "name": "Host.rvt",        //!<<< the name of the target file from the response of extracting file list above
               "size": 5398528,           //!<<< the actual file size of the target from the response of extracting file list above
               "compressedSize": 5399353, //!<<< the compressed file size in the zip of the target from the response of extracting file list above
               "offset": 5407588          //!<<< the offset in zip central header of the target from the response of extracting file list above
            }'
      ```
      Notes:

      - **uploadToDocs**: True to tell service to do file upload to BIM360/ACC Docs after the extration is done.
      - **projectId:** The id of BIM360/ACC project you want to upload the extracted file.
      - **folderUrn:** The folder id/urn of BIM360/ACC project you want to upload the extracted file.
      - **renameConflict**: If the upload folder is the same as the original composite Revit Cloud Workshariong model does, this is fatal to set `renameConflict=true`. Otherwise, the uploaded fille will be appended to the original composite Revit Cloud Workshariong model, which will cases the data corruption.
         

</details>

<details>
   <summary>Extract/download SVF model files from APS Model Derivative service.</summary>
   
   - Call Web API of this sample like this way, and it will create a ZIP containing all SVF files for the given URN.

      ```bash
      curl --location 'http://127.0.0.1:5000/api/extract/dXJuOmFkc2sub2JqZWN0czpvcy5vYmplY3Q6c2FuZGJveC9yYWNfYmFzaWNfc2FtcGxlX3Byb2plY3QucnZ0/derivatives'

      // To prevent the extracted result being deleted from bubbles folder after downloading the ZIP containing extracted SVF files
      // curl --location 'http://127.0.0.1:5000/api/extract/dXJuOmFkc2sub2JqZWN0czpvcy5vYmplY3Q6c2FuZGJveC9yYWNfYmFzaWNfc2FtcGxlX3Byb2plY3QucnZ0/derivatives?deleteAfterDownload=false'
      ```
</details>

<details>
   <summary>Configure viewer to load extracted/download SVF models from web server</summary>

   1. Ensure extracted/download SVF model files are put under the `bubbles` folder aside assemblies (DLL) of this project
   2. Configure viewer endpoint
   3. Initialize your viewer app in this way:

      ```JavaScript
      const options = {
         env: 'AutodeskProduction',
         accessToken: 'eyJhbGciOiJIUzI1NiIsImtpZCI6Imp3dF9zeW1tZXRyaWNfa2V5X2RldiJ9.eyJjbGllbnRfaWQiOiJjWTFqcm1rQXhPSVptbnNsOVhYN0puVURtVEVETGNGeCIsImV4cCI6MTQ4NzU2NzgwMSwic2NvcGUiOlsiZGF0YTpyZWFkIl0sImF1ZCI6Imh0dHBzOi8vYXV0b2Rlc2suY29tL2F1ZC9qd3RleHAzMCIsImp0aSI6InJZcEZZTURyemtMOWZ1ZFdKSVVlVkxucGNWT29BTDg0dFpKbXlmZ29ORW1MakF0YVVtWktRWU1lYUR2UGlnNGsifQ.uzNexXCeu4efGPKGGhHdKxoJDXHAzLb28B2nSjrq_ys' //!<<< Pass a expired token to avoid initializing auth issue on the APS Viewer v7.x
      };

      Autodesk.Viewing.Initializer( options, () => {
         // Change derivative endpoint to Proxy endpoint
         Autodesk.Viewing.endpoint.setEndpointAndApi('http://127.0.0.1:5000/api/bubbles', 'derivativeV2');
         //Autodesk.Viewing.endpoint.setEndpointAndApi('http://127.0.0.1:5000/api/bubbles', 'modelDerivativeV2');

         Autodesk.Viewing.Document.load(documentId, onDocumentLoadSuccess, onDocumentLoadFailure);
      });
      ```
</details>

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.

## Written by

Eason Kang [@yiskang](https://twitter.com/yiskang), [Developer Advocacy and Support](http://aps.autodesk.com)
