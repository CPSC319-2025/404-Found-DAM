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
1. ./run.sh (only the first time)
2. press the play button in VSCode
3. go to localhost:5155

To run frontend:
0. Run this in a different terminal than the backend is running
1. Go to frontend folder
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
