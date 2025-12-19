import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Layout } from '@/components';
import { useAuth } from '@/contexts';
import { loteService } from '@/services';
import { LoteResumo } from '@/types';
import './Dashboard.css';

/**
 * Dashboard principal.
 * 
 * Exibe visão geral do sistema:
 * - Lotes ativos e em processamento
 * - Progresso de lotes
 * - Ações rápidas (conforme RBAC)
 * 
 * IMPORTANTE: Não contém lógica de negócio.
 * Apenas exibe dados da API.
 */

export function DashboardPage() {
  const { usuario, papel, temPermissao } = useAuth();
  const [lotesAtivos, setLotesAtivos] = useState<LoteResumo[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');

  useEffect(() => {
    carregarDados();
    
    // Atualizar a cada 10 segundos
    const interval = setInterval(carregarDados, 10000);
    return () => clearInterval(interval);
  }, []);

  const carregarDados = async () => {
    try {
      const lotes = await loteService.listarAtivos();
      setLotesAtivos(lotes);
      setErro('');
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Erro ao carregar dados');
    } finally {
      setCarregando(false);
    }
  };

  const obterCorStatus = (status: string): string => {
    switch (status) {
      case 'Pendente': return 'status-pendente';
      case 'EmProcessamento': return 'status-processando';
      case 'Concluido': return 'status-sucesso';
      case 'ConcluidoComFalhas': return 'status-aviso';
      case 'Cancelado': return 'status-erro';
      default: return '';
    }
  };

  return (
    <Layout>
      <div className="dashboard">
        <header className="page-header">
          <h1>Dashboard</h1>
          <p>Bem-vindo(a), {usuario?.nome || 'Usuário'}!</p>
        </header>

        {/* Cards de Resumo */}
        <section className="cards-resumo">
          <div className="card-resumo">
            <span className="card-icone">📊</span>
            <div className="card-info">
              <span className="card-valor">{lotesAtivos.length}</span>
              <span className="card-label">Lotes Ativos</span>
            </div>
          </div>

          <div className="card-resumo">
            <span className="card-icone">⏳</span>
            <div className="card-info">
              <span className="card-valor">
                {lotesAtivos.filter(l => l.status === 'EmProcessamento').length}
              </span>
              <span className="card-label">Em Processamento</span>
            </div>
          </div>

          <div className="card-resumo">
            <span className="card-icone">👤</span>
            <div className="card-info">
              <span className="card-valor">{papel}</span>
              <span className="card-label">Seu Papel</span>
            </div>
          </div>
        </section>

        {/* Ações Rápidas */}
        <section className="secao">
          <h2>Ações Rápidas</h2>
          <div className="acoes-rapidas">
            {temPermissao(['lote:criar']) && (
              <Link to="/lotes/novo" className="btn-acao">
                ➕ Criar Lote
              </Link>
            )}
            <Link to="/processamentos" className="btn-acao">
              📋 Ver Processamentos
            </Link>
            <Link to="/funcionarios" className="btn-acao">
              👥 Ver Funcionários
            </Link>
          </div>
        </section>

        {/* Lotes Ativos */}
        <section className="secao">
          <h2>Lotes Ativos</h2>
          
          {carregando && <p>Carregando...</p>}
          
          {erro && <div className="alerta alerta-erro">{erro}</div>}
          
          {!carregando && lotesAtivos.length === 0 && (
            <p className="texto-vazio">Nenhum lote ativo no momento.</p>
          )}

          {lotesAtivos.length > 0 && (
            <div className="tabela-container">
              <table className="tabela">
                <thead>
                  <tr>
                    <th>Competência</th>
                    <th>Status</th>
                    <th>Progresso</th>
                    <th>Itens</th>
                    <th>Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {lotesAtivos.map(lote => (
                    <tr key={lote.loteId}>
                      <td>
                        {String(lote.competenciaMes).padStart(2, '0')}/{lote.competenciaAno}
                      </td>
                      <td>
                        <span className={`badge ${obterCorStatus(lote.status)}`}>
                          {lote.status}
                        </span>
                      </td>
                      <td>
                        <div className="barra-progresso">
                          <div 
                            className="barra-progresso-preenchido"
                            style={{ width: `${lote.percentualConcluido}%` }}
                          />
                        </div>
                        <span className="texto-progresso">
                          {lote.percentualConcluido.toFixed(1)}%
                        </span>
                      </td>
                      <td>
                        {lote.itensConcluidos}/{lote.totalItens}
                        {lote.itensComFalha > 0 && (
                          <span className="texto-erro"> ({lote.itensComFalha} falhas)</span>
                        )}
                      </td>
                      <td>
                        <Link to={`/lotes/${lote.loteId}`} className="btn-link">
                          Ver Detalhes
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    </Layout>
  );
}

export default DashboardPage;
