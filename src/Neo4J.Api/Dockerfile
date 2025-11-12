FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /App

COPY ./src/Neo4J.Api ./Neo4J.Api
RUN dotnet restore ./Neo4J.Api
RUN dotnet publish ./Neo4J.Api -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
EXPOSE 8080
WORKDIR /App
COPY --from=build /App/out .
ENTRYPOINT ["dotnet", "Neo4J.Api.dll"]

