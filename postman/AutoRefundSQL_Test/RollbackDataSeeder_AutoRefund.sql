-- SELECT "OrderCode","Status" FROM "Orders"
-- WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003';

-- SELECT "RefundId","Status","Amount","Reason"
-- FROM "Refunds"
-- WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003'
-- ORDER BY "CreatedAt" DESC;

BEGIN;

-- ========= 0) Khôi phục config N về seed =========
-- DataSeeder hiện để ORDER_READY_TO_SHIP_MAX_WAIT_MINUTES = 90
UPDATE "SystemConfigs"
SET "ConfigValue" = '90',
    "UpdatedAt" = NOW()
WHERE "ConfigKey" = 'ORDER_READY_TO_SHIP_MAX_WAIT_MINUTES';

-- ========= 1) Dọn refund phát sinh trong lúc test =========
-- Giữ lại refund seed completed có RefundId cố định
DELETE FROM "Refunds"
WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003'
  AND "RefundId" <> 'fffa2222-4444-4444-4444-444444444444';

-- Nếu refund seed completed bị xóa trước đó thì tạo lại
INSERT INTO "Refunds"
("RefundId","OrderId","TransactionId","Amount","Reason","Status","ProcessedBy","ProcessedAt","CreatedAt")
SELECT
  'fffa2222-4444-4444-4444-444444444444'::uuid,
  'ffff0003-0003-0003-0003-000000000003'::uuid,
  'fffa1111-3333-3333-3333-333333333333'::uuid,
  15000,
  '[Seed] Hoàn tiền sau khi đóng gói — đã chuyển khoản',
  3, -- RefundState.Completed
  'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
  NOW() - INTERVAL '1 hour',
  NOW() - INTERVAL '3 hours'
WHERE NOT EXISTS (
  SELECT 1
  FROM "Refunds"
  WHERE "RefundId" = 'fffa2222-4444-4444-4444-444444444444'
);

-- ========= 2) Dọn status log test =========
DELETE FROM "OrderStatusLogs"
WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003'
  AND (
    "Note" ILIKE 'Seed RTS for auto-refund test%'
    OR "Note" ILIKE 'Hệ thống tự hoàn tiền:%'
  );

-- ========= 3) Trả order về trạng thái seed gần đúng =========
UPDATE "Orders"
SET "Status" = 2, -- OrderState.ReadyToShip
    "UpdatedAt" = NOW() - INTERVAL '1 hour'
WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003';

-- ========= 4) Đảm bảo transaction vẫn Paid =========
UPDATE "Transactions"
SET "PaymentStatus" = 1 -- PaymentState.Paid
WHERE "TransactionId" = 'fffa1111-3333-3333-3333-333333333333';

COMMIT;