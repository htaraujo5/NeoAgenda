<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';
import Button from 'primevue/button';
import Tag from 'primevue/tag';

import LoginView from './components/LoginView.vue';
import ContactsView from './components/ContactsView.vue';
import AccessManagementPanel from './components/AccessManagementPanel.vue';
import PerformanceMetricsPanel from './components/PerformanceMetricsPanel.vue';
import UserProfileView from './components/UserProfileView.vue';

import { useContactStore } from './stores/contactStore';
import { useAuthStore } from './stores/authStore';
import { authApi, extractErrorMessage } from './api/client';

const store = useContactStore();
const authStore = useAuthStore();
const toast = useToast();

// Menus de navegação:
// 'contacts' (Agenda & Contatos)
// 'profile' (Dados do Usuário & Permissões)
// 'access' (Gestão de Usuários - Exclusivo Admin)
// 'metrics' (Desempenho & APM - Exclusivo Admin)
const currentNav = ref<'contacts' | 'profile' | 'access' | 'metrics'>('contacts');
const isDarkMode = ref(false);

onMounted(async () => {
  // Inicialização do Tema
  const savedTheme = localStorage.getItem('neoagenda-theme');
  if (savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
    isDarkMode.value = true;
    document.documentElement.classList.add('dark');
  } else {
    isDarkMode.value = false;
    document.documentElement.classList.remove('dark');
  }

  window.addEventListener('auth:expired', handleAuthExpired);

  if (authStore.isAuthenticated) {
    await loadInitialData();
  }
});

function toggleDarkMode() {
  isDarkMode.value = !isDarkMode.value;
  if (isDarkMode.value) {
    document.documentElement.classList.add('dark');
    localStorage.setItem('neoagenda-theme', 'dark');
  } else {
    document.documentElement.classList.remove('dark');
    localStorage.setItem('neoagenda-theme', 'light');
  }
}

async function loadInitialData() {
  try {
    await Promise.all([
      store.fetchContacts(),
      store.fetchRecycleBin()
    ]);
  } catch (err) {
    toast.add({
      severity: 'error',
      summary: 'Erro de Conexão',
      detail: extractErrorMessage(err),
      life: 5000
    });
  }
}

function handleAuthExpired() {
  authStore.logout();
  toast.add({
    severity: 'warn',
    summary: 'Sessão Expirada',
    detail: 'Sua sessão expirou. Por favor, faça login novamente.',
    life: 5000
  });
}

async function handleLogout() {
  await authApi.logout();
  authStore.logout();
  currentNav.value = 'contacts';
  toast.add({
    severity: 'info',
    summary: 'Logout Realizado',
    detail: 'Você encerrou a sessão com sucesso.',
    life: 3000
  });
}
</script>

<template>
  <Toast />
  <ConfirmDialog />

  <!-- Tela de Login Se Não Autenticado -->
  <LoginView v-if="!authStore.isAuthenticated" @login-success="loadInitialData" />

  <!-- Estrutura com Menu e Telas Modulares -->
  <div v-else class="app-layout">
    <!-- Barra Lateral de Navegação (Sidebar) -->
    <aside class="app-sidebar">
      <!-- Marca -->
      <div class="sidebar-brand">
        <div class="brand-icon">
          <i class="pi pi-address-book"></i>
        </div>
        <div>
          <span class="brand-name">NeoAgenda</span>
          <span class="brand-tag">v2.0 Pro</span>
        </div>
      </div>

      <!-- Menu Principal -->
      <nav class="sidebar-nav">
        <div class="nav-section-title">MENU</div>

        <button
          class="nav-item"
          :class="{ active: currentNav === 'contacts' }"
          @click="currentNav = 'contacts'"
        >
          <i class="pi pi-users"></i>
          <span>Agenda & Contatos</span>
          <Tag v-if="store.totalCount > 0" :value="store.totalCount" severity="secondary" class="nav-counter" />
        </button>

        <!-- Seção Administrativa (Exclusivo Administrador) -->
        <template v-if="authStore.isAdmin">
          <div class="nav-section-title" style="margin-top: 1.25rem;">ADMINISTRAÇÃO</div>

          <button
            class="nav-item"
            :class="{ active: currentNav === 'access' }"
            @click="currentNav = 'access'"
          >
            <i class="pi pi-id-card"></i>
            <span>Gestão de Usuários</span>
          </button>

          <button
            class="nav-item"
            :class="{ active: currentNav === 'metrics' }"
            @click="currentNav = 'metrics'"
          >
            <i class="pi pi-chart-line"></i>
            <span>Desempenho & APM</span>
          </button>
        </template>
      </nav>

      <!-- Rodapé da Sidebar: Usuário & Tema -->
      <div class="sidebar-footer">
        <div class="user-pill" @click="currentNav = 'profile'" title="Ver dados do usuário">
          <div class="user-avatar-mini">
            {{ (authStore.user?.username || 'U').substring(0, 1).toUpperCase() }}
          </div>
          <div class="user-details-mini">
            <span class="user-name-mini">{{ authStore.user?.name || authStore.user?.username }}</span>
            <span class="user-role-mini">{{ authStore.isAdmin ? 'Administrador' : 'Usuário' }}</span>
          </div>
        </div>

        <div style="display: flex; gap: 0.25rem;">
          <Button
            :icon="isDarkMode ? 'pi pi-sun' : 'pi pi-moon'"
            severity="secondary"
            text
            rounded
            @click="toggleDarkMode"
            :title="isDarkMode ? 'Tema Claro' : 'Tema Escuro'"
          />
          <Button
            icon="pi pi-power-off"
            severity="danger"
            text
            rounded
            @click="handleLogout"
            title="Encerrar Sessão"
          />
        </div>
      </div>
    </aside>

    <!-- Conteúdo Principal Dinâmico -->
    <main class="app-main-content">
      <!-- Topbar com Título da Tela e Breadcrumb -->
      <header class="content-header">
        <div>
          <h2 class="content-title">
            <template v-if="currentNav === 'contacts'">Gestão de Contatos & Agenda</template>
            <template v-else-if="currentNav === 'profile'">Meus Dados & Histórico</template>
            <template v-else-if="currentNav === 'access'">Gestão de Usuários & Auditoria</template>
            <template v-else-if="currentNav === 'metrics'">Métricas & Acompanhamento de Desempenho</template>
          </h2>
          <span class="content-desc">
            <template v-if="currentNav === 'contacts'">Controle completo com unicidade E.164, retenção de 30 dias e LGPD.</template>
            <template v-else-if="currentNav === 'profile'">Visualize suas informações cadastrais e o histórico pessoal das suas atividades.</template>
            <template v-else-if="currentNav === 'access'">Criação de novos operadores, logs gerais e revogação de acessos.</template>
            <template v-else-if="currentNav === 'metrics'">Telemetria isolada com latência média, percentil P95 e status codes da API.</template>
          </span>
        </div>

        <Tag
          :value="authStore.isAdmin ? 'Acesso Total (Admin)' : 'Acesso Limitado (Usuário)'"
          :severity="authStore.isAdmin ? 'danger' : 'info'"
        />
      </header>

      <!-- Painel Principal Renderizado Conforme Navegação -->
      <div class="view-card">
        <ContactsView v-if="currentNav === 'contacts'" />
        <UserProfileView v-else-if="currentNav === 'profile'" />
        <AccessManagementPanel v-else-if="currentNav === 'access' && authStore.isAdmin" />
        <PerformanceMetricsPanel v-else-if="currentNav === 'metrics' && authStore.isAdmin" />
      </div>
    </main>
  </div>
</template>

<style scoped>
.app-layout {
  display: flex;
  min-height: 100vh;
  background-color: var(--bg-primary);
}

/* Sidebar */
.app-sidebar {
  width: 260px;
  background: var(--bg-card);
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 1.25rem 1rem;
  position: sticky;
  top: 0;
  height: 100vh;
  z-index: 100;
  flex-shrink: 0;
}

.sidebar-brand {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding-bottom: 1.25rem;
  border-bottom: 1px solid var(--border-color);
}

.brand-icon {
  width: 2.25rem;
  height: 2.25rem;
  background: linear-gradient(135deg, #6366f1, #4f46e5);
  color: white;
  border-radius: 0.6rem;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.15rem;
}

.brand-name {
  font-size: 1.2rem;
  font-weight: 800;
  color: var(--text-main);
  line-height: 1.1;
  display: block;
}

.brand-tag {
  font-size: 0.7rem;
  color: #6366f1;
  font-weight: 700;
  letter-spacing: 0.05em;
}

.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin-top: 1.25rem;
  flex: 1;
}

.nav-section-title {
  font-size: 0.7rem;
  font-weight: 800;
  color: var(--text-muted);
  letter-spacing: 0.08em;
  padding: 0.5rem 0.75rem 0.25rem 0.75rem;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.65rem 0.85rem;
  border-radius: 0.65rem;
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 0.875rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.15s ease;
  width: 100%;
  text-align: left;
}

.nav-item i {
  font-size: 1rem;
}

.nav-item:hover {
  background: var(--bg-primary);
  color: var(--text-main);
}

.nav-item.active {
  background: rgba(99, 102, 241, 0.1);
  color: #6366f1;
}

.nav-counter {
  margin-left: auto;
  font-size: 0.7rem;
}

.sidebar-footer {
  padding-top: 1rem;
  border-top: 1px solid var(--border-color);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.user-pill {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
  padding: 0.35rem;
  border-radius: 0.5rem;
  flex: 1;
  min-width: 0;
  transition: background 0.15s ease;
}

.user-pill:hover {
  background: var(--bg-primary);
}

.user-avatar-mini {
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  background: #6366f1;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.85rem;
  font-weight: 700;
  flex-shrink: 0;
}

.user-details-mini {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.user-name-mini {
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--text-main);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-role-mini {
  font-size: 0.7rem;
  color: var(--text-muted);
}

/* Conteúdo Principal */
.app-main-content {
  flex: 1;
  padding: 2rem;
  overflow-y: auto;
  max-width: 1400px;
}

.content-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  flex-wrap: wrap;
  gap: 1rem;
}

.content-title {
  font-size: 1.4rem;
  font-weight: 800;
  color: var(--text-main);
  margin: 0;
}

.content-desc {
  font-size: 0.85rem;
  color: var(--text-muted);
}

.view-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 1rem;
  padding: 1.5rem;
  box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.05);
}

@media (max-width: 768px) {
  .app-layout {
    flex-direction: column;
  }
  .app-sidebar {
    width: 100%;
    height: auto;
    position: relative;
    border-right: none;
    border-bottom: 1px solid var(--border-color);
  }
  .app-main-content {
    padding: 1rem;
  }
}
</style>
