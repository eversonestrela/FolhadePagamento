namespace FolhadePagamento.Dominio.ValueObjects;

/// <summary>
/// Value Object representando identificador único de Funcionário.
/// Encapsula um Guid para prover tipagem forte e evitar obsessão por primitivos.
/// </summary>
public sealed class FuncionarioId : IEquatable<FuncionarioId>
{
    public Guid Valor { get; }

    private FuncionarioId(Guid valor)
    {
        Valor = valor;
    }

    /// <summary>
    /// Cria um novo FuncionarioId único.
    /// </summary>
    public static FuncionarioId Novo() => new FuncionarioId(Guid.NewGuid());

    /// <summary>
    /// Cria um FuncionarioId a partir de um Guid existente.
    /// </summary>
    public static FuncionarioId De(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("FuncionarioId não pode estar vazio", nameof(id));

        return new FuncionarioId(id);
    }

    // Igualdade
    public bool Equals(FuncionarioId? outro)
    {
        if (outro is null) return false;
        return Valor == outro.Valor;
    }

    public override bool Equals(object? obj) => Equals(obj as FuncionarioId);

    public override int GetHashCode() => Valor.GetHashCode();

    public static bool operator ==(FuncionarioId? esquerda, FuncionarioId? direita)
    {
        if (esquerda is null && direita is null) return true;
        if (esquerda is null || direita is null) return false;
        return esquerda.Equals(direita);
    }

    public static bool operator !=(FuncionarioId? esquerda, FuncionarioId? direita) => !(esquerda == direita);

    public override string ToString() => Valor.ToString();
}
