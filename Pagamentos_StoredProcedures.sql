USE Pagamentos;
GO

-- Create initial balance for a new user (called when user is registered in Apostas)
-- Awards the 50.00€ platform promotion and records it as a DE (Deposit) transaction
CREATE OR ALTER PROCEDURE sp_CriarSaldoUtilizador
    @Utilizador_ID INT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Saldo_Utilizador WHERE Utilizador_ID = @Utilizador_ID)
    BEGIN
        RAISERROR('Já existe um saldo para o utilizador %d.', 16, 1, @Utilizador_ID);
        RETURN;
    END

    INSERT INTO Saldo_Utilizador (Utilizador_ID, Saldo, Data_Hora_Atualizacao)
    VALUES (@Utilizador_ID, 50.00, GETDATE());

    -- Record the initial deposit as a transaction
    INSERT INTO Transacao (Aposta_ID, Utilizador_ID, Tipo_Transacao, Valor, Data_Hora, Estado)
    VALUES (NULL, @Utilizador_ID, 'DE', 50.00, GETDATE(), 'Processada');
END;
GO

-- Get balance for a user
CREATE OR ALTER PROCEDURE sp_GetSaldo
    @Utilizador_ID INT
AS
BEGIN
    SELECT * FROM Saldo_Utilizador WHERE Utilizador_ID = @Utilizador_ID;
END;
GO

-- Deposit fictitious money (for testing)
CREATE OR ALTER PROCEDURE sp_Deposito
    @Utilizador_ID INT,
    @Valor DECIMAL(10,2)
AS
BEGIN
    IF @Valor <= 0
    BEGIN
        RAISERROR('O valor do depósito deve ser maior que zero.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM Saldo_Utilizador WHERE Utilizador_ID = @Utilizador_ID)
    BEGIN
        RAISERROR('Utilizador %d não encontrado no sistema de pagamentos.', 16, 2, @Utilizador_ID);
        RETURN;
    END

    UPDATE Saldo_Utilizador
    SET Saldo = Saldo + @Valor, Data_Hora_Atualizacao = GETDATE()
    WHERE Utilizador_ID = @Utilizador_ID;

    INSERT INTO Transacao (Aposta_ID, Utilizador_ID, Tipo_Transacao, Valor, Data_Hora, Estado)
    VALUES (NULL, @Utilizador_ID, 'DE', @Valor, GETDATE(), 'Processada');
END;
GO

-- Debit balance when a bet is placed (AP transaction)
CREATE OR ALTER PROCEDURE sp_DebitarAposta
    @Utilizador_ID INT,
    @Aposta_ID INT,
    @Valor DECIMAL(10,2)
AS
BEGIN
    DECLARE @SaldoAtual DECIMAL(10,2);
    SELECT @SaldoAtual = Saldo FROM Saldo_Utilizador WHERE Utilizador_ID = @Utilizador_ID;

    IF @SaldoAtual IS NULL
    BEGIN
        RAISERROR('Utilizador %d não encontrado no sistema de pagamentos.', 16, 1, @Utilizador_ID);
        RETURN;
    END

    IF @SaldoAtual < @Valor
    BEGIN
        RAISERROR('Saldo insuficiente para cobrir o valor apostado.', 16, 2);
        RETURN;
    END

    UPDATE Saldo_Utilizador
    SET Saldo = Saldo - @Valor, Data_Hora_Atualizacao = GETDATE()
    WHERE Utilizador_ID = @Utilizador_ID;

    INSERT INTO Transacao (Aposta_ID, Utilizador_ID, Tipo_Transacao, Valor, Data_Hora, Estado)
    VALUES (@Aposta_ID, @Utilizador_ID, 'AP', @Valor, GETDATE(), 'Processada');
END;
GO

-- Get all transactions for a user
CREATE OR ALTER PROCEDURE sp_GetTransacoes
    @Utilizador_ID INT
AS
BEGIN
    SELECT * FROM Transacao
    WHERE Utilizador_ID = @Utilizador_ID
    ORDER BY Data_Hora DESC;
END;
GO
