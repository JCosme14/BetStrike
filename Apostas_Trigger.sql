USE Apostas;
GO

-- Trigger that fires after any UPDATE on the Aposta table.
-- Handles cross-database communication with the Pagamentos DB:
--   Estado = 2 (Ganha)  -> PG transaction + credit Valor_Apostado * Odd_Momento
--   Estado = 4 (Anulada) -> RE transaction + credit Valor_Apostado back
--   Estado = 3 (Perdida) -> no action (amount was already debited on bet creation)

CREATE OR ALTER TRIGGER trg_Aposta_EstadoChanged
ON Aposta
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Only act when the Estado column actually changed
    IF NOT UPDATE(Estado)
        RETURN;

    -- ----------------------------------------------------------------
    -- Handle WINNING bets (Estado = 2)
    -- ----------------------------------------------------------------
    INSERT INTO Pagamentos.dbo.Transacao
        (Aposta_ID, Utilizador_ID, Tipo_Transacao, Valor, Data_Hora, Estado)
    SELECT
        i.ID,
        i.Utilizador_ID,
        'PG',
        i.Valor_Apostado * i.Odd_Momento,
        GETDATE(),
        'Processada'
    FROM inserted i
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE i.Estado = 2
      AND d.Estado <> 2;  -- only on transition TO Ganha

    -- Credit saldo for winning bets
    UPDATE Pagamentos.dbo.Saldo_Utilizador
    SET
        Saldo = Saldo + (i.Valor_Apostado * i.Odd_Momento),
        Data_Hora_Atualizacao = GETDATE()
    FROM Pagamentos.dbo.Saldo_Utilizador su
    INNER JOIN inserted i ON su.Utilizador_ID = i.Utilizador_ID
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE i.Estado = 2
      AND d.Estado <> 2;

    -- ----------------------------------------------------------------
    -- Handle CANCELLED/ANNULLED bets (Estado = 4)
    -- ----------------------------------------------------------------
    INSERT INTO Pagamentos.dbo.Transacao
        (Aposta_ID, Utilizador_ID, Tipo_Transacao, Valor, Data_Hora, Estado)
    SELECT
        i.ID,
        i.Utilizador_ID,
        'RE',
        i.Valor_Apostado,
        GETDATE(),
        'Reembolsada'
    FROM inserted i
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE i.Estado = 4
      AND d.Estado <> 4;  -- only on transition TO Anulada

    -- Credit saldo for cancelled bets
    UPDATE Pagamentos.dbo.Saldo_Utilizador
    SET
        Saldo = Saldo + i.Valor_Apostado,
        Data_Hora_Atualizacao = GETDATE()
    FROM Pagamentos.dbo.Saldo_Utilizador su
    INNER JOIN inserted i ON su.Utilizador_ID = i.Utilizador_ID
    INNER JOIN deleted d ON i.ID = d.ID
    WHERE i.Estado = 4
      AND d.Estado <> 4;

    -- Estado = 3 (Perdida): no action needed.
    -- The AP debit was already created when the bet was registered.
END;
GO
