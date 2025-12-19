using FolhadePagamento.Aplicacao.DTOs;
using FolhadePagamento.Dominio.Entidades;
using FolhadePagamento.Dominio.Folha;
using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Aplicacao.CasosDeUso;

/// <summary>
/// Caso de Uso: Processar Folha Básica para um único funcionário.
/// 
/// RESPONSABILIDADE:
/// - Validar DTOs de entrada
/// - Mapear DTOs para objetos de Domínio
/// - Delegar cálculo para Serviço de Domínio
/// - Mapear resultados de volta para DTOs de saída
/// 
/// NÃO CONTÉM:
/// - Lógica de negócio (tratada pela CalculadoraFolha)
/// - Acesso a banco de dados (usará repositórios no futuro)
/// - Dependências de framework
/// </summary>
public sealed class ProcessarFolhaBasicaCasoDeUso
{
    private readonly CalculadoraFolha _calculadora;

    public ProcessarFolhaBasicaCasoDeUso(CalculadoraFolha calculadora)
    {
        _calculadora = calculadora ?? throw new ArgumentNullException(nameof(calculadora));
    }

    /// <summary>
    /// Executa o caso de uso.
    /// </summary>
    /// <param name="entrada">Dados de entrada para cálculo da folha</param>
    /// <returns>Saída com resultados do cálculo</returns>
    public ProcessarFolhaBasicaSaida Executar(ProcessarFolhaBasicaEntrada entrada)
    {
        try
        {
            // 1. Validar entrada
            ValidarEntrada(entrada);

            // 2. Mapear DTO para objetos de Domínio
            var funcionarioId = FuncionarioId.De(entrada.FuncionarioId);
            var salarioBase = Dinheiro.DeDecimal(entrada.SalarioBase);
            var competencia = Competencia.Converter(entrada.Competencia);

            // 3. Criar entidade Funcionário
            // No futuro: será carregado do IFuncionarioRepositorio
            var funcionario = Funcionario.Criar(funcionarioId, entrada.NomeFuncionario, salarioBase);

            // 4. Delegar para Serviço de Domínio (CalculadoraFolha)
            var resultado = _calculadora.Calcular(
                funcionario,
                competencia,
                entrada.TimestampCalculo);

            // 5. Mapear resultado para DTO de saída
            return MapearParaSaida(resultado);
        }
        catch (Exception ex)
        {
            return new ProcessarFolhaBasicaSaida
            {
                FuncionarioId = entrada.FuncionarioId,
                Competencia = entrada.Competencia,
                Sucesso = false,
                MensagemErro = ex.Message
            };
        }
    }

    private static void ValidarEntrada(ProcessarFolhaBasicaEntrada entrada)
    {
        if (entrada is null)
            throw new ArgumentNullException(nameof(entrada));

        if (entrada.FuncionarioId == Guid.Empty)
            throw new ArgumentException("FuncionarioId é obrigatório");

        if (string.IsNullOrWhiteSpace(entrada.NomeFuncionario))
            throw new ArgumentException("NomeFuncionario é obrigatório");

        if (entrada.SalarioBase <= 0)
            throw new ArgumentException("SalarioBase deve ser maior que zero");

        if (string.IsNullOrWhiteSpace(entrada.Competencia))
            throw new ArgumentException("Competencia é obrigatória");

        if (entrada.TimestampCalculo == default)
            throw new ArgumentException("TimestampCalculo é obrigatório");
    }

    private static ProcessarFolhaBasicaSaida MapearParaSaida(ResultadoCalculo resultado)
    {
        return new ProcessarFolhaBasicaSaida
        {
            FuncionarioId = resultado.FuncionarioId.Valor,
            Competencia = resultado.Competencia.ToString(),
            SalarioBruto = resultado.SalarioBruto.Valor,
            TotalDescontos = resultado.TotalDescontos.Valor,
            SalarioLiquido = resultado.SalarioLiquido.Valor,
            CalculadoEm = resultado.CalculadoEm,
            Sucesso = true,
            MensagemErro = null
        };
    }
}
