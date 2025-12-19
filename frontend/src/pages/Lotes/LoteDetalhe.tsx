import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { Layout } from '@/components';
import { useAuth } from '@/contexts';
import { loteService } from '@/services';
import { Lote, ItemLote, ProgressoLote } from '@/types';
import './Lotes.css';

/**
 * Página de Detalhe do Lote.
 * 
 * Exibe informações detalhadas de um lote de processamento,
 * incluindo itens, progresso e erros.
 * 
 * IMPORTANTE: Não contém lógica de processamento.
 * Apenas exibe dados da API.
 */

export function LoteDetalhePage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { temPermissao } = useAuth();
  
  const [lote, setLote] = useState<Lote | null>(null);
  const [itens, setItens] = useState<ItemLote[]>([]);
  const [progresso, setProgresso] = useState<ProgressoLote | null>(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [cancelando, setCancelando] = useState(false);

  const podeCancelar = temPermissao(['lote:cancelar']);

  const carregarDados = useCallback(async () => {
    if (!id) return;

    try {
      const [dadosLote, dadosItens, dadosProgresso] = await Promise.all([
        loteService.obterPorId(id),
        loteService.listarItens(id),
        loteService.obterProgresso(id),
      ]);

      setLote(dadosLote);
      setItens(dadosItens);
      setProgresso(dadosProgresso);
      setErro('');
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Erro ao carregar');
    } finally {
      setCarregando(false);
    }
  }, [id]);

  useEffect(() => {
    carregarDados();

    // Auto-refresh se lote estiver em processamento
    const intervalo = setInterval(() => {
      if (lote?.status === 'EmProcessamento' || lote?.status === 'Pendente') {
        carregarDados();
      }
    }, 5000);

    return () => clearInterval(intervalo);
  }, [carregarDados, lote?.status]);

  const cancelarLote = async () => {
    if (!lote || !confirm('Confirma o cancelamento do lote?')) return;

    try {
      setCancelando(true);
      await loteService.cancelar(lote.loteId);
      await carregarDados();
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Erro ao cancelar');
    } finally {
      setCancelando(false);
    }
  };

  const obterCorStatus = (status: string): string => {
    const cores: Record<string, string> = {
      Pendente: 'status-pendente',
      EmProcessamento: 'status-processando',
      Concluido: 'status-sucesso',
      ConcluidoComFalhas: 'status-aviso',
      Cancelado: 'status-erro',
    };
    return cores[status] || '';
  };

  const obterCorStatusItem = (status: string): string => {
    const cores: Record<string, string> = {
      Pendente: 'status-pendente',
      Processando: 'status-processando',
      Sucesso: 'status-sucesso',
      Falha: 'status-erro',
    };
    return cores[status] || '';
  };

  const formatarMoeda = (valor: number): string => {
    return valor.toLocaleString('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    });
  };

  if (carregando) {
    return (
      <Layout>
        <p>Carregando...</p>
      </Layout>
    );
  }

  if (!lote) {
    return (
      <Layout>
        <div className="alerta alerta-erro">
          Lote não encontrado.
          <Link to="/lotes" className="btn-link">Voltar</Link>
        </div>
      </Layout>
    );
  }

  const podeCancelarEste = podeCancelar && 
    (lote.status === 'Pendente' || lote.status === 'EmProcessamento');

  return (
    <Layout>
      <div className="lote-detalhe-page">
        <header className="page-header">
          <div>
            <Link to="/lotes" className="btn-link voltar">← Voltar</Link>
            <h1>
              Lote {String(lote.competenciaMes).padStart(2, '0')}/{lote.competenciaAno}
            </h1>
          </div>
          {podeCancelarEste && (
            <button 
              className="btn-danger"
              onClick={cancelarLote}
              disabled={cancelando}
            >
              {cancelando ? 'Cancelando...' : '❌ Cancelar Lote'}
            </button>
          )}
        </header>

        {erro && <div className="alerta alerta-erro">{erro}</div>}

        {/* Resumo do Lote */}
        <section className="secao-resumo">
          <div className="card-resumo">
            <h3>Status</h3>
            <span className={`badge grande ${obterCorStatus(lote.status)}`}>
              {lote.status}
            </span>
          </div>

          {progresso && (
            <>
              <div className="card-resumo">
                <h3>Progresso</h3>
                <div className="barra-progresso grande">
                  <div 
                    className="barra-preenchida"
                    style={{ width: `${progresso.percentualConcluido}%` }}
                  />
                </div>
                <span className="texto-progresso">
                  {progresso.percentualConcluido.toFixed(1)}%
                </span>
              </div>

              <div className="card-resumo">
                <h3>Itens</h3>
                <div className="contadores">
                  <span className="contador total">{progresso.totalItens} total</span>
                  <span className="contador sucesso">{progresso.itensConcluidos} concluídos</span>
                  {progresso.itensComFalha > 0 && (
                    <span className="contador erro">{progresso.itensComFalha} falhas</span>
                  )}
                </div>
              </div>
            </>
          )}

          <div className="card-resumo">
            <h3>Criado Em</h3>
            <span>{new Date(lote.criadoEm).toLocaleString('pt-BR')}</span>
          </div>

          {lote.iniciadoEm && (
            <div className="card-resumo">
              <h3>Iniciado Em</h3>
              <span>{new Date(lote.iniciadoEm).toLocaleString('pt-BR')}</span>
            </div>
          )}

          {lote.finalizadoEm && (
            <div className="card-resumo">
              <h3>Finalizado Em</h3>
              <span>{new Date(lote.finalizadoEm).toLocaleString('pt-BR')}</span>
            </div>
          )}
        </section>

        {/* Lista de Itens */}
        <section className="secao-itens">
          <h2>Itens do Lote ({itens.length})</h2>
          
          {itens.length === 0 ? (
            <p className="texto-vazio">Nenhum item encontrado</p>
          ) : (
            <div className="tabela-container">
              <table className="tabela">
                <thead>
                  <tr>
                    <th>Funcionário</th>
                    <th>Status</th>
                    <th>Salário Líquido</th>
                    <th>Tentativas</th>
                    <th>Processado Em</th>
                    <th>Erro</th>
                  </tr>
                </thead>
                <tbody>
                  {itens.map(item => (
                    <tr key={item.itemId} className={item.status === 'Falha' ? 'linha-erro' : ''}>
                      <td>
                        {item.funcionarioNome || item.funcionarioId}
                      </td>
                      <td>
                        <span className={`badge ${obterCorStatusItem(item.status)}`}>
                          {item.status}
                        </span>
                      </td>
                      <td>
                        {item.salarioLiquido != null 
                          ? formatarMoeda(item.salarioLiquido) 
                          : '-'
                        }
                      </td>
                      <td>{item.tentativas}</td>
                      <td>
                        {item.processadoEm 
                          ? new Date(item.processadoEm).toLocaleString('pt-BR')
                          : '-'
                        }
                      </td>
                      <td className="coluna-erro">
                        {item.mensagemErro || '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        {/* Nota Importante */}
        <p className="nota">
          💡 Todos os cálculos são realizados pela API.
          Este front-end apenas exibe os resultados.
        </p>
      </div>
    </Layout>
  );
}

export default LoteDetalhePage;
