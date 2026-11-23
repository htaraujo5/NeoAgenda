# 05 — Especificação de Contratos da API REST

A API segue padrões RESTful, codificação JSON em `UTF-8` e respostas de erro em conformidade com o padrão **RFC 7807 (Problem Details)**.

---

## 1. Sumário de Endpoints

| Método | Rota | Descrição |
| :--- | :--- | :--- |
| `GET` | `/api/contacts` | Lista contatos ativos com paginação, busca e ordenação |
| `GET` | `/api/contacts/{id}` | Obtém detalhes de um contato ativo |
| `POST` | `/api/contacts` | Cria novo contato com validação e unicidade |
| `PUT` | `/api/contacts/{id}` | Atualiza dados de um contato existente |
| `DELETE` | `/api/contacts/{id}` | Exclusão lógica (Soft Delete) com retenção de 30 dias |
| `POST` | `/api/contacts/{id}/restore` | Restaura contato da lixeira para a listagem ativa |
| `DELETE` | `/api/contacts/{id}/lgpd` | Exclusão definitiva irrevogável (LGPD - Direito ao Esquecimento) |
| `GET` | `/api/contacts/recycle-bin` | Lista contatos excluídos temporariamente na lixeira |
| `POST` | `/api/contacts/import-csv` | Importação em lote via arquivo CSV multipart |
| `GET` | `/api/contacts/export-csv` | Exporta contatos ativos em formato CSV |

---

## 2. Detalhamento dos Endpoints

### 2.1 Listagem de Contatos Ativos
`GET /api/contacts?page=1&pageSize=10&search=maria&sortBy=name&sortDirection=asc`

**Resposta de Sucesso (200 OK):**
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Maria Silva",
      "email": "maria@exemplo.com",
      "phoneOriginal": "(11) 98765-4321",
      "phoneNormalized": "+5511987654321",
      "createdAt": "2026-11-20T14:30:00Z",
      "updatedAt": null
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

---

### 2.2 Criação de Contato
`POST /api/contacts`

**Payload de Entrada:**
```json
{
  "name": "Carlos Eduardo",
  "email": "carlos@exemplo.com",
  "phone": "+55 (11) 91234-5678"
}
```

**Respostas Possíveis:**
- `201 Created`: Retorna o contato criado com `Location` header.
- `400 Bad Request`: Erros de validação (RFC 7807).
- `409 Conflict`: Telefone duplicado já cadastrado.

**Exemplo de Resposta 409 Conflict:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Conflito de Unicidade",
  "status": 409,
  "detail": "O telefone informado já está associado a outro contato ativo (Carlos Eduardo)."
}
```

---

### 2.3 Exclusão Temporária (Soft Delete)
`DELETE /api/contacts/{id}`

**Resposta (200 OK):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "Contato movido para a lixeira. Restauração permitida por 30 dias.",
  "deletedAt": "2026-11-23T20:00:00Z",
  "retentionExpiresAt": "2026-12-23T20:00:00Z"
}
```

---

### 2.4 Restauração de Contato
`POST /api/contacts/{id}/restore`

**Respostas Possíveis:**
- `200 OK`: Contato restaurado com sucesso.
- `404 Not Found`: Contato não encontrado ou não está excluído.
- `410 Gone`: Prazo de retenção de 30 dias expirado.
- `409 Conflict`: O telefone do contato a ser restaurado foi assumido por outro contato ativo durante o período na lixeira.

---

### 2.5 Exclusão Definitiva por LGPD
`DELETE /api/contacts/{id}/lgpd`

**Payload de Entrada (Opcional):**
```json
{
  "confirmation": true,
  "legalReason": "Solicitação explícita do titular (Art. 18, VI LGPD)"
}
```

**Resposta (200 OK):**
```json
{
  "protocol": "LGPD-PURGE-20261123-A9B8",
  "message": "Todos os dados pessoais do titular foram permanentemente e irreversivelmente destruídos.",
  "purgedAt": "2026-11-23T20:05:00Z"
}
```

---

### 2.6 Importação em Lote via CSV
`POST /api/contacts/import-csv`
*Content-Type: multipart/form-data*
*Campo: file (arquivo .csv)*

**Resposta de Sucesso (200 OK):**
```json
{
  "totalRows": 25,
  "importedCount": 22,
  "failedCount": 3,
  "errors": [
    {
      "line": 5,
      "raw": "Ana;;(11) 99999-1111",
      "field": "Email",
      "reason": "O e-mail é obrigatório e deve ter formato válido."
    },
    {
      "line": 14,
      "raw": "Pedro;pedro@teste.com;11987654321",
      "field": "Phone",
      "reason": "Telefone duplicado com contato já cadastrado no sistema."
    },
    {
      "line": 18,
      "raw": "Lucas;lucas@teste.com;11987654321",
      "field": "Phone",
      "reason": "Telefone duplicado com a linha 14 do mesmo arquivo."
    }
  ]
}
```
