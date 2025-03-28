### Requirements:
- .NET 8

## Getting Started
Navigate to the `dotnet-backend` directory:
```bash
cd dotnet-backend
```
Execute the following to install packages
```bash
dotnet restore
```

To run the API, navigate to the `APIs` directory and use the following commands to build and start the application:
```bash
cd APIs

dotnet run
```

### Cheatsheet:
- visit 'http://localhost:5155/swagger' to manually try and test.

Every week or so, update your branch with the changes from main. Using GitHub Desktop,
1. Go to your branch.
2. Click "Branch" (in the top bar), then "Update from main"

To run backend:
1. cd to the root folder (404-Found-DAM) and run ./run.sh (only the first time)
2. press the play button in VSCode, if that doesn't work cd to the dotnet-backend/APIs folder and run "dotnet run".
- if that doesn't work, cd to the dotnet-backend/APIs folder and run "dotnet build", then do step 2.
3. go to localhost:5155

If you encounter issues with running the backend
1. manually go to Docker and delete the containers
2. run git pull in DataGrip
3. cd to root folder (404-FOUND-DAM) and run "./run.sh"
--- if you get an error like: An error was generated for warning 'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning': The model for context 'DAMDbContext' has pending changes. Add a new migration before updating the database. See https://aka.ms/efcore-docs-pending-changes. This exception can be suppressed or logged by passing event ID 'RelationalEventId.PendingModelChangesWarning' to the 'ConfigureWarnings' method in 'DbContext.OnConfiguring' or 'AddDbContext'.
--- then:
--------- 1. cd to dotnet-backend/Infrastructure/Migrations and delete the Migrations folder (this folder).
--------- 2. run this command (2a: mac; 2b: windows):
--------- 3. cd to root folder and run "./run.sh"
2a:
dotnet ef migrations add InitialCreate \
  --project Infrastructure \
  --startup-project APIs && \
docker run --platform linux/amd64 -e "ACCEPT_EULA=Y" -e 'SA_PASSWORD=LetsGoTeam!' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest
2b:
dotnet ef migrations add InitialCreate --project ./dotnet-backend/Infrastructure --startup-project ./dotnet-backend/APIs
& docker run --platform linux/amd64 -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=LetsGoTeam!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest

expected message after running 2:
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
71885fdfbe4c0370baa37cdccd4e330509c5c96b33a6fd91e1e52f870027036a

4. cd to dotnet-backend/APIs and run "dotnet build" then run "dotnet run"

To run frontend:
0. Run this in a different terminal than the backend is running
1. Go (cd) to frontend folder
2. npm install (only the first time)
3. npm run dev
4. go to localhost:3000

You can run either the frontend first or the backend first

To setup connection to DataGrip:
1. Choose Microsoft SQL Server and enter the below information
Port:1433
Username: sa 
Password: (see Discord)
Database: DAMDatabase

To interact with the database, write a file for the object in Infrastructure/DataAccess (eg. ActivityLogRepository)

APIs (to receive input from and provide resposnse to frontend) go into the APIs/Controllers folder: ActivityLogController

Logic and any functionality that does not need to access database goes into Service (Core/Services)

FLOW:
- Frontend calls an endpoint (in ActivityLogController).
- Controller calls Service.
- Service calls DataAccess/Repository (if necessary).
- DataAccess/Repository returns to Service
- Service returns to Controller
- Controller returns to frontend.

EXAMPLE FLOW:
- Frontend calls an endpoint (eg. ActivityLogController)
- Controller calls Service (eg. ActivityLogService)
- Service calls DataAccess/Repository (if necessary) (eg. ActivityLogRepository)
- DataAccess/Repository returns to Service
- Service returns to Controller
- Controller returns to frontend

Server is in a blackbox. You don't want people to know what's in your server, except through accessing the APIs

To create/modify an object that will go into the database: Core/Entities/DataModel.cs

Interfaces: go into the Core/Interfaces folder
- IActivityLogRepository
- IActivityLogService

Additionally, we are using DTO, e.g. CreateActivityLogDto.cs


Activity Log mostly done.
