[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$artifacts = [IO.Directory]::CreateDirectory((Join-Path $PSScriptRoot 'artifacts'))
$staging = $artifacts.CreateSubdirectory('.publish-' + [Guid]::NewGuid().ToString('N'))
$release = $staging.CreateSubdirectory('release')
$destination = Join-Path $artifacts.FullName 'release'
$previous = Join-Path $staging.FullName 'previous'
$published = $false

try {
    $windowsProject = Join-Path $PSScriptRoot 'FEFWRNGSolver/FEFWRNGSolver.csproj'
    $windowsOutput = Join-Path $release.FullName 'windows-x64'
    dotnet publish $windowsProject -c Release -r win-x64 --self-contained true -o $windowsOutput `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }

    $webProject = Join-Path $PSScriptRoot 'FEFWRNGSolver.Web/FEFWRNGSolver.Web.csproj'
    $webPublish = Join-Path $staging.FullName 'blazor'
    dotnet publish $webProject -c Release -o $webPublish
    if ($LASTEXITCODE -ne 0) { throw 'Web publish failed.' }

    $webOutput = Join-Path $release.FullName 'web'
    [IO.Directory]::Move((Join-Path $webPublish 'wwwroot'), $webOutput)
    [IO.File]::WriteAllText((Join-Path $webOutput '.nojekyll'), '')

    # Replace the two distributions together, keeping the previous release until the move succeeds.
    if ([IO.Directory]::Exists($destination)) {
        [IO.Directory]::Move($destination, $previous)
    }
    [IO.Directory]::Move($release.FullName, $destination)
    $published = $true
}
finally {
    if (!$published -and [IO.Directory]::Exists($previous)) {
        [IO.Directory]::Move($previous, $destination)
    }
    $staging.Delete($true)
}

Write-Output ("Windows: " + (Join-Path $destination 'windows-x64/FEFWRNGSolver.exe'))
Write-Output ("Web:     " + (Join-Path $destination 'web'))
