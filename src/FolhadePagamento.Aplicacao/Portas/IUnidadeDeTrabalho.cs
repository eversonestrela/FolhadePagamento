namespace FolhadePagamento.Aplicacao.Portas;

/// <summary>
/// Interface para gerenciar transações de banco de dados.
/// Permite commits e rollbacks explícitos na camada de aplicação.
/// </summary>
public interface IUnidadeDeTrabalho : IDisposable
{
    /// <summary>
    /// Inicia uma nova transação.
    /// </summary>
    Task IniciarTransacaoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma todas as alterações pendentes.
    /// </summary>
    Task ConfirmarAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Descarta todas as alterações pendentes.
    /// </summary>
    Task ReverterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica se há uma transação ativa.
    /// </summary>
    bool TransacaoAtiva { get; }
}
