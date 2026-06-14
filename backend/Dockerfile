FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/LastHour.slnx ./
COPY backend/src/LastHour.Domain/*.csproj src/LastHour.Domain/
COPY backend/src/LastHour.Application/*.csproj src/LastHour.Application/
COPY backend/src/LastHour.Infrastructure/*.csproj src/LastHour.Infrastructure/
COPY backend/src/LastHour.API/*.csproj src/LastHour.API/
RUN dotnet restore

COPY backend/src/ src/
WORKDIR /src/src/LastHour.API
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080 8081
ENTRYPOINT ["dotnet", "LastHour.API.dll"]
