# Setup Script for Windows 10 using PowerShell
# May need to run "Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process" to run only for the current PowerShell session
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
$DOTNET_VERSION = "8.0.406"
$NODE_VERSION = "18.16.0"

Write-Host "Setting environment variables and configuration..."
Write-Host "MySQL container will run on port ${MYSQL_PORT}"
Write-Host "Using .NET SDK version ${DOTNET_VERSION} and Node.js version ${NODE_VERSION}"

# #############################################
# # Check if Docker is Installed
# #############################################
Write-Host "Checking for Docker installation..."
if (-Not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Docker is not installed. Please install Docker for Windows and re-run this script."
    exit 1
}

# #############################################
# # Start/Restart MySQL Docker Container
# #############################################
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
# Check if .NET is installed
Write-Host "Checking if .NET SDK is installed..."

$isDotnetInstalled = Get-Command dotnet -ErrorAction SilentlyContinue

if (-Not $isDotnetInstalled) {
    # Try to Install .NET SDK using winget
    try {
        Write-Host ".NET SDK is not installed. Installing .NET SDK version '${DOTNET_VERSION}'..."
        winget install --id Microsoft.DotNet.SDK.8 --version $DOTNET_VERSION -e --source winget
        
        # Check if the installation was successful
        $isDotnetInstalled = Get-Command dotnet -ErrorAction SilentlyContinue

        if ($isDotnetInstalled) {
            Write-Host ".NET SDK version '${DOTNET_VERSION}' installation successful."
        } else {
            Write-Host "Installation failed. Please check for errors."
            exit
        }
    } catch {
        Write-Host "An error occurred during installation: $_"
    }
} else {
        # Verify if the installed datnet version matches the expected one.
        $required_dotnet = $DOTNET_VERSION
        $installed_dotnet = dotnet --version
        if ($installed_dotnet -ne $required_dotnet) {
            Write-Host "Warning: Your dotnet version is $installed_dotnet. This project requires dotnet v$required_dotnet."
            exit
        } else {
            Write-Host "The expected version $installed_dotnet of .NET was already installed."
        }

        # If error message like "The command could not be loaded, possibly because:..." pops up,
        # try to delete the dotnet.exe and run the script again.
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
# Create .NET Backend Solution
#############################################
$BACKEND_DIR = "dotnet-backend"
$SolutionName = "AssetManagement"

if (-Not (Test-Path $BACKEND_DIR)) {
    # Create Solution
    Write-Host "Creating .NET backend solution AssetManagement in '${BACKEND_DIR}'..."
    New-Item -ItemType Directory -Force -Path $BACKEND_DIR
    dotnet new sln --name $SolutionName -o $BACKEND_DIR 
    
    # Create Class Library 'APIs'
    Write-Host "Creating Class Library 'APIs' and add it to the solution..."
    $APIsProjectPath = Join-Path $BACKEND_DIR "APIs"
    $AppsettingsPath = Join-Path $APIsProjectPath "appsettings.json"
    dotnet new webapi --no-https -o $APIsProjectPath -n APIs
    dotnet sln $BACKEND_DIR add $APIsProjectPath\APIs.csproj

    
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
    $appsettings | Set-Content -Path $AppsettingsPath -Encoding UTF8

    # Create Class Library 'Core' and internal folders
    Write-Host "Creating Class Library 'Core' and add it to the solution..."
    $CoreProjectPath = Join-Path $BACKEND_DIR "Core"
    dotnet new classlib -o $CoreProjectPath -n Core
    dotnet sln $BACKEND_DIR add $CoreProjectPath\Core.csproj

    $directories = @("Entities", "Interfaces", "Services")
    foreach ($dir in $directories) {
        Write-Host "Creating directory '$dir' inside Class Library 'Core'..."
        $DirPath = Join-Path $CoreProjectPath $dir
        New-Item -ItemType Directory -Force -Path $DirPath
    
        Write-Host "Creating dummy.cs file in '$dir'..."
        $DummyFilePath = Join-Path $DirPath "dummy$dir.cs"
        New-Item -ItemType File -Path $DummyFilePath -Force  
        if ($dir -eq "Interfaces") {
            $DummyFilePath = Join-Path $DirPath "IDummyRepository.cs"
            New-Item -ItemType File -Path $DummyFilePath -Force 
        } 
    }

    # Create Class Library 'Infrastructure' and internal folders
    Write-Host "Creating Class Library 'Infrastructure' and add it to the solution..."
    $InfrastructureProjectPath = Join-Path $BACKEND_DIR "Infrastructure"
    dotnet new classlib -o $InfrastructureProjectPath -n Infrastructure
    dotnet sln $BACKEND_DIR add $InfrastructureProjectPath\Infrastructure.csproj

    $directories = @("DataAccess")
    foreach ($dir in $directories) {
        Write-Host "Creating directory '$dir' inside Class Library 'Infrastructure'..."
        $DirPath = Join-Path $InfrastructureProjectPath $dir
        New-Item -ItemType Directory -Force -Path $DirPath
    
        Write-Host "Creating dummy cs file in '$dir'..."
        $DummyFilePath = Join-Path $DirPath "dummyDbContext.cs"
        New-Item -ItemType File -Path $DummyFilePath -Force
        $DummyFilePath = Join-Path $DirPath "dummyRepository.cs"
        New-Item -ItemType File -Path $DummyFilePath -Force
    }


    # Create Class Library 'Tests'
    Write-Host "Creating Class Library 'Tests' and add it to the solution..."
    $TestsProjectPath = Join-Path $BACKEND_DIR "Tests"
    dotnet new classlib -o $TestsProjectPath -n Tests
    dotnet sln $BACKEND_DIR add $TestsProjectPath\Tests.csproj

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

dotnet tool install --global dotnet-ef
& docker run --platform linux/amd64 -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=LetsGoTeam!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest
dotnet ef database update --project ./dotnet-backend/Infrastructure --startup-project ./dotnet-backend/APIs
