#!/usr/bin/env pwsh

cd $PSScriptRoot

Remove-Item ./NugetPackages/* -Recurse -Force

$packages = @("Dusharp.SourceGenerator", "Dusharp.Json", "Dusharp.Newtonsoft")
foreach ($package in $packages)
{
    dotnet pack ./src/$package -o ./NugetPackages
}