#!/usr/bin/env bash
# Exercises the migration runner against this development database, without
# resetting it or touching existing business rows. Intentionally failing DDL rolls back.
set -euo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
testroot=$(mktemp -d)
trap 'rm -rf -- "$testroot"' EXIT
cp -R "$root"/. "$testroot"/
bash "$root/migrate.sh" up
bash "$root/migrate.sh" up

# An applied migration is immutable, including comment-only changes.
printf '\n-- Intentional checksum test\n' >> "$testroot/migrations/001_registry_inventory.sql"
if bash "$testroot/migrate.sh" up > "$testroot/checksum.log" 2>&1; then
    echo 'FAIL: modified applied migration accepted' >&2; exit 1
fi
grep -q 'checksum mismatch' "$testroot/checksum.log"
echo 'PASS: modified applied migration refused'
cp "$root/migrations/001_registry_inventory.sql" "$testroot/migrations/001_registry_inventory.sql"

mv "$testroot/migrations/001_registry_inventory.sql" "$testroot/001_saved.sql"
if bash "$testroot/migrate.sh" up > "$testroot/missing.log" 2>&1; then
    echo 'FAIL: checkout missing an applied version accepted' >&2; exit 1
fi
grep -q 'versions absent from this checkout' "$testroot/missing.log"
mv "$testroot/001_saved.sql" "$testroot/migrations/001_registry_inventory.sql"
echo 'PASS: missing applied migration refused'

cat > "$testroot/migrations/004_atomicity_probe.sql" <<'SQL'
CREATE TABLE dbo.__migration_atomicity_probe (id int NOT NULL);
THROW 51210, 'Intentional migration rollback test', 1;
SQL
if bash "$testroot/migrate.sh" up > "$testroot/failure.log" 2>&1; then
    echo 'FAIL: invalid migration accepted' >&2; exit 1
fi
grep -q 'Intentional migration rollback test' "$testroot/failure.log"
/opt/mssql-tools18/bin/sqlcmd -S "${SQLSERVER_HOST:-sqlserver}" -U sa -C -b -d RTS_DB -Q \
  "IF OBJECT_ID(N'dbo.__migration_atomicity_probe',N'U') IS NOT NULL THROW 51211,'Failed migration left its table behind',1; IF EXISTS(SELECT 1 FROM dbo.__schema_migration WHERE version='004_atomicity_probe.sql') THROW 51212,'Failed migration was journaled',1; PRINT 'PASS: failed migration DDL and journal entry rolled back';"

# Concurrent reruns must serialize, not double-apply or conflict.
bash "$root/migrate.sh" up > "$testroot/concurrent1.log" 2>&1 &
first=$!
bash "$root/migrate.sh" up > "$testroot/concurrent2.log" 2>&1 &
second=$!
wait "$first"
wait "$second"
echo 'PASS: concurrent migration reruns serialized'
bash "$root/migrate.sh" verify
bash "$root/migrate.sh" test
