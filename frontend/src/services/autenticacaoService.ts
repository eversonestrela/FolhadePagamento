import apiClient, { tokenStorage } from './apiClient';
import { LoginRequest, LoginResponse, UsuarioAutenticado, PapelInfo } from '@/types';

/**
 * Serviço de autenticação.
 * 
 * IMPORTANTE: Não contém lógica de autorização.
 * Apenas consome endpoints da API.
 */
export const autenticacaoService = {
  /**
   * Realiza login e armazena token.
   */
  async login(credenciais: LoginRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>('/autenticacao/login', credenciais);
    const { token, papel } = response.data;
    
    tokenStorage.setToken(token);
    if (papel) {
      tokenStorage.setPapel(papel);
    }
    
    return response.data;
  },

  /**
   * Realiza logout removendo token.
   */
  logout(): void {
    tokenStorage.removeToken();
  },

  /**
   * Verifica se o token atual é válido.
   */
  async verificar(): Promise<UsuarioAutenticado> {
    const response = await apiClient.get<UsuarioAutenticado>('/autenticacao/verificar');
    return response.data;
  },

  /**
   * Lista papéis e permissões disponíveis.
   */
  async listarPapeis(): Promise<PapelInfo[]> {
    const response = await apiClient.get<PapelInfo[]>('/autenticacao/papeis');
    return response.data;
  },

  /**
   * Verifica se está autenticado (tem token).
   */
  estaAutenticado(): boolean {
    return !!tokenStorage.getToken();
  },

  /**
   * Obtém o papel do usuário atual.
   */
  obterPapel(): string | null {
    return tokenStorage.getPapel();
  },
};

export default autenticacaoService;
