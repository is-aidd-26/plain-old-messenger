import { useEffect, useState, type SubmitEvent } from 'react'
import { fetchDialogs, type DialogSummary } from './api'

const PollIntervalMs = 2000

type DialogListProps = {
  user: string
  activeDialogId: number | null
  onSelect: (dialog: { dialogId: number | null; peer: string }) => void
}

function DialogList({ user, activeDialogId, onSelect }: DialogListProps) {
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

  // Новый диалог начинается с ника собеседника; идентификатор даст первое сообщение.
  function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    const peer = newPeer.trim()
    if (peer !== '') {
      onSelect({ dialogId: null, peer })
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
            <li key={dialog.dialogId}>
              <button
                type="button"
                className={
                  dialog.dialogId === activeDialogId ? 'dialog-item active' : 'dialog-item'
                }
                onClick={() => onSelect({ dialogId: dialog.dialogId, peer: dialog.peer })}
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
