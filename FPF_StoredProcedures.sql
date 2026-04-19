USE FPF;
GO

-- Insert a new game
CREATE PROCEDURE sp_InsertJogo
    @Codigo_Jogo VARCHAR(20),
    @Data_Hora_Inicio DATETIME,
    @Equipa_Casa VARCHAR(100),
    @Equipa_Fora VARCHAR(100),
    @Estado INT
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo)
    BEGIN
        RAISERROR('Já existe um jogo com o código %s.', 16, 1, @Codigo_Jogo);
        RETURN;
    END

    INSERT INTO Jogo (Codigo_Jogo, Data_Hora_Inicio, Equipa_Casa, Equipa_Fora, Estado)
    VALUES (@Codigo_Jogo, @Data_Hora_Inicio, @Equipa_Casa, @Equipa_Fora, @Estado);

    SELECT SCOPE_IDENTITY() AS ID;
END;
GO

-- Get all games with optional filters
CREATE PROCEDURE sp_GetJogos
    @Data DATETIME = NULL,
    @Estado INT = NULL
AS
BEGIN
    SELECT * FROM Jogo
    WHERE (@Data IS NULL OR CAST(Data_Hora_Inicio AS DATE) = CAST(@Data AS DATE))
    AND (@Estado IS NULL OR Estado = @Estado);
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

-- Update a game's state and score
CREATE PROCEDURE sp_UpdateJogo
    @Codigo_Jogo VARCHAR(20),
    @Estado INT,
    @Golos_Casa INT,
    @Golos_Fora INT
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

    UPDATE Jogo
    SET Estado = @Estado, Golos_Casa = @Golos_Casa, Golos_Fora = @Golos_Fora
    WHERE Codigo_Jogo = @Codigo_Jogo;
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

    DELETE FROM Jogo WHERE Codigo_Jogo = @Codigo_Jogo;
END;
GO