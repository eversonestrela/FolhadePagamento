namespace FolhadePagamento.Api.Autorizacao;

/// <summary>
/// Nomes das policies de autorização.
/// 
/// Cada policy representa uma permissão específica no sistema.
/// </summary>
public static class Policies
{
    // Funcionários
    public const string FuncionarioConsultar = "Policy.Funcionario.Consultar";
    public const string FuncionarioCriar = "Policy.Funcionario.Criar";
    public const string FuncionarioAtualizar = "Policy.Funcionario.Atualizar";
    public const string FuncionarioDesativar = "Policy.Funcionario.Desativar";

    // Processamentos
    public const string ProcessamentoConsultar = "Policy.Processamento.Consultar";
    public const string ProcessamentoExecutar = "Policy.Processamento.Executar";

    // Lotes
    public const string LoteConsultar = "Policy.Lote.Consultar";
    public const string LoteCriar = "Policy.Lote.Criar";
    public const string LoteCancelar = "Policy.Lote.Cancelar";
}
