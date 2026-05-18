-- Seed data for CompletePackagingAsync testcases
-- Requires existing seed data for users, time slots, collection points, and stock lots.

DO $$
DECLARE
  lot1 uuid;
  lot2 uuid;
  lot3 uuid;
BEGIN
  SELECT "LotId" INTO lot1 FROM "StockLots" WHERE "Status" = 3 ORDER BY "ExpiryDate" LIMIT 1;
  SELECT "LotId" INTO lot2 FROM "StockLots" WHERE "Status" = 3 ORDER BY "ExpiryDate" OFFSET 1 LIMIT 1;
  SELECT "LotId" INTO lot3 FROM "StockLots" WHERE "Status" = 3 ORDER BY "ExpiryDate" OFFSET 2 LIMIT 1;

  IF lot1 IS NULL OR lot2 IS NULL THEN
    RAISE NOTICE 'Not enough published lots to seed CompletePackagingAsync data.';
    RETURN;
  END IF;

  -- Order: normal complete packaging (Paid, Pending/Packaging items)
  IF NOT EXISTS (SELECT 1 FROM "Orders" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000001') THEN
    INSERT INTO "Orders" (
      "OrderId", "OrderCode", "UserId", "TimeSlotId", "CollectionId", "AddressId",
      "DeliveryType", "TotalAmount", "DiscountAmount", "FinalAmount", "DeliveryFee",
      "SystemUsageFeeAmount", "Status", "OrderDate", "DeliveryNote", "CreatedAt", "UpdatedAt"
    ) VALUES (
      'ffff0100-0000-0000-0000-000000000001', 'PKG-COMP-OK-001',
      'ffffffff-0000-0000-0000-000000000000', 'cccc0001-0001-0001-0001-000000000001',
      'dddd0001-0001-0001-0001-000000000001', NULL, 'Pickup',
      120000, 0, 120000, 0, 0, 1, now(), 'seed for complete packaging ok', now(), now()
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-000000000001') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-000000000001', 'ffff0100-0000-0000-0000-000000000001',
      lot1, 1, 50000, 50000, 0, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-000000000002') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-000000000002', 'ffff0100-0000-0000-0000-000000000001',
      lot2, 1, 70000, 70000, 1, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderPackaging" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000001' AND "OrderItemId" = 'ffff1100-0000-0000-0000-000000000001') THEN
    INSERT INTO "OrderPackaging" ("PackagingId", "OrderId", "OrderItemId", "UserId", "Status", "PackagedAt")
    VALUES ('ffff2100-0000-0000-0000-000000000001', 'ffff0100-0000-0000-0000-000000000001',
      'ffff1100-0000-0000-0000-000000000001', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 0, NULL);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderPackaging" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000001' AND "OrderItemId" = 'ffff1100-0000-0000-0000-000000000002') THEN
    INSERT INTO "OrderPackaging" ("PackagingId", "OrderId", "OrderItemId", "UserId", "Status", "PackagedAt")
    VALUES ('ffff2100-0000-0000-0000-000000000002', 'ffff0100-0000-0000-0000-000000000001',
      'ffff1100-0000-0000-0000-000000000002', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 1, NULL);
  END IF;

  -- Order: empty list test (Paid, Pending/Packaging items)
  IF NOT EXISTS (SELECT 1 FROM "Orders" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000002') THEN
    INSERT INTO "Orders" (
      "OrderId", "OrderCode", "UserId", "TimeSlotId", "CollectionId", "AddressId",
      "DeliveryType", "TotalAmount", "DiscountAmount", "FinalAmount", "DeliveryFee",
      "SystemUsageFeeAmount", "Status", "OrderDate", "DeliveryNote", "CreatedAt", "UpdatedAt"
    ) VALUES (
      'ffff0100-0000-0000-0000-000000000002', 'PKG-COMP-EMPTY-001',
      'ffffffff-0000-0000-0000-000000000000', 'cccc0001-0001-0001-0001-000000000001',
      'dddd0001-0001-0001-0001-000000000001', NULL, 'Pickup',
      100000, 0, 100000, 0, 0, 1, now(), 'seed for complete packaging empty list', now(), now()
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-000000000003') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-000000000003', 'ffff0100-0000-0000-0000-000000000002',
      lot1, 1, 40000, 40000, 0, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-000000000004') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-000000000004', 'ffff0100-0000-0000-0000-000000000002',
      lot2, 1, 60000, 60000, 1, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderPackaging" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000002' AND "OrderItemId" = 'ffff1100-0000-0000-0000-000000000003') THEN
    INSERT INTO "OrderPackaging" ("PackagingId", "OrderId", "OrderItemId", "UserId", "Status", "PackagedAt")
    VALUES ('ffff2100-0000-0000-0000-000000000003', 'ffff0100-0000-0000-0000-000000000002',
      'ffff1100-0000-0000-0000-000000000003', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 0, NULL);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderPackaging" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000002' AND "OrderItemId" = 'ffff1100-0000-0000-0000-000000000004') THEN
    INSERT INTO "OrderPackaging" ("PackagingId", "OrderId", "OrderItemId", "UserId", "Status", "PackagedAt")
    VALUES ('ffff2100-0000-0000-0000-000000000004', 'ffff0100-0000-0000-0000-000000000002',
      'ffff1100-0000-0000-0000-000000000004', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 1, NULL);
  END IF;

  -- Order: partial packaging (Paid, 3 items, only 2 targeted)
  IF NOT EXISTS (SELECT 1 FROM "Orders" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000003') THEN
    INSERT INTO "Orders" (
      "OrderId", "OrderCode", "UserId", "TimeSlotId", "CollectionId", "AddressId",
      "DeliveryType", "TotalAmount", "DiscountAmount", "FinalAmount", "DeliveryFee",
      "SystemUsageFeeAmount", "Status", "OrderDate", "DeliveryNote", "CreatedAt", "UpdatedAt"
    ) VALUES (
      'ffff0100-0000-0000-0000-000000000003', 'PKG-COMP-PARTIAL-001',
      'ffffffff-0000-0000-0000-000000000000', 'cccc0001-0001-0001-0001-000000000001',
      'dddd0001-0001-0001-0001-000000000001', NULL, 'Pickup',
      100000, 0, 100000, 0, 0, 1, now(), 'seed for partial complete packaging', now(), now()
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-000000000005') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-000000000005', 'ffff0100-0000-0000-0000-000000000003',
      lot1, 1, 30000, 30000, 0, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-000000000006') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-000000000006', 'ffff0100-0000-0000-0000-000000000003',
      lot2, 1, 45000, 45000, 1, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-000000000007') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-000000000007', 'ffff0100-0000-0000-0000-000000000003',
      COALESCE(lot3, lot1), 1, 25000, 25000, 0, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderPackaging" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000003' AND "OrderItemId" = 'ffff1100-0000-0000-0000-000000000005') THEN
    INSERT INTO "OrderPackaging" ("PackagingId", "OrderId", "OrderItemId", "UserId", "Status", "PackagedAt")
    VALUES ('ffff2100-0000-0000-0000-000000000005', 'ffff0100-0000-0000-0000-000000000003',
      'ffff1100-0000-0000-0000-000000000005', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 0, NULL);
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderPackaging" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000003' AND "OrderItemId" = 'ffff1100-0000-0000-0000-000000000006') THEN
    INSERT INTO "OrderPackaging" ("PackagingId", "OrderId", "OrderItemId", "UserId", "Status", "PackagedAt")
    VALUES ('ffff2100-0000-0000-0000-000000000006', 'ffff0100-0000-0000-0000-000000000003',
      'ffff1100-0000-0000-0000-000000000006', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 1, NULL);
  END IF;

  -- Order: record owned by other staff
  IF NOT EXISTS (SELECT 1 FROM "Orders" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000004') THEN
    INSERT INTO "Orders" (
      "OrderId", "OrderCode", "UserId", "TimeSlotId", "CollectionId", "AddressId",
      "DeliveryType", "TotalAmount", "DiscountAmount", "FinalAmount", "DeliveryFee",
      "SystemUsageFeeAmount", "Status", "OrderDate", "DeliveryNote", "CreatedAt", "UpdatedAt"
    ) VALUES (
      'ffff0100-0000-0000-0000-000000000004', 'PKG-COMP-FOREIGN-001',
      'ffffffff-0000-0000-0000-000000000000', 'cccc0001-0001-0001-0001-000000000001',
      'dddd0001-0001-0001-0001-000000000001', NULL, 'Pickup',
      55000, 0, 55000, 0, 0, 1, now(), 'seed for foreign record owner', now(), now()
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-000000000008') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-000000000008', 'ffff0100-0000-0000-0000-000000000004',
      lot1, 1, 55000, 55000, 0, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderPackaging" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000004' AND "OrderItemId" = 'ffff1100-0000-0000-0000-000000000008') THEN
    INSERT INTO "OrderPackaging" ("PackagingId", "OrderId", "OrderItemId", "UserId", "Status", "PackagedAt")
    VALUES ('ffff2100-0000-0000-0000-000000000007', 'ffff0100-0000-0000-0000-000000000004',
      'ffff1100-0000-0000-0000-000000000008', 'cccccccc-cccc-cccc-cccc-cccccccccccc', 0, NULL);
  END IF;

  -- Order: failed item status
  IF NOT EXISTS (SELECT 1 FROM "Orders" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000005') THEN
    INSERT INTO "Orders" (
      "OrderId", "OrderCode", "UserId", "TimeSlotId", "CollectionId", "AddressId",
      "DeliveryType", "TotalAmount", "DiscountAmount", "FinalAmount", "DeliveryFee",
      "SystemUsageFeeAmount", "Status", "OrderDate", "DeliveryNote", "CreatedAt", "UpdatedAt"
    ) VALUES (
      'ffff0100-0000-0000-0000-000000000005', 'PKG-COMP-FAILED-001',
      'ffffffff-0000-0000-0000-000000000000', 'cccc0001-0001-0001-0001-000000000001',
      'dddd0001-0001-0001-0001-000000000001', NULL, 'Pickup',
      65000, 0, 65000, 0, 0, 1, now(), 'seed for failed item status', now(), now()
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-000000000009') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-000000000009', 'ffff0100-0000-0000-0000-000000000005',
      lot2, 1, 65000, 65000, 3, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  -- Order: not paid status
  IF NOT EXISTS (SELECT 1 FROM "Orders" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000006') THEN
    INSERT INTO "Orders" (
      "OrderId", "OrderCode", "UserId", "TimeSlotId", "CollectionId", "AddressId",
      "DeliveryType", "TotalAmount", "DiscountAmount", "FinalAmount", "DeliveryFee",
      "SystemUsageFeeAmount", "Status", "OrderDate", "DeliveryNote", "CreatedAt", "UpdatedAt"
    ) VALUES (
      'ffff0100-0000-0000-0000-000000000006', 'PKG-COMP-NOTPAID-001',
      'ffffffff-0000-0000-0000-000000000000', 'cccc0001-0001-0001-0001-000000000001',
      'dddd0001-0001-0001-0001-000000000001', NULL, 'Pickup',
      75000, 0, 75000, 0, 0, 0, now(), 'seed for not paid status', now(), now()
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-00000000000a') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-00000000000a', 'ffff0100-0000-0000-0000-000000000006',
      lot1, 1, 75000, 75000, 0, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  -- Order: no items
  IF NOT EXISTS (SELECT 1 FROM "Orders" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000007') THEN
    INSERT INTO "Orders" (
      "OrderId", "OrderCode", "UserId", "TimeSlotId", "CollectionId", "AddressId",
      "DeliveryType", "TotalAmount", "DiscountAmount", "FinalAmount", "DeliveryFee",
      "SystemUsageFeeAmount", "Status", "OrderDate", "DeliveryNote", "CreatedAt", "UpdatedAt"
    ) VALUES (
      'ffff0100-0000-0000-0000-000000000007', 'PKG-COMP-NOITEM-001',
      'ffffffff-0000-0000-0000-000000000000', 'cccc0001-0001-0001-0001-000000000001',
      'dddd0001-0001-0001-0001-000000000001', NULL, 'Pickup',
      0, 0, 0, 0, 0, 1, now(), 'seed for no items', now(), now()
    );
  END IF;

  -- Order: ready to ship idempotent
  IF NOT EXISTS (SELECT 1 FROM "Orders" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000008') THEN
    INSERT INTO "Orders" (
      "OrderId", "OrderCode", "UserId", "TimeSlotId", "CollectionId", "AddressId",
      "DeliveryType", "TotalAmount", "DiscountAmount", "FinalAmount", "DeliveryFee",
      "SystemUsageFeeAmount", "Status", "OrderDate", "DeliveryNote", "CreatedAt", "UpdatedAt"
    ) VALUES (
      'ffff0100-0000-0000-0000-000000000008', 'PKG-COMP-READY-001',
      'ffffffff-0000-0000-0000-000000000000', 'cccc0001-0001-0001-0001-000000000001',
      'dddd0001-0001-0001-0001-000000000001', NULL, 'Pickup',
      110000, 0, 110000, 0, 0, 2, now(), 'seed for ready idempotent', now(), now()
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-00000000000b') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-00000000000b', 'ffff0100-0000-0000-0000-000000000008',
      lot1, 1, 50000, 50000, 2, 0, now() - interval '1 hour', NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-00000000000c') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-00000000000c', 'ffff0100-0000-0000-0000-000000000008',
      lot2, 1, 60000, 60000, 2, 0, now() - interval '1 hour', NULL, NULL, NULL, NULL
    );
  END IF;

  -- Order: ready to ship but not all completed
  IF NOT EXISTS (SELECT 1 FROM "Orders" WHERE "OrderId" = 'ffff0100-0000-0000-0000-000000000009') THEN
    INSERT INTO "Orders" (
      "OrderId", "OrderCode", "UserId", "TimeSlotId", "CollectionId", "AddressId",
      "DeliveryType", "TotalAmount", "DiscountAmount", "FinalAmount", "DeliveryFee",
      "SystemUsageFeeAmount", "Status", "OrderDate", "DeliveryNote", "CreatedAt", "UpdatedAt"
    ) VALUES (
      'ffff0100-0000-0000-0000-000000000009', 'PKG-COMP-READY-PARTIAL-001',
      'ffffffff-0000-0000-0000-000000000000', 'cccc0001-0001-0001-0001-000000000001',
      'dddd0001-0001-0001-0001-000000000001', NULL, 'Pickup',
      110000, 0, 110000, 0, 0, 2, now(), 'seed for ready partial', now(), now()
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-00000000000d') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-00000000000d', 'ffff0100-0000-0000-0000-000000000009',
      lot1, 1, 50000, 50000, 0, NULL, NULL, NULL, NULL, NULL, NULL
    );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM "OrderItems" WHERE "OrderItemId" = 'ffff1100-0000-0000-0000-00000000000e') THEN
    INSERT INTO "OrderItems" (
      "OrderItemId", "OrderId", "LotId", "Quantity", "UnitPrice", "TotalPrice",
      "PackagingStatus", "DeliveryStatus", "PackagedAt", "DeliveredAt",
      "PackagingFailedReason", "DeliveryFailedReason", "DeliveryGroupId"
    ) VALUES (
      'ffff1100-0000-0000-0000-00000000000e', 'ffff0100-0000-0000-0000-000000000009',
      lot2, 1, 60000, 60000, 2, 0, now() - interval '1 hour', NULL, NULL, NULL, NULL
    );
  END IF;
END $$;
