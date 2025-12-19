namespace FolhadePagamento.Dominio.ValueObjects;

/// <summary>
/// Value Object representando um período de vigência de uma regra.
/// 
/// Vigência define quando uma regra (rubrica, tabela de imposto, etc.) é válida.
/// Fundamental para sistemas de folha onde legislação muda ao longo do tempo.
/// 
/// Exemplos de uso:
/// - Tabela IRRF vigente de 01/01/2025 a 31/03/2025
/// - Rubrica de bônus vigente de 01/06/2025 (sem data fim = indefinida)
/// - Alíquota INSS que mudou em 01/04/2025
/// 
/// Imutável por design.
/// </summary>
public sealed class Vigencia : IEquatable<Vigencia>
{
    /// <summary>
    /// Data de início da vigência (inclusive).
    /// </summary>
    public DateTime DataInicio { get; }

    /// <summary>
    /// Data de fim da vigência (inclusive).
    /// Null significa vigência indefinida (sem data fim).
    /// </summary>
    public DateTime? DataFim { get; }

    private Vigencia(DateTime dataInicio, DateTime? dataFim)
    {
        DataInicio = dataInicio.Date; // Apenas a data, sem hora
        DataFim = dataFim?.Date;
    }

    /// <summary>
    /// Cria uma vigência com data início e data fim.
    /// </summary>
    /// <param name="dataInicio">Data de início (inclusive)</param>
    /// <param name="dataFim">Data de fim (inclusive). Null para vigência indefinida.</param>
    public static Vigencia Criar(DateTime dataInicio, DateTime? dataFim = null)
    {
        if (dataFim.HasValue && dataFim.Value < dataInicio)
            throw new ArgumentException("Data fim não pode ser anterior à data início", nameof(dataFim));

        return new Vigencia(dataInicio, dataFim);
    }

    /// <summary>
    /// Cria uma vigência indefinida (apenas data início, sem data fim).
    /// </summary>
    public static Vigencia Indefinida(DateTime dataInicio)
    {
        return new Vigencia(dataInicio, null);
    }

    /// <summary>
    /// Verifica se a vigência está ativa para uma data específica.
    /// </summary>
    /// <param name="data">Data a verificar</param>
    /// <returns>True se a data está dentro do período de vigência</returns>
    public bool EstaVigenteEm(DateTime data)
    {
        var dataVerificar = data.Date;

        if (dataVerificar < DataInicio)
            return false;

        if (DataFim.HasValue && dataVerificar > DataFim.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Verifica se a vigência está ativa para uma competência.
    /// A vigência é válida se estiver ativa em qualquer dia do mês da competência.
    /// </summary>
    /// <param name="competencia">Competência a verificar</param>
    /// <returns>True se a vigência cobre a competência</returns>
    public bool EstaVigenteParaCompetencia(Competencia competencia)
    {
        if (competencia is null)
            throw new ArgumentNullException(nameof(competencia));

        // A vigência cobre a competência se:
        // - DataInicio <= último dia da competência E
        // - DataFim (se existir) >= primeiro dia da competência

        var primeiroDiaCompetencia = competencia.PrimeiroDia;
        var ultimoDiaCompetencia = competencia.UltimoDia;

        // Se a vigência começa depois do fim do mês, não é válida
        if (DataInicio > ultimoDiaCompetencia)
            return false;

        // Se há data fim e ela é antes do início do mês, não é válida
        if (DataFim.HasValue && DataFim.Value < primeiroDiaCompetencia)
            return false;

        return true;
    }

    /// <summary>
    /// Verifica se a vigência está expirada em relação a uma data.
    /// </summary>
    public bool EstaExpiradaEm(DateTime data)
    {
        if (!DataFim.HasValue)
            return false; // Vigência indefinida nunca expira

        return data.Date > DataFim.Value;
    }

    /// <summary>
    /// Verifica se a vigência ainda não iniciou em relação a uma data.
    /// </summary>
    public bool AindaNaoIniciouEm(DateTime data)
    {
        return data.Date < DataInicio;
    }

    /// <summary>
    /// Retorna se a vigência é indefinida (sem data fim).
    /// </summary>
    public bool EhIndefinida => !DataFim.HasValue;

    // Igualdade
    public bool Equals(Vigencia? outra)
    {
        if (outra is null) return false;
        return DataInicio == outra.DataInicio && DataFim == outra.DataFim;
    }

    public override bool Equals(object? obj) => Equals(obj as Vigencia);

    public override int GetHashCode() => HashCode.Combine(DataInicio, DataFim);

    public static bool operator ==(Vigencia? esquerda, Vigencia? direita)
    {
        if (esquerda is null && direita is null) return true;
        if (esquerda is null || direita is null) return false;
        return esquerda.Equals(direita);
    }

    public static bool operator !=(Vigencia? esquerda, Vigencia? direita) => !(esquerda == direita);

    public override string ToString()
    {
        if (DataFim.HasValue)
            return $"{DataInicio:dd/MM/yyyy} a {DataFim:dd/MM/yyyy}";

        return $"{DataInicio:dd/MM/yyyy} (indefinida)";
    }
}
