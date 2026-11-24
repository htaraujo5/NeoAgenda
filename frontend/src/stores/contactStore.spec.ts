import { describe, it, expect, beforeEach, vi } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useContactStore } from './contactStore';
import { contactApi } from '../api/client';
import type { ContactDto, PagedResultDto } from '../types/contact';

describe('contactStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.restoreAllMocks();
  });

  it('should fetch contacts and update state', async () => {
    const mockContacts: ContactDto[] = [
      {
        id: '1',
        name: 'Ana Silva',
        email: 'ana@teste.com',
        phoneOriginal: '(11) 98765-4321',
        phoneNormalized: '+5511987654321',
        createdAt: '2026-11-20T00:00:00Z',
        updatedAt: null
      }
    ];

    const mockResult: PagedResultDto<ContactDto> = {
      items: mockContacts,
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1
    };

    vi.spyOn(contactApi, 'getContacts').mockResolvedValue(mockResult);

    const store = useContactStore();
    await store.fetchContacts();

    expect(store.contacts).toHaveLength(1);
    expect(store.contacts[0].name).toBe('Ana Silva');
    expect(store.totalCount).toBe(1);
    expect(store.isLoading).toBe(false);
  });

  it('should handle soft delete and refresh store lists', async () => {
    vi.spyOn(contactApi, 'softDeleteContact').mockResolvedValue({
      id: '1',
      message: 'Na lixeira',
      deletedAt: '2026-11-23T00:00:00Z',
      retentionExpiresAt: '2026-12-23T00:00:00Z'
    });
    vi.spyOn(contactApi, 'getContacts').mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0
    });
    vi.spyOn(contactApi, 'getRecycleBin').mockResolvedValue([]);

    const store = useContactStore();
    const result = await store.softDeleteContact('1');

    expect(result.id).toBe('1');
    expect(contactApi.softDeleteContact).toHaveBeenCalledWith('1');
    expect(contactApi.getContacts).toHaveBeenCalled();
    expect(contactApi.getRecycleBin).toHaveBeenCalled();
  });
});
