using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Entities;
using ProClubesApi.Services;

namespace ProClubes.UnitTests;

public class DomainRulesUnitTests
{
    [Theory]
    [InlineData("123.456.789-01", "12345678901", "***.456.789-**")]
    [InlineData("11122233344", "11122233344", "***.222.333-**")]
    public void CpfHelper_Normalize_E_Mask_AplicaPadraoOficial(string rawCpf, string expectedDigits, string expectedMask)
    {
        var normalized = CpfHelper.Normalize(rawCpf);
        Assert.Equal(expectedDigits, normalized);

        var masked = CpfHelper.Mask(normalized);
        Assert.Equal(expectedMask, masked);
    }

    [Fact]
    public void CpfHelper_RejeitaCpfInvalidoComDigitosRepetidosOuTamanhoIncorreto()
    {
        Assert.False(CpfHelper.IsValidFormat("000.000.000-00"));
        Assert.False(CpfHelper.IsValidFormat("123456789"));
        Assert.True(CpfHelper.IsValidFormat("123.456.789-01"));
    }

    [Fact]
    public void PasswordHasherHelper_Hash_E_Verify_ValidaSenhaCorretaERejeitaIncorreta()
    {
        var hash = PasswordHasherHelper.HashPassword("SenhaForte@123");
        Assert.True(PasswordHasherHelper.VerifyPassword(hash, "SenhaForte@123"));
        Assert.False(PasswordHasherHelper.VerifyPassword(hash, "SenhaErrada@123"));
    }

    [Fact]
    public void TimeZoneHelper_ResolveDayInMonth_Dia31EmFevereiroResolveParaUltimoDiaValido()
    {
        // Ano não bissexto (2026) -> 28 de fevereiro
        var fev2026 = TimeZoneHelper.ResolveDayInMonth(2026, 2, 31);
        Assert.Equal(new DateOnly(2026, 2, 28), fev2026);

        // Ano bissexto (2028) -> 29 de fevereiro
        var fev2028 = TimeZoneHelper.ResolveDayInMonth(2028, 2, 31);
        Assert.Equal(new DateOnly(2028, 2, 29), fev2028);

        // Abril (30 dias) -> 30 de abril
        var abr2026 = TimeZoneHelper.ResolveDayInMonth(2026, 4, 31);
        Assert.Equal(new DateOnly(2026, 4, 30), abr2026);
    }

    [Fact]
    public void TimeZoneHelper_CalculateAge_CalculaTransicaoDe21Para22AnosExatamenteNoAniversario()
    {
        var nascimento = new DateOnly(2004, 11, 15);

        Assert.Equal(21, TimeZoneHelper.CalculateAge(nascimento, new DateOnly(2026, 11, 14)));
        Assert.Equal(22, TimeZoneHelper.CalculateAge(nascimento, new DateOnly(2026, 11, 15)));
    }

    [Fact]
    public void FinanceiroService_CalcularEncargos_AplicaMultaFixaEJurosSimplesProporcionaisAoAtraso()
    {
        var mensalidade = new Mensalidade
        {
            ValorPrincipal = 300.00m,
            DataVencimento = new DateOnly(2026, 10, 10),
            MultaPercentualEmissao = 2.0000m,      // 2% = R$ 6,00
            JuroMensalPercentualEmissao = 3.0000m, // 3% ao mês = 0,1% ao dia (R$ 0,30/dia)
            Status = StatusMensalidade.Aberta
        };

        // No dia do vencimento: sem multa nem juros
        var emDia = FinanceiroService.CalcularEncargos(mensalidade, new DateOnly(2026, 10, 10));
        Assert.Equal(0m, emDia.Multa);
        Assert.Equal(0m, emDia.Juros);
        Assert.Equal(300.00m, emDia.Total);
        Assert.Equal(0, emDia.DiasAtraso);

        // 15 dias de atraso: Multa = R$ 6,00, Juros = 300 * 0,03 * (15/30) = R$ 4,50, Total = R$ 310,50
        var atrasada = FinanceiroService.CalcularEncargos(mensalidade, new DateOnly(2026, 10, 25));
        Assert.Equal(6.00m, atrasada.Multa);
        Assert.Equal(4.50m, atrasada.Juros);
        Assert.Equal(310.50m, atrasada.Total);
        Assert.Equal(15, atrasada.DiasAtraso);
    }

    [Fact]
    public void PaginationQuery_LimitaPageSizeAoMaximoDe100EUsaPadrao20()
    {
        var padrao = new PaginationQuery();
        Assert.Equal(1, padrao.NormalizedPage);
        Assert.Equal(20, padrao.NormalizedPageSize);

        var acimaDoLimite = new PaginationQuery(Page: 2, PageSize: 500);
        Assert.Equal(2, acimaDoLimite.NormalizedPage);
        Assert.Equal(100, acimaDoLimite.NormalizedPageSize);
        Assert.Equal(100, acimaDoLimite.Skip);
    }
}
