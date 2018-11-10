pushd GraphServer
set Consensus__Self=http://localhost:%1
dotnet bin/Debug/netcoreapp2.1/GraphServer.dll --urls http://*:%1
