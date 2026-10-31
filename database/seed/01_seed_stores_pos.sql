USE RTS_DB;
GO

-- 1. Chèn 3 Store
IF NOT EXISTS (SELECT 1 FROM dbo.store WHERE store_id = 'STORE-DN1')
    INSERT INTO dbo.store (store_id, name, latitude, longitude, region_id, is_active)
    VALUES ('STORE-DN1', N'Cửa hàng Hải Châu', 16.067800, 108.220800, 'DN', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.store WHERE store_id = 'STORE-DN2')
    INSERT INTO dbo.store (store_id, name, latitude, longitude, region_id, is_active)
    VALUES ('STORE-DN2', N'Cửa hàng Thanh Khê', 16.061000, 108.180000, 'DN', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.store WHERE store_id = 'STORE-DN3')
    INSERT INTO dbo.store (store_id, name, latitude, longitude, region_id, is_active)
    VALUES ('STORE-DN3', N'Cửa hàng Sơn Trà', 16.075000, 108.235000, 'DN', 1);

-- 2. Chèn 6 POS Terminal
IF NOT EXISTS (SELECT 1 FROM dbo.pos_terminal WHERE pos_id = 'POS-DN1-01')
    INSERT INTO dbo.pos_terminal (pos_id, store_id, status, registered_at)
    VALUES ('POS-DN1-01', 'STORE-DN1', 'ACTIVE', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.pos_terminal WHERE pos_id = 'POS-DN1-02')
    INSERT INTO dbo.pos_terminal (pos_id, store_id, status, registered_at)
    VALUES ('POS-DN1-02', 'STORE-DN1', 'ACTIVE', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.pos_terminal WHERE pos_id = 'POS-DN2-01')
    INSERT INTO dbo.pos_terminal (pos_id, store_id, status, registered_at)
    VALUES ('POS-DN2-01', 'STORE-DN2', 'ACTIVE', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.pos_terminal WHERE pos_id = 'POS-DN2-02')
    INSERT INTO dbo.pos_terminal (pos_id, store_id, status, registered_at)
    VALUES ('POS-DN2-02', 'STORE-DN2', 'ACTIVE', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.pos_terminal WHERE pos_id = 'POS-DN3-01')
    INSERT INTO dbo.pos_terminal (pos_id, store_id, status, registered_at)
    VALUES ('POS-DN3-01', 'STORE-DN3', 'ACTIVE', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.pos_terminal WHERE pos_id = 'POS-DN3-02')
    INSERT INTO dbo.pos_terminal (pos_id, store_id, status, registered_at)
    VALUES ('POS-DN3-02', 'STORE-DN3', 'ACTIVE', SYSUTCDATETIME());
GO