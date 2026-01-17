-- ============================================
-- CorpProcure Sample Data Seed Script
-- Jalankan di SQL Server untuk populate test data
-- ============================================

-- Hapus data lama (urutan penting karena foreign key)
DELETE FROM ApprovalHistories;
DELETE FROM RequestItems;
DELETE FROM PurchaseOrderItems;
DELETE FROM PurchaseOrders;
DELETE FROM PurchaseRequests;
DELETE FROM VendorItems;
DELETE FROM Items;
DELETE FROM ItemCategories;
DELETE FROM Budgets;
DELETE FROM Vendors;
DELETE FROM AspNetUserRoles;
DELETE FROM AspNetUsers WHERE Email != 'admin@corpprocure.com';
DELETE FROM Departments;

-- ============================================
-- 1. DEPARTMENTS
-- ============================================
DECLARE @DeptIT UNIQUEIDENTIFIER = NEWID();
DECLARE @DeptHR UNIQUEIDENTIFIER = NEWID();
DECLARE @DeptFinance UNIQUEIDENTIFIER = NEWID();
DECLARE @DeptOperations UNIQUEIDENTIFIER = NEWID();
DECLARE @DeptMarketing UNIQUEIDENTIFIER = NEWID();

INSERT INTO Departments (Id, Name, Code, Description, IsActive, CreatedAt) VALUES
(@DeptIT, 'Information Technology', 'IT', 'Departemen IT dan Pengembangan Sistem', 1, GETUTCDATE()),
(@DeptHR, 'Human Resources', 'HR', 'Departemen SDM dan Kepegawaian', 1, GETUTCDATE()),
(@DeptFinance, 'Finance & Accounting', 'FIN', 'Departemen Keuangan dan Akuntansi', 1, GETUTCDATE()),
(@DeptOperations, 'Operations', 'OPS', 'Departemen Operasional', 1, GETUTCDATE()),
(@DeptMarketing, 'Marketing', 'MKT', 'Departemen Pemasaran', 1, GETUTCDATE());

-- ============================================
-- 2. USERS (Password: Password123!)
-- ============================================
-- Password hash for 'Password123!' (ASP.NET Identity)
DECLARE @PasswordHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEKz6HHxF2PxJXhQXAJXqDNGVFnR8YqPeR7KqW+R7XSjGKJKc8Z9NvPCJnK0LN8XTYA==';

-- Staff Users
DECLARE @StaffIT1 UNIQUEIDENTIFIER = NEWID();
DECLARE @StaffIT2 UNIQUEIDENTIFIER = NEWID();
DECLARE @StaffHR1 UNIQUEIDENTIFIER = NEWID();
DECLARE @StaffOps1 UNIQUEIDENTIFIER = NEWID();

-- Manager Users
DECLARE @ManagerIT UNIQUEIDENTIFIER = NEWID();
DECLARE @ManagerHR UNIQUEIDENTIFIER = NEWID();
DECLARE @ManagerOps UNIQUEIDENTIFIER = NEWID();

-- Finance Users  
DECLARE @FinanceUser1 UNIQUEIDENTIFIER = NEWID();
DECLARE @FinanceUser2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, EmployeeId, DepartmentId, IsActive, CreatedAt) VALUES
-- Staff IT
(@StaffIT1, 'budi.santoso@corpprocure.com', 'BUDI.SANTOSO@CORPPROCURE.COM', 'budi.santoso@corpprocure.com', 'BUDI.SANTOSO@CORPPROCURE.COM', 1, @PasswordHash, NEWID(), NEWID(), '081234567001', 0, 0, 1, 0, 'Budi Santoso', 'EMP-IT-001', @DeptIT, 1, GETUTCDATE()),
(@StaffIT2, 'andi.wijaya@corpprocure.com', 'ANDI.WIJAYA@CORPPROCURE.COM', 'andi.wijaya@corpprocure.com', 'ANDI.WIJAYA@CORPPROCURE.COM', 1, @PasswordHash, NEWID(), NEWID(), '081234567002', 0, 0, 1, 0, 'Andi Wijaya', 'EMP-IT-002', @DeptIT, 1, GETUTCDATE()),
-- Staff HR
(@StaffHR1, 'siti.rahayu@corpprocure.com', 'SITI.RAHAYU@CORPPROCURE.COM', 'siti.rahayu@corpprocure.com', 'SITI.RAHAYU@CORPPROCURE.COM', 1, @PasswordHash, NEWID(), NEWID(), '081234567003', 0, 0, 1, 0, 'Siti Rahayu', 'EMP-HR-001', @DeptHR, 1, GETUTCDATE()),
-- Staff Operations
(@StaffOps1, 'ahmad.fauzi@corpprocure.com', 'AHMAD.FAUZI@CORPPROCURE.COM', 'ahmad.fauzi@corpprocure.com', 'AHMAD.FAUZI@CORPPROCURE.COM', 1, @PasswordHash, NEWID(), NEWID(), '081234567004', 0, 0, 1, 0, 'Ahmad Fauzi', 'EMP-OPS-001', @DeptOperations, 1, GETUTCDATE()),
-- Managers
(@ManagerIT, 'rudi.hartono@corpprocure.com', 'RUDI.HARTONO@CORPPROCURE.COM', 'rudi.hartono@corpprocure.com', 'RUDI.HARTONO@CORPPROCURE.COM', 1, @PasswordHash, NEWID(), NEWID(), '081234567010', 0, 0, 1, 0, 'Rudi Hartono', 'EMP-IT-MGR', @DeptIT, 1, GETUTCDATE()),
(@ManagerHR, 'dewi.lestari@corpprocure.com', 'DEWI.LESTARI@CORPPROCURE.COM', 'dewi.lestari@corpprocure.com', 'DEWI.LESTARI@CORPPROCURE.COM', 1, @PasswordHash, NEWID(), NEWID(), '081234567011', 0, 0, 1, 0, 'Dewi Lestari', 'EMP-HR-MGR', @DeptHR, 1, GETUTCDATE()),
(@ManagerOps, 'joko.susilo@corpprocure.com', 'JOKO.SUSILO@CORPPROCURE.COM', 'joko.susilo@corpprocure.com', 'JOKO.SUSILO@CORPPROCURE.COM', 1, @PasswordHash, NEWID(), NEWID(), '081234567012', 0, 0, 1, 0, 'Joko Susilo', 'EMP-OPS-MGR', @DeptOperations, 1, GETUTCDATE()),
-- Finance
(@FinanceUser1, 'maya.putri@corpprocure.com', 'MAYA.PUTRI@CORPPROCURE.COM', 'maya.putri@corpprocure.com', 'MAYA.PUTRI@CORPPROCURE.COM', 1, @PasswordHash, NEWID(), NEWID(), '081234567020', 0, 0, 1, 0, 'Maya Putri', 'EMP-FIN-001', @DeptFinance, 1, GETUTCDATE()),
(@FinanceUser2, 'ryan.pratama@corpprocure.com', 'RYAN.PRATAMA@CORPPROCURE.COM', 'ryan.pratama@corpprocure.com', 'RYAN.PRATAMA@CORPPROCURE.COM', 1, @PasswordHash, NEWID(), NEWID(), '081234567021', 0, 0, 1, 0, 'Ryan Pratama', 'EMP-FIN-002', @DeptFinance, 1, GETUTCDATE());

-- Update Managers di Department
UPDATE Departments SET ManagerId = @ManagerIT WHERE Id = @DeptIT;
UPDATE Departments SET ManagerId = @ManagerHR WHERE Id = @DeptHR;
UPDATE Departments SET ManagerId = @ManagerOps WHERE Id = @DeptOperations;

-- ============================================
-- 3. USER ROLES
-- ============================================
DECLARE @RoleStaff NVARCHAR(450);
DECLARE @RoleManager NVARCHAR(450);
DECLARE @RoleFinance NVARCHAR(450);

SELECT @RoleStaff = Id FROM AspNetRoles WHERE Name = 'Staff';
SELECT @RoleManager = Id FROM AspNetRoles WHERE Name = 'Manager';
SELECT @RoleFinance = Id FROM AspNetRoles WHERE Name = 'Finance';

INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES
(@StaffIT1, @RoleStaff),
(@StaffIT2, @RoleStaff),
(@StaffHR1, @RoleStaff),
(@StaffOps1, @RoleStaff),
(@ManagerIT, @RoleManager),
(@ManagerHR, @RoleManager),
(@ManagerOps, @RoleManager),
(@FinanceUser1, @RoleFinance),
(@FinanceUser2, @RoleFinance);

-- ============================================
-- 4. VENDORS
-- ============================================
DECLARE @VendorTokopedia UNIQUEIDENTIFIER = NEWID();
DECLARE @VendorBukalapak UNIQUEIDENTIFIER = NEWID();
DECLARE @VendorBhinneka UNIQUEIDENTIFIER = NEWID();
DECLARE @VendorOfficemate UNIQUEIDENTIFIER = NEWID();
DECLARE @VendorCVMaju UNIQUEIDENTIFIER = NEWID();

INSERT INTO Vendors (Id, Name, Code, Address, City, PostalCode, Phone, Email, ContactPerson, TaxId, BankName, BankAccountNumber, BankAccountName, IsActive, IsDeleted, CreatedAt) VALUES
(@VendorTokopedia, 'PT Tokopedia Indonesia', 'VND-001', 'Tokopedia Tower, Jl. Prof. DR. Satrio Kav. 11', 'Jakarta Selatan', '12950', '021-53691234', 'procurement@tokopedia.com', 'Hendri Wijaya', '01.234.567.8-012.000', 'BCA', '1234567890', 'PT Tokopedia Indonesia', 1, 0, GETUTCDATE()),
(@VendorBukalapak, 'PT Bukalapak.com', 'VND-002', 'Metropolitan Tower, Jl. R.A. Kartini Kav. 14', 'Jakarta Selatan', '12430', '021-29682888', 'vendor@bukalapak.com', 'Sari Melati', '02.345.678.9-023.000', 'Mandiri', '1200012345678', 'PT Bukalapak.com', 1, 0, GETUTCDATE()),
(@VendorBhinneka, 'PT Bhinneka Mentari Dimensi', 'VND-003', 'Jl. Gunung Sahari Raya No. 73', 'Jakarta Pusat', '10610', '021-29292828', 'sales@bhinneka.com', 'Agus Setiawan', '03.456.789.0-034.000', 'BNI', '0123456789', 'PT Bhinneka Mentari Dimensi', 1, 0, GETUTCDATE()),
(@VendorOfficemate, 'PT Officemate Indonesia', 'VND-004', 'Kawasan Industri Pulogadung, Jl. Rawa Gelam V No. 1', 'Jakarta Timur', '13930', '021-46822828', 'order@officemate.co.id', 'Linda Kusuma', '04.567.890.1-045.000', 'BRI', '012345678901234', 'PT Officemate Indonesia', 1, 0, GETUTCDATE()),
(@VendorCVMaju, 'CV Maju Jaya Abadi', 'VND-005', 'Jl. Raya Bogor Km. 27 No. 45', 'Depok', '16418', '021-87712345', 'cvmajujaya@gmail.com', 'Bambang Supriadi', '05.678.901.2-056.000', 'BCA', '2345678901', 'CV Maju Jaya Abadi', 1, 0, GETUTCDATE());

-- ============================================
-- 5. ITEM CATEGORIES
-- ============================================
DECLARE @CatIT UNIQUEIDENTIFIER = NEWID();
DECLARE @CatOffice UNIQUEIDENTIFIER = NEWID();
DECLARE @CatFurniture UNIQUEIDENTIFIER = NEWID();
DECLARE @CatElectronics UNIQUEIDENTIFIER = NEWID();

INSERT INTO ItemCategories (Id, Name, Description, IsActive, IsDeleted, CreatedAt) VALUES
(@CatIT, 'IT Equipment', 'Peralatan IT seperti komputer, laptop, server', 1, 0, GETUTCDATE()),
(@CatOffice, 'Office Supplies', 'Perlengkapan kantor seperti ATK, kertas, toner', 1, 0, GETUTCDATE()),
(@CatFurniture, 'Furniture', 'Perabotan kantor seperti meja, kursi, lemari', 1, 0, GETUTCDATE()),
(@CatElectronics, 'Electronics', 'Elektronik seperti AC, printer, proyektor', 1, 0, GETUTCDATE());

-- ============================================
-- 6. BUDGETS (Tahun 2026)
-- ============================================
INSERT INTO Budgets (Id, DepartmentId, FiscalYear, AllocatedAmount, UsedAmount, Description, IsActive, IsDeleted, CreatedAt) VALUES
(NEWID(), @DeptIT, 2026, 500000000, 75000000, 'Budget IT Department 2026', 1, 0, GETUTCDATE()),
(NEWID(), @DeptHR, 2026, 200000000, 25000000, 'Budget HR Department 2026', 1, 0, GETUTCDATE()),
(NEWID(), @DeptFinance, 2026, 150000000, 10000000, 'Budget Finance Department 2026', 1, 0, GETUTCDATE()),
(NEWID(), @DeptOperations, 2026, 300000000, 50000000, 'Budget Operations Department 2026', 1, 0, GETUTCDATE()),
(NEWID(), @DeptMarketing, 2026, 250000000, 30000000, 'Budget Marketing Department 2026', 1, 0, GETUTCDATE());

-- ============================================
-- 7. ITEMS
-- ============================================
DECLARE @ItemLaptop UNIQUEIDENTIFIER = NEWID();
DECLARE @ItemMonitor UNIQUEIDENTIFIER = NEWID();
DECLARE @ItemKeyboard UNIQUEIDENTIFIER = NEWID();
DECLARE @ItemPaper UNIQUEIDENTIFIER = NEWID();
DECLARE @ItemToner UNIQUEIDENTIFIER = NEWID();

INSERT INTO Items (Id, CategoryId, Name, Description, DefaultUnitPrice, UoM, IsActive, IsDeleted, CreatedAt) VALUES
(@ItemLaptop, @CatIT, 'Laptop Lenovo ThinkPad X1 Carbon', 'Laptop bisnis high-performance 14 inch', 25000000, 'Unit', 1, 0, GETUTCDATE()),
(@ItemMonitor, @CatIT, 'Monitor LG 27 inch 4K', 'Monitor UHD 4K untuk desain dan produktivitas', 5500000, 'Unit', 1, 0, GETUTCDATE()),
(@ItemKeyboard, @CatIT, 'Keyboard Logitech MX Keys', 'Keyboard wireless premium', 1500000, 'Unit', 1, 0, GETUTCDATE()),
(@ItemPaper, @CatOffice, 'Kertas HVS A4 70gsm', 'Kertas fotocopy A4 1 rim (500 lembar)', 55000, 'Rim', 1, 0, GETUTCDATE()),
(@ItemToner, @CatOffice, 'Toner HP 85A', 'Toner original untuk printer HP LaserJet', 850000, 'Unit', 1, 0, GETUTCDATE());

PRINT 'Sample data seeded successfully!';
PRINT '';
PRINT 'Login Credentials (Password: Password123!):';
PRINT '--------------------------------------------';
PRINT 'Staff IT: budi.santoso@corpprocure.com';
PRINT 'Staff HR: siti.rahayu@corpprocure.com';
PRINT 'Manager IT: rudi.hartono@corpprocure.com';
PRINT 'Manager HR: dewi.lestari@corpprocure.com';
PRINT 'Finance: maya.putri@corpprocure.com';
