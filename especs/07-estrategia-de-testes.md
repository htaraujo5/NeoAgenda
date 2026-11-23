# 07 — Estratégia de Testes Automatizados

## 1. Pirâmide e Foco de Testes

Seguindo o princípio do desafio prático de priorizar testes no que é **crítico para as regras de negócio**, estruturamos a suíte de testes com foco nos seguintes pilares:

```
          / \
         / E2E \        (Smoke tests de fluxo principal)
        /-------\
       / Integr. \      (API Endpoints com SQLite in-memory / WebApplicationFactory)
      /-----------\
     /  Unitários  \    (Normalizador de telefone, Validador CSV, Regras de Retenção)
    /---------------\
```

---

## 2. Casos de Teste Críticos (Matriz de Cobertura)

### 2.1 Normalizador e Unicidade de Telefone (`PhoneNormalizerTests`)
1. **Casos de Equivalência de Formatação:**
   - Deve reconhecer que `(11) 98765-4321`, `11987654321`, `+55 11 98765-4321`, `5511987654321` e `011 98765 4321` geram exatamente o mesmo `PhoneNormalized` (`+5511987654321`).
2. **Casos com Caracteres Especiais e Espaços:**
   - Deve sanitizar traços, pontos, barras, parênteses e múltiplos espaços (`(11) 9.8765-4321` -> `+5511987654321`).
3. **Casos de Telefones Fixos vs Móveis:**
   - Celular com 9 dígitos (ex: `11987654321` -> 11 dígitos com DDD).
   - Fixo com 8 dígitos (ex: `1133334444` -> 10 dígitos com DDD).
4. **Casos Inválidos:**
   - Números curtos demais (ex: `123456`).
   - Caracteres alfabéticos ou vazios.
   - Devem disparar erro de validação com mensagem clara.

### 2.2 Importação de CSV Resiliente (`CsvContactParserTests`)
1. **Arquivo com Delimitadores Diferentes:**
   - Importação com vírgula (`,`) e ponto-e-vírgula (`;`).
2. **Variações de Cabeçalho:**
   - `nome,email,telefone` vs `name,e-mail,phone` vs `contato,correio,celular`.
3. **Tolerância a Falhas Linha por Linha:**
   - Arquivo com 5 linhas: 3 válidas, 1 com email malformado e 1 com telefone inválido.
   - O resultado deve indicar 3 importadas e relatório de erro detalhando exatamente as linhas 2 e 4.
4. **Duplicidade Intra-arquivo:**
   - Arquivo contendo duas linhas com o mesmo telefone em formatos diferentes: a primeira é aceita e a segunda é rejeitada com aviso de duplicidade no próprio lote.
5. **Duplicidade com Banco Existente:**
   - Arquivo contendo número que já existe ativo no banco de dados deve ser rejeitado individualmente.

### 2.3 Ciclo de Vida: Exclusão com Retenção de 30 Dias (`ContactLifecycleTests`)
1. **Soft Delete:**
   - Ao excluir, `IsDeleted` deve ser `true`, `DeletedAt` preenchido e `RetentionExpiresAt` igual a `DeletedAt + 30 dias`.
   - Contato não deve constar na listagem de ativos.
   - Contato deve constar na listagem da lixeira.
2. **Restauração Válida:**
   - Restaurar contato com menos de 30 dias de exclusão deve reativar o contato e zerar datas de exclusão.
3. **Restauração Bloqueada por Expiração:**
   - Contato com mais de 30 dias (`RetentionExpiresAt < Now`) não deve permitir restauração.
4. **Restauração com Conflito de Telefone:**
   - Se o número foi cadastrado por outro contato enquanto o original estava na lixeira, a restauração deve falhar com conflito 409.

### 2.4 Exclusão Definitiva por LGPD (`LgpdPurgeTests`)
1. **Expurgo Completo dos Dados Pessoais:**
   - Ao executar a exclusão LGPD, o registro deve ser fisicamente removido da tabela `Contacts`.
   - Tentativa de buscar o ID deve retornar `404 Not Found`.
2. **Geração do Log de Auditoria sem PII:**
   - Deve ser inserido registro em `LgpdComplianceAudits` contendo hash irreversível e timestamp, sem expor nome, email ou telefone.

---

## 3. Testes de Integração de API (`ContactApiIntegrationTests`)
- Utiliza `WebApplicationFactory<Program>` e SQLite em memória.
- Testa os fluxos HTTP completos:
  - `POST /api/contacts` seguido de `POST` com mesmo telefone formatado diferente (garantindo retorno 409).
  - `POST /api/contacts/import-csv` com envio multipart real.
  - `DELETE /api/contacts/{id}` seguido de `GET /api/contacts` (garantindo ausência) e `GET /api/contacts/recycle-bin` (garantindo presença).
  - `DELETE /api/contacts/{id}/lgpd` garantindo remoção física total.
