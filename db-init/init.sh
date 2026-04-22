#!/bin/bash
set -e

SERVER="sqlserver"
USER="sa"
PASSWORD="BetStrike_2026!"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd -S ${SERVER} -U ${USER} -P ${PASSWORD} -C"
# /scripts      -> db-init folder (init.sh + 00_create_databases.sql)
# /sql-scripts  -> solution root (all *.sql files)

echo "=========================================="
echo "BetStrike Database Initialization"
echo "=========================================="

echo "Checking if databases are already initialized..."
if ${SQLCMD} -d master -Q "IF DB_ID('Apostas') IS NOT NULL AND EXISTS (SELECT 1 FROM Apostas.sys.tables WHERE name = 'Jogo') SELECT 'READY' ELSE SELECT 'EMPTY'" -h -1 2>/dev/null | grep -q "READY"; then
    echo "Databases already initialized. Skipping."
    exit 0
fi

echo ""
echo "Step 1: Creating databases..."
${SQLCMD} -d master -i /scripts/00_create_databases.sql

echo ""
echo "Step 2: Creating FPF schema..."
${SQLCMD} -d FPF -i /sql-scripts/FPF_Tables.sql
${SQLCMD} -d FPF -i /sql-scripts/FPF_StoredProcedures.sql

echo ""
echo "Step 3: Creating Pagamentos schema..."
${SQLCMD} -d Pagamentos -i /sql-scripts/Pagamentos_Tables.sql
${SQLCMD} -d Pagamentos -i /sql-scripts/Pagamentos_StoredProcedures.sql

echo ""
echo "Step 4: Creating Apostas schema..."
${SQLCMD} -d Apostas -i /sql-scripts/Apostas_Tables.sql
${SQLCMD} -d Apostas -i /sql-scripts/Apostas_StoredProcedures.sql

echo ""
echo "Step 5: Creating cross-database trigger (last - it references Pagamentos)..."
${SQLCMD} -d Apostas -i /sql-scripts/Apostas_Trigger.sql

echo ""
echo "=========================================="
echo "Database initialization complete!"
echo "=========================================="
