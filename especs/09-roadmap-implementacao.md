# 09 — Roadmap de Implementação e Fluxo Git

Este documento consolida o histórico de entregas de engenharia e branches utilizadas no desenvolvimento do NeoAgenda.

---

## 🗺️ Mapa de Branches e Fases Concluídas

| Etapa | Branch | Escopo Principal | Status |
| :--- | :--- | :--- | :--- |
| **01** | `feature/backend-core-domain` | Entidades de domínio, ciclo de vida (30 dias), PhoneNormalizer canônico E.164 e testes unitários. | Concluído ✅ |
| **02** | `feature/backend-csv-parser` | Processador resiliente de CSV com diagnóstico linha a linha, tolerância a falhas e testes. | Concluído ✅ |
| **03** | `feature/backend-infra-api` | EF Core, índices únicos filtrados, controllers RESTful e testes de integração com WebApplicationFactory. | Concluído ✅ |
| **04** | `feature/frontend-vue-primevue` | Interface Vue 3 + PrimeVue com gerenciamento de contatos, lixeira com countdown, modal LGPD e importador CSV. | Concluído ✅ |
| **05** | `feature/docker-and-docs` | Dockerfile multi-stage, docker-compose unificado e documentação completa. | Concluído ✅ |
| **06** | `feature/ci-github-actions` | Pipeline de Integração Contínua com validação de build e testes .NET e Vitest. | Concluído ✅ |
| **07** | `feature/extras-and-advanced-features` | Worker de expurgo automático de lixeira, painel de auditoria LGPD com exportação JSON e arquivos de amostra. | Concluído ✅ |
| **08** | `feature/auth-jwt-and-theme-fixes` | Autenticação JWT, Refresh Token rotation, RBAC (Admin/Usuario), Auditoria de acessos, APM e refatoração de layout com Sidebar. | Concluído ✅ |
| **09** | `feature/admin-global-contacts-access` | Acesso global e permissão total de exclusão de contatos para administradores com segregação de agenda por usuário. | Concluído ✅ |
| **10** | `docs/update-specs-and-deliverables` | Atualização e sincronização final de todas as especificações e documentação de entregáveis. | Concluído ✅ |
