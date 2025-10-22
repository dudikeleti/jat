$ErrorActionPreference = 'Continue'
$env:ASPNETCORE_URLS = 'http://localhost:5000'
Set-Location 'C:\dev\datadog\DdSample-net8.0'
dotnet run --project 'DdSample-net8.0.csproj' --no-launch-profile
Read-Host 'Press Enter to close'
