
![.NET](https://img.shields.io/badge/.NET-7.0-blue.svg)
![Platforms](https://img.shields.io/badge/platform-windows%20%7C%20osx%20%7C%20linux-lightgray.svg)
[![License](http://img.shields.io/:license-mit-blue.svg)](http://opensource.org/licenses/MIT)

# Autodesk APS Utility Service

## Overview

This sample is demonstrating the following in a set of Web API:

1. A proxy server with [AspNetCore.Proxy](https://github.com/twitchax/AspNetCore.Proxy) to forward all requests from the APS Viewer to this backend without showing access token in your viewer frontend for viewing SVF model (not SVF2).
2. A utility service of the below by single API call
   - Extract file list from composite [Revit Cloud Workshariong model](https://aps.autodesk.com/blog/make-composite-revit-design-work-design-automation-api-revit) that `attributes.extension.isCompositeDesign` is true in the response of Data Management API. (This idea is from https://github.com/wallabyway/bim360-zip-extract)
   - Extract/download SVF model files from APS Model Derivative service.

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
   <summary>Configure viewer to load models from proxy</summary>

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
            "comment": "",
            "compressedSize": 5407550,
            "crc32": 678220000,
            "externalAttributes": 0,
            "fullName": "Link.rvt",
            "lastWriteTime": "2023-09-21T00:00:00+08:00",
            "size": 5406720,
            "name": "Link.rvt"
         },
         {
            "comment": "",
            "compressedSize": 5399353,
            "crc32": 3893408300,
            "externalAttributes": 0,
            "fullName": "Host.rvt",
            "lastWriteTime": "2023-09-21T00:00:00+08:00",
            "size": 5398528,
            "name": "Host.rvt"
         }
      ]
      ```
</details>

<details>
   <summary>Extract/download SVF model files from APS Model Derivative service.</summary>
   
   - Call Web API of this sample like this way, and it will create a ZIP containing all SVF files for the given URN.

      ```bash
      curl --location 'http://127.0.0.1:5000/api/extract/dXJuOmFkc2sub2JqZWN0czpvcy5vYmplY3Q6c2FuZGJveC9yYWNfYmFzaWNfc2FtcGxlX3Byb2plY3QucnZ0/derivaitves'
      ```
</details>

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.

## Written by

Eason Kang [@yiskang](https://twitter.com/yiskang), [Developer Advocacy and Support](http://aps.autodesk.com)
