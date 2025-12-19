using FolhadePagamento.Dominio.Consignados;
using FolhadePagamento.Dominio.Entidades;
using FolhadePagamento.Dominio.Fgts;
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
/// 4. Calcular FGTS (encargo patronal, Base = Bruto)
/// 5. Calcular Consignados (respeita margem e ordem)
/// 6. Calcular Líquido (NÃO inclui FGTS - é encargo patronal)
/// 7. Criar Resultado Imutável
/// </summary>
public sealed class CalculadoraFolha
{
    private readonly CalculadoraInss? _calculadoraInss;
    private readonly CalculadoraIrrf? _calculadoraIrrf;
    private readonly CalculadoraFgts? _calculadoraFgts;
    private readonly CalculadoraConsignados? _calculadoraConsignados;

    /// <summary>
    /// Cria uma calculadora de folha SEM INSS/IRRF/FGTS/Consignados (retrocompatibilidade).
    /// </summary>
    public CalculadoraFolha()
    {
        _calculadoraInss = null;
        _calculadoraIrrf = null;
        _calculadoraFgts = null;
        _calculadoraConsignados = null;
    }

    /// <summary>
    /// Cria uma calculadora de folha COM suporte a INSS (retrocompatibilidade v0.3).
    /// </summary>
    /// <param name="calculadoraInss">Calculadora de INSS configurada com tabelas</param>
    public CalculadoraFolha(CalculadoraInss calculadoraInss)
    {
        _calculadoraInss = calculadoraInss ?? throw new ArgumentNullException(nameof(calculadoraInss));
        _calculadoraIrrf = null;
        _calculadoraFgts = null;
        _calculadoraConsignados = null;
    }

    /// <summary>
    /// Cria uma calculadora de folha COM suporte a INSS e IRRF (retrocompatibilidade v0.4).
    /// </summary>
    /// <param name="calculadoraInss">Calculadora de INSS configurada com tabelas</param>
    /// <param name="calculadoraIrrf">Calculadora de IRRF configurada com tabelas</param>
    public CalculadoraFolha(CalculadoraInss calculadoraInss, CalculadoraIrrf calculadoraIrrf)
    {
        _calculadoraInss = calculadoraInss ?? throw new ArgumentNullException(nameof(calculadoraInss));
        _calculadoraIrrf = calculadoraIrrf ?? throw new ArgumentNullException(nameof(calculadoraIrrf));
        _calculadoraFgts = null;
        _calculadoraConsignados = null;
    }

    /// <summary>
    /// Cria uma calculadora de folha COM suporte a INSS, IRRF e FGTS (retrocompatibilidade v0.6).
    /// </summary>
    /// <param name="calculadoraInss">Calculadora de INSS configurada com tabelas</param>
    /// <param name="calculadoraIrrf">Calculadora de IRRF configurada com tabelas</param>
    /// <param name="calculadoraFgts">Calculadora de FGTS configurada com tabelas</param>
    public CalculadoraFolha(CalculadoraInss calculadoraInss, CalculadoraIrrf calculadoraIrrf, CalculadoraFgts calculadoraFgts)
    {
        _calculadoraInss = calculadoraInss ?? throw new ArgumentNullException(nameof(calculadoraInss));
        _calculadoraIrrf = calculadoraIrrf ?? throw new ArgumentNullException(nameof(calculadoraIrrf));
        _calculadoraFgts = calculadoraFgts ?? throw new ArgumentNullException(nameof(calculadoraFgts));
        _calculadoraConsignados = null;
    }

    /// <summary>
    /// Cria uma calculadora de folha COM suporte completo: INSS, IRRF, FGTS e Consignados.
    /// </summary>
    /// <param name="calculadoraInss">Calculadora de INSS configurada com tabelas</param>
    /// <param name="calculadoraIrrf">Calculadora de IRRF configurada com tabelas</param>
    /// <param name="calculadoraFgts">Calculadora de FGTS configurada com tabelas</param>
    /// <param name="calculadoraConsignados">Calculadora de consignados</param>
    public CalculadoraFolha(
        CalculadoraInss calculadoraInss,
        CalculadoraIrrf calculadoraIrrf,
        CalculadoraFgts calculadoraFgts,
        CalculadoraConsignados calculadoraConsignados)
    {
        _calculadoraInss = calculadoraInss ?? throw new ArgumentNullException(nameof(calculadoraInss));
        _calculadoraIrrf = calculadoraIrrf ?? throw new ArgumentNullException(nameof(calculadoraIrrf));
        _calculadoraFgts = calculadoraFgts ?? throw new ArgumentNullException(nameof(calculadoraFgts));
        _calculadoraConsignados = calculadoraConsignados ?? throw new ArgumentNullException(nameof(calculadoraConsignados));
    }

    /// <summary>
    /// Calcula a folha de pagamento para um funcionário em uma competência.
    /// 
    /// PIPELINE:
    /// - Etapa 1: Coletar Proventos (SalarioBruto = SalarioBase)
    /// - Etapa 2: Calcular INSS (progressivo, se calculadora configurada)
    /// - Etapa 3: Calcular IRRF (progressivo, Base = Bruto - INSS)
    /// - Etapa 4: Calcular FGTS (encargo patronal, Base = Bruto)
    /// - Etapa 5: Calcular Consignados (respeitando margem)
    /// - Etapa 6: Criar Resultado Imutável
    /// </summary>
    /// <param name="funcionario">O funcionário para calcular a folha</param>
    /// <param name="competencia">O período de competência (ano-mês)</param>
    /// <param name="timestampCalculo">Timestamp do cálculo (passado explicitamente para determinismo)</param>
    /// <param name="numeroDependentes">Número de dependentes para dedução no IRRF (padrão: 0)</param>
    /// <param name="ehAprendiz">Se o funcionário é aprendiz (alíquota FGTS de 2%)</param>
    /// <returns>Resultado imutável do cálculo</returns>
    public ResultadoCalculo Calcular(
        Funcionario funcionario,
        Competencia competencia,
        DateTime timestampCalculo,
        int numeroDependentes = 0,
        bool ehAprendiz = false)
    {
        // Chama sobrecarga sem consignados (retrocompatibilidade)
        return Calcular(
            funcionario,
            competencia,
            timestampCalculo,
            Array.Empty<ContratoConsignado>(),
            numeroDependentes,
            ehAprendiz);
    }

    /// <summary>
    /// Calcula a folha de pagamento para um funcionário em uma competência, incluindo consignados.
    /// 
    /// PIPELINE:
    /// - Etapa 1: Coletar Proventos (SalarioBruto = SalarioBase)
    /// - Etapa 2: Calcular INSS (progressivo, se calculadora configurada)
    /// - Etapa 3: Calcular IRRF (progressivo, Base = Bruto - INSS)
    /// - Etapa 4: Calcular FGTS (encargo patronal, Base = Bruto)
    /// - Etapa 5: Calcular Consignados (respeitando margem do líquido)
    /// - Etapa 6: Criar Resultado Imutável
    /// </summary>
    /// <param name="funcionario">O funcionário para calcular a folha</param>
    /// <param name="competencia">O período de competência (ano-mês)</param>
    /// <param name="timestampCalculo">Timestamp do cálculo (passado explicitamente para determinismo)</param>
    /// <param name="contratosConsignados">Lista de contratos de consignados ativos</param>
    /// <param name="numeroDependentes">Número de dependentes para dedução no IRRF (padrão: 0)</param>
    /// <param name="ehAprendiz">Se o funcionário é aprendiz (alíquota FGTS de 2%)</param>
    /// <returns>Resultado imutável do cálculo</returns>
    public ResultadoCalculo Calcular(
        Funcionario funcionario,
        Competencia competencia,
        DateTime timestampCalculo,
        IEnumerable<ContratoConsignado> contratosConsignados,
        int numeroDependentes = 0,
        bool ehAprendiz = false)
    {
        // Validação
        if (funcionario is null)
            throw new ArgumentNullException(nameof(funcionario));

        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        if (contratosConsignados is null)
            throw new ArgumentNullException(nameof(contratosConsignados));

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

        // ===== ETAPA 4: Calcular FGTS (Encargo Patronal) =====
        // IMPORTANTE: FGTS NÃO desconta do funcionário
        // Base do FGTS = Salário Bruto
        Dinheiro valorFgts;
        ResultadoCalculoFgts? detalheFgts;

        if (_calculadoraFgts is not null && _calculadoraFgts.ExisteTabelaVigente(competencia))
        {
            detalheFgts = _calculadoraFgts.Calcular(salarioBruto, competencia, ehAprendiz);
            valorFgts = detalheFgts.ValorFgts;
        }
        else
        {
            // Sem FGTS (sem tabela configurada ou vigente)
            valorFgts = Dinheiro.Zero;
            detalheFgts = null;
        }

        // ===== ETAPA 5: Calcular Consignados =====
        // Base para consignados = Salário líquido ANTES dos consignados
        // Líquido parcial = Bruto - INSS - IRRF
        var liquidoAntesConsignados = salarioBruto.Subtrair(valorInss).Subtrair(valorIrrf);
        
        Dinheiro valorConsignados;
        ResultadoCalculoConsignados? detalheConsignados;

        if (_calculadoraConsignados is not null)
        {
            detalheConsignados = _calculadoraConsignados.Calcular(
                liquidoAntesConsignados,
                contratosConsignados,
                competencia);
            valorConsignados = detalheConsignados.TotalDescontado;
        }
        else
        {
            // Sem calculadora de consignados - retorna vazio
            valorConsignados = Dinheiro.Zero;
            detalheConsignados = null;
        }

        // ===== ETAPA 6: Criar Resultado Imutável =====
        var resultado = ResultadoCalculo.Criar(
            funcionarioId: funcionario.Id,
            competencia: competencia,
            salarioBruto: salarioBruto,
            valorInss: valorInss,
            detalheInss: detalheInss,
            valorIrrf: valorIrrf,
            detalheIrrf: detalheIrrf,
            valorConsignados: valorConsignados,
            detalheConsignados: detalheConsignados,
            valorFgts: valorFgts,
            detalheFgts: detalheFgts,
            outrosDescontos: Dinheiro.Zero,
            calculadoEm: timestampCalculo);

        return resultado;
    }
}
