cd $PSScriptRoot

Remove-Item ./NugetPackages/* -Recurse -Force

$packages = @("Dusharp.SourceGenerator", "Dusharp.Json")
foreach ($package in $packages)
{
    dotnet pack ./src/$package -c Release -o ./NugetPackages
}