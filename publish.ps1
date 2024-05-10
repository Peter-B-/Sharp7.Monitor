
dotnet publish .\Sharp7.Monitor\Sharp7.Monitor.csproj -c Release --output publish\non-sc --no-self-contained

dotnet publish .\Sharp7.Monitor\Sharp7.Monitor.csproj -c Release -r win-x64 --self-contained  -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true  --output publish\sc\win-x64
dotnet publish .\Sharp7.Monitor\Sharp7.Monitor.csproj -c Release -r linux-x64 --self-contained  -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true  --output publish\sc\linux-x64

