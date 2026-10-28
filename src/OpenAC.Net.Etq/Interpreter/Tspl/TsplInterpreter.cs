using System;
using System.Text;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;
using OpenAC.Net.Etq.Preview;

namespace OpenAC.Net.Etq.Interpreter.Tspl;

/// <summary>Interpretador TSPL/TSPL2.</summary>
public sealed class TsplInterpreter(Encoding encoding) : EtqInterpreter(encoding)
{
    public override int LimiteCopias => 65535;
    
    public override string QuebraLinha => "\r\n";

    protected override string Iniciar(EtqConfig s)
    {
        Range(s.Temperatura, 0, 15, nameof(s.Temperatura));
        if (s.Velocidade > 0 && s.Velocidade != 1 && s.Velocidade != 2 && s.Velocidade != 3 && s.Velocidade != 4 &&
            s.Velocidade != 5 && s.Velocidade != 6 && s.Velocidade != 8 && s.Velocidade != 10 && s.Velocidade != 12)
            throw new ArgumentOutOfRangeException(nameof(s.Velocidade));
        return Lines("SIZE " + N(s.ToMilimetros(s.Largura)) + " mm," + N(s.ToMilimetros(s.Altura)) + " mm",
            (s.Deteccao == DeteccaoEtiqueta.MarcaPreta ? "BLINE " : "GAP ") + (s.Deteccao == DeteccaoEtiqueta.Continua
                ? "0 mm,0 mm"
                : N(s.ToMilimetros(s.EspacoEntreEtiquetas)) + " mm,0 mm"),
            "DIRECTION " + (s.OrigemInferior ? "1" : "0"), "REFERENCE 0,0", "SHIFT 0", "DENSITY " + s.Temperatura,
            "CODEPAGE " + (s.PaginaCodigo == PaginaCodigo.pcUTF8
                ? "UTF-8"
                : ((int)s.PaginaCodigo).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            "SET CUTTER " + (s.Guilhotina ? "BATCH" : "OFF"), s.Velocidade > 0 ? "SPEED " + s.Velocidade : "",
            s.BackFeed == BackFeed.Padrao ? "" : "SET BACK " + (s.BackFeed == BackFeed.Ativado ? "ON" : "OFF"), "CLS");
    }

    protected override string Finalizar(EtqConfig s, int copias) => Lines("PRINT " + copias);
    
    private static string Position(PositionedCommand c, EtqConfig s) => X(c, s) + "," + Y(c, s);
    
    private static string Quote(string value) => "\"" + value.Replace("\"", "\\[\"]") + "\"";

    protected override string Text(TextCommand c, EtqConfig s)
    {
        var fonte = c.Fonte ?? "3";
        if (fonte[0] < '0' || fonte[0] > '8') throw new ArgumentException("Fonte TSPL deve estar entre 0 e 8.");
        Range(c.MultiplicadorHorizontal, 1, 10, nameof(c.MultiplicadorHorizontal));
        Range(c.MultiplicadorVertical, 1, 10, nameof(c.MultiplicadorVertical));
        var texto = Lines("TEXT " + Position(c, s) + ",\"" + fonte + "\"," + (int)c.Orientacao + "," +
                          c.MultiplicadorHorizontal + "," + c.MultiplicadorVertical + "," + Quote(c.Texto));
        if (c.Reverso && c.Orientacao != OrientacaoEtiqueta.Normal)
            throw new NotSupportedException("Texto TSPL reverso rotacionado requer região explícita.");
        return texto + (c.Reverso
            ? Lines("REVERSE " + Position(c, s) + "," + c.Texto.Length * 16 * c.MultiplicadorHorizontal + "," +
                    24 * c.MultiplicadorVertical)
            : "");
    }

    protected override string Barcode(BarcodeCommand c, EtqConfig s)
    {
        var tipo = c.Tipo switch
        {
            TipoCodigoBarras.EAN13 => "EAN13", TipoCodigoBarras.EAN8 => "EAN8", TipoCodigoBarras.UPCA => "UPCA",
            TipoCodigoBarras.Interleaved2of5 => "25", TipoCodigoBarras.Code128 => "128",
            TipoCodigoBarras.Code39 => "39",
            TipoCodigoBarras.Code93 => "93", TipoCodigoBarras.Codabar => "CODA", TipoCodigoBarras.MSI => "MSI",
            _ => throw new NotSupportedException("Tipo de código não suportado por TSPL.")
        };
        return Lines("BARCODE " + Position(c, s) + ",\"" + tipo + "\"," + s.ToDots(c.Altura) + "," +
                     (c.ExibirCodigo ? "1" : "0") + "," +
                     (int)c.Orientacao + "," + c.BarraFina + "," + c.BarraLarga + "," + Quote(c.Texto));
    }

    protected override string QrCode(QrCodeCommand c, EtqConfig s) => Lines(
        "QRCODE " + Position(c, s) + "," + c.ErrorLevel + "," +
        c.LarguraModulo + ",A," + (int)c.Orientacao + ",M" + c.Modelo + "," + Quote(c.Texto));

    protected override string Line(LineCommand c, EtqConfig s) =>
        Lines("BAR " + Position(c, s) + "," + s.ToDots(c.Largura) + "," + s.ToDots(c.Altura));

    protected override string Box(BoxCommand c, EtqConfig s) => Lines("BOX " + Position(c, s) + "," +
                                                                      s.ToDots(c.Horizontal + s.MargemEsquerda +
                                                                               c.Largura) + "," +
                                                                      s.ToDots(c.Vertical + c.Altura) + "," +
                                                                      Math.Max(1, s.ToDots(c.Espessura)));

    protected override byte[] Image(ImageCommand c, EtqConfig s)
    {
        var raster = ImageRaster.Create(c);
        // TSPL BITMAP usa 0 para preto e 1 para branco (o inverso do raster EPL/ZPL).
        var data = (byte[])raster.Data.Clone();
        for (var i = 0; i < data.Length; i++) data[i] = (byte)~data[i];
        return Concat(Encode("BITMAP " + Position(c, s) + "," + raster.BytesPerRow + "," + raster.Height + ",0,"), data,
            Encode(QuebraLinha));
    }
}