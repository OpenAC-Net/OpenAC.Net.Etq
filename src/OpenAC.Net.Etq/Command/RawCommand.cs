namespace OpenAC.Net.Etq.Command;

/// <summary>Elemento RawCommand da etiqueta.</summary>
public sealed class RawCommand : IPrintCommand
{
    public byte[] Dados { get; set; } = [];
}
