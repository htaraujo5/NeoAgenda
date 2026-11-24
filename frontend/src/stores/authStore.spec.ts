import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useAuthStore } from './authStore';

describe('authStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    localStorage.clear();
  });

  it('should initialize with unauthenticated state', () => {
    const store = useAuthStore();
    expect(store.isAuthenticated).toBe(false);
    expect(store.token).toBeNull();
    expect(store.user).toBeNull();
  });

  it('should set authentication data on login and clear on logout', () => {
    const store = useAuthStore();

    store.setAuth({
      accessToken: 'access_mock_123',
      refreshToken: 'refresh_mock_456',
      expiresAt: new Date().toISOString(),
      username: 'admin',
      name: 'Admin User',
      role: 'Admin'
    });

    expect(store.isAuthenticated).toBe(true);
    expect(store.token).toBe('access_mock_123');
    expect(store.user?.username).toBe('admin');

    store.logout();
    expect(store.isAuthenticated).toBe(false);
    expect(store.token).toBeNull();
    expect(store.user).toBeNull();
  });
});
