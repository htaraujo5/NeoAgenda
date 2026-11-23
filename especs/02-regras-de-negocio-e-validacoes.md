# 02 — Regras de Negócio e Validações

## 1. RN01: Unicidade de Telefone Independente de Formatação

### 1.1 Contexto e Desafio
Usuários digitam telefones de diversas formas:
- `(11) 98765-4321`
- `+55 11 98765-4321`
- `5511987654321`
- `11987654321`
- `011 98765 4321`
- `+55 (11) 9.8765-4321`

A regra exige que qualquer uma dessas variações identifique o mesmo número, impedindo duplicidade no sistema.

### 1.2 Algoritmo de Normalização Canônica (Canonical Phone Normalizer)
Para o padrão brasileiro / internacional:
1. **Remoção de caracteres não numéricos:** Remove espaços, parênteses, traços, pontos e barras. Mantém apenas dígitos (`0-9`).
2. **Tratamento do DDI (+55):**
   - Se os dígitos começarem com `55` e tiverem mais de 11 dígitos (ex: 12 ou 13 dígitos como `5511987654321`), remove-se o prefixo `55` para a comparação local padrão, ou armazena-se de forma padronizada internacional (`+5511987654321`).
   - Adotamos o formato **E.164 canônico**: `+55{DDD}{Número}`.
3. **Tratamento do zero de longa distância:**
   - Se o número iniciar com `0` antes do DDD (ex: `011987654321`), o zero inicial é descartado.
4. **Tratamento de 8 vs 9 dígitos em celulares:**
   - Números móveis no Brasil possuem 9 dígitos (`9XXXX-XXXX`) após o DDD. Se informado com 8 dígitos em faixa móvel legada ou se for fixo (8 dígitos, inicial 2-5), a normalização valida a consistência de tamanho (10 ou 11 dígitos com DDD).
5. **Persistência Dupla:**
   - `PhoneOriginal`: String como digitada pelo usuário para preservar a preferência visual se desejado.
   - `PhoneNormalized`: String limpa padronizada no formato canônico (ex: `+5511987654321` ou `11987654321`), indexada com restrição de unicidade.

### 1.3 Validação de Unicidade no Banco
- A verificação ocorre antes do salvamento (`ExistsAsync(normalizedPhone, excludeContactId)`) e é blindada no banco de dados através de um **Unique Index condicional** (excluindo contatos com soft-delete ativo):
  ```sql
  CREATE UNIQUE INDEX IX_Contacts_PhoneNormalized ON Contacts (PhoneNormalized) WHERE IsDeleted = 0;
  ```

---

## 2. RN02: Importação de Contatos via CSV

### 2.1 Especificação do Formato de Arquivo
- **Extensões aceitas:** `.csv`, `.txt`
- **Encodings suportados:** `UTF-8`, `ISO-8859-1` (Latin1)
- **Delimitadores suportados:** Vírgula (`,`) ou Ponto-e-vírgula (`;`) detectados automaticamente.
- **Cabeçalho esperado:** Colunas flexíveis com sinônimos suportados:
  - Nome: `nome`, `name`, `full_name`, `contato`
  - E-mail: `email`, `e-mail`, `correio`
  - Telefone: `telefone`, `phone`, `celular`, `tel`, `whatsapp`

### 2.2 Estratégia de Processamento: Tolerância a Falhas e Relatório Detalhado
A importação **não deve falhar totalmente** se algumas linhas forem inválidas:
- Linhas válidas são importadas e persistidas com sucesso.
- Linhas com inconsistências são catalogadas em um relatório de processamento.
- Casos de invalidação por linha:
  - Nome em branco ou com caracteres inválidos.
  - E-mail com formato inválido (RFC 5322 regex).
  - Telefone vazio, com tamanho insuficiente ou que não pode ser normalizado.
  - Telefone duplicado com contato já existente no banco de dados.
  - Telefone duplicado com outra linha do próprio arquivo CSV em processamento.
- **Retorno da API:**
  ```json
  {
    "totalRows": 100,
    "importedCount": 85,
    "failedCount": 15,
    "errors": [
      { "line": 4, "rawContent": "João;;11999999999", "reason": "E-mail inválido ou vazio" },
      { "line": 12, "rawContent": "Maria;maria@email.com;(11) 98765-4321", "reason": "Telefone já cadastrado no sistema" }
    ]
  }
  ```

---

## 3. RN03: Exclusão com Janela de Restauração de 30 Dias (Soft Delete)

### 3.1 Ciclo de Vida da Exclusão Temporária
1. Quando o usuário clica em "Excluir", o contato não é apagado fisicamente.
2. É aplicado o Soft Delete:
   - `IsDeleted = true`
   - `DeletedAt = DateTime.UtcNow`
   - `RetentionExpiresAt = DeletedAt.AddDays(30)`
3. O contato deixa de aparecer na listagem padrão e vai para a **Lixeira (Recycle Bin)**.
4. **Impacto na Unicidade do Telefone:**
   - *Premissa Adotada:* Enquanto estiver na lixeira, o número do contato excluído **não** deve bloquear um novo cadastro caso um novo titular assuma o número (ou alternativamente, a restauração verifica se o número continua disponível no momento de restaurar). A melhor prática adotada: o índice de unicidade ignora registros onde `IsDeleted = true`. Se outro contato usar aquele telefone enquanto este estiver na lixeira, e o usuário tentar restaurar o contato antigo, o sistema avisa amigavelmente que o telefone já foi associado a outro contato ativo.

### 3.2 Restauração
- A qualquer momento dentro do período de 30 dias (`DateTime.UtcNow <= RetentionExpiresAt`), o usuário pode restaurar o contato.
- Ação de Restauração:
  - Verifica se o telefone normalizado colide com algum contato ativo atual.
  - Se estiver limpo, seta `IsDeleted = false`, `DeletedAt = null`, `RetentionExpiresAt = null`.
- **Expurgo Automático de Vencidos:**
  - Registros com `IsDeleted = true` e `RetentionExpiresAt < DateTime.UtcNow` tornam-se inelegíveis para restauração e são expurgados por rotina de limpeza periódica ou marcados como expirados.

---

## 4. RN04: Exclusão Definitiva por LGPD (Direito ao Esquecimento)

### 4.1 Requisitos Legais da LGPD (Lei 13.709/2018)
O artigo 18, inciso VI da LGPD garante ao titular a eliminação dos dados pessoais tratados com o seu consentimento.

### 4.2 Comportamento da Exclusão LGPD
1. **Irrevogabilidade:** O usuário/administrador solicita a exclusão explícita por LGPD (ação separada da exclusão comum).
2. **Hard Delete dos Dados Pessoais (PII):**
   - Todos os dados cadastrais (Nome, E-mail, Telefone original e normalizado, etc.) são permanentemente excluídos da tabela de contatos.
3. **Auditoria de Conformidade Anonimizada:**
   - Para fins de comprovação regulatória perante a ANPD (Autoridade Nacional de Proteção de Dados), gera-se um registro na tabela `LgpdComplianceAudit`:
     - `RequestId`: UUID único do pedido.
     - `ContactIdOriginal`: Identificador ou hash irreversível (SHA-256) do contato.
     - `PurgedAt`: Data e hora UTC da exclusão.
     - `LegalReason`: "Exercício do direito de exclusão pelo titular - Art. 18, VI LGPD".
     - *Nenhum dado pessoal (nome, email, telefone em texto puro) é mantido na auditoria.*
