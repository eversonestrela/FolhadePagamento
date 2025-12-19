import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Layout } from '@/components';
import { useAuth } from '@/contexts';
import { loteService } from '@/services';
import { Lote, ItemLote, ProgressoLote, LoteResumo } from '@/types';
import './Lotes.css';

/**
 * Página de Lotes.
 * 
 * Lista e detalhe de lotes de processamento.
 * 
 * IMPORTANTE: Não contém lógica de processamento.
 * Apenas exibe dados da API.
 */

export function LotesPage() {
  const { temPermissao } = useAuth();
  const navigate = useNavigate();
  const [lotes, setLotes] = useState<LoteResumo[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [ano, setAno] = useState(new Date().getFullYear());
  const [mes, setMes] = useState(new Date().getMonth() + 1);

  const podeCriar = temPermissao(['lote:criar']);

  useEffect(() => {
    carregarLotes();
  }, [ano, mes]);

  const carregarLotes = async () => {
    try {
      setCarregando(true);
      const dados = await loteService.listarPorCompetencia(ano, mes);
      setLotes(dados);
      setErro('');
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Erro ao carregar');
    } finally {
      setCarregando(false);
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

  const meses = [
    'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'
  ];

  return (
    <Layout>
      <div className="lotes-page">
        <header className="page-header">
          <div>
            <h1>Lotes de Processamento</h1>
          </div>
          <div className="header-acoes">
            <div className="filtros">
              <select value={mes} onChange={e => setMes(Number(e.target.value))}>
                {meses.map((nome, idx) => (
                  <option key={idx} value={idx + 1}>{nome}</option>
                ))}
              </select>
              <select value={ano} onChange={e => setAno(Number(e.target.value))}>
                {[2023, 2024, 2025, 2026].map(a => (
                  <option key={a} value={a}>{a}</option>
                ))}
              </select>
            </div>
            {podeCriar && (
              <button 
                className="btn-primario"
                onClick={() => navigate('/lotes/novo')}
              >
                ➕ Novo Lote
              </button>
            )}
          </div>
        </header>

        {erro && <div className="alerta alerta-erro">{erro}</div>}

        {carregando ? (
          <p>Carregando...</p>
        ) : lotes.length === 0 ? (
          <p className="texto-vazio">
            Nenhum lote encontrado para {meses[mes - 1]}/{ano}
          </p>
        ) : (
          <div className="tabela-container">
            <table className="tabela">
              <thead>
                <tr>
                  <th>Competência</th>
                  <th>Status</th>
                  <th>Progresso</th>
                  <th>Itens</th>
                  <th>Criado Em</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {lotes.map(lote => (
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
                          className="barra-preenchida"
                          style={{ width: `${lote.percentualConcluido}%` }}
                        />
                      </div>
                      <span className="texto-progresso">
                        {lote.percentualConcluido.toFixed(1)}%
                      </span>
                    </td>
                    <td>
                      <span className="texto-sucesso">{lote.itensConcluidos}</span>
                      {' / '}
                      {lote.totalItens}
                      {lote.itensComFalha > 0 && (
                        <span className="texto-erro"> ({lote.itensComFalha} falhas)</span>
                      )}
                    </td>
                    <td>
                      {new Date(lote.criadoEm).toLocaleDateString('pt-BR')}
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
      </div>
    </Layout>
  );
}

export default LotesPage;
