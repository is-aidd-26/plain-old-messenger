# Пример Plain Old Messenger на C# и React

Это пример проекта «Мессенджер» от преподавателей курса. Состоит из двух частей:

* Бэкенд — API-сервис на ASP.NET Core (.NET 10, C# 14);
* Фронтенд — SPA на React, для сборки использует Vite.

Документация — в каталоге docs/.

## Требования

* .NET 10 SDK: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
* Node.js 20 или новее: https://nodejs.org/ (проверить версию: `node --version`)
* Docker (Docker Desktop или Docker Engine с плагином compose): https://docs.docker.com/get-docker/

## Ветки

Все этапы разработки этого проекта сохранены в отдельных ветках. В master — последний завершённый этап.

| Ветка                | Что добавлено                          |
| -------------------- | -------------------------------------- |
| 00_template          | Скопирован шаблон проекта              |
| 01_prd               | Написан PRD.md и SRS.md                |
| 02_arch              | Добавлены C4 диаграммы                 |
| 03_er                | Добавлены ER-диаграммы                 |
| 04_postgres          | Добавлено хранение в Postgres          |
| 05_integration_tests | Добавлены интеграционные тесты бэкенда |
| 06_gherkin           | Тесты переписаны на Gherkin + Reqnroll |

## Запуск фронтенда и бэкенда

Понадобится две вкладки терминала.

### Терминал 1 — запуск бэкенда:

```bash
# Запускаем docker-контейнер PostgreSQL 18 в фоновом режиме.
docker compose up -d --wait

# Собираем и запускаем бэкенд.
# При запуске он сам применит миграции.
dotnet run --project src/ChatApi/
```

### Терминал 2 — запуск фронтенда:

```bash
# Однократно устанавливаем зависимости.
npm -C frontend/ ci

# Запускаем фронтенд.
npm -C frontend/ run dev
```

Дальше можно открыть в браузере http://localhost:5173

Чтобы проверить чат, откройте две вкладку браузера с разными никнеймами

### Запуск psql

Позволяет выполнять запросы в базе данных:

```bash
# Открывает сессию psql
# Далее можно выполнить:
#   SELECT id, nickname FROM users
docker compose exec db psql -U chat -d chat
```

## Сборка и проверки

```bash
# Сборка бэкенда, в том числе статический анализ линтерами
dotnet build

# Запуск интеграционных тестов (Gherkin + Reqnroll).
# Сценарии описаны в tests/ChatApi.Specs/Messaging.feature на русском диалекте
# Gherkin и исполняются через Reqnroll.xUnit: приложение поднимается в памяти,
# PostgreSQL запускается через testcontainers. Каждый сценарий получает
# собственную базу данных внутри одного контейнера.
dotnet test

# Переход в каталог фронтенда
cd frontend/

# Запуск линтера
npm run lint

# Автоформатирование
npm run format

# Сборка TypeScript в минимизированный JavaScript.
npm run build
```
