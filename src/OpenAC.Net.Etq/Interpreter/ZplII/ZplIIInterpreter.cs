using System;
using System.Globalization;
using System.Text;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;
using OpenAC.Net.Etq.Preview;

namespace OpenAC.Net.Etq.Interpreter.ZplII;

/// <summary>Interpretador ZPL II. Campos são escapados com ^FH para preservar dados.</summary>
public class ZplIIInterpreter(Encoding encoding) : EtqInterpreter(encoding)
{
    public override int LimiteCopias => 999;

    public override string QuebraLinha => "\r\n";

    protected virtual string Cor(EtqConfig config) => "";

    protected virtual string CodePage(EtqConfig config) => config.PaginaCodigo switch
    {
        PaginaCodigo.pc437 => "0", 
        PaginaCodigo.pc850 => "13", 
        PaginaCodigo.pc1252 => "27", 
        PaginaCodigo.pcUTF8 => "28",
        _ => throw new NotSupportedException("Página de código não suportada pelo interpretador ZPL II.")
    };

    protected override string Iniciar(EtqConfig s)
    {
        Range(s.Temperatura, 0, 30, nameof(s.Temperatura));
        if (s.Velocidade > 0) Range(s.Velocidade, 2, 12, nameof(s.Velocidade));
        return Lines("^XA", "^PW" + s.ToDots(s.Largura), "^LL" + s.ToDots(s.Altura), "^LH0,0",
            "^CI" + CodePage(s), "~SD" + s.Temperatura.ToString("D2", CultureInfo.InvariantCulture),
            "^PO" + (s.OrigemInferior ? "I" : "N"), "^MM" + (s.Guilhotina ? "C" : "T"),
            s.Deteccao == DeteccaoEtiqueta.Continua ? "^MNN" :
            s.Deteccao == DeteccaoEtiqueta.MarcaPreta ? "^MNM" : "^MNY",
            s.BackFeed == BackFeed.Padrao ? "" : s.BackFeed == BackFeed.Ativado ? "~JSA" : "~JSO",
            s.Velocidade > 0 ? "^PR" + s.Velocidade : "");
    }

    protected override string Finalizar(EtqConfig s, int copias) => Lines("^PQ" + copias, "^XZ");

    protected static char Orientation(OrientacaoEtiqueta orientation) => orientation switch
    {
        OrientacaoEtiqueta.Graus90 => 'R', OrientacaoEtiqueta.Graus180 => 'I', OrientacaoEtiqueta.Graus270 => 'B',
        _ => 'N'
    };

    protected string Position(PositionedCommand c, EtqConfig s) => "^FO" + X(c, s) + "," + Y(c, s) + Cor(s);

    protected string Field(string texto)
    {
        var builder = new StringBuilder("^FH\\^FD");
        foreach (var b in Encoding.GetBytes(texto))
        {
            if (b == '^' || b == '~' || b == '\\' || b < 32 || b > 126)
                builder.Append('\\').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            else builder.Append((char)b);
        }

        return builder.Append("^FS").ToString();
    }

    protected override string Text(TextCommand c, EtqConfig s) => Lines(Position(c, s) + "^A" + (c.Fonte ?? "0") +
                                                                        Orientation(c.Orientacao) + "," +
                                                                        24 * c.MultiplicadorVertical + "," +
                                                                        16 * c.MultiplicadorHorizontal +
                                                                        (c.Reverso ? "^FR" : "") + Field(c.Texto));

    protected override string Barcode(BarcodeCommand c, EtqConfig s)
    {
        var tipo = c.Tipo switch
        {
            TipoCodigoBarras.Code39 => "3", TipoCodigoBarras.UPCA => "U", TipoCodigoBarras.Interleaved2of5 => "2",
            TipoCodigoBarras.Code128 => "C", TipoCodigoBarras.EAN13 => "E", TipoCodigoBarras.EAN8 => "8",
            TipoCodigoBarras.Codabar => "K", TipoCodigoBarras.Code93 => "A", TipoCodigoBarras.MSI => "M",
            TipoCodigoBarras.Code11 => "1", TipoCodigoBarras.Standard2of5 => "J", _ => throw new NotSupportedException()
        };
        return Lines(Position(c, s) + "^BY" + c.BarraFina + "," + N((decimal)c.BarraLarga / c.BarraFina) +
                     "^B" + tipo + Orientation(c.Orientacao) + "," + s.ToDots(c.Altura) + "," +
                     (c.ExibirCodigo ? "Y" : "N") + ",N" + Field(c.Texto));
    }

    protected override string QrCode(QrCodeCommand c, EtqConfig s) => Lines(
        Position(c, s) + "^BQ" + Orientation(c.Orientacao) + "," +
        c.Modelo + "," + c.LarguraModulo + Field(c.ErrorLevel + "A," + c.Texto));

    protected override string Line(LineCommand c, EtqConfig s) => Lines(Position(c, s) + "^GB" + s.ToDots(c.Largura) +
                                                                        "," +
                                                                        s.ToDots(c.Altura) + "," +
                                                                        Math.Max(1,
                                                                            s.ToDots(Math.Min(c.Largura, c.Altura))) +
                                                                        ",B,0^FS");

    protected override string Box(BoxCommand c, EtqConfig s) => Lines(Position(c, s) + "^GB" + s.ToDots(c.Largura) +
                                                                      "," +
                                                                      s.ToDots(c.Altura) + "," +
                                                                      Math.Max(1, s.ToDots(c.Espessura)) + ",B," +
                                                                      Range(c.Canto, 0, 8, nameof(c.Canto)) + "^FS");

    protected override byte[] Image(ImageCommand c, EtqConfig s)
    {
        var raster = ImageRaster.Create(c);
        return Encode(Lines(Position(c, s) + "^GFA," + raster.Data.Length + "," + raster.Data.Length + "," +
                            raster.BytesPerRow + "," + raster.Hex + "^FS"));
    }
}