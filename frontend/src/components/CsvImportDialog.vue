<script setup lang="ts">
import { ref, computed } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import ProgressBar from 'primevue/progressbar';
import type { CsvImportResultDto, CsvRowErrorDto } from '../types/contact';

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
const activeErrorFilter = ref<'all' | 'phone' | 'email' | 'name' | 'other'>('all');

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
  activeErrorFilter.value = 'all';
}

function resetForm() {
  selectedFile.value = null;
  importResult.value = null;
  activeErrorFilter.value = 'all';
}

const successRate = computed(() => {
  if (!importResult.value || importResult.value.totalRows === 0) return 0;
  return Math.round((importResult.value.importedCount / importResult.value.totalRows) * 100);
});

const filteredErrors = computed(() => {
  if (!importResult.value) return [];
  if (activeErrorFilter.value === 'all') return importResult.value.errors;
  return importResult.value.errors.filter(e => {
    const field = (e.field || '').toLowerCase();
    if (activeErrorFilter.value === 'phone') return field.includes('phone') || field.includes('tel');
    if (activeErrorFilter.value === 'email') return field.includes('email');
    if (activeErrorFilter.value === 'name') return field.includes('name') || field.includes('nome');
    return !field.includes('phone') && !field.includes('email') && !field.includes('name');
  });
});

function getFieldSeverity(field: string | null): 'danger' | 'warning' | 'info' | 'secondary' {
  if (!field) return 'secondary';
  const f = field.toLowerCase();
  if (f.includes('phone') || f.includes('tel')) return 'warning';
  if (f.includes('email')) return 'danger';
  if (f.includes('name') || f.includes('nome')) return 'info';
  return 'secondary';
}

function getFieldIcon(field: string | null): string {
  if (!field) return 'pi pi-exclamation-circle';
  const f = field.toLowerCase();
  if (f.includes('phone') || f.includes('tel')) return 'pi pi-phone';
  if (f.includes('email')) return 'pi pi-envelope';
  if (f.includes('name') || f.includes('nome')) return 'pi pi-user';
  return 'pi pi-exclamation-triangle';
}

function getSuggestion(error: CsvRowErrorDto): string {
  const reason = error.reason.toLowerCase();
  if (reason.includes('duplicado')) {
    return 'Dica: Remova linhas repetidas ou use números distintos para contatos diferentes.';
  }
  if (reason.includes('ddd')) {
    return 'Dica: Insira um DDD brasileiro válido com 2 dígitos (ex: 11 a 99).';
  }
  if (reason.includes('quantidade de dígitos') || reason.includes('dígitos')) {
    return 'Dica: Telefones no Brasil precisam ter 10 dígitos (fixo) ou 11 dígitos (celular).';
  }
  if (reason.includes('e-mail') || reason.includes('email')) {
    return 'Dica: Certifique-se de que o e-mail possui formato nome@dominio.com.';
  }
  if (reason.includes('nome')) {
    return 'Dica: O nome é obrigatório e precisa ter ao menos 2 caracteres.';
  }
  return 'Dica: Revise as colunas deste registro no seu editor de planilhas.';
}

function exportErrorsCsv() {
  if (!importResult.value || importResult.value.errors.length === 0) return;
  const header = 'Linha;CampoAfetado;MotivoRejeicao;ConteudoBrutoDaLinha;SugestaoCorrecao\n';
  const rows = importResult.value.errors.map(e => 
    `"${e.lineNumber}";"${e.field || 'Geral'}";"${e.reason.replace(/"/g, '""')}";"${e.rawLine.replace(/"/g, '""')}";"${getSuggestion(e).replace(/"/g, '""')}"`
  ).join('\n');
  const blob = new Blob([header + rows], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `relatorio_inconsistencias_neoagenda_${Date.now()}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}

defineExpose({ setResult, isUploading });
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    modal
    :style="{ width: '820px', maxWidth: '95vw' }"
    :breakpoints="{ '960px': '90vw', '640px': '98vw' }"
  >
    <template #header>
      <div style="display: flex; align-items: center; gap: 0.75rem;">
        <div style="width: 2.25rem; height: 2.25rem; border-radius: 0.6rem; background: rgba(99, 102, 241, 0.15); display: flex; align-items: center; justify-content: center; color: #6366f1;">
          <i class="pi pi-file-import" style="font-size: 1.25rem;"></i>
        </div>
        <div>
          <h3 style="font-size: 1.15rem; font-weight: 700; margin: 0; color: var(--text-main);">
            Importação Inteligente de Contatos (CSV)
          </h3>
          <p style="font-size: 0.8rem; color: var(--text-muted); margin: 0;">
            Processamento resiliente: contatos válidos são importados mesmo se houver falhas pontuais.
          </p>
        </div>
      </div>
    </template>

    <div style="display: flex; flex-direction: column; gap: 1.25rem; padding-top: 0.5rem;">
      <!-- Drag & Drop Zone -->
      <div
        v-if="!importResult"
        @click="triggerFileInput"
        @dragover.prevent="isDragging = true"
        @dragleave="isDragging = false"
        @drop.prevent="onDrop"
        class="drop-zone"
        :class="{ 'is-dragging': isDragging, 'has-file': !!selectedFile }"
      >
        <input ref="fileInput" type="file" accept=".csv,.txt" style="display: none;" @change="onFileChange" />
        <div class="drop-zone-icon">
          <i :class="selectedFile ? 'pi pi-file-check' : 'pi pi-cloud-upload'"></i>
        </div>
        <div v-if="selectedFile">
          <p class="drop-zone-title" style="color: #6366f1;">
            {{ selectedFile.name }}
          </p>
          <span class="drop-zone-desc">
            Pronto para processamento • {{ (selectedFile.size / 1024).toFixed(1) }} KB
          </span>
        </div>
        <div v-else>
          <p class="drop-zone-title">Clique para selecionar ou arraste o arquivo CSV</p>
          <span class="drop-zone-desc">Delimitadores suportados: vírgula (,) ou ponto-e-vírgula (;) • UTF-8</span>
        </div>
      </div>

      <!-- Result Panel Detalhado e Enriquecido -->
      <div v-if="importResult" class="result-container">
        <!-- Banner de Métricas -->
        <div class="metrics-grid">
          <div class="metric-card metric-total">
            <span class="metric-label"><i class="pi pi-list"></i> Total no Arquivo</span>
            <span class="metric-val">{{ importResult.totalRows }}</span>
          </div>

          <div class="metric-card metric-success">
            <span class="metric-label"><i class="pi pi-check-circle"></i> Importados com Sucesso</span>
            <span class="metric-val">{{ importResult.importedCount }}</span>
          </div>

          <div class="metric-card metric-failed">
            <span class="metric-label"><i class="pi pi-times-circle"></i> Linhas Ignoradas</span>
            <span class="metric-val">{{ importResult.failedCount }}</span>
          </div>

          <div class="metric-card metric-rate">
            <span class="metric-label"><i class="pi pi-chart-pie"></i> Taxa de Aproveitamento</span>
            <span class="metric-val">{{ successRate }}%</span>
          </div>
        </div>

        <!-- Barra de Progresso Visual de Aproveitamento -->
        <div style="display: flex; flex-direction: column; gap: 0.25rem;">
          <ProgressBar :value="successRate" :showValue="false" style="height: 6px; border-radius: 4px;" />
        </div>

        <!-- Feedback Se Tudo Deu Certo -->
        <div v-if="importResult.errors.length === 0" class="empty-errors-banner">
          <i class="pi pi-verified" style="font-size: 2rem; color: #22c55e;"></i>
          <div>
            <h4 style="font-weight: 700; color: #22c55e; margin: 0;">Arquivo 100% Consistente!</h4>
            <p style="font-size: 0.85rem; color: var(--text-muted); margin: 0;">
              Todos os {{ importResult.importedCount }} contatos foram validados, normalizados em padrão E.164 e gravados com sucesso.
            </p>
          </div>
        </div>

        <!-- Relatório de Inconsistências Granular -->
        <div v-else class="inconsistencies-section">
          <div class="inconsistencies-header">
            <div>
              <h4 style="font-weight: 700; font-size: 0.95rem; color: var(--text-main); display: flex; align-items: center; gap: 0.5rem; margin: 0;">
                <i class="pi pi-info-circle" style="color: #ef4444;"></i>
                Diagnóstico Linha a Linha das Inconsistências
              </h4>
              <p style="font-size: 0.8rem; color: var(--text-muted); margin: 0.2rem 0 0 0;">
                Veja exatamente o trecho bruto do arquivo CSV que causou a rejeição e como solucionar:
              </p>
            </div>

            <Button
              label="Baixar Relatório (CSV)"
              icon="pi pi-download"
              size="small"
              severity="secondary"
              outlined
              @click="exportErrorsCsv"
            />
          </div>

          <!-- Filtros de Erro por Categoria -->
          <div class="error-filters">
            <button
              class="filter-pill"
              :class="{ active: activeErrorFilter === 'all' }"
              @click="activeErrorFilter = 'all'"
            >
              Todos ({{ importResult.errors.length }})
            </button>
            <button
              class="filter-pill"
              :class="{ active: activeErrorFilter === 'phone' }"
              @click="activeErrorFilter = 'phone'"
            >
              <i class="pi pi-phone"></i> Telefone
            </button>
            <button
              class="filter-pill"
              :class="{ active: activeErrorFilter === 'email' }"
              @click="activeErrorFilter = 'email'"
            >
              <i class="pi pi-envelope"></i> E-mail
            </button>
            <button
              class="filter-pill"
              :class="{ active: activeErrorFilter === 'name' }"
              @click="activeErrorFilter = 'name'"
            >
              <i class="pi pi-user"></i> Nome
            </button>
          </div>

          <!-- Tabela Rica com Linha Bruta e Sugestão -->
          <DataTable
            :value="filteredErrors"
            :rows="4"
            paginator
            responsiveLayout="stack"
            size="small"
            class="inconsistency-table"
          >
            <template #empty>
              <div style="text-align: center; padding: 1.5rem; color: var(--text-muted); font-size: 0.85rem;">
                Nenhum erro encontrado para o filtro selecionado.
              </div>
            </template>

            <!-- Coluna Linha -->
            <Column field="lineNumber" header="Linha" style="width: 80px;">
              <template #body="{ data }">
                <span class="line-badge">#{{ data.lineNumber }}</span>
              </template>
            </Column>

            <!-- Coluna Campo -->
            <Column field="field" header="Campo" style="width: 110px;">
              <template #body="{ data }">
                <Tag
                  :value="data.field || 'Registro'"
                  :severity="getFieldSeverity(data.field)"
                  :icon="getFieldIcon(data.field)"
                  style="font-size: 0.75rem;"
                />
              </template>
            </Column>

            <!-- Coluna Diagnóstico e Sugestão -->
            <Column header="Motivo da Rejeição & Orientação">
              <template #body="{ data }">
                <div class="diagnostic-box">
                  <div class="reason-title">
                    <i class="pi pi-times-circle" style="color: #ef4444; font-size: 0.9rem;"></i>
                    <span>{{ data.reason }}</span>
                  </div>

                  <!-- Conteúdo Original do CSV -->
                  <div class="raw-snippet">
                    <span class="raw-label">Linha no CSV:</span>
                    <code>{{ data.rawLine }}</code>
                  </div>

                  <!-- Sugestão de Correção Prática -->
                  <div class="suggestion-row">
                    <i class="pi pi-lightbulb" style="color: #eab308; font-size: 0.85rem;"></i>
                    <span>{{ getSuggestion(data) }}</span>
                  </div>
                </div>
              </template>
            </Column>
          </DataTable>
        </div>
      </div>
    </div>

    <template #footer>
      <div style="display: flex; justify-content: space-between; align-items: center; width: 100%;">
        <Button
          v-if="importResult"
          label="Importar Outro Arquivo"
          icon="pi pi-refresh"
          severity="secondary"
          text
          @click="resetForm"
        />
        <div v-else></div>

        <div style="display: flex; gap: 0.5rem;">
          <Button
            label="Fechar"
            icon="pi pi-times"
            severity="secondary"
            text
            @click="emit('update:visible', false)"
          />
          <Button
            v-if="selectedFile && !importResult"
            label="Processar Importação"
            icon="pi pi-upload"
            :loading="isUploading"
            @click="handleUpload"
          />
        </div>
      </div>
    </template>
  </Dialog>
</template>

<style scoped>
.drop-zone {
  border: 2px dashed var(--border-color);
  background: var(--bg-card);
  border-radius: 1rem;
  padding: 2.25rem 1.5rem;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.drop-zone:hover,
.drop-zone.is-dragging {
  border-color: #6366f1;
  background: rgba(99, 102, 241, 0.05);
}

.drop-zone.has-file {
  border-color: #6366f1;
  background: rgba(99, 102, 241, 0.03);
}

.drop-zone-icon {
  width: 3.5rem;
  height: 3.5rem;
  border-radius: 1rem;
  background: rgba(99, 102, 241, 0.1);
  color: #6366f1;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.75rem;
}

.drop-zone-title {
  font-weight: 700;
  font-size: 1rem;
  color: var(--text-main);
  margin: 0;
}

.drop-zone-desc {
  font-size: 0.8rem;
  color: var(--text-muted);
}

.result-container {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.metrics-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.75rem;
}

@media (max-width: 640px) {
  .metrics-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

.metric-card {
  padding: 0.85rem 1rem;
  border-radius: 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
}

.metric-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--text-muted);
  display: flex;
  align-items: center;
  gap: 0.35rem;
}

.metric-val {
  font-size: 1.5rem;
  font-weight: 800;
  line-height: 1.2;
}

.metric-success .metric-val { color: #22c55e; }
.metric-failed .metric-val { color: #ef4444; }
.metric-rate .metric-val { color: #6366f1; }
.metric-total .metric-val { color: var(--text-main); }

.empty-errors-banner {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.25rem;
  border-radius: 0.75rem;
  background: rgba(34, 197, 94, 0.08);
  border: 1px solid rgba(34, 197, 94, 0.3);
}

.inconsistencies-section {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 0.85rem;
  padding: 1rem;
}

.inconsistencies-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.error-filters {
  display: flex;
  gap: 0.4rem;
  flex-wrap: wrap;
  padding: 0.35rem 0;
}

.filter-pill {
  border: 1px solid var(--border-color);
  background: var(--bg-primary);
  color: var(--text-muted);
  border-radius: 2rem;
  padding: 0.25rem 0.75rem;
  font-size: 0.75rem;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.35rem;
  transition: all 0.15s ease;
}

.filter-pill:hover {
  background: rgba(99, 102, 241, 0.08);
  color: #6366f1;
}

.filter-pill.active {
  background: #6366f1;
  color: white;
  border-color: #6366f1;
}

.line-badge {
  font-family: monospace;
  font-weight: 700;
  font-size: 0.8rem;
  padding: 0.15rem 0.45rem;
  border-radius: 0.35rem;
  background: rgba(148, 163, 184, 0.15);
  color: var(--text-main);
}

.diagnostic-box {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.35rem 0;
}

.reason-title {
  display: flex;
  align-items: flex-start;
  gap: 0.45rem;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-main);
  line-height: 1.3;
}

.raw-snippet {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.75rem;
  background: rgba(15, 23, 42, 0.04);
  padding: 0.25rem 0.5rem;
  border-radius: 0.35rem;
  border-left: 2px solid #ef4444;
  overflow-x: auto;
}

html.dark .raw-snippet {
  background: rgba(255, 255, 255, 0.05);
}

.raw-label {
  font-weight: 700;
  color: var(--text-muted);
  white-space: nowrap;
}

.raw-snippet code {
  font-family: monospace;
  color: var(--text-main);
  word-break: break-all;
}

.suggestion-row {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.78rem;
  color: var(--text-muted);
  font-style: italic;
}
</style>
