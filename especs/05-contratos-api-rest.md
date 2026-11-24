# 05 — Especificação de Contratos da API REST

A API segue padrões RESTful, codificação JSON em `UTF-8` e respostas de erro em conformidade com o padrão **RFC 7807 (Problem Details)**.
Todas as rotas da API (exceto `/api/auth/login` e `/api/auth/refresh-token`) exigem o cabeçalho HTTP:
```http
Authorization: Bearer <seu_token_jwt>
```

---

## 1. Sumário de Endpoints

### 1.1 Autenticação (`/api/auth`)
| Método | Rota | Papel Mínimo | Descrição |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/login` | Público | Autentica usuário com username e senha, retornando JWT e Refresh Token |
| `POST` | `/api/auth/refresh-token` | Público | Renova a sessão e emite um novo token JWT |
| `POST` | `/api/auth/revoke` | `Usuario` | Revoga o Refresh Token ativo do usuário |

### 1.2 Gestão de Contatos (`/api/contacts`)
| Método | Rota | Papel Mínimo | Descrição |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/contacts` | `Usuario` | Lista contatos ativos (Usuário vê seus contatos; Admin vê todas as agendas) |
| `GET` | `/api/contacts/{id}` | `Usuario` | Obtém detalhes de um contato ativo |
| `POST` | `/api/contacts` | `Usuario` | Cadastra novo contato vinculando ao usuário logado |
| `PUT` | `/api/contacts/{id}` | `Usuario` | Atualiza contato existente (usuário atualiza próprio; Admin atualiza qualquer) |
| `DELETE` | `/api/contacts/{id}` | `Usuario` | Soft Delete (usuário exclui próprio; Admin exclui qualquer) |
| `POST` | `/api/contacts/{id}/restore` | `Usuario` | Restaura contato da lixeira (usuário restaura próprio; Admin restaura qualquer) |
| `DELETE` | `/api/contacts/{id}/lgpd` | `Usuario` | Exclusão definitiva LGPD (usuário expurga próprio; Admin expurga qualquer) |
| `GET` | `/api/contacts/recycle-bin` | `Usuario` | Lista lixeira (Usuário vê seus excluídos; Admin vê lixeira geral) |
| `GET` | `/api/contacts/lgpd-audits` | `Usuario` | Lista histórico anônimo de protocolos de exclusão LGPD |
| `POST` | `/api/contacts/import-csv` | `Usuario` | Importação tolerante via CSV (vincula contatos criados ao usuário) |
| `GET` | `/api/contacts/export-csv` | `Usuario` | Exporta contatos da agenda em CSV UTF-8 |

### 1.3 Gestão de Acessos & Auditoria (`/api/access`)
| Método | Rota | Papel Mínimo | Descrição |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/access/users` | `Admin` | Lista todos os usuários cadastrados e seus perfis de acesso |
| `POST` | `/api/access/users` | `Admin` | Cadastra um novo usuário no sistema |
| `GET` | `/api/access/logs` | `Admin` | Consulta o histórico geral de auditoria e tentativas de login |
| `GET` | `/api/access/my-logs` | `Usuario` | Consulta o histórico de atividades e logins do próprio usuário logado |

### 1.4 Telemetria de Desempenho APM (`/api/metrics`)
| Método | Rota | Papel Mínimo | Descrição |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/metrics/summary` | `Admin` | Retorna métricas consolidadas de desempenho (p50, p95, p99, taxa de erro) |
| `GET` | `/api/metrics/recent` | `Admin` | Lista as requisições mais recentes com latência e status HTTP |

---

## 2. Exemplos de Payloads

### 2.1 Autenticação (`POST /api/auth/login`)
```json
// Requisição
{
  "username": "admin",
  "password": "Admin@123"
}

// Resposta 200 OK
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "7qL8w9...",
  "expiresAt": "2026-11-24T18:00:00Z",
  "username": "admin",
  "name": "Administrador do Sistema",
  "role": "Admin"
}
```

### 2.2 Listagem de Contatos Ativos (`GET /api/contacts`)
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Maria Silva",
      "email": "maria@exemplo.com",
      "phoneOriginal": "(11) 98765-4321",
      "phoneNormalized": "+5511987654321",
      "createdBy": "helisson.araujo",
      "createdAt": "2026-11-24T12:00:00Z",
      "updatedAt": null
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

### 2.3 Resposta de Métricas APM (`GET /api/metrics/summary`)
```json
{
  "totalRequests": 154,
  "averageElapsedMs": 14.2,
  "p50ElapsedMs": 8.5,
  "p95ElapsedMs": 42.1,
  "p99ElapsedMs": 88.0,
  "errorRatePercent": 0.65
}
```
