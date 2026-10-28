using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;
using OpenAC.Net.Etq.Interpreter.ZplII;
using OpenAC.Net.Etq.Preview;
using SkiaSharp;
namespace OpenAC.Net.Etq.Interpreter.EscLabel;

/// <summary>EscLabel baseado em ZPL II, com cores RGBA e imagens PNG.</summary>
public sealed class EscLabelInterpreter : ZplIIInterpreter
{
    public EscLabelInterpreter(Encoding encoding) : base(encoding) { }
    public override int LimiteCopias => 99999999;
    protected override string CodePage(EtqConfig s) => s.PaginaCodigo == PaginaCodigo.pc1250 ? "31" : base.CodePage(s);
    protected override string Iniciar(EtqConfig s)
    {
        if (s.Velocidade > 0) Range(s.Velocidade, 1, 14, nameof(s.Velocidade));
        var dpi = s.DPI == 203 ? 200 : s.DPI;
        return Lines("^XA", "^CI" + CodePage(s), "^S(CLR,R," + dpi, "^S(CLR,P," + dpi, "^S(CLR,Z," + dpi,
            "^S(CLS,P," + s.ToDots(s.Largura), "^S(CLS,L," + s.ToDots(s.Altura), "^S(CLS,C," + s.ToDots(s.EspacoEntreEtiquetas),
            "^S(CLM,D," + (s.Deteccao == DeteccaoEtiqueta.Continua ? "N" : s.Deteccao == DeteccaoEtiqueta.MarcaPreta ? "M" : "W"),
            s.Velocidade > 0 ? "^S(CMP,S," + s.Velocidade : "", "^PO" + (s.OrigemInferior ? "I" : "N"),
            "^MM" + (s.Guilhotina ? "C" : "T"),
            s.BackFeed == BackFeed.Padrao ? "" : s.BackFeed == BackFeed.Ativado ? "~JSA" : "~JSO");
    }
    protected override string Cor(EtqConfig s) => "^F(C" + s.CorFrente + ",D," + s.CorFundo + ",D";
    protected override byte[] Preparar(IReadOnlyList<IPrintCommand> commands, EtqConfig s)
    {
        using var output = new MemoryStream();
        foreach (var c in commands.OfType<ImageCommand>())
        {
            using var bitmap = ImageRaster.Decode(c);
            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            Append(output, Encode("^IDR:" + c.NomeImagem + ".PNG^FS~DYR:" + c.NomeImagem + ".PNG,B,P," + data.Size + ",0,"));
            Append(output, data.ToArray());
            Append(output, Encode(QuebraLinha));
        }
        return output.ToArray();
    }
    protected override byte[] Image(ImageCommand c, EtqConfig s) => Encode(Lines(Position(c, s) + "^XGR:" + c.NomeImagem + ".PNG,1,1^FS"));
}
