# 08 — Infraestrutura, Docker e Execução

## 1. Topologia dos Containers (Docker Compose)

O ambiente é 100% conteinerizado e provisionado via `docker-compose.yml`:

```
+---------------------------------------------------------------+
|                       Docker Compose                          |
|                                                               |
|   +-----------------------+       +-----------------------+   |
|   |   neoagenda-front     |       |     neoagenda-api     |   |
|   |   (Vue 3 + Nginx)     |       |   (.NET 8 ASP.NET)    |   |
|   |   Porta: 8080 / 8081  |------>|   Porta: 5000         |   |
|   +-----------------------+       +-----------------------+   |
|                                               |               |
|                                               v               |
|                                   +-----------------------+   |
|                                   |  Volume Persistente   |   |
|                                   |  (SQLite / Data)      |   |
|                                   +-----------------------+   |
+---------------------------------------------------------------+
```

---

## 2. Instruções de Execução

### 2.1 Em 1 Comando com Docker
```bash
docker compose up --build
```

### 2.2 Credenciais Padrão Pré-configuradas (Seed):
- **Administrador:**
  - Usuário: `admin`
  - Senha: `Admin@123`
- **Usuário Padrão:**
  - Usuário: `usuario`
  - Senha: `User@123`

---

## 3. URLs e Portas de Acesso

| Serviço | URL | Descrição |
| :--- | :--- | :--- |
| **Frontend Web** | `http://localhost:8080` (ou porta configurada) | Interface do usuário Vue 3 + PrimeVue |
| **Backend API & Swagger** | `http://localhost:5000` | Documentação interativa OpenAPI |
| **Healthcheck** | `http://localhost:5000/health` | Status de saúde da aplicação |
