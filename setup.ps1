# Setup Script for Windows 10 using PowerShell
# May need to run "Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process" to run only for the current PowerShell session
$ErrorActionPreference = "Stop"

#############################################
# Configuration Variables and Versions
#############################################
# .NET and Node.js versions
$DOTNET_VERSION = "8.0.406"
$NODE_VERSION = "18.16.0"

Write-Host "Setting environment variables and configuration..."
Write-Host "Using .NET SDK version ${DOTNET_VERSION} and Node.js version ${NODE_VERSION}"

# #############################################
# # Check if Docker is Installed
# #############################################
Write-Host "Checking for Docker installation..."
if (-Not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Docker is not installed. Please install Docker for Windows and re-run this script."
    exit 1
}

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

dotnet tool install --global dotnet-ef
Write-Host "Copy paste the exporting path to bash_profile or zsh_profile at the buttom of dotnet tool install --global dotnet-ef"
