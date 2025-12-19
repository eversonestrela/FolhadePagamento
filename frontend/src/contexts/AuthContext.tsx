import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { autenticacaoService } from '@/services';
import { UsuarioAutenticado, Papel } from '@/types';

/**
 * Contexto de autenticação.
 * 
 * Gerencia o estado de autenticação da aplicação.
 * IMPORTANTE: Não contém lógica de autorização - apenas expõe dados.
 */

interface AuthContextType {
  usuario: UsuarioAutenticado | null;
  papel: Papel | null;
  carregando: boolean;
  autenticado: boolean;
  login: (usuario: string, senha: string) => Promise<void>;
  logout: () => void;
  temPermissao: (permissoesRequeridas: string[]) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

// ============================================================================
// MAPEAMENTO DE PERMISSÕES POR PAPEL (espelhando backend)
// ============================================================================

const PERMISSOES_POR_PAPEL: Record<string, string[]> = {
  Administrador: [
    'funcionario:consultar', 'funcionario:criar', 'funcionario:atualizar', 'funcionario:desativar',
    'processamento:consultar', 'processamento:executar',
    'lote:consultar', 'lote:criar', 'lote:cancelar',
  ],
  Operador: [
    'funcionario:consultar',
    'processamento:consultar', 'processamento:executar',
    'lote:consultar', 'lote:criar',
  ],
  Consulta: [
    'funcionario:consultar',
    'processamento:consultar',
    'lote:consultar',
  ],
};

// ============================================================================
// PROVIDER
// ============================================================================

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [usuario, setUsuario] = useState<UsuarioAutenticado | null>(null);
  const [papel, setPapel] = useState<Papel | null>(null);
  const [carregando, setCarregando] = useState(true);

  // Verificar autenticação ao carregar
  useEffect(() => {
    const verificarAuth = async () => {
      if (autenticacaoService.estaAutenticado()) {
        try {
          const usuarioData = await autenticacaoService.verificar();
          setUsuario(usuarioData);
          setPapel((autenticacaoService.obterPapel() || usuarioData.papeis[0]) as Papel);
        } catch {
          autenticacaoService.logout();
        }
      }
      setCarregando(false);
    };

    verificarAuth();
  }, []);

  const login = async (usuarioNome: string, senha: string) => {
    const response = await autenticacaoService.login({ usuario: usuarioNome, senha });
    
    // Verificar dados do usuário após login
    const usuarioData = await autenticacaoService.verificar();
    setUsuario(usuarioData);
    setPapel((response.papel || usuarioData.papeis[0]) as Papel);
  };

  const logout = () => {
    autenticacaoService.logout();
    setUsuario(null);
    setPapel(null);
  };

  const temPermissao = (permissoesRequeridas: string[]): boolean => {
    if (!papel) return false;
    
    const permissoesDoPapel = PERMISSOES_POR_PAPEL[papel] || [];
    return permissoesRequeridas.some(p => permissoesDoPapel.includes(p));
  };

  const value: AuthContextType = {
    usuario,
    papel,
    carregando,
    autenticado: !!usuario,
    login,
    logout,
    temPermissao,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

// ============================================================================
// HOOK
// ============================================================================

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth deve ser usado dentro de um AuthProvider');
  }
  return context;
}

export default AuthContext;
