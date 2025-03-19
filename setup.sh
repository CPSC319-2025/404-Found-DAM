#!/bin/bash
set -e

# --- Ensure the script runs on macOS ---
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "This setup script is only intended for macOS (Darwin). Exiting."
    exit 1
fi

#############################################
# Configuration Variables and Versions
#############################################

# Versions for .NET and Node.js (for React)
DOTNET_VERSION="8.0.100"   # Expected .NET SDK version
NODE_VERSION="18.16.0"     # Expected Node.js version

echo "Setting environment variables and configuration..."
echo "MySQL container will run on port ${MYSQL_PORT}"
echo "Using .NET SDK version ${DOTNET_VERSION} and Node.js version ${NODE_VERSION}"

#############################################
# Ensure Homebrew is Installed
#############################################
if ! command -v brew &> /dev/null; then
    echo "Error: Homebrew is required but not installed. Please install Homebrew from https://brew.sh/ and re-run this script."
    exit 1
fi

#############################################
# Install .NET SDK via Homebrew (if not installed)
#############################################
if ! command -v dotnet &> /dev/null; then
    echo "Installing .NET SDK (expected version ${DOTNET_VERSION}) via Homebrew..."
    brew install dotnet@8
    echo "export PATH=\"/opt/homebrew/opt/dotnet@8/bin:\$PATH\"" >> ~/.bash_profile
    # source ~/.bash_profile
else
    echo ".NET SDK is already installed: $(dotnet --version)"
fi


#############################################
# Install Node.js via Homebrew (if not installed)
#############################################
if ! command -v node &> /dev/null; then
    echo "Installing Node.js (expected version ${NODE_VERSION}) via Homebrew..."
    brew install node
else
    echo "Node.js is already installed: $(node --version)"
fi

#############################################
# Check for Docker Installation
#############################################
if ! command -v docker &> /dev/null; then
    echo "Error: Docker is not installed. Please install Docker for Mac and re-run this script."
    exit 1
fi

#############################################
# Start/Restart MySQL Docker Container
#############################################

echo "Starting MySQL container"
# Check if container is already running
CONTAINER_NAME="mssql-server"
docker run --platform linux/amd64 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=LetsGoTeam!' -p 1433:1433 -d --name $CONTAINER_NAME mcr.microsoft.com/mssql/server:2019-latest
echo "Waiting for MySQL to initialize (this may take a few seconds)..."
sleep 30

#############################################
# Verify Node and npm Versions for React Frontend
#############################################
echo "Verifying Node and npm versions for the React frontend..."

# Required versions
required_node="18.18.0"
required_npm="9.8.1"

# Get the installed versions (strip the leading "v" from Node's version)
installed_node=$(node --version | sed 's/^v//')
installed_npm=$(npm --version)

# Check Node version
if [ "$installed_node" != "$required_node" ]; then
    echo "Warning: Your Node version is $installed_node. This project requires Node v$required_node."
else
    echo "Node version v$installed_node is as expected."
fi

# Check npm version
if [ "$installed_npm" != "$required_npm" ]; then
    echo "Warning: Your npm version is $installed_npm. This project requires npm v$required_npm."
else
    echo "npm version v$installed_npm is as expected."
fi



echo "Setup ef core tool and update database using the commented out commands commands in setup.sh "
# dotnet tool install --global dotnet-ef
# copy paste the exporting path to bash_profile at the buttom of dotnet tool install --global dotnet-ef
dotnet ef database update --project ./dotnet-backend/Infrastructure --startup-project ./dotnet-backend/APIs


#############################################
# Final Message
#############################################
echo "--------------------------------------------------"
echo "Setup complete!"
echo "Remember to update your backend code to connect to MySQL using the connection string in appsettings.json."
echo "--------------------------------------------------"
