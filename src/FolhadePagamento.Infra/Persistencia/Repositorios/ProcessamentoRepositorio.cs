using FolhadePagamento.Aplicacao.Portas;
using FolhadePagamento.Infra.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FolhadePagamento.Infra.Persistencia.Repositorios;

/// <summary>
/// Implementação do repositório de processamentos.
/// 
/// REGRAS IMPORTANTES:
/// - Este repositório apenas persiste e consulta dados
/// - Nenhuma regra de negócio é executada aqui
/// - Valores já vêm calculados do Core
/// </summary>
public class ProcessamentoRepositorio : IProcessamentoRepositorio
{
    private readonly FolhaDbContext _contexto;

    public ProcessamentoRepositorio(FolhaDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    // ========================================================================
    // GRAVAÇÃO
    // ========================================================================

    public async Task SalvarProcessamentoAsync(
        ProcessamentoPersistencia processamento,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processamento);

        // Criar entidade de processamento
        var processamentoDb = new ProcessamentoVersaoDb
        {
            ProcessamentoVersaoId = processamento.ProcessamentoVersaoId,
            FuncionarioId = processamento.FuncionarioId,
            CompetenciaAno = processamento.CompetenciaAno,
            CompetenciaMes = processamento.CompetenciaMes,
            VersaoNumero = processamento.VersaoNumero,
            VersaoAnteriorId = processamento.VersaoAnteriorId,
            Status = processamento.Status,
            IniciadoEm = processamento.IniciadoEm,
            FinalizadoEm = processamento.FinalizadoEm,
            MotivoReprocessamento = processamento.MotivoReprocessamento,
            DescricaoReprocessamento = processamento.DescricaoReprocessamento,
            UsuarioId = processamento.UsuarioId,
            HashResultado = processamento.HashResultado,
            CriadoEm = DateTime.UtcNow
        };

        // Criar entidade de resultado
        var resultadoDb = new ResultadoCalculoDb
        {
            ResultadoCalculoId = processamento.Resultado.ResultadoCalculoId,
            ProcessamentoVersaoId = processamento.ProcessamentoVersaoId,
            SalarioBruto = processamento.Resultado.SalarioBruto,
            ValorInss = processamento.Resultado.ValorInss,
            ValorIrrf = processamento.Resultado.ValorIrrf,
            ValorFgts = processamento.Resultado.ValorFgts,
            ValorConsignados = processamento.Resultado.ValorConsignados,
            TotalDescontos = processamento.Resultado.TotalDescontos,
            SalarioLiquido = processamento.Resultado.SalarioLiquido,
            TotalEncargosPatronais = processamento.Resultado.TotalEncargosPatronais,
            CustoTotalEmpregador = processamento.Resultado.CustoTotalEmpregador,
            CalculadoEm = processamento.Resultado.CalculadoEm
        };

        // Adicionar entidades principais
        await _contexto.ProcessamentosVersao.AddAsync(processamentoDb, cancellationToken);
        await _contexto.ResultadosCalculo.AddAsync(resultadoDb, cancellationToken);

        // Adicionar detalhes se existirem
        if (processamento.Resultado.DetalheInss is not null)
        {
            var detalhe = processamento.Resultado.DetalheInss;
            await _contexto.DetalhesInss.AddAsync(new DetalheInssDb
            {
                DetalheInssId = detalhe.DetalheInssId,
                ResultadoCalculoId = processamento.Resultado.ResultadoCalculoId,
                BaseCalculo = detalhe.BaseCalculo,
                TabelaIdUsada = detalhe.TabelaIdUsada,
                AliquotaEfetiva = detalhe.AliquotaEfetiva,
                TetoAplicado = detalhe.TetoAplicado,
                ContribuicaoPorFaixaJson = detalhe.ContribuicaoPorFaixaJson
            }, cancellationToken);
        }

        if (processamento.Resultado.DetalheIrrf is not null)
        {
            var detalhe = processamento.Resultado.DetalheIrrf;
            await _contexto.DetalhesIrrf.AddAsync(new DetalheIrrfDb
            {
                DetalheIrrfId = detalhe.DetalheIrrfId,
                ResultadoCalculoId = processamento.Resultado.ResultadoCalculoId,
                BaseCalculo = detalhe.BaseCalculo,
                DeducaoInss = detalhe.DeducaoInss,
                NumeroDependentes = detalhe.NumeroDependentes,
                DeducaoPorDependente = detalhe.DeducaoPorDependente,
                TabelaIdUsada = detalhe.TabelaIdUsada,
                FaixaAplicada = detalhe.FaixaAplicada,
                AliquotaAplicada = detalhe.AliquotaAplicada,
                ParcelaDedutivelUsada = detalhe.ParcelaDedutivelUsada,
                Isento = detalhe.Isento
            }, cancellationToken);
        }

        if (processamento.Resultado.DetalheFgts is not null)
        {
            var detalhe = processamento.Resultado.DetalheFgts;
            await _contexto.DetalhesFgts.AddAsync(new DetalheFgtsDb
            {
                DetalheFgtsId = detalhe.DetalheFgtsId,
                ResultadoCalculoId = processamento.Resultado.ResultadoCalculoId,
                BaseCalculo = detalhe.BaseCalculo,
                TabelaIdUsada = detalhe.TabelaIdUsada,
                AliquotaAplicada = detalhe.AliquotaAplicada,
                TipoContribuinte = detalhe.TipoContribuinte
            }, cancellationToken);
        }

        if (processamento.Resultado.DetalheConsignados is not null)
        {
            var detalhe = processamento.Resultado.DetalheConsignados;
            await _contexto.DetalhesConsignados.AddAsync(new DetalheConsignadosDb
            {
                DetalheConsignadosId = detalhe.DetalheConsignadosId,
                ResultadoCalculoId = processamento.Resultado.ResultadoCalculoId,
                SalarioBaseConsiderado = detalhe.SalarioBaseConsiderado,
                PercentualMargem = detalhe.PercentualMargem,
                MargemTotal = detalhe.MargemTotal,
                MargemUtilizada = detalhe.MargemUtilizada,
                MargemDisponivel = detalhe.MargemDisponivel,
                TotalContratosAtivos = detalhe.TotalContratosAtivos,
                DescontosJson = detalhe.DescontosJson
            }, cancellationToken);
        }

        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task MarcarComoSuperadoAsync(
        Guid processamentoVersaoId,
        DateTime superadoEm,
        CancellationToken cancellationToken = default)
    {
        var processamento = await _contexto.ProcessamentosVersao
            .FirstOrDefaultAsync(p => p.ProcessamentoVersaoId == processamentoVersaoId, cancellationToken);

        if (processamento is not null)
        {
            processamento.Status = "Superado";
            processamento.SuperadoEm = superadoEm;
            await _contexto.SaveChangesAsync(cancellationToken);
        }
    }

    // ========================================================================
    // CONSULTA
    // ========================================================================

    public async Task<ProcessamentoConsulta?> ObterPorIdAsync(
        Guid processamentoVersaoId,
        CancellationToken cancellationToken = default)
    {
        var processamento = await _contexto.ProcessamentosVersao
            .Include(p => p.Funcionario)
            .Include(p => p.Resultado)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProcessamentoVersaoId == processamentoVersaoId, cancellationToken);

        if (processamento is null || processamento.Resultado is null)
            return null;

        return MapearParaConsulta(processamento);
    }

    public async Task<ProcessamentoConsulta?> ObterVersaoAtualAsync(
        Guid funcionarioId,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default)
    {
        var processamento = await _contexto.ProcessamentosVersao
            .Include(p => p.Funcionario)
            .Include(p => p.Resultado)
            .AsNoTracking()
            .Where(p => p.FuncionarioId == funcionarioId
                && p.CompetenciaAno == competenciaAno
                && p.CompetenciaMes == competenciaMes
                && p.Status == "Finalizado")
            .OrderByDescending(p => p.VersaoNumero)
            .FirstOrDefaultAsync(cancellationToken);

        if (processamento is null || processamento.Resultado is null)
            return null;

        return MapearParaConsulta(processamento);
    }

    public async Task<IReadOnlyList<ProcessamentoResumoConsulta>> ObterHistoricoVersoesAsync(
        Guid funcionarioId,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default)
    {
        var processamentos = await _contexto.ProcessamentosVersao
            .Include(p => p.Funcionario)
            .Include(p => p.Resultado)
            .AsNoTracking()
            .Where(p => p.FuncionarioId == funcionarioId
                && p.CompetenciaAno == competenciaAno
                && p.CompetenciaMes == competenciaMes)
            .OrderByDescending(p => p.VersaoNumero)
            .ToListAsync(cancellationToken);

        return processamentos
            .Where(p => p.Resultado is not null)
            .Select(MapearParaResumo)
            .ToList();
    }

    public async Task<int> ObterProximoNumeroVersaoAsync(
        Guid funcionarioId,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default)
    {
        var maxVersao = await _contexto.ProcessamentosVersao
            .Where(p => p.FuncionarioId == funcionarioId
                && p.CompetenciaAno == competenciaAno
                && p.CompetenciaMes == competenciaMes)
            .MaxAsync(p => (int?)p.VersaoNumero, cancellationToken);

        return (maxVersao ?? 0) + 1;
    }

    public async Task<bool> ExisteProcessamentoAsync(
        Guid funcionarioId,
        int competenciaAno,
        int competenciaMes,
        CancellationToken cancellationToken = default)
    {
        return await _contexto.ProcessamentosVersao
            .AnyAsync(p => p.FuncionarioId == funcionarioId
                && p.CompetenciaAno == competenciaAno
                && p.CompetenciaMes == competenciaMes,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProcessamentoResumoConsulta>> ListarPorCompetenciaAsync(
        int competenciaAno,
        int competenciaMes,
        bool apenasAtual = true,
        CancellationToken cancellationToken = default)
    {
        var query = _contexto.ProcessamentosVersao
            .Include(p => p.Funcionario)
            .Include(p => p.Resultado)
            .AsNoTracking()
            .Where(p => p.CompetenciaAno == competenciaAno
                && p.CompetenciaMes == competenciaMes);

        if (apenasAtual)
        {
            query = query.Where(p => p.Status == "Finalizado");
        }

        var processamentos = await query
            .OrderBy(p => p.Funcionario!.Nome)
            .ThenByDescending(p => p.VersaoNumero)
            .ToListAsync(cancellationToken);

        if (apenasAtual)
        {
            // Apenas a versão mais recente de cada funcionário
            processamentos = processamentos
                .GroupBy(p => p.FuncionarioId)
                .Select(g => g.First())
                .ToList();
        }

        return processamentos
            .Where(p => p.Resultado is not null)
            .Select(MapearParaResumo)
            .ToList();
    }

    // ========================================================================
    // MAPEADORES PRIVADOS
    // ========================================================================

    private static ProcessamentoConsulta MapearParaConsulta(ProcessamentoVersaoDb processamento)
    {
        return new ProcessamentoConsulta
        {
            ProcessamentoVersaoId = processamento.ProcessamentoVersaoId,
            FuncionarioId = processamento.FuncionarioId,
            FuncionarioNome = processamento.Funcionario?.Nome ?? "N/A",
            CompetenciaAno = processamento.CompetenciaAno,
            CompetenciaMes = processamento.CompetenciaMes,
            VersaoNumero = processamento.VersaoNumero,
            VersaoAnteriorId = processamento.VersaoAnteriorId,
            Status = processamento.Status,
            IniciadoEm = processamento.IniciadoEm,
            FinalizadoEm = processamento.FinalizadoEm,
            SuperadoEm = processamento.SuperadoEm,
            MotivoReprocessamento = processamento.MotivoReprocessamento,
            DescricaoReprocessamento = processamento.DescricaoReprocessamento,
            Resultado = new ResultadoConsulta
            {
                SalarioBruto = processamento.Resultado!.SalarioBruto,
                ValorInss = processamento.Resultado.ValorInss,
                ValorIrrf = processamento.Resultado.ValorIrrf,
                ValorFgts = processamento.Resultado.ValorFgts,
                ValorConsignados = processamento.Resultado.ValorConsignados,
                TotalDescontos = processamento.Resultado.TotalDescontos,
                SalarioLiquido = processamento.Resultado.SalarioLiquido,
                TotalEncargosPatronais = processamento.Resultado.TotalEncargosPatronais,
                CustoTotalEmpregador = processamento.Resultado.CustoTotalEmpregador,
                CalculadoEm = processamento.Resultado.CalculadoEm
            }
        };
    }

    private static ProcessamentoResumoConsulta MapearParaResumo(ProcessamentoVersaoDb processamento)
    {
        return new ProcessamentoResumoConsulta
        {
            ProcessamentoVersaoId = processamento.ProcessamentoVersaoId,
            FuncionarioId = processamento.FuncionarioId,
            FuncionarioNome = processamento.Funcionario?.Nome ?? "N/A",
            CompetenciaAno = processamento.CompetenciaAno,
            CompetenciaMes = processamento.CompetenciaMes,
            VersaoNumero = processamento.VersaoNumero,
            Status = processamento.Status,
            IniciadoEm = processamento.IniciadoEm,
            FinalizadoEm = processamento.FinalizadoEm,
            MotivoReprocessamento = processamento.MotivoReprocessamento,
            SalarioLiquido = processamento.Resultado?.SalarioLiquido ?? 0
        };
    }
}
