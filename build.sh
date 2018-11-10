pushd client
yarn build
popd
rm -rf TimelineWebServer/wwwroot
cp -R client/build TimelineWebServer/wwwroot

export DOCKER_ACCOUNT=danielearwicker

pushd GraphServer
dotnet publish -c release -o pub
docker build . -t $DOCKER_ACCOUNT/instaface-graphserver
rm -rf pub
popd

docker push $DOCKER_ACCOUNT/instaface-graphserver

pushd TimelineWebServer
dotnet publish -c release -o pub
docker build . -t $DOCKER_ACCOUNT/instaface-timelineserver
rm -rf pub
popd

docker push $DOCKER_ACCOUNT/instaface-timelineserver
