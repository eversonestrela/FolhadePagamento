import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { Layout } from '@/components';
import { processamentoService } from '@/services';
import { ProcessamentoResumo, Processamento } from '@/types';
import './Processamentos.css';

/**
 * Página de Processamentos.
 * 
 * Exibe histórico de processamentos e versões.
 * 
 * IMPORTANTE: Não contém regras de cálculo.
 * Valores são exibidos conforme retornados pela API.
 */

export function ProcessamentosPage() {
  const [ano, setAno] = useState(new Date().getFullYear());
  const [mes, setMes] = useState(new Date().getMonth() + 1);
  const [processamentos, setProcessamentos] = useState<ProcessamentoResumo[]>([]);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState('');
  const [detalhe, setDetalhe] = useState<Processamento | null>(null);

  useEffect(() => {
    carregarProcessamentos();
  }, [ano, mes]);

  const carregarProcessamentos = async () => {
    try {
      setCarregando(true);
      const dados = await processamentoService.listarPorCompetencia(ano, mes);
      setProcessamentos(dados);
      setErro('');
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Erro ao carregar');
    } finally {
      setCarregando(false);
    }
  };

  const verDetalhe = async (processamentoVersaoId: string) => {
    try {
      const dados = await processamentoService.obterPorId(processamentoVersaoId);
      setDetalhe(dados);
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Erro ao carregar detalhes');
    }
  };

  const formatarMoeda = (valor: number) => {
    return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  };

  const meses = [
    'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'
  ];

  return (
    <Layout>
      <div className="processamentos-page">
        <header className="page-header">
          <h1>Processamentos</h1>
          
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
        </header>

        {erro && <div className="alerta alerta-erro">{erro}</div>}

        {carregando ? (
          <p>Carregando...</p>
        ) : processamentos.length === 0 ? (
          <p className="texto-vazio">
            Nenhum processamento encontrado para {meses[mes - 1]}/{ano}
          </p>
        ) : (
          <div className="tabela-container">
            <table className="tabela">
              <thead>
                <tr>
                  <th>Funcionário</th>
                  <th>Versão</th>
                  <th>Status</th>
                  <th>Salário Líquido</th>
                  <th>Finalizado</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {processamentos.map(proc => (
                  <tr key={proc.processamentoVersaoId}>
                    <td>{proc.nomeFuncionario || proc.funcionarioId.slice(0, 8)}</td>
                    <td>v{proc.versaoNumero}</td>
                    <td>
                      <span className={`badge status-${proc.status.toLowerCase()}`}>
                        {proc.status}
                      </span>
                    </td>
                    <td>{formatarMoeda(proc.salarioLiquido)}</td>
                    <td>
                      {proc.finalizadoEm 
                        ? new Date(proc.finalizadoEm).toLocaleDateString('pt-BR')
                        : '-'}
                    </td>
                    <td>
                      <button 
                        className="btn-link"
                        onClick={() => verDetalhe(proc.processamentoVersaoId)}
                      >
                        Ver Detalhes
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Modal de Detalhe */}
        {detalhe && (
          <DetalheProcessamento 
            processamento={detalhe} 
            onFechar={() => setDetalhe(null)} 
          />
        )}
      </div>
    </Layout>
  );
}

// ============================================================================
// COMPONENTE DE DETALHE
// ============================================================================

interface DetalheProcessamentoProps {
  processamento: Processamento;
  onFechar: () => void;
}

function DetalheProcessamento({ processamento, onFechar }: DetalheProcessamentoProps) {
  const formatarMoeda = (valor: number) => {
    return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  };

  const { resultado } = processamento;

  return (
    <div className="modal-overlay" onClick={onFechar}>
      <div className="modal modal-grande" onClick={e => e.stopPropagation()}>
        <h2>Detalhes do Processamento</h2>
        
        <div className="detalhe-info">
          <p><strong>Competência:</strong> {processamento.competenciaMes}/{processamento.competenciaAno}</p>
          <p><strong>Versão:</strong> {processamento.versaoNumero}</p>
          <p><strong>Status:</strong> {processamento.status}</p>
        </div>

        {resultado && (
          <div className="detalhe-resultado">
            <h3>Resultado do Cálculo</h3>
            <p className="nota">
              ⚠️ Valores calculados pela API. Front-end apenas exibe.
            </p>
            
            <table className="tabela-detalhe">
              <tbody>
                <tr>
                  <td>Salário Bruto</td>
                  <td className="valor">{formatarMoeda(resultado.salarioBruto)}</td>
                </tr>
                <tr className="desconto">
                  <td>(-) INSS</td>
                  <td className="valor">{formatarMoeda(resultado.valorInss)}</td>
                </tr>
                <tr className="desconto">
                  <td>(-) IRRF</td>
                  <td className="valor">{formatarMoeda(resultado.valorIrrf)}</td>
                </tr>
                <tr className="desconto">
                  <td>(-) Consignados</td>
                  <td className="valor">{formatarMoeda(resultado.valorConsignados)}</td>
                </tr>
                <tr className="total">
                  <td>Total Descontos</td>
                  <td className="valor">{formatarMoeda(resultado.totalDescontos)}</td>
                </tr>
                <tr className="liquido">
                  <td><strong>Salário Líquido</strong></td>
                  <td className="valor"><strong>{formatarMoeda(resultado.salarioLiquido)}</strong></td>
                </tr>
                <tr>
                  <td colSpan={2}><hr /></td>
                </tr>
                <tr className="encargos">
                  <td>FGTS (Patronal)</td>
                  <td className="valor">{formatarMoeda(resultado.valorFgts)}</td>
                </tr>
                <tr className="encargos">
                  <td>Custo Total Empregador</td>
                  <td className="valor">{formatarMoeda(resultado.custoTotalEmpregador)}</td>
                </tr>
              </tbody>
            </table>
          </div>
        )}

        <div className="modal-acoes">
          <button className="btn-secundario" onClick={onFechar}>
            Fechar
          </button>
        </div>
      </div>
    </div>
  );
}

export default ProcessamentosPage;
