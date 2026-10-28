using OpenAC.Net.Etq.Commom;
namespace OpenAC.Net.Etq.Command;

/// <summary>Elemento LineCommand da etiqueta.</summary>
public sealed class LineCommand : PositionedCommand
{
    public decimal Largura { get; set; }
    public decimal Altura { get; set; }
}
