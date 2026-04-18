SELECT COUNT(*)::bigint AS total_market_price_rows FROM "MarketPrices";

SELECT mp."Barcode", COUNT(*)::int AS n,
       MIN(mp."Price")::numeric(12,2) AS min_p,
       ROUND(AVG(mp."Price")::numeric, 2) AS avg_p,
       MAX(mp."Price")::numeric(12,2) AS max_p
FROM "MarketPrices" mp
GROUP BY mp."Barcode"
ORDER BY n DESC
LIMIT 10;

-- Barcodes có cả Product verified (supermarket supplier) để test API
SELECT p."ProductId", p."Name", p."Barcode", p."Status"
FROM "Products" p
WHERE p."Barcode" IN (SELECT mp."Barcode" FROM "MarketPrices" mp GROUP BY mp."Barcode" HAVING COUNT(*) >= 1)
  AND p."SupermarketId" = '33333333-3333-3333-3333-333333333333'
LIMIT 5;
