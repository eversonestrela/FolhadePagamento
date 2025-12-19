import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts';

/**
 * Componente de rota protegida.
 * 
 * Redireciona para login se não autenticado.
 * Verifica permissões opcionalmente.
 */

interface RotaProtegidaProps {
  permissoesRequeridas?: string[];
}

export function RotaProtegida({ permissoesRequeridas = [] }: RotaProtegidaProps) {
  const { autenticado, carregando, temPermissao } = useAuth();
  const location = useLocation();

  if (carregando) {
    return (
      <div className="carregando-container">
        <div className="spinner"></div>
        <p>Carregando...</p>
      </div>
    );
  }

  if (!autenticado) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (permissoesRequeridas.length > 0 && !temPermissao(permissoesRequeridas)) {
    return <Navigate to="/sem-permissao" replace />;
  }

  return <Outlet />;
}

export default RotaProtegida;
