-- Browse TempEmail for temp email
-- email: xapaw40576@smkanba.com , padix86801@smkanba.com
-- role: Vendor - 6
-- Pass: Test@1234

INSERT INTO public."Users"(
	"UserId", "FullName", "Email", "Phone", 
	"PasswordHash", 
	"RoleId", "Status", 
	"FailedLoginCount", "CreatedAt", 
	"UpdatedAt", "OtpCode", "OtpExpiresAt", 
	"OtpFailedCount", "EmailVerifiedAt", "GoogleId")
VALUES
('10101010-1010-1010-1010-101010101010', 'Vendor For Lock', 'xapaw40576@smkanba.com', '0910101010', '$2a$11$eImiTXuWVxfM37uY4JANjO.S9X96Ym8bX3.M6C3V9K6.K6S.S6S6S', 6, 2, 0, '2026-03-29 13:50:00+07', '2026-03-29 13:50:00+07', NULL, NULL, 0, '2026-03-29 13:50:00+07', NULL),
('11111111-1111-1111-1111-111111111111', 'Vendor Banned', 'padix86801@smkanba.com', '0911111111', '$2a$11$eImiTXuWVxfM37uY4JANjO.S9X96Ym8bX3.M6C3V9K6.K6S.S6S6S', 6, 5, 0, '2026-03-29 13:50:00+07', '2026-03-29 13:50:00+07', NULL, NULL, 0, '2026-03-29 13:50:00+07', NULL);