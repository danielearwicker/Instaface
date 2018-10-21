FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
COPY pub .
EXPOSE 80
ENTRYPOINT ["dotnet", "GraphServer.dll"]
