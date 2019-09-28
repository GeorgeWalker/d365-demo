@echo off

echo Updating meta data

dotnet run -p ..\OData.OpenAPI\odata2openapi\odata2openapi.csproj

echo Updating client

rem autorest --verbose Readme.md