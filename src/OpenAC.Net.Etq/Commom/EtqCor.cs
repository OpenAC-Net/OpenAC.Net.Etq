namespace OpenAC.Net.Etq.Commom;

/// <summary>Cor RGBA para impressoras EscLabel.</summary>
public readonly struct EtqCor(byte vermelho, byte verde, byte azul, byte opacidade = 255)
{
    public byte Vermelho { get; } = vermelho;

    public byte Verde { get; } = verde;

    public byte Azul { get; } = azul;

    public byte Opacidade { get; } = opacidade;

    public override string ToString() => $"{Vermelho},{Verde},{Azul},{Opacidade}";
}