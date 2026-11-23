# 08 — Infraestrutura, Docker e Execução

## 1. Princípios de Execução Descomplicada

A aplicação foi projetada para ser executada em qualquer máquina com **Docker** e **Docker Compose** instalados, sem necessidade de instalar previamente os SDKs de .NET ou Node.js na máquina hospedeira.

---

## 2. Topologia dos Containers (Docker Compose)

```
+-------------------------------------------------------+
|                   Docker Compose                      |
|                                                       |
|   +--------------------+     +--------------------+   |
|   |  neoagenda-front   |     |   neoagenda-api    |   |
|   |  (Vue + Nginx)     |     |   (.NET 8/9 WebAPI)|   |
|   |  Porta: 5173 / 80  |---->|   Porta: 5000      |   |
|   +--------------------+     +--------------------+   |
|                                         |             |
|                                         v             |
|                              +--------------------+   |
|                              |   neoagenda-db     |   |
|                              |   (PostgreSQL 16)  |   |
|                              |   Porta: 5432      |   |
|                              +--------------------+   |
+-------------------------------------------------------+
```

*Nota de Flexibilidade:* Para desenvolvimento local ultra-rápido ou testes automatizados, o backend também suporta SQLite integrado, enquanto o ambiente Docker padrão sobe o PostgreSQL completo ou SQLite conteinerizado de forma transparente.

---

## 3. Arquitetura Multi-Stage dos Dockerfiles

### 3.1 Dockerfile Backend (`backend/Dockerfile`)
- **Stage 1 (Build & Test):** Utiliza imagem SDK `mcr.microsoft.com/dotnet/sdk:9.0` (ou 8.0) para restaurar pacotes, compilar e rodar a suíte de testes unitários.
- **Stage 2 (Publish):** Publica os binários otimizados em modo `Release`.
- **Stage 3 (Runtime):** Utiliza imagem mínima `mcr.microsoft.com/dotnet/aspnet:9.0` (chiseled/alpine), garantindo tamanho reduzido, alta performance e baixa superfície de ataque.

### 3.2 Dockerfile Frontend (`frontend/Dockerfile`)
- **Stage 1 (Build):** Imagem `node:20-alpine`, instala dependências via `npm ci` e executa `npm run build`.
- **Stage 2 (Runtime):** Imagem leve `nginx:alpine`, servindo os arquivos estáticos e atuando como reverse proxy opcional para as requisições da API.

---

## 4. Instruções de Execução em 1 Comando

Para subir todo o ambiente:
```bash
docker compose up --build -d
```

URLs de Acesso:
- **Frontend (Interface do Usuário):** `http://localhost:5173` ou `http://localhost:8080`
- **Backend API & Swagger:** `http://localhost:5000/swagger` ou `http://localhost:5000/api-docs`
- **Healthcheck:** `http://localhost:5000/health`
