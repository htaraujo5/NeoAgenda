<script setup lang="ts">
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import Checkbox from 'primevue/checkbox';
import type { ContactDto, RecycleBinContactDto } from '../types/contact';

const props = defineProps<{
  visible: boolean;
  contact?: ContactDto | RecycleBinContactDto | null;
  appendTo?: string;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'confirm', id: string): void;
}>();

const confirmed = ref(false);

watch(() => props.visible, (isOpen) => {
  if (isOpen) {
    confirmed.value = false;
  }
});

function handlePurge() {
  if (!confirmed.value || !props.contact) return;
  emit('confirm', props.contact.id);
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    header="Exclusão Definitiva por LGPD"
    modal
    :style="{ width: '520px' }"
    :appendTo="appendTo || 'body'"
  >
    <div style="display: flex; flex-direction: column; gap: 1.25rem; padding-top: 0.5rem;">
      <div style="background: rgba(239, 68, 68, 0.1); border: 1px solid #ef4444; border-radius: 0.75rem; padding: 1rem; display: flex; gap: 1rem; align-items: flex-start;">
        <i class="pi pi-shield" style="font-size: 2rem; color: #ef4444; margin-top: 0.2rem;"></i>
        <div>
          <h4 style="color: #ef4444; font-weight: 700; margin-bottom: 0.25rem;">Direito ao Esquecimento (Art. 18, VI LGPD)</h4>
          <p style="font-size: 0.85rem; line-height: 1.4; color: var(--text-main);">
            Esta ação eliminará <strong>permanentemente e de forma irreversível</strong> todos os dados pessoais do contato
            <strong>{{ contact?.name }}</strong>. Nenhuma recuperação será possível, nem mesmo pela equipe técnica.
          </p>
        </div>
      </div>

      <div style="font-size: 0.85rem; color: var(--text-muted); background: var(--bg-card); border: 1px solid var(--border-color); padding: 0.75rem 1rem; border-radius: 0.5rem;">
        <div style="margin-bottom: 0.35rem;"><strong>Nome:</strong> {{ contact?.name }}</div>
        <div style="margin-bottom: 0.35rem;"><strong>E-mail:</strong> {{ contact?.email }}</div>
        <div><strong>Telefone:</strong> {{ contact?.phoneOriginal }}</div>
      </div>

      <div style="display: flex; align-items: center; gap: 0.75rem; cursor: pointer;" @click="confirmed = !confirmed">
        <Checkbox v-model="confirmed" :binary="true" inputId="lgpd-consent" />
        <label for="lgpd-consent" style="font-size: 0.85rem; font-weight: 600; cursor: pointer;">
          Confirmo que o titular solicitou a eliminação definitiva dos seus dados pessoais.
        </label>
      </div>
    </div>

    <template #footer>
      <Button label="Cancelar" icon="pi pi-times" text @click="emit('update:visible', false)" />
      <Button
        label="Excluir Definitivamente"
        icon="pi pi-trash"
        severity="danger"
        :disabled="!confirmed"
        @click="handlePurge"
      />
    </template>
  </Dialog>
</template>
