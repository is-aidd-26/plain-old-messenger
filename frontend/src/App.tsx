import { useState } from 'react'
import ChatPane from './ChatPane'
import DialogList from './DialogList'
import LoginForm from './LoginForm'
import './App.css'

// Открытый диалог. Черновик нового диалога ещё не имеет идентификатора:
// он появится в ответе на первое отправленное сообщение.
type ActiveDialog = {
  dialogId: number | null
  peer: string
}

function App() {
  const [user, setUser] = useState<string | null>(null)
  const [active, setActive] = useState<ActiveDialog | null>(null)

  if (user === null) {
    return <LoginForm onLogin={setUser} />
  }

  // Первый отправленное сообщение создаёт диалог и даёт его идентификатор.
  function handleDialogStarted(dialogId: number) {
    setActive((prev) => (prev === null ? prev : { ...prev, dialogId }))
  }

  return (
    <main className="app">
      <header className="app-header">
        Plain Old Messenger <span className="app-user">— вы вошли как {user}</span>
      </header>
      <div className="app-body">
        <DialogList user={user} activeDialogId={active?.dialogId ?? null} onSelect={setActive} />
        {active === null ? (
          <section className="chat-placeholder">
            <p className="hint">Выберите диалог или начните новый.</p>
          </section>
        ) : (
          <ChatPane
            user={user}
            peer={active.peer}
            dialogId={active.dialogId}
            onDialogStarted={handleDialogStarted}
          />
        )}
      </div>
    </main>
  )
}

export default App
