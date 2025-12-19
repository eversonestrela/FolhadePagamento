import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { ErroResponse } from '@/types';

/**
 * Cliente HTTP configurado para consumir a API.
 * 
 * IMPORTANTE:
 * - Adiciona token JWT automaticamente em todas as requisições
 * - Trata erros 401/403 globalmente
 * - Redireciona para login se token expirado
 */

const API_URL = import.meta.env.VITE_API_URL || 'https://localhost:7001';

export const apiClient = axios.create({
  baseURL: `${API_URL}/api/v1`,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ============================================================================
// GERENCIAMENTO DE TOKEN
// ============================================================================

const TOKEN_KEY = 'folha_token';
const PAPEL_KEY = 'folha_papel';

export const tokenStorage = {
  getToken: (): string | null => {
    return sessionStorage.getItem(TOKEN_KEY);
  },

  setToken: (token: string): void => {
    sessionStorage.setItem(TOKEN_KEY, token);
  },

  removeToken: (): void => {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(PAPEL_KEY);
  },

  getPapel: (): string | null => {
    return sessionStorage.getItem(PAPEL_KEY);
  },

  setPapel: (papel: string): void => {
    sessionStorage.setItem(PAPEL_KEY, papel);
  },
};

// ============================================================================
// INTERCEPTOR DE REQUISIÇÃO - Adiciona JWT
// ============================================================================

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = tokenStorage.getToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// ============================================================================
// INTERCEPTOR DE RESPOSTA - Trata erros globalmente
// ============================================================================

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ErroResponse>) => {
    const status = error.response?.status;

    if (status === 401) {
      // Token expirado ou inválido
      tokenStorage.removeToken();
      window.location.href = '/login?sessaoExpirada=true';
      return Promise.reject(new Error('Sessão expirada. Faça login novamente.'));
    }

    if (status === 403) {
      // Sem permissão
      return Promise.reject(new Error('Você não tem permissão para realizar esta ação.'));
    }

    // Outros erros
    const mensagem = error.response?.data?.mensagem || error.message || 'Erro desconhecido';
    return Promise.reject(new Error(mensagem));
  }
);

export default apiClient;
