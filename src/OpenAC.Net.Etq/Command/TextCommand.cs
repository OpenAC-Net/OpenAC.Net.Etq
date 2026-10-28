using OpenAC.Net.Etq.Commom;
namespace OpenAC.Net.Etq.Command;

/// <summary>Elemento TextCommand da etiqueta.</summary>
public sealed class TextCommand : PositionedCommand
{
    public string Texto { get; set; } = "";
    public string? Fonte { get; set; }
    public int MultiplicadorHorizontal { get; set; } = 1;
    public int MultiplicadorVertical { get; set; } = 1;
    public int SubFonte { get; set; }
    public bool Reverso { get; set; }
}
