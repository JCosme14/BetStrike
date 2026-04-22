USE Apostas;

CREATE TABLE Jogo (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Codigo_Jogo VARCHAR(20) NOT NULL UNIQUE,
    Data_Hora_Inicio DATETIME NOT NULL,
    Equipa_Casa VARCHAR(100) NOT NULL,
    Equipa_Fora VARCHAR(100) NOT NULL,
    Golos_Casa INT NOT NULL DEFAULT 0,
    Golos_Fora INT NOT NULL DEFAULT 0,
    Tipo_Competicao VARCHAR(100),
    Estado INT NOT NULL DEFAULT 1
);

CREATE TABLE Resultado (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Jogo_ID INT NOT NULL UNIQUE,
    Golos_Casa INT NOT NULL DEFAULT 0,
    Golos_Fora INT NOT NULL DEFAULT 0,
    Data_Hora_Atualizacao DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Resultado_Jogo FOREIGN KEY (Jogo_ID) 
        REFERENCES Jogo(ID) ON DELETE CASCADE
);

CREATE TABLE Utilizador (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    Data_Registo DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Aposta (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    Jogo_ID INT NOT NULL,
    Utilizador_ID INT NOT NULL,
    Tipo_Aposta CHAR(1) NOT NULL CHECK (Tipo_Aposta IN ('1', 'X', '2')),
    Valor_Apostado DECIMAL(10,2) NOT NULL CHECK (Valor_Apostado > 0),
    Odd_Momento DECIMAL(10,2) NOT NULL CHECK (Odd_Momento > 1.0),
    Estado INT NOT NULL DEFAULT 1,
    Data_Hora_Aposta DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Aposta_Jogo FOREIGN KEY (Jogo_ID) 
        REFERENCES Jogo(ID),
    CONSTRAINT FK_Aposta_Utilizador FOREIGN KEY (Utilizador_ID) 
        REFERENCES Utilizador(ID)
);