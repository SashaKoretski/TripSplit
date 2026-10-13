# syntax=docker/dockerfile:1
# Образ тестируемого приложения (ТИОПО ЛР2: "окружение, которое будет тестироваться,
# тоже лучше развернуть в контейнере"). Используется стендом ЛР2 (docker-compose.yml,
# сервис webui) для integration/e2e, НЕ заменяет обычный dotnet run в dev-режиме.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TripSplit.sln ./
COPY TripSplit.BusinessLogic/TripSplit.BusinessLogic.csproj TripSplit.BusinessLogic/
COPY TripSplit.DataAccess/TripSplit.DataAccess.csproj TripSplit.DataAccess/
COPY TripSplit.Logger/TripSplit.Logger.csproj TripSplit.Logger/
COPY TripSplit.WebUI/TripSplit.WebUI.csproj TripSplit.WebUI/
RUN dotnet restore TripSplit.WebUI/TripSplit.WebUI.csproj

COPY TripSplit.BusinessLogic/ TripSplit.BusinessLogic/
COPY TripSplit.DataAccess/ TripSplit.DataAccess/
COPY TripSplit.Logger/ TripSplit.Logger/
COPY TripSplit.WebUI/ TripSplit.WebUI/
RUN dotnet publish TripSplit.WebUI/TripSplit.WebUI.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TripSplit.WebUI.dll"]
