# 03 — Arquitetura de Software e Padrões Técnicos

## 1. Visão Arquitetural

O **NeoAgenda** adota uma arquitetura limpa, desacoplada e baseada em responsabilidades bem delimitadas, otimizada para legibilidade, testabilidade e manutenibilidade.

```
NeoAgenda/
├── backend/
│   ├── src/
│   │   ├── NeoAgenda.Domain/          # Entidades, Value Objects, Interfaces, Regras Puras
│   │   ├── NeoAgenda.Application/     # Casos de Uso, DTOs, Serviços, Validadores FluentValidation
│   │   ├── NeoAgenda.Infrastructure/  # EF Core, Repositórios, Normalizadores, CsvHelper, Migrations
│   │   └── NeoAgenda.Api/             # Controllers/Endpoints, Middlewares, DI, OpenAPI/Swagger
│   └── tests/
│       ├── NeoAgenda.UnitTests/       # Testes unitários de regras, normalização e validações
│       └── NeoAgenda.IntegrationTests/# Testes de integração de API com WebApplicationFactory
├── frontend/
│   ├── src/
│   │   ├── api/                       # Clientes HTTP (Axios) tipados
│   │   ├── components/                # Componentes reutilizáveis PrimeVue
│   │   ├── views/                     # Páginas (Contatos, Lixeira, Importação)
│   │   ├── stores/                    # Gerenciamento de estado (Pinia)
│   │   ├── types/                     # Interfaces TypeScript
│   │   └── utils/                     # Formatadores e máscaras de telefone
├── especs/                            # Especificações e Contexto de IA
├── docker/                            # Arquivos de orquestração Docker
├── docker-compose.yml                 # Orquestração do ecossistema
└── README.md                          # Guia de execução e decisões
```

---

## 2. Padrões do Backend (.NET 8/9 C#)

### 2.1 Camadas
- **Domain:**
  - `Contact`: Entidade raiz com métodos ricos (`MarkAsDeleted()`, `Restore()`, `Update()`).
  - `ValueObjects`: `PhoneNumber` com encapsulamento de normalização e validação intrínseca.
  - `Specifications / Interfaces`: `IContactRepository`, `IPhoneNormalizer`, `ICsvContactParser`.
- **Application:**
  - `ContactService`: Orquestra casos de uso com validação prévia.
  - `FluentValidation`: `CreateContactDtoValidator`, `UpdateContactDtoValidator`, `CsvRowValidator`.
  - `DTOs`: `ContactResponseDto`, `CreateContactDto`, `CsvImportResultDto`.
- **Infrastructure:**
  - `AppDbContext`: Mapeamento EF Core com filtros globais de consulta (Global Query Filters) configuráveis para Soft Delete.
  - `PhoneNormalizer`: Implementação com suporte a regras de DDDs brasileiros e formato E.164.
  - `CsvContactParser`: Leitura com `CsvHelper` tolerante a variações de cabeçalhos e codificações.
- **API:**
  - `GlobalExceptionHandler`: Middleware para converter exceções em respostas RFC 7807 (`ProblemDetails`).
  - `Controllers`: Controllers RESTful organizados e documentados via Swagger/OpenAPI.

---

## 3. Padrões do Frontend (Vue 3 + PrimeVue)

### 3.1 Tecnologias e Bibliotecas
- **Vue 3:** Single File Components com `<script setup lang="ts">`.
- **PrimeVue 4:** Componentes de alta fidelidade visual (DataTable, Dialog, Toast, ConfirmDialog, Tag, Button, InputText, FileUpload, ProgressBar).
- **Pinia:** Armazenamento reativo do estado global de contatos, notificações e filtros de busca.
- **Axios:** Cliente HTTP com interceptors para tratamento de mensagens de erro amigáveis vindas da API.

---

## 4. Decisões Arquiteturais e Trade-offs

1. **Uso de Clean Architecture enxuta:**
   - *Decisão:* Criamos projetos separados por responsabilidade (Domain, Application, Infrastructure, Api) para garantir testes unitários sem dependências externas.
   - *Trade-off:* Um pouco mais de arquivos no início, porém isolamento total das regras de negócio críticas (normalização de telefone e CSV).
2. **Normalização de Telefone no Backend:**
   - *Decisão:* Embora o frontend forneça máscara visual, a normalização e validação definitiva ocorrem no backend, garantindo que integrações via API ou CSV não contornem a regra.
3. **Soft Delete com Filtro Global do EF Core:**
   - *Decisão:* Consultas padrão utilizam `.Where(c => !c.IsDeleted)` automaticamente via `HasQueryFilter`. Para a tela de Lixeira, utiliza-se `.IgnoreQueryFilters()`.
