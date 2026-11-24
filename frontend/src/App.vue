<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Tag from 'primevue/tag';
import Toast from 'primevue/toast';
import ConfirmDialog from 'primevue/confirmdialog';

import ContactFormDialog from './components/ContactFormDialog.vue';
import LgpdConfirmDialog from './components/LgpdConfirmDialog.vue';
import CsvImportDialog from './components/CsvImportDialog.vue';
import { useContactStore } from './stores/contactStore';
import { contactApi, extractErrorMessage } from './api/client';
import type { ContactDto, RecycleBinContactDto } from './types/contact';

const store = useContactStore();
const toast = useToast();
const confirm = useConfirm();

// State
const activeTab = ref<'active' | 'recycle' | 'lgpd'>('active');
const isDarkMode = ref(false);

const formDialogVisible = ref(false);
const editingContact = ref<ContactDto | null>(null);

const lgpdDialogVisible = ref(false);
const targetLgpdContact = ref<ContactDto | RecycleBinContactDto | null>(null);

const csvDialogVisible = ref(false);
const csvDialogRef = ref<InstanceType<typeof CsvImportDialog> | null>(null);

onMounted(async () => {
  // Inicialização do Dark Mode
  const savedTheme = localStorage.getItem('neoagenda-theme');
  if (savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
    isDarkMode.value = true;
    document.documentElement.classList.add('dark');
  }

  await loadData();
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

async function loadData() {
  try {
    await Promise.all([
      store.fetchContacts(),
      store.fetchRecycleBin(),
      store.fetchLgpdAudits()
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

async function handleSearch() {
  store.currentPage = 1;
  await store.fetchContacts();
}

function openCreateDialog() {
  editingContact.value = null;
  formDialogVisible.value = true;
}

function openEditDialog(contact: ContactDto) {
  editingContact.value = contact;
  formDialogVisible.value = true;
}

async function handleSaveContact(payload: { name: string; email: string; phone: string }) {
  try {
    if (editingContact.value) {
      await store.updateContact(editingContact.value.id, payload);
      toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Contato atualizado com sucesso.', life: 3000 });
    } else {
      await store.createContact(payload);
      toast.add({ severity: 'success', summary: 'Sucesso', detail: 'Contato cadastrado com sucesso.', life: 3000 });
    }
    formDialogVisible.value = false;
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Não foi possível salvar', detail: extractErrorMessage(err), life: 5000 });
  }
}

function confirmSoftDelete(contact: ContactDto) {
  confirm.require({
    message: `Deseja mover o contato "${contact.name}" para a lixeira? Ele poderá ser restaurado por até 30 dias.`,
    header: 'Mover para a Lixeira',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Cancelar',
    acceptLabel: 'Mover para Lixeira',
    acceptClass: 'p-button-warning',
    accept: async () => {
      try {
        await store.softDeleteContact(contact.id);
        toast.add({ severity: 'info', summary: 'Na Lixeira', detail: 'Contato movido para a lixeira.', life: 3000 });
      } catch (err) {
        toast.add({ severity: 'error', summary: 'Erro', detail: extractErrorMessage(err), life: 4000 });
      }
    }
  });
}

async function handleRestore(contact: RecycleBinContactDto) {
  try {
    await store.restoreContact(contact.id);
    toast.add({ severity: 'success', summary: 'Restaurado', detail: `Contato "${contact.name}" reativado na agenda.`, life: 3000 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Falha na Restauração', detail: extractErrorMessage(err), life: 5000 });
  }
}

function openLgpdDialog(contact: ContactDto | RecycleBinContactDto) {
  targetLgpdContact.value = contact;
  lgpdDialogVisible.value = true;
}

async function handleConfirmLgpdPurge(id: string) {
  try {
    const result = await store.purgeLgpd(id);
    lgpdDialogVisible.value = false;
    toast.add({
      severity: 'warn',
      summary: 'LGPD: Dados Excluídos',
      detail: `${result.message} Protocolo: ${result.protocol}`,
      life: 6000
    });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro na exclusão LGPD', detail: extractErrorMessage(err), life: 5000 });
  }
}

async function handleCsvUpload(file: File) {
  if (csvDialogRef.value) csvDialogRef.value.isUploading = true;
  try {
    const result = await store.importCsv(file);
    if (csvDialogRef.value) csvDialogRef.value.setResult(result);
    toast.add({
      severity: result.failedCount === 0 ? 'success' : 'warn',
      summary: 'Importação Concluída',
      detail: `${result.importedCount} importados, ${result.failedCount} inconsistências ignoradas.`,
      life: 5000
    });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Falha na Importação', detail: extractErrorMessage(err), life: 5000 });
  } finally {
    if (csvDialogRef.value) csvDialogRef.value.isUploading = false;
  }
}

function downloadExportCsv() {
  window.open(contactApi.getExportCsvUrl(), '_blank');
}

function exportLgpdAuditsJson() {
  if (store.lgpdAudits.length === 0) return;
  const blob = new Blob([JSON.stringify(store.lgpdAudits, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `comprovantes_auditoria_lgpd_${Date.now()}.json`;
  a.click();
  URL.revokeObjectURL(url);
}
</script>

<template>
  <Toast />
  <ConfirmDialog />

  <div class="layout-container">
    <!-- Header -->
    <header class="app-header">
      <div class="brand-badge">
        <div class="brand-icon">
          <i class="pi pi-address-book"></i>
        </div>
        <div>
          <h1 class="brand-title">NeoAgenda</h1>
          <p class="brand-subtitle">Gestão Inteligente & Privacidade LGPD</p>
        </div>
      </div>

      <div class="toolbar-actions">
        <Button label="Novo Contato" icon="pi pi-plus" @click="openCreateDialog" />
        <Button label="Importar CSV" icon="pi pi-upload" severity="secondary" outlined @click="csvDialogVisible = true" />
        <Button label="Exportar CSV" icon="pi pi-download" severity="secondary" text @click="downloadExportCsv" />
        <Button
          :icon="isDarkMode ? 'pi pi-sun' : 'pi pi-moon'"
          severity="secondary"
          text
          rounded
          @click="toggleDarkMode"
          :title="isDarkMode ? 'Mudar para tema claro' : 'Mudar para tema escuro'"
        />
      </div>
    </header>

    <!-- Main Card -->
    <main class="card">
      <!-- Tabs Switcher -->
      <div style="display: flex; gap: 0.5rem; margin-bottom: 1.5rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.75rem; flex-wrap: wrap;">
        <Button
          :label="'Contatos Ativos (' + store.totalCount + ')'"
          icon="pi pi-users"
          :text="activeTab !== 'active'"
          :severity="activeTab === 'active' ? 'primary' : 'secondary'"
          @click="activeTab = 'active'"
        />
        <Button
          :label="'Lixeira (' + store.recycleBinContacts.length + ')'"
          icon="pi pi-trash"
          :text="activeTab !== 'recycle'"
          :severity="activeTab === 'recycle' ? 'warning' : 'secondary'"
          @click="activeTab = 'recycle'"
        />
        <Button
          :label="'Auditoria LGPD (' + store.lgpdAudits.length + ')'"
          icon="pi pi-shield"
          :text="activeTab !== 'lgpd'"
          :severity="activeTab === 'lgpd' ? 'danger' : 'secondary'"
          @click="activeTab = 'lgpd'; store.fetchLgpdAudits();"
        />
      </div>

      <!-- Search & Filters (Ativos) -->
      <div v-if="activeTab === 'active'" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; gap: 1rem; flex-wrap: wrap;">
        <div style="display: flex; gap: 0.5rem; flex: 1; max-width: 450px;">
          <InputText
            v-model="store.searchQuery"
            placeholder="Buscar por nome, e-mail ou telefone..."
            @keyup.enter="handleSearch"
            style="width: 100%;"
          />
          <Button icon="pi pi-search" @click="handleSearch" />
        </div>
        <small style="color: var(--text-muted);">Total de registros: <strong>{{ store.totalCount }}</strong></small>
      </div>

      <!-- Tabela de Contatos Ativos -->
      <div v-if="activeTab === 'active'">
        <DataTable
          :value="store.contacts"
          :loading="store.isLoading"
          lazy
          :totalRecords="store.totalCount"
          :rows="store.pageSize"
          :first="(store.currentPage - 1) * store.pageSize"
          @page="(e) => { store.currentPage = (e.page ?? 0) + 1; store.pageSize = e.rows; store.fetchContacts(); }"
          paginator
          responsiveLayout="stack"
          stripedRows
        >
          <template #empty>
            <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
              <i class="pi pi-inbox" style="font-size: 2.5rem; margin-bottom: 0.5rem; display: block;"></i>
              Nenhum contato encontrado.
            </div>
          </template>

          <Column field="name" header="Nome" sortable style="min-width: 200px;">
            <template #body="{ data }">
              <span style="font-weight: 600;">{{ data.name }}</span>
            </template>
          </Column>

          <Column field="email" header="E-mail" style="min-width: 220px;">
            <template #body="{ data }">
              <a :href="'mailto:' + data.email" style="color: #6366f1; text-decoration: none;">{{ data.email }}</a>
            </template>
          </Column>

          <Column field="phoneOriginal" header="Telefone" style="min-width: 180px;">
            <template #body="{ data }">
              <span class="phone-badge">{{ data.phoneOriginal }}</span>
            </template>
          </Column>

          <Column header="Ações" style="width: 140px; text-align: right;">
            <template #body="{ data }">
              <div style="display: flex; gap: 0.25rem; justify-content: flex-end;">
                <Button icon="pi pi-pencil" text rounded size="small" @click="openEditDialog(data)" title="Editar" />
                <Button icon="pi pi-trash" text rounded severity="warning" size="small" @click="confirmSoftDelete(data)" title="Mover para lixeira" />
                <Button icon="pi pi-shield" text rounded severity="danger" size="small" @click="openLgpdDialog(data)" title="Exclusão Definitiva LGPD" />
              </div>
            </template>
          </Column>
        </DataTable>
      </div>

      <!-- Tabela da Lixeira (Janela de 30 Dias) -->
      <div v-else-if="activeTab === 'recycle'">
        <div style="background: rgba(245, 158, 11, 0.08); border: 1px solid #f59e0b; border-radius: 0.75rem; padding: 1rem; margin-bottom: 1.25rem; display: flex; gap: 0.75rem; align-items: center;">
          <i class="pi pi-clock" style="font-size: 1.5rem; color: #f59e0b;"></i>
          <span style="font-size: 0.85rem; color: var(--text-main);">
            Contatos na lixeira podem ser <strong>restaurados durante até 30 dias</strong> após a exclusão. Após esse prazo, são expurgados permanentemente.
          </span>
        </div>

        <DataTable :value="store.recycleBinContacts" :loading="store.isRecycleBinLoading" responsiveLayout="stack" stripedRows>
          <template #empty>
            <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
              <i class="pi pi-check-circle" style="font-size: 2.5rem; margin-bottom: 0.5rem; display: block; color: #22c55e;"></i>
              A lixeira está vazia. Nenhum contato excluído temporariamente.
            </div>
          </template>

          <Column field="name" header="Nome" style="min-width: 180px;">
            <template #body="{ data }">
              <span style="font-weight: 600;">{{ data.name }}</span>
            </template>
          </Column>

          <Column field="email" header="E-mail" style="min-width: 200px;" />
          <Column field="phoneOriginal" header="Telefone" style="min-width: 160px;">
            <template #body="{ data }">
              <span class="phone-badge">{{ data.phoneOriginal }}</span>
            </template>
          </Column>

          <Column header="Retenção (30 dias)" style="min-width: 180px;">
            <template #body="{ data }">
              <Tag
                :severity="data.daysRemaining > 5 ? 'warning' : 'danger'"
                :value="data.isExpired ? 'Expirado' : 'Restam ' + data.daysRemaining + ' dias'"
                icon="pi pi-clock"
              />
            </template>
          </Column>

          <Column header="Ações" style="width: 180px; text-align: right;">
            <template #body="{ data }">
              <div style="display: flex; gap: 0.5rem; justify-content: flex-end;">
                <Button
                  label="Restaurar"
                  icon="pi pi-replay"
                  size="small"
                  severity="success"
                  outlined
                  :disabled="data.isExpired"
                  @click="handleRestore(data)"
                />
                <Button
                  icon="pi pi-shield"
                  size="small"
                  severity="danger"
                  text
                  @click="openLgpdDialog(data)"
                  title="Expurgo Imediato LGPD"
                />
              </div>
            </template>
          </Column>
        </DataTable>
      </div>

      <!-- Tabela de Auditoria LGPD (Compliance Art. 18, VI) -->
      <div v-else-if="activeTab === 'lgpd'">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.25rem; flex-wrap: wrap; gap: 1rem;">
          <div style="display: flex; align-items: center; gap: 0.75rem;">
            <i class="pi pi-shield" style="font-size: 1.75rem; color: #ef4444;"></i>
            <div>
              <h4 style="font-size: 1.1rem; font-weight: 700;">Trilha de Auditoria e Conformidade LGPD</h4>
              <p style="font-size: 0.8rem; color: var(--text-muted);">
                Comprovações anônimas de atendimento ao Direito ao Esquecimento (Art. 18, VI da Lei 13.709/2018).
              </p>
            </div>
          </div>
          <Button
            label="Exportar Comprovantes (JSON)"
            icon="pi pi-download"
            size="small"
            severity="secondary"
            outlined
            :disabled="store.lgpdAudits.length === 0"
            @click="exportLgpdAuditsJson"
          />
        </div>

        <DataTable :value="store.lgpdAudits" :loading="store.isLgpdAuditsLoading" responsiveLayout="stack" stripedRows>
          <template #empty>
            <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
              <i class="pi pi-verified" style="font-size: 2.5rem; margin-bottom: 0.5rem; display: block; color: #6366f1;"></i>
              Nenhum pedido de exclusão LGPD registrado até o momento.
            </div>
          </template>

          <Column field="id" header="Protocolo / ID" style="min-width: 140px;">
            <template #body="{ data }">
              <Tag :value="data.id.substring(0, 8).toUpperCase()" severity="info" />
            </template>
          </Column>

          <Column field="contactHash" header="Hash Criptográfico Anônimo (SHA-256)" style="min-width: 260px;">
            <template #body="{ data }">
              <span style="font-family: monospace; font-size: 0.75rem; word-break: break-all; color: var(--text-muted);">
                {{ data.contactHash }}
              </span>
            </template>
          </Column>

          <Column field="executedAt" header="Data / Hora UTC" style="min-width: 180px;">
            <template #body="{ data }">
              <span>{{ new Date(data.executedAt).toLocaleString('pt-BR') }}</span>
            </template>
          </Column>

          <Column field="legalBasis" header="Base Legal" style="min-width: 220px;">
            <template #body="{ data }">
              <Tag :value="data.legalBasis" severity="secondary" />
            </template>
          </Column>

          <Column field="requestedBy" header="Origem da Solicitação" style="min-width: 150px;">
            <template #body="{ data }">
              <span style="font-size: 0.85rem; font-weight: 500;">{{ data.requestedBy }}</span>
            </template>
          </Column>
        </DataTable>
      </div>
    </main>
  </div>

  <!-- Dialogs -->
  <ContactFormDialog
    v-model:visible="formDialogVisible"
    :contactToEdit="editingContact"
    @save="handleSaveContact"
  />

  <LgpdConfirmDialog
    v-model:visible="lgpdDialogVisible"
    :contact="targetLgpdContact"
    @confirm="handleConfirmLgpdPurge"
  />

  <CsvImportDialog
    ref="csvDialogRef"
    v-model:visible="csvDialogVisible"
    @upload="handleCsvUpload"
  />
</template>
