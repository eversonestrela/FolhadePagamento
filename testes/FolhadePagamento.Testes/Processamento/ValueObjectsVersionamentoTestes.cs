using FluentAssertions;
using FolhadePagamento.Dominio.Processamento;
using Xunit;

namespace FolhadePagamento.Testes.Processamento;

/// <summary>
/// Testes para os Value Objects de Versionamento.
/// </summary>
public class ValueObjectsVersionamentoTestes
{
    #region ProcessamentoId

    [Fact]
    public void ProcessamentoId_Novo_DeveCriarIdUnico()
    {
        // Act
        var id1 = ProcessamentoId.Novo();
        var id2 = ProcessamentoId.Novo();

        // Assert
        id1.Should().NotBe(id2);
        id1.Valor.Should().NotBe(Guid.Empty);
        id2.Valor.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ProcessamentoId_DeGuid_DeveRestaurarCorretamente()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = ProcessamentoId.DeGuid(guid);

        // Assert
        id.Valor.Should().Be(guid);
    }

    [Fact]
    public void ProcessamentoId_DeGuid_ComGuidVazio_DeveLancarExcecao()
    {
        // Act
        var acao = () => ProcessamentoId.DeGuid(Guid.Empty);

        // Assert
        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessamentoId_DeString_DeveRestaurarCorretamente()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var str = guid.ToString();

        // Act
        var id = ProcessamentoId.DeString(str);

        // Assert
        id.Valor.Should().Be(guid);
    }

    [Fact]
    public void ProcessamentoId_DeString_ComValorInvalido_DeveLancarExcecao()
    {
        // Act
        var acao = () => ProcessamentoId.DeString("nao-e-um-guid");

        // Assert
        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProcessamentoId_Igualdade_MesmoGuid_DeveSerIgual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = ProcessamentoId.DeGuid(guid);
        var id2 = ProcessamentoId.DeGuid(guid);

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    #endregion

    #region VersaoProcessamento

    [Fact]
    public void VersaoProcessamento_Primeira_DeveSerV1()
    {
        // Act
        var versao = VersaoProcessamento.Primeira;

        // Assert
        versao.Numero.Should().Be(1);
        versao.EhPrimeira.Should().BeTrue();
        versao.ToString().Should().Be("V1");
    }

    [Fact]
    public void VersaoProcessamento_Proxima_DeveIncrementar()
    {
        // Arrange
        var v1 = VersaoProcessamento.Primeira;

        // Act
        var v2 = v1.Proxima();
        var v3 = v2.Proxima();

        // Assert
        v2.Numero.Should().Be(2);
        v3.Numero.Should().Be(3);
        v2.EhPrimeira.Should().BeFalse();
    }

    [Fact]
    public void VersaoProcessamento_DeNumero_ComValorValido_DeveCriar()
    {
        // Act
        var versao = VersaoProcessamento.DeNumero(5);

        // Assert
        versao.Numero.Should().Be(5);
    }

    [Fact]
    public void VersaoProcessamento_DeNumero_ComZero_DeveLancarExcecao()
    {
        // Act
        var acao = () => VersaoProcessamento.DeNumero(0);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void VersaoProcessamento_DeNumero_ComNegativo_DeveLancarExcecao()
    {
        // Act
        var acao = () => VersaoProcessamento.DeNumero(-1);

        // Assert
        acao.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void VersaoProcessamento_Comparacao_DeveOrdenarCorretamente()
    {
        // Arrange
        var v1 = VersaoProcessamento.Primeira;
        var v2 = v1.Proxima();
        var v3 = v2.Proxima();

        // Assert
        (v1 < v2).Should().BeTrue();
        (v2 < v3).Should().BeTrue();
        (v3 > v1).Should().BeTrue();
        (v1 <= v1).Should().BeTrue();
        (v2 >= v2).Should().BeTrue();
    }

    #endregion

    #region StatusProcessamento

    [Fact]
    public void StatusProcessamento_DeveConterTodosOsValores()
    {
        // Assert
        Enum.GetValues<StatusProcessamento>().Should().HaveCount(4);
        Enum.IsDefined(StatusProcessamento.EmProcessamento).Should().BeTrue();
        Enum.IsDefined(StatusProcessamento.Finalizado).Should().BeTrue();
        Enum.IsDefined(StatusProcessamento.Cancelado).Should().BeTrue();
        Enum.IsDefined(StatusProcessamento.Superado).Should().BeTrue();
    }

    #endregion

    #region MotivoReprocessamento

    [Fact]
    public void MotivoReprocessamento_Criar_ComValoresValidos_DeveCriar()
    {
        // Act
        var motivo = MotivoReprocessamento.Criar("TESTE", "Motivo de teste");

        // Assert
        motivo.Codigo.Should().Be("TESTE");
        motivo.Descricao.Should().Be("Motivo de teste");
    }

    [Fact]
    public void MotivoReprocessamento_Criar_SemCodigo_DeveLancarExcecao()
    {
        // Act
        var acao = () => MotivoReprocessamento.Criar("", "Descrição");

        // Assert
        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MotivoReprocessamento_Criar_SemDescricao_DeveLancarExcecao()
    {
        // Act
        var acao = () => MotivoReprocessamento.Criar("CODIGO", "");

        // Assert
        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MotivoReprocessamento_MotivosPredefinidos_DevemExistir()
    {
        // Assert
        MotivoReprocessamento.CorrecaoCalculo.Codigo.Should().Be("CORRECAO_CALCULO");
        MotivoReprocessamento.AtualizacaoLegislacao.Codigo.Should().Be("ATUALIZACAO_LEGISLACAO");
        MotivoReprocessamento.CorrecaoCadastro.Codigo.Should().Be("CORRECAO_CADASTRO");
        MotivoReprocessamento.AjusteConsignado.Codigo.Should().Be("AJUSTE_CONSIGNADO");
        MotivoReprocessamento.SolicitacaoAuditoria.Codigo.Should().Be("SOLICITACAO_AUDITORIA");
    }

    [Fact]
    public void MotivoReprocessamento_Igualdade_MesmoCodigo_DeveSerIgual()
    {
        // Arrange
        var m1 = MotivoReprocessamento.Criar("TESTE", "Descrição 1");
        var m2 = MotivoReprocessamento.Criar("teste", "Descrição 2"); // Código diferente case

        // Assert - Código é convertido para uppercase
        m1.Should().Be(m2);
    }

    #endregion
}
