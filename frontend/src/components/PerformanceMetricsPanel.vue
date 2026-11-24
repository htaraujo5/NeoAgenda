<script setup lang="ts">
import { ref, onMounted } from 'vue';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Button from 'primevue/button';
import Tag from 'primevue/tag';
import { useToast } from 'primevue/usetoast';
import { metricsApi, extractErrorMessage } from '../api/client';
import type { PerformanceSummaryDto } from '../types/telemetry';

const toast = useToast();
const summary = ref<PerformanceSummaryDto | null>(null);
const isLoading = ref(false);

onMounted(async () => {
  await loadMetrics();
});

async function loadMetrics() {
  isLoading.value = true;
  try {
    summary.value = await metricsApi.getSummary();
  } catch (err) {
    toast.add({ severity: 'error', summary: 'Erro de Telemetria', detail: extractErrorMessage(err), life: 4000 });
  } finally {
    isLoading.value = false;
  }
}

function getDurationSeverity(ms: number): 'success' | 'warning' | 'danger' {
  if (ms < 100) return 'success';
  if (ms < 300) return 'warning';
  return 'danger';
}

function getStatusSeverity(status: number): 'success' | 'warning' | 'danger' | 'info' {
  if (status >= 200 && status < 300) return 'success';
  if (status >= 300 && status < 400) return 'info';
  if (status >= 400 && status < 500) return 'warning';
  return 'danger';
}
</script>

<template>
  <div style="display: flex; flex-direction: column; gap: 1.5rem;">
    <!-- Header e Atualização -->
    <div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 0.75rem;">
      <div>
        <h4 style="font-size: 1.1rem; font-weight: 700; margin: 0; color: var(--text-main); display: flex; align-items: center; gap: 0.5rem;">
          <i class="pi pi-chart-line" style="color: #6366f1;"></i>
          Telemetria e Acompanhamento de Desempenho
        </h4>
        <p style="font-size: 0.8rem; color: var(--text-muted); margin: 0.2rem 0 0 0;">
          Métricas em tempo real de latência, taxa de erro e throughput da API backend.
        </p>
      </div>

      <Button
        label="Atualizar Métricas"
        icon="pi pi-refresh"
        size="small"
        severity="secondary"
        outlined
        :loading="isLoading"
        @click="loadMetrics"
      />
    </div>

    <!-- Cards de Métricas Principais -->
    <div v-if="summary" class="metrics-grid">
      <div class="metric-card">
        <span class="metric-title"><i class="pi pi-send"></i> Total de Requisições</span>
        <span class="metric-value">{{ summary.totalRequests }}</span>
        <span class="metric-sub">Amostra recente de tráfego</span>
      </div>

      <div class="metric-card">
        <span class="metric-title"><i class="pi pi-bolt"></i> Tempo Médio (Avg)</span>
        <span class="metric-value" style="color: #6366f1;">{{ summary.averageDurationMs }} ms</span>
        <span class="metric-sub">Latência média por chamada</span>
      </div>

      <div class="metric-card">
        <span class="metric-title"><i class="pi pi-clock"></i> Percentil P95</span>
        <span class="metric-value" :style="{ color: summary.p95DurationMs > 250 ? '#f59e0b' : '#22c55e' }">
          {{ summary.p95DurationMs }} ms
        </span>
        <span class="metric-sub">95% das chamadas abaixo deste tempo</span>
      </div>

      <div class="metric-card">
        <span class="metric-title"><i class="pi pi-shield"></i> Taxa de Erro HTTP</span>
        <span class="metric-value" :style="{ color: summary.errorRatePercentage > 5 ? '#ef4444' : '#22c55e' }">
          {{ summary.errorRatePercentage }}%
        </span>
        <span class="metric-sub">{{ summary.errorCount }} erros 4xx/5xx registrados</span>
      </div>
    </div>

    <!-- Endpoints e Distribuição de Status Codes -->
    <div v-if="summary" style="display: grid; grid-template-columns: 2fr 1fr; gap: 1.25rem;">
      <!-- Endpoints Mais Solicitados / Mais Lentos -->
      <div class="detail-card">
        <h5 style="font-weight: 700; margin-bottom: 0.75rem; color: var(--text-main); display: flex; align-items: center; gap: 0.5rem;">
          <i class="pi pi-sort-amount-down" style="color: #6366f1;"></i>
          Endpoints e Gargalos de Latência
        </h5>

        <DataTable :value="summary.topSlowestEndpoints" size="small" responsiveLayout="stack">
          <template #empty>
            <div style="text-align: center; padding: 1.5rem; color: var(--text-muted);">
              Nenhum dado de chamada coletado ainda.
            </div>
          </template>

          <Column field="method" header="Método" style="width: 80px;">
            <template #body="{ data }">
              <Tag :value="data.method" severity="info" />
            </template>
          </Column>

          <Column field="path" header="Endpoint" style="min-width: 180px;">
            <template #body="{ data }">
              <code style="font-size: 0.8rem; color: var(--text-main);">{{ data.path }}</code>
            </template>
          </Column>

          <Column field="totalCalls" header="Chamadas" style="width: 90px;" />

          <Column field="averageDurationMs" header="Média" style="width: 100px;">
            <template #body="{ data }">
              <Tag
                :value="data.averageDurationMs + ' ms'"
                :severity="getDurationSeverity(data.averageDurationMs)"
              />
            </template>
          </Column>

          <Column field="maxDurationMs" header="Máximo" style="width: 100px;">
            <template #body="{ data }">
              <span style="font-size: 0.85rem; color: var(--text-muted);">{{ data.maxDurationMs }} ms</span>
            </template>
          </Column>
        </DataTable>
      </div>

      <!-- Distribuição de Status Codes -->
      <div class="detail-card">
        <h5 style="font-weight: 700; margin-bottom: 0.75rem; color: var(--text-main); display: flex; align-items: center; gap: 0.5rem;">
          <i class="pi pi-check-circle" style="color: #22c55e;"></i>
          Respostas HTTP
        </h5>

        <div v-if="summary.statusCodeDistribution.length === 0" style="text-align: center; padding: 1.5rem; color: var(--text-muted);">
          Sem registros de status.
        </div>

        <div v-else style="display: flex; flex-direction: column; gap: 0.75rem;">
          <div
            v-for="st in summary.statusCodeDistribution"
            :key="st.statusCode"
            style="display: flex; justify-content: space-between; align-items: center; padding: 0.5rem 0.75rem; background: var(--bg-primary); border-radius: 0.5rem;"
          >
            <div style="display: flex; align-items: center; gap: 0.5rem;">
              <Tag :value="'HTTP ' + st.statusCode" :severity="getStatusSeverity(st.statusCode)" />
              <span style="font-size: 0.8rem; color: var(--text-muted);">
                {{ st.statusCode < 400 ? 'Sucesso' : 'Erro de Cliente/Servidor' }}
              </span>
            </div>
            <strong style="font-size: 0.95rem; color: var(--text-main);">{{ st.count }}</strong>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
}

@media (max-width: 900px) {
  .metrics-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

.metric-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 0.85rem;
  padding: 1.25rem 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.metric-title {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-muted);
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.metric-value {
  font-size: 1.6rem;
  font-weight: 800;
  line-height: 1.2;
}

.metric-sub {
  font-size: 0.75rem;
  color: var(--text-muted);
}

.detail-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 0.85rem;
  padding: 1.25rem;
}

@media (max-width: 800px) {
  div[style*="grid-template-columns: 2fr 1fr"] {
    grid-template-columns: 1fr !important;
  }
}
</style>
