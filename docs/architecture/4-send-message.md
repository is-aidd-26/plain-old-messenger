# C4 — Уровень 4: код бэкенда (отправка сообщения)

Сценарий «отправка сообщения» — динамика взаимодействия контейнеров и компонентов. Статическая структура — на [диаграмме классов](4-code.md), получение сообщений — на [sequence-диаграмме получения](4-receive-message.md).

```mermaid
sequenceDiagram
    participant SPA as Фронтенд
    participant API as HTTP-слой
    participant Svc as ChatService
    participant Store as PostgresChatStore
    participant DB as PostgreSQL

    SPA->>API: POST /api/messages
    API->>Svc: SendMessageAsync(...)
    Svc->>Svc: валидация и trim
    Svc->>Store: SendMessageAsync(...)
    Store->>DB: INSERT в транзакции
    DB-->>Store: dialogId, id
    Store-->>Svc: SentMessage
    Svc-->>API: SentMessage
    API-->>SPA: 200 OK, SentMessage
```

## Пояснения

- Валидация в `ChatService`: никнеймы не пустые и не длиннее 64 символов, текст не пустой и не длиннее 500; нарушение — `ArgumentException` → эндпоинт отвечает `400 {error}`.
- Всё пишется одной транзакцией: незнакомые отправитель и получатель появляются в `users`, диалог находится по составу участников или создаётся вместе с первым сообщением. Advisory lock по паре участников не даёт параллельным «первым» сообщениям создать второй диалог.
- Ответ содержит `dialogId` — фронтенд использует его для истории и поллинга после первой отправки.
