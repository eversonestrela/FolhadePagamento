import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts';
import './Layout.css';

/**
 * Layout principal da aplicação.
 * 
 * Exibe menu de navegação e informações do usuário.
 * Menu adapta-se ao papel do usuário (RBAC).
 */

interface LayoutProps {
  children: React.ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const { usuario, papel, logout, temPermissao } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="layout">
      <header className="header">
        <div className="header-brand">
          <h1>📋 Folha de Pagamento</h1>
        </div>
        
        <nav className="nav">
          <Link to="/dashboard" className="nav-link">Dashboard</Link>
          
          <Link to="/funcionarios" className="nav-link">Funcionários</Link>
          
          <Link to="/processamentos" className="nav-link">Processamentos</Link>
          
          <Link to="/lotes" className="nav-link">Lotes</Link>
        </nav>

        <div className="header-user">
          <span className="user-info">
            👤 {usuario?.nome || 'Usuário'}
            <span className="user-papel">{papel}</span>
          </span>
          <button onClick={handleLogout} className="btn-logout">
            Sair
          </button>
        </div>
      </header>

      <main className="main-content">
        {children}
      </main>

      <footer className="footer">
        <p>Folha de Pagamento v1.0 • {new Date().getFullYear()}</p>
      </footer>
    </div>
  );
}

export default Layout;
