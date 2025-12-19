using FolhadePagamento.Dominio.ValueObjects;

namespace FolhadePagamento.Dominio.Entidades;

/// <summary>
/// Entidade representando um Funcionário.
/// Contém dados mestres utilizados para cálculos de folha.
/// Esta é uma versão simplificada para implementação inicial.
/// </summary>
public class Funcionario
{
    public FuncionarioId Id { get; private set; }
    public string Nome { get; private set; }
    public Dinheiro SalarioBase { get; private set; }
    public bool Ativo { get; private set; }

    // Construtor privado para criação controlada
    private Funcionario(FuncionarioId id, string nome, Dinheiro salarioBase, bool ativo)
    {
        Id = id;
        Nome = nome;
        SalarioBase = salarioBase;
        Ativo = ativo;
    }

    /// <summary>
    /// Método fábrica para criar um novo Funcionário.
    /// Garante que todas as invariantes são satisfeitas.
    /// </summary>
    public static Funcionario Criar(FuncionarioId id, string nome, Dinheiro salarioBase)
    {
        if (id is null)
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do funcionário não pode estar vazio", nameof(nome));

        if (salarioBase is null)
            throw new ArgumentNullException(nameof(salarioBase));

        return new Funcionario(id, nome.Trim(), salarioBase, ativo: true);
    }

    /// <summary>
    /// Desativa o funcionário (para cenários de desligamento).
    /// Funcionários desativados não devem ser incluídos em novos cálculos de folha.
    /// </summary>
    public void Desativar()
    {
        Ativo = false;
    }

    /// <summary>
    /// Atualiza o salário base.
    /// Deve ser feito apenas antes de processar nova competência.
    /// </summary>
    public void AtualizarSalarioBase(Dinheiro novoSalario)
    {
        if (novoSalario is null)
            throw new ArgumentNullException(nameof(novoSalario));

        SalarioBase = novoSalario;
    }

    public override string ToString() => $"Funcionário {Nome} (ID: {Id})";
}
