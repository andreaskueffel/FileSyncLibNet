dotnet publish -o pub-winx64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=false -p:Version=0.0.19-dev -r win-x64 --self-contained .\FileSyncApp.csproj
dotnet publish -o pub-linux64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=false -p:Version=0.0.19-dev -r linux-x64 --self-contained .\FileSyncApp.csproj
