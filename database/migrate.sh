#!/usr/bin/env bash
# Versioned, forward-only SQL Server migrations; no Python/backend dependencies.
set -euo pipefail
export LC_ALL=C
: "${SQLCMDPASSWORD:?SQLCMDPASSWORD is required}"
sqlcmd=/opt/mssql-tools18/bin/sqlcmd
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
mode=${1:-up}
case "$mode" in up|verify|status|test|test-runner) ;; *) echo 'Usage: migrate.sh up|verify|status|test|test-runner' >&2; exit 2;; esac
run_sql() { "$sqlcmd" -S "${SQLSERVER_HOST:-sqlserver}" -U sa -C -I -b -V 11 -r 1 -l 30 "$@"; }

if [[ "$mode" == up ]]; then
  # Serialize database creation separately; CREATE DATABASE cannot run in a transaction.
  run_sql -d master -Q "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'RetailSync.create', @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=60000; IF @r<0 THROW 51000, 'Database creation lock unavailable', 1; IF DB_ID(N'RTS_DB') IS NULL EXEC(N'CREATE DATABASE RTS_DB');"
  script=$(mktemp)
  trap 'rm -f -- "$script"' EXIT
  cat > "$script" <<'SQL'
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @lock_result int;
EXEC @lock_result=sys.sp_getapplock @Resource=N'RetailSync.migrate', @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=60000;
IF @lock_result < 0 THROW 51000, 'Migration lock unavailable', 1;
IF OBJECT_ID(N'dbo.__schema_migration', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.__schema_migration (
    version varchar(100) NOT NULL CONSTRAINT pk_schema_migration PRIMARY KEY,
    checksum char(64) NOT NULL,
    applied_at datetime2 NOT NULL CONSTRAINT df_schema_migration_applied DEFAULT SYSUTCDATETIME()
  );
END;
GO
SQL
  # Filenames are code-controlled, restricted before interpolation into SQL.
  shopt -s nullglob
  files=("$root"/migrations/*.sql)
  [[ ${#files[@]} -gt 0 ]] || { echo 'No migrations found' >&2; exit 1; }
  # Refuse a checkout missing a version already applied to this database.
  printf "IF EXISTS (SELECT 1 FROM dbo.__schema_migration WHERE version NOT IN (" >> "$script"
  separator=''
  for file in "${files[@]}"; do
    version=$(basename -- "$file")
    [[ "$version" =~ ^[0-9]{3}_[a-z0-9_]+\.sql$ ]] || { echo 'Invalid migration filename' >&2; exit 1; }
    printf "%s'%s'" "$separator" "$version" >> "$script"
    separator=,
  done
  printf ")) THROW 51001, 'Database contains migration versions absent from this checkout', 1;\nGO\n" >> "$script"
  for file in "${files[@]}"; do
    version=$(basename -- "$file")
    checksum=$(sha256sum "$file" | cut -d ' ' -f 1)
    cat >> "$script" <<SQL
IF EXISTS (SELECT 1 FROM dbo.__schema_migration WHERE version='$version' AND checksum<>'$checksum')
  THROW 51002, 'Applied migration checksum mismatch: $version', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.__schema_migration WHERE version='$version')
BEGIN
  BEGIN TRANSACTION;
  EXEC sys.sp_executesql N'
SQL
    # Dynamic SQL isolates compilation and prevents skipped CREATE statements from
    # binding against an older schema. Migration files contain T-SQL, without GO.
    sed "s/'/''/g" "$file" >> "$script"
    cat >> "$script" <<SQL
';
  INSERT dbo.__schema_migration(version,checksum) VALUES('$version','$checksum');
  COMMIT TRANSACTION;
  PRINT 'Applied $version';
END
ELSE PRINT 'Already applied $version';
GO
SQL
  done
  run_sql -d RTS_DB -i "$script"
elif [[ "$mode" == status ]]; then
  run_sql -d RTS_DB -Q 'SELECT version,checksum,applied_at FROM dbo.__schema_migration ORDER BY version;'
elif [[ "$mode" == verify ]]; then
  run_sql -d RTS_DB -i "$root/verification/schema.sql"
elif [[ "$mode" == test ]]; then
  run_sql -d RTS_DB -i "$root/verification/constraints.sql"
else
  bash "$root/verification/runner.sh"
fi
