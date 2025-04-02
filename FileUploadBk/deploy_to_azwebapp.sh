#!/bin/bash

# Exit immediately if a command fails
set -e

# Remove previous publish directories/files
rm -rf publish*

echo "Starting .NET publish..."
# Publish .NET application
dotnet publish -o ./publish

echo "Zipping published files..."
# Create a ZIP archive of the published output
zip -r publish.zip ./publish

echo "Deploying to Azure Web App..."
# Deploy the ZIP file to Azure Web App
az webapp deploy --resource-group appsvc_windows_centralus \
                 --name fileUploadappservice01001 \
                 --src-path ./publish.zip \
                 --type zip

echo "Deployment completed successfully!"
