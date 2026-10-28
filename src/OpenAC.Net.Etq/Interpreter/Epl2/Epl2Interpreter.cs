using System;
using System.Text;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;
using OpenAC.Net.Etq.Preview;

namespace OpenAC.Net.Etq.Interpreter.Epl2;

/// <summary>Interpretador EPL2, também usado por impressoras PPLB.</summary>
public sealed class Epl2Interpreter(Encoding encoding) : EtqInterpreter(encoding)
{
    public override int LimiteCopias => 65535;

    protected override string Iniciar(EtqConfig config)
    {
        Range(config.Temperatura, 0, 15, nameof(config.Temperatura));
        if (config.Velocidade >= 0) Range(config.Velocidade, 0, 6, nameof(config.Velocidade));
        
        var code = config.PaginaCodigo switch
        {
            PaginaCodigo.pc437 => "0", PaginaCodigo.pc850 => "1", PaginaCodigo.pc852 => "2", PaginaCodigo.pc860 => "3",
            PaginaCodigo.pc1250 => "B", PaginaCodigo.pc1252 => "A",
            _ => throw new NotSupportedException("EPL2 não suporta UTF-8 nesta implementação.")
        };
        
        if (config.Deteccao == DeteccaoEtiqueta.MarcaPreta)
            throw new NotSupportedException("Detecção por marca preta EPL2 requer configuração específica do equipamento.");
        
        return Lines("N", "R0,0", "q" + config.ToDots(config.Largura),
            "Q" + config.ToDots(config.Altura) + "," +
            (config.Deteccao == DeteccaoEtiqueta.Continua ? 0 : config.ToDots(config.EspacoEntreEtiquetas)),
            "I8," + code + ",001", "D" + config.Temperatura, config.OrigemInferior ? "ZB" : "ZT", config.Guilhotina ? "OC" : "O",
            config.Velocidade >= 0 ? "S" + config.Velocidade : "",
            config.BackFeed == BackFeed.Padrao ? "" : config.BackFeed == BackFeed.Ativado ? "JF" : "JB");
    }

    protected override string Finalizar(EtqConfig s, int copias) => Lines("P" + copias);
    
    private static int Rotation(PositionedCommand c) => (int)c.Orientacao / 90;
    
    private static string Position(PositionedCommand c, EtqConfig s) => X(c, s) + "," + Y(c, s);

    protected override string Text(TextCommand c, EtqConfig s)
    {
        var fonte = c.Fonte ?? "3";
        if (fonte.Length != 1 || fonte[0] < '0' || fonte[0] > '5')
            throw new ArgumentException("Fonte EPL2 deve estar entre 0 e 5.");
        return Lines("A" + Position(c, s) + "," + Rotation(c) + "," + (fonte == "0" ? "3" : fonte) + "," +
                     c.MultiplicadorHorizontal + "," + c.MultiplicadorVertical + "," +
                     (c.Reverso ? "R" : "N") + "," + Quoted(fonte == "5" ? c.Texto.ToUpperInvariant() : c.Texto));
    }

    protected override string Barcode(BarcodeCommand c, EtqConfig s)
    {
        var tipo = c.Tipo switch
        {
            TipoCodigoBarras.EAN13 => "E30", TipoCodigoBarras.EAN8 => "E80", TipoCodigoBarras.UPCA => "UA0",
            TipoCodigoBarras.Interleaved2of5 => "2", TipoCodigoBarras.Code128 => "1", TipoCodigoBarras.Code39 => "3",
            TipoCodigoBarras.Code93 => "9", TipoCodigoBarras.Codabar => "K", TipoCodigoBarras.MSI => "L",
            _ => throw new NotSupportedException("Tipo de código não suportado por EPL2.")
        };
        
        if (c.Tipo is TipoCodigoBarras.EAN13 or TipoCodigoBarras.EAN8 or TipoCodigoBarras.UPCA)
            Range(c.BarraFina, 2, 4, nameof(c.BarraFina));
        
        return Lines("B" + Position(c, s) + "," + Rotation(c) + "," + tipo + "," + c.BarraFina + "," + c.BarraLarga +
                     "," +
                     s.ToDots(c.Altura) + "," + (c.ExibirCodigo ? "B" : "N") + "," + Quoted(c.Texto));
    }

    protected override string QrCode(QrCodeCommand c, EtqConfig s)
    {
        if (c.Orientacao != OrientacaoEtiqueta.Normal)
            throw new NotSupportedException("QR Code EPL2 usa orientação normal.");
        return Lines("b" + Position(c, s) + ",Q,m" + c.Modelo + ",s" + c.LarguraModulo + ",e" + c.ErrorLevel + ",iA," +
                     Quoted(c.Texto));
    }

    protected override string Line(LineCommand c, EtqConfig s) =>
        Lines("LO" + Position(c, s) + "," + s.ToDots(c.Largura) + "," + s.ToDots(c.Altura));

    protected override string Box(BoxCommand c, EtqConfig s) => Lines("X" + Position(c, s) + "," +
                                                                      Math.Max(1, s.ToDots(c.Espessura)) + "," +
                                                                      s.ToDots(c.Horizontal + s.MargemEsquerda +
                                                                               c.Largura) + "," +
                                                                      s.ToDots(c.Vertical + c.Altura));

    protected override byte[] Image(ImageCommand c, EtqConfig s)
    {
        var raster = ImageRaster.Create(c);
        return Concat(Encode("GW" + Position(c, s) + "," + raster.BytesPerRow + "," + raster.Height + ","), raster.Data,
            Encode(QuebraLinha));
    }
}