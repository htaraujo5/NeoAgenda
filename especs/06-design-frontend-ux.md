# 06 — Design Frontend e Experiência do Usuário (PrimeVue)

## 1. Princípios de Interface e Usabilidade
A interface do **NeoAgenda** é projetada para ser minimalista, ágil, acessível e fornecer clareza imediata sobre as operações críticas (principalmente duplicidades, lixeira e LGPD).

### 1.1 Paleta e Identidade Visual
- Utilização do design system do **PrimeVue** (tema Aura ou Lara) com suporte a modo escuro/claro suave.
- Badges semânticas:
  - 🟢 **Ativo**: Tag verde.
  - 🟡 **Na Lixeira (Restaurável)**: Tag amarela com contagem regressiva de dias restantes (ex: `Restam 22 dias`).
  - 🔴 **Ação Crítica LGPD**: Vermelho forte com ícone de escudo/destruição irreversível.

---

## 2. Mapa de Telas e Componentes

### 2.1 Tela Principal: Agenda de Contatos
- **Barra Superior (Toolbar):**
  - Título do sistema com badge de contagem de contatos ativos.
  - Campo de busca instantânea (Search Input com debounce).
  - Botão "Novo Contato" (abre modal de criação).
  - Botão "Importar CSV" (abre drawer/modal de upload com drag & drop).
  - Botão "Lixeira" com badge exibindo quantidade de itens retidos.
  - Botão "Exportar CSV" (download do arquivo).
- **Tabela de Dados (PrimeVue DataTable):**
  - Colunas: Nome, E-mail, Telefone (com máscara de visualização), Data de Cadastro, Ações.
  - Ações por linha: Editar (ícone lápis), Enviar para Lixeira (ícone lixeira amarela), Exclusão LGPD (ícone escudo vermelho).
  - Paginação reativa com seletor de linhas por página (10, 25, 50).

---

### 2.2 Modal de Criação / Edição de Contato
- Campos:
  - Nome Completo (`InputText`) com validação visual.
  - E-mail (`InputText`) com validação de formato.
  - Telefone com máscara brasileira reativa (`(99) 99999-9999` ou internacional).
- Ao digitar o telefone, o frontend executa sanitização em tempo real. Se houver tentativa de salvar número duplicado, exibe erro inline e Toast de aviso.

---

### 2.3 Tela / Tab da Lixeira (Janela de 30 Dias)
- Exibe apenas contatos com `IsDeleted = true`.
- Coluna com **Countdown de Retenção**:
  - Exibe quantos dias e horas restam até a expiração permanente (ex: `Exclui em 18 dias`).
- Ações:
  - **Restaurar Contato (Botão Verde):** Traz o contato de volta para a lista ativa. Se o telefone tiver sido ocupado por outro contato ativo, exibe alerta explicativo.
  - **Exclusão Definitiva por LGPD:** Possibilidade de antecipar a destruição completa imediata.

---

### 2.4 Modal de Confirmação de Exclusão LGPD
- **Importância Crítica:** Como se trata de um expurgo permanente e irrevogável, o modal exige confirmação expressa:
  - Exibe alerta em destaque: *"Atenção: Esta ação é definitiva e decorre do Direito ao Esquecimento previsto na LGPD. O registro será irrevogavelmente eliminado e não poderá ser recuperado nem mesmo pela equipe técnica."*
  - Checkbox de ciência obrigatório: `[ ] Estou ciente de que esta ação é irreversível`.
  - Botão vermelho de confirmação: *"Excluir Definitivamente por LGPD"*.

---

### 2.5 Modal / Drawer de Importação CSV
- Área de Drag & Drop para arquivo `.csv`.
- Mostra pré-visualização das primeiras linhas antes de submeter.
- Barra de progresso durante o envio.
- **Painel de Resultados da Importação:**
  - Card verde: `X contatos importados com sucesso`.
  - Card vermelho/amarelo: `Y linhas com inconsistências ignoradas`.
  - Tabela sanfonada listando o número da linha original, o conteúdo bruto e o motivo exato da rejeição (ex: linha 8 - telefone duplicado).
  - Botão para exportar o relatório de erros em CSV caso o usuário queira corrigir sua planilha original.
