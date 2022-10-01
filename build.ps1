[CmdletBinding()]
Param(
    [Parameter(
            Mandatory=$True,
            HelpMessage = "The image version number")]
    [string] $version,
    
    [Parameter(
            Mandatory=$True,
            HelpMessage = "Build type, must be one of these: mysql, sqlite, sqlserver, postgresql, or all")]
    [ValidateSet("mysql", "sqlite", "sqlserver", "postgresql", "all")]
    [string] $build
)

$currentDir = Get-Location
$Env:OUTPUT = "$PSScriptRoot/bin/output"

if("sqlite" -eq $build -or "all" -eq $build) {
    # Build exporter
    dotnet build -c Release $PSScriptRoot/src/Sqlite.Exporter/Sqlite.Exporter.csproj -o $PSScriptRoot/bin/build
    Set-Location $PSScriptRoot/bin

    # Execute exporter
    dotnet $PSScriptRoot/bin/build/Sqlite.Exporter.dll

    # Copy result
    Copy-Item $PSScriptRoot/bin/output/database.db -Destination "$PSScriptRoot/bin/akka-sqlite.$version.db"
    
    # Clean-up
    Remove-Item -Recurse -Force $PSScriptRoot/bin/build
    Remove-Item -Recurse -Force $PSScriptRoot/bin/output
}

if("mysql" -eq $build -or "all" -eq $build) {
    # Build exporter
    dotnet build -c Release $PSScriptRoot/src/MySql.Exporter/MySql.Exporter.csproj -o $PSScriptRoot/bin/build
    Set-Location $PSScriptRoot/bin
    
    # Execute exporter
    dotnet $PSScriptRoot/bin/build/MySql.Exporter.dll
    
    # Build docker image
    Copy-Item $PSScriptRoot/src/MySql.Exporter/Dockerfile -Destination $PSScriptRoot/bin
    docker build -t akka-mysql:$version .
    
    # Clean-up
    Remove-Item -Recurse -Force $PSScriptRoot/bin/build
    Remove-Item -Recurse -Force $PSScriptRoot/bin/output
    Remove-Item $PSScriptRoot/bin/Dockerfile
}

if("sqlserver" -eq $build -or "all" -eq $build) {
    # Build exporter
    dotnet build -c Release $PSScriptRoot/src/SqlServer.Exporter/SqlServer.Exporter.csproj -o $PSScriptRoot/bin/build
    Set-Location $PSScriptRoot/bin

    # Execute exporter
    dotnet $PSScriptRoot/bin/build/SqlServer.Exporter.dll

    # Build docker image
    Copy-Item $PSScriptRoot/src/SqlServer.Exporter/Dockerfile -Destination $PSScriptRoot/bin
    docker build -t akka-mysql:$version .

    # Clean-up
    Remove-Item -Recurse -Force $PSScriptRoot/bin/build
    Remove-Item -Recurse -Force $PSScriptRoot/bin/output
    Remove-Item $PSScriptRoot/bin/Dockerfile
}

Set-Location $currentDir