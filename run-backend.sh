#!/bin/bash
     
set -e

if [ ! -f "./dotnet-backend/APIs/appsettings.json" ]; then
  echo "Missing dotnet-backend/APIs/appsettings.json"
  exit 1
fi
     
if ! docker info > /dev/null 2>&1; then
  echo "Docker daemon is not running. Please start Docker first."
  exit 1
fi

CONTAINER_NAME="mssql-server"
CONTAINER_STARTED=false

if ! docker ps | grep -q $CONTAINER_NAME; then
  echo "MSSQL container is not running. Starting it now..."
  docker run --platform linux/amd64 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=LetsGoTeam!' -p 1433:1433 -d --name $CONTAINER_NAME mcr.microsoft.com/mssql/server:2019-latest
  CONTAINER_STARTED=true
else
  echo "MSSQL container is already running."
fi
sleep 5

echo "Applying EF migrations..."
     
dotnet ef database update --project ./dotnet-backend/Infrastructure --startup-project ./dotnet-backend/APIs
     
if [ "$CONTAINER_STARTED" = true ]; then
  echo "Running backend with seed"
  dotnet run --project ./dotnet-backend/APIs --seed
else
  echo "Database already up. Running backend without seed"
  dotnet run --project ./dotnet-backend/APIs
fi
