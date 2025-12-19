using FolhadePagamento.Aplicacao.Portas;
using Microsoft.EntityFrameworkCore.Storage;

namespace FolhadePagamento.Infra.Persistencia.Repositorios;

/// <summary>
/// Implementação da Unidade de Trabalho para gerenciamento de transações.
/// Permite commits e rollbacks explícitos.
/// </summary>
public class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly FolhaDbContext _contexto;
    private IDbContextTransaction? _transacao;
    private bool _disposed;

    public UnidadeDeTrabalho(FolhaDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    public bool TransacaoAtiva => _transacao is not null;

    public async Task IniciarTransacaoAsync(CancellationToken cancellationToken = default)
    {
        if (_transacao is not null)
            throw new InvalidOperationException("Já existe uma transação ativa.");

        _transacao = await _contexto.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task ConfirmarAsync(CancellationToken cancellationToken = default)
    {
        if (_transacao is null)
            throw new InvalidOperationException("Não há transação ativa para confirmar.");

        try
        {
            await _contexto.SaveChangesAsync(cancellationToken);
            await _transacao.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transacao.DisposeAsync();
            _transacao = null;
        }
    }

    public async Task ReverterAsync(CancellationToken cancellationToken = default)
    {
        if (_transacao is null)
            throw new InvalidOperationException("Não há transação ativa para reverter.");

        try
        {
            await _transacao.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transacao.DisposeAsync();
            _transacao = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transacao?.Dispose();
            }

            _disposed = true;
        }
    }
}
