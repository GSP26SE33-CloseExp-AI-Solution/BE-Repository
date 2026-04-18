-- CloseExp — Chuẩn hóa dữ liệu cũ sau thay đổi StockLot.CreatedBy, NutritionFactsParser, API category/lots.
-- Chạy sau khi đã apply migration schema (ví dụ StockLotCreatedBy).
-- Idempotent: có thể chạy lại; các bước chỉ sửa hàng còn điều kiện.

BEGIN;

-- ---------------------------------------------------------------------------
-- 1) StockLots: UpdatedAt mặc định / rác (tránh timestamptz + Kind issues ở client cũ)
-- ---------------------------------------------------------------------------
UPDATE "StockLots" sl
SET "UpdatedAt" = sl."CreatedAt"
WHERE sl."UpdatedAt" < TIMESTAMPTZ '2000-01-01';

-- ---------------------------------------------------------------------------
-- 2) ProductDetails: NutritionFacts dạng plain text → JSON { "description": "..." }
--    (đồng bộ với parser BE; các tool đọc thẳng DB vẫn thấy JSON hợp lệ)
-- ---------------------------------------------------------------------------
UPDATE "ProductDetails" pd
SET "NutritionFacts" = (jsonb_build_object('description', pd."NutritionFacts"))::text
WHERE pd."NutritionFacts" IS NOT NULL
  AND btrim(pd."NutritionFacts") <> ''
  AND left(btrim(pd."NutritionFacts"), 1) <> '{';

-- ---------------------------------------------------------------------------
-- 3) Products: gán CategoryId còn NULL
--    3a) Cùng barcode đã có sản phẩm khác đã có category (ưu tiên cùng siêu thị)
-- ---------------------------------------------------------------------------
UPDATE "Products" p
SET "CategoryId" = src."CategoryId",
    "UpdatedAt"  = NOW()
FROM (
    SELECT DISTINCT ON (p2."Barcode", p2."SupermarketId")
        p2."Barcode",
        p2."SupermarketId",
        p2."CategoryId"
    FROM "Products" p2
    WHERE p2."CategoryId" IS NOT NULL
      AND p2."Barcode" IS NOT NULL
      AND btrim(p2."Barcode") <> ''
    ORDER BY p2."Barcode", p2."SupermarketId", p2."UpdatedAt" DESC NULLS LAST
) AS src
WHERE p."CategoryId" IS NULL
  AND p."Barcode" IS NOT NULL
  AND btrim(p."Barcode") <> ''
  AND p."Barcode" = src."Barcode"
  AND p."SupermarketId" = src."SupermarketId";

UPDATE "Products" p
SET "CategoryId" = src."CategoryId",
    "UpdatedAt"  = NOW()
FROM (
    SELECT DISTINCT ON (p2."Barcode")
        p2."Barcode",
        p2."CategoryId"
    FROM "Products" p2
    WHERE p2."CategoryId" IS NOT NULL
      AND p2."Barcode" IS NOT NULL
      AND btrim(p2."Barcode") <> ''
    ORDER BY p2."Barcode", p2."UpdatedAt" DESC NULLS LAST
) AS src
WHERE p."CategoryId" IS NULL
  AND p."Barcode" IS NOT NULL
  AND btrim(p."Barcode") <> ''
  AND p."Barcode" = src."Barcode";

--    3b) Gợi ý theo tên → một category active khớp (ORDER BY tên để ổn định)
-- ---------------------------------------------------------------------------
UPDATE "Products" p
SET "CategoryId" = c."CategoryId",
    "UpdatedAt"  = NOW()
FROM "Categories" c
WHERE p."CategoryId" IS NULL
  AND c."IsActive" = TRUE
  AND (p."Name" ILIKE '%heo%' OR p."Name" ILIKE '%bò%' OR p."Name" ILIKE '%thịt%' OR p."Name" ILIKE '%vissan%'
       OR p."Name" ILIKE '%cá%' OR p."Name" ILIKE '%tôm%' OR p."Name" ILIKE '%hải sản%')
  AND c."CategoryId" = (
      SELECT c3."CategoryId"
      FROM "Categories" c3
      WHERE c3."IsActive" = TRUE
        AND (c3."Name" ILIKE '%thịt%' OR c3."Name" ILIKE '%hải sản%' OR c3."Name" ILIKE '%meat%' OR c3."Name" ILIKE '%seafood%')
      ORDER BY c3."Name"
      LIMIT 1
  );

UPDATE "Products" p
SET "CategoryId" = c."CategoryId",
    "UpdatedAt"  = NOW()
FROM "Categories" c
WHERE p."CategoryId" IS NULL
  AND c."IsActive" = TRUE
  AND (
        p."Name" ILIKE '%mì%' OR p."Name" ILIKE '%phở%' OR p."Name" ILIKE '%bún%'
        OR p."Name" ILIKE '%bánh%' OR p."Name" ILIKE '%oreo%' OR p."Name" ILIKE '%snack%'
      )
  AND c."CategoryId" = (
      SELECT c3."CategoryId"
      FROM "Categories" c3
      WHERE c3."IsActive" = TRUE
        AND (c3."Name" ILIKE '%snack%' OR c3."Name" ILIKE '%bánh%' OR c3."Name" ILIKE '%ăn vặt%')
      ORDER BY c3."Name"
      LIMIT 1
  );

UPDATE "Products" p
SET "CategoryId" = c."CategoryId",
    "UpdatedAt"  = NOW()
FROM "Categories" c
WHERE p."CategoryId" IS NULL
  AND c."IsActive" = TRUE
  AND (
        p."Name" ILIKE '%nước%' OR p."Name" ILIKE '%bia%' OR p."Name" ILIKE '%coca%'
        OR p."Name" ILIKE '%pepsi%' OR p."Name" ILIKE '%trà%' OR p."Name" ILIKE '%sữa%'
      )
  AND c."CategoryId" = (
      SELECT c3."CategoryId"
      FROM "Categories" c3
      WHERE c3."IsActive" = TRUE
        AND (c3."Name" ILIKE '%nước%' OR c3."Name" ILIKE '%đồ uống%' OR c3."Name" ILIKE '%beverage%' OR c3."Name" ILIKE '%sữa%')
      ORDER BY c3."Name"
      LIMIT 1
  );

--    3c) Fallback: category active đầu tiên theo tên (ổn định)
-- ---------------------------------------------------------------------------
UPDATE "Products" p
SET "CategoryId" = fc."CategoryId",
    "UpdatedAt"  = NOW()
FROM (
    SELECT c."CategoryId"
    FROM "Categories" c
    WHERE c."IsActive" = TRUE
    ORDER BY
        CASE
            WHEN c."Name" ILIKE '%khác%' THEN 0
            WHEN c."Name" ILIKE '%other%' THEN 0
            WHEN c."Name" ILIKE '%tổng%' THEN 1
            ELSE 2
        END,
        c."Name"
    LIMIT 1
) AS fc
WHERE p."CategoryId" IS NULL
  AND EXISTS (SELECT 1 FROM "Categories" c WHERE c."IsActive" = TRUE);

COMMIT;
