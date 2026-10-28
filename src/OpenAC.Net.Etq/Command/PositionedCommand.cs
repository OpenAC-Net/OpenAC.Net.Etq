using OpenAC.Net.Etq.Commom;

namespace OpenAC.Net.Etq.Command;

/// <summary>Posição e orientação de um elemento na etiqueta.</summary>
public abstract class PositionedCommand : IPrintCommand
{
    public decimal Horizontal { get; set; }
    
    public decimal Vertical { get; set; }
    
    public OrientacaoEtiqueta Orientacao { get; set; }
}