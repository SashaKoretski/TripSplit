# Захват трафика E2E-сценария (ТИОПО ЛР2, З5)

## Как снято

1. Стенд: `postgres-test` + `webui-test` (сервис `webui-test` в `docker-compose.yml`,
   билдится из `src/TripSplit.WebUI/Dockerfile`), оба в одной docker-сети.
2. Захватчик трафика: сайдкар-контейнер `alpine` с установленным `tcpdump`,
   запущенный с `--network container:ppo_github-webui-test-1` - то есть делящий
   сетевой namespace именно с контейнером приложения, слушающий его `eth0`.
3. Средство отправки запросов (не сам xUnit E2E-тест, по условию З5):
   `docker/traffic-capture/replay-e2e-scenario.sh` - bash/curl-скрипт, повторяющий
   по смыслу те же шаги, что `src/TripSplit.E2E.Tests/TripLifecycleE2ETests.cs`:
   регистрация Sasha и Masha -> создание поездки -> приглашение -> добавление
   траты -> просмотр расчета -> завершение поездки -> логаут обоих.
4. Хранилище сброшено (`reset-test-data.sql` через psql) непосредственно перед
   прогоном под захватом, чтобы сценарий был воспроизводим "с нуля".

## Файлы

- `e2e-scenario.pcap` - бинарный capture-файл (tcpdump, формат pcap).
- `e2e-scenario-summary.txt` - текстовая расшифровка (`tcpdump -r ... -nn`), 524 строки.

## Соответствие шагов сценария и пакетов (timestamps из summary.txt)

| Шаг сценария (TripLifecycleE2ETests.cs) | HTTP в капчуре |
|---|---|
| Act 1: GET /Account/Login (Sasha) | `12:32:23.821781 GET /Account/Login` -> `200 OK` |
| Act 1: POST /Account/Register (Sasha) | `12:32:23.952890 POST /Account/Register` -> `302 Found` |
| Act 2: GET/POST аналогично для Masha | `12:32:24.007167 GET /Account/Login` -> `12:32:24.140712 POST /Account/Register` -> `302` |
| Act 3: GET /Trips, POST /Trips/Create | `12:32:24.212764 GET /Trips` -> `12:32:24.346596 POST /Trips/Create` -> `302` |
| Act 4: GET/POST /Trips/Invite | `12:32:24.515563 GET /Trips/Details/<id>` -> `12:32:24.649228 POST /Trips/Invite/<id>` -> `302` |
| Act 5: GET/POST /Expenses/Create | `12:32:24.826248 GET /Expenses/Create` -> `12:32:25.059006 POST /Expenses/Create` -> `302` |
| Act 6: GET /Settlement | `12:32:25.138626 GET /Settlement` -> `200 OK` |
| Act 7: POST /Trips/Finish | `12:32:25.345985 POST /Trips/Finish/<id>` -> `302` |
| Act 8: GET /Expenses/Create после Finish (авария) | не выполнялся в curl-реплее (его смысл проверен в самом xUnit E2E-тесте); здесь реплей ограничен основным успешным путем |
| finally: POST /Account/Logout x2 | `12:32:25.602878` и `12:32:25.792042 POST /Account/Logout` -> `302 Found` каждый |

## Что видно в захвате и почему

Приложение в этом тестовом стенде работает по обычному HTTP (без TLS, см.
`docker-compose.yml` сервис `webui-test`, порт 8080) - это осознанное и
безопасное упрощение именно тестового стенда, не рабочей конфигурации
(prod/README описывает ту же модель: cookie HttpOnly + CSRF, TLS - вопрос
окружения развертывания, не предмет этой ЛР). Поэтому в `pcap` виден полный
текст HTTP-запросов и ответов, включая заголовки `Cookie`/`Set-Cookie`.

Значения cookie - это session id и antiforgery-токен синтетических тестовых
аккаунтов (`sasha@example.test`, `masha@example.test`), созданных и удаленных
в рамках этого же прогона (хранилище сбрасывается до и после) - не секреты
реального пользователя и не пароли (вход в приложении - по email без пароля).

Нерелевантный шум в начале `pcap` (несколько пакетов на 443 порт, TCP RST) -
фоновая активность сетевого стека контейнера, не относящаяся к сценарию;
отфильтрован в таблице выше по смысловым HTTP-строкам.
