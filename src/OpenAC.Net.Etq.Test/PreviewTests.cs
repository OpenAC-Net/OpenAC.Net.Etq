using OpenAC.Net.Devices;
using OpenAC.Net.Etq.Commom;
using SkiaSharp;
using ZXing;

namespace OpenAC.Net.Etq.Test;

public class PreviewTests
{
    [Test]
    [Arguments(ProtocoloEtq.Ppla)]
    [Arguments(ProtocoloEtq.Pplb)]
    [Arguments(ProtocoloEtq.Epl2)]
    [Arguments(ProtocoloEtq.ZplII)]
    [Arguments(ProtocoloEtq.Tspl)]
    [Arguments(ProtocoloEtq.EscLabel)]
    public async Task PreviewGeraPngComDimensoesFisicas(ProtocoloEtq protocolo)
    {
        using var printer = new EtqPrinter<FileConfig> { Protocolo = protocolo };
        printer.ImprimirTexto("OPENAC", 5, 5);
        printer.ImprimirCaixa(2, 2, 95, 45);
        printer.ImprimirBarras("123456789012", TipoCodigoBarras.Code128, 5, 20);
        printer.ImprimirQRCode("https://openac.net.br", 70, 5);
        using var bitmap = SKBitmap.Decode(printer.GerarPreview());
        await Assert.That(bitmap.Width).IsEqualTo(799);
        await Assert.That(bitmap.Height).IsEqualTo(400);
        await Assert.That(bitmap.Pixels.Any(c => c.Red < 100)).IsTrue();
        await Assert.That(printer.QuantidadeComandos).IsEqualTo(4);
        using var output = new MemoryStream();
        printer.GerarPreview(output, limparBuffer: true);
        await Assert.That(output.Length).IsGreaterThan(0);
        await Assert.That(printer.QuantidadeComandos).IsEqualTo(0);
    }

    [Test]
    public async Task QrDoPreviewPodeSerDecodificado()
    {
        using var printer = new EtqPrinter<FileConfig>();
        printer.ImprimirQRCode("OPENAC-ETQ-123", 5, 5);
        using var bitmap = SKBitmap.Decode(printer.GerarPreview());
        var pixels = new byte[bitmap.Width * bitmap.Height * 4];
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            var p = bitmap.GetPixel(x, y); var i = (y * bitmap.Width + x) * 4;
            pixels[i] = p.Red; pixels[i + 1] = p.Green; pixels[i + 2] = p.Blue; pixels[i + 3] = p.Alpha;
        }
        var source = new RGBLuminanceSource(pixels, bitmap.Width, bitmap.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
        var decoded = new BarcodeReaderGeneric().Decode(source);
        await Assert.That(decoded?.Text).IsEqualTo("OPENAC-ETQ-123");
    }

    [Test]
    public async Task RasterPreservaPolaridadeEPreencheBitsDePadding()
    {
        using var bitmap = new SKBitmap(8, 1);
        bitmap.Erase(SKColors.White); bitmap.SetPixel(0, 0, SKColors.Black); bitmap.SetPixel(7, 0, SKColors.Black);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        foreach (var protocolo in new[] { ProtocoloEtq.ZplII, ProtocoloEtq.Epl2, ProtocoloEtq.Tspl, ProtocoloEtq.Ppla, ProtocoloEtq.EscLabel })
        {
            using var printer = new EtqPrinter<FileConfig> { Protocolo = protocolo };
            printer.Configuracoes.Unidade = UnidadeEtiqueta.Dots;
            printer.Configuracoes.Largura = 200; printer.Configuracoes.Altura = 100;
            printer.ImprimirImagem(png.ToArray(), 10, 20);
            var bytes = printer.GerarComandos();
            var text = System.Text.Encoding.GetEncoding(28591).GetString(bytes);
            if (protocolo == ProtocoloEtq.ZplII) await Assert.That(text).Contains("^GFA,1,1,1,81");
            if (protocolo == ProtocoloEtq.Epl2) await Assert.That(text).Contains("GW10,20,1,1," + (char)0x81);
            if (protocolo == ProtocoloEtq.Tspl) await Assert.That(text).Contains("BITMAP 10,20,1,1,0," + (char)0x7E);
            if (protocolo == ProtocoloEtq.Ppla) await Assert.That(text.IndexOf("\u0002IAF", StringComparison.Ordinal) < text.IndexOf("\u0002L", StringComparison.Ordinal)).IsTrue();
            using var preview = SKBitmap.Decode(printer.GerarPreview());
            await Assert.That(preview.GetPixel(10, 20).Red).IsEqualTo((byte)0);
            await Assert.That(preview.GetPixel(11, 20).Red).IsEqualTo((byte)255);
        }
    }

    [Test]
    public async Task DuasImagensPossuemIdentificadoresDistintosEUploadAntesDaEtiqueta()
    {
        using var bitmap = new SKBitmap(8, 1); bitmap.Erase(SKColors.Black);
        using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
        using var printer = new EtqPrinter<FileConfig> { Protocolo = ProtocoloEtq.EscLabel };
        printer.ImprimirImagem(png.ToArray(), 5, 5); printer.ImprimirImagem(png.ToArray(), 10, 10);
        var bytes = printer.GerarComandos();
        var text = System.Text.Encoding.GetEncoding(28591).GetString(bytes);
        var names = System.Text.RegularExpressions.Regex.Matches(text, @"\^XGR:(G[A-F0-9]{7})\.PNG").Select(m => m.Groups[1].Value).ToArray();
        await Assert.That(names.Length).IsEqualTo(2);
        await Assert.That(names[0] != names[1]).IsTrue();
        await Assert.That(printer.GerarComandos().SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task ErrosDePreviewPreservamBuffer()
    {
        using var printer = new EtqPrinter<FileConfig>();
        printer.ImprimirComando([1, 2, 3]);
        await Assert.That(() => printer.GerarPreview(limparBuffer: true)).Throws<NotSupportedException>();
        await Assert.That(printer.QuantidadeComandos).IsEqualTo(1);
        printer.Clear(); printer.ImprimirImagem([1, 2, 3], 0, 0);
        await Assert.That(() => printer.GerarComandos()).Throws<ArgumentException>();
        await Assert.That(printer.QuantidadeComandos).IsEqualTo(1);
    }
}
