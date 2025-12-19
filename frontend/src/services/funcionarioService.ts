import apiClient from './apiClient';
import { 
  Funcionario, 
  CriarFuncionarioRequest, 
  AtualizarFuncionarioRequest 
} from '@/types';

/**
 * Serviço de funcionários.
 * 
 * IMPORTANTE: Não contém regras de negócio.
 * Apenas consome endpoints da API.
 */
export const funcionarioService = {
  /**
   * Lista todos os funcionários ativos.
   */
  async listar(): Promise<Funcionario[]> {
    const response = await apiClient.get<Funcionario[]>('/funcionarios');
    return response.data;
  },

  /**
   * Obtém um funcionário pelo ID.
   */
  async obterPorId(funcionarioId: string): Promise<Funcionario> {
    const response = await apiClient.get<Funcionario>(`/funcionarios/${funcionarioId}`);
    return response.data;
  },

  /**
   * Cria um novo funcionário.
   * Requer permissão: Administrador
   */
  async criar(dados: CriarFuncionarioRequest): Promise<{ funcionarioId: string }> {
    const response = await apiClient.post('/funcionarios', dados);
    return response.data;
  },

  /**
   * Atualiza um funcionário existente.
   * Requer permissão: Administrador
   */
  async atualizar(funcionarioId: string, dados: AtualizarFuncionarioRequest): Promise<void> {
    await apiClient.put(`/funcionarios/${funcionarioId}`, dados);
  },

  /**
   * Desativa um funcionário (soft delete).
   * Requer permissão: Administrador
   */
  async desativar(funcionarioId: string): Promise<void> {
    await apiClient.delete(`/funcionarios/${funcionarioId}`);
  },
};

export default funcionarioService;
