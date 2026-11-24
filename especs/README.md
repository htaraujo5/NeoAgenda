# NeoAgenda — Especificações Técnicas e Contexto de IA (especs)

Este diretório contém a base de conhecimento estruturada, regras de negócio, contratos de API, arquitetura e convenções técnicas para o projeto **NeoAgenda**.

As especificações aqui reunidas funcionam como o **Contexto Primário de IA**, servindo de guia inequívoco para a implementação, testes, auditoria, governança e revisões arquiteturais.

---

## 📚 Índice de Especificações

| Documento | Assunto | Objetivo |
| :--- | :--- | :--- |
| **[01-visao-geral-e-requisitos.md](file:///home/helisson/Projetos/NeoAgenda/especs/01-visao-geral-e-requisitos.md)** | Visão Geral, Escopo e Rastreabilidade | Objetivos da solução, matriz de requisitos funcionais e não funcionais, papéis e premissas. |
| **[02-regras-de-negocio-e-validacoes.md](file:///home/helisson/Projetos/NeoAgenda/especs/02-regras-de-negocio-e-validacoes.md)** | Regras de Negócio e Validações Críticas | Normalização de telefone, prevenção de duplicidade, importação CSV resiliente, ciclo de vida (soft-delete 30 dias), exclusão LGPD, autenticação JWT, RBAC, auditoria de acessos e telemetria APM. |
| **[03-arquitetura-e-padroes.md](file:///home/helisson/Projetos/NeoAgenda/especs/03-arquitetura-e-padroes.md)** | Arquitetura de Software e Padrões | Clean Architecture (.NET 8 C#), pipeline de middlewares, background worker, Vue 3 + PrimeVue com Sidebar modular e temas. |
| **[04-modelo-de-dados-e-persistencia.md](file:///home/helisson/Projetos/NeoAgenda/especs/04-modelo-de-dados-e-persistencia.md)** | Modelo de Dados e Banco de Dados | Diagrama ER, entidades (Contacts, Users, AccessLogs, RequestMetrics, LgpdAudits), índices únicos filtrados e persistência. |
| **[05-contratos-api-rest.md](file:///home/helisson/Projetos/NeoAgenda/especs/05-contratos-api-rest.md)** | Especificação de Contratos da API REST | Endpoints, cabeçalhos de autorização Bearer JWT, rotas de contatos, autenticação, gestão de acessos e métricas APM. |
| **[06-design-frontend-ux.md](file:///home/helisson/Projetos/NeoAgenda/especs/06-design-frontend-ux.md)** | Design de Interface e Experiência do Usuário (UX) | Componentes PrimeVue, navegação por Sidebar, separação de módulos, perfil pessoal com histórico de ações e dark mode. |
| **[07-estrategia-de-testes.md](file:///home/helisson/Projetos/NeoAgenda/especs/07-estrategia-de-testes.md)** | Estratégia e Casos de Testes Automatizados | Pirâmide de testes, matriz de testes unitários do backend (55 testes), testes de integração (8 testes) e frontend (8 testes Vitest). |
| **[08-infraestrutura-e-docker.md](file:///home/helisson/Projetos/NeoAgenda/especs/08-infraestrutura-e-docker.md)** | Infraestrutura, Docker e Execução | Topologia Docker Compose, volume persistente, credenciais de acesso padrão e portas. |
| **[09-roadmap-implementacao.md](file:///home/helisson/Projetos/NeoAgenda/especs/09-roadmap-implementacao.md)** | Roadmap de Implementação e Git Flow | Histórico completo de branches temáticas e entregas consolidadas. |
