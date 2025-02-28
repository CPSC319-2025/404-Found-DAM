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
# MySQL Docker container settings
MYSQL_ROOT_PASSWORD="YourRootPass"
MYSQL_DATABASE="MyProjectDB"
MYSQL_USER="MyUser"
MYSQL_PASSWORD="MyUser@Passw0rd"
CONTAINER_NAME="mysql_dev"
MYSQL_PORT=3306
MYSQL_IMAGE="mysql:8.0.33"  # Using MySQL 8.0.33

# Azure Storage connection string (if applicable)
export AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=yourAccountKey;EndpointSuffix=core.windows.net"
export AZURE_STORAGE_CONTAINER="dam-assets-container"
export ENVIRONMENT="Production"

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
echo "Setting up MySQL container '${CONTAINER_NAME}'..."
if [ "$(docker ps -a -q -f name=${CONTAINER_NAME})" ]; then
    echo "Removing existing container named ${CONTAINER_NAME}..."
    docker rm -f ${CONTAINER_NAME}
fi

echo "Starting MySQL container '${CONTAINER_NAME}' with image ${MYSQL_IMAGE}..."
docker run --name ${CONTAINER_NAME} \
  -e MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD} \
  -e MYSQL_DATABASE=${MYSQL_DATABASE} \
  -e MYSQL_USER=${MYSQL_USER} \
  -e MYSQL_PASSWORD=${MYSQL_PASSWORD} \
  -p ${MYSQL_PORT}:3306 \
  -d ${MYSQL_IMAGE}

echo "Waiting for MySQL to initialize (this may take a few seconds)..."
sleep 30

echo "MySQL container '${CONTAINER_NAME}' is now running."
echo "Connection details:"
echo "  Host: localhost"
echo "  Port: ${MYSQL_PORT}"
echo "  Database: ${MYSQL_DATABASE}"
echo "  User: ${MYSQL_USER}"
echo "  Password: ${MYSQL_PASSWORD}"

#############################################
# Create .NET Backend Project
#############################################
BACKEND_DIR="dotnet-backend"
if [ ! -d "${BACKEND_DIR}" ]; then
    echo "Creating .NET backend project in '${BACKEND_DIR}'..."
    mkdir "${BACKEND_DIR}"
    pushd "${BACKEND_DIR}" > /dev/null
    dotnet new webapi --no-https -o .
    # Create appsettings.json with MySQL connection string
    cat > appsettings.json <<EOF
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
EOF
    popd > /dev/null
    echo ".NET backend project created."
else
    echo ".NET backend directory '${BACKEND_DIR}' already exists; skipping creation."
fi

#############################################
# Check node version before creating React Frontend Project
#############################################
# FRONTEND_DIR="react-frontend"
# if [ ! -d "${FRONTEND_DIR}" ]; then
#     echo "Creating React frontend project in '${FRONTEND_DIR}'..."
#     mkdir "${FRONTEND_DIR}"
#     pushd "${FRONTEND_DIR}" > /dev/null
#     # Use create-react-app to scaffold the project
#     npx create-react-app . --template cra-template
#     popd > /dev/null
#     echo "React frontend project created."
# else
#     echo "React frontend directory '${FRONTEND_DIR}' already exists; skipping creation."
# fi

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


#############################################
# Final Message
#############################################
echo "--------------------------------------------------"
echo "Setup complete!"
echo "MySQL container '${CONTAINER_NAME}' is running."
echo ".NET backend project is in the '${BACKEND_DIR}' directory."
echo "React frontend project is in the '${FRONTEND_DIR}' directory."
echo "Remember to update your backend code to connect to MySQL using the connection string in appsettings.json."
echo "--------------------------------------------------"

# dotnet ef migrations add InitialCreate --project ./dotnet-backend/Infrastructure --startup-project ./dotnet-backend/APIs
docker run --platform linux/amd64 -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=LetsGoTeam!' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2019-latest
dotnet ef database update --project ./dotnet-backend/Infrastructure --startup-project ./dotnet-backend/APIs
