# 01 — Visão Geral e Requisitos

## 1. Objetivo do Produto
O **NeoAgenda** é uma solução moderna e resiliente de gerenciamento de contatos corporativos e pessoais, construída com foco em confiabilidade de dados, privacidade (aderência à LGPD), facilidade de importação em lote e experiência de usuário fluida.

---

## 2. Requisitos do Desafio Técnico

Com base no documento de requisitos do projeto (`requisitos.txt`), a aplicação deve satisfazer:

1. **Stack Obrigatória:**
   - Backend: API em **.NET 8+** (C#)
   - Frontend: **Vue.js** (com recomendação do **PrimeVue**)
2. **Entidade Principal:** Contato com atributos mínimos:
   - Nome (`Name`)
   - E-mail (`Email`)
   - Telefone (`Phone`)
3. **Regras Mandatórias de Negócio:**
   - **R1 - Telefone Único:** Não permitir telefone duplicado, independentemente de máscara, pontuação ou formatação.
   - **R2 - Importação CSV Resiliente:** Importação em lote via arquivo CSV, com tratamento e relatório de linhas inválidas sem abortar as linhas válidas.
   - **R3 - Exclusão com Restauração (Soft Delete):** Exclusão temporária com janela de restauração de até 30 dias.
   - **R4 - Exclusão Definitiva por LGPD:** Atendimento explícito ao direito ao esquecimento e eliminação irrevogável de dados pessoais solicitados pelo titular.
4. **Entregáveis de Engenharia:**
   - Repositório Git com histórico atômico e semântico de commits.
   - Testes automatizados focados no que for considerado crítico (normalização de telefone, regras de duplicidade, importador CSV, ciclo de vida e retenção).
   - Documentação README abrangente contemplando:
     - Como rodar com Docker em um único comando;
     - Decisões de design e trade-offs assumidos;
     - Escopo descartado ou postergado e suas justificativas;
     - Registro transparente do uso de IA (o que foi aceito, corrigido ou rejeitado);
     - Registro de tempo investido.

---

## 3. Matriz de Requisitos Funcionais (RF)

| ID | Nome do Requisito | Descrição | Criticidade |
| :--- | :--- | :--- | :--- |
| **RF01** | Cadastro de Contato | Criação de novo contato com validação de formato e unicidade de telefone. | Alta |
| **RF02** | Edição de Contato | Atualização de dados cadastrais, garantindo que alteração de telefone mantenha unicidade. | Alta |
| **RF03** | Listagem Paginada | Listagem de contatos ativos com suporte a paginação, ordenação e busca textual (filtro rápido por nome, e-mail ou telefone). | Média |
| **RF04** | Exclusão com Janela de Restauração | Envio do contato para a Lixeira (Soft Delete) com cálculo automático da data limite de retenção (30 dias). | Alta |
| **RF05** | Restauração de Contato | Restauração de contato dentro do prazo de 30 dias, reativando-o na listagem principal (caso o telefone ainda continue elegível/único). | Alta |
| **RF06** | Exclusão Definitiva LGPD | Expurgar definitivamente todos os dados do titular sem possibilidade de recuperação, registrando evento de conformidade anônimo. | Crítica |
| **RF07** | Importação em Lote via CSV | Upload de arquivo `.csv`, validação linha a linha, inserção das válidas e geração de sumário de erros para as inválidas. | Alta |
| **RF08** | Visualização da Lixeira | Painel dedicado para listar itens excluídos temporariamente com badge de dias restantes para expiração. | Média |
| **RF09** | Exportação de Contatos | Exportação dos contatos ativos no formato CSV padronizado (recurso de conveniência). | Baixa |

---

## 4. Requisitos Não Funcionais (RNF)

| ID | Tipo | Especificação |
| :--- | :--- | :--- |
| **RNF01** | **Confiabilidade de Dados** | A unicidade do telefone deve ser assegurada na camada de domínio e garantida no banco de dados através de índice parcial exclusivo. |
| **RNF02** | **Performance** | Importação de arquivos CSV com centenas ou milhares de linhas deve ocorrer em processamento otimizado (em stream ou lote em batch), sem travar a memória. |
| **RNF03** | **Portabilidade** | Aplicação 100% conteinerizada via Docker e Docker Compose, possibilitando execução em qualquer ambiente Linux/macOS/Windows com um único comando. |
| **RNF04** | **Usabilidade** | Interface reativa e intuitiva utilizando componentes PrimeVue com feedback visual rico (toasts, loading spinners, mensagens contextuais de validação). |
| **RNF05** | **Segurança e LGPD** | Exclusão definitiva não deixa rastros recuperáveis dos dados pessoais (PII) nos registros operacionais. |
| **RNF06** | **Tratamento de Erros** | API segue a RFC 7807 (Problem Details) para respostas de erro padronizadas e descritivas. |
