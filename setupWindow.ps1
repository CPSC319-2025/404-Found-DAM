# Setup Script for Windows 10 using PowerShell
$ErrorActionPreference = "Stop"



#############################################
# Configuration Variables and Versions
#############################################
$MYSQL_ROOT_PASSWORD = "YourRootPass"
$MYSQL_DATABASE = "MyProjectDB"
$MYSQL_USER = "MyUser"
$MYSQL_PASSWORD = "MyUser@Passw0rd"
$CONTAINER_NAME = "mysql_dev"
$MYSQL_PORT = 3306
$MYSQL_IMAGE = "mysql:8.0.33"  # Using MySQL 8.0.33

$AZURE_STORAGE_CONNECTION_STRING = "DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=yourAccountKey;EndpointSuffix=core.windows.net"
$AZURE_STORAGE_CONTAINER = "dam-assets-container"
$ENVIRONMENT = "Production"

# .NET and Node.js versions
$DOTNET_VERSION = "7.0.100"
$NODE_VERSION = "18.16.0"

Write-Host "Setting environment variables and configuration..."
Write-Host "MySQL container will run on port ${MYSQL_PORT}"
Write-Host "Using .NET SDK version ${DOTNET_VERSION} and Node.js version ${NODE_VERSION}"

#############################################
# Check if Docker is Installed
#############################################
Write-Host "Checking for Docker installation..."
if (-Not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Docker is not installed. Please install Docker for Windows and re-run this script."
    exit 1
}

#############################################
# Start/Restart MySQL Docker Container
#############################################
Write-Host "Setting up MySQL container '${CONTAINER_NAME}'..."
$existingContainer = docker ps -a -q -f "name=${CONTAINER_NAME}"
if ($existingContainer) {
    Write-Host "Removing existing container named ${CONTAINER_NAME}..."
    docker rm -f $existingContainer
}
  
Write-Host "Starting MySQL container '${CONTAINER_NAME}' with image ${MYSQL_IMAGE}..."
docker run --name $CONTAINER_NAME `
  -e MYSQL_ROOT_PASSWORD=$MYSQL_ROOT_PASSWORD `
  -e MYSQL_DATABASE=$MYSQL_DATABASE `
  -e MYSQL_USER=$MYSQL_USER `
  -e MYSQL_PASSWORD=$MYSQL_PASSWORD `
  -p 3306:3306 `
  -d $MYSQL_IMAGE


Write-Host "Waiting for MySQL to initialize (this may take a few seconds)..."
Start-Sleep -Seconds 30

Write-Host "MySQL container '${CONTAINER_NAME}' is now running."
Write-Host "Connection details:"
Write-Host "  Host: localhost"
Write-Host "  Port: $MYSQL_PORT"
Write-Host "  Database: $MYSQL_DATABASE"
Write-Host "  User: $MYSQL_USER"
Write-Host "  Password: $MYSQL_PASSWORD"

#############################################
# Install .NET SDK (via Winget or direct installer)
#############################################
Write-Host "Checking if .NET SDK is installed..."
if (-Not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Installing .NET SDK (version $DOTNET_VERSION)..."
    winget install --id Microsoft.DotNet.SDK -e --source winget
} else {
    Write-Host ".NET SDK is already installed: $(dotnet --version)"
}

#############################################
# Install Node.js (via Winget or direct installer)
#############################################
Write-Host "Checking if Node.js is installed..."
if (-Not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Node.js (version $NODE_VERSION)..."
    winget install --id OpenJS.NodeJS -e --source winget
} else {
    Write-Host "Node.js is already installed: $(node --version)"
}

#############################################
# Create .NET Backend Project
#############################################
$BACKEND_DIR = "dotnet-backend"
if (-Not (Test-Path $BACKEND_DIR)) {
    Write-Host "Creating .NET backend project in '${BACKEND_DIR}'..."
    New-Item -ItemType Directory -Force -Path $BACKEND_DIR
    Set-Location $BACKEND_DIR
    dotnet new webapi --no-https -o .
    
    # Create appsettings.json with MySQL connection string
    $appsettings = @"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=${MYSQL_PORT};Database=${MYSQL_DATABASE};User=${MYSQL_USER};Password=${MYSQL_PASSWORD};"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
"@
    $appsettings | Set-Content -Path "appsettings.json"
    Set-Location -Path ".."  # Go back to previous directory
    Write-Host ".NET backend project created."
} else {
    Write-Host ".NET backend directory '${BACKEND_DIR}' already exists; skipping creation."
}

#############################################
# Verify Node and npm Versions for React Frontend (if applicable)
#############################################
Write-Host "Verifying Node and npm versions for the React frontend..."

# Required versions
$required_node = "18.18.0"
$required_npm = "9.8.1"

$installed_node = node --version
$installed_npm = npm --version

if ($installed_node -ne "v$required_node") {
    Write-Host "Warning: Your Node version is $installed_node. This project requires Node v$required_node."
} else {
    Write-Host "Node version $installed_node is as expected."
}

if ($installed_npm -ne $required_npm) {
    Write-Host "Warning: Your npm version is $installed_npm. This project requires npm v$required_npm."
} else {
    Write-Host "npm version $installed_npm is as expected."
}

#############################################
# Final Message
#############################################
Write-Host "--------------------------------------------------"
Write-Host "Setup complete!"
Write-Host "MySQL container '${CONTAINER_NAME}' is running."
Write-Host ".NET backend project is in the '${BACKEND_DIR}' directory."
Write-Host "--------------------------------------------------"
