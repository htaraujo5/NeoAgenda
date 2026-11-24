<script setup lang="ts">
import { computed } from 'vue';
import Tag from 'primevue/tag';
import { useAuthStore } from '../stores/authStore';

const authStore = useAuthStore();

const userInitials = computed(() => {
  const name = authStore.user?.name || authStore.user?.username || 'U';
  return name.substring(0, 2).toUpperCase();
});
</script>

<template>
  <div class="profile-container">
    <!-- Header do Perfil -->
    <div class="profile-card profile-header-card">
      <div class="avatar-circle">
        {{ userInitials }}
      </div>
      <div class="user-info">
        <div style="display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap;">
          <h2 style="font-size: 1.5rem; font-weight: 800; color: var(--text-main); margin: 0;">
            {{ authStore.user?.name || 'Usuário NeoAgenda' }}
          </h2>
          <Tag
            :value="authStore.isAdmin ? 'Administrador' : 'Usuário Padrão'"
            :severity="authStore.isAdmin ? 'danger' : 'info'"
            :icon="authStore.isAdmin ? 'pi pi-shield' : 'pi pi-user'"
          />
        </div>
        <span style="font-size: 0.9rem; color: var(--text-muted); margin-top: 0.25rem; display: block;">
          Login de acesso: <strong>@{{ authStore.user?.username }}</strong>
        </span>
      </div>
    </div>

    <!-- Seção de Permissões e Escopo -->
    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 1.25rem;">
      <div class="profile-card">
        <h4 style="font-size: 1.05rem; font-weight: 700; color: var(--text-main); margin-bottom: 0.75rem; display: flex; align-items: center; gap: 0.5rem;">
          <i class="pi pi-key" style="color: #6366f1;"></i>
          Perfil de Acesso & Permissões
        </h4>
        <p style="font-size: 0.85rem; color: var(--text-muted); line-height: 1.5; margin-bottom: 1rem;">
          {{ authStore.isAdmin 
            ? 'Você possui perfil de Administrador com acesso irrestrito a contatos, importação/exportação, auditoria LGPD, gestão de usuários e métricas de desempenho do sistema.' 
            : 'Você possui perfil de Usuário Padrão. Seu acesso é focado na gestão diária da agenda de contatos (cadastro, busca e edição).' 
          }}
        </p>

        <div class="permission-list">
          <div class="permission-item">
            <i class="pi pi-check-circle" style="color: #22c55e;"></i>
            <span>Gerenciar Contatos (Criar, Editar, Consultar)</span>
          </div>
          <div class="permission-item">
            <i class="pi pi-check-circle" style="color: #22c55e;"></i>
            <span>Importação e Exportação Resiliente de CSV</span>
          </div>
          <div class="permission-item">
            <i :class="authStore.isAdmin ? 'pi pi-check-circle' : 'pi pi-times-circle'" :style="{ color: authStore.isAdmin ? '#22c55e' : '#94a3b8' }"></i>
            <span :style="{ color: authStore.isAdmin ? 'var(--text-main)' : 'var(--text-muted)' }">
              Gestão de Usuários e Revogação de Acessos
            </span>
          </div>
          <div class="permission-item">
            <i :class="authStore.isAdmin ? 'pi pi-check-circle' : 'pi pi-times-circle'" :style="{ color: authStore.isAdmin ? '#22c55e' : '#94a3b8' }"></i>
            <span :style="{ color: authStore.isAdmin ? 'var(--text-main)' : 'var(--text-muted)' }">
              Telemetria e Acompanhamento de Desempenho (APM)
            </span>
          </div>
        </div>
      </div>

      <!-- Métricas do Usuário -->
      <div class="profile-card">
        <h4 style="font-size: 1.05rem; font-weight: 700; color: var(--text-main); margin-bottom: 0.75rem; display: flex; align-items: center; gap: 0.5rem;">
          <i class="pi pi-shield" style="color: #22c55e;"></i>
          Sessão & Segurança
        </h4>
        <div style="display: flex; flex-direction: column; gap: 0.75rem;">
          <div class="security-row">
            <span class="security-label">Autenticação:</span>
            <Tag value="JWT Bearer Token (60 min)" severity="success" />
          </div>
          <div class="security-row">
            <span class="security-label">Renovação de Sessão:</span>
            <Tag value="Refresh Token Ativo (7 dias)" severity="info" />
          </div>
          <div class="security-row">
            <span class="security-label">Criptografia de Senha:</span>
            <span>SHA-256 com Fixed-Time Equals</span>
          </div>
          <div class="security-row">
            <span class="security-label">Conformidade Legal:</span>
            <span>LGPD (Lei 13.709/2018)</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.profile-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.profile-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: 1rem;
  padding: 1.5rem;
}

.profile-header-card {
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.avatar-circle {
  width: 4.5rem;
  height: 4.5rem;
  border-radius: 50%;
  background: linear-gradient(135deg, #6366f1, #4f46e5);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.75rem;
  font-weight: 800;
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
}

.permission-list {
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.permission-item {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  font-size: 0.85rem;
  font-weight: 500;
}

.security-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem 0.75rem;
  border-radius: 0.5rem;
  background: var(--bg-primary);
  font-size: 0.825rem;
}

.security-label {
  color: var(--text-muted);
  font-weight: 600;
}
</style>
