#!/bin/bash
set -e

docker run --platform linux/amd64 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=LetsGoTeam!' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest
dotnet ef database update --project ./dotnet-backend/Infrastructure --startup-project ./dotnet-backend/APIs
dotnet run --project ./dotnet-backend/APIs --seed
