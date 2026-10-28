using System;

namespace OpenAC.Net.Etq.Commom;

/// <summary>Dimensões e parâmetros de impressão. Coordenadas usam <see cref="Unidade"/>.</summary>
public sealed class EtqConfig
{
    public UnidadeEtiqueta Unidade { get; set; } = UnidadeEtiqueta.Milimetros;
    
    public int DPI { get; set; } = 203;
    
    public decimal Largura { get; set; } = 100;
    
    public decimal Altura { get; set; } = 50;
    
    public decimal EspacoEntreEtiquetas { get; set; } = 3;
    
    public decimal MargemEsquerda { get; set; }
    
    public PaginaCodigo PaginaCodigo { get; set; } = PaginaCodigo.pc850;
    
    public int Temperatura { get; set; } = 10;
    
    public int Velocidade { get; set; } = -1;
    
    public bool Guilhotina { get; set; }
    
    public DeteccaoEtiqueta Deteccao { get; set; } = DeteccaoEtiqueta.Gap;
    
    public BackFeed BackFeed { get; set; }
    
    public EtqCor CorFrente { get; set; } = new(0, 0, 0);
    
    public EtqCor CorFundo { get; set; } = new(255, 255, 255);
    
    public bool OrigemInferior { get; set; }

    /// <summary>Converte uma medida na unidade configurada para dots da impressora.</summary>
    public int ToDots(decimal valor) =>
        (int)decimal.Round(ToMilimetros(valor) * DPI / 25.4M, 0, MidpointRounding.AwayFromZero);

    /// <summary>Converte uma medida na unidade configurada para milímetros.</summary>
    public decimal ToMilimetros(decimal valor) => Unidade switch
    {
        UnidadeEtiqueta.Milimetros => valor,
        UnidadeEtiqueta.Polegadas => valor * 25.4M,
        UnidadeEtiqueta.Dots => valor * 25.4M / DPI,
        UnidadeEtiqueta.DecimosDeMilimetro => valor / 10M,
        _ => throw new ArgumentOutOfRangeException(nameof(Unidade))
    };

    internal EtqConfig Snapshot()
    {
        if (DPI != 203 && DPI != 300 && DPI != 600)
            throw new ArgumentOutOfRangeException(nameof(DPI));

        if (Largura <= 0 || Altura <= 0 || EspacoEntreEtiquetas < 0 || MargemEsquerda < 0)
            throw new ArgumentOutOfRangeException(nameof(Largura), "Dimensões inválidas.");

        if (!Enum.IsDefined(typeof(DeteccaoEtiqueta), Deteccao) || !Enum.IsDefined(typeof(BackFeed), BackFeed))
            throw new ArgumentOutOfRangeException(nameof(Deteccao));

        var largura = ToDots(Largura);
        var altura = ToDots(Altura);

        if (largura < 1 || altura < 1 || largura > 32000 || altura > 32000 || (long)largura * altura > 64000000)
            throw new ArgumentOutOfRangeException(nameof(Largura),
                "Etiqueta limitada a 32000 dots por lado e 64 megapixels.");

        return (EtqConfig)MemberwiseClone();
    }
}