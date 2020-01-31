
![.NET](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)
![Platforms](https://img.shields.io/badge/platform-windows%20%7C%20osx%20%7C%20linux-lightgray.svg)
[![License](http://img.shields.io/:license-mit-blue.svg)](http://opensource.org/licenses/MIT)

# Autodesk Forge Proxy Server

## Overview

This sample is demonstrating how to host a proxy server with [AspNetCore.Proxy](https://github.com/twitchax/AspNetCore.Proxy) to forward all requests from the Forge Viewer to this backend without showing access token in your viewer frontend.

## Requirements

* asp.net core 2.2 or later

<a name="setup"></a>
## Setup

1. Download this repo anywhere you want
3. Execute 'dotnet restore', this command will download and install the required node modules automatically for you. <br />
   ```bash
   dotnet restore
   ```

<a name="UseOfTheSample"></a>
## Use of the sample

Run the server <br />
   ```bash
   dotnet build
   dotnet run
   ```

- Configure viewer endpoint
Initialize your viewer app in this way:

```JavaScript
const options = {
   env: 'AutodeskProduction',
   accessToken: 'eyJhbGciOiJIUzI1NiIsImtpZCI6Imp3dF9zeW1tZXRyaWNfa2V5X2RldiJ9.eyJjbGllbnRfaWQiOiJjWTFqcm1rQXhPSVptbnNsOVhYN0puVURtVEVETGNGeCIsImV4cCI6MTQ4NzU2NzgwMSwic2NvcGUiOlsiZGF0YTpyZWFkIl0sImF1ZCI6Imh0dHBzOi8vYXV0b2Rlc2suY29tL2F1ZC9qd3RleHAzMCIsImp0aSI6InJZcEZZTURyemtMOWZ1ZFdKSVVlVkxucGNWT29BTDg0dFpKbXlmZ29ORW1MakF0YVVtWktRWU1lYUR2UGlnNGsifQ.uzNexXCeu4efGPKGGhHdKxoJDXHAzLb28B2nSjrq_ys' //!<<< Pass a expired token to avoid initializing auth issue on the Forge Viewer v7.x
};

Autodesk.Viewing.Initializer( options, () => {
  // Change derivative endpoint to Proxy endpoint
  Autodesk.Viewing.endpoint.setApiEndpoint( 'http://127.0.0.1:8085/forge-proxy', 'derivativeV2' );

  Autodesk.Viewing.Document.load(documentId, onDocumentLoadSuccess, onDocumentLoadFailure);
});
```

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.

## Written by

Eason Kang <br />
Forge Partner Development <br />
https://developer.autodesk.com/ <br />
https://forge.autodesk.com/blog <br />