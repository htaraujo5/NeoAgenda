# 📇 NeoAgenda — Gestão Inteligente de Contatos & Privacidade LGPD

[![CI - Build & Test Validation](https://github.com/htaraujo5/NeoAgenda/actions/workflows/ci.yml/badge.svg)](https://github.com/htaraujo5/NeoAgenda/actions/workflows/ci.yml)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Vue 3](https://img.shields.io/badge/Vue.js-3.5-4FC08D?logo=vue.js)
![PrimeVue](https://img.shields.io/badge/PrimeVue-4.2-41B883?logo=primevue)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![LGPD Compliant](https://img.shields.io/badge/LGPD-Art._18_Compliant-059669)

Aplicação corporativa de gerenciamento de contatos resiliente, desenvolvida com API RESTful em **.NET 8** e interface reativa em **Vue 3** com componentes **PrimeVue 4**. O sistema foi concebido com foco estrito em integridade de dados, normalização telefônica canônica, importação tolerante a falhas, janela de retenção de 30 dias e conformidade legal com o **Direito ao Esquecimento da LGPD**.

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

O Docker realizará a compilação multi-stage, executará a suíte de testes automatizados dentro do container de build e iniciará os serviços:

| Serviço | URL | Descrição |
| :--- | :--- | :--- |
| **Frontend Web (Vue + PrimeVue)** | [http://localhost:8080](http://localhost:8080) | Interface gráfica com tabela reativa, lixeira e importador |
| **Backend API & Swagger** | [http://localhost:5000](http://localhost:5000) | Documentação interativa OpenAPI / Swagger UI |
| **Healthcheck da API** | [http://localhost:5000/health](http://localhost:5000/health) | Verificação de disponibilidade do backend |

*(A base de dados SQLite é inicializada automaticamente com dados de exemplo e persiste em um volume Docker nomeado).*

---

### Execução Local (Sem Docker)

Caso prefira rodar diretamente no seu ambiente de desenvolvimento:

#### 1. Backend (.NET 8 SDK)
```bash
cd backend
dotnet restore
dotnet test
dotnet run --project src/NeoAgenda.Api
# A API estará disponível em http://localhost:5000
```

#### 2. Frontend (Node.js 20+)
```bash
cd frontend
npm install
npm test
npm run dev
# O frontend estará disponível em http://localhost:5173 (com proxy automático para a API)
```

---

## 🏛️ Principais Decisões de Arquitetura e Regras de Negócio

### 1. Normalização Canônica e Unicidade de Telefone (RN01)
- **Problema:** Usuários e planilhas inserem telefones em dezenas de formatos distintos: `(11) 98765-4321`, `+55 11 98765-4321`, `5511987654321`, `11987654321`, `011 98765 4321`, `(11) 9.8765-4321` ou fixos como `(11) 3333-4444`.
- **Solução Implementada:**
  - Criação do serviço de domínio `PhoneNormalizer`.
  - Converte qualquer formato válido para a representação **canônica internacional E.164** (`+55{DDD}{Número}`).
  - Valida o conjunto oficial de DDDs de todas as unidades federativas do Brasil.
  - Distingue celulares (11 dígitos, nono dígito `9` obrigatório) de fixos (10 dígitos, faixa 2 a 5).
  - Trata prefixos de saída internacional (`00`) e prefixos de longa distância (`0`).
- **Garantia no Banco de Dados:** Além da validação na camada de aplicação, o banco possui um **Filtered Unique Index**:
  ```sql
  CREATE UNIQUE INDEX "IX_Contacts_PhoneNormalized" ON "Contacts" ("PhoneNormalized") WHERE "IsDeleted" = false;
  ```
  Isso garante concorrência segura sem risco de race conditions.

### 2. Importação Resiliente de CSV (RN02)
- **Problema:** Lotes de importação costumam ser rejeitados inteiramente caso uma única linha possua um erro de digitação.
- **Solução Implementada:**
  - `CsvContactParser`: processador em stream tolerante a falhas.
  - **Detecção automática de delimitadores:** Suporta vírgula (`,`), ponto-e-vírgula (`;`) e tabulação (`\t`).
  - **Mapeamento flexível de cabeçalhos:** Reconhece sinônimos em português e inglês (`nome`/`name`, `email`/`e-mail`, `telefone`/`phone`/`celular`).
  - **Resiliência linha a linha:** Linhas válidas são persistidas; linhas inconsistentes geram um relatório com o número exato da linha, campo afetado e motivo do erro.
  - **Detecção intra-arquivo:** Se a linha 2 e a linha 10 do mesmo arquivo contiverem o mesmo telefone formatado diferente, a primeira é importada e a segunda é rejeitada com aviso explicativo.

### 3. Exclusão com Janela de Restauração de 30 Dias (RN03)
- O contato excluído recebe `IsDeleted = true`, `DeletedAt = UtcNow` e `RetentionExpiresAt = DeletedAt + 30 dias`.
- As consultas padrão utilizam o **Global Query Filter** do EF Core (`HasQueryFilter(c => !c.IsDeleted)`).
- Na aba **Lixeira**, os contatos são exibidos com uma **Tag de contagem regressiva** (`Restam X dias`).
- **Restauração Segura:** Caso o usuário tente restaurar um contato cujo número foi cadastrado por outra pessoa durante os 30 dias, a API informa o conflito amigavelmente impedindo duplicidades.

### 4. Exclusão Definitiva por LGPD (RN04)
- Atende ao **Direito ao Esquecimento** previsto no **Art. 18, VI da Lei 13.709/2018 (LGPD)**.
- **Expurgo Completo dos Dados Pessoais (PII):** Remoção física imediata (`Hard Delete`) da tabela de contatos.
- **Trilha de Conformidade Anônima:** Gera um registro na tabela `LgpdComplianceAudits` com um hash irreversível SHA-256 do titular, timestamp UTC e protocolo para comprovação perante órgãos reguladores (sem manter nome, email ou telefone legíveis).

---

## 🌟 Recursos Extras Implementados (Diferenciais de Engenharia)

Além dos requisitos obrigatórios, foram incorporados recursos avançados que agregam valor real ao produto:

1. **📁 Arquivos de Amostra Prontos para Teste (`samples/`):**
   - [contatos_validos.csv](samples/contatos_validos.csv): 7 contatos com múltiplos DDDs, formatos e DDI.
   - [contatos_com_erros.csv](samples/contatos_com_erros.csv): 7 linhas desenhadas para demonstrar a resiliência visual da RN02 (e-mails inválidos, telefones curtos, DDD 00 inexistente e duplicidade intra-arquivo).
2. **⏰ Worker em Segundo Plano para Expurgo da Lixeira (`BackgroundService` .NET):**
   - `RecycleBinPurgeBackgroundService`: serviço periódico que varre a base expurgando fisicamente contatos com mais de 30 dias na lixeira, emitindo logs estruturados no `ILogger`.
3. **🛡️ Painel e Endpoint de Auditoria LGPD (`/api/contacts/lgpd-audits`):**
   - Aba dedicada na interface web com tabela de protocolos, hashes criptográficos SHA-256 anônimos, data/hora UTC e exportação em formato JSON para auditoria da ANPD.
4. **🌓 Modo Escuro / Claro Dinâmico (Dark Mode Switcher):**
   - Alternador de tema no cabeçalho com persistência em `localStorage` e suporte nativo pelo design system do PrimeVue Aura.
5. **📊 Relatório Visual de Testes e Cobertura no CI:**
   - O GitHub Actions publica automaticamente o resumo da execução de testes do backend (.NET) e frontend (Vitest) na aba do workflow e nos Pull Requests.

---

## ⚖️ Trade-offs Assumidos e O Que Ficou de Fora

1. **Autenticação e Multi-Tenancy:**
   - *Decisão:* Não incluímos controle de login JWT ou tenants.
   - *Motivo:* O teste especificou esforço esperado de 4 a 6 horas focado nas regras de negócio da agenda. Adicionar OAuth2/JWT desviaria o foco do desafio sem agregar valor à conversa técnica principal.
2. **Mensageria Assíncrona (RabbitMQ/Kafka) para CSV:**
   - *Decisão:* O processamento do CSV é feito de forma síncrona em stream.
   - *Motivo:* Para arquivos de até dezenas de milhares de linhas, o processamento direto em memória/stream leva menos de 2 segundos. Introduzir brokers externos traria complexidade de infraestrutura desnecessária para o escopo.
3. **Serviço de Background Cron para Purga Automática da Lixeira:**
   - *Decisão:* A regra de expiração de 30 dias é validada em tempo real na camada de domínio (`RetentionExpiresAt < UtcNow`).
   - *Motivo:* Um contato com mais de 30 dias já fica bloqueado para restauração automaticamente. Um `BackgroundService` de limpeza em batch pode ser facilmente acoplado se necessário.

---

## 🤖 Como a Inteligência Artificial Foi Utilizada

O uso de IA neste projeto seguiu rigorosos critérios de engenharia de software:

### 1. O que foi aceito da IA
- **Scaffolding e Estrutura:** Criação da estrutura de pastas da Clean Architecture (.NET) e componentes Vue 3 + PrimeVue.
- **Mapeamento de Regras Oficiais:** Tabela com todos os DDDs válidos do Brasil segundo o plano da Anatel.
- **Suíte de Testes Automatizados:** Geração de múltiplos cenários de testes unitários para o normalizador de telefone e parser de CSV.
- **OpenAPI e Documentação:** Geração de descrições dos contratos RESTful seguindo a RFC 7807 (ProblemDetails).

### 2. O que foi corrigido
- **Algoritmo de Zeros na Normalização de Telefone:** A IA inicialmente sugeriu `digitsOnly = digitsOnly.TrimStart('0')`. Ao testar o DDD `00` (inválido), essa abordagem apagava ambos os zeros, reduzindo o número para 9 dígitos e gerando erro de tamanho ao invés de código de área inválido. A lógica foi reescrita manualmente para distinguir chamadas com zero antes do DDD de DDDs iniciados em zero.
- **Configuração no EF Core:** Foi corrigida a chamada de extensão de configuração que exigia o pacote `Microsoft.Extensions.Configuration.Binder`.
- **Testes Unitários no Frontend com PrimeVue:** Os componentes `<Dialog>` do PrimeVue usam `<Teleport to="body">` por padrão, o que deixava o DOM do teste vazio no Vitest. Ajustamos o componente para aceitar `appendTo="self"` durante os testes unitários.

### 3. O que foi rejeitado
- **Bibliotecas Externas Opacas:** Sugestões de pacotes pesados como `libphonenumber-csharp` foram rejeitadas para manter o controle total, leveza e aderência específica às regras de telefonia brasileiras.
- **Hard Delete Desgovernado para LGPD:** Propostas iniciais de apagar o registro sem gerar comprovante foram rejeitadas. Implementamos a tabela de auditoria anônima com hash SHA-256 para compliance legal real com a LGPD.
- **Overengineering de Microserviços:** Propostas de segregar em múltiplos serviços e bancos distribuídos foram descartadas em prol de uma solução modular, limpa e coesa.

---

## ⏱️ Esforço e Tempo Total Investido

| Etapa | Atividades | Tempo Estimado |
| :--- | :--- | :---: |
| **Planejamento & Specs** | Leitura de requisitos, modelagem de dados, arquitetura em `especs/` e setup Git Flow | ~45 min |
| **Backend Core & Domínio** | Entidades, ciclo de vida de soft delete (30 dias), normalizador canônico e testes xUnit | ~1h 10 min |
| **Importador CSV Resiliente** | Parser com detecção de delimitadores, tolerância a falhas e testes unitários | ~50 min |
| **Pipeline de CI (GitHub Actions)** | Configuração de workflows automatizados para backend e frontend e validações | ~30 min |
| **Infraestrutura EF Core & API** | Repositórios, Filtered Unique Index, controllers RESTful e testes de integração | ~1h 00 min |
| **Frontend Vue 3 + PrimeVue** | Interface com DataTable, modais com máscara, contagem regressiva, LGPD e Vitest | ~1h 15 min |
| **Docker, Compose & Documentação** | Dockerfiles multi-stage, proxy Nginx, Compose e README executivo | ~40 min |
| **Total Investido** | **Dentro da faixa esperada pelo desafio (4 a 6 horas)** | **~5h 10 min** |

---

## 🧪 Estrutura de Testes Automatizados

O projeto conta com mais de **60 testes automatizados** distribuídos entre:
- **Testes Unitários de Domínio:** `PhoneNormalizerTests`, `ContactEntityTests`.
- **Testes Unitários de Aplicação:** `ContactServiceTests`, `ContactServiceCsvTests`, `CsvContactParserTests`.
- **Testes de Integração de API:** `ContactsControllerIntegrationTests` (com `WebApplicationFactory` e SQLite in-memory).
- **Testes Unitários de Frontend:** `client.spec.ts`, `contactStore.spec.ts`, `LgpdConfirmDialog.spec.ts` (Vitest).

Todos os testes são executados automaticamente a cada Pull Request pelo GitHub Actions.
