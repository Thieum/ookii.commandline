# This script is used to create a distribution folder that can be packaged into a zip file for release.
# Before running this script, make sure you have built a Release version of the solution, and have created
# an up-to-date Documentation.chm file.
param(
    [parameter(Mandatory=$true, Position=0)][string]$TargetPath
)
$commonItems = "..\docs\User Guide.html",
    "..\docs\Help\Documentation.chm",
    "..\docs\license.txt",
    "Snippets"

$netFrameworkItems = "Ookii.CommandLine\bin\Release\net20\Ookii.CommandLine.dll",
    "Ookii.CommandLine\bin\Release\net20\Ookii.CommandLine.xml",
    "Ookii.CommandLine\bin\Release\net20\Ookii.CommandLine.pdb",
    "CommandLineSampleCS\bin\Release\CommandLineSampleCS.exe",
    "ShellCommandSampleCS\bin\Release\ShellCommandSampleCS.exe"

$netStandardItems = "Ookii.CommandLine\bin\Release\netstandard2.0\Ookii.CommandLine.dll",
    "Ookii.CommandLine\Ookii.CommandLine.xml",
    "Ookii.CommandLine\bin\Release\netstandard2.0\Ookii.CommandLine.pdb"

if( [System.IO.Directory]::GetFileSystemEntries($TargetPath).Length -gt 0 ) {
    throw "Target directory not empty." 
}

$commonItems | 
    ForEach-Object { Join-Path $PSScriptRoot $_ } | 
    Copy-Item -Destination $TargetPath -Recurse

$target = Join-Path $TargetPath net20
New-Item $target -ItemType Directory | Out-Null
$netFrameworkItems | 
    ForEach-Object { Join-Path $PSScriptRoot $_ } | 
    Copy-Item -Destination $target -Recurse

$target = Join-Path $TargetPath netstandard2.0
New-Item $target -ItemType Directory | Out-Null
$netStandardItems | 
    ForEach-Object { Join-Path $PSScriptRoot $_ } | 
    Copy-Item -Destination $target -Recurse
