if [ ! -f "./dotnet-backend/APIs/appsettings.json" ]; then
  echo "❌ Missing dotnet-backend/APIs/appsettings.json"
  exit 1
fi

try {
    docker info > $null
} catch {
    Write-Host "❌ Docker daemon is not running. Please start Docker first."
    exit 1
}

$CONTAINER_NAME = "mssql-server"
$containerRunning = docker ps | Select-String -Pattern $CONTAINER_NAME

$containerStarted = $false

if (-not $containerRunning) {
    Write-Host "MSSQL container is not running. Starting it now..."
    docker run --platform linux/amd64 -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=LetsGoTeam!" -p 1433:1433 -d --name $CONTAINER_NAME mcr.microsoft.com/mssql/server:2019-latest
    $containerStarted = $true
} else {
    Write-Host "MSSQL container is already running."
}

Write-Host "Applying EF migrations..."

dotnet ef database update --project .\dotnet-backend\Infrastructure --startup-project .\dotnet-backend\APIs

if ($containerStarted) {
    Write-Host "Running backend with seed"
    dotnet run --project .\dotnet-backend\APIs --seed
} else {
    Write-Host "Database already up. Running backend without seed"
    dotnet run --project .\dotnet-backend\APIs
}
