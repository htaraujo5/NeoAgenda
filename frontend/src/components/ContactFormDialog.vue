<script setup lang="ts">
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import InputText from 'primevue/inputtext';
import Button from 'primevue/button';
import Message from 'primevue/message';
import type { ContactDto } from '../types/contact';

const props = defineProps<{
  visible: boolean;
  contactToEdit?: ContactDto | null;
}>();

const emit = defineEmits<{
  (e: 'update:visible', value: boolean): void;
  (e: 'save', payload: { name: string; email: string; phone: string }): void;
}>();

const name = ref('');
const email = ref('');
const phone = ref('');
const errors = ref<{ name?: string; email?: string; phone?: string }>({});

watch(() => props.visible, (isOpen) => {
  if (isOpen) {
    if (props.contactToEdit) {
      name.value = props.contactToEdit.name;
      email.value = props.contactToEdit.email;
      phone.value = props.contactToEdit.phoneOriginal;
    } else {
      name.value = '';
      email.value = '';
      phone.value = '';
    }
    errors.value = {};
  }
});

function applyPhoneMask(event: Event) {
  const input = event.target as HTMLInputElement;
  let digits = input.value.replace(/\D/g, '');

  if (digits.length <= 10) {
    // Formato Fixo: (XX) XXXX-XXXX
    digits = digits.replace(/^(\d{2})(\d{4})(\d{0,4})/, '($1) $2-$3');
  } else {
    // Formato Celular: (XX) XXXXX-XXXX
    digits = digits.slice(0, 11).replace(/^(\d{2})(\d{5})(\d{0,4})/, '($1) $2-$3');
  }

  phone.value = digits;
}

function validate(): boolean {
  errors.value = {};
  if (!name.value.trim()) {
    errors.value.name = 'O nome do contato é obrigatório.';
  }
  if (!email.value.trim()) {
    errors.value.email = 'O e-mail é obrigatório.';
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim())) {
    errors.value.email = 'Informe um endereço de e-mail válido.';
  }
  const digits = phone.value.replace(/\D/g, '');
  if (!digits) {
    errors.value.phone = 'O telefone é obrigatório.';
  } else if (digits.length < 10) {
    errors.value.phone = 'O telefone deve conter pelo menos DDD + 8 dígitos.';
  }
  return Object.keys(errors.value).length === 0;
}

function handleSave() {
  if (!validate()) return;
  emit('save', {
    name: name.value.trim(),
    email: email.value.trim(),
    phone: phone.value.trim()
  });
}

function handleClose() {
  emit('update:visible', false);
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="emit('update:visible', $event)"
    :header="contactToEdit ? 'Editar Contato' : 'Novo Contato'"
    modal
    :style="{ width: '450px' }"
  >
    <div class="p-fluid" style="display: flex; flex-direction: column; gap: 1.25rem; padding-top: 0.5rem;">
      <div>
        <label for="name" style="font-weight: 600; font-size: 0.9rem; margin-bottom: 0.35rem; display: block;">Nome Completo</label>
        <InputText
          id="name"
          v-model="name"
          placeholder="Ex: Maria Silva"
          :invalid="!!errors.name"
          style="width: 100%;"
        />
        <Message v-if="errors.name" severity="error" size="small" variant="simple" style="margin-top: 0.25rem;">{{ errors.name }}</Message>
      </div>

      <div>
        <label for="email" style="font-weight: 600; font-size: 0.9rem; margin-bottom: 0.35rem; display: block;">E-mail</label>
        <InputText
          id="email"
          v-model="email"
          type="email"
          placeholder="exemplo@dominio.com"
          :invalid="!!errors.email"
          style="width: 100%;"
        />
        <Message v-if="errors.email" severity="error" size="small" variant="simple" style="margin-top: 0.25rem;">{{ errors.email }}</Message>
      </div>

      <div>
        <label for="phone" style="font-weight: 600; font-size: 0.9rem; margin-bottom: 0.35rem; display: block;">Telefone / WhatsApp</label>
        <InputText
          id="phone"
          :value="phone"
          @input="applyPhoneMask"
          placeholder="(11) 98765-4321"
          :invalid="!!errors.phone"
          style="width: 100%;"
        />
        <Message v-if="errors.phone" severity="error" size="small" variant="simple" style="margin-top: 0.25rem;">{{ errors.phone }}</Message>
        <small style="color: var(--text-muted); font-size: 0.75rem; display: block; margin-top: 0.25rem;">
          Aceita fixo ou celular com DDD. A formatação é normalizada automaticamente.
        </small>
      </div>
    </div>

    <template #footer>
      <Button label="Cancelar" icon="pi pi-times" text @click="handleClose" />
      <Button :label="contactToEdit ? 'Salvar Alterações' : 'Cadastrar'" icon="pi pi-check" @click="handleSave" autofocus />
    </template>
  </Dialog>
</template>
