
dotnet publish .\Sharp7.Monitor\Sharp7.Monitor.csproj -c Release --output publish\non-sc --no-self-contained

dotnet publish .\Sharp7.Monitor\Sharp7.Monitor.csproj -c Release --output publish\sc -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true --self-contained
