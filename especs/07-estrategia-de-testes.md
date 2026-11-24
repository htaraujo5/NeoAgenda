# 07 — Estratégia de Testes Automatizados

## 1. Pirâmide e Foco de Testes

A suíte de testes automatizados do NeoAgenda cobre todos os componentes críticos da aplicação:

```
          / \
         / E2E \        (Verificação em container via Docker)
        /-------\
       / Integr. \      (API Controllers + Auth + SQLite In-Memory)
      /-----------\
     /  Unitários  \    (PhoneNormalizer, CSV, Entidades, Serviços, Frontend)
    /---------------\
```

---

## 2. Cobertura de Testes Automatizados

### 2.1 Backend Unit Tests (55 testes em `NeoAgenda.UnitTests`)
1. **`PhoneNormalizerTests`:**
   - Validação de DDDs válidos e inválidos (rejeição de DDD 00, 01, etc.).
   - Normalização para padrão E.164 (`+5511987654321`).
   - Telefones fixos (8 dígitos após DDD) vs celulares (9 dígitos com nono dígito obrigatório).
   - Sanitização de espaços, parênteses, pontos e traços.
2. **`ContactEntityTests`:**
   - Criação com estado ativo padrão e autor (`CreatedBy`).
   - Cálculo automático da janela de retenção de 30 dias no soft delete.
   - Restauração permitida dentro dos 30 dias e bloqueio de restauração após expiração.
   - Cálculo de dias restantes na lixeira (`GetDaysRemainingInRecycleBin`).
3. **`ContactServiceTests`:**
   - Persistência e verificação de duplicidade de telefone.
   - Permissão de Administrador para exclusão e expurgo de qualquer agenda.
   - Bloqueio de usuário comum tentando alterar ou excluir contatos de outra agenda.
4. **`ContactServiceCsvTests`:**
   - Importação de contatos com validação tolerante a falhas.
   - Exportação em formato CSV UTF-8.
5. **`CsvContactParserTests`:**
   - Suporte a múltiplos delimitadores (`,`, `;`).
   - Detecção de duplicidades intra-arquivo.

### 2.2 Backend Integration Tests (8 testes em `NeoAgenda.IntegrationTests`)
- Executados contra instância in-memory do SQLite e `CustomWebApplicationFactory`:
  - `POST /api/auth/login`: Autenticação e geração de Bearer Token.
  - Ciclo de vida completo do contato via HTTP: criação -> conflito 409 -> soft delete -> lixeira -> restauração -> expurgo LGPD.
  - Teste de importação de arquivo CSV real via multipart/form-data.
  - Exportação de CSV via endpoint HTTP.
  - Verificação do endpoint de healthcheck `/health`.

### 2.3 Frontend Tests (8 testes em Vitest)
- Testes unitários de cliente HTTP, Pinia stores (`authStore`, `contactStore`) e componentes (`LgpdConfirmDialog.spec.ts`).
