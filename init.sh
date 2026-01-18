#!/bin/bash

# Start the .NET backend server
echo "Starting CollaborativeTaskManager backend..."

# Navigate to the HttpApi.Host directory and run dotnet
cd "$(dirname "$0")/src/CollaborativeTaskManager.HttpApi.Host"
dotnet run &

echo "Backend server starting on https://localhost:44396"
echo "Frontend is available at http://localhost:4200"

# Wait a moment for the server to start
sleep 10
echo "Servers should be ready now."
