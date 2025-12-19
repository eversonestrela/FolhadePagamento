import apiClient from './apiClient';
import { 
  Lote, 
  LoteResumo, 
  ItemLote, 
  ProgressoLote, 
  CriarLoteRequest 
} from '@/types';

/**
 * Serviço de lotes de processamento.
 * 
 * IMPORTANTE: Não contém lógica de processamento.
 * Apenas consome endpoints da API.
 */
export const loteService = {
  /**
   * Cria um novo lote de processamento.
   * Requer permissão: Administrador ou Operador
   */
  async criar(dados: CriarLoteRequest): Promise<{ loteId: string; totalItens: number }> {
    const response = await apiClient.post('/lotes', dados);
    return response.data;
  },

  /**
   * Obtém detalhes de um lote pelo ID.
   */
  async obterPorId(loteId: string): Promise<Lote> {
    const response = await apiClient.get<Lote>(`/lotes/${loteId}`);
    return response.data;
  },

  /**
   * Lista itens de um lote.
   */
  async listarItens(loteId: string): Promise<ItemLote[]> {
    const response = await apiClient.get<ItemLote[]>(`/lotes/${loteId}/itens`);
    return response.data;
  },

  /**
   * Lista lotes ativos (pendentes ou em processamento).
   */
  async listarAtivos(): Promise<LoteResumo[]> {
    const response = await apiClient.get<LoteResumo[]>('/lotes/ativos');
    return response.data;
  },

  /**
   * Lista lotes por competência.
   */
  async listarPorCompetencia(ano: number, mes: number): Promise<LoteResumo[]> {
    const response = await apiClient.get<LoteResumo[]>(`/lotes/competencia/${ano}/${mes}`);
    return response.data;
  },

  /**
   * Obtém progresso de um lote.
   */
  async obterProgresso(loteId: string): Promise<ProgressoLote> {
    const response = await apiClient.get<ProgressoLote>(`/lotes/${loteId}/progresso`);
    return response.data;
  },

  /**
   * Cancela um lote pendente.
   * Requer permissão: Apenas Administrador
   */
  async cancelar(loteId: string): Promise<void> {
    await apiClient.post(`/lotes/${loteId}/cancelar`);
  },
};

export default loteService;
