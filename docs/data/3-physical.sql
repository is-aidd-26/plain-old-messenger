-- СУБД: PostgreSQL 18.
--
-- Решения физического уровня:
--   * ключи — bigint GENERATED ALWAYS AS IDENTITY: глобальная монотонная
--     последовательность пригодна как курсор поллинга `after`;
--   * трассируемость по минимуму: created_at у users и dialogs, sent_at
--     у messages с DEFAULT now() как страховкой (момент задаёт приложение);
--   * удаление не предусмотрено, все FK — ON DELETE RESTRICT;
--   * бизнес-правила, не выражаемые декларативно, остаются на приложении:
--     ровно два участника диалога (допускается диалог с самим собой),
--     уникальность состава участников, автор сообщения — участник диалога,
--     диалог создаётся вместе с первым сообщением.

CREATE TABLE users (
    id          bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nickname    text        NOT NULL,  -- регистр значим: «Bob» и «bob» — разные пользователи
    created_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT users_nickname_key UNIQUE (nickname),
    CONSTRAINT users_nickname_len CHECK (char_length(nickname) BETWEEN 1 AND 64),
    CONSTRAINT users_nickname_not_blank CHECK (btrim(nickname) <> '')
);

CREATE TABLE dialogs (
    id          bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE dialog_participants (
    dialog_id   bigint      NOT NULL,
    user_id     bigint      NOT NULL,

    PRIMARY KEY (dialog_id, user_id),
    FOREIGN KEY (dialog_id) REFERENCES dialogs (id) ON DELETE RESTRICT,
    FOREIGN KEY (user_id)  REFERENCES users (id)   ON DELETE RESTRICT
);

-- Под запрос «диалоги пользователя».
CREATE INDEX dialog_participants_user_id_idx ON dialog_participants (user_id);

CREATE TABLE messages (
    id          bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    dialog_id   bigint      NOT NULL,
    author_id   bigint      NOT NULL,
    text        text        NOT NULL,  -- trim и непустоту гарантирует приложение
    sent_at     timestamptz NOT NULL DEFAULT now(),  -- UTC; DEFAULT — страховка, значение задаёт приложение

    FOREIGN KEY (dialog_id) REFERENCES dialogs (id) ON DELETE RESTRICT,
    FOREIGN KEY (author_id) REFERENCES users (id)  ON DELETE RESTRICT,
    CONSTRAINT messages_text_len CHECK (char_length(text) BETWEEN 1 AND 500)
);

-- Под историю диалога и поллинг `after`: сообщения диалога по возрастанию id.
CREATE INDEX messages_dialog_id_id_idx ON messages (dialog_id, id);
