-- PB-01-4. Database design v1.1, tables 5, 17 and 18.
CREATE TABLE dbo.recommendation (
    rec_id bigint IDENTITY(1,1) NOT NULL CONSTRAINT pk_recommendation PRIMARY KEY,
    store_id varchar(10) NOT NULL,
    sku varchar(20) NOT NULL,
    batch_id varchar(30) NULL,
    alert_id bigint NULL,
    forecast_id bigint NULL,
    action_type varchar(15) NOT NULL,
    discount_pct decimal(5,2) NULL,
    reason nvarchar(300) NOT NULL,
    status varchar(12) NOT NULL,
    created_at datetime2 NOT NULL,
    CONSTRAINT fk_recommendation_store FOREIGN KEY (store_id) REFERENCES dbo.store(store_id),
    CONSTRAINT fk_recommendation_product FOREIGN KEY (sku) REFERENCES dbo.product(sku),
    CONSTRAINT ck_recommendation_action CHECK (action_type IN ('DISCOUNT','CLEARANCE','REDISTRIBUTION')),
    CONSTRAINT ck_recommendation_status CHECK (status IN ('GENERATED','BLOCKED','PENDING','ACCEPTED','REJECTED')),
    CONSTRAINT ck_recommendation_discount CHECK (discount_pct BETWEEN 0 AND 100)
);
-- batch_id, alert_id and forecast_id FKs wait for their actual feature tables.
CREATE INDEX ix_recommendation_store_status ON dbo.recommendation(store_id,status,created_at);
CREATE TABLE dbo.guardrail_rule (
    rule_id int IDENTITY(1,1) NOT NULL CONSTRAINT pk_guardrail_rule PRIMARY KEY,
    rule_type varchar(20) NOT NULL,
    threshold decimal(12,2) NOT NULL,
    category varchar(50) NULL,
    is_active bit NOT NULL,
    updated_by int NOT NULL,
    updated_at datetime2 NOT NULL,
    CONSTRAINT fk_guardrail_rule_user FOREIGN KEY (updated_by) REFERENCES dbo.staff_user(user_id),
    CONSTRAINT ck_guardrail_rule_type CHECK (rule_type IN ('MAX_DISCOUNT','MIN_PRICE','MIN_SHELF_LIFE')),
    CONSTRAINT ck_guardrail_rule_threshold CHECK (threshold >= 0 AND (rule_type <> 'MAX_DISCOUNT' OR threshold <= 100))
);
CREATE INDEX ix_guardrail_rule_active ON dbo.guardrail_rule(is_active,category,rule_type);
CREATE TABLE dbo.audit_log (
    audit_id bigint IDENTITY(1,1) NOT NULL CONSTRAINT pk_audit_log PRIMARY KEY,
    user_id int NULL,
    action varchar(50) NOT NULL,
    entity_name varchar(50) NOT NULL,
    entity_key varchar(50) NOT NULL,
    details nvarchar(500) NULL,
    created_at datetime2 NOT NULL,
    CONSTRAINT fk_audit_log_user FOREIGN KEY (user_id) REFERENCES dbo.staff_user(user_id)
);
CREATE INDEX ix_audit_log_time ON dbo.audit_log(created_at);
CREATE INDEX ix_audit_log_actor_action ON dbo.audit_log(user_id,action,created_at);
