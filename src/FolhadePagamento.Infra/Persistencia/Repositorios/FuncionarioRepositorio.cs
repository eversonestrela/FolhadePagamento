using FolhadePagamento.Aplicacao.Portas;
using FolhadePagamento.Infra.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FolhadePagamento.Infra.Persistencia.Repositorios;

/// <summary>
/// Implementação do repositório de funcionários.
/// </summary>
public class FuncionarioRepositorio : IFuncionarioRepositorio
{
    private readonly FolhaDbContext _contexto;

    public FuncionarioRepositorio(FolhaDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    // ========================================================================
    // GRAVAÇÃO
    // ========================================================================

    public async Task SalvarAsync(
        FuncionarioPersistencia funcionario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(funcionario);

        var funcionarioDb = new FuncionarioDb
        {
            FuncionarioId = funcionario.FuncionarioId,
            Nome = funcionario.Nome,
            SalarioBase = funcionario.SalarioBase,
            DataAdmissao = funcionario.DataAdmissao,
            Ativo = funcionario.Ativo,
            CriadoEm = funcionario.CriadoEm
        };

        await _contexto.Funcionarios.AddAsync(funcionarioDb, cancellationToken);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(
        Guid funcionarioId,
        FuncionarioAtualizacao atualizacao,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(atualizacao);

        var funcionario = await _contexto.Funcionarios
            .FirstOrDefaultAsync(f => f.FuncionarioId == funcionarioId, cancellationToken);

        if (funcionario is null)
            return;

        if (atualizacao.Nome is not null)
            funcionario.Nome = atualizacao.Nome;

        if (atualizacao.SalarioBase.HasValue)
            funcionario.SalarioBase = atualizacao.SalarioBase.Value;

        if (atualizacao.DataAdmissao.HasValue)
            funcionario.DataAdmissao = atualizacao.DataAdmissao.Value;

        funcionario.AtualizadoEm = atualizacao.AtualizadoEm;

        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task DesativarAsync(
        Guid funcionarioId,
        CancellationToken cancellationToken = default)
    {
        var funcionario = await _contexto.Funcionarios
            .FirstOrDefaultAsync(f => f.FuncionarioId == funcionarioId, cancellationToken);

        if (funcionario is not null)
        {
            funcionario.Ativo = false;
            funcionario.AtualizadoEm = DateTime.UtcNow;
            await _contexto.SaveChangesAsync(cancellationToken);
        }
    }

    // ========================================================================
    // CONSULTA
    // ========================================================================

    public async Task<FuncionarioConsulta?> ObterPorIdAsync(
        Guid funcionarioId,
        CancellationToken cancellationToken = default)
    {
        var funcionario = await _contexto.Funcionarios
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FuncionarioId == funcionarioId, cancellationToken);

        if (funcionario is null)
            return null;

        return MapearParaConsulta(funcionario);
    }

    public async Task<IReadOnlyList<FuncionarioConsulta>> ListarAtivosAsync(
        CancellationToken cancellationToken = default)
    {
        var funcionarios = await _contexto.Funcionarios
            .AsNoTracking()
            .Where(f => f.Ativo)
            .OrderBy(f => f.Nome)
            .ToListAsync(cancellationToken);

        return funcionarios.Select(MapearParaConsulta).ToList();
    }

    public async Task<bool> ExisteEAtivoAsync(
        Guid funcionarioId,
        CancellationToken cancellationToken = default)
    {
        return await _contexto.Funcionarios
            .AnyAsync(f => f.FuncionarioId == funcionarioId && f.Ativo, cancellationToken);
    }

    // ========================================================================
    // MAPEADORES PRIVADOS
    // ========================================================================

    private static FuncionarioConsulta MapearParaConsulta(FuncionarioDb funcionario)
    {
        return new FuncionarioConsulta
        {
            FuncionarioId = funcionario.FuncionarioId,
            Nome = funcionario.Nome,
            SalarioBase = funcionario.SalarioBase,
            DataAdmissao = funcionario.DataAdmissao,
            Ativo = funcionario.Ativo,
            CriadoEm = funcionario.CriadoEm,
            AtualizadoEm = funcionario.AtualizadoEm
        };
    }
}
