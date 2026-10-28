using System;

namespace OpenAC.Net.Etq.Command;

internal static class CommandSnapshot
{
    internal static IPrintCommand Clone(IPrintCommand c)
    {
        IPrintCommand copy = c switch
        {
            TextCommand t => new TextCommand
            {
                Texto = t.Texto, Fonte = t.Fonte, MultiplicadorHorizontal = t.MultiplicadorHorizontal,
                MultiplicadorVertical = t.MultiplicadorVertical, SubFonte = t.SubFonte, Reverso = t.Reverso
            },
            BarcodeCommand b => new BarcodeCommand
            {
                Texto = b.Texto, Tipo = b.Tipo, Altura = b.Altura, BarraFina = b.BarraFina, BarraLarga = b.BarraLarga,
                ExibirCodigo = b.ExibirCodigo
            },
            QrCodeCommand q => new QrCodeCommand
                { Texto = q.Texto, LarguraModulo = q.LarguraModulo, ErrorLevel = q.ErrorLevel, Modelo = q.Modelo },
            LineCommand l => new LineCommand { Largura = l.Largura, Altura = l.Altura },
            BoxCommand b => new BoxCommand
                { Largura = b.Largura, Altura = b.Altura, Espessura = b.Espessura, Canto = b.Canto },
            ImageCommand i => new ImageCommand
            {
                NomeImagem = i.NomeImagem,
                Imagem =
                    i.Imagem == null ? throw new ArgumentNullException(nameof(i.Imagem)) : (byte[])i.Imagem.Clone(),
                LarguraDots = i.LarguraDots, AlturaDots = i.AlturaDots, Limiar = i.Limiar
            },
            RawCommand r => new RawCommand
            {
                Dados = r.Dados == null ? throw new ArgumentNullException(nameof(r.Dados)) : (byte[])r.Dados.Clone()
            },
            _ => throw new NotSupportedException($"Comando não suportado: {c.GetType().Name}.")
        };
        
        if (c is not PositionedCommand position || copy is not PositionedCommand target) return copy;
        target.Horizontal = position.Horizontal;
        target.Vertical = position.Vertical;
        target.Orientacao = position.Orientacao;

        return copy;
    }
}