-- PB-01-5: expected columns transcribed from database design v1.1 tables 2-12,17,18.
-- Nullable choices and deferred relationships are documented in database/README.md.
SET NOCOUNT ON;
IF DB_NAME() <> 'RTS_DB' THROW 51100, 'Run schema verification in RTS_DB', 1;
DECLARE @expected TABLE(table_name sysname, column_name sysname, data_type varchar(40), nullable bit, identity_column bit);
INSERT @expected VALUES
('store','store_id','varchar(10)',0,0),
('store','name','nvarchar(100)',0,0),
('store','latitude','decimal(9,6)',0,0),
('store','longitude','decimal(9,6)',0,0),
('store','region_id','varchar(10)',0,0),
('store','is_active','bit',0,0),
('pos_terminal','pos_id','varchar(20)',0,0),
('pos_terminal','store_id','varchar(10)',0,0),
('pos_terminal','status','varchar(10)',0,0),
('pos_terminal','registered_at','datetime2',0,0),
('staff_user','user_id','int',0,1),
('staff_user','username','varchar(50)',0,0),
('staff_user','full_name','nvarchar(100)',0,0),
('staff_user','role','varchar(20)',0,0),
('staff_user','store_id','varchar(10)',1,0),
('staff_user','is_active','bit',0,0),
('audit_log','audit_id','bigint',0,1),
('audit_log','user_id','int',1,0),
('audit_log','action','varchar(50)',0,0),
('audit_log','entity_name','varchar(50)',0,0),
('audit_log','entity_key','varchar(50)',0,0),
('audit_log','details','nvarchar(500)',1,0),
('audit_log','created_at','datetime2',0,0),
('product','sku','varchar(20)',0,0),
('product','barcode','varchar(30)',0,0),
('product','name','nvarchar(150)',0,0),
('product','category','varchar(50)',0,0),
('product','unit_price','decimal(12,2)',0,0),
('inventory','store_id','varchar(10)',0,0),
('inventory','sku','varchar(20)',0,0),
('inventory','quantity','int',0,0),
('inventory','updated_at','datetime2',0,0),
('inventory_movement','movement_id','bigint',0,1),
('inventory_movement','store_id','varchar(10)',0,0),
('inventory_movement','sku','varchar(20)',0,0),
('inventory_movement','movement_type','varchar(20)',0,0),
('inventory_movement','quantity','int',0,0),
('inventory_movement','txn_id','varchar(40)',1,0),
('inventory_movement','batch_id','varchar(30)',1,0),
('inventory_movement','reason','nvarchar(200)',1,0),
('inventory_movement','created_by','int',1,0),
('inventory_movement','created_at','datetime2',0,0),
('pos_transaction','txn_id','varchar(40)',0,0),
('pos_transaction','store_id','varchar(10)',0,0),
('pos_transaction','pos_id','varchar(20)',0,0),
('pos_transaction','txn_type','varchar(15)',0,0),
('pos_transaction','ref_txn_id','varchar(40)',1,0),
('pos_transaction','status','varchar(12)',0,0),
('pos_transaction','sync_batch_id','varchar(40)',1,0),
('pos_transaction','payload_hash','char(64)',0,0),
('pos_transaction','created_at','datetime2',0,0),
('transaction_item','item_id','bigint',0,1),
('transaction_item','txn_id','varchar(40)',0,0),
('transaction_item','sku','varchar(20)',0,0),
('transaction_item','qty','int',0,0),
('transaction_item','unit_price','decimal(12,2)',0,0),
('sync_batch','sync_batch_id','varchar(40)',0,0),
('sync_batch','store_id','varchar(10)',0,0),
('sync_batch','period_start','datetime2',0,0),
('sync_batch','period_end','datetime2',0,0),
('sync_batch','txn_count','int',0,0),
('sync_batch','payload_bytes','int',1,0),
('sync_batch','raw_bytes','int',1,0),
('sync_batch','status','varchar(10)',0,0),
('sync_batch','attempts','int',0,0),
('sync_batch','last_error','nvarchar(200)',1,0),
('sync_batch','next_retry_at','datetime2',1,0),
('sync_batch','created_at','datetime2',0,0),
('sync_batch','acked_at','datetime2',1,0),
('recommendation','rec_id','bigint',0,1),
('recommendation','store_id','varchar(10)',0,0),
('recommendation','sku','varchar(20)',0,0),
('recommendation','batch_id','varchar(30)',1,0),
('recommendation','alert_id','bigint',1,0),
('recommendation','forecast_id','bigint',1,0),
('recommendation','action_type','varchar(15)',0,0),
('recommendation','discount_pct','decimal(5,2)',1,0),
('recommendation','reason','nvarchar(300)',0,0),
('recommendation','status','varchar(12)',0,0),
('recommendation','created_at','datetime2',0,0),
('guardrail_rule','rule_id','int',0,1),
('guardrail_rule','rule_type','varchar(20)',0,0),
('guardrail_rule','threshold','decimal(12,2)',0,0),
('guardrail_rule','category','varchar(50)',1,0),
('guardrail_rule','is_active','bit',0,0),
('guardrail_rule','updated_by','int',0,0),
('guardrail_rule','updated_at','datetime2',0,0);
SELECT t.name COLLATE DATABASE_DEFAULT table_name, c.name COLLATE DATABASE_DEFAULT column_name,
 CAST(ty.name + CASE
 WHEN ty.name IN ('varchar','char','nvarchar','nchar') THEN '('+CONVERT(varchar(10),CASE WHEN ty.name IN ('nvarchar','nchar') THEN c.max_length/2 ELSE c.max_length END)+')'
 WHEN ty.name IN ('decimal','numeric') THEN '('+CONVERT(varchar(10),c.precision)+','+CONVERT(varchar(10),c.scale)+')'
 ELSE '' END AS varchar(40)) COLLATE DATABASE_DEFAULT data_type,
 c.is_nullable nullable, c.is_identity identity_column
INTO #actual
FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
WHERE s.name='dbo' AND t.name IN (SELECT table_name FROM @expected);
IF EXISTS (SELECT * FROM @expected EXCEPT SELECT * FROM #actual)
 OR EXISTS (SELECT * FROM #actual EXCEPT SELECT * FROM @expected)
BEGIN
 SELECT 'missing or different' issue,* FROM (SELECT * FROM @expected EXCEPT SELECT * FROM #actual) d;
 SELECT 'unexpected or different' issue,* FROM (SELECT * FROM #actual EXCEPT SELECT * FROM @expected) d;
 THROW 51101, 'Column metadata differs from the PB-01 specification', 1;
END;
IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name NOT IN (SELECT table_name FROM @expected) AND name<>'__schema_migration')
 THROW 51102, 'Unexpected table in PB-01 database', 1;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1)
 OR EXISTS (SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1)
 THROW 51103, 'Disabled or untrusted constraint', 1;
DECLARE @keys TABLE(table_name sysname, kind char(2), columns varchar(100));
INSERT @keys VALUES
('store','PK','store_id'),('pos_terminal','PK','pos_id'),('staff_user','PK','user_id'),
('product','PK','sku'),('inventory','PK','store_id,sku'),('sync_batch','PK','sync_batch_id'),
('pos_transaction','PK','txn_id'),('transaction_item','PK','item_id'),('inventory_movement','PK','movement_id'),
('recommendation','PK','rec_id'),('guardrail_rule','PK','rule_id'),('audit_log','PK','audit_id'),
('staff_user','UQ','username'),('product','UQ','barcode');
SELECT t.name COLLATE DATABASE_DEFAULT table_name,k.type COLLATE DATABASE_DEFAULT kind,
 STRING_AGG(CONVERT(varchar(max),c.name),',') WITHIN GROUP (ORDER BY ic.key_ordinal) COLLATE DATABASE_DEFAULT columns
INTO #keys FROM sys.key_constraints k JOIN sys.tables t ON t.object_id=k.parent_object_id
JOIN sys.index_columns ic ON ic.object_id=t.object_id AND ic.index_id=k.unique_index_id
JOIN sys.columns c ON c.object_id=t.object_id AND c.column_id=ic.column_id
WHERE t.name IN (SELECT table_name FROM @expected)
GROUP BY t.name,k.type;
IF EXISTS(SELECT * FROM @keys EXCEPT SELECT * FROM #keys)
 OR EXISTS(SELECT * FROM #keys EXCEPT SELECT * FROM @keys)
 THROW 51104, 'Primary or unique key mismatch',1;
DECLARE @fk TABLE(child sysname, cols varchar(100), parent sysname, refs varchar(100));
INSERT @fk VALUES
('pos_terminal','store_id','store','store_id'),('staff_user','store_id','store','store_id'),
('inventory','store_id','store','store_id'),('inventory','sku','product','sku'),
('sync_batch','store_id','store','store_id'),('pos_transaction','store_id','store','store_id'),
('pos_transaction','pos_id','pos_terminal','pos_id'),('pos_transaction','ref_txn_id','pos_transaction','txn_id'),
('pos_transaction','sync_batch_id','sync_batch','sync_batch_id'),
('transaction_item','txn_id','pos_transaction','txn_id'),('transaction_item','sku','product','sku'),
('inventory_movement','store_id,sku','inventory','store_id,sku'),
('inventory_movement','txn_id','pos_transaction','txn_id'),('inventory_movement','created_by','staff_user','user_id'),
('recommendation','store_id','store','store_id'),('recommendation','sku','product','sku'),
('guardrail_rule','updated_by','staff_user','user_id'),('audit_log','user_id','staff_user','user_id');
SELECT ct.name COLLATE DATABASE_DEFAULT child,
 STRING_AGG(CONVERT(varchar(max),cc.name),',') WITHIN GROUP(ORDER BY fkc.constraint_column_id) COLLATE DATABASE_DEFAULT cols,
 pt.name COLLATE DATABASE_DEFAULT parent,
 STRING_AGG(CONVERT(varchar(max),pc.name),',') WITHIN GROUP(ORDER BY fkc.constraint_column_id) COLLATE DATABASE_DEFAULT refs
INTO #fk FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id
JOIN sys.tables ct ON ct.object_id=fk.parent_object_id JOIN sys.tables pt ON pt.object_id=fk.referenced_object_id
JOIN sys.columns cc ON cc.object_id=ct.object_id AND cc.column_id=fkc.parent_column_id
JOIN sys.columns pc ON pc.object_id=pt.object_id AND pc.column_id=fkc.referenced_column_id
GROUP BY fk.object_id,ct.name,pt.name;
IF EXISTS(SELECT * FROM @fk EXCEPT SELECT * FROM #fk)
 OR EXISTS(SELECT * FROM #fk EXCEPT SELECT * FROM @fk)
 THROW 51105, 'Foreign key mismatch',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE delete_referential_action<>0 OR update_referential_action<>0)
 THROW 51106,'Unexpected cascading foreign key',1;
IF (SELECT COUNT(*) FROM dbo.__schema_migration)<>3 THROW 51107,'Expected three applied PB-01 migrations',1;
PRINT 'PASS: all 12 PB-01 tables match expected columns, types, nullability, identity, primary/unique keys and 18 trusted foreign keys.';
PRINT 'Deferred: inventory_movement.batch_id and recommendation.batch_id/alert_id/forecast_id foreign keys. See database/README.md.';
