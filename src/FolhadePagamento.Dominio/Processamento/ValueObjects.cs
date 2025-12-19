namespace FolhadePagamento.Dominio.Processamento;

/// <summary>
/// Value Object que representa o identificador único de um processamento.
/// Imutável por design.
/// 
/// Um ProcessamentoId identifica uma execução específica de cálculo de folha,
/// permitindo rastreabilidade completa de cada processamento realizado.
/// </summary>
public sealed class ProcessamentoId : IEquatable<ProcessamentoId>
{
    /// <summary>
    /// Valor interno do identificador (GUID).
    /// </summary>
    public Guid Valor { get; }

    private ProcessamentoId(Guid valor)
    {
        Valor = valor;
    }

    /// <summary>
    /// Cria um novo identificador de processamento único.
    /// </summary>
    public static ProcessamentoId Novo() => new ProcessamentoId(Guid.NewGuid());

    /// <summary>
    /// Restaura um ProcessamentoId a partir de um GUID existente.
    /// Usado para reconstitição de agregados persistidos.
    /// </summary>
    public static ProcessamentoId DeGuid(Guid guid)
    {
        if (guid == Guid.Empty)
            throw new ArgumentException("GUID não pode ser vazio.", nameof(guid));

        return new ProcessamentoId(guid);
    }

    /// <summary>
    /// Restaura um ProcessamentoId a partir de uma string.
    /// </summary>
    public static ProcessamentoId DeString(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("Valor não pode ser vazio.", nameof(valor));

        if (!Guid.TryParse(valor, out var guid))
            throw new ArgumentException("Valor não é um GUID válido.", nameof(valor));

        return DeGuid(guid);
    }

    #region Igualdade

    public bool Equals(ProcessamentoId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Valor == other.Valor;
    }

    public override bool Equals(object? obj) => Equals(obj as ProcessamentoId);

    public override int GetHashCode() => Valor.GetHashCode();

    public static bool operator ==(ProcessamentoId? left, ProcessamentoId? right) =>
        Equals(left, right);

    public static bool operator !=(ProcessamentoId? left, ProcessamentoId? right) =>
        !Equals(left, right);

    #endregion

    public override string ToString() => Valor.ToString();
}

/// <summary>
/// Value Object que representa o número da versão de um processamento.
/// Imutável por design.
/// 
/// A versão é incremental: V1, V2, V3, etc.
/// Cada reprocessamento de uma competência gera uma nova versão.
/// </summary>
public sealed class VersaoProcessamento : IEquatable<VersaoProcessamento>, IComparable<VersaoProcessamento>
{
    /// <summary>
    /// Número da versão (1, 2, 3, ...).
    /// </summary>
    public int Numero { get; }

    private VersaoProcessamento(int numero)
    {
        Numero = numero;
    }

    /// <summary>
    /// Cria a primeira versão (V1).
    /// </summary>
    public static VersaoProcessamento Primeira => new VersaoProcessamento(1);

    /// <summary>
    /// Cria uma versão a partir de um número específico.
    /// </summary>
    public static VersaoProcessamento DeNumero(int numero)
    {
        if (numero <= 0)
            throw new ArgumentOutOfRangeException(nameof(numero), "Versão deve ser maior que zero.");

        return new VersaoProcessamento(numero);
    }

    /// <summary>
    /// Retorna a próxima versão (incrementa em 1).
    /// </summary>
    public VersaoProcessamento Proxima() => new VersaoProcessamento(Numero + 1);

    /// <summary>
    /// Verifica se esta é a primeira versão.
    /// </summary>
    public bool EhPrimeira => Numero == 1;

    #region Igualdade e Comparação

    public bool Equals(VersaoProcessamento? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Numero == other.Numero;
    }

    public override bool Equals(object? obj) => Equals(obj as VersaoProcessamento);

    public override int GetHashCode() => Numero.GetHashCode();

    public int CompareTo(VersaoProcessamento? other)
    {
        if (other is null) return 1;
        return Numero.CompareTo(other.Numero);
    }

    public static bool operator ==(VersaoProcessamento? left, VersaoProcessamento? right) =>
        Equals(left, right);

    public static bool operator !=(VersaoProcessamento? left, VersaoProcessamento? right) =>
        !Equals(left, right);

    public static bool operator >(VersaoProcessamento left, VersaoProcessamento right) =>
        left.CompareTo(right) > 0;

    public static bool operator <(VersaoProcessamento left, VersaoProcessamento right) =>
        left.CompareTo(right) < 0;

    public static bool operator >=(VersaoProcessamento left, VersaoProcessamento right) =>
        left.CompareTo(right) >= 0;

    public static bool operator <=(VersaoProcessamento left, VersaoProcessamento right) =>
        left.CompareTo(right) <= 0;

    #endregion

    public override string ToString() => $"V{Numero}";
}

/// <summary>
/// Enum que representa o status de um processamento.
/// 
/// Status possíveis:
/// - EmProcessamento: Cálculo em andamento
/// - Finalizado: Cálculo concluído com sucesso, resultado imutável
/// - Cancelado: Processamento cancelado antes de finalizar
/// - Superado: Existe uma versão mais nova (V2 supera V1)
/// </summary>
public enum StatusProcessamento
{
    /// <summary>
    /// Cálculo em andamento. Pode ser cancelado.
    /// </summary>
    EmProcessamento = 1,

    /// <summary>
    /// Cálculo concluído com sucesso.
    /// Resultado é IMUTÁVEL a partir deste ponto.
    /// </summary>
    Finalizado = 2,

    /// <summary>
    /// Processamento foi cancelado antes de finalizar.
    /// </summary>
    Cancelado = 3,

    /// <summary>
    /// Processamento foi superado por uma versão mais nova.
    /// Mantido para histórico e auditoria.
    /// </summary>
    Superado = 4
}

/// <summary>
/// Value Object que representa o motivo de um reprocessamento.
/// Usado para auditoria e rastreabilidade.
/// </summary>
public sealed class MotivoReprocessamento : IEquatable<MotivoReprocessamento>
{
    /// <summary>
    /// Código do motivo.
    /// </summary>
    public string Codigo { get; }

    /// <summary>
    /// Descrição detalhada do motivo.
    /// </summary>
    public string Descricao { get; }

    private MotivoReprocessamento(string codigo, string descricao)
    {
        Codigo = codigo;
        Descricao = descricao;
    }

    /// <summary>
    /// Cria um motivo de reprocessamento.
    /// </summary>
    public static MotivoReprocessamento Criar(string codigo, string descricao)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("Código é obrigatório.", nameof(codigo));

        if (string.IsNullOrWhiteSpace(descricao))
            throw new ArgumentException("Descrição é obrigatória.", nameof(descricao));

        return new MotivoReprocessamento(codigo.ToUpperInvariant(), descricao);
    }

    // Motivos pré-definidos
    public static MotivoReprocessamento CorrecaoCalculo =>
        new MotivoReprocessamento("CORRECAO_CALCULO", "Correção de erro no cálculo original");

    public static MotivoReprocessamento AtualizacaoLegislacao =>
        new MotivoReprocessamento("ATUALIZACAO_LEGISLACAO", "Atualização de legislação tributária");

    public static MotivoReprocessamento CorrecaoCadastro =>
        new MotivoReprocessamento("CORRECAO_CADASTRO", "Correção de dados cadastrais do funcionário");

    public static MotivoReprocessamento AjusteConsignado =>
        new MotivoReprocessamento("AJUSTE_CONSIGNADO", "Ajuste em contratos de consignados");

    public static MotivoReprocessamento SolicitacaoAuditoria =>
        new MotivoReprocessamento("SOLICITACAO_AUDITORIA", "Reprocessamento solicitado por auditoria");

    #region Igualdade

    public bool Equals(MotivoReprocessamento? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Codigo == other.Codigo;
    }

    public override bool Equals(object? obj) => Equals(obj as MotivoReprocessamento);

    public override int GetHashCode() => Codigo.GetHashCode();

    public static bool operator ==(MotivoReprocessamento? left, MotivoReprocessamento? right) =>
        Equals(left, right);

    public static bool operator !=(MotivoReprocessamento? left, MotivoReprocessamento? right) =>
        !Equals(left, right);

    #endregion

    public override string ToString() => $"[{Codigo}] {Descricao}";
}
