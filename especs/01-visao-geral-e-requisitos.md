# 01 — Visão Geral e Requisitos

## 1. Objetivo do Produto
O **NeoAgenda** é uma solução moderna e resiliente de gerenciamento de contatos corporativos e multiusuário, construída com foco em confiabilidade de dados, privacidade (aderência estrita à LGPD), facilidade de importação em lote, controle de acessos (RBAC), auditoria de segurança, telemetria APM e experiência de usuário fluida.

---

## 2. Requisitos do Desafio Técnico & Evoluções Arquiteturais

Com base no documento de requisitos do projeto (`requisitos.txt`) e na evolução para ambiente corporativo multiusuário:

1. **Stack Obrigatória:**
   - Backend: API em **.NET 8** (C#)
   - Frontend: **Vue 3** com ecossistema **PrimeVue 4**
2. **Entidade Principal:** Contato com atributos de domínio:
   - Identificador (`Id` Guid)
   - Nome (`Name`)
   - E-mail (`Email`)
   - Telefone Original (`PhoneOriginal`)
   - Telefone Canônico (`PhoneNormalized` em formato E.164)
   - Proprietário/Agenda (`CreatedBy` Username do autor)
   - Datas de Ciclo de Vida (`CreatedAt`, `UpdatedAt`)
   - Controles de Lixeira (`IsDeleted`, `DeletedAt`, `RetentionExpiresAt`)
3. **Regras Mandatórias de Negócio:**
   - **R1 - Telefone Único:** Não permitir telefone duplicado entre contatos ativos, independentemente de máscara, pontuação ou formatação.
   - **R2 - Importação CSV Resiliente:** Importação em lote via arquivo CSV, com tratamento e relatório diagnóstico de linhas inválidas sem abortar as linhas válidas.
   - **R3 - Exclusão com Restauração (Soft Delete):** Exclusão temporária com janela de restauração de até 30 dias e worker automático de purga.
   - **R4 - Exclusão Definitiva por LGPD:** Atendimento explícito ao Artigo 18, VI da LGPD (eliminação definitiva e irrevogável) com trilha de auditoria criptográfica SHA-256 anônima.
4. **Segurança Corporativa, Autenticação e Observabilidade:**
   - **Autenticação JWT Bearer + Refresh Token Rotation:** Sessão segura com tokens de acesso (60 min) e renovação automática (7 dias).
   - **RBAC (Role-Based Access Control):** Papéis segregados `Admin` (Administrador) e `Usuario` (Usuário Padrão).
   - **Multi-Agenda & Acesso Global de Administrador:** O usuário padrão acessa e gerencia exclusivamente a sua própria agenda de contatos; o Administrador possui visão panorâmica e controle irrestrito de exclusão (soft-delete e LGPD) sobre todas as agendas.
   - **Auditoria de Acessos & Segurança:** Registro contínuo de eventos de login (sucessos, falhas com motivo, IP e User-Agent) e histórico de atividades pessoais.
   - **Telemetria de Desempenho (APM):** Middleware de observabilidade de latência HTTP, percentis p50/p95/p99, throughput e taxa de erros.
5. **Entregáveis de Engenharia:**
   - Repositório Git com branches atômicas e histórico semântico de commits.
   - Suíte abrangente de testes automatizados (.NET xUnit + FluentAssertions + Moq + WebApplicationFactory e Frontend Vitest).
   - Documentação viva e especificações (`especs/`) sincronizadas com o estado real do software.
   - Execução em 1 comando via `docker compose up --build`.

---

## 3. Matriz de Requisitos Funcionais (RF)

| ID | Nome do Requisito | Descrição | Papel Mínimo | Criticidade |
| :--- | :--- | :--- | :--- | :--- |
| **RF01** | Cadastro de Contato | Criação de novo contato com validação e unicidade de telefone, vinculando ao usuário logado (`CreatedBy`). | `Usuario` | Alta |
| **RF02** | Edição de Contato | Atualização de dados cadastrais mantendo regras de integridade e unicidade de telefone. | `Usuario` (próprio) / `Admin` (todos) | Alta |
| **RF03** | Listagem Paginada | Listagem de contatos ativos com busca e paginação. Usuário vê sua agenda; Admin vê todas as agendas. | `Usuario` | Alta |
| **RF04** | Exclusão com Janela (Soft Delete) | Move o contato para a Lixeira com cálculo de expiração de 30 dias. | `Usuario` (próprio) / `Admin` (todos) | Alta |
| **RF05** | Restauração de Contato | Restaura contato da lixeira dentro dos 30 dias se o telefone continuar livre no momento. | `Usuario` (próprio) / `Admin` (todos) | Alta |
| **RF06** | Exclusão Definitiva LGPD | Expurgar irrevogavelmente dados pessoais conforme Art. 18, VI LGPD, registrando hash anônimo. | `Usuario` (próprio) / `Admin` (todos) | Crítica |
| **RF07** | Importação Resiliente de CSV | Processamento tolerante de lotes CSV com relatório de inconsistências por linha e download de rejeitados. | `Usuario` | Alta |
| **RF08** | Lixeira & Contagem Regressiva | Visualização de contatos retidos com prazo restante e ações de restauração ou expurgo LGPD. | `Usuario` (própria) / `Admin` (geral) | Média |
| **RF09** | Exportação de Contatos | Exportação dos contatos ativos da agenda em formato CSV padronizado UTF-8. | `Usuario` | Baixa |
| **RF10** | Autenticação & Refresh Token | Login com usuário e senha, geração de JWT e renovação segura via refresh token. | Público | Crítica |
| **RF11** | Gestão de Acessos & Usuários | Cadastro, revogação e atribuição de perfis de acesso (`Admin` ou `Usuario`). | `Admin` | Alta |
| **RF12** | Auditoria de Segurança | Consulta e filtragem de logs de acesso de segurança e histórico de logins do sistema. | `Admin` | Alta |
| **RF13** | Histórico Pessoal de Ações | Visualização na aba do usuário logado do histórico das suas próprias atividades e movimentações. | `Usuario` | Média |
| **RF14** | Métricas de Desempenho (APM) | Painel de monitoramento de performance com latência por endpoint, p95 e status HTTP. | `Admin` | Média |

---

## 4. Requisitos Não Funcionais (RNF)

| ID | Tipo | Especificação |
| :--- | :--- | :--- |
| **RNF01** | **Confiabilidade & Integridade** | Unicidade de telefone garantida no domínio e no banco por índice parcial exclusivo (`IsDeleted = false`). |
| **RNF02** | **Performance & Baixa Latência** | Processamento de importação e buscas paginadas otimizadas sem carregar bases inteiras em memória. |
| **RNF03** | **Portabilidade Docker** | Ecossistema completo 100% conteinerizado (API + Frontend Nginx + SQLite persistente), subindo com 1 comando. |
| **RNF04** | **Usabilidade & UX** | Navegação por menu lateral (Sidebar), layout modularizado, formulários com seleção estrita de funções e dark mode. |
| **RNF05** | **Privacidade & Conformidade LGPD** | Exclusão definitiva elimina dados pessoais sem deixar cópias residuais e audita via hash unidirecional SHA-256. |
| **RNF06** | **Segurança de Acessos** | Criptografia de senhas com algoritmo SHA-256 e comparação em tempo constante (`CryptographicOperations.FixedTimeEquals`). |
| **RNF07** | **Padronização de Erros** | API segue a RFC 7807 (Problem Details) para todas as respostas 4xx e 5xx. |
