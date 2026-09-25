# C4 — Уровень 4: код бэкенда (последовательность)

Сценарий «отправка сообщения» — динамика взаимодействия контейнеров и компонентов. Статическая структура — на [диаграмме классов](4-code.md).

```mermaid
sequenceDiagram
    participant SPA as Фронтенд
    participant API as HTTP-слой
    participant Svc as ChatService
    participant Store as Хранилище

    SPA->>API: POST /api/messages {from, to, text}
    API->>Svc: SendMessage(from, to, text)
    Svc->>Svc: валидация, ++Id
    Svc->>Store: запись в обе стороны диалога
    Svc-->>API: Message
    API-->>SPA: 200 OK, Message

    Note over SPA,Store: У собеседника — те же участники: поллинг GET /api/messages?after=Id раз в 2 с
```

## Пояснения

- Валидация в `ChatService`: никнеймы не пустые, текст не пустой и не длиннее 500 символов; нарушение — `ArgumentException` → эндпоинт отвечает `400 {error}`.
- `Id` назначается один и используется в обеих копиях сообщения; диалог с собой (`from == to`) не дублирует запись.
- «Хранилище» здесь — внутренние структуры `ChatService`; отдельного процесса нет (см. [диаграмму контейнеров](2-containers.md)).
