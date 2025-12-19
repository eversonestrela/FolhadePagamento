using FolhadePagamento.Dominio.Entidades;
using FolhadePagamento.Dominio.Inss;
using FolhadePagamento.Dominio.Irrf;
using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Folha;

/// <summary>
/// Serviço de Domínio responsável pelo cálculo da folha de pagamento.
/// 
/// GARANTIA DE DETERMINISMO:
/// - Mesmas entradas SEMPRE produzem mesmas saídas
/// - Sem uso de DateTime.Now
/// - Sem dependências externas (BD, HTTP, etc.)
/// - Sem efeitos colaterais
/// 
/// Esta é a engine central de cálculo do sistema.
/// Toda lógica de folha deve estar concentrada aqui.
/// 
/// PIPELINE DE CÁLCULO:
/// 1. Coletar Proventos (salário base + adicionais futuros)
/// 2. Calcular INSS (progressivo, conforme tabela vigente)
/// 3. Calcular IRRF (progressivo, Base = Bruto - INSS)
/// 4. Calcular outros descontos (consignados, benefícios, etc.)
/// 5. Calcular Líquido
/// 6. Criar Resultado Imutável
/// </summary>
public sealed class CalculadoraFolha
{
    private readonly CalculadoraInss? _calculadoraInss;
    private readonly CalculadoraIrrf? _calculadoraIrrf;

    /// <summary>
    /// Cria uma calculadora de folha SEM INSS/IRRF (retrocompatibilidade).
    /// </summary>
    public CalculadoraFolha()
    {
        _calculadoraInss = null;
        _calculadoraIrrf = null;
    }

    /// <summary>
    /// Cria uma calculadora de folha COM suporte a INSS (retrocompatibilidade v0.3).
    /// </summary>
    /// <param name="calculadoraInss">Calculadora de INSS configurada com tabelas</param>
    public CalculadoraFolha(CalculadoraInss calculadoraInss)
    {
        _calculadoraInss = calculadoraInss ?? throw new ArgumentNullException(nameof(calculadoraInss));
        _calculadoraIrrf = null;
    }

    /// <summary>
    /// Cria uma calculadora de folha COM suporte a INSS e IRRF.
    /// </summary>
    /// <param name="calculadoraInss">Calculadora de INSS configurada com tabelas</param>
    /// <param name="calculadoraIrrf">Calculadora de IRRF configurada com tabelas</param>
    public CalculadoraFolha(CalculadoraInss calculadoraInss, CalculadoraIrrf calculadoraIrrf)
    {
        _calculadoraInss = calculadoraInss ?? throw new ArgumentNullException(nameof(calculadoraInss));
        _calculadoraIrrf = calculadoraIrrf ?? throw new ArgumentNullException(nameof(calculadoraIrrf));
    }

    /// <summary>
    /// Calcula a folha de pagamento para um funcionário em uma competência.
    /// 
    /// PIPELINE:
    /// - Etapa 1: Coletar Proventos (SalarioBruto = SalarioBase)
    /// - Etapa 2: Calcular INSS (progressivo, se calculadora configurada)
    /// - Etapa 3: Calcular IRRF (progressivo, Base = Bruto - INSS)
    /// - Etapa 4: Calcular outros descontos (futuro: consignados)
    /// - Etapa 5: Criar Resultado Imutável
    /// </summary>
    /// <param name="funcionario">O funcionário para calcular a folha</param>
    /// <param name="competencia">O período de competência (ano-mês)</param>
    /// <param name="timestampCalculo">Timestamp do cálculo (passado explicitamente para determinismo)</param>
    /// <param name="numeroDependentes">Número de dependentes para dedução no IRRF (padrão: 0)</param>
    /// <returns>Resultado imutável do cálculo</returns>
    public ResultadoCalculo Calcular(
        Funcionario funcionario,
        Competencia competencia,
        DateTime timestampCalculo,
        int numeroDependentes = 0)
    {
        // Validação
        if (funcionario is null)
            throw new ArgumentNullException(nameof(funcionario));

        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        if (!funcionario.Ativo)
            throw new InvalidOperationException($"Não é possível calcular folha para funcionário inativo: {funcionario.Id}");

        // ===== ETAPA 1: Coletar Proventos =====
        var salarioBruto = funcionario.SalarioBase;

        // ===== ETAPA 2: Calcular INSS =====
        Dinheiro valorInss;
        ResultadoCalculoInss? detalheInss;

        if (_calculadoraInss is not null && _calculadoraInss.ExisteTabelaVigente(competencia))
        {
            detalheInss = _calculadoraInss.Calcular(salarioBruto, competencia);
            valorInss = detalheInss.ValorInss;
        }
        else
        {
            // Sem INSS (sem tabela configurada ou vigente)
            valorInss = Dinheiro.Zero;
            detalheInss = null;
        }

        // ===== ETAPA 3: Calcular IRRF =====
        // IMPORTANTE: Base do IRRF = Salário Bruto - INSS
        // O IRRF NÃO recalcula o INSS - apenas usa o valor já calculado
        Dinheiro valorIrrf;
        ResultadoCalculoIrrf? detalheIrrf;

        if (_calculadoraIrrf is not null && _calculadoraIrrf.ExisteTabelaVigente(competencia))
        {
            var baseIrrf = salarioBruto.Subtrair(valorInss);
            detalheIrrf = _calculadoraIrrf.Calcular(baseIrrf, competencia, numeroDependentes);
            valorIrrf = detalheIrrf.ValorIrrf;
        }
        else
        {
            // Sem IRRF (sem tabela configurada ou vigente)
            valorIrrf = Dinheiro.Zero;
            detalheIrrf = null;
        }

        // ===== ETAPA 4: Outros Descontos (futuro: consignados) =====
        var outrosDescontos = Dinheiro.Zero;

        // ===== ETAPA 5: Criar Resultado Imutável =====
        var resultado = ResultadoCalculo.Criar(
            funcionarioId: funcionario.Id,
            competencia: competencia,
            salarioBruto: salarioBruto,
            valorInss: valorInss,
            detalheInss: detalheInss,
            valorIrrf: valorIrrf,
            detalheIrrf: detalheIrrf,
            outrosDescontos: outrosDescontos,
            calculadoEm: timestampCalculo);

        return resultado;
    }
}
