#!/bin/bash

# Start the .NET backend server directly
echo "Starting CollaborativeTaskManager backend..."

# Navigate to the HttpApi.Host directory and run dotnet
PROJECT_DIR="D:/Programmi/PORTFOLIO/Collaborative Task Manager/src/CollaborativeTaskManager.HttpApi.Host"

dotnet run --project "$PROJECT_DIR/CollaborativeTaskManager.HttpApi.Host.csproj"
