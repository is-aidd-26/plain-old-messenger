import { useState, type FormEvent } from 'react'

type LoginFormProps = {
  onLogin: (nickname: string) => void
}

function LoginForm({ onLogin }: LoginFormProps) {
  const [nickname, setNickname] = useState('')
  const [error, setError] = useState<string | null>(null)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = nickname.trim()
    if (trimmed === '') {
      setError('Укажите никнейм.')
      return
    }
    onLogin(trimmed)
  }

  return (
    <main className="login">
      <form className="login-card" onSubmit={handleSubmit}>
        <h1>Plain Old Messenger</h1>
        <p className="hint">Введите никнейм, чтобы начать переписку.</p>
        <input
          value={nickname}
          onChange={(event) => setNickname(event.target.value)}
          placeholder="Никнейм"
          aria-label="Никнейм"
        />
        <button type="submit">Войти</button>
        {error !== null && <p className="error">{error}</p>}
      </form>
    </main>
  )
}

export default LoginForm
