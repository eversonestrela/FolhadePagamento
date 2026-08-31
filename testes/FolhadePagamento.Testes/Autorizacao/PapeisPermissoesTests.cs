using FolhadePagamento.Aplicacao.Autorizacao;
using Xunit;

namespace FolhadePagamento.Testes.Autorizacao;

/// <summary>
/// Testes para o modelo de papéis e permissões RBAC.
/// </summary>
public class PapeisPermissoesTests
{
    // ========================================================================
    // TESTES DE PAPÉIS
    // ========================================================================

    [Fact]
    public void TodosOsPapeis_DeveConterTresPapeis()
    {
        // Act
        var papeis = Papeis.TodosOsPapeis;

        // Assert
        Assert.Equal(3, papeis.Count);
        Assert.Contains(Papeis.Administrador, papeis);
        Assert.Contains(Papeis.Operador, papeis);
        Assert.Contains(Papeis.Consulta, papeis);
    }

    [Fact]
    public void Administrador_DeveSerStringCorreta()
    {
        Assert.Equal("Administrador", Papeis.Administrador);
    }

    [Fact]
    public void Operador_DeveSerStringCorreta()
    {
        Assert.Equal("Operador", Papeis.Operador);
    }

    [Fact]
    public void Consulta_DeveSerStringCorreta()
    {
        Assert.Equal("Consulta", Papeis.Consulta);
    }

    // ========================================================================
    // TESTES DE PERMISSÕES DO ADMINISTRADOR
    // ========================================================================

    [Theory]
    [InlineData(Permissoes.FuncionarioConsultar)]
    [InlineData(Permissoes.FuncionarioCriar)]
    [InlineData(Permissoes.FuncionarioAtualizar)]
    [InlineData(Permissoes.FuncionarioDesativar)]
    [InlineData(Permissoes.ProcessamentoConsultar)]
    [InlineData(Permissoes.ProcessamentoExecutar)]
    [InlineData(Permissoes.LoteConsultar)]
    [InlineData(Permissoes.LoteCriar)]
    [InlineData(Permissoes.LoteCancelar)]
    public void Administrador_DeveTermTodasAsPermissoes(string permissao)
    {
        // Act
        var temPermissao = MapeamentoPapelPermissao.TemPermissao(Papeis.Administrador, permissao);

        // Assert
        Assert.True(temPermissao, $"Administrador deveria ter permissão '{permissao}'");
    }

    [Fact]
    public void Administrador_DeveTerNovePermissoes()
    {
        // Act
        var permissoes = MapeamentoPapelPermissao.ObterPermissoes(Papeis.Administrador);

        // Assert
        Assert.Equal(9, permissoes.Count);
    }

    // ========================================================================
    // TESTES DE PERMISSÕES DO OPERADOR
    // ========================================================================

    [Theory]
    [InlineData(Permissoes.FuncionarioConsultar)]
    [InlineData(Permissoes.ProcessamentoConsultar)]
    [InlineData(Permissoes.ProcessamentoExecutar)]
    [InlineData(Permissoes.LoteConsultar)]
    [InlineData(Permissoes.LoteCriar)]
    public void Operador_DeveTerPermissoesOperacionais(string permissao)
    {
        // Act
        var temPermissao = MapeamentoPapelPermissao.TemPermissao(Papeis.Operador, permissao);

        // Assert
        Assert.True(temPermissao, $"Operador deveria ter permissão '{permissao}'");
    }

    [Theory]
    [InlineData(Permissoes.FuncionarioCriar)]
    [InlineData(Permissoes.FuncionarioAtualizar)]
    [InlineData(Permissoes.FuncionarioDesativar)]
    [InlineData(Permissoes.LoteCancelar)]
    public void Operador_NaoDeveTerPermissoesAdministrativas(string permissao)
    {
        // Act
        var temPermissao = MapeamentoPapelPermissao.TemPermissao(Papeis.Operador, permissao);

        // Assert
        Assert.False(temPermissao, $"Operador NÃO deveria ter permissão '{permissao}'");
    }

    [Fact]
    public void Operador_DeveTerCincoPermissoes()
    {
        // Act
        var permissoes = MapeamentoPapelPermissao.ObterPermissoes(Papeis.Operador);

        // Assert
        Assert.Equal(5, permissoes.Count);
    }

    // ========================================================================
    // TESTES DE PERMISSÕES DO CONSULTA
    // ========================================================================

    [Theory]
    [InlineData(Permissoes.FuncionarioConsultar)]
    [InlineData(Permissoes.ProcessamentoConsultar)]
    [InlineData(Permissoes.LoteConsultar)]
    public void Consulta_DeveTerApenasPermissoesLeitura(string permissao)
    {
        // Act
        var temPermissao = MapeamentoPapelPermissao.TemPermissao(Papeis.Consulta, permissao);

        // Assert
        Assert.True(temPermissao, $"Consulta deveria ter permissão '{permissao}'");
    }

    [Theory]
    [InlineData(Permissoes.FuncionarioCriar)]
    [InlineData(Permissoes.FuncionarioAtualizar)]
    [InlineData(Permissoes.FuncionarioDesativar)]
    [InlineData(Permissoes.ProcessamentoExecutar)]
    [InlineData(Permissoes.LoteCriar)]
    [InlineData(Permissoes.LoteCancelar)]
    public void Consulta_NaoDeveTerPermissoesEscrita(string permissao)
    {
        // Act
        var temPermissao = MapeamentoPapelPermissao.TemPermissao(Papeis.Consulta, permissao);

        // Assert
        Assert.False(temPermissao, $"Consulta NÃO deveria ter permissão '{permissao}'");
    }

    [Fact]
    public void Consulta_DeveTerTresPermissoes()
    {
        // Act
        var permissoes = MapeamentoPapelPermissao.ObterPermissoes(Papeis.Consulta);

        // Assert
        Assert.Equal(3, permissoes.Count);
    }

    // ========================================================================
    // TESTES DE MAPEAMENTO COM MÚLTIPLOS PAPÉIS
    // ========================================================================

    [Fact]
    public void TemPermissao_ComMultiplosPapeis_DeveRetornarTrueSeQualquerPapelTemPermissao()
    {
        // Arrange
        var papeis = new[] { Papeis.Consulta, Papeis.Operador };

        // Act - Operador tem permissão para executar processamento
        var temPermissao = MapeamentoPapelPermissao.TemPermissao(papeis, Permissoes.ProcessamentoExecutar);

        // Assert
        Assert.True(temPermissao);
    }

    [Fact]
    public void TemPermissao_ComMultiplosPapeis_DeveRetornarFalseSeNenhumPapelTemPermissao()
    {
        // Arrange
        var papeis = new[] { Papeis.Consulta, Papeis.Operador };

        // Act - Nenhum deles pode cancelar lote
        var temPermissao = MapeamentoPapelPermissao.TemPermissao(papeis, Permissoes.LoteCancelar);

        // Assert
        Assert.False(temPermissao);
    }

    [Fact]
    public void TemPermissao_ComPapelInexistente_DeveRetornarFalse()
    {
        // Act
        var temPermissao = MapeamentoPapelPermissao.TemPermissao("PapelInexistente", Permissoes.FuncionarioConsultar);

        // Assert
        Assert.False(temPermissao);
    }

    [Fact]
    public void ObterPermissoes_ComPapelInexistente_DeveRetornarListaVazia()
    {
        // Act
        var permissoes = MapeamentoPapelPermissao.ObterPermissoes("PapelInexistente");

        // Assert
        Assert.Empty(permissoes);
    }

    // ========================================================================
    // TESTES DE CENÁRIOS DE ACESSO
    // ========================================================================

    [Fact]
    public void CenarioAcesso_AdministradorPodeCriarFuncionario()
    {
        Assert.True(MapeamentoPapelPermissao.TemPermissao(Papeis.Administrador, Permissoes.FuncionarioCriar));
    }

    [Fact]
    public void CenarioAcesso_OperadorNaoPodeCriarFuncionario()
    {
        Assert.False(MapeamentoPapelPermissao.TemPermissao(Papeis.Operador, Permissoes.FuncionarioCriar));
    }

    [Fact]
    public void CenarioAcesso_OperadorPodeProcessarFolha()
    {
        Assert.True(MapeamentoPapelPermissao.TemPermissao(Papeis.Operador, Permissoes.ProcessamentoExecutar));
    }

    [Fact]
    public void CenarioAcesso_ConsultaNaoPodeProcessarFolha()
    {
        Assert.False(MapeamentoPapelPermissao.TemPermissao(Papeis.Consulta, Permissoes.ProcessamentoExecutar));
    }

    [Fact]
    public void CenarioAcesso_OperadorNaoPodeCancelarLote()
    {
        Assert.False(MapeamentoPapelPermissao.TemPermissao(Papeis.Operador, Permissoes.LoteCancelar));
    }

    [Fact]
    public void CenarioAcesso_AdministradorPodeCancelarLote()
    {
        Assert.True(MapeamentoPapelPermissao.TemPermissao(Papeis.Administrador, Permissoes.LoteCancelar));
    }

    [Fact]
    public void CenarioAcesso_TodosPodeConsultarFuncionario()
    {
        Assert.True(MapeamentoPapelPermissao.TemPermissao(Papeis.Administrador, Permissoes.FuncionarioConsultar));
        Assert.True(MapeamentoPapelPermissao.TemPermissao(Papeis.Operador, Permissoes.FuncionarioConsultar));
        Assert.True(MapeamentoPapelPermissao.TemPermissao(Papeis.Consulta, Permissoes.FuncionarioConsultar));
    }

    [Fact]
    public void CenarioAcesso_OperadorPodeCriarLote()
    {
        Assert.True(MapeamentoPapelPermissao.TemPermissao(Papeis.Operador, Permissoes.LoteCriar));
    }

    [Fact]
    public void CenarioAcesso_ConsultaNaoPodeCriarLote()
    {
        Assert.False(MapeamentoPapelPermissao.TemPermissao(Papeis.Consulta, Permissoes.LoteCriar));
    }
}
