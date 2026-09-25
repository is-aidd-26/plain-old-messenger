# C4 — Уровень 3: компоненты бэкенда

Диаграмма раскрывает внутреннее устройство контейнера «Бэкенд». Общий вид системы — на [диаграмме контейнеров](2-containers.md).

```mermaid
C4Component
    title Plain Old Messenger — компоненты бэкенда

    Container(spa, "Фронтенд", "React + TypeScript", "SPA")

    ContainerDb(store, "Хранилище", "", "объекты в памяти")

    Container_Boundary(backend, "Бэкенд") {
        Component(http, "HTTP-слой", "Minimal API", "маршруты /api/*")
        Component(chat, "ChatService", "C#", "валидация и логика диалогов")
    }

    Rel(spa, http, "REST/JSON")
    Rel(http, chat, "")
    Rel(chat, store, "хранит в")
```

## Соответствие коду

| Компонент | Файл | Ответственность |
| --- | --- | --- |
| HTTP-слой | `src/ChatApi/Program.cs` | маршруты `GET /api/dialogs`, `GET /api/messages`, `POST /api/messages`; разбор запросов, ответы JSON и код 400 при ошибках валидации |
| ChatService | `src/ChatApi/ChatService.cs` | проверка никнеймов и текста (лимит 500), сквозная нумерация сообщений, хранение диалогов, потокобезопасность (`lock`) |

## Пояснения

- HTTP-слой тонкий: только маршруты и преобразование «запрос → вызов сервиса → ответ».
- DTO (`Message`, `DialogSummary`, `MessageRequest`) — контракты запросов и ответов, отдельным компонентом не выделяются.
- «Хранилище» повторяет контейнер с диаграммы 2: физически это поля `ChatService`, отдельного процесса нет.
