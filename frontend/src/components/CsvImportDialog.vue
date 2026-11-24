<script setup lang="ts">
import { ref } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import type { CsvImportResultDto } from '../types/contact';

defineProps<{
  visible: boolean;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'upload', file: File): void;
}>();

const selectedFile = ref<File | null>(null);
const fileInput = ref<HTMLInputElement | null>(null);
const isDragging = ref(false);

const importResult = ref<CsvImportResultDto | null>(null);
const isUploading = ref(false);

function triggerFileInput() {
  fileInput.value?.click();
}

function onFileChange(event: Event) {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files.length > 0) {
    selectedFile.value = target.files[0];
    importResult.value = null;
  }
}

function onDrop(event: DragEvent) {
  isDragging.value = false;
  if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
    const file = event.dataTransfer.files[0];
    if (file.name.endsWith('.csv') || file.name.endsWith('.txt')) {
      selectedFile.value = file;
      importResult.value = null;
    }
  }
}

function handleUpload() {
  if (!selectedFile.value) return;
  emit('upload', selectedFile.value);
}

function setResult(result: CsvImportResultDto) {
  importResult.value = result;
  selectedFile.value = null;
}

function exportErrorsCsv() {
  if (!importResult.value || importResult.value.errors.length === 0) return;
  const header = 'Linha;ConteudoBruto;Campo;MotivoRejeicao\n';
  const rows = importResult.value.errors.map(e => 
    `"${e.lineNumber}";"${e.rawLine.replace(/"/g, '""')}";"${e.field || ''}";"${e.reason.replace(/"/g, '""')}"`
  ).join('\n');
  const blob = new Blob([header + rows], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `erros_importacao_${Date.now()}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}

defineExpose({ setResult, isUploading });
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    header="Importação de Contatos via CSV"
    modal
    :style="{ width: '650px' }"
  >
    <div style="display: flex; flex-direction: column; gap: 1.25rem;">
      <p style="font-size: 0.85rem; color: var(--text-muted);">
        O arquivo CSV pode usar delimitador de vírgula (<code>,</code>) ou ponto-e-vírgula (<code>;</code>).
        Cabeçalhos aceitos: <code>nome</code>, <code>email</code> e <code>telefone</code> (ou <code>name</code>, <code>phone</code>).
      </p>

      <!-- Drag & Drop Zone -->
      <div
        @click="triggerFileInput"
        @dragover.prevent="isDragging = true"
        @dragleave="isDragging = false"
        @drop.prevent="onDrop"
        :style="{
          border: '2px dashed ' + (isDragging ? '#6366f1' : 'var(--border-color)'),
          background: isDragging ? 'rgba(99, 102, 241, 0.05)' : 'var(--bg-card)',
          borderRadius: '0.75rem',
          padding: '2rem 1.5rem',
          textAlign: 'center',
          cursor: 'pointer',
          transition: 'all 0.2s'
        }"
      >
        <input ref="fileInput" type="file" accept=".csv,.txt" style="display: none;" @change="onFileChange" />
        <i class="pi pi-file-excel" style="font-size: 2.5rem; color: #6366f1; margin-bottom: 0.75rem; display: block;"></i>
        <div v-if="selectedFile" style="font-weight: 600; color: var(--text-main);">
          Arquivo selecionado: <span style="color: #6366f1;">{{ selectedFile.name }}</span> ({{ (selectedFile.size / 1024).toFixed(1) }} KB)
        </div>
        <div v-else>
          <p style="font-weight: 600; margin-bottom: 0.25rem;">Clique para selecionar ou arraste o arquivo CSV aqui</p>
          <small style="color: var(--text-muted);">Tamanho máximo suportado: 10 MB</small>
        </div>
      </div>

      <!-- Result Panel -->
      <div v-if="importResult" style="margin-top: 0.5rem; display: flex; flex-direction: column; gap: 1rem;">
        <div style="display: flex; gap: 1rem; justify-content: space-around;">
          <div style="flex: 1; padding: 1rem; border-radius: 0.5rem; background: rgba(34, 197, 94, 0.1); border: 1px solid #22c55e; text-align: center;">
            <div style="font-size: 1.5rem; font-weight: 700; color: #22c55e;">{{ importResult.importedCount }}</div>
            <div style="font-size: 0.8rem; font-weight: 600; color: #22c55e;">Importados com Sucesso</div>
          </div>
          <div style="flex: 1; padding: 1rem; border-radius: 0.5rem; background: rgba(239, 68, 68, 0.1); border: 1px solid #ef4444; text-align: center;">
            <div style="font-size: 1.5rem; font-weight: 700; color: #ef4444;">{{ importResult.failedCount }}</div>
            <div style="font-size: 0.8rem; font-weight: 600; color: #ef4444;">Linhas com Erros Ignoradas</div>
          </div>
        </div>

        <!-- Tabela de Inconsistências -->
        <div v-if="importResult.errors.length > 0">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;">
            <h5 style="font-weight: 600; font-size: 0.9rem;">Relatório de Inconsistências</h5>
            <Button label="Exportar Erros (CSV)" icon="pi pi-download" size="small" text @click="exportErrorsCsv" />
          </div>

          <DataTable :value="importResult.errors" :rows="5" paginator responsiveLayout="scroll" size="small">
            <Column field="lineNumber" header="Linha" style="width: 70px;">
              <template #body="{ data }">
                <Tag :value="'#' + data.lineNumber" severity="secondary" />
              </template>
            </Column>
            <Column field="field" header="Campo" style="width: 90px;">
              <template #body="{ data }">
                <Tag :value="data.field || 'Geral'" severity="warning" />
              </template>
            </Column>
            <Column field="reason" header="Motivo da Rejeição" />
          </DataTable>
        </div>
      </div>
    </div>

    <template #footer>
      <Button label="Fechar" icon="pi pi-times" text @click="emit('update:visible', false)" />
      <Button
        v-if="selectedFile"
        label="Processar Importação"
        icon="pi pi-upload"
        :loading="isUploading"
        @click="handleUpload"
      />
    </template>
  </Dialog>
</template>
