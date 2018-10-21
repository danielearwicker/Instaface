set DOCKER_ACCOUNT=danielearwicker

pushd GraphServer
dotnet publish -c release -o pub
docker build . -t %DOCKER_ACCOUNT%/instaface-graphserver
rmdir /q /s pub
popd

docker push %DOCKER_ACCOUNT%/instaface-graphserver

pushd TimelineWebServer
dotnet publish -c release -o pub
docker build . -t %DOCKER_ACCOUNT%/instaface-timelineserver
rmdir /q /s pub
popd

docker push %DOCKER_ACCOUNT%/instaface-timelineserver
