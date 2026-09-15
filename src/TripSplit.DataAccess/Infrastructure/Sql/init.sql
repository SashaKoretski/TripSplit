CREATE TABLE IF NOT EXISTS users (
    id        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name      VARCHAR(100) NOT NULL,
    email     VARCHAR(255) NOT NULL UNIQUE,
    google_id VARCHAR(255) NOT NULL UNIQUE
);
CREATE INDEX IF NOT EXISTS idx_users_google_id ON users(google_id);
CREATE INDEX IF NOT EXISTS idx_users_email     ON users(email);

CREATE TABLE IF NOT EXISTS trips (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name       VARCHAR(200) NOT NULL,
    status     VARCHAR(20)  NOT NULL DEFAULT 'active'
                            CHECK (status IN ('active','finished')),
    currency   CHAR(3)      NOT NULL DEFAULT 'RUB',
    created_at TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ  NOT NULL DEFAULT now(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS trip_participants (
    trip_id   UUID NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
    user_id   UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (trip_id, user_id)
);
CREATE INDEX IF NOT EXISTS idx_trip_participants_user_id ON trip_participants(user_id);

CREATE TABLE IF NOT EXISTS receipts (
    id       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    trip_id  UUID NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
    name     VARCHAR(200) NOT NULL,
    date     DATE NOT NULL
);

CREATE TABLE IF NOT EXISTS receipt_images (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    receipt_id          UUID NOT NULL UNIQUE REFERENCES receipts(id) ON DELETE CASCADE,
    storage_key         VARCHAR(500) NOT NULL,
    content_type        VARCHAR(100) NOT NULL,
    file_size_bytes     BIGINT       NOT NULL,
    original_file_name  VARCHAR(255) NOT NULL,
    uploaded_at         TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS expenses (
    id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    trip_id    UUID NOT NULL REFERENCES trips(id) ON DELETE CASCADE,
    receipt_id UUID REFERENCES receipts(id) ON DELETE SET NULL,
    payer_id   UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    name       VARCHAR(200)  NOT NULL,
    type       VARCHAR(30)   NOT NULL DEFAULT 'other'
                             CHECK (type IN ('food','transport','accommodation','entertainment','other')),
    value      DECIMAL(12,2) NOT NULL,
    discount   DECIMAL(12,2) NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ   NOT NULL DEFAULT now(),
    deleted_at TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS idx_expenses_trip_id    ON expenses(trip_id);
CREATE INDEX IF NOT EXISTS idx_expenses_payer_id   ON expenses(payer_id);
CREATE INDEX IF NOT EXISTS idx_expenses_receipt_id ON expenses(receipt_id);

CREATE TABLE IF NOT EXISTS expense_consumers (
    expense_id UUID NOT NULL REFERENCES expenses(id) ON DELETE CASCADE,
    user_id    UUID NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    PRIMARY KEY (expense_id, user_id)
);
CREATE INDEX IF NOT EXISTS idx_expense_consumers_user_id ON expense_consumers(user_id);