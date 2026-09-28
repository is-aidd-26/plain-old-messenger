# C4 — Уровень 4: код бэкенда (получение сообщений)

Сценарий «получение сообщений» — поллинг открытого диалога. Статическая структура — на [диаграмме классов](4-code.md), отправка — на [sequence-диаграмме отправки](4-send-message.md).

```mermaid
sequenceDiagram
    participant SPA as Фронтенд
    participant API as HTTP-слой
    participant Svc as ChatService
    participant Store as Хранилище

    loop каждые 2 с
        SPA->>API: GET /api/messages?after=Id
        API->>Svc: GetMessages(...)
        Svc->>Store: чтение новых
        Svc-->>API: Message[]
        API-->>SPA: 200 OK, новые
    end
```

## Пояснения

- `after` — `Id` последнего показанного сообщения; `0` — вся история при открытии диалога.
- Пустой список — обычный ответ, не ошибка: большинство опросов ничего не находят.
- Список диалогов обновляется отдельным поллингом `GET /api/dialogs` с тем же интервалом.
