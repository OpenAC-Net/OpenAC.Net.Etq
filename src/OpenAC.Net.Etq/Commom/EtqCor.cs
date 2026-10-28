namespace OpenAC.Net.Etq.Commom;
/// <summary>Cor RGBA para impressoras EscLabel.</summary>
public readonly struct EtqCor
{
    public EtqCor(byte vermelho, byte verde, byte azul, byte opacidade = 255)
    { Vermelho = vermelho; Verde = verde; Azul = azul; Opacidade = opacidade; }
    public byte Vermelho { get; }
    public byte Verde { get; }
    public byte Azul { get; }
    public byte Opacidade { get; }
    public override string ToString() => $"{Vermelho},{Verde},{Azul},{Opacidade}";
}
