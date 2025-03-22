# Check if Docker daemon is running
try {
    docker info > $null
} catch {
    Write-Host "Docker daemon is not running. Please start Docker first."
    exit 1
}

# Check if container is already running
$CONTAINER_NAME = "mssql-server"
$containerRunning = docker ps | Select-String -Pattern $CONTAINER_NAME

if (-not $containerRunning) {
    Write-Host "MSSQL container is not running. Starting it now..."
    docker run --platform linux/amd64 -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=LetsGoTeam!" -p 1433:1433 -d --name $CONTAINER_NAME mcr.microsoft.com/mssql/server:2019-latest
} else {
    Write-Host "MSSQL container is already running."
}

# Run database updates and seed the database
dotnet ef database update --project .\dotnet-backend\Infrastructure --startup-project .\dotnet-backend\APIs
dotnet run --project .\dotnet-backend\APIs

# -- --seed