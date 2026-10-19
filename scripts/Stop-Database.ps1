$root = Split-Path $PSScriptRoot -Parent
& "$root/.local/pgsql/bin/pg_ctl.exe" stop -D "$root/.local/pgdata" -m fast -w
