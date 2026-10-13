-- Откат тестового хранилища к состоянию "до прогона тестов" (ТИОПО ЛР2, Т4/Т10).
-- Запускается перед каждым интеграционным/E2E-тестом и в аварийном cleanup после сбоя.
-- RESTART IDENTITY не нужен (все id - UUID), но сохраняем для единообразия с последовательностями,
-- если они появятся. CASCADE - чтобы не заботиться о порядке таблиц со ссылками по FK.
TRUNCATE expense_consumers, expenses, receipt_images, receipts, trip_participants, trips, users
    RESTART IDENTITY CASCADE;
