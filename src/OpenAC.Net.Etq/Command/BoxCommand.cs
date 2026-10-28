using OpenAC.Net.Etq.Commom;
namespace OpenAC.Net.Etq.Command;

/// <summary>Elemento BoxCommand da etiqueta.</summary>
public sealed class BoxCommand : PositionedCommand
{
    public decimal Largura { get; set; }
    public decimal Altura { get; set; }
    public decimal Espessura { get; set; } = 0.3M;
    public int Canto { get; set; }
}
