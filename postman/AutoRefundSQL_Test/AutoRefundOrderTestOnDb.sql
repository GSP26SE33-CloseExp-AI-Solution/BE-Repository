BEGIN;

-- 0) Chọn N nhỏ để test nhanh (1 phút)
UPDATE "SystemConfigs"
SET "ConfigValue" = '1', "UpdatedAt" = NOW()
WHERE "ConfigKey" = 'ORDER_READY_TO_SHIP_MAX_WAIT_MINUTES';

-- 1) Chọn order seed dùng làm test
-- PKG-READY-001 = ffff0003-0003-0003-0003-000000000003
-- Đảm bảo order ở ReadyToShip (enum int: ReadyToShip = 2)
UPDATE "Orders"
SET "Status" = 2,
    "UpdatedAt" = NOW() - INTERVAL '2 hours'
WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003';

-- 2) Xóa refund cũ để không bị guard "đã có refund active"
DELETE FROM "Refunds"
WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003';

-- 3) Đảm bảo có transaction Paid cho order này (PaymentState.Paid = 1)
UPDATE "Transactions"
SET "PaymentStatus" = 1,
    "UpdatedAt" = NOW() - INTERVAL '2 hours'
WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003';

-- 4) Tạo log mốc vào ReadyToShip đủ cũ (processor cần log này)
INSERT INTO "OrderStatusLogs"
("LogId","OrderId","FromStatus","ToStatus","ChangedBy","Note","ChangedAt")
VALUES
(gen_random_uuid(),'ffff0003-0003-0003-0003-000000000003',1,2,'manual-test','Seed RTS for auto-refund test', NOW() - INTERVAL '2 hours');

COMMIT;


-- Kiểm tra ==================
-- A) Order đã chuyển Refunded chưa? (Refunded = 6)
SELECT "OrderId","OrderCode","Status","UpdatedAt"
FROM "Orders"
WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003';

-- B) Có refund mới Pending chưa? (Pending = 0)
SELECT "RefundId","OrderId","TransactionId","Amount","Status","Reason","CreatedAt"
FROM "Refunds"
WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003'
ORDER BY "CreatedAt" DESC;

-- C) Có status log RTS -> Refunded + note chưa?
SELECT "OrderId","FromStatus","ToStatus","Note","ChangedAt"
FROM "OrderStatusLogs"
WHERE "OrderId" = 'ffff0003-0003-0003-0003-000000000003'
ORDER BY "ChangedAt" DESC;