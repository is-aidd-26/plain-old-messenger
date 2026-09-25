# C4 — Уровень 4: код бэкенда (классы)

Статическая структура ключевого компонента — `ChatService` — и его контракты. Динамика сценария — на [sequence-диаграмме](4-sequence.md), состав компонентов — на [диаграмме компонентов](3-components.md).

```mermaid
classDiagram
    class ChatService {
        <<service>>
        -dialogs user → peer → List~Message~
        -lastMessageId long
        +GetDialogs(user) IReadOnlyList~DialogSummary~
        +GetMessages(user, peer, afterId) IReadOnlyList~Message~
        +SendMessage(from, to, text) Message
    }
    class Message {
        <<record>>
        +Id long
        +Author string
        +Text string
        +SentAt DateTimeOffset
    }
    class DialogSummary {
        <<record>>
        +Peer string
        +LastMessage Message
    }
    class MessageRequest {
        <<record>>
        +From string
        +To string
        +Text string
    }

    DialogSummary o-- Message : LastMessage
    ChatService ..> Message : хранит и создаёт
    ChatService ..> DialogSummary : возвращает
    ChatService ..> MessageRequest : поля из POST
```

## Пояснения

- `ChatService` — singleton с доступом под `lock` (приватные детали на диаграмме опущены); каждое сообщение попадает в обе стороны диалога, поэтому `Id` один на оба списка.
- `Message.Id` — сквозная нумерация: по ней работает поллинг (`after` = номер последнего полученного сообщения).
- Записи (record) — иммутабельные контракты запросов и ответов; `DialogSummary` — проекция для списка диалогов.
