# 📇 NeoAgenda — Gestão Inteligente de Contatos & Privacidade LGPD

[![CI - Build & Test Validation](https://github.com/htaraujo5/NeoAgenda/actions/workflows/ci.yml/badge.svg)](https://github.com/htaraujo5/NeoAgenda/actions/workflows/ci.yml)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Vue 3](https://img.shields.io/badge/Vue.js-3.5-4FC08D?logo=vue.js)
![PrimeVue](https://img.shields.io/badge/PrimeVue-4.2-41B883?logo=primevue)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![LGPD Compliant](https://img.shields.io/badge/LGPD-Art._18_Compliant-059669)

Aplicação corporativa de gerenciamento de contatos resiliente e multiusuário, desenvolvida com API RESTful em **.NET 8** e interface reativa em **Vue 3** com componentes **PrimeVue 4**. O sistema foi concebido com foco estrito em integridade de dados, normalização telefônica canônica (E.164), importação tolerante a falhas, janela de retenção de 30 dias (soft delete com expurgo automático), conformidade legal com a **LGPD (Art. 18, VI)**, autenticação JWT com Refresh Tokens, controle de acessos baseado em perfis (RBAC), auditoria de segurança e telemetria de desempenho (APM).

---

## 🚀 Como Rodar a Aplicação

A forma mais simples, rápida e recomendada de executar o ecossistema completo é via **Docker Compose**:

### Pré-requisitos
- [Docker](https://docs.docker.com/get-docker/) e [Docker Compose](https://docs.docker.com/compose/) instalados.

### Execução em 1 Comando
Na raiz do repositório, execute:

```bash
docker compose up --build
```

O Docker realizará a compilação multi-stage, executará as suítes de testes automatizados dentro dos containers e iniciará os serviços:

| Serviço | URL | Descrição |
| :--- | :--- | :--- |
| **Frontend Web (Vue 3 + PrimeVue)** | [http://localhost:8080](http://localhost:8080) (ou 8081) | Interface web reativa com Sidebar, tabela de contatos, lixeira e métricas |
| **Backend API & Swagger** | [http://localhost:5000](http://localhost:5000) | Documentação interativa OpenAPI / Swagger UI |
| **Healthcheck da API** | [http://localhost:5000/health](http://localhost:5000/health) | Verificação de disponibilidade e saúde do backend |

*(A base de dados SQLite é inicializada automaticamente com seeds de usuários padrão e persiste em um volume Docker nomeado).*

---

## 🔑 Credenciais Padrão Pré-configuradas (Seeds)

| Usuário | Senha | Papel (Role) | Permissões |
| :--- | :--- | :--- | :--- |
| **`admin`** | `Admin@123` | **`Admin`** | **Acesso Global**: gerencia e visualiza todas as agendas, exclusão/expurgo total, gestão de usuários, auditoria completa e APM. |
| **`usuario`** | `User@123` | **`Usuario`** | **Agenda Pessoal**: gerencia apenas seus próprios contatos, histórico pessoal de movimentações e importação própria de CSV. |

---

## 🏛️ Principais Regras de Negócio e Recursos de Engenharia

### 1. Normalização Canônica e Unicidade de Telefone (RN01)
- Converte qualquer formato válido para a representação **canônica internacional E.164** (`+55{DDD}{Número}`).
- Validação estrita do conjunto de DDDs oficiais de todas as UFs brasileiras segundo o plano da Anatel.
- Distingue celulares (11 dígitos, nono dígito `9` obrigatório) de fixos (10 dígitos, dígitos iniciais 2 a 5).
- **Garantia Concorrente no Banco de Dados:** Além da validação de aplicação, o banco possui um **Filtered Unique Index**:
  ```sql
  CREATE UNIQUE INDEX "IX_Contacts_PhoneNormalized" ON "Contacts" ("PhoneNormalized") WHERE "IsDeleted" = false;
  ```

### 2. Importação Resiliente de CSV (RN02)
- Processador em stream com detecção automática de delimitadores (`,`, `;`, `\t`) e encodings (UTF-8, Latin1).
- Mapeamento flexível de cabeçalhos (`nome`/`name`, `email`/`e-mail`, `telefone`/`phone`/`celular`).
- **Resiliência linha a linha:** Linhas válidas são persistidas; linhas inconsistentes geram relatório com número da linha, campo afetado, motivo e botão de download do arquivo de erros.
- Detecção e bloqueio de duplicidades no próprio lote CSV.

### 3. Exclusão com Janela de Restauração de 30 Dias (RN03)
- Soft Delete com `RetentionExpiresAt = DeletedAt + 30 dias`.
- **Worker em Segundo Plano (`RecycleBinPurgeBackgroundService`):** Serviço hospedado do .NET que roda periodicamente e expurga registros fisicamente após os 30 dias.
- Exibição de contagem regressiva em dias restantes na lixeira.
- Restauração segura com verificação de colisão caso o número tenha sido cadastrado por outro contato ativo.

### 4. Exclusão Definitiva por LGPD (RN04)
- Atende ao **Direito ao Esquecimento** do **Art. 18, VI da Lei 13.709/2018 (LGPD)**.
- **Hard Delete dos Dados Pessoais (PII):** Destruição física irreversível do registro na base de contatos.
- **Trilha de Auditoria Criptográfica:** Inserção na tabela `LgpdComplianceAudits` com hash SHA-256 anônimo (`PhoneNormalized + ContactId`) para comprovação legal perante a ANPD, sem armazenar dados pessoais legíveis.

### 5. Controle de Acessos, Auditoria & Telemetria APM (RN05 / RN06)
- **Autenticação JWT & Refresh Token Rotation:** Sessão segura com expiração de 60 minutos e renovação em segundo plano por 7 dias.
- **Segregação de Agenda por Usuário:** Usuários comuns operam somente sua própria lista; Administradores visualizam e podem excluir contatos de qualquer agenda.
- **Trilha de Auditoria de Acesso:** Registro detalhado de logins (sucessos e falhas) com IP, User-Agent e data/hora.
- **Telemetria de Desempenho (APM):** Middleware que intercepta requisições HTTP e calcula métricas de latência (média, p50, p95, p99) e taxa de erro.

---

## 🧪 Estratégia e Cobertura de Testes Automatizados

O projeto conta com testes automatizados executados tanto localmente quanto no pipeline de CI:
- **Backend (.NET 8):**
  - **55 Testes Unitários:** Validação exaustiva do `PhoneNormalizer`, regras de ciclo de vida, permissões de Admin/Usuário, tolerância do parser CSV e cálculo de retenção.
  - **8 Testes de Integração:** Fluxos HTTP completos com `WebApplicationFactory` e SQLite in-memory (autenticação JWT, ciclo de vida do contato, conflito 409, lixeira, restore, expurgo LGPD e importação de arquivo real).
- **Frontend (Vue 3 / Vitest):**
  - **8 Testes Unitários:** Clientes HTTP, stores Pinia (`authStore`, `contactStore`) e componentes de confirmação de exclusão LGPD.

Para executar os testes manualmente:
```bash
# Testes do Backend
dotnet test backend/NeoAgenda.sln

# Testes do Frontend
cd frontend && npm test
```

---

## ⚖️ Trade-offs Assumidos e Decisões Técnicas

1. **SQLite Persistente para o Desafio:**
   - *Decisão:* Utilizamos SQLite no Docker Compose por simplificar a inicialização imediata sem dependência de containers pesados de banco, com total suporte a Filtered Unique Indexes. A Clean Architecture permite plugar PostgreSQL ou SQL Server alterando apenas a Connection String no `appsettings.json`.
2. **Processamento em Stream vs Mensageria:**
   - *Decisão:* O parser de CSV opera diretamente em stream de memória com tempo de resposta na casa de milissegundos para lotes usuais, evitando custos e complexidades desnecessárias de mensageria externa (RabbitMQ/Kafka).
3. **Formulários Guiados:**
   - *Decisão:* Na gestão de acessos, a atribuição de papéis é restrita a uma lista suspensa (`Admin` ou `Usuario`), prevenindo inconsistências no banco.

---

## 🤖 Uso Transparente de Inteligência Artificial

A utilização de IA neste projeto seguiu diretrizes de arquitetura e validação contínua:

### 1. O que foi aceito da IA
- Criação inicial da estrutura em Clean Architecture (.NET) e configuração dos componentes PrimeVue.
- Mapeamento inicial da tabela oficial de códigos DDD da Anatel.
- Esboço de cenários de teste unitários para casos limites de telefonia e CSV.
- Geração de documentação dos contratos OpenAPI (Swagger) segundo a RFC 7807.

### 2. O que foi corrigido e refinado
- **Normalização de Zeros Telefônicos:** A IA sugeriu inicialmente usar `TrimStart('0')`, o que causava a perda de zeros em números internacionais e mascarava erros de DDDs inválidos como `00`. A lógica foi reescrita manualmente para validar códigos de área estritamente antes do descarte de zeros de operadora.
- **Evento de Clique Duplo no PrimeVue:** No componente de confirmação LGPD, a combinação de `@click` no container pai com o checkbox interno causava duplo chaveamento. O evento foi desacoplado e atrelado diretamente ao `id` do `<label>` HTML.
- **Isolamento de Conexão no SQLite In-Memory:** Nos testes de integração, múltiplas instâncias colidiam no banco `:memory:`. Ajustamos a connection string para usar `Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared`.

### 3. O que foi rejeitado
- **Bibliotecas Externas Excessivas:** Rejeitada a adoção de bibliotecas opacas pesadas (ex: `libphonenumber-csharp`), optando por um domínio limpo e especializado na telefonia brasileira e padrão canônico E.164.

---

## ⏱️ Registro de Tempo Investido

- **Planejamento, Especificação Técnica & Arquitetura (`especs/`):** 1h 00m
- **Backend Core, Normalização Telefônica & Testes Unitários:** 1h 30m
- **Motor Resiliente de Importação CSV & Testes:** 1h 15m
- **Infraestrutura EF Core, RESTful API & Testes de Integração:** 1h 15m
- **Frontend Vue 3 + PrimeVue & Experiência do Usuário:** 1h 45m
- **Autenticação JWT, RBAC, Auditoria de Segurança & APM:** 1h 30m
- **Acesso Global do Administrador & Segregação de Agenda:** 0h 45m
- **Containerização Docker, CI GitHub Actions & Ajustes Finais:** 0h 45m
- **Tempo Total:** ~9h 45m
