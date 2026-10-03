import { useEffect, useRef, useState, type SubmitEvent } from 'react'
import { fetchMessages, sendMessage, type ChatMessage } from './api'

const PollIntervalMs = 2000

type ChatPaneProps = {
  user: string
  peer: string
  dialogId: number | null
  onDialogStarted: (dialogId: number) => void
}

function formatTime(sentAt: string): string {
  return new Date(sentAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
}

function ChatPane({ user, peer, dialogId, onDialogStarted }: ChatPaneProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [text, setText] = useState('')
  const [error, setError] = useState<string | null>(null)
  const lastIdRef = useRef(0)
  const messagesRef = useRef<HTMLUListElement>(null)

  // При переключении диалога сбрасываем состояние прямо в рендере (паттерн из документации React).
  // Черновик нового диалога живёт под именем собеседника, пока первое сообщение не даст идентификатор.
  const identity = dialogId ?? `draft:${peer}`
  const [openedIdentity, setOpenedIdentity] = useState(identity)
  if (openedIdentity !== identity) {
    setOpenedIdentity(identity)
    setMessages([])
    setError(null)
  }

  // Загружаем историю нового диалога, затем опрашиваем только свежие сообщения.
  useEffect(() => {
    if (dialogId === null) {
      return // у черновика ещё нет истории — она появится вместе с первым сообщением
    }

    const currentId = dialogId
    let cancelled = false
    lastIdRef.current = 0

    function append(fresh: ChatMessage[]) {
      const unseen = fresh.filter((message) => message.id > lastIdRef.current)
      if (unseen.length > 0) {
        lastIdRef.current = unseen[unseen.length - 1].id
        setMessages((prev) => [...prev, ...unseen])
      }
    }

    async function poll() {
      try {
        append(await fetchMessages(currentId, user, lastIdRef.current))
        if (!cancelled) {
          setError(null)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Не удалось получить сообщения.')
        }
      }
    }

    void poll()
    const timer = setInterval(() => void poll(), PollIntervalMs)
    return () => {
      cancelled = true
      clearInterval(timer)
    }
  }, [user, dialogId])

  // Прокручиваем переписку вниз при появлении новых сообщений.
  useEffect(() => {
    messagesRef.current?.scrollTo({ top: messagesRef.current.scrollHeight })
  }, [messages])

  async function handleSend() {
    const trimmed = text.trim()
    if (trimmed === '') {
      setError('Сообщение не может быть пустым.')
      return
    }

    try {
      const sent = await sendMessage(user, peer, trimmed)
      if (dialogId === null) {
        // Черновик получил идентификатор: эффект перезагрузит историю уже по нему.
        onDialogStarted(sent.dialogId)
      } else if (sent.id > lastIdRef.current) {
        lastIdRef.current = sent.id
        setMessages((prev) => [...prev, sent])
      }
      setText('')
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Не удалось отправить сообщение.')
    }
  }

  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    void handleSend()
  }

  return (
    <section className="chat-pane">
      <header className="chat-header">
        <h2>Диалог с {peer}</h2>
      </header>
      <ul ref={messagesRef} className="messages">
        {messages.map((message) => (
          <li key={message.id} className={message.author === user ? 'message own' : 'message'}>
            <div className="message-meta">
              <span className="message-author">{message.author}</span>
              <span className="message-time">{formatTime(message.sentAt)}</span>
            </div>
            <div className="message-text">{message.text}</div>
          </li>
        ))}
      </ul>
      {error !== null && <p className="error">{error}</p>}
      <form className="chat-form" onSubmit={handleSubmit}>
        <input
          value={text}
          onChange={(event) => setText(event.target.value)}
          placeholder="Введите сообщение…"
          aria-label="Текст сообщения"
          maxLength={500}
        />
        <button type="submit">Отправить</button>
      </form>
    </section>
  )
}

export default ChatPane
