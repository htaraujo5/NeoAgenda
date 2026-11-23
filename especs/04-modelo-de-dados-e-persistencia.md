# 04 — Modelo de Dados e Persistência

## 1. Esquema Relacional de Entidades

A base de dados contempla duas tabelas principais: `Contacts` (dados operacionais) e `LgpdComplianceAudits` (registro de conformidade com privacidade).

### 1.1 Tabela `Contacts`
Armazena os contatos cadastrados, seu histórico de datas e controle de soft delete.

| Coluna | Tipo | Nulo? | Descrição |
| :--- | :--- | :--- | :--- |
| `Id` | `UUID` (Guid) | NÃO | Chave primária única do contato. |
| `Name` | `VARCHAR(150)` | NÃO | Nome completo do contato. |
| `Email` | `VARCHAR(254)` | NÃO | Endereço de e-mail válido. |
| `PhoneOriginal` | `VARCHAR(30)` | NÃO | Telefone no formato digitado ou recebido no CSV. |
| `PhoneNormalized`| `VARCHAR(20)` | NÃO | Telefone em formato canônico (ex: `+5511987654321`), sem máscaras. |
| `CreatedAt` | `TIMESTAMPTZ` | NÃO | Data/hora UTC de criação do registro. |
| `UpdatedAt` | `TIMESTAMPTZ` | SIM | Data/hora UTC da última edição dos dados. |
| `IsDeleted` | `BOOLEAN` | NÃO | Indicador de exclusão lógica (padrão: `FALSE`). |
| `DeletedAt` | `TIMESTAMPTZ` | SIM | Data/hora UTC da exclusão temporária (soft delete). |
| `RetentionExpiresAt` | `TIMESTAMPTZ` | SIM | Data limite para restauração (`DeletedAt + 30 dias`). |

### 1.2 Tabela `LgpdComplianceAudits`
Armazena a trilha de auditoria para fins de compliance perante a LGPD.
*Importante:* Não contém nenhum dado pessoal (PII) do titular.

| Coluna | Tipo | Nulo? | Descrição |
| :--- | :--- | :--- | :--- |
| `Id` | `UUID` (Guid) | NÃO | Identificador do evento de auditoria. |
| `ContactHash` | `VARCHAR(64)` | NÃO | Hash SHA-256 (do PhoneNormalized + Salt) para comprovação unívoca de eliminação. |
| `ExecutedAt` | `TIMESTAMPTZ` | NÃO | Data/hora UTC da eliminação definitiva. |
| `LegalBasis` | `VARCHAR(100)` | NÃO | Ex: "Artigo 18, VI da Lei 13.709/2018 (LGPD)". |
| `RequestedBy` | `VARCHAR(100)` | NÃO | Canal ou identificação da requisição (ex: "portal_usuario_lgpd"). |

---

## 2. Índices e Restrições de Unicidade

### 2.1 Índice Parcial Exclusivo (Filtered Unique Index)
Para garantir que telefones nunca se repitam entre contatos ativos:

```sql
-- PostgreSQL / SQLite Filtered Unique Index
CREATE UNIQUE INDEX "IX_Contacts_PhoneNormalized_Active"
ON "Contacts" ("PhoneNormalized")
WHERE "IsDeleted" = FALSE;
```

### 2.2 Índices de Consulta e Performance
- `CREATE INDEX "IX_Contacts_Name" ON "Contacts" ("Name");` (Otimização para busca textual por prefixo)
- `CREATE INDEX "IX_Contacts_Email" ON "Contacts" ("Email");`
- `CREATE INDEX "IX_Contacts_RetentionExpiresAt" ON "Contacts" ("RetentionExpiresAt") WHERE "IsDeleted" = TRUE;` (Otimização para queries de expurgo de lixeira)

---

## 3. Mapeamento com Entity Framework Core

```csharp
public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(254).IsRequired();
        builder.Property(c => c.PhoneOriginal).HasMaxLength(30).IsRequired();
        builder.Property(c => c.PhoneNormalized).HasMaxLength(20).IsRequired();

        builder.HasIndex(c => c.PhoneNormalized)
               .IsUnique()
               .HasFilter("\"IsDeleted\" = FALSE"); // no SQLite: HasFilter("IsDeleted = 0")

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
```
