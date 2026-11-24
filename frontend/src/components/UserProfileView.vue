<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import { useAuthStore } from '../stores/authStore';
import { accessApi, extractErrorMessage } from '../api/client';
import type { AccessLogDto } from '../types/telemetry';

const authStore = useAuthStore();
const activeTab = ref<'dados' | 'historico'>('dados');
const userLogs = ref<AccessLogDto[]>([]);
const isLoadingLogs = ref(false);

const userInitials = computed(() => {
  const name = authStore.user?.name || authStore.user?.username || 'U';
  return name.substring(0, 2).toUpperCase();
});

onMounted(async () => {
  await loadUserHistory();
});

async function loadUserHistory() {
  isLoadingLogs.value = true;
  try {
    userLogs.value = await accessApi.getMyLogs(50);
  } catch (err) {
    console.error('Erro ao carregar histórico pessoal:', extractErrorMessage(err));
  } finally {
    isLoadingLogs.value = false;
  }
}

function getActionLabel(action: string): string {
  switch (action) {
    case 'LOGIN_SUCCESS': return 'Acesso ao Sistema (Login)';
    case 'LOGIN_FAILED': return 'Tentativa de Acesso Falha';
    case 'TOKEN_REFRESH': return 'Renovação de Sessão';
    case 'USER_CREATED': return 'Cadastro de Novo Usuário';
    case 'ACCESS_REVOKED': return 'Revogação de Acesso';
    default: return action;
  }
}

function getActionSeverity(action: string, success: boolean): 'success' | 'danger' | 'warning' | 'info' {
  if (!success) return 'danger';
  if (action === 'LOGIN_SUCCESS') return 'success';
  if (action === 'ACCESS_REVOKED') return 'warning';
  return 'info';
}
</script>

<template>
  <div class="user-view-container">
    <!-- Sub-Abas: Meus Dados / Histórico de Movimentações -->
    <div style="display: flex; gap: 0.5rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem;">
      <Button
        label="Meus Dados"
        icon="pi pi-user"
        :text="activeTab !== 'dados'"
        :severity="activeTab === 'dados' ? 'primary' : 'secondary'"
        @click="activeTab = 'dados'"
      />
      <Button
        label="Histórico de Movimentações"
        icon="pi pi-history"
        :text="activeTab !== 'historico'"
        :severity="activeTab === 'historico' ? 'primary' : 'secondary'"
        @click="activeTab = 'historico'; loadUserHistory();"
      />
    </div>

    <!-- Aba 1: Meus Dados -->
    <div v-if="activeTab === 'dados'" style="display: flex; flex-direction: column; gap: 1.25rem;">
      <div class="card-box header-box">
        <div class="avatar-large">
          {{ userInitials }}
        </div>
        <div>
          <div style="display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;">
            <h3 style="font-size: 1.35rem; font-weight: 800; color: var(--text-main); margin: 0;">
              {{ authStore.user?.name || 'Usuário' }}
            </h3>
            <Tag
              :value="authStore.isAdmin ? 'Administrador' : 'Usuário'"
              :severity="authStore.isAdmin ? 'danger' : 'info'"
            />
          </div>
          <span style="font-size: 0.85rem; color: var(--text-muted); margin-top: 0.25rem; display: block;">
            Nome de usuário: <strong>{{ authStore.user?.username }}</strong>
          </span>
        </div>
      </div>

      <div class="card-box">
        <h4 style="font-size: 1rem; font-weight: 700; color: var(--text-main); margin-bottom: 1rem;">
          Informações da Conta
        </h4>
        <div class="info-grid">
          <div class="info-field">
            <span class="info-label">Nome Completo</span>
            <span class="info-value">{{ authStore.user?.name }}</span>
          </div>
          <div class="info-field">
            <span class="info-label">Login de Acesso</span>
            <span class="info-value">{{ authStore.user?.username }}</span>
          </div>
          <div class="info-field">
            <span class="info-label">Perfil de Acesso</span>
            <span class="info-value">{{ authStore.isAdmin ? 'Administrador do Sistema' : 'Usuário Operador' }}</span>
          </div>
          <div class="info-field">
            <span class="info-label">Status da Conta</span>
            <Tag value="Ativo e Verificado" severity="success" style="width: fit-content;" />
          </div>
        </div>
      </div>
    </div>

    <!-- Aba 2: Histórico de Movimentações Pessoal -->
    <div v-if="activeTab === 'historico'" style="display: flex; flex-direction: column; gap: 1rem;">
      <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.5rem;">
        <div>
          <h4 style="font-size: 1.05rem; font-weight: 700; margin: 0; color: var(--text-main);">
            Meu Histórico de Acessos e Ações
          </h4>
          <p style="font-size: 0.8rem; color: var(--text-muted); margin: 0.2rem 0 0 0;">
            Registro cronológico das suas entradas, renovações de sessão e movimentações na sua conta.
          </p>
        </div>

        <Button
          label="Atualizar"
          icon="pi pi-refresh"
          size="small"
          severity="secondary"
          text
          :loading="isLoadingLogs"
          @click="loadUserHistory"
        />
      </div>

      <DataTable :value="userLogs" :loading="isLoadingLogs" :rows="8" paginator responsiveLayout="stack" stripedRows size="small">
        <template #empty>
          <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
            Nenhum registro de atividade pessoal encontrado.
          </div>
        </template>

        <Column field="timestamp" header="Data e Horário" style="min-width: 170px;">
          <template #body="{ data }">
            <span style="font-size: 0.85rem;">{{ new Date(data.timestamp).toLocaleString('pt-BR') }}</span>
          </template>
        </Column>

        <Column field="action" header="Ação Realizada" style="min-width: 200px;">
          <template #body="{ data }">
            <Tag
              :value="getActionLabel(data.action)"
              :severity="getActionSeverity(data.action, data.success)"
              style="font-size: 0.8rem;"
            />
          </template>
        </Column>

        <Column field="ipAddress" header="Endereço IP" style="min-width: 130px;">
          <template #body="{ data }">
            <code style="font-size: 0.8rem; color: var(--text-muted);">{{ data.ipAddress }}</code>
          </template>
        </Column>

        <Column field="details" header="Detalhes da Operação" style="min-width: 220px;">
          <template #body="{ data }">
            <span style="font-size: 0.85rem; color: var(--text-muted);">{{ data.details || '-' }}</span>
          </template>
        </Column>
      </DataTable>
    </div>
  </div>
</template>

<style scoped>
.user-view-container {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.card-box {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 0.85rem;
  padding: 1.25rem;
}

.header-box {
  display: flex;
  align-items: center;
  gap: 1.25rem;
  flex-wrap: wrap;
}

.avatar-large {
  width: 4rem;
  height: 4rem;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #4f46e5);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  font-weight: 800;
  box-shadow: 0 4px 10px rgba(99, 102, 241, 0.25);
}

.info-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
}

.info-field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding: 0.75rem;
  border-radius: 0.5rem;
  background: var(--bg-primary);
}

.info-label {
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.info-value {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--text-main);
}
</style>
