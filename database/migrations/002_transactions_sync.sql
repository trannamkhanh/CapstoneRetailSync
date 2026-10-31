-- PB-01-3. Database design v1.1, tables 9-12.
CREATE TABLE dbo.sync_batch (
    sync_batch_id varchar(40) NOT NULL CONSTRAINT pk_sync_batch PRIMARY KEY,
    store_id varchar(10) NOT NULL,
    period_start datetime2 NOT NULL,
    period_end datetime2 NOT NULL,
    txn_count int NOT NULL,
    payload_bytes int NULL,
    raw_bytes int NULL,
    status varchar(10) NOT NULL,
    attempts int NOT NULL,
    last_error nvarchar(200) NULL,
    next_retry_at datetime2 NULL,
    created_at datetime2 NOT NULL,
    acked_at datetime2 NULL,
    CONSTRAINT fk_sync_batch_store FOREIGN KEY (store_id) REFERENCES dbo.store(store_id),
    CONSTRAINT ck_sync_batch_status CHECK (status IN ('PENDING','SENT','ACKED','FAILED')),
    CONSTRAINT ck_sync_batch_counts CHECK (txn_count >= 0 AND attempts >= 0),
    CONSTRAINT ck_sync_batch_bytes CHECK (payload_bytes >= 0 AND raw_bytes >= 0),
    CONSTRAINT ck_sync_batch_period CHECK (period_end >= period_start)
);
CREATE INDEX ix_sync_batch_queue ON dbo.sync_batch(status,created_at) INCLUDE(next_retry_at,store_id);
CREATE TABLE dbo.pos_transaction (
    txn_id varchar(40) NOT NULL CONSTRAINT pk_pos_transaction PRIMARY KEY,
    store_id varchar(10) NOT NULL,
    pos_id varchar(20) NOT NULL,
    txn_type varchar(15) NOT NULL,
    ref_txn_id varchar(40) NULL,
    status varchar(12) NOT NULL,
    sync_batch_id varchar(40) NULL,
    payload_hash char(64) NOT NULL,
    created_at datetime2 NOT NULL,
    CONSTRAINT fk_pos_transaction_store FOREIGN KEY (store_id) REFERENCES dbo.store(store_id),
    CONSTRAINT fk_pos_transaction_terminal FOREIGN KEY (pos_id) REFERENCES dbo.pos_terminal(pos_id),
    CONSTRAINT fk_pos_transaction_reference FOREIGN KEY (ref_txn_id) REFERENCES dbo.pos_transaction(txn_id),
    CONSTRAINT fk_pos_transaction_sync FOREIGN KEY (sync_batch_id) REFERENCES dbo.sync_batch(sync_batch_id),
    CONSTRAINT ck_pos_transaction_type CHECK (txn_type IN ('SALE','RETURN','CANCELLATION')),
    CONSTRAINT ck_pos_transaction_status CHECK (status IN ('OK','CANCELLED'))
);
CREATE INDEX ix_pos_transaction_unbatched ON dbo.pos_transaction(store_id,created_at) WHERE sync_batch_id IS NULL;
CREATE INDEX ix_pos_transaction_reference ON dbo.pos_transaction(ref_txn_id) WHERE ref_txn_id IS NOT NULL;
CREATE INDEX ix_pos_transaction_sync ON dbo.pos_transaction(sync_batch_id) WHERE sync_batch_id IS NOT NULL;
CREATE TABLE dbo.transaction_item (
    item_id bigint IDENTITY(1,1) NOT NULL CONSTRAINT pk_transaction_item PRIMARY KEY,
    txn_id varchar(40) NOT NULL,
    sku varchar(20) NOT NULL,
    qty int NOT NULL,
    unit_price decimal(12,2) NOT NULL,
    CONSTRAINT fk_transaction_item_transaction FOREIGN KEY (txn_id) REFERENCES dbo.pos_transaction(txn_id),
    CONSTRAINT fk_transaction_item_product FOREIGN KEY (sku) REFERENCES dbo.product(sku),
    CONSTRAINT ck_transaction_item_qty CHECK (qty > 0),
    CONSTRAINT ck_transaction_item_price CHECK (unit_price >= 0)
);
CREATE INDEX ix_transaction_item_transaction ON dbo.transaction_item(txn_id) INCLUDE(sku,qty,unit_price);
CREATE TABLE dbo.inventory_movement (
    movement_id bigint IDENTITY(1,1) NOT NULL CONSTRAINT pk_inventory_movement PRIMARY KEY,
    store_id varchar(10) NOT NULL,
    sku varchar(20) NOT NULL,
    movement_type varchar(20) NOT NULL,
    quantity int NOT NULL,
    txn_id varchar(40) NULL,
    batch_id varchar(30) NULL,
    reason nvarchar(200) NULL,
    created_by int NULL,
    created_at datetime2 NOT NULL,
    CONSTRAINT fk_inventory_movement_inventory FOREIGN KEY (store_id,sku) REFERENCES dbo.inventory(store_id,sku),
    CONSTRAINT fk_inventory_movement_transaction FOREIGN KEY (txn_id) REFERENCES dbo.pos_transaction(txn_id),
    CONSTRAINT fk_inventory_movement_user FOREIGN KEY (created_by) REFERENCES dbo.staff_user(user_id),
    CONSTRAINT ck_inventory_movement_type CHECK (movement_type IN ('SALE','RETURN','CANCELLATION','ADJUSTMENT','STOCK_RECEIVED','TRANSFER_IN','TRANSFER_OUT'))
);
-- batch_id -> product_batch is added WITH CHECK when PB-20 creates that table.
CREATE INDEX ix_inventory_movement_stock ON dbo.inventory_movement(store_id,sku,created_at);
CREATE INDEX ix_inventory_movement_transaction ON dbo.inventory_movement(txn_id) WHERE txn_id IS NOT NULL;
