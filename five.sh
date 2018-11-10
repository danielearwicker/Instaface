. gs.sh 6001
. gs.sh 6002
. gs.sh 6003
. gs.sh 6004
. gs.sh 6005

pushd TimelineWebServer
dotnet bin/Debug/netcoreapp2.1/TimelineWebServer.dll --urls http://*:6500
popd
