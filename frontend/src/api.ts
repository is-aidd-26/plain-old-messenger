export type ChatMessage = {
  id: number
  author: string
  text: string
  sentAt: string
}

export type DialogSummary = {
  dialogId: number
  peer: string
  lastMessage: ChatMessage
}

export type SentMessage = {
  dialogId: number
} & ChatMessage

async function readError(response: Response): Promise<string> {
  const body = (await response.json().catch(() => null)) as { error?: string } | null
  return body?.error ?? `Сервер вернул ошибку: ${response.status}`
}

export async function fetchDialogs(user: string): Promise<DialogSummary[]> {
  const response = await fetch(`/api/dialogs?user=${encodeURIComponent(user)}`)
  if (!response.ok) {
    throw new Error(await readError(response))
  }
  return response.json()
}

export async function fetchMessages(
  dialogId: number,
  user: string,
  after: number,
): Promise<ChatMessage[]> {
  const params = new URLSearchParams({ user, after: String(after) })
  const response = await fetch(`/api/dialogs/${dialogId}/messages?${params.toString()}`)
  if (!response.ok) {
    throw new Error(await readError(response))
  }
  return response.json()
}

export async function sendMessage(from: string, to: string, text: string): Promise<SentMessage> {
  const response = await fetch('/api/messages', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ from, to, text }),
  })
  if (!response.ok) {
    throw new Error(await readError(response))
  }
  return response.json()
}
