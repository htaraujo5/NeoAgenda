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
  LgpdAuditLogDto,
  CsvImportResultDto
} from '../types/contact';
import type {
  LoginRequestDto,
  RefreshTokenRequestDto,
  AuthResponseDto
} from '../types/auth';

export const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Interceptor de Requisição: Injeta Bearer Token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('neoagenda_access_token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor de Resposta: Tratamento de 401 com Renovação Automática (Refresh Token)
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean };

    // Se for 401 e não for rota de login/refresh e ainda não tiver tentado retry
    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.url?.includes('/auth/login') &&
      !originalRequest.url?.includes('/auth/refresh-token')
    ) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = localStorage.getItem('neoagenda_refresh_token');
      if (!refreshToken) {
        localStorage.removeItem('neoagenda_access_token');
        localStorage.removeItem('neoagenda_refresh_token');
        localStorage.removeItem('neoagenda_user');
        window.dispatchEvent(new CustomEvent('auth:expired'));
        isRefreshing = false;
        return Promise.reject(error);
      }

      try {
        const response = await axios.post<AuthResponseDto>('/api/auth/refresh-token', {
          refreshToken
        });

        const newAuth = response.data;
        const newAccessToken = newAuth.accessToken || (newAuth as any).AccessToken;
        const newRefreshToken = newAuth.refreshToken || (newAuth as any).RefreshToken;

        localStorage.setItem('neoagenda_access_token', newAccessToken);
        localStorage.setItem('neoagenda_refresh_token', newRefreshToken);

        api.defaults.headers.common['Authorization'] = `Bearer ${newAccessToken}`;
        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
        }

        processQueue(null, newAccessToken);
        return api(originalRequest);
      } catch (refreshErr) {
        processQueue(refreshErr, null);
        localStorage.removeItem('neoagenda_access_token');
        localStorage.removeItem('neoagenda_refresh_token');
        localStorage.removeItem('neoagenda_user');
        window.dispatchEvent(new CustomEvent('auth:expired'));
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export const authApi = {
  async login(dto: LoginRequestDto): Promise<AuthResponseDto> {
    const response = await api.post<AuthResponseDto>('/auth/login', dto);
    return response.data;
  },

  async refreshToken(dto: RefreshTokenRequestDto): Promise<AuthResponseDto> {
    const response = await api.post<AuthResponseDto>('/auth/refresh-token', dto);
    return response.data;
  },

  async logout(): Promise<void> {
    try {
      await api.post('/auth/logout');
    } finally {
      localStorage.removeItem('neoagenda_access_token');
      localStorage.removeItem('neoagenda_refresh_token');
      localStorage.removeItem('neoagenda_user');
    }
  }
};

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

  async getLgpdAudits(): Promise<LgpdAuditLogDto[]> {
    const response = await api.get<LgpdAuditLogDto[]>('/contacts/lgpd-audits');
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

  async exportCsv(): Promise<Blob> {
    const response = await api.get('/contacts/export-csv', {
      responseType: 'blob'
    });
    return response.data;
  }
};

export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const err = error as AxiosError<{ message?: string; detail?: string; title?: string }>;
    if (err.response?.data) {
      return (
        err.response.data.message ||
        err.response.data.detail ||
        err.response.data.title ||
        'Erro na operação com o servidor.'
      );
    }
  }
  if (error instanceof Error) {
    return error.message;
  }
  return 'Ocorreu um erro inesperado ao processar a requisição.';
}
