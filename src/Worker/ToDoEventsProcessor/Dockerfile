# Użyj oficjalnego obrazu .NET 8.0 SDK jako build-env
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /src/Worker/ToDoEventsProcessor

# Skopiuj tylko pliki projektowe
COPY *.csproj ./
# Przywróć zależności jako osobne warstwy
RUN dotnet restore

# Skopiuj resztę plików i zbuduj aplikację
COPY . ./
RUN dotnet publish -c Release -o out

# Zbuduj runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /src/Worker/ToDoEventsProcessor/out .
RUN ls -a
ENTRYPOINT ["dotnet", "ToDoEventsProcessor.dll"]
