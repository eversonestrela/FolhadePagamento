/**
 * Tipos base da API de Folha de Pagamento.
 * 
 * IMPORTANTE: Front-end NÃO conhece regras de cálculo.
 * Apenas exibe valores recebidos da API.
 */

// ============================================================================
// AUTENTICAÇÃO
// ============================================================================

export interface LoginRequest {
  usuario: string;
  senha: string;
}

export interface LoginResponse {
  token: string;
  expiraEm: string;
  tipoToken: string;
  papel?: string;
}

export interface UsuarioAutenticado {
  usuarioId: string;
  nome: string;
  papeis: string[];
  valido: boolean;
  verificadoEm: string;
}

// ============================================================================
// FUNCIONÁRIOS
// ============================================================================

export interface Funcionario {
  funcionarioId: string;
  nome: string;
  salarioBase: number;
  dataAdmissao: string;
  ativo: boolean;
  criadoEm: string;
}

export interface CriarFuncionarioRequest {
  nome: string;
  salarioBase: number;
  dataAdmissao: string;
}

export interface AtualizarFuncionarioRequest {
  nome?: string;
  salarioBase?: number;
  dataAdmissao?: string;
}

// ============================================================================
// PROCESSAMENTOS
// ============================================================================

export interface ProcessamentoResumo {
  processamentoVersaoId: string;
  funcionarioId: string;
  nomeFuncionario?: string;
  competenciaAno: number;
  competenciaMes: number;
  versaoNumero: number;
  status: string;
  salarioLiquido: number;
  finalizadoEm?: string;
}

export interface Processamento {
  processamentoVersaoId: string;
  funcionarioId: string;
  nomeFuncionario?: string;
  competenciaAno: number;
  competenciaMes: number;
  versaoNumero: number;
  versaoAnteriorId?: string;
  status: string;
  iniciadoEm: string;
  finalizadoEm?: string;
  motivoReprocessamento?: string;
  resultado?: ResultadoCalculo;
}

export interface ResultadoCalculo {
  resultadoCalculoId: string;
  salarioBruto: number;
  valorInss: number;
  valorIrrf: number;
  valorFgts: number;
  valorConsignados: number;
  totalDescontos: number;
  salarioLiquido: number;
  totalEncargosPatronais: number;
  custoTotalEmpregador: number;
  calculadoEm: string;
}

export interface ProcessarFolhaRequest {
  funcionarioId: string;
  competenciaAno: number;
  competenciaMes: number;
  numeroDependentes: number;
}

// ============================================================================
// LOTES
// ============================================================================

export interface LoteResumo {
  loteId: string;
  competenciaAno: number;
  competenciaMes: number;
  status: string;
  totalItens: number;
  itensConcluidos: number;
  itensComFalha: number;
  percentualConcluido: number;
  criadoEm: string;
}

export interface Lote {
  loteId: string;
  competenciaAno: number;
  competenciaMes: number;
  status: string;
  totalItens: number;
  itensConcluidos: number;
  itensComFalha: number;
  itensIgnorados: number;
  criadoEm: string;
  iniciadoEm?: string;
  concluidoEm?: string;
  percentualConcluido: number;
  duracaoTotal?: string;
  usuarioId?: string;
  observacao?: string;
}

export interface ItemLote {
  itemLoteId: string;
  loteId: string;
  funcionarioId: string;
  nomeFuncionario?: string;
  status: string;
  processamentoVersaoId?: string;
  mensagemErro?: string;
  tentativas: number;
  iniciadoEm?: string;
  concluidoEm?: string;
}

export interface ProgressoLote {
  loteId: string;
  status: string;
  totalItens: number;
  pendentes: number;
  emProcessamento: number;
  concluidos: number;
  comFalha: number;
  ignorados: number;
  percentualConcluido: number;
  iniciadoEm?: string;
  concluidoEm?: string;
  duracaoTotal?: string;
}

export interface CriarLoteRequest {
  competenciaAno: number;
  competenciaMes: number;
  funcionarioIds?: string[];
  observacao?: string;
}

// ============================================================================
// ERROS
// ============================================================================

export interface ErroResponse {
  mensagem: string;
  detalhe?: string;
  codigo?: string;
  timestamp: string;
}

// ============================================================================
// PAPÉIS E PERMISSÕES (RBAC)
// ============================================================================

export type Papel = 'Administrador' | 'Operador' | 'Consulta';

export interface PapelInfo {
  papel: string;
  permissoes: string[];
}
