<script setup lang="ts">
import { ref } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import { useConfirm } from 'primevue/useconfirm';

import ContactFormDialog from './ContactFormDialog.vue';
import LgpdConfirmDialog from './LgpdConfirmDialog.vue';
import CsvImportDialog from './CsvImportDialog.vue';
import { useContactStore } from '../stores/contactStore';
import { useAuthStore } from '../stores/authStore';
import { contactApi, extractErrorMessage } from '../api/client';
import type { ContactDto, RecycleBinContactDto } from '../types/contact';

const store = useContactStore();
const authStore = useAuthStore();
const toast = useToast();
const confirm = useConfirm();

const activeSubTab = ref<'active' | 'recycle' | 'lgpd'>('active');

const formDialogVisible = ref(false);
const editingContact = ref<ContactDto | null>(null);

const lgpdDialogVisible = ref(false);
const targetLgpdContact = ref<ContactDto | RecycleBinContactDto | null>(null);

const csvDialogVisible = ref(false);
const csvDialogRef = ref<InstanceType<typeof CsvImportDialog> | null>(null);

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
    toast.add({ severity: 'error', summary: 'Erro', detail: extractErrorMessage(err), life: 4000 });
  }
}

function confirmSoftDelete(contact: ContactDto) {
  confirm.require({
    message: `Tem certeza que deseja mover "${contact.name}" para a Lixeira? Ele ficará retido por 30 dias.`,
    header: 'Confirmar Exclusão',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'Mover para Lixeira',
    rejectLabel: 'Cancelar',
    acceptClass: 'p-button-warning',
    accept: async () => {
      try {
        await store.softDeleteContact(contact.id);
        toast.add({ severity: 'info', summary: 'Lixeira', detail: 'Contato movido para a lixeira (retenção de 30 dias).', life: 3000 });
      } catch (err) {
        toast.add({ severity: 'error', summary: 'Erro', detail: extractErrorMessage(err), life: 4000 });
      }
    }
  });
}

async function handleRestore(contact: RecycleBinContactDto) {
  try {
    await store.restoreContact(contact.id);
    toast.add({ severity: 'success', summary: 'Restaurado', detail: `Contato ${contact.name} foi restaurado com sucesso.`, life: 3000 });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro ao Restaurar', detail: extractErrorMessage(err), life: 4000 });
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
      summary: 'Eliminação Definitiva LGPD',
      detail: `Protocolo ${result.protocol}: Dados destruídos permanentemente.`,
      life: 6000
    });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro LGPD', detail: extractErrorMessage(err), life: 5000 });
  }
}

async function handleCsvUpload(file: File) {
  try {
    if (csvDialogRef.value) csvDialogRef.value.isUploading = true;
    const result = await contactApi.importCsv(file);
    if (csvDialogRef.value) csvDialogRef.value.setResult(result);
    await store.fetchContacts();
    await store.fetchRecycleBin();
    toast.add({
      severity: result.failedCount > 0 ? 'warn' : 'success',
      summary: 'Importação Finalizada',
      detail: `${result.importedCount} importados com sucesso. ${result.failedCount} com erros.`,
      life: 5000
    });
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Falha na Importação', detail: extractErrorMessage(err), life: 5000 });
  } finally {
    if (csvDialogRef.value) csvDialogRef.value.isUploading = false;
  }
}

async function downloadExportCsv() {
  try {
    const blob = await contactApi.exportCsv();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `contatos_exportados_${Date.now()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro', detail: extractErrorMessage(err), life: 4000 });
  }
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
  <div style="display: flex; flex-direction: column; gap: 1.25rem;">
    <!-- Top Action Bar -->
    <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 1rem;">
      <!-- Sub-tabs da Agenda -->
      <div style="display: flex; gap: 0.5rem; flex-wrap: wrap;">
        <Button
          :label="'Contatos Ativos (' + store.totalCount + ')'"
          icon="pi pi-users"
          :text="activeSubTab !== 'active'"
          :severity="activeSubTab === 'active' ? 'primary' : 'secondary'"
          @click="activeSubTab = 'active'"
        />
        <Button
          :label="'Lixeira (' + store.recycleBinContacts.length + ')'"
          icon="pi pi-trash"
          :text="activeSubTab !== 'recycle'"
          :severity="activeSubTab === 'recycle' ? 'warning' : 'secondary'"
          @click="activeSubTab = 'recycle'"
        />
        <Button
          :label="'Auditoria LGPD (' + store.lgpdAudits.length + ')'"
          icon="pi pi-shield"
          :text="activeSubTab !== 'lgpd'"
          :severity="activeSubTab === 'lgpd' ? 'danger' : 'secondary'"
          @click="activeSubTab = 'lgpd'; store.fetchLgpdAudits();"
        />
      </div>

      <!-- Quick Actions -->
      <div style="display: flex; gap: 0.5rem; flex-wrap: wrap;">
        <Button label="Novo Contato" icon="pi pi-plus" @click="openCreateDialog" />
        <Button label="Importar CSV" icon="pi pi-upload" severity="secondary" outlined @click="csvDialogVisible = true" />
        <Button label="Exportar CSV" icon="pi pi-download" severity="secondary" text @click="downloadExportCsv" />
      </div>
    </div>

    <!-- Conteúdo: Contatos Ativos -->
    <div v-if="activeSubTab === 'active'" style="display: flex; flex-direction: column; gap: 1rem;">
      <div style="display: flex; gap: 0.5rem; max-width: 450px;">
        <InputText
          v-model="store.searchQuery"
          placeholder="Buscar por nome, e-mail ou telefone..."
          @keyup.enter="handleSearch"
          style="width: 100%;"
        />
        <Button icon="pi pi-search" @click="handleSearch" />
      </div>

      <DataTable
        :value="store.contacts"
        :loading="store.isLoading"
        responsiveLayout="stack"
        stripedRows
      >
        <template #empty>
          <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
            Nenhum contato encontrado.
          </div>
        </template>

        <Column field="name" header="Nome" style="min-width: 180px;">
          <template #body="{ data }">
            <span style="font-weight: 700; color: var(--text-main);">{{ data.name }}</span>
          </template>
        </Column>

        <Column field="email" header="E-mail" style="min-width: 200px;" />

        <Column field="phoneOriginal" header="Telefone (Normalizado)" style="min-width: 190px;">
          <template #body="{ data }">
            <span class="phone-badge">{{ data.phoneNormalized }}</span>
            <small style="display: block; color: var(--text-muted); font-size: 0.75rem;">{{ data.phoneOriginal }}</small>
          </template>
        </Column>

        <Column v-if="authStore.isAdmin" field="createdBy" header="Agenda / Criado por" style="min-width: 160px;">
          <template #body="{ data }">
            <Tag :value="'@' + (data.createdBy || 'system')" severity="info" />
          </template>
        </Column>

        <Column header="Ações" style="width: 110px; text-align: right;">
          <template #body="{ data }">
            <div style="display: flex; justify-content: flex-end; gap: 0.25rem;">
              <Button icon="pi pi-pencil" text rounded size="small" @click="openEditDialog(data)" title="Editar" />
              <Button icon="pi pi-trash" text rounded size="small" severity="danger" @click="confirmSoftDelete(data)" title="Mover para Lixeira" />
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Conteúdo: Lixeira -->
    <div v-if="activeSubTab === 'recycle'">
      <DataTable :value="store.recycleBinContacts" :loading="store.isRecycleBinLoading" responsiveLayout="stack" stripedRows>
        <template #empty>
          <div style="text-align: center; padding: 2rem; color: var(--text-muted);">
            A lixeira está vazia. Nenhum contato retido.
          </div>
        </template>

        <Column field="name" header="Nome" style="min-width: 180px;" />
        <Column field="email" header="E-mail" style="min-width: 200px;" />
        <Column field="phoneNormalized" header="Telefone" style="min-width: 160px;">
          <template #body="{ data }">
            <span class="phone-badge">{{ data.phoneNormalized }}</span>
          </template>
        </Column>
        <Column v-if="authStore.isAdmin" field="createdBy" header="Agenda / Criado por" style="min-width: 160px;">
          <template #body="{ data }">
            <Tag :value="'@' + (data.createdBy || 'system')" severity="info" />
          </template>
        </Column>
        <Column field="remainingDays" header="Prazo de Retenção" style="min-width: 150px;">
          <template #body="{ data }">
            <Tag :value="data.remainingDays + ' dias restantes'" severity="warning" />
          </template>
        </Column>
        <Column header="Ações" style="width: 150px; text-align: right;">
          <template #body="{ data }">
            <div style="display: flex; justify-content: flex-end; gap: 0.35rem;">
              <Button icon="pi pi-replay" text rounded size="small" severity="success" @click="handleRestore(data)" title="Restaurar Contato" />
              <Button icon="pi pi-shield" text rounded size="small" severity="danger" @click="openLgpdDialog(data)" title="Exclusão Definitiva (LGPD)" />
            </div>
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Conteúdo: Auditoria LGPD -->
    <div v-if="activeSubTab === 'lgpd'">
      <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; gap: 0.75rem;">
        <div>
          <h4 style="font-size: 1.05rem; font-weight: 700; margin: 0; color: var(--text-main);">Trilha de Auditoria LGPD</h4>
          <p style="font-size: 0.8rem; color: var(--text-muted); margin: 0.2rem 0 0 0;">
            Comprovações anônimas de atendimento ao Direito ao Esquecimento (Art. 18, VI da Lei 13.709/2018).
          </p>
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
            Nenhum pedido de exclusão LGPD registrado.
          </div>
        </template>
        <Column field="id" header="Protocolo" style="min-width: 130px;">
          <template #body="{ data }">
            <Tag :value="data.id.substring(0, 8).toUpperCase()" severity="info" />
          </template>
        </Column>
        <Column field="contactHash" header="Hash Criptográfico Anônimo (SHA-256)" style="min-width: 250px;">
          <template #body="{ data }">
            <span style="font-family: monospace; font-size: 0.75rem; word-break: break-all; color: var(--text-muted);">{{ data.contactHash }}</span>
          </template>
        </Column>
        <Column field="executedAt" header="Data / Hora UTC" style="min-width: 170px;">
          <template #body="{ data }">
            <span>{{ new Date(data.executedAt).toLocaleString('pt-BR') }}</span>
          </template>
        </Column>
        <Column field="legalBasis" header="Base Legal" style="min-width: 220px;">
          <template #body="{ data }">
            <Tag :value="data.legalBasis" severity="secondary" />
          </template>
        </Column>
      </DataTable>
    </div>

    <!-- Modais Auxiliares -->
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
  </div>
</template>
