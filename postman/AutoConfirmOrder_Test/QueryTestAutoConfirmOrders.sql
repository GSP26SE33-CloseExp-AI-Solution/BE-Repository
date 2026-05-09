-- trước khi đợi job
select "OrderId","Status" from "Orders"
where "OrderId"='ffff0002-0002-0002-0002-000000000002';

select "OrderId","DeliveryStatus","DeliveredAt"
from "OrderItems"
where "OrderId"='ffff0002-0002-0002-0002-000000000002';

-- Sau 1 vòng job (15 phút), kỳ vọng:
-- Orders.Status -> Completed (4)
-- OrderItems.DeliveryStatus -> Completed (5)

-- =======================
-- 1) ép order vào DeliveredWaitConfirm
update "Orders"
set "Status" = 3,         -- OrderState.DeliveredWaitConfirm
    "UpdatedAt" = now()
where "OrderId" = 'ffff0002-0002-0002-0002-000000000002';

-- 2) ép các line item vào DeliveredWaitConfirm + DeliveredAt cũ hơn cutoff
update "OrderItems"
set "DeliveryStatus" = 3, -- DeliveryState.DeliveredWaitConfirm
    "DeliveredAt" = now() - interval '3 days'
where "OrderId" = 'ffff0002-0002-0002-0002-000000000002';

