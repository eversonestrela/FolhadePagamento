import { useState, useEffect } from 'react';
import { Layout } from '@/components';
import { useAuth } from '@/contexts';
import { funcionarioService } from '@/services';
import { Funcionario, CriarFuncionarioRequest } from '@/types';
import './Funcionarios.css';

/**
 * Página de Funcionários.
 * 
 * CRUD de funcionários respeitando RBAC:
 * - Consulta: Apenas visualização
 * - Operador: Apenas visualização
 * - Administrador: CRUD completo
 * 
 * IMPORTANTE: Não contém regras de negócio.
 */

export function FuncionariosPage() {
  const { temPermissao } = useAuth();
  const [funcionarios, setFuncionarios] = useState<Funcionario[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [modalAberto, setModalAberto] = useState(false);
  const [funcionarioEditando, setFuncionarioEditando] = useState<Funcionario | null>(null);

  // Permissões
  const podeCriar = temPermissao(['funcionario:criar']);
  const podeEditar = temPermissao(['funcionario:atualizar']);
  const podeDesativar = temPermissao(['funcionario:desativar']);

  useEffect(() => {
    carregarFuncionarios();
  }, []);

  const carregarFuncionarios = async () => {
    try {
      setCarregando(true);
      const dados = await funcionarioService.listar();
      setFuncionarios(dados);
      setErro('');
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Erro ao carregar funcionários');
    } finally {
      setCarregando(false);
    }
  };

  const handleDesativar = async (funcionarioId: string) => {
    if (!confirm('Deseja realmente desativar este funcionário?')) return;

    try {
      await funcionarioService.desativar(funcionarioId);
      await carregarFuncionarios();
    } catch (err) {
      alert(err instanceof Error ? err.message : 'Erro ao desativar');
    }
  };

  const formatarData = (data: string) => {
    return new Date(data).toLocaleDateString('pt-BR');
  };

  const formatarMoeda = (valor: number) => {
    return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  };

  return (
    <Layout>
      <div className="funcionarios-page">
        <header className="page-header">
          <div>
            <h1>Funcionários</h1>
            <p>{funcionarios.length} funcionário(s) ativo(s)</p>
          </div>
          {podeCriar && (
            <button 
              className="btn-primario"
              onClick={() => { setFuncionarioEditando(null); setModalAberto(true); }}
            >
              ➕ Novo Funcionário
            </button>
          )}
        </header>

        {erro && <div className="alerta alerta-erro">{erro}</div>}

        {carregando ? (
          <p>Carregando...</p>
        ) : funcionarios.length === 0 ? (
          <p className="texto-vazio">Nenhum funcionário cadastrado.</p>
        ) : (
          <div className="tabela-container">
            <table className="tabela">
              <thead>
                <tr>
                  <th>Nome</th>
                  <th>Salário Base</th>
                  <th>Admissão</th>
                  <th>Status</th>
                  {(podeEditar || podeDesativar) && <th>Ações</th>}
                </tr>
              </thead>
              <tbody>
                {funcionarios.map(func => (
                  <tr key={func.funcionarioId}>
                    <td>{func.nome}</td>
                    <td>{formatarMoeda(func.salarioBase)}</td>
                    <td>{formatarData(func.dataAdmissao)}</td>
                    <td>
                      <span className={`badge ${func.ativo ? 'status-sucesso' : 'status-erro'}`}>
                        {func.ativo ? 'Ativo' : 'Inativo'}
                      </span>
                    </td>
                    {(podeEditar || podeDesativar) && (
                      <td className="acoes">
                        {podeEditar && (
                          <button 
                            className="btn-icon"
                            onClick={() => { setFuncionarioEditando(func); setModalAberto(true); }}
                            title="Editar"
                          >
                            ✏️
                          </button>
                        )}
                        {podeDesativar && func.ativo && (
                          <button 
                            className="btn-icon btn-danger"
                            onClick={() => handleDesativar(func.funcionarioId)}
                            title="Desativar"
                          >
                            🗑️
                          </button>
                        )}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Modal de Criação/Edição */}
        {modalAberto && (
          <ModalFuncionario
            funcionario={funcionarioEditando}
            onFechar={() => setModalAberto(false)}
            onSalvar={async () => {
              setModalAberto(false);
              await carregarFuncionarios();
            }}
          />
        )}
      </div>
    </Layout>
  );
}

// ============================================================================
// MODAL DE FUNCIONÁRIO
// ============================================================================

interface ModalFuncionarioProps {
  funcionario: Funcionario | null;
  onFechar: () => void;
  onSalvar: () => Promise<void>;
}

function ModalFuncionario({ funcionario, onFechar, onSalvar }: ModalFuncionarioProps) {
  const [nome, setNome] = useState(funcionario?.nome || '');
  const [salarioBase, setSalarioBase] = useState(funcionario?.salarioBase?.toString() || '');
  const [dataAdmissao, setDataAdmissao] = useState(
    funcionario?.dataAdmissao ? funcionario.dataAdmissao.split('T')[0] : ''
  );
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErro('');
    setSalvando(true);

    try {
      if (funcionario) {
        // Editar
        await funcionarioService.atualizar(funcionario.funcionarioId, {
          nome,
          salarioBase: parseFloat(salarioBase),
          dataAdmissao,
        });
      } else {
        // Criar
        await funcionarioService.criar({
          nome,
          salarioBase: parseFloat(salarioBase),
          dataAdmissao,
        });
      }
      await onSalvar();
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Erro ao salvar');
    } finally {
      setSalvando(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onFechar}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <h2>{funcionario ? 'Editar Funcionário' : 'Novo Funcionário'}</h2>
        
        {erro && <div className="alerta alerta-erro">{erro}</div>}

        <form onSubmit={handleSubmit}>
          <div className="campo">
            <label htmlFor="nome">Nome</label>
            <input
              id="nome"
              type="text"
              value={nome}
              onChange={e => setNome(e.target.value)}
              required
              disabled={salvando}
            />
          </div>

          <div className="campo">
            <label htmlFor="salarioBase">Salário Base</label>
            <input
              id="salarioBase"
              type="number"
              step="0.01"
              min="0"
              value={salarioBase}
              onChange={e => setSalarioBase(e.target.value)}
              required
              disabled={salvando}
            />
          </div>

          <div className="campo">
            <label htmlFor="dataAdmissao">Data de Admissão</label>
            <input
              id="dataAdmissao"
              type="date"
              value={dataAdmissao}
              onChange={e => setDataAdmissao(e.target.value)}
              required
              disabled={salvando}
            />
          </div>

          <div className="modal-acoes">
            <button type="button" className="btn-secundario" onClick={onFechar} disabled={salvando}>
              Cancelar
            </button>
            <button type="submit" className="btn-primario" disabled={salvando}>
              {salvando ? 'Salvando...' : 'Salvar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default FuncionariosPage;
