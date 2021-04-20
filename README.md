
![.NET](https://img.shields.io/badge/.NET%20Standard-2.1-blue.svg)
![.NET](https://img.shields.io/badge/.NET%20Core-3.1-blue.svg)
![Platforms](https://img.shields.io/badge/platform-windows%20%7C%20osx%20%7C%20linux-lightgray.svg)
[![License](http://img.shields.io/:license-mit-blue.svg)](http://opensource.org/licenses/MIT)

[![oAuth2](https://img.shields.io/badge/oAuth2-v1-green.svg)](http://forge.autodesk.com/)
[![Data-Management](https://img.shields.io/badge/Data%20Management-v1-green.svg)](http://forge.autodesk.com/)
[![Viewer](https://img.shields.io/badge/Viewer-v7-green.svg)](http://forge.autodesk.com/)

![Advanced](https://img.shields.io/badge/Level-Advanced-red.svg)

# Autodesk Forge Proxy Server

## Overview

This sample is demonstrating how to host a proxy server with [AspNetCore.Proxy](https://github.com/twitchax/AspNetCore.Proxy) to forward all requests from the Forge Viewer to this backend without showing access token in your viewer frontend.

## Requirements

* asp.net core 3.1 or later

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
function fetchForgeToken( callback ) {
   fetch( 'http://localhost:5000/api/forge/oauth/token', {
   method: 'get',
   headers: new Headers({ 'Content-Type': 'application/json' })
   })
   .then( ( response ) => {
   if( response.status === 200 ) {
      return response.json();
   } else {
      return Promise.reject(
         new Error( `Failed to fetch token from server (status: ${response.status}, message: ${response.statusText})` )
      );
   }
   })
   .then( ( data ) => {
   if( !data ) return Promise.reject( new Error( 'Empty token response' ) );

   callback( data.access_token, data.expires_in );
   })
   .catch( ( error ) => console.error( error ) );
}

const options = {
   env: 'MD20ProdUS',
   getAccessToken: fetchForgeToken, //!<<< Workaround: Get `viewable:read` access token for SVF2 model loader
};

Autodesk.Viewing.Initializer( options, () => {
  // Change derivative endpoint to Proxy endpoint
  Autodesk.Viewing.endpoint.setEndpointAndApi( 'http://localhost:5000/forge-proxy', 'D3S' );

  Autodesk.Viewing.Document.load(documentId, onDocumentLoadSuccess, onDocumentLoadFailure);
});
```

**Note. 1** See [Autodesk.Forge/wwwroot/index.html](/Autodesk.Forge/wwwroot/index.html) for the full example
**Note. 2**

   1. Replace `documentId` to your own URN before playing with this project.
   2. Enter your client id and client secret in [Autodesk.Forge/appsettings.json](/Autodesk.Forge/appsettings.json)

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.

## Written by

Eason Kang <br />
Forge Partner Development <br />
https://developer.autodesk.com/ <br />
https://forge.autodesk.com/blog <br />