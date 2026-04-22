-- Create the three databases if they don't already exist
IF DB_ID('FPF') IS NULL CREATE DATABASE FPF;
GO
IF DB_ID('Apostas') IS NULL CREATE DATABASE Apostas;
GO
IF DB_ID('Pagamentos') IS NULL CREATE DATABASE Pagamentos;
GO