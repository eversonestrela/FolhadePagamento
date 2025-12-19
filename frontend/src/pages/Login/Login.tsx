import { useState, FormEvent } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '@/contexts';
import './Login.css';

/**
 * Página de Login.
 * 
 * IMPORTANTE: Não valida credenciais localmente.
 * Toda validação é feita pela API.
 */

export function LoginPage() {
  const [usuario, setUsuario] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro] = useState('');
  const [carregando, setCarregando] = useState(false);
  
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const sessaoExpirada = searchParams.get('sessaoExpirada') === 'true';

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setErro('');
    setCarregando(true);

    try {
      await login(usuario, senha);
      navigate('/dashboard');
    } catch (err) {
      setErro(err instanceof Error ? err.message : 'Erro ao fazer login');
    } finally {
      setCarregando(false);
    }
  };

  return (
    <div className="login-container">
      <div className="login-card">
        <div className="login-header">
          <h1>📋 Folha de Pagamento</h1>
          <p>Sistema de Processamento</p>
        </div>

        {sessaoExpirada && (
          <div className="alerta alerta-aviso">
            Sua sessão expirou. Faça login novamente.
          </div>
        )}

        {erro && (
          <div className="alerta alerta-erro">
            {erro}
          </div>
        )}

        <form onSubmit={handleSubmit} className="login-form">
          <div className="campo">
            <label htmlFor="usuario">Usuário</label>
            <input
              id="usuario"
              type="text"
              value={usuario}
              onChange={(e) => setUsuario(e.target.value)}
              placeholder="Digite seu usuário"
              required
              disabled={carregando}
              autoFocus
            />
          </div>

          <div className="campo">
            <label htmlFor="senha">Senha</label>
            <input
              id="senha"
              type="password"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              placeholder="Digite sua senha"
              required
              disabled={carregando}
            />
          </div>

          <button 
            type="submit" 
            className="btn-login"
            disabled={carregando}
          >
            {carregando ? 'Entrando...' : 'Entrar'}
          </button>
        </form>

        <div className="login-info">
          <p><strong>Credenciais de demonstração:</strong></p>
          <ul>
            <li><code>admin / admin123</code> — Administrador</li>
            <li><code>operador / operador123</code> — Operador</li>
            <li><code>consulta / consulta123</code> — Consulta</li>
          </ul>
        </div>
      </div>
    </div>
  );
}

export default LoginPage;
