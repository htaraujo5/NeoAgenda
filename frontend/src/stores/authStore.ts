import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { AuthResponseDto } from '../types/auth';

const TOKEN_KEY = 'neoagenda_access_token';
const REFRESH_TOKEN_KEY = 'neoagenda_refresh_token';
const USER_KEY = 'neoagenda_user';

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem(TOKEN_KEY));
  const refreshToken = ref<string | null>(localStorage.getItem(REFRESH_TOKEN_KEY));
  const user = ref<{ username: string; name: string; role: string } | null>(
    localStorage.getItem(USER_KEY) ? JSON.parse(localStorage.getItem(USER_KEY)!) : null
  );

  const isAuthenticated = computed(() => !!token.value);
  const isAdmin = computed(() => (user.value?.role || '').toLowerCase() === 'admin' || (user.value?.role || '').toLowerCase() === 'administrador');
  const isUser = computed(() => !isAdmin.value);

  function setAuth(authData: AuthResponseDto) {
    token.value = authData.accessToken;
    refreshToken.value = authData.refreshToken;
    user.value = {
      username: authData.username,
      name: authData.name,
      role: authData.role
    };

    localStorage.setItem(TOKEN_KEY, authData.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, authData.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(user.value));
  }

  function updateToken(newToken: string, newRefreshToken?: string) {
    token.value = newToken;
    localStorage.setItem(TOKEN_KEY, newToken);
    if (newRefreshToken) {
      refreshToken.value = newRefreshToken;
      localStorage.setItem(REFRESH_TOKEN_KEY, newRefreshToken);
    }
  }

  function logout() {
    token.value = null;
    refreshToken.value = null;
    user.value = null;

    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  return {
    token,
    refreshToken,
    user,
    isAuthenticated,
    isAdmin,
    isUser,
    setAuth,
    updateToken,
    logout
  };
});
