#!/bin/bash
     
set -e
     
# Check if Docker daemon is running
if ! docker info > /dev/null 2>&1; then
  echo "Docker daemon is not running. Please start Docker first."
  exit 1
fi

# Check if container is already running
CONTAINER_NAME="mssql-server"
if ! docker ps | grep -q $CONTAINER_NAME; then
  echo "MSSQL container is not running. Starting it now..."
  docker run --platform linux/amd64 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=LetsGoTeam!' -p 1433:1433 -d --name $CONTAINER_NAME mcr.microsoft.com/mssql/server:2019-latest
else
  echo "MSSQL container is already running."
fi
     
dotnet ef database update --project ./dotnet-backend/Infrastructure --startup-project ./dotnet-backend/APIs
     
dotnet run --project ./dotnet-backend/APIs --seed