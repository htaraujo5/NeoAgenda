export interface ContactDto {
  id: string;
  name: string;
  email: string;
  phoneOriginal: string;
  phoneNormalized: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateContactDto {
  name: string;
  email: string;
  phone: string;
}

export interface UpdateContactDto {
  name: string;
  email: string;
  phone: string;
}

export interface PagedResultDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface RecycleBinContactDto {
  id: string;
  name: string;
  email: string;
  phoneOriginal: string;
  phoneNormalized: string;
  deletedAt: string;
  retentionExpiresAt: string;
  daysRemaining: number;
  isExpired: boolean;
}

export interface SoftDeleteResultDto {
  id: string;
  message: string;
  deletedAt: string;
  retentionExpiresAt: string;
}

export interface RestoreContactResultDto {
  id: string;
  message: string;
  contact: ContactDto;
}

export interface LgpdPurgeResultDto {
  protocol: string;
  message: string;
  purgedAt: string;
}

export interface CsvRowErrorDto {
  lineNumber: number;
  rawLine: string;
  field: string | null;
  reason: string;
}

export interface CsvImportResultDto {
  totalRows: number;
  importedCount: number;
  failedCount: number;
  errors: CsvRowErrorDto[];
  importedContacts: ContactDto[];
}
