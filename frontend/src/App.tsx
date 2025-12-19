import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from '@/contexts';
import { RotaProtegida } from '@/components';
import {
  LoginPage,
  DashboardPage,
  FuncionariosPage,
  ProcessamentosPage,
  LotesPage,
  LoteDetalhePage,
} from '@/pages';
import './App.css';

/**
 * Componente principal do SPA.
 * 
 * Configura roteamento e autenticação.
 * 
 * IMPORTANTE:
 * - Toda lógica de negócio está na API
 * - Este front-end apenas exibe dados e respeita RBAC
 */

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          {/* Rota pública */}
          <Route path="/login" element={<LoginPage />} />

          {/* Rotas protegidas - Dashboard (qualquer usuário autenticado) */}
          <Route 
            path="/" 
            element={
              <RotaProtegida>
                <DashboardPage />
              </RotaProtegida>
            } 
          />
          <Route 
            path="/dashboard" 
            element={<Navigate to="/" replace />} 
          />

          {/* Funcionários */}
          <Route 
            path="/funcionarios" 
            element={
              <RotaProtegida permissoesRequeridas={['funcionario:listar']}>
                <FuncionariosPage />
              </RotaProtegida>
            } 
          />

          {/* Processamentos */}
          <Route 
            path="/processamentos" 
            element={
              <RotaProtegida permissoesRequeridas={['processamento:listar']}>
                <ProcessamentosPage />
              </RotaProtegida>
            } 
          />

          {/* Lotes */}
          <Route 
            path="/lotes" 
            element={
              <RotaProtegida permissoesRequeridas={['lote:listar']}>
                <LotesPage />
              </RotaProtegida>
            } 
          />
          <Route 
            path="/lotes/:id" 
            element={
              <RotaProtegida permissoesRequeridas={['lote:listar']}>
                <LoteDetalhePage />
              </RotaProtegida>
            } 
          />

          {/* Página de sem permissão */}
          <Route 
            path="/sem-permissao" 
            element={
              <div className="pagina-erro">
                <h1>🚫 Acesso Negado</h1>
                <p>Você não tem permissão para acessar esta página.</p>
                <a href="/">Voltar ao Início</a>
              </div>
            } 
          />

          {/* Rota 404 */}
          <Route 
            path="*" 
            element={
              <div className="pagina-erro">
                <h1>404</h1>
                <p>Página não encontrada.</p>
                <a href="/">Voltar ao Início</a>
              </div>
            } 
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
