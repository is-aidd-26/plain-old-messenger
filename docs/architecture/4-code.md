# C4 — Уровень 4: код бэкенда (классы)

Статическая структура ключевого компонента — `ChatService` — и его контракты. Динамика — на sequence-диаграммах [отправки](4-send-message.md) и [получения](4-receive-message.md) сообщений, состав компонентов — на [диаграмме компонентов](3-components.md).

```mermaid
classDiagram
    direction LR
    class ChatService {
        <<service>>
        -dialogs Dictionary&lt;string, Dictionary&lt;string, List&lt;Message&gt;&gt;&gt;
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

    DialogSummary o-- Message : LastMessage
    ChatService ..> Message : хранит и создаёт
    ChatService ..> DialogSummary : возвращает
```

## Пояснения

- `ChatService` — singleton с доступом под `lock` (приватные детали на диаграмме опущены); каждое сообщение попадает в обе стороны диалога, поэтому `Id` один на оба списка.
- `Message.Id` — сквозная нумерация: по ней работает поллинг (`after` = номер последнего полученного сообщения).
- Записи (record) — иммутабельные контракты запросов и ответов; `DialogSummary` — проекция для списка диалогов. `MessageRequest` — контракт тела POST-запроса, на диаграмме опущен.
