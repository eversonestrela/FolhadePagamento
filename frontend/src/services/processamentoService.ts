import apiClient from './apiClient';
import { 
  Processamento, 
  ProcessamentoResumo, 
  ProcessarFolhaRequest 
} from '@/types';

/**
 * Serviço de processamentos.
 * 
 * IMPORTANTE: Não contém regras de cálculo.
 * Valores vêm calculados da API.
 */
export const processamentoService = {
  /**
   * Obtém um processamento pelo ID.
   */
  async obterPorId(processamentoVersaoId: string): Promise<Processamento> {
    const response = await apiClient.get<Processamento>(`/processamentos/${processamentoVersaoId}`);
    return response.data;
  },

  /**
   * Obtém a versão atual do processamento para funcionário e competência.
   */
  async obterVersaoAtual(
    funcionarioId: string, 
    ano: number, 
    mes: number
  ): Promise<Processamento> {
    const response = await apiClient.get<Processamento>(
      `/processamentos/funcionario/${funcionarioId}/competencia/${ano}/${mes}`
    );
    return response.data;
  },

  /**
   * Obtém histórico de versões para funcionário e competência.
   */
  async obterHistorico(
    funcionarioId: string, 
    ano: number, 
    mes: number
  ): Promise<ProcessamentoResumo[]> {
    const response = await apiClient.get<ProcessamentoResumo[]>(
      `/processamentos/funcionario/${funcionarioId}/competencia/${ano}/${mes}/historico`
    );
    return response.data;
  },

  /**
   * Lista processamentos de uma competência.
   */
  async listarPorCompetencia(
    ano: number, 
    mes: number, 
    incluirHistorico = false
  ): Promise<ProcessamentoResumo[]> {
    const response = await apiClient.get<ProcessamentoResumo[]>(
      `/processamentos/competencia/${ano}/${mes}`,
      { params: { incluirHistorico } }
    );
    return response.data;
  },

  /**
   * Processa folha de pagamento de um funcionário.
   * Requer permissão: Administrador ou Operador
   */
  async processar(dados: ProcessarFolhaRequest): Promise<{ processamentoVersaoId: string }> {
    const response = await apiClient.post('/processamentos', dados);
    return response.data;
  },
};

export default processamentoService;
