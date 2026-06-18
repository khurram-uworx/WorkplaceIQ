ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY WorkplaceIQ.sln ./
COPY src/WorkplaceIQ/WorkplaceIQ.csproj src/WorkplaceIQ/
COPY src/WorkplaceIQ.AspNet/WorkplaceIQ.AspNet.csproj src/WorkplaceIQ.AspNet/
COPY src/WorkplaceIQ.Web/WorkplaceIQ.Web.csproj src/WorkplaceIQ.Web/
COPY tests/WorkplaceIQ.Tests/WorkplaceIQ.Tests.csproj tests/WorkplaceIQ.Tests/

RUN dotnet restore src/WorkplaceIQ.Web/WorkplaceIQ.Web.csproj

COPY . .
RUN dotnet publish src/WorkplaceIQ.Web/WorkplaceIQ.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:4792
EXPOSE 4792

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WorkplaceIQ.Web.dll"]
