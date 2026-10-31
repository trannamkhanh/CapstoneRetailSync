-- Run in the dedicated development database. All fixture rows are rolled back.
SET NOCOUNT ON;
SET XACT_ABORT OFF;
IF DB_NAME()<>'RTS_DB' THROW 51200,'Run constraint checks in RTS_DB',1;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @store varchar(10)=LEFT(REPLACE(CONVERT(varchar(36),NEWID()),'-',''),10);
    DECLARE @sku varchar(20)=LEFT(CONVERT(varchar(36),NEWID()),20);
    DECLARE @pos varchar(20)=LEFT(CONVERT(varchar(36),NEWID()),20);
    DECLARE @txn varchar(40)=CONVERT(varchar(36),NEWID());
    DECLARE @sync varchar(40)=CONVERT(varchar(36),NEWID());
    INSERT dbo.store VALUES(@store,N'PB-01 test store',10.123456,106.123456,'TEST',1);
    INSERT dbo.pos_terminal VALUES(@pos,@store,'ACTIVE',SYSUTCDATETIME());
    INSERT dbo.staff_user(username,full_name,role,store_id,is_active)
        VALUES(@sku,N'Test admin','BUSINESS_ADMIN',NULL,1);
    DECLARE @user int=CONVERT(int,SCOPE_IDENTITY());
    INSERT dbo.product VALUES(@sku,@sku,N'Test product','TEST',10.50);
    INSERT dbo.inventory VALUES(@store,@sku,0,SYSUTCDATETIME());
    INSERT dbo.sync_batch(sync_batch_id,store_id,period_start,period_end,txn_count,status,attempts,created_at)
        VALUES(@sync,@store,SYSUTCDATETIME(),SYSUTCDATETIME(),1,'PENDING',0,SYSUTCDATETIME());
    INSERT dbo.pos_transaction VALUES(@txn,@store,@pos,'SALE',NULL,'OK',@sync,REPLICATE('a',64),SYSUTCDATETIME());
    INSERT dbo.transaction_item(txn_id,sku,qty,unit_price) VALUES(@txn,@sku,1,10.50);
    INSERT dbo.inventory_movement(store_id,sku,movement_type,quantity,txn_id,created_at)
        VALUES(@store,@sku,'SALE',-1,@txn,SYSUTCDATETIME());
    INSERT dbo.recommendation(store_id,sku,action_type,discount_pct,reason,status,created_at)
        VALUES(@store,@sku,'DISCOUNT',100,N'Test only','GENERATED',SYSUTCDATETIME());
    DECLARE @rec bigint=CONVERT(bigint,SCOPE_IDENTITY());
    INSERT dbo.guardrail_rule(rule_type,threshold,category,is_active,updated_by,updated_at)
        VALUES('MAX_DISCOUNT',100,NULL,1,@user,SYSUTCDATETIME());
    INSERT dbo.audit_log(user_id,action,entity_name,entity_key,details,created_at)
        VALUES(NULL,'TEST','store',@store,NULL,SYSUTCDATETIME());

    DECLARE @cases TABLE(name varchar(80), command nvarchar(max), expected_error int);
    INSERT @cases VALUES
    ('negative inventory',N'UPDATE dbo.inventory SET quantity=-1 WHERE store_id=@store AND sku=@sku',547),
    ('zero transaction quantity',N'INSERT dbo.transaction_item(txn_id,sku,qty,unit_price) VALUES(@txn,@sku,0,1)',547),
    ('negative line price',N'INSERT dbo.transaction_item(txn_id,sku,qty,unit_price) VALUES(@txn,@sku,1,-1)',547),
    ('duplicate transaction id',N'INSERT dbo.pos_transaction SELECT * FROM dbo.pos_transaction WHERE txn_id=@txn',2627),
    ('duplicate sync id',N'INSERT dbo.sync_batch SELECT * FROM dbo.sync_batch WHERE sync_batch_id=@sync',2627),
    ('duplicate inventory key',N'INSERT dbo.inventory SELECT * FROM dbo.inventory WHERE store_id=@store AND sku=@sku',2627),
    ('duplicate barcode',N'INSERT dbo.product VALUES(''__other_sku__'',@sku,N''Other'',''TEST'',1)',2627),
    ('duplicate username',N'INSERT dbo.staff_user(username,full_name,role,is_active) VALUES(@sku,N''Other'',''AUDITOR'',1)',2627),
    ('orphan store',N'INSERT dbo.inventory VALUES(''__missing'',@sku,0,SYSUTCDATETIME())',547),
    ('orphan transaction',N'INSERT dbo.transaction_item(txn_id,sku,qty,unit_price) VALUES(''__missing'',@sku,1,1)',547),
    ('orphan composite stock',N'INSERT dbo.inventory_movement(store_id,sku,movement_type,quantity,created_at) VALUES(@store,''__missing'',''SALE'',-1,SYSUTCDATETIME())',547),
    ('orphan original sale',N'UPDATE dbo.pos_transaction SET ref_txn_id=''__missing'' WHERE txn_id=@txn',547),
    ('orphan sync batch',N'UPDATE dbo.pos_transaction SET sync_batch_id=''__missing'' WHERE txn_id=@txn',547),
    ('orphan guardrail user',N'UPDATE dbo.guardrail_rule SET updated_by=-1 WHERE updated_by=@user',547),
    ('invalid latitude',N'UPDATE dbo.store SET latitude=91 WHERE store_id=@store',547),
    ('invalid longitude',N'UPDATE dbo.store SET longitude=-181 WHERE store_id=@store',547),
    ('invalid terminal status',N'UPDATE dbo.pos_terminal SET status=''UNKNOWN'' WHERE pos_id=@pos',547),
    ('invalid role',N'UPDATE dbo.staff_user SET role=''UNKNOWN'' WHERE user_id=@user',547),
    ('invalid event type',N'UPDATE dbo.pos_transaction SET txn_type=''UNKNOWN'' WHERE txn_id=@txn',547),
    ('invalid transaction status',N'UPDATE dbo.pos_transaction SET status=''UNKNOWN'' WHERE txn_id=@txn',547),
    ('invalid movement type',N'UPDATE dbo.inventory_movement SET movement_type=''UNKNOWN'' WHERE txn_id=@txn',547),
    ('invalid sync status',N'UPDATE dbo.sync_batch SET status=''UNKNOWN'' WHERE sync_batch_id=@sync',547),
    ('negative attempts',N'UPDATE dbo.sync_batch SET attempts=-1 WHERE sync_batch_id=@sync',547),
    ('reversed batch period',N'UPDATE dbo.sync_batch SET period_end=DATEADD(day,-1,period_start) WHERE sync_batch_id=@sync',547),
    ('invalid recommendation status',N'UPDATE dbo.recommendation SET status=''UNKNOWN'' WHERE rec_id=@rec',547),
    ('invalid recommendation action',N'UPDATE dbo.recommendation SET action_type=''UNKNOWN'' WHERE rec_id=@rec',547),
    ('discount above maximum',N'UPDATE dbo.recommendation SET discount_pct=100.01 WHERE rec_id=@rec',547),
    ('negative discount',N'UPDATE dbo.recommendation SET discount_pct=-0.01 WHERE rec_id=@rec',547),
    ('invalid guardrail type',N'UPDATE dbo.guardrail_rule SET rule_type=''UNKNOWN'' WHERE updated_by=@user',547),
    ('discount rule above maximum',N'UPDATE dbo.guardrail_rule SET threshold=100.01 WHERE updated_by=@user',547),
    ('negative rule threshold',N'UPDATE dbo.guardrail_rule SET threshold=-1 WHERE updated_by=@user',547),
    ('no cascading store deletion',N'DELETE dbo.store WHERE store_id=@store',547);
    DECLARE @name varchar(80),@command nvarchar(max),@expected int,@actual int,@count int=0;
    DECLARE cases CURSOR LOCAL FAST_FORWARD FOR SELECT name,command,expected_error FROM @cases;
    OPEN cases;
    FETCH NEXT FROM cases INTO @name,@command,@expected;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @actual=0;
        BEGIN TRY
            EXEC sys.sp_executesql @command,
                N'@store varchar(10),@sku varchar(20),@pos varchar(20),@txn varchar(40),@sync varchar(40),@user int,@rec bigint',
                @store,@sku,@pos,@txn,@sync,@user,@rec;
        END TRY
        BEGIN CATCH
            SET @actual=ERROR_NUMBER();
        END CATCH;
        IF @actual<>@expected
        BEGIN
            PRINT CONCAT('FAIL: ',@name,' expected ',@expected,' got ',@actual);
            THROW 51201,'Constraint test did not reject the invalid operation as expected',1;
        END;
        SET @count+=1;
        FETCH NEXT FROM cases INTO @name,@command,@expected;
    END;
    CLOSE cases;
    DEALLOCATE cases;
    IF (SELECT quantity FROM dbo.inventory WHERE store_id=@store AND sku=@sku)<>0
        THROW 51202,'Rejected statement changed inventory',1;
    ROLLBACK TRANSACTION;
    IF EXISTS(SELECT 1 FROM dbo.store WHERE store_id=@store)
        THROW 51203,'Fixture rollback failed',1;
    PRINT CONCAT('PASS: valid rows in all 12 tables; ',@count,' invalid operations rejected; fixture transaction rolled back.');
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
