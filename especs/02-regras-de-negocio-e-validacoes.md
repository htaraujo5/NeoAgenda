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

A regra exige que qualquer uma dessas variações identifique o mesmo número, impedindo duplicidade no sistema entre contatos ativos.

### 1.2 Algoritmo de Normalização Canônica (PhoneNormalizer)
1. **Remoção de caracteres não numéricos:** Remove pontuações, espaços e caracteres especiais, retendo apenas dígitos (`0-9`).
2. **Tratamento do DDI (+55):**
   - Reconhece o código internacional do Brasil (`55`). Converte para o padrão internacional **E.164 canônico**: `+55{DDD}{Número}`.
3. **Tratamento do zero de longa distância:**
   - Se o número iniciar com `0` antes de um DDD válido (ex: `011987654321`), o zero inicial é descartado de forma controlada.
4. **Validação Estrita de DDDs e Telefonia Brasileira:**
   - Validação da tabela oficial de códigos de área (DDDs) da Anatel.
   - Celulares: 11 dígitos no total (com DDD), exigindo o nono dígito `9`.
   - Fixo: 10 dígitos no total (com DDD), aceitando dígitos iniciais de 2 a 5.
5. **Persistência Dupla:**
   - `PhoneOriginal`: Mantém a string original digitada para conveniência visual.
   - `PhoneNormalized`: String limpa padronizada no formato canônico E.164, indexada com restrição de unicidade.

### 1.3 Validação de Unicidade no Banco
- A verificação ocorre antes do salvamento (`ExistsByNormalizedPhoneAsync`) e é blindada no banco de dados através de um **Unique Index condicional** (excluindo contatos com soft-delete ativo):
  ```sql
  CREATE UNIQUE INDEX IX_Contacts_PhoneNormalized ON Contacts (PhoneNormalized) WHERE IsDeleted = 0;
  ```

---

## 2. RN02: Importação Resiliente de Contatos via CSV

### 2.1 Especificação e Formato
- **Extensões aceitas:** `.csv`, `.txt`
- **Encodings suportados:** `UTF-8` (com ou sem BOM), `ISO-8859-1` (Latin1).
- **Detecção automática de delimitadores:** Suporta vírgula (`,`), ponto-e-vírgula (`;`) e tabulação (`\t`).
- **Mapeamento flexível de cabeçalhos:** Reconhece variações comuns:
  - Nome: `nome`, `name`, `full_name`, `contato`
  - E-mail: `email`, `e-mail`, `correio`, `mail`
  - Telefone: `telefone`, `phone`, `celular`, `tel`, `whatsapp`

### 2.2 Tolerância a Falhas e Diagnóstico Detalhado
A importação **não é abortada** por causa de linhas incorretas:
- Linhas válidas são importadas e persistidas com sucesso.
- Linhas com inconsistências são catalogadas em um relatório detalhado contendo:
  - Número da linha (`LineNumber`)
  - Conteúdo original (`RawLine`)
  - Campo com falha (`Field`)
  - Motivo claro da rejeição (`Reason`)
- **Prevenção de Duplicidades Intra-arquivo:** Se o mesmo lote CSV contiver duas linhas com o mesmo telefone normalizado, a primeira é aceita e as subsequentes são rejeitadas com erro explicativo indicando a colisão intra-lote.
- **Vínculo de Propriedade:** Contatos importados são automaticamente vinculados ao usuário logado executor da importação.

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
   - O índice de unicidade ignora registros onde `IsDeleted = true`. Se outro contato ativo for cadastrado com o mesmo número enquanto o original estiver na lixeira, e houver tentativa de restaurar o original, o sistema acusa conflito amigável (HTTP 409) informando que o número foi ocupado.

### 3.2 Restauração e Worker Automático de Purga
- Dentro dos 30 dias (`DateTime.UtcNow <= RetentionExpiresAt`), o contato pode ser restaurado para a lista ativa.
- **Expurgo em Segundo Plano (`RecycleBinPurgeBackgroundService`):**
  - Serviço em background do .NET que executa periodicamente para remover fisicamente do banco de dados registros cuja retenção tenha expirado (`RetentionExpiresAt <= UtcNow`).

---

## 4. RN04: Exclusão Definitiva por LGPD (Direito ao Esquecimento)

### 4.1 Requisitos Legais da LGPD (Art. 18, VI da Lei 13.709/2018)
Garante ao titular a eliminação irreversível de dados tratados com seu consentimento.

### 4.2 Comportamento da Exclusão LGPD
1. **Irrevogabilidade:** Ação explícita de destruição com aviso claro e confirmação obrigatória de ciência.
2. **Hard Delete dos Dados Pessoais (PII):**
   - Todos os dados cadastrais (Nome, E-mail, Telefone original e normalizado) são permanentemente destruídos e removidos do banco.
3. **Auditoria Criptográfica Anonimizada:**
   - Inserção na tabela `LgpdComplianceAudits`:
     - `ContactHash`: Hash criptográfico SHA-256 (`PhoneNormalized + ContactId`) para comprovação legal perante a ANPD.
     - `ExecutedAt`: Timestamp UTC.
     - `LegalBasis`: Artigo 18, VI da Lei 13.709/2018.
     - `RequestedBy`: Identificador do solicitante.
     - *Nenhum dado pessoal legível (PII) é mantido.*

---

## 5. RN05: Autenticação Segura & RBAC (Role-Based Access Control)

### 5.1 Mecanismo de Autenticação
- Emissão de **Tokens JWT** assinados com chave simétrica HMAC-SHA256, contendo `NameIdentifier`, `Name`, `FullName` e `Role`.
- **Validade do Access Token:** 60 minutos.
- **Refresh Token Rotation:** Chave aleatória de 64 bytes com validade de 7 dias, armazenada de forma segura na entidade `User` com revogação e rotação automática a cada renovação.

### 5.2 Papéis e Matriz de Permissões
1. **Administrador (`Admin`):**
   - Acesso global à aplicação.
   - Gestão de usuários (criação, consulta, revogação de acessos).
   - Visualização e exclusão (soft delete e expurgo LGPD) de contatos de **todas as agendas**.
   - Acesso à auditoria completa de segurança e acessos (`/api/access/logs`).
   - Monitoramento das métricas de desempenho APM (`/api/metrics/summary`).
2. **Usuário Padrão (`Usuario`):**
   - Gerenciamento restrito à sua própria agenda (criação, edição, exclusão e importação CSV de contatos próprios).
   - Acesso ao seu perfil com histórico de suas próprias movimentações e logins.
   - Não possui visibilidade de agendas alheias, gestão de usuários ou métricas de desempenho APM.

---

## 6. RN06: Auditoria de Segurança & Telemetria APM

### 6.1 Logs de Auditoria de Acesso (`AccessLogs`)
- Cada tentativa de autenticação gera um evento imutável com status de sucesso (`LOGIN_SUCCESS`) ou falha (`LOGIN_FAILED`), endereço IP de origem, User-Agent do navegador, timestamp e justificativa em caso de erro.
- Administradores consultam a trilha completa de auditoria; usuários visualizam apenas o seu próprio histórico.

### 6.2 Telemetria de Desempenho (`PerformanceTelemetryMiddleware`)
- Intercepta requisições HTTP na API, calculando o tempo exato de execução em milissegundos.
- Registra rota, método HTTP, status code e duração.
- O endpoint `/api/metrics/summary` consolida métricas de APM: total de requisições, latência média, p50, p95, p99 e taxa de erro.
