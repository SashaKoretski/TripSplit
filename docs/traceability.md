# Трассировка требований

Связь сценариев, требований, сущностей и экранов - основа для проектирования
REST API и тестов.

| Scenario ID | Requirement ID | Entity | Screen | Acceptance criteria |
|---|---|---|---|---|
| SC-001 | FR-004, FR-007, FR-008, BR-01 | Trip, User | `Trips/Index` (Create) | SC-001-AC1, SC-001-AC2 |
| SC-001 | FR-005 | Trip, User | `Trips/Index` (Join) | SC-001-AC1 |
| SC-001 | FR-006, BR-09 | Trip, User | `Trips/Details` (Invite) | SC-001-AC3, SC-001-AC4 |
| SC-002 | FR-009, BR-02, BR-03, BR-04, BR-10, BR-11 | Expense, User | `Expenses/Create`, `Expenses/Index` | SC-002-AC1, SC-002-AC3 |
| SC-002 | FR-012, BR-07 | Receipt | `Receipts/Create` | SC-002-AC4 |
| SC-002 | FR-013 | ReceiptImage | `Receipts/Create`, `Expenses/Create` | SC-002-AC4 |
| SC-002 | FR-014 | Expense, Receipt | `Receipts/AttachExpenses` | SC-002-AC4 |
| SC-002 | FR-015 | Expense, Receipt, ReceiptImage | `Expenses/Create` | SC-002-AC4 |
| SC-002 | BR-05 | Trip, Expense | `Expenses/Create` | SC-002-AC2 |
| SC-003 | FR-016, BR-05 | Trip | `Trips/Details` (Finish) | SC-003-AC1, SC-003-AC4 |
| SC-003 | FR-017, BR-06 | Expense, Transfer, TripStatistics | `Settlement/Index` | SC-003-AC2, SC-003-AC3 |

## Покрытие требований сценариями

Все FR-001..FR-017 задействованы хотя бы в одном сценарии, кроме:

- FR-001, FR-002, FR-003 (регистрация/вход/выход) - предусловия ко всем трем сценариям, отдельным сценарием не выделены, так как это стандартный auth-флоу без ветвлений, специфичных для домена TripSplit.
- FR-011 (просмотр списка трат) - подмножество основного потока SC-002 (шаг 5), отдельно не трассируется.

Это сознательное решение, а не пробел: сценарии SC-001..SC-003 выбраны как
доменно значимые (см. `README.md`, раздел "Документация").
