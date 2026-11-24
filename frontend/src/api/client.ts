import axios, { AxiosError } from 'axios';
import type {
  ContactDto,
  CreateContactDto,
  UpdateContactDto,
  PagedResultDto,
  RecycleBinContactDto,
  SoftDeleteResultDto,
  RestoreContactResultDto,
  LgpdPurgeResultDto,
  CsvImportResultDto
} from '../types/contact';

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

export const contactApi = {
  async getContacts(params?: {
    search?: string;
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortDesc?: boolean;
  }): Promise<PagedResultDto<ContactDto>> {
    const response = await api.get<PagedResultDto<ContactDto>>('/contacts', { params });
    return response.data;
  },

  async getContactById(id: string): Promise<ContactDto> {
    const response = await api.get<ContactDto>(`/contacts/${id}`);
    return response.data;
  },

  async createContact(dto: CreateContactDto): Promise<ContactDto> {
    const response = await api.post<ContactDto>('/contacts', dto);
    return response.data;
  },

  async updateContact(id: string, dto: UpdateContactDto): Promise<ContactDto> {
    const response = await api.put<ContactDto>(`/contacts/${id}`, dto);
    return response.data;
  },

  async softDeleteContact(id: string): Promise<SoftDeleteResultDto> {
    const response = await api.delete<SoftDeleteResultDto>(`/contacts/${id}`);
    return response.data;
  },

  async restoreContact(id: string): Promise<RestoreContactResultDto> {
    const response = await api.post<RestoreContactResultDto>(`/contacts/${id}/restore`);
    return response.data;
  },

  async purgeLgpd(id: string, requestedBy?: string): Promise<LgpdPurgeResultDto> {
    const response = await api.delete<LgpdPurgeResultDto>(`/contacts/${id}/lgpd`, {
      params: { requestedBy: requestedBy || 'usuario_interface_web' }
    });
    return response.data;
  },

  async getRecycleBin(): Promise<RecycleBinContactDto[]> {
    const response = await api.get<RecycleBinContactDto[]>('/contacts/recycle-bin');
    return response.data;
  },

  async importCsv(file: File): Promise<CsvImportResultDto> {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<CsvImportResultDto>('/contacts/import-csv', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    });
    return response.data;
  },

  getExportCsvUrl(): string {
    return '/api/contacts/export-csv';
  }
};

export function extractErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const axiosErr = err as AxiosError<{ detail?: string; title?: string }>;
    if (axiosErr.response?.data?.detail) {
      return axiosErr.response.data.detail;
    }
    if (axiosErr.response?.data?.title) {
      return axiosErr.response.data.title;
    }
    if (axiosErr.message) {
      return axiosErr.message;
    }
  }
  return 'Ocorreu um erro inesperado ao se comunicar com o servidor.';
}
