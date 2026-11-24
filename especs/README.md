# NeoAgenda — Especificações Técnicas e Contexto de IA (especs)

Este diretório contém a base de conhecimento estruturada, regras de negócio, contratos de API, arquitetura e convenções técnicas para a implementação do projeto **NeoAgenda**.

As especificações aqui reunidas funcionam como o **Contexto Primário de IA**, servindo de guia inequívoco para a geração de código, testes, automações, validações de compliance e revisões arquiteturais.

---

## 📚 Índice de Especificações

| Documento | Assunto | Objetivo |
| :--- | :--- | :--- |
| **[01-visao-geral-e-requisitos.md](file:///home/helisson/Projetos/NeoAgenda/especs/01-visao-geral-e-requisitos.md)** | Visão Geral, Escopo e Rastreabilidade | Objetivos da solução, requisitos funcionais e não funcionais, público-alvo e premissas. |
| **[02-regras-de-negocio-e-validacoes.md](file:///home/helisson/Projetos/NeoAgenda/especs/02-regras-de-negocio-e-validacoes.md)** | Regras de Negócio e Validações Críticas | Normalização de telefone, prevenção de duplicidade, importação CSV resiliente, ciclo de vida de exclusão (soft-delete 30 dias) e exclusão definitiva por LGPD. |
| **[03-arquitetura-e-padroes.md](file:///home/helisson/Projetos/NeoAgenda/especs/03-arquitetura-e-padroes.md)** | Arquitetura de Software e Padrões | Estrutura Clean Architecture / Vertical Slices (.NET 8/9 C#), Vue 3 + PrimeVue (Frontend), monorepo e convenções. |
| **[04-modelo-de-dados-e-persistencia.md](file:///home/helisson/Projetos/NeoAgenda/especs/04-modelo-de-dados-e-persistencia.md)** | Modelo de Dados e Banco de Dados | Entidades, diagramas ER, estratégia de índices únicos parciais (para soft-delete), migrações e auditoria. |
| **[05-contratos-api-rest.md](file:///home/helisson/Projetos/NeoAgenda/especs/05-contratos-api-rest.md)** | Especificação de Contratos da API REST | Endpoints, payloads (JSON e multipart), códigos de retorno HTTP, tratamento padronizado de erros (RFC 7807 ProblemDetails). |
| **[06-design-frontend-ux.md](file:///home/helisson/Projetos/NeoAgenda/especs/06-design-frontend-ux.md)** | Design de Interface e Experiência do Usuário (UX) | Componentes PrimeVue, telas (Listagem, Edição, Lixeira com contagem de 30 dias, Modal LGPD, Importador CSV com feedback). |
| **[07-estrategia-de-testes.md](file:///home/helisson/Projetos/NeoAgenda/especs/07-estrategia-de-testes.md)** | Estratégia e Casos de Testes Automatizados | Pirâmide de testes, matriz de testes unitários de regras críticas, testes de integração de API e testes de ponta a ponta. |
| **[08-infraestrutura-e-docker.md](file:///home/helisson/Projetos/NeoAgenda/especs/08-infraestrutura-e-docker.md)** | Infraestrutura, Docker e Execução | Dockerfiles multi-stage, Docker Compose unificado, scripts de execução e verificação de saúde da aplicação. |
| **[09-roadmap-implementacao.md](file:///home/helisson/Projetos/NeoAgenda/especs/09-roadmap-implementacao.md)** | Roadmap de Implementação e Git Flow | Fases de desenvolvimento, divisão de branches `feature/...` e entregáveis por etapa. |

---

## 🛠️ Stack Tecnológica Recomendada

- **Backend:** .NET 8/9, ASP.NET Core Web API, Entity Framework Core, FluentValidation, CsvHelper.
- **Frontend:** Vue.js 3 (Composition API com `<script setup>`), TypeScript, PrimeVue, PrimeIcons, Pinia, Axios.
- **Banco de Dados:** PostgreSQL (produção/container) / SQLite (para desenvolvimento e testes in-memory de alta velocidade).
- **Containerização:** Docker e Docker Compose.
- **Testes:** xUnit, FluentAssertions, Moq/NSubstitute, WebApplicationFactory, Vitest (frontend).
