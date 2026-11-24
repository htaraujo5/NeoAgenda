import { defineStore } from 'pinia';
import { ref } from 'vue';
import { contactApi, extractErrorMessage } from '../api/client';
import type { ContactDto, RecycleBinContactDto, CreateContactDto, UpdateContactDto, CsvImportResultDto, LgpdAuditLogDto } from '../types/contact';

export const useContactStore = defineStore('contact', () => {
  // State
  const contacts = ref<ContactDto[]>([]);
  const recycleBinContacts = ref<RecycleBinContactDto[]>([]);
  const lgpdAudits = ref<LgpdAuditLogDto[]>([]);
  const totalCount = ref(0);
  const totalPages = ref(1);
  const currentPage = ref(1);
  const pageSize = ref(10);
  const searchQuery = ref('');
  const sortBy = ref('name');
  const sortDesc = ref(false);

  const isLoading = ref(false);
  const isRecycleBinLoading = ref(false);
  const isLgpdAuditsLoading = ref(false);
  const errorMessage = ref<string | null>(null);

  // Actions
  async function fetchContacts() {
    isLoading.value = true;
    errorMessage.value = null;
    try {
      const result = await contactApi.getContacts({
        search: searchQuery.value || undefined,
        page: currentPage.value,
        pageSize: pageSize.value,
        sortBy: sortBy.value,
        sortDesc: sortDesc.value
      });
      contacts.value = result.items;
      totalCount.value = result.totalCount;
      totalPages.value = result.totalPages;
    } catch (err) {
      errorMessage.value = extractErrorMessage(err);
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchRecycleBin() {
    isRecycleBinLoading.value = true;
    try {
      recycleBinContacts.value = await contactApi.getRecycleBin();
    } catch (err) {
      errorMessage.value = extractErrorMessage(err);
      throw err;
    } finally {
      isRecycleBinLoading.value = false;
    }
  }

  async function fetchLgpdAudits() {
    isLgpdAuditsLoading.value = true;
    try {
      lgpdAudits.value = await contactApi.getLgpdAudits();
    } catch (err) {
      errorMessage.value = extractErrorMessage(err);
      throw err;
    } finally {
      isLgpdAuditsLoading.value = false;
    }
  }

  async function createContact(dto: CreateContactDto) {
    const created = await contactApi.createContact(dto);
    await fetchContacts();
    return created;
  }

  async function updateContact(id: string, dto: UpdateContactDto) {
    const updated = await contactApi.updateContact(id, dto);
    await fetchContacts();
    return updated;
  }

  async function softDeleteContact(id: string) {
    const result = await contactApi.softDeleteContact(id);
    await fetchContacts();
    await fetchRecycleBin();
    return result;
  }

  async function restoreContact(id: string) {
    const result = await contactApi.restoreContact(id);
    await fetchRecycleBin();
    await fetchContacts();
    return result;
  }

  async function purgeLgpd(id: string, requestedBy?: string) {
    const result = await contactApi.purgeLgpd(id, requestedBy);
    await fetchContacts();
    await fetchRecycleBin();
    await fetchLgpdAudits();
    return result;
  }

  async function importCsv(file: File): Promise<CsvImportResultDto> {
    const result = await contactApi.importCsv(file);
    await fetchContacts();
    return result;
  }

  return {
    contacts,
    recycleBinContacts,
    lgpdAudits,
    totalCount,
    totalPages,
    currentPage,
    pageSize,
    searchQuery,
    sortBy,
    sortDesc,
    isLoading,
    isRecycleBinLoading,
    isLgpdAuditsLoading,
    errorMessage,
    fetchContacts,
    fetchRecycleBin,
    fetchLgpdAudits,
    createContact,
    updateContact,
    softDeleteContact,
    restoreContact,
    purgeLgpd,
    importCsv
  };
});
