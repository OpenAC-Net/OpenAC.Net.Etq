using OpenAC.Net.Etq.Commom;
namespace OpenAC.Net.Etq.Command;

/// <summary>Elemento ImageCommand da etiqueta.</summary>
public sealed class ImageCommand : PositionedCommand
{
    public string NomeImagem { get; set; } = "G" + System.Guid.NewGuid().ToString("N").Substring(0, 7).ToUpperInvariant();
    public byte[] Imagem { get; set; } = System.Array.Empty<byte>();
    public int LarguraDots { get; set; }
    public int AlturaDots { get; set; }
    public byte Limiar { get; set; } = 128;
}
