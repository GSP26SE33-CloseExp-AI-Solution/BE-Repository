-- Optional migration: copy legacy Order.DeliveryGroupId onto every OrderItem when
-- OrderItem.DeliveryGroupId is null (single-group-per-order deployments).
-- Review in staging before production; run inside a transaction.
--
-- PostgreSQL (EF table names use double-quoted identifiers in migrations).

BEGIN;

UPDATE "OrderItems" AS oi
SET "DeliveryGroupId" = o."DeliveryGroupId"
FROM "Orders" AS o
WHERE oi."OrderId" = o."OrderId"
  AND oi."DeliveryGroupId" IS NULL
  AND o."DeliveryGroupId" IS NOT NULL;

COMMIT;
