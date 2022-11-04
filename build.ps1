[CmdletBinding()]
Param(
    [Parameter(
            Mandatory=$false,
            HelpMessage = "Build type, must be one of these: 'mysql', 'sqlite', 'sqlserver', 'postgresql', or 'all'")]
    [Alias("t","target")]
    [ValidateSet("mysql", "sqlite", "sqlserver", "postgresql", "all")]
    [string] $BuildType = "all",

    [Parameter(
        Mandatory=$false,
        HelpMessage = "Dotnet build configuration, must be either 'Debug' or 'Release'")]
    [Alias("c")]
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",
    
    [Parameter(
            Mandatory=$false,
            HelpMessage = "Push built docker image to the repository")]
    [Alias("p")]
    [switch] $Push,

    [Parameter(
            Mandatory=$false,
            HelpMessage = "The docker hub repository name")]
    [Alias("r","repo")]
    [string] $DockerRepo = "",

    [Parameter(
            Mandatory=$false,
            HelpMessage = "Also tag the docker image with latest tag")]
    [Alias("l")]
    [switch] $Latest
)

$ErrorActionPreference = 'Stop'

class Builder {
    [string] static $BinFolder
    [string] static $BuildFolder
    [string] static $OutputFolder

    [string] static $Conf
    [string] static $DockerRepo
    [bool] static $Push
    [bool] static $Latest

    [string] $Project
    [string] $ProjectFolder
    [string] $ProjectFile
    [string] $Version
    [string] $DockerName

    Builder([string] $p)
    {
        $this.Project = $p
        $this.ProjectFolder = "$PSScriptRoot/src/$($this.Project).Exporter"
        $this.ProjectFile = "$($this.ProjectFolder)/$($this.Project).Exporter.csproj"
        $this.Version = $this.GetProjectVersion()
        if([Builder]::DockerRepo -eq "") {
            $this.DockerName = "akka-persistence-$($this.Project.ToLowerInvariant())-test-data"
        } else {
            $this.DockerName = "$([Builder]::DockerRepo)/akka-persistence-$($this.Project.ToLowerInvariant())-test-data"
        }
    }

    [void] BuildProject() {
        Write-Host "Building $($this.Project) exporter" -ForegroundColor White
        dotnet build -c $([Builder]::Conf) "$($this.ProjectFile)" -o $([Builder]::BuildFolder) | Out-Host
        if($LASTEXITCODE -ne 0) {
            exit 1
        }

        Write-Host "Executing $($this.Project) exporter" -ForegroundColor White
        $oldLocation = Get-Location
        Set-Location $([Builder]::BinFolder)
        dotnet "$([Builder]::BuildFolder)/$($this.Project).Exporter.dll" | Out-Host
        Set-Location $oldLocation
        if($LASTEXITCODE -ne 0) {
            exit 1
        }
    }

    [string] GetProjectVersion() {
        Write-Host "Grabbing Akka.Persistence.$($this.Project) module version" -ForegroundColor White
        $match = Select-String -Pattern """Akka.Persistence.$($this.Project)"" Version=""([a-zA-Z0-9.-]*)""" -Path $($this.ProjectFile)
        if($null -eq $match)
        {
            Write-Error -Message "Failed to retrieve version number for Akka.Persistence.$($this.Project), could not find regex pattern"
        }
        if($match.Matches.Length -gt 1)
        {
            Write-Error -Message "Failed to retrieve version number for Akka.Persistence.$($this.Project), found multiple possible versions"
        }
        $v = $match.Matches[0].Groups[1].Value
        Write-Host "Found version: $v" -ForegroundColor White
        return $v
    }
    
    [void] BuildDockerImage() {
        Write-Host "Building docker image: $($this.DockerName)" -ForegroundColor White
        Copy-Item "$($this.ProjectFolder)/Dockerfile" -Destination $([Builder]::BinFolder)
        
        $oldLocation = Get-Location
        Set-Location $([Builder]::BinFolder)
        docker build -t "$($this.DockerName):$($this.Version)" . | Out-Host
        Set-Location $oldLocation
        if($LASTEXITCODE -ne 0) {
            exit 1
        }

        if([Builder]::Latest -eq $true) {
            docker image tag "$($this.DockerName):$($this.Version)" "$($this.DockerName):latest" | Out-Host
            if($LASTEXITCODE -ne 0) {
                exit 1
            }
        }

        if([Builder]::Push -eq $true){
            docker image push -a $this.DockerName | Out-Host
            if($LASTEXITCODE -ne 0) {
                exit 1
            }
        }
        
    }

    [void] CleanUp() {
        Write-Host "Cleaning up temporary folders" -ForegroundColor White
        if(Test-Path -Path $([Builder]::BuildFolder)) {
            Write-Verbose -Message "Removing $([Builder]::BuildFolder)"
            Remove-Item -Recurse -Force $([Builder]::BuildFolder)
        }
        if(Test-Path -Path $([Builder]::OutputFolder)) {
            Write-Verbose -Message "Removing $([Builder]::OutputFolder)"
            Remove-Item -Recurse -Force $([Builder]::OutputFolder)
        }
        if(Test-Path -Path "$([Builder]::BinFolder)/Dockerfile") {
            Write-Verbose -Message "Removing $([Builder]::BinFolder)/Dockerfile"
            Remove-Item -Force "$([Builder]::BinFolder)/Dockerfile"
        }
    }
}

# Variables
$BinFolder = "$PSScriptRoot/bin"
$BuildFolder = "$BinFolder/build"
$OutputFolder = "$BinFolder/output"
$Env:OUTPUT = $OutputFolder
$Output = @()

# Set object static variables
[Builder]::BinFolder = $BinFolder
[Builder]::BuildFolder = $BuildFolder
[Builder]::OutputFolder = $OutputFolder
[Builder]::Conf = $Configuration
[Builder]::DockerRepo = $DockerRepo
[Builder]::Push = $Push
[Builder]::Latest = $Latest

# Create bin folder
if( -not (Test-Path -Path $BinFolder))
{
    try 
    {
        New-Item -Path $BinFolder -ItemType Directory
    } 
    catch
    {
        Write-Error -Message "Failed to create directory $BinFolder"
    }
}

if("sqlite" -eq $BuildType -or "all" -eq $BuildType) {
    $sqlite = [Builder]::new("Sqlite")
    
    $sqlite.BuildProject()

    # SqLite project outputs a file, not a docker image
    $outputFile = "$BinFolder/akka-persistence-sqlite-test-data.$($sqlite.Version).db"
    Copy-Item -Force -Path "$OutputFolder/database.db" -Destination $outputFile
    $Output += "Akka.Persistence.Sqlite exporter database file: $outputFile"
    
    if($Latest -eq $true) {
        $outputFile = "$BinFolder/akka-persistence-sqlite-test-data.latest.db"
        Copy-Item -Force -Path "$OutputFolder/database.db" -Destination $outputFile
        $Output += "Akka.Persistence.Sqlite exporter database file: $outputFile"
    }
    
    $sqlite.CleanUp()
}

if("mysql" -eq $BuildType -or "all" -eq $BuildType) {
    $mysql = [Builder]::new("MySql")
    $mysql.BuildProject()
    $mysql.BuildDockerImage()
    $mysql.CleanUp()
    $Output += "Akka.Persistence.MySql exporter docker image name: $($mysql.DockerName)"
}

if("sqlserver" -eq $BuildType -or "all" -eq $BuildType) {
    $sqlserver = [Builder]::new("SqlServer")
    $sqlserver.BuildProject()
    $sqlserver.BuildDockerImage()
    $sqlserver.CleanUp()
    $Output += "Akka.Persistence.SqlServer exporter docker image name: $($sqlserver.DockerName)"
}

if("postgresql" -eq $BuildType -or "all" -eq $BuildType) {
    $postgresql = [Builder]::new("PostgreSql")
    $postgresql.BuildProject()
    $postgresql.BuildDockerImage()
    $postgresql.CleanUp()
    $Output += "Akka.Persistence.PostgreSql exporter docker image name: $($postgresql.DockerName)"
}

Write-Host $Output -Separator "`n" -ForegroundColor White
