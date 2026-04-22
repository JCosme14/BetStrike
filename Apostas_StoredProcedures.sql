USE Apostas;
GO

-- Insert a new game
CREATE PROCEDURE sp_InsertJogo
    @Codigo_Jogo VARCHAR(20),
    @Data_Hora_Inicio DATETIME,
    @Equipa_Casa VARCHAR(100),
    @Equipa_Fora VARCHAR(100),
    @Tipo_Competicao VARCHAR(100) = NULL,
    @Estado INT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo)
    BEGIN
        RAISERROR('Já existe um jogo com o código %s.', 16, 1, @Codigo_Jogo);
        RETURN;
    END

    INSERT INTO Jogo (Codigo_Jogo, Data_Hora_Inicio, Equipa_Casa, Equipa_Fora, Tipo_Competicao, Estado)
    VALUES (@Codigo_Jogo, @Data_Hora_Inicio, @Equipa_Casa, @Equipa_Fora, @Tipo_Competicao, @Estado);

    SELECT SCOPE_IDENTITY() AS ID;
END;
GO

-- Get all games with optional filters
CREATE PROCEDURE sp_GetJogos
    @Data DATETIME = NULL,
    @Estado INT = NULL,
    @Tipo_Competicao VARCHAR(100) = NULL
AS
BEGIN
    SELECT * FROM Jogo
    WHERE (@Data IS NULL OR CAST(Data_Hora_Inicio AS DATE) = CAST(@Data AS DATE))
    AND (@Estado IS NULL OR Estado = @Estado)
    AND (@Tipo_Competicao IS NULL OR Tipo_Competicao = @Tipo_Competicao);
END;
GO

-- Get a specific game by code
CREATE PROCEDURE sp_GetJogo
    @Codigo_Jogo VARCHAR(20)
AS
BEGIN
    SELECT * FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo;
END;
GO

-- Update a game's state and live score
CREATE PROCEDURE sp_UpdateJogo
    @Codigo_Jogo VARCHAR(20),
    @Estado INT,
    @Golos_Casa INT = NULL,
    @Golos_Fora INT = NULL
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo)
    BEGIN
        RAISERROR('Jogo %s não encontrado.', 16, 2, @Codigo_Jogo);
        RETURN;
    END

    DECLARE @EstadoAtual INT;
    SELECT @EstadoAtual = Estado FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo;

    IF @EstadoAtual = 3 AND @Estado = 2
    BEGIN
        RAISERROR('Um jogo Finalizado não pode voltar a Em Curso.', 16, 3);
        RETURN;
    END

    IF @EstadoAtual IN (4, 5)
    BEGIN
        RAISERROR('Um jogo Cancelado ou Adiado não pode mudar de estado.', 16, 3);
        RETURN;
    END

    -- Update state and live score (score columns only updated when provided)
    UPDATE Jogo
    SET
        Estado = @Estado,
        Golos_Casa = ISNULL(@Golos_Casa, Golos_Casa),
        Golos_Fora = ISNULL(@Golos_Fora, Golos_Fora)
    WHERE Codigo_Jogo = @Codigo_Jogo;

    -- If game is finished, insert result FIRST then resolve bets
    IF @Estado = 3
    BEGIN
        DECLARE @Jogo_ID INT;
        SELECT @Jogo_ID = ID FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo;

        -- Insert result before resolving so sp_ResolverApostas can read the score
        IF NOT EXISTS (SELECT 1 FROM Resultado WHERE Jogo_ID = @Jogo_ID)
        BEGIN
            INSERT INTO Resultado (Jogo_ID, Golos_Casa, Golos_Fora)
            VALUES (@Jogo_ID, ISNULL(@Golos_Casa, 0), ISNULL(@Golos_Fora, 0));
        END
        ELSE
        BEGIN
            -- Update existing result with final score
            UPDATE Resultado
            SET
                Golos_Casa = ISNULL(@Golos_Casa, Golos_Casa),
                Golos_Fora = ISNULL(@Golos_Fora, Golos_Fora),
                Data_Hora_Atualizacao = GETDATE()
            WHERE Jogo_ID = @Jogo_ID;
        END

        EXEC sp_ResolverApostas @Jogo_ID;
    END

    -- If game is cancelled or postponed, annul all pending bets
    IF @Estado IN (4, 5)
    BEGIN
        DECLARE @Jogo_ID2 INT;
        SELECT @Jogo_ID2 = ID FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo;
        UPDATE Aposta SET Estado = 4 WHERE Jogo_ID = @Jogo_ID2 AND Estado = 1;
    END
END;
GO

-- Delete a game (only if Scheduled)
CREATE PROCEDURE sp_DeleteJogo
    @Codigo_Jogo VARCHAR(20)
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo AND Estado = 1)
    BEGIN
        RAISERROR('Só é permitido remover jogos no estado Agendado.', 16, 4);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Aposta a INNER JOIN Jogo j ON a.Jogo_ID = j.ID WHERE j.Codigo_Jogo = @Codigo_Jogo)
    BEGIN
        RAISERROR('Não é permitido remover um jogo com apostas associadas.', 16, 4);
        RETURN;
    END

    DELETE FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo;
END;
GO

-- Insert a new user
CREATE PROCEDURE sp_InsertUtilizador
    @Nome VARCHAR(100),
    @Email VARCHAR(100)
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Utilizador WHERE Email = @Email)
    BEGIN
        RAISERROR('Já existe um utilizador com o email %s.', 16, 5, @Email);
        RETURN;
    END

    INSERT INTO Utilizador (Nome, Email)
    VALUES (@Nome, @Email);

    SELECT SCOPE_IDENTITY() AS ID;
END;
GO

-- Get a specific user
CREATE PROCEDURE sp_GetUtilizador
    @ID INT
AS
BEGIN
    SELECT * FROM Utilizador WHERE ID = @ID;
END;
GO

-- Insert a new bet
CREATE PROCEDURE sp_InsertAposta
    @Jogo_ID INT,
    @Utilizador_ID INT,
    @Tipo_Aposta CHAR(1),
    @Valor_Apostado DECIMAL(10,2),
    @Odd_Momento DECIMAL(10,2)
AS
BEGIN
    -- Validate game state
    DECLARE @EstadoJogo INT;
    SELECT @EstadoJogo = Estado FROM Jogo WHERE ID = @Jogo_ID;

    IF @EstadoJogo IN (3, 4, 5)
    BEGIN
        RAISERROR('Não é permitido registar apostas neste jogo.', 16, 6);
        RETURN;
    END

    -- Validate bet type
    IF @Tipo_Aposta NOT IN ('1', 'X', '2')
    BEGIN
        RAISERROR('Tipo de aposta inválido. Deve ser 1, X ou 2.', 16, 6);
        RETURN;
    END

    -- Validate amount
    IF @Valor_Apostado <= 0
    BEGIN
        RAISERROR('O valor apostado deve ser maior que zero.', 16, 6);
        RETURN;
    END

    -- Validate odd
    IF @Odd_Momento <= 1.0
    BEGIN
        RAISERROR('A odd deve ser maior que 1.0.', 16, 6);
        RETURN;
    END

    INSERT INTO Aposta (Jogo_ID, Utilizador_ID, Tipo_Aposta, Valor_Apostado, Odd_Momento, Estado)
    VALUES (@Jogo_ID, @Utilizador_ID, @Tipo_Aposta, @Valor_Apostado, @Odd_Momento, 1);

    SELECT SCOPE_IDENTITY() AS ID;
END;
GO

-- Get bets with filters
CREATE PROCEDURE sp_GetApostas
    @Utilizador_ID INT = NULL,
    @Jogo_ID INT = NULL,
    @Estado INT = NULL,
    @Data_Inicio DATETIME = NULL,
    @Data_Fim DATETIME = NULL
AS
BEGIN
    SELECT * FROM Aposta
    WHERE (@Utilizador_ID IS NULL OR Utilizador_ID = @Utilizador_ID)
    AND (@Jogo_ID IS NULL OR Jogo_ID = @Jogo_ID)
    AND (@Estado IS NULL OR Estado = @Estado)
    AND (@Data_Inicio IS NULL OR Data_Hora_Aposta >= @Data_Inicio)
    AND (@Data_Fim IS NULL OR Data_Hora_Aposta <= @Data_Fim);
END;
GO

-- Get a specific bet
CREATE PROCEDURE sp_GetAposta
    @ID INT
AS
BEGIN
    SELECT * FROM Aposta WHERE ID = @ID;
END;
GO

-- Cancel a bet
CREATE PROCEDURE sp_CancelarAposta
    @ID INT
AS
BEGIN
    DECLARE @EstadoAposta INT;
    DECLARE @EstadoJogo INT;

    SELECT @EstadoAposta = a.Estado, @EstadoJogo = j.Estado
    FROM Aposta a
    INNER JOIN Jogo j ON a.Jogo_ID = j.ID
    WHERE a.ID = @ID;

    IF @EstadoAposta != 1
    BEGIN
        RAISERROR('Só é permitido cancelar apostas pendentes.', 16, 7);
        RETURN;
    END

    IF @EstadoJogo != 1
    BEGIN
        RAISERROR('Só é permitido cancelar apostas em jogos Agendados.', 16, 7);
        RETURN;
    END

    UPDATE Aposta SET Estado = 4 WHERE ID = @ID;
END;
GO

-- Resolve bets for a finished game
CREATE PROCEDURE sp_ResolverApostas
    @Jogo_ID INT
AS
BEGIN
    DECLARE @Golos_Casa INT;
    DECLARE @Golos_Fora INT;

    SELECT @Golos_Casa = Golos_Casa, @Golos_Fora = Golos_Fora
    FROM Resultado WHERE Jogo_ID = @Jogo_ID;

    -- Resolve type 1 bets
    UPDATE Aposta SET Estado = CASE
        WHEN @Golos_Casa > @Golos_Fora THEN 2
        ELSE 3
    END
    WHERE Jogo_ID = @Jogo_ID AND Estado = 1 AND Tipo_Aposta = '1';

    -- Resolve type X bets
    UPDATE Aposta SET Estado = CASE
        WHEN @Golos_Casa = @Golos_Fora THEN 2
        ELSE 3
    END
    WHERE Jogo_ID = @Jogo_ID AND Estado = 1 AND Tipo_Aposta = 'X';

    -- Resolve type 2 bets
    UPDATE Aposta SET Estado = CASE
        WHEN @Golos_Fora > @Golos_Casa THEN 2
        ELSE 3
    END
    WHERE Jogo_ID = @Jogo_ID AND Estado = 1 AND Tipo_Aposta = '2';
END;
GO

-- Insert a result
CREATE PROCEDURE sp_InsertResultado
    @Jogo_ID INT,
    @Golos_Casa INT,
    @Golos_Fora INT
AS
BEGIN
    DECLARE @EstadoJogo INT;
    SELECT @EstadoJogo = Estado FROM Jogo WHERE ID = @Jogo_ID;

    IF @EstadoJogo != 3
    BEGIN
        RAISERROR('Só é permitido inserir resultado para jogos Finalizados.', 16, 8);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Resultado WHERE Jogo_ID = @Jogo_ID)
    BEGIN
        RAISERROR('Já existe um resultado para este jogo.', 16, 8);
        RETURN;
    END

    INSERT INTO Resultado (Jogo_ID, Golos_Casa, Golos_Fora)
    VALUES (@Jogo_ID, @Golos_Casa, @Golos_Fora);
END;
GO

-- Get a result
CREATE PROCEDURE sp_GetResultado
    @Jogo_ID INT
AS
BEGIN
    SELECT * FROM Resultado WHERE Jogo_ID = @Jogo_ID;
END;
GO

-- Get statistics for a game
CREATE PROCEDURE sp_GetEstatisticasJogo
    @Jogo_ID INT
AS
BEGIN
    SELECT
        SUM(Valor_Apostado) AS TotalApostado,
        SUM(CASE WHEN Tipo_Aposta = '1' THEN 1 ELSE 0 END) AS NumApostas1,
        SUM(CASE WHEN Tipo_Aposta = 'X' THEN 1 ELSE 0 END) AS NumApostasX,
        SUM(CASE WHEN Tipo_Aposta = '2' THEN 1 ELSE 0 END) AS NumApostas2,
        SUM(CASE WHEN Estado = 1 THEN 1 ELSE 0 END) AS NumApostasPendentes,
        SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END) AS NumApostasGanhas,
        SUM(CASE WHEN Estado = 3 THEN 1 ELSE 0 END) AS NumApostasPerdidas,
        SUM(CASE WHEN Estado = 4 THEN 1 ELSE 0 END) AS NumApostasAnuladas,
        SUM(Valor_Apostado) - SUM(CASE WHEN Estado = 2 THEN Valor_Apostado * Odd_Momento ELSE 0 END) AS MargemPlataforma
    FROM Aposta
    WHERE Jogo_ID = @Jogo_ID;
END;
GO

-- Get statistics for a competition
CREATE PROCEDURE sp_GetEstatisticasCompeticao
    @Tipo_Competicao VARCHAR(100)
AS
BEGIN
    SELECT
        AVG(CAST(r.Golos_Casa + r.Golos_Fora AS FLOAT)) AS MediaGolosPorJogo,
        SUM(a.Valor_Apostado) AS TotalApostado,
        AVG(CASE WHEN r.Golos_Casa > r.Golos_Fora THEN 1.0 ELSE 0.0 END) AS TaxaVitoria1,
        AVG(CASE WHEN r.Golos_Casa = r.Golos_Fora THEN 1.0 ELSE 0.0 END) AS TaxaVitoriaX,
        AVG(CASE WHEN r.Golos_Fora > r.Golos_Casa THEN 1.0 ELSE 0.0 END) AS TaxaVitoria2
    FROM Jogo j
    LEFT JOIN Resultado r ON j.ID = r.Jogo_ID
    LEFT JOIN Aposta a ON j.ID = a.Jogo_ID
    WHERE j.Tipo_Competicao = @Tipo_Competicao;
END;
GO