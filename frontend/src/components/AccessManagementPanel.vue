<script setup lang="ts">
import { ref, onMounted } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import { useToast } from 'primevue/usetoast';
import { accessApi, extractErrorMessage } from '../api/client';
import type { UserDto, AccessLogDto } from '../types/telemetry';

const toast = useToast();
const users = ref<UserDto[]>([]);
const logs = ref<AccessLogDto[]>([]);
const isLoadingUsers = ref(false);
const isLoadingLogs = ref(false);

const subTab = ref<'users' | 'logs'>('users');

const createUserDialogVisible = ref(false);
const isCreatingUser = ref(false);
const newUsername = ref('');
const newPassword = ref('');
const newName = ref('');
const newRole = ref('Admin');

onMounted(async () => {
  await loadAll();
});

async function loadAll() {
  await Promise.all([loadUsers(), loadLogs()]);
}

async function loadUsers() {
  isLoadingUsers.value = true;
  try {
    users.value = await accessApi.getUsers();
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: extractErrorMessage(err), life: 4000 });
  } finally {
    isLoadingUsers.value = false;
  }
}

async function loadLogs() {
  isLoadingLogs.value = true;
  try {
    logs.value = await accessApi.getLogs(100);
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: extractErrorMessage(err), life: 4000 });
  } finally {
    isLoadingLogs.value = false;
  }
}

async function handleRevoke(user: UserDto) {
  try {
    await accessApi.revokeAccess(user.id);
    toast.add({
      severity: 'success',
      summary: 'Sessão Revogada',
      detail: `Sessões ativas do usuário ${user.username} foram revogadas.`,
      life: 3000
    });
    await loadAll();
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: extractErrorMessage(err), life: 4000 });
  }
}

async function handleCreateUser() {
  if (!newUsername.value.trim() || !newPassword.value || !newName.value.trim()) {
    toast.add({ severity: 'warn', summary: 'Atenção', detail: 'Preencha todos os campos.', life: 3000 });
    return;
  }

  isCreatingUser.value = true;
  try {
    await accessApi.createUser({
      username: newUsername.value.trim(),
      password: newPassword.value,
      name: newName.value.trim(),
      role: newRole.value
    });

    toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Novo usuário criado com sucesso.', life: 3000 });
    createUserDialogVisible.value = false;
    newUsername.value = '';
    newPassword.value = '';
    newName.value = '';
    await loadAll();
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro ao Criar', detail: extractErrorMessage(err), life: 4000 });
  } finally {
    isCreatingUser.value = false;
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
  <div style="display: flex; flex-direction: column; gap: 1.25rem;">
    <!-- Subtabs -->
    <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.75rem;">
      <div style="display: flex; gap: 0.5rem;">
        <Button
          label="Usuários Cadastrados"
          icon="pi pi-users"
          :text="subTab !== 'users'"
          :severity="subTab === 'users' ? 'primary' : 'secondary'"
          @click="subTab = 'users'"
        />
        <Button
          label="Trilha de Auditoria de Acessos"
          icon="pi pi-shield"
          :text="subTab !== 'logs'"
          :severity="subTab === 'logs' ? 'primary' : 'secondary'"
          @click="subTab = 'logs'; loadLogs();"
        />
      </div>

      <div style="display: flex; gap: 0.5rem;">
        <Button
          v-if="subTab === 'users'"
          label="Novo Usuário"
          icon="pi pi-user-plus"
          size="small"
          @click="createUserDialogVisible = true"
        />
        <Button
          icon="pi pi-refresh"
          severity="secondary"
          text
          rounded
          @click="loadAll"
          title="Atualizar dados"
        />
      </div>
    </div>

    <!-- Tabela de Usuários -->
    <div v-if="subTab === 'users'">
      <DataTable :value="users" :loading="isLoadingUsers" responsiveLayout="stack" stripedRows>
        <template #empty>
          <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
            Nenhum usuário cadastrado.
          </div>
        </template>

        <Column field="username" header="Usuário / Login" style="min-width: 150px;">
          <template #body="{ data }">
            <span style="font-weight: 700; color: var(--text-main);">{{ data.username }}</span>
          </template>
        </Column>

        <Column field="name" header="Nome Completo" style="min-width: 180px;" />

        <Column field="role" header="Função" style="min-width: 110px;">
          <template #body="{ data }">
            <Tag :value="data.role" severity="info" />
          </template>
        </Column>

        <Column field="hasActiveSession" header="Sessão Ativa" style="min-width: 140px;">
          <template #body="{ data }">
            <Tag
              :value="data.hasActiveSession ? 'Conectado' : 'Inativo'"
              :severity="data.hasActiveSession ? 'success' : 'secondary'"
              :icon="data.hasActiveSession ? 'pi pi-check' : 'pi pi-minus'"
            />
          </template>
        </Column>

        <Column field="createdAt" header="Cadastrado em" style="min-width: 160px;">
          <template #body="{ data }">
            <span>{{ new Date(data.createdAt).toLocaleDateString('pt-BR') }}</span>
          </template>
        </Column>

        <Column header="Ações" style="width: 140px; text-align: right;">
          <template #body="{ data }">
            <Button
              v-if="data.hasActiveSession"
              label="Revogar Sessão"
              icon="pi pi-ban"
              size="small"
              severity="danger"
              outlined
              @click="handleRevoke(data)"
            />
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Tabela de Auditoria de Acessos -->
    <div v-if="subTab === 'logs'">
      <DataTable :value="logs" :loading="isLoadingLogs" :rows="10" paginator responsiveLayout="stack" stripedRows>
        <template #empty>
          <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
            Nenhum evento de acesso registrado.
          </div>
        </template>

        <Column field="timestamp" header="Data / Hora" style="min-width: 160px;">
          <template #body="{ data }">
            <span>{{ new Date(data.timestamp).toLocaleString('pt-BR') }}</span>
          </template>
        </Column>

        <Column field="username" header="Usuário" style="min-width: 120px;">
          <template #body="{ data }">
            <span style="font-weight: 600;">{{ data.username }}</span>
          </template>
        </Column>

        <Column field="action" header="Ação" style="min-width: 160px;">
          <template #body="{ data }">
            <Tag
              :value="data.action"
              :severity="getActionSeverity(data.action, data.success)"
            />
          </template>
        </Column>

        <Column field="ipAddress" header="IP" style="min-width: 120px;">
          <template #body="{ data }">
            <code style="font-size: 0.8rem; color: var(--text-muted);">{{ data.ipAddress }}</code>
          </template>
        </Column>

        <Column field="details" header="Detalhes" style="min-width: 200px;">
          <template #body="{ data }">
            <span style="font-size: 0.85rem; color: var(--text-muted);">{{ data.details || '-' }}</span>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Modal Novo Usuário -->
    <Dialog
      v-model:visible="createUserDialogVisible"
      header="Cadastrar Novo Usuário"
      modal
      :style="{ width: '420px' }"
    >
      <form @submit.prevent="handleCreateUser" style="display: flex; flex-direction: column; gap: 1rem; padding-top: 0.5rem;">
        <div style="display: flex; flex-direction: column; gap: 0.35rem;">
          <label style="font-size: 0.85rem; font-weight: 600;">Nome Completo</label>
          <InputText v-model="newName" placeholder="Ex: Maria Fernandes" />
        </div>

        <div style="display: flex; flex-direction: column; gap: 0.35rem;">
          <label style="font-size: 0.85rem; font-weight: 600;">Login / Nome de Usuário</label>
          <InputText v-model="newUsername" placeholder="Ex: maria.fernandes" />
        </div>

        <div style="display: flex; flex-direction: column; gap: 0.35rem;">
          <label style="font-size: 0.85rem; font-weight: 600;">Senha de Acesso</label>
          <Password v-model="newPassword" placeholder="Mínimo 6 caracteres" toggleMask :feedback="false" :inputStyle="{ width: '100%' }" />
        </div>

        <div style="display: flex; flex-direction: column; gap: 0.35rem;">
          <label style="font-size: 0.85rem; font-weight: 600;">Função / Perfil</label>
          <InputText v-model="newRole" placeholder="Ex: Admin, Operador" />
        </div>

        <div style="display: flex; justify-content: flex-end; gap: 0.5rem; margin-top: 0.5rem;">
          <Button label="Cancelar" icon="pi pi-times" text @click="createUserDialogVisible = false" />
          <Button label="Salvar Usuário" icon="pi pi-check" :loading="isCreatingUser" type="submit" />
        </div>
      </form>
    </Dialog>
  </div>
</template>
