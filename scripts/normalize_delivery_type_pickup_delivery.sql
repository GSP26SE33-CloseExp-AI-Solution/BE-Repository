-- One-off: normalize Order.DeliveryType and DeliveryGroup.DeliveryType to PICKUP / DELIVERY (PostgreSQL).
-- Run after backup. Matches CloseExpAISolution.Domain.DeliveryMethod.NormalizeOrThrow aliases.

-- Preview before update:
-- SELECT DISTINCT "DeliveryType" FROM "Orders" ORDER BY 1;
-- SELECT DISTINCT "DeliveryType" FROM "DeliveryGroups" ORDER BY 1;

BEGIN;

UPDATE "Orders"
SET "DeliveryType" = 'PICKUP'
WHERE lower(replace(replace(replace(trim("DeliveryType"), '-', ''), '_', ''), ' ', '')) IN (
    'pickup',
    'collectionpoint',
    'storepickup'
);

UPDATE "Orders"
SET "DeliveryType" = 'DELIVERY'
WHERE lower(replace(replace(replace(trim("DeliveryType"), '-', ''), '_', ''), ' ', '')) IN (
    'delivery',
    'homedelivery'
);

UPDATE "DeliveryGroups"
SET "DeliveryType" = 'PICKUP'
WHERE lower(replace(replace(replace(trim("DeliveryType"), '-', ''), '_', ''), ' ', '')) IN (
    'pickup',
    'collectionpoint',
    'storepickup'
);

UPDATE "DeliveryGroups"
SET "DeliveryType" = 'DELIVERY'
WHERE lower(replace(replace(replace(trim("DeliveryType"), '-', ''), '_', ''), ' ', '')) IN (
    'delivery',
    'homedelivery'
);

COMMIT;

-- Rows still not PICKUP/DELIVERY (investigate manually):
-- SELECT "OrderId", "DeliveryType" FROM "Orders" WHERE "DeliveryType" NOT IN ('PICKUP', 'DELIVERY');
-- SELECT "DeliveryGroupId", "DeliveryType" FROM "DeliveryGroups" WHERE "DeliveryType" NOT IN ('PICKUP', 'DELIVERY');
