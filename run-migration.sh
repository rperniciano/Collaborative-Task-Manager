#!/bin/bash

# Run the DbMigrator to apply migrations
cd "$(dirname "$0")/src/CollaborativeTaskManager.DbMigrator"
dotnet run
