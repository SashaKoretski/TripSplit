# syntax=docker/dockerfile:1
# Контейнер запуска тестов (ТИОПО ЛР2, Т1): скачивает репозиторий с кодом внутри сборки
# образа (git clone + checkout конкретной ревизии), а не копирует уже выгруженный исходный
# код из контекста сборки, ставит .NET SDK и все внешние зависимости, нужные для unit/
# integration/e2e на используемом языке (C#/.NET). Вызывается из CI/CD (см. .github/workflows).
FROM mcr.microsoft.com/dotnet/sdk:8.0

ARG REPO_URL=https://github.com/SashaKoretski/TripSplit.git
ARG REPO_REVISION=main

RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /repo
RUN git clone "$REPO_URL" . \
    && git checkout "$REPO_REVISION" \
    && echo "Testing revision: $(git rev-parse HEAD)"

WORKDIR /repo/src
RUN dotnet restore TripSplit.sln

COPY run-tests.sh /usr/local/bin/run-tests.sh
RUN chmod +x /usr/local/bin/run-tests.sh

ENTRYPOINT ["/usr/local/bin/run-tests.sh"]
