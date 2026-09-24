import { useEffect, useRef, useState, type FormEvent } from 'react'
import './App.css'

function App() {
  const [text, setText] = useState('')
  const [sentMessages, setSentMessages] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)
  const messagesRef = useRef<HTMLUListElement>(null)

  useEffect(() => {
    messagesRef.current?.scrollTo({
      top: messagesRef.current.scrollHeight,
      behavior: 'smooth',
    })
  }, [sentMessages])

  async function sendMessage() {
    const message = text.trim()
    if (message === '') {
      setError('Сообщение не может быть пустым.')
      return
    }

    setError(null)
    try {
      // Отправляем сообщение на бэкенд. В dev-режиме Vite проксирует /api
      // на http://localhost:5000 (см. vite.config.ts).
      const response = await fetch('/api/messages', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text: message }),
      })

      if (!response.ok) {
        throw new Error(`Сервер вернул ошибку: ${response.status}`)
      }

      setSentMessages([...sentMessages, message])
      setText('')
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Не удалось отправить сообщение. Возможно, бэкенд не запущен.',
      )
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    void sendMessage()
  }

  return (
    <main className="chat">
      <header className="chat-header">
        <h1>Чат</h1>
        <p className="chat-hint">
          Сообщения никуда не сохраняются — они только попадают в лог бэкенда.
        </p>
      </header>
      <ul ref={messagesRef} className="chat-messages">
        {sentMessages.map((message, index) => (
          <li key={index}>{message}</li>
        ))}
      </ul>
      {error !== null && <p className="chat-error">{error}</p>}
      <form onSubmit={handleSubmit}>
        <input
          value={text}
          onChange={(event) => setText(event.target.value)}
          placeholder="Введите сообщение…"
          aria-label="Текст сообщения"
        />
        <button type="submit">Отправить</button>
      </form>
    </main>
  )
}

export default App
