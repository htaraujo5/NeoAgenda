<script setup lang="ts">
import { ref } from 'vue';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Button from 'primevue/button';
import Message from 'primevue/message';
import { useAuthStore } from '../stores/authStore';
import { authApi, extractErrorMessage } from '../api/client';

const emit = defineEmits<{
  (e: 'login-success'): void;
}>();

const authStore = useAuthStore();
const username = ref('admin');
const password = ref('Admin@123');
const errorMessage = ref('');
const isLoading = ref(false);

async function handleLogin() {
  if (!username.value.trim() || !password.value.trim()) {
    errorMessage.value = 'Por favor, informe o usuário e a senha.';
    return;
  }

  isLoading.value = true;
  errorMessage.value = '';

  try {
    const authData = await authApi.login({
      username: username.value.trim(),
      password: password.value
    });

    authStore.setAuth(authData);
    emit('login-success');
  } catch (err: unknown) {
    errorMessage.value = extractErrorMessage(err);
  } finally {
    isLoading.value = false;
  }
}
</script>

<template>
  <div class="login-wrapper">
    <div class="login-card">
      <div class="login-header">
        <div class="brand-icon">
          <i class="pi pi-shield"></i>
        </div>
        <h2>NeoAgenda</h2>
        <p>Acesso Seguro e Autenticado</p>
      </div>

      <Message v-if="errorMessage" severity="error" :closable="false" style="margin-bottom: 1.25rem;">
        {{ errorMessage }}
      </Message>

      <form @submit.prevent="handleLogin" class="login-form">
        <div class="field">
          <label for="username">Usuário</label>
          <div class="p-input-icon-left" style="width: 100%;">
            <i class="pi pi-user" />
            <InputText
              id="username"
              v-model="username"
              placeholder="Digite seu usuário"
              style="width: 100%;"
              autocomplete="username"
              :disabled="isLoading"
            />
          </div>
        </div>

        <div class="field">
          <label for="password">Senha</label>
          <div style="width: 100%;">
            <Password
              id="password"
              v-model="password"
              placeholder="Digite sua senha"
              :feedback="false"
              toggleMask
              style="width: 100%;"
              :inputStyle="{ width: '100%' }"
              autocomplete="current-password"
              :disabled="isLoading"
            />
          </div>
        </div>

        <Button
          type="submit"
          label="Entrar no Sistema"
          icon="pi pi-sign-in"
          :loading="isLoading"
          style="width: 100%; margin-top: 0.5rem; height: 2.75rem;"
        />

        <div class="login-hint">
          <span>Credenciais padrão de demonstração:</span>
          <code>admin</code> / <code>Admin@123</code>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.login-wrapper {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
  background-color: var(--bg-primary);
}

.login-card {
  width: 100%;
  max-width: 420px;
  background-color: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 1.25rem;
  padding: 2.5rem 2rem;
  box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.1), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
}

.login-header {
  text-align: center;
  margin-bottom: 2rem;
}

.login-header .brand-icon {
  margin: 0 auto 1rem auto;
}

.login-header h2 {
  font-size: 1.75rem;
  font-weight: 800;
  letter-spacing: -0.025em;
  color: var(--text-main);
}

.login-header p {
  color: var(--text-muted);
  font-size: 0.9rem;
  margin-top: 0.25rem;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.field label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--text-main);
}

.login-hint {
  margin-top: 1rem;
  padding: 0.75rem;
  border-radius: 0.5rem;
  background: rgba(99, 102, 241, 0.08);
  font-size: 0.8rem;
  text-align: center;
  color: var(--text-muted);
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.login-hint code {
  color: #6366f1;
  font-weight: 700;
}
</style>
