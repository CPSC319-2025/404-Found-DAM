## Backend

### Requirements
- .NET 8.0/9.0
- Entity Framework Core (v9.0.2)
- ASP.NET Core (v8.0)

### Required packages
- See Directory.Packages.props file

### Dependencies
These dependencies are required for the application to run and can be found in the Directory.Packages.props file along with their respective versions:
- Azure.Storage.Blobs (12.24.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
- Microsoft.AspNetCore.Http (2.3.0)
- Microsoft.AspNetCore.Http.Features (5.0.17)
- Microsoft.AspNetCore.Identity (2.3.1)
- Microsoft.AspNetCore.Mvc.Testing (8.0.13)
- Microsoft.AspNetCore.OpenApi (8.0.13)
- Microsoft.EntityFrameworkCore (9.0.2)
- Microsoft.EntityFrameworkCore.Design (9.0.2)
- Microsoft.EntityFrameworkCore.SqlServer (9.0.2)
- Microsoft.Extensions.Hosting (9.0.3)
- Newtonsoft.Json (13.0.3)
- NetVips (3.0.0)
- NetVips.Native.linux-x64 (8.16.1)
- NetVips.Native.osx-arm64 (8.16.1)
- NetVips.Native.win-x64 (8.16.0)
- NetVips.Native.osx-x64 (8.16.0)
- SixLabors.ImageSharp (3.1.7)
- CoenM.ImageSharp.ImageHash (1.3.6)
- Swashbuckle.AspNetCore (6.6.2)
- System.Text.Json (9.0.3)
- ZstdSharp.Port (0.7.3)
- ClosedXML (0.104.2)

### Dev Dependencies
These dependencies are used during development and testing and can also be found in the Directory.Packages.props file along with their respective versions:
- Microsoft.NET.Test.Sdk (17.6.0)
- xunit (2.4.2)
- xunit.runner.visualstudio (2.4.5)
- coverlet.collector (6.0.0)
- NSubstitute (5.3.0)

## Backend Installation Documentation (Dev Environment)

First you must create an appsettings.json file in the current directory:
- for MAC
  ```bash
  touch APIs/appsettings.json
  ```
- for Windows
  ```
  New-Item -Path APIs/appsettings.json -ItemType File
  ```

Then copy the contents from the file provided in the final submission folder: appsettings.json.dev

To install and run the .NET API as well as the MySQL database for local development run:

- To ensure Docker is installed and running:
```bash
docker ps
```

- for MAC
  ```bash
  ./run-backend.sh
  ```

- for Windows
  ```bash
  .\run-backend.ps1
  ```

The .NET API should be running and reachable by http://localhost:5155. The MySQL database should also be running and reachable through Docker.

### Resetting local database and API.
To stop the .NET api simply type <ctrc-c> in the running window. To reset your local database run:
```bash
docker ps
```

Then search for the respective container id then run:
```bash
docker rm -f <container-id>
```

Now you can run either run-backend.sh or run-backend.ps1 depending on your host OS as mentioned above.
