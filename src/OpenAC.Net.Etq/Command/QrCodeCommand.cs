using OpenAC.Net.Etq.Commom;
namespace OpenAC.Net.Etq.Command;

/// <summary>Elemento QrCodeCommand da etiqueta.</summary>
public sealed class QrCodeCommand : PositionedCommand
{
    public string Texto { get; set; } = "";
    public int LarguraModulo { get; set; } = 4;
    public QrCodeErrorLevel ErrorLevel { get; set; } = QrCodeErrorLevel.M;
    public int Modelo { get; set; } = 2;
}
