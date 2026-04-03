-- Browse TempEmail for temp email
-- email: limapid320@marvetos.com , fepeye3497@nexafilm.com
-- role: Vendor - 6
-- Pass: 123456

INSERT INTO public."Users"(
	"UserId", "FullName", "Email", "Phone", 
	"PasswordHash", 
	"RoleId", "Status", 
	"FailedLoginCount", "CreatedAt", "UpdatedAt",
	"OtpCode", "OtpExpiresAt", 
	"OtpFailedCount", "EmailVerifiedAt", "GoogleId")
VALUES
('12121212-1212-1212-1212-121212121212', 'Vendor Test', 'customer@example.com', '0910101123', '$2a$11$Amq4M7xennv3NS0z4ynetedbGW1HmGfoZtZhbu1SGgKlsQQWuttci', 6, 2, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, NULL, 0, '2026-03-29 13:50:00+07', NULL),
('10101010-1010-1010-1010-101010101010', 'Vendor Lock', 'limapid320@marvetos.com', '0910101010', '$2a$11$Amq4M7xennv3NS0z4ynetedbGW1HmGfoZtZhbu1SGgKlsQQWuttci', 6, 2, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, NULL, 0, '2026-03-29 13:50:00+07', NULL),
('11111111-1111-1111-1111-111111111111', 'Vendor Banned', 'fepeye3497@nexafilm.com', '0911111111','$2a$11$Amq4M7xennv3NS0z4ynetedbGW1HmGfoZtZhbu1SGgKlsQQWuttci', 6, 5, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, NULL, NULL, 0, '2026-03-29 13:50:00+07', NULL);