import { useEffect, useState, type FormEvent } from 'react'
import { fetchDialogs, type DialogSummary } from './api'

const PollIntervalMs = 2000

type DialogListProps = {
  user: string
  activePeer: string | null
  onSelect: (peer: string) => void
}

function DialogList({ user, activePeer, onSelect }: DialogListProps) {
  const [dialogs, setDialogs] = useState<DialogSummary[]>([])
  const [newPeer, setNewPeer] = useState('')

  // Загружаем список диалогов и обновляем его опросом, чтобы замечать новые входящие.
  useEffect(() => {
    let cancelled = false

    async function refresh() {
      try {
        const fresh = await fetchDialogs(user)
        if (!cancelled) {
          setDialogs(fresh)
        }
      } catch {
        // Список обновится при следующем опросе.
      }
    }

    void refresh()
    const timer = setInterval(() => void refresh(), PollIntervalMs)
    return () => {
      cancelled = true
      clearInterval(timer)
    }
  }, [user])

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const peer = newPeer.trim()
    if (peer !== '') {
      onSelect(peer)
      setNewPeer('')
    }
  }

  return (
    <aside className="sidebar">
      <h2>Диалоги</h2>
      <form className="dialog-form" onSubmit={handleSubmit}>
        <input
          value={newPeer}
          onChange={(event) => setNewPeer(event.target.value)}
          placeholder="Ник собеседника"
          aria-label="Ник собеседника"
        />
        <button type="submit">Начать</button>
      </form>
      {dialogs.length === 0 ? (
        <p className="hint">Нет диалогов. Укажите ник собеседника, чтобы начать новый.</p>
      ) : (
        <ul className="dialog-list">
          {dialogs.map((dialog) => (
            <li key={dialog.peer}>
              <button
                type="button"
                className={dialog.peer === activePeer ? 'dialog-item active' : 'dialog-item'}
                onClick={() => onSelect(dialog.peer)}
              >
                <span className="dialog-peer">{dialog.peer}</span>
                <span className="dialog-preview">{dialog.lastMessage.text}</span>
              </button>
            </li>
          ))}
        </ul>
      )}
    </aside>
  )
}

export default DialogList
