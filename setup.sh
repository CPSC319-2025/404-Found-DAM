#!/bin/bash
# setup.sh - Script to set up a local MySQL container for development

# TODOLIST:
# 1. Add react instalation
# 2. add .NET installation
# 3. connect .NET to database

# --- Configuration Variables ---
MYSQL_ROOT_PASSWORD="YourRootPass"
MYSQL_DATABASE="MyProjectDB"
MYSQL_USER="MyUser"
MYSQL_PASSWORD="MyUser@Passw0rd"
CONTAINER_NAME="mysql_dev"
MYSQL_PORT=3306

# --- Check for Docker ---
if ! command -v docker &> /dev/null; then
    echo "Error: Docker is not installed. Please install Docker first."
    exit 1
fi

# --- Remove any existing container with the same name (optional) ---
if [ "$(docker ps -a -q -f name=${CONTAINER_NAME})" ]; then
    echo "Removing existing container named ${CONTAINER_NAME}..."
    docker rm -f ${CONTAINER_NAME}
fi

# --- Run the MySQL Container ---
echo "Starting MySQL container '${CONTAINER_NAME}'..."
docker run --name ${CONTAINER_NAME} \
  -e MYSQL_ROOT_PASSWORD=${MYSQL_ROOT_PASSWORD} \
  -e MYSQL_DATABASE=${MYSQL_DATABASE} \
  -e MYSQL_USER=${MYSQL_USER} \
  -e MYSQL_PASSWORD=${MYSQL_PASSWORD} \
  -p ${MYSQL_PORT}:3306 \
  -d mysql:latest

# --- Wait for MySQL to Initialize ---
echo "Waiting for MySQL to initialize (this may take a few seconds)..."
# Adjust sleep time if needed based on your system's performance.
sleep 30

# --- Output Connection Information ---
echo "MySQL container '${CONTAINER_NAME}' is now running."
echo "Connection details:"
echo "  Host: localhost"
echo "  Port: ${MYSQL_PORT}"
echo "  Database: ${MYSQL_DATABASE}"
echo "  User: ${MYSQL_USER}"
echo "  Password: ${MYSQL_PASSWORD}"

# --- Next Steps ---
echo "You can now configure your .NET application's connection string to use these credentials."
echo "For example, in appsettings.json:"
echo '{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=MyProjectDB;User=MyUser;Password=MyUser@Passw0rd;"
  }
}'
