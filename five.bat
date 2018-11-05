start gs.bat 6001
start gs.bat 6002
start gs.bat 6003
start gs.bat 6004
start gs.bat 6005

pushd TimelineWebServer
start dotnet bin\Debug\netcoreapp2.1\TimelineWebServer.dll --urls http://*:6500
popd
