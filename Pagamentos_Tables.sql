USE Pagamentos;

CREATE TABLE Saldo_Utilizador (
    Utilizador_ID INT NOT NULL PRIMARY KEY,
    Saldo DECIMAL(10,2) NOT NULL DEFAULT 0.00 CHECK (Saldo >= 0),
    Data_Hora_Atualizacao DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Transacao (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Aposta_ID INT NULL,
    Utilizador_ID INT NOT NULL,
    Tipo_Transacao CHAR(2) NOT NULL CHECK (Tipo_Transacao IN ('AP', 'PG', 'RE', 'DE', 'LV')),
    Valor DECIMAL(10,2) NOT NULL CHECK (Valor > 0),
    Data_Hora DATETIME NOT NULL DEFAULT GETDATE(),
    Estado VARCHAR(20) NOT NULL DEFAULT 'Pendente' CHECK (Estado IN ('Pendente', 'Processada', 'Falhada', 'Reembolsada'))
);