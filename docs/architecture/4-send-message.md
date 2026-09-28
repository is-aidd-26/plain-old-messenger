# C4 — Уровень 4: код бэкенда (отправка сообщения)

Сценарий «отправка сообщения» — динамика взаимодействия контейнеров и компонентов. Статическая структура — на [диаграмме классов](4-code.md), получение сообщений — на [sequence-диаграмме получения](4-receive-message.md).

```mermaid
sequenceDiagram
    participant SPA as Фронтенд
    participant API as HTTP-слой
    participant Svc as ChatService
    participant Store as Хранилище

    SPA->>API: POST /api/messages
    API->>Svc: SendMessage(...)
    Svc->>Svc: валидация
    Svc->>Store: запись в чат
    Svc-->>API: Message
    API-->>SPA: 200 OK, Message
```

## Пояснения

- Валидация в `ChatService`: никнеймы не пустые, текст не пустой и не длиннее 500 символов; нарушение — `ArgumentException` → эндпоинт отвечает `400 {error}`.
- `Id` назначается один и используется в обеих копиях сообщения; диалог с собой (`from == to`) не дублирует запись.
- «Хранилище» здесь — внутренние структуры `ChatService`; отдельного процесса нет (см. [диаграмму контейнеров](2-containers.md)).
