-- PB-01-2. Database design v1.1, tables 2-4, 6 and 8.
CREATE TABLE dbo.store (
    store_id varchar(10) NOT NULL CONSTRAINT pk_store PRIMARY KEY,
    name nvarchar(100) NOT NULL,
    latitude decimal(9,6) NOT NULL,
    longitude decimal(9,6) NOT NULL,
    region_id varchar(10) NOT NULL,
    is_active bit NOT NULL,
    CONSTRAINT ck_store_latitude CHECK (latitude BETWEEN -90 AND 90),
    CONSTRAINT ck_store_longitude CHECK (longitude BETWEEN -180 AND 180)
);
CREATE TABLE dbo.pos_terminal (
    pos_id varchar(20) NOT NULL CONSTRAINT pk_pos_terminal PRIMARY KEY,
    store_id varchar(10) NOT NULL,
    status varchar(10) NOT NULL,
    registered_at datetime2 NOT NULL,
    CONSTRAINT fk_pos_terminal_store FOREIGN KEY (store_id) REFERENCES dbo.store(store_id),
    CONSTRAINT ck_pos_terminal_status CHECK (status IN ('ACTIVE','INACTIVE'))
);
CREATE INDEX ix_pos_terminal_store ON dbo.pos_terminal(store_id);
CREATE TABLE dbo.staff_user (
    user_id int IDENTITY(1,1) NOT NULL CONSTRAINT pk_staff_user PRIMARY KEY,
    username varchar(50) NOT NULL CONSTRAINT uq_staff_user_username UNIQUE,
    full_name nvarchar(100) NOT NULL,
    role varchar(20) NOT NULL,
    store_id varchar(10) NULL,
    is_active bit NOT NULL,
    CONSTRAINT fk_staff_user_store FOREIGN KEY (store_id) REFERENCES dbo.store(store_id),
    CONSTRAINT ck_staff_user_role CHECK (role IN ('STORE_STAFF','STORE_MANAGER','HQ_OPERATOR','SYSTEM_ADMIN','BUSINESS_ADMIN','AUDITOR','POS_SYSTEM'))
);
CREATE INDEX ix_staff_user_store ON dbo.staff_user(store_id);
CREATE TABLE dbo.product (
    sku varchar(20) NOT NULL CONSTRAINT pk_product PRIMARY KEY,
    barcode varchar(30) NOT NULL CONSTRAINT uq_product_barcode UNIQUE,
    name nvarchar(150) NOT NULL,
    category varchar(50) NOT NULL,
    unit_price decimal(12,2) NOT NULL,
    CONSTRAINT ck_product_price CHECK (unit_price >= 0)
);
CREATE TABLE dbo.inventory (
    store_id varchar(10) NOT NULL,
    sku varchar(20) NOT NULL,
    quantity int NOT NULL,
    updated_at datetime2 NOT NULL,
    CONSTRAINT pk_inventory PRIMARY KEY (store_id,sku),
    CONSTRAINT fk_inventory_store FOREIGN KEY (store_id) REFERENCES dbo.store(store_id),
    CONSTRAINT fk_inventory_product FOREIGN KEY (sku) REFERENCES dbo.product(sku),
    CONSTRAINT ck_inventory_quantity CHECK (quantity >= 0)
);
CREATE INDEX ix_inventory_sku ON dbo.inventory(sku,store_id) INCLUDE(quantity);
