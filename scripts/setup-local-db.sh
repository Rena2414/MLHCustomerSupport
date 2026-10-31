#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
secrets="$root/backend/src/MlhSupport.Api/secrets.json"

if [ ! -f "$secrets" ]; then
  echo "Missing $secrets. Copy secrets.example.json to secrets.json and set a password first." >&2
  exit 1
fi

read -r -d '' parse_connection <<'PY' || true
import json, shlex, sys

connection = json.load(open(sys.argv[1]))["ConnectionStrings"]["Default"]
parts = dict(item.split("=", 1) for item in connection.split(";") if item)
for name, key, default in [
    ("db_host", "Host", "localhost"),
    ("db_port", "Port", "5432"),
    ("db_name", "Database", ""),
    ("db_user", "Username", ""),
    ("db_pass", "Password", ""),
]:
    print(name + "=" + shlex.quote(parts.get(key, default)))
PY

eval "$(python3 -c "$parse_connection" "$secrets")"

if [ -z "$db_name" ] || [ -z "$db_user" ] || [ -z "$db_pass" ] || [ "$db_pass" = "<choose-a-password>" ]; then
  echo "Set Database, Username and a real Password in secrets.json first." >&2
  exit 1
fi

psql_bin="$(command -v psql || true)"
for candidate in /opt/homebrew/opt/postgresql@16/bin/psql /usr/local/opt/postgresql@16/bin/psql; do
  [ -n "$psql_bin" ] || { [ -x "$candidate" ] && psql_bin="$candidate"; }
done
[ -n "$psql_bin" ] || { echo "psql not found. Install PostgreSQL 16 first." >&2; exit 1; }

"$psql_bin" -h "$db_host" -p "$db_port" -d postgres -v ON_ERROR_STOP=1 \
  -v dbname="$db_name" -v dbuser="$db_user" -v dbpass="$db_pass" <<'SQL'
SELECT format('CREATE ROLE %I LOGIN CREATEDB PASSWORD %L', :'dbuser', :'dbpass')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = :'dbuser') \gexec
SELECT format('ALTER ROLE %I PASSWORD %L', :'dbuser', :'dbpass') \gexec
SELECT format('CREATE DATABASE %I OWNER %I', :'dbname', :'dbuser')
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = :'dbname') \gexec
SQL

echo "Database '$db_name' and role '$db_user' are ready on $db_host:$db_port."
