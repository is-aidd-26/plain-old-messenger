import { useState } from 'react'
import ChatPane from './ChatPane'
import DialogList from './DialogList'
import LoginForm from './LoginForm'
import './App.css'

function App() {
  const [user, setUser] = useState<string | null>(null)
  const [peer, setPeer] = useState<string | null>(null)

  if (user === null) {
    return <LoginForm onLogin={setUser} />
  }

  return (
    <main className="app">
      <header className="app-header">
        Plain Old Messenger <span className="app-user">— вы вошли как {user}</span>
      </header>
      <div className="app-body">
        <DialogList user={user} activePeer={peer} onSelect={setPeer} />
        {peer === null ? (
          <section className="chat-placeholder">
            <p className="hint">Выберите диалог или начните новый.</p>
          </section>
        ) : (
          <ChatPane user={user} peer={peer} />
        )}
      </div>
    </main>
  )
}

export default App
