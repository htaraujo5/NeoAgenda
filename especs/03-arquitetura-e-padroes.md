# 03 — Arquitetura de Software e Padrões Técnicos

## 1. Visão Arquitetural

O **NeoAgenda** adota os princípios da **Clean Architecture**, com separação clara de responsabilidades, independência de frameworks no domínio e desacoplamento via injeção de dependências.

```
NeoAgenda/
├── backend/
│   ├── src/
│   │   ├── NeoAgenda.Domain/          # Entidades (Contact, User, AccessLog, RequestMetric), Regras Puras, Interfaces
│   │   ├── NeoAgenda.Application/     # Serviços de Aplicação (ContactService, AccessService, MetricsService), DTOs
│   │   ├── NeoAgenda.Infrastructure/  # EF Core, AppDbContext, Repositórios, AuthService, Middlewares, BackgroundServices
│   │   └── NeoAgenda.Api/             # Controllers RESTful, DI, Swagger OpenAPI, Configurações
│   └── tests/
│       ├── NeoAgenda.UnitTests/       # Suíte xUnit para regras de domínio, normalizador, CSV e segurança
│       └── NeoAgenda.IntegrationTests/# Testes de ponta a ponta com WebApplicationFactory e SQLite in-memory
├── frontend/
│   ├── src/
│   │   ├── api/                       # Clientes HTTP (Axios) tipados e interceptores de autenticação JWT
│   │   ├── components/                # Componentes modulares (ContactsView, UserProfileView, AccessManagementPanel, etc.)
│   │   ├── stores/                    # Gestão reativa de estado Pinia (authStore, contactStore)
│   │   ├── types/                     # Interfaces TypeScript (contact, auth, telemetry)
│   │   └── style.css                  # Variáveis semânticas e suporte a tema claro e escuro
├── especs/                            # Especificações funcionais e arquiteturais
├── docker-compose.yml                 # Orquestração do ecossistema completo
└── README.md                          # Guia executivo, decisões e manual de uso
```

---

## 2. Padrões do Backend (.NET 8 C#)

### 2.1 Pipeline de Middlewares HTTP
A ordem dos middlewares na API segue o fluxo recomendado de segurança e observabilidade:
1. `PerformanceTelemetryMiddleware`: Mede a latência de ponta a ponta e registra métricas de requisição (`RequestMetrics`).
2. `GlobalExceptionHandler`: Captura exceções não tratadas e formata respostas segundo o padrão RFC 7807 (`ProblemDetails`).
3. `AuthenticationMiddleware`: Valida e decodifica tokens JWT Bearer nos headers de autorização.
4. `AuthorizationMiddleware`: Aplica políticas de acesso baseadas em papéis (`[Authorize(Roles = "Admin")]`).

### 2.2 Camada de Domínio (`NeoAgenda.Domain`)
- **Entidades Ricas:**
  - `Contact`: métodos de encapsulamento como `Create()`, `Update()`, `MarkAsDeleted()`, `Restore()`, `CanBeRestored()` e `GetDaysRemainingInRecycleBin()`.
  - `User`: métodos para manipulação de credenciais (`SetPasswordHash()`, `SetRefreshToken()`, `RevokeRefreshToken()`).
  - `AccessLog`: entidade imutável para auditoria de autenticação e tentativas de acesso.
  - `RequestMetric`: registro temporal para acompanhamento de desempenho (APM).
  - `LgpdComplianceAudit`: log anônimo de atendimento ao Art. 18 da LGPD.

### 2.3 Camada de Infraestrutura (`NeoAgenda.Infrastructure`)
- **Persistência com EF Core:**
  - `AppDbContext`: mapeamento das 5 tabelas com convenções e fluent API.
  - `ContactConfiguration`: índice único filtrado para `PhoneNormalized` onde `IsDeleted = false`.
- **Background Worker:**
  - `RecycleBinPurgeBackgroundService`: serviço periódico que executa a cada 1 hora para purgar contatos cuja data de retenção de 30 dias tenha expirado.
- **Segurança Criptográfica:**
  - `PasswordHasher`: geração de hash SHA-256 e comparação segura contra ataques de temporização (`CryptographicOperations.FixedTimeEquals`).

---

## 3. Padrões do Frontend (Vue 3 + PrimeVue)

### 3.1 Arquitetura de Componentes
- **Layout Modular com Navegação Lateral (Sidebar):**
  - Agenda de Contatos (`ContactsView.vue`)
  - Meus Dados & Histórico Pessoal (`UserProfileView.vue`)
  - Gestão de Usuários & Acessos (`AccessManagementPanel.vue` - exclusivo Admin)
  - Desempenho & Métricas APM (`PerformanceMetricsPanel.vue` - exclusivo Admin)
- **Modais e Diálogos Especializados:**
  - `ContactFormDialog.vue`: Formulário de criação/edição com dropdown estrito de roles.
  - `LgpdConfirmDialog.vue`: Diálogo de conformidade com confirmação obrigatória de ciência.
  - `CsvImportDialog.vue`: Painel de diagnóstico de importação com exibição tabular de inconsistências e download de relatório.
- **Gestão de Estado (Pinia):**
  - `authStore`: Gerencia token JWT, refresh token, credenciais salvas em storage e decodificação do perfil de acesso.
  - `contactStore`: Gerencia contatos ativos, lixeira, auditorias LGPD, paginação e filtros.
- **Design Tokens e Temas:**
  - Suporte suave e dinâmico a Modo Claro / Modo Escuro via variáveis CSS (`--bg-app`, `--card-bg`, `--text-main`, `--border-color`).
