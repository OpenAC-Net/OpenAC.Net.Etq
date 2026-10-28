using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;
using OpenAC.Net.Etq.Preview;
namespace OpenAC.Net.Etq.Interpreter.Ppla;

/// <summary>Interpretador PPLA, usando coordenadas métricas de décimos de milímetro.</summary>
public sealed class PplaInterpreter : EtqInterpreter
{
    public PplaInterpreter(Encoding encoding) : base(encoding) { }
    public override int LimiteCopias => 9999;
    public override string QuebraLinha => "\r";
    private static string Pad(int value, int digits) => Range(value, 0, digits == 3 ? 999 : 9999, nameof(value)).ToString("D" + digits, CultureInfo.InvariantCulture);
    private static int Metric(decimal value, EtqConfig s) => checked((int)decimal.Round(s.ToMilimetros(value) * 10M, 0, MidpointRounding.AwayFromZero));
    private static string Position(PositionedCommand c, EtqConfig s) => Pad(Metric(c.Vertical, s), 4) + Pad(Metric(c.Horizontal + s.MargemEsquerda, s), 4);
    private static int Rotation(PositionedCommand c) => c.Orientacao switch
    { OrientacaoEtiqueta.Graus270 => 2, OrientacaoEtiqueta.Graus180 => 3, OrientacaoEtiqueta.Graus90 => 4, _ => 1 };
    private static string Mult(int value) => value < 10 ? value.ToString(CultureInfo.InvariantCulture) : ((char)('A' + Range(value, 10, 23, nameof(value)) - 10)).ToString();
    protected override string Iniciar(EtqConfig s)
    {
        Range(s.Temperatura, 0, 20, nameof(s.Temperatura));
        if (s.Velocidade > 0) Range(s.Velocidade, 1, 4, nameof(s.Velocidade));
        if (s.PaginaCodigo != PaginaCodigo.pc437 && s.PaginaCodigo != PaginaCodigo.pc850)
            throw new NotSupportedException("PPLA suporta CP437 e CP850 nesta implementação.");
        if (s.Deteccao != DeteccaoEtiqueta.Gap || s.BackFeed != BackFeed.Padrao || s.OrigemInferior)
            throw new NotSupportedException("PPLA requer detecção, backfeed e origem configurados no equipamento.");
        return Lines("\u0002m", "\u0002KI<" + (s.PaginaCodigo == PaginaCodigo.pc437 ? "0" : "2"),
            "\u0002V" + (s.Guilhotina ? "1" : "0"), "\u0002L", "D11", "H" + Pad(s.Temperatura, 2),
            s.Velocidade > 0 ? "P" + (char)('A' + s.Velocidade - 1) : "");
    }
    protected override string Finalizar(EtqConfig s, int copias) => Lines("Q" + Pad(copias, 4), "E");
    protected override string Text(TextCommand c, EtqConfig s)
    {
        var fonte = c.Fonte ?? "2";
        if (fonte[0] < '0' || fonte[0] > '9') throw new ArgumentException("Fonte PPLA deve estar entre 0 e 9.");
        if (Encoding.GetByteCount(c.Texto) > 255) throw new ArgumentException("Texto PPLA limitado a 255 bytes.");
        return Lines(c.Reverso ? "A5" : "A1", Rotation(c) + fonte + Mult(c.MultiplicadorHorizontal) + Mult(c.MultiplicadorVertical) +
            (fonte == "9" ? Pad(c.SubFonte, 3) : "000") + Position(c, s) + c.Texto);
    }
    protected override string Barcode(BarcodeCommand c, EtqConfig s)
    {
        var tipo = c.Tipo switch
        {
            TipoCodigoBarras.Code39 => "A", TipoCodigoBarras.UPCA => "B", TipoCodigoBarras.Interleaved2of5 => "D",
            TipoCodigoBarras.Code128 => "E", TipoCodigoBarras.EAN13 => "F", TipoCodigoBarras.EAN8 => "G",
            TipoCodigoBarras.Codabar => "I", TipoCodigoBarras.Code93 => "O", TipoCodigoBarras.MSI => "K", _ => throw new NotSupportedException("Código de barras não suportado por PPLA.")
        };
        if (Encoding.GetByteCount(c.Texto) > 255) throw new ArgumentException("Código PPLA limitado a 255 bytes.");
        return Lines(Rotation(c) + (c.ExibirCodigo ? tipo : tipo.ToLowerInvariant()) + Mult(c.BarraLarga) + Mult(c.BarraFina) + Pad(Metric(c.Altura, s), 3) + Position(c, s) + c.Texto);
    }
    protected override string QrCode(QrCodeCommand c, EtqConfig s)
    {
        if (c.Orientacao != OrientacaoEtiqueta.Normal) throw new NotSupportedException("QR Code PPLA usa orientação normal.");
        return Lines("1W1D" + Mult(c.LarguraModulo) + Mult(c.LarguraModulo) + "000" + Position(c, s) + c.Modelo + "," + c.ErrorLevel + "A," + c.Texto);
    }
    protected override string Line(LineCommand c, EtqConfig s) => Lines("1X11000" + Position(c, s) + "L" + Pad(Metric(c.Largura, s), 3) + Pad(Metric(c.Altura, s), 3));
    protected override string Box(BoxCommand c, EtqConfig s) => Lines("1X11000" + Position(c, s) + "B" + Pad(Metric(c.Largura, s), 3) + Pad(Metric(c.Altura, s), 3) + Pad(Metric(c.Espessura, s), 3) + Pad(Metric(c.Espessura, s), 3));
    protected override byte[] Preparar(IReadOnlyList<IPrintCommand> commands, EtqConfig s)
    {
        var output = new StringBuilder();
        foreach (var c in commands.OfType<ImageCommand>())
        {
        var raster = ImageRaster.Create(c);
        Range(raster.BytesPerRow, 1, 255, nameof(raster.BytesPerRow));
        var builder = new StringBuilder("\u0002IAF" + c.NomeImagem + "\r");
        for (var row = raster.Height - 1; row >= 0; row--)
            builder.Append("80").Append(raster.BytesPerRow.ToString("X2", CultureInfo.InvariantCulture)).Append(raster.Hex.Substring(row * raster.BytesPerRow * 2, raster.BytesPerRow * 2)).Append('\r');
        builder.Append("FFFF\r");
        output.Append(builder);
        }
        return Encode(output.ToString());
    }
    protected override byte[] Image(ImageCommand c, EtqConfig s) => Encode(Lines("1Y01000" + Position(c, s) + c.NomeImagem));
}
