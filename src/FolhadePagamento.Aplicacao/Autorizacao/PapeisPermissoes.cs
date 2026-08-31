namespace FolhadePagamento.Aplicacao.Autorizacao;

/// <summary>
/// Papéis disponíveis no sistema.
/// 
/// MODELO RBAC:
/// - Administrador: Acesso total
/// - Operador: Processar e consultar
/// - Consulta: Apenas leitura
/// </summary>
public static class Papeis
{
    /// <summary>
    /// Acesso total ao sistema.
    /// Pode: CRUD funcionários, processar, reprocessar, cancelar lote.
    /// </summary>
    public const string Administrador = "Administrador";

    /// <summary>
    /// Acesso operacional ao sistema.
    /// Pode: Processar folha, consultar lotes e processamentos.
    /// NÃO pode: Cancelar lote, gerenciar funcionários.
    /// </summary>
    public const string Operador = "Operador";

    /// <summary>
    /// Acesso somente leitura.
    /// Pode: Consultar funcionários, lotes e processamentos (GET).
    /// NÃO pode: Modificar dados.
    /// </summary>
    public const string Consulta = "Consulta";

    /// <summary>
    /// Lista de todos os papéis disponíveis.
    /// </summary>
    public static readonly IReadOnlyList<string> TodosOsPapeis = new[]
    {
        Administrador,
        Operador,
        Consulta
    };
}

/// <summary>
/// Permissões granulares do sistema.
/// Cada permissão representa uma ação específica.
/// </summary>
public static class Permissoes
{
    // Funcionários
    public const string FuncionarioConsultar = "funcionario:consultar";
    public const string FuncionarioCriar = "funcionario:criar";
    public const string FuncionarioAtualizar = "funcionario:atualizar";
    public const string FuncionarioDesativar = "funcionario:desativar";

    // Processamentos
    public const string ProcessamentoConsultar = "processamento:consultar";
    public const string ProcessamentoExecutar = "processamento:executar";

    // Lotes
    public const string LoteConsultar = "lote:consultar";
    public const string LoteCriar = "lote:criar";
    public const string LoteCancelar = "lote:cancelar";
}

/// <summary>
/// Mapeamento de papéis para permissões.
/// </summary>
public static class MapeamentoPapelPermissao
{
    /// <summary>
    /// Obtém todas as permissões de um papel.
    /// </summary>
    public static IReadOnlyList<string> ObterPermissoes(string papel)
    {
        return papel switch
        {
            Papeis.Administrador => PermissoesAdministrador,
            Papeis.Operador => PermissoesOperador,
            Papeis.Consulta => PermissoesConsulta,
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Verifica se um papel tem determinada permissão.
    /// </summary>
    public static bool TemPermissao(string papel, string permissao)
    {
        var permissoes = ObterPermissoes(papel);
        return permissoes.Contains(permissao);
    }

    /// <summary>
    /// Verifica se algum dos papéis tem a permissão.
    /// </summary>
    public static bool TemPermissao(IEnumerable<string> papeis, string permissao)
    {
        return papeis.Any(p => TemPermissao(p, permissao));
    }

    /// <summary>
    /// Permissões do papel Administrador - acesso total.
    /// </summary>
    private static readonly IReadOnlyList<string> PermissoesAdministrador = new[]
    {
        // Funcionários
        Permissoes.FuncionarioConsultar,
        Permissoes.FuncionarioCriar,
        Permissoes.FuncionarioAtualizar,
        Permissoes.FuncionarioDesativar,

        // Processamentos
        Permissoes.ProcessamentoConsultar,
        Permissoes.ProcessamentoExecutar,

        // Lotes
        Permissoes.LoteConsultar,
        Permissoes.LoteCriar,
        Permissoes.LoteCancelar
    };

    /// <summary>
    /// Permissões do papel Operador - processar e consultar.
    /// </summary>
    private static readonly IReadOnlyList<string> PermissoesOperador = new[]
    {
        // Funcionários - apenas consulta
        Permissoes.FuncionarioConsultar,

        // Processamentos
        Permissoes.ProcessamentoConsultar,
        Permissoes.ProcessamentoExecutar,

        // Lotes - criar e consultar, mas NÃO cancelar
        Permissoes.LoteConsultar,
        Permissoes.LoteCriar
    };

    /// <summary>
    /// Permissões do papel Consulta - somente leitura.
    /// </summary>
    private static readonly IReadOnlyList<string> PermissoesConsulta = new[]
    {
        Permissoes.FuncionarioConsultar,
        Permissoes.ProcessamentoConsultar,
        Permissoes.LoteConsultar
    };
}
