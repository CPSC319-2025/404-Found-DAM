## Backend

### Requirements
- .NET Core (v8.0)
- Azure SDK (v1.1.0): for accessing Azure services such as Blob Storage.
- Azure Entity Framework Core (v9.0.1): ORM for interactions with Azure Database
- Azure Database for MySQL (v8.0)
- Docker (v27.5)

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
