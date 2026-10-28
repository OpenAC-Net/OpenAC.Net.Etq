using System.Globalization;
using System.Text;
using OpenAC.Net.Devices;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;

namespace OpenAC.Net.Etq.Test;

public class ProtocolTests
{
    [Test]
    [Arguments(ProtocoloEtq.Ppla, "122200000200010PRODUTO\r", "Q0002\rE\r")]
    [Arguments(ProtocoloEtq.Pplb, "A10,20,0,2,2,2,N,\"PRODUTO\"\n", "P2\n")]
    [Arguments(ProtocoloEtq.Epl2, "A10,20,0,2,2,2,N,\"PRODUTO\"\n", "P2\n")]
    [Arguments(ProtocoloEtq.ZplII, "^FO10,20^A2N,48,32^FH\\^FDPRODUTO^FS", "^PQ2\r\n^XZ\r\n")]
    [Arguments(ProtocoloEtq.EscLabel, "^FO10,20^F(C0,0,0,255,D,255,255,255,255,D^A2N,48,32^FH\\^FDPRODUTO^FS", "^PQ2\r\n^XZ\r\n")]
    [Arguments(ProtocoloEtq.Tspl, "TEXT 10,20,\"2\",0,2,2,\"PRODUTO\"", "PRINT 2\r\n")]
    public async Task GeraTextoECopiasConformeProtocolo(ProtocoloEtq protocolo, string texto, string fim)
    {
        using var printer = Create(protocolo);
        printer.ImprimirTexto("PRODUTO", 10, 20, "2", 2, 2);
        var bytes = printer.GerarComandos(2);
        var data = Encoding.ASCII.GetString(bytes);
        await Assert.That(data).Contains(texto);
        await Assert.That(data.EndsWith(fim, StringComparison.Ordinal)).IsTrue();
        await Assert.That(printer.Conectado).IsFalse();
        await Assert.That(printer.QuantidadeComandos).IsEqualTo(1);
    }

    [Test]
    [Arguments(ProtocoloEtq.Ppla, "1E42080")]
    [Arguments(ProtocoloEtq.Epl2, "B10,20,0,1,2,4,80,B,\"123456\"")]
    [Arguments(ProtocoloEtq.Pplb, "B10,20,0,1,2,4,80,B,\"123456\"")]
    [Arguments(ProtocoloEtq.ZplII, "^BY2,2^BCN,80,Y,N")]
    [Arguments(ProtocoloEtq.EscLabel, "^BY2,2^BCN,80,Y,N")]
    [Arguments(ProtocoloEtq.Tspl, "BARCODE 10,20,\"128\",80,1,0,2,4,\"123456\"")]
    public async Task GeraCode128(ProtocoloEtq protocolo, string esperado)
    {
        using var printer = Create(protocolo);
        printer.ImprimirBarras("123456", TipoCodigoBarras.Code128, 10, 20, 80);
        await Assert.That(Encoding.ASCII.GetString(printer.GerarComandos())).Contains(esperado);
    }

    [Test]
    [Arguments(ProtocoloEtq.Ppla, "1W1D44000002000102,HA,OPENAC")]
    [Arguments(ProtocoloEtq.Epl2, "b10,20,Q,m2,s4,eH,iA,\"OPENAC\"")]
    [Arguments(ProtocoloEtq.Pplb, "b10,20,Q,m2,s4,eH,iA,\"OPENAC\"")]
    [Arguments(ProtocoloEtq.ZplII, "^BQN,2,4^FH\\^FDHA,OPENAC^FS")]
    [Arguments(ProtocoloEtq.EscLabel, "^BQN,2,4^FH\\^FDHA,OPENAC^FS")]
    [Arguments(ProtocoloEtq.Tspl, "QRCODE 10,20,H,4,A,0,M2,\"OPENAC\"")]
    public async Task GeraQrCode(ProtocoloEtq protocolo, string esperado)
    {
        using var printer = Create(protocolo);
        printer.ImprimirQRCode("OPENAC", 10, 20, 4, QrCodeErrorLevel.H);
        await Assert.That(Encoding.ASCII.GetString(printer.GerarComandos())).Contains(esperado);
    }

    [Test]
    [Arguments(ProtocoloEtq.Ppla, "1X1100000200010L040002", "1X1100000200010B040030001001")]
    [Arguments(ProtocoloEtq.Epl2, "LO10,20,40,2", "X10,20,1,50,50")]
    [Arguments(ProtocoloEtq.Pplb, "LO10,20,40,2", "X10,20,1,50,50")]
    [Arguments(ProtocoloEtq.ZplII, "^FO10,20^GB40,2,2,B,0^FS", "^FO10,20^GB40,30,1,B,0^FS")]
    [Arguments(ProtocoloEtq.EscLabel, "^FO10,20^F(C0,0,0,255,D,255,255,255,255,D^GB40,2,2,B,0^FS", "^FO10,20^F(C0,0,0,255,D,255,255,255,255,D^GB40,30,1,B,0^FS")]
    [Arguments(ProtocoloEtq.Tspl, "BAR 10,20,40,2", "BOX 10,20,50,50,1")]
    public async Task GeraLinhaECaixa(ProtocoloEtq protocolo, string linha, string caixa)
    {
        using var printer = Create(protocolo);
        printer.ImprimirLinha(10, 20, 40, 2);
        printer.ImprimirCaixa(10, 20, 40, 30, 1);
        var data = Encoding.ASCII.GetString(printer.GerarComandos());
        await Assert.That(data).Contains(linha);
        await Assert.That(data).Contains(caixa);
    }

    [Test]
    [Arguments(OrientacaoEtiqueta.Normal, "N")]
    [Arguments(OrientacaoEtiqueta.Graus90, "R")]
    [Arguments(OrientacaoEtiqueta.Graus180, "I")]
    [Arguments(OrientacaoEtiqueta.Graus270, "B")]
    public async Task ZplConverteOrientacao(OrientacaoEtiqueta orientacao, string codigo)
    {
        using var printer = Create(ProtocoloEtq.ZplII);
        printer.ImprimirTexto("TEXTO", 0, 0, orientacao: orientacao);
        await Assert.That(Encoding.ASCII.GetString(printer.GerarComandos())).Contains("^A0" + codigo);
    }

    [Test]
    public async Task ZplEscapaCaracteresReservadosSemExecutarComandosDoTexto()
    {
        using var printer = Create(ProtocoloEtq.ZplII);
        printer.ImprimirTexto("A^XZ~JA\\B", 1, 2);
        var data = Encoding.ASCII.GetString(printer.GerarComandos());
        await Assert.That(data).Contains("A\\5EXZ\\7EJA\\5CB");
        await Assert.That(data.Split("^XZ").Length).IsEqualTo(2);
    }

    [Test]
    [Arguments(ProtocoloEtq.Epl2)]
    [Arguments(ProtocoloEtq.Tspl)]
    public async Task EscapaAspasDoTexto(ProtocoloEtq protocolo)
    {
        using var printer = Create(protocolo);
        printer.ImprimirTexto("Produto \"ABC\"", 0, 0);
        var data = Encoding.ASCII.GetString(printer.GerarComandos());
        await Assert.That(data.Contains(protocolo == ProtocoloEtq.Tspl ? "\\[\"]" : "\\\"")).IsTrue();
    }

    [Test]
    public async Task ConversaoFisicaEhIndependenteDaCultura()
    {
        foreach (var cultura in new[] { "pt-BR", "en-US", "de-DE" })
        {
            var anterior = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultura);
                using var printer = new EtqPrinter<FileConfig> { Protocolo = ProtocoloEtq.Tspl };
                printer.Configuracoes.Unidade = UnidadeEtiqueta.Polegadas;
                printer.Configuracoes.Largura = 4;
                printer.Configuracoes.Altura = 2;
                printer.Configuracoes.EspacoEntreEtiquetas = 0.1M;
                printer.ImprimirTexto("X", 1, 1);
                var data = Encoding.ASCII.GetString(printer.GerarComandos());
                await Assert.That(data).Contains("SIZE 101.6 mm,50.8 mm");
                await Assert.That(data).Contains("TEXT 203,203");
            }
            finally { CultureInfo.CurrentCulture = anterior; }
        }
    }

    [Test]
    [Arguments(UnidadeEtiqueta.Milimetros, 25.4)]
    [Arguments(UnidadeEtiqueta.Polegadas, 1)]
    [Arguments(UnidadeEtiqueta.Dots, 203)]
    [Arguments(UnidadeEtiqueta.DecimosDeMilimetro, 254)]
    public async Task UnidadesEquivalentes(UnidadeEtiqueta unidade, double valor)
    {
        var config = new EtqConfig { Unidade = unidade, DPI = 203 };
        await Assert.That(config.ToDots((decimal)valor)).IsEqualTo(203);
    }

    [Test]
    [Arguments(ProtocoloEtq.Ppla, 9999)]
    [Arguments(ProtocoloEtq.Epl2, 65535)]
    [Arguments(ProtocoloEtq.ZplII, 999)]
    [Arguments(ProtocoloEtq.EscLabel, 99999999)]
    [Arguments(ProtocoloEtq.Tspl, 65535)]
    public async Task ValidaLimitesDeCopias(ProtocoloEtq protocolo, int maximo)
    {
        using var printer = Create(protocolo);
        await Assert.That(printer.GerarComandos(maximo).Length).IsGreaterThan(0);
        await Assert.That(() => printer.GerarComandos(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => printer.GerarComandos(maximo + 1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task RejeitaControlesCodigosInvalidosERecursosSemSuporte()
    {
        using var printer = Create(ProtocoloEtq.ZplII);
        printer.ImprimirTexto("X\r\n^XZ", 0, 0);
        await Assert.That(() => printer.GerarComandos()).Throws<ArgumentException>();
        printer.Clear();
        printer.ImprimirBarras("123", TipoCodigoBarras.EAN13, 0, 0);
        await Assert.That(() => printer.GerarComandos()).Throws<ArgumentException>();
        printer.Clear();
        printer.Configuracoes.PaginaCodigo = PaginaCodigo.pcUTF8;
        printer.Protocolo = ProtocoloEtq.Epl2;
        await Assert.That(() => printer.GerarComandos()).Throws<NotSupportedException>();
    }

    [Test]
    public async Task BufferPossuiSnapshotDoComando()
    {
        using var printer = Create(ProtocoloEtq.ZplII);
        var command = new TextCommand { Texto = "ORIGINAL", Horizontal = 10 };
        printer.AdicionarComando(command);
        command.Texto = "MODIFICADO";
        await Assert.That(Encoding.ASCII.GetString(printer.GerarComandos())).Contains("ORIGINAL");
        var bytes = printer.GerarComandos();
        await Assert.That(Convert.FromBase64String(printer.GerarStreamBase64()).SequenceEqual(bytes)).IsTrue();
        printer.GerarComandos(limparBuffer: true);
        await Assert.That(printer.QuantidadeComandos).IsEqualTo(0);
    }

    private static EtqPrinter<FileConfig> Create(ProtocoloEtq protocolo)
    {
        var printer = new EtqPrinter<FileConfig> { Protocolo = protocolo };
        printer.Configuracoes.Unidade = protocolo == ProtocoloEtq.Ppla ? UnidadeEtiqueta.DecimosDeMilimetro : UnidadeEtiqueta.Dots;
        printer.Configuracoes.Largura = 800;
        printer.Configuracoes.Altura = 400;
        return printer;
    }
}
