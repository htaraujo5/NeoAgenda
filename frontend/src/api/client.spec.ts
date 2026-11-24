import { describe, it, expect } from 'vitest';
import { extractErrorMessage } from './client';
import { AxiosError } from 'axios';

describe('client.ts - extractErrorMessage', () => {
  it('should extract detail from ProblemDetails response', () => {
    const error = new AxiosError(
      'Request failed with status code 409',
      'ERR_BAD_REQUEST',
      undefined,
      undefined,
      {
        data: { detail: 'O telefone já está cadastrado no sistema.' },
        status: 409,
        statusText: 'Conflict',
        headers: {},
        config: {} as any
      }
    );

    const message = extractErrorMessage(error);
    expect(message).toBe('O telefone já está cadastrado no sistema.');
  });

  it('should extract title when detail is missing', () => {
    const error = new AxiosError(
      'Request failed',
      'ERR_BAD_REQUEST',
      undefined,
      undefined,
      {
        data: { title: 'Erro de validação' },
        status: 400,
        statusText: 'Bad Request',
        headers: {},
        config: {} as any
      }
    );

    const message = extractErrorMessage(error);
    expect(message).toBe('Erro de validação');
  });

  it('should return fallback message for unknown error', () => {
    const message = extractErrorMessage(null);
    expect(message).toContain('Ocorreu um erro inesperado');
  });
});
