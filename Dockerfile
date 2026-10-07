# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props Nilogistic.slnx ./
COPY src/ src/
RUN dotnet restore src/Nilogistic.Aplicacion/Nilogistic.Aplicacion.csproj
RUN dotnet publish src/Nilogistic.Aplicacion/Nilogistic.Aplicacion.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcServer=0
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Nilogistic.Aplicacion.dll"]
