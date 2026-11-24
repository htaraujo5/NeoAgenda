# 06 — Design Frontend e Experiência do Usuário (PrimeVue)

## 1. Princípios de Interface e Usabilidade
A interface do **NeoAgenda** adota uma arquitetura de navegação limpa, segregando as funcionalidades de negócio das ferramentas de governança e telemetria:
- **Menu Lateral Fixo (Sidebar):** Organiza a navegação com destaque para o perfil do usuário logado, status da sessão e alternador de tema.
- **Segregação de Papéis no Menu:** Menus de Gestão de Usuários e Telemetria APM são renderizados dinamicamente apenas para o perfil `Admin`.
- **Formulários Guiados:** Seleção de papéis em dropdown estrito (`Admin` ou `Usuario`), prevenindo digitação livre ou valores inválidos.
- **Visibilidade de Propriedade:** Administradores contam com uma tag indicativa da agenda/proprietário do contato (`@username`), enquanto usuários padrão têm foco na sua própria listagem.

---

## 2. Mapa de Módulos e Componentes

### 2.1 Módulo 1: Agenda & Contatos (`ContactsView.vue`)
- Abas de trabalho:
  - **Contatos Ativos:** Busca em tempo real, ordenação, badges de telefone canônico e botões de ação rápida.
  - **Lixeira (30 Dias):** Contagem regressiva em dias, restauração segura e expurgo direto por LGPD.
  - **Trilha LGPD:** Visualizador de protocolos e hashes anônimos com exportação de comprovantes em JSON.
- Ações no cabeçalho:
  - Criação rápida ("Novo Contato")
  - Importação resiliente de CSV com drag & drop
  - Exportação direta da agenda em CSV

### 2.2 Módulo 2: Meus Dados & Histórico Pessoal (`UserProfileView.vue`)
- Exibe os dados cadastrais do usuário autenticado (Nome, Username e Perfil).
- **Aba "Histórico":** Tabela dedicada às movimentações e logins do próprio usuário autenticado (data/hora, IP de origem, dispositivo e status).
- Limpeza visual: remoção de blocos técnicos desnecessários para a experiência do usuário (tokens brutos e tags redundantes).

### 2.3 Módulo 3: Gestão de Usuários & Acessos (`AccessManagementPanel.vue` - Admin)
- Cadastro de novos operadores no sistema com seleção obrigatória do perfil (`Admin` ou `Usuario`).
- Tabela com lista completa de usuários cadastrados e data de criação.
- Trilha geral de auditoria de acessos (`AccessLogs`), permitindo auditar tentativas de invasão, logins com falha e IPs remotos.

### 2.4 Módulo 4: Acompanhamento de Desempenho APM (`PerformanceMetricsPanel.vue` - Admin)
- Painel desacoplado da visão de contatos, focado em governança técnica.
- Cards de indicadores chave: Total de requisições, Latência média, Percentil p95 e Taxa de erro.
- Tabela com histórico recente de requisições HTTP, rotas acessadas, status code e tempo de processamento em milissegundos.
