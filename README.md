#BetStrike - Sistema de Integração de Apostas

[![Integracão](https://img.shields.io/badge/Integração-Sistemas-blue)](https://www.utad.pt)
[![C#](https://img.shields.io/badge/Language-C%23-purple)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/DB-MS%20SQL%20Server-red)](https://www.microsoft.com/sql-server)

##Descrição do Projeto
Este repositório contém a solução desenvolvida para a disciplina de **Integração de Sistemas (Mestrado em Engenharia Informática - UTAD)**. O projeto foca na automação do fluxo de dados entre a **Federação Portuguesa de Futebol (FPF)** e a plataforma de apostas **BetStrike**, eliminando processos manuais e garantindo a resolução de apostas em tempo real.

##Arquitetura da Solução
A aplicação está dividida em serviços independentes que comunicam via APIs REST e triggers de base de dados:
* **Simulador FPF:** Gera eventos de jogos e golos em tempo fictício.
* **Plataforma BetStrike:** Gere utilizadores, odds e submissão de apostas.
* **Sistema de Pagamentos:** Base de dados isolada que processa transações financeiras via Stored Procedures.

##Grupo
* **Aluno 1:** Gonçalo Araújo   al78478   goncalo@utad.pt
* **Aluno 2:** João Cosme       al78351   al78351@alunos.utad.pt
