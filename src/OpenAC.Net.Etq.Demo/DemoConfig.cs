using System.IO.Ports;
using OpenAC.Net.Etq.Commom;
namespace OpenAC.Net.Etq.Demo;
internal sealed class DemoConfig
{
    public ProtocoloEtq Protocolo { get; set; } = ProtocoloEtq.ZplII;
    public int DPI { get; set; } = 203;
    public PaginaCodigo PaginaCodigo { get; set; } = PaginaCodigo.pc850;
    public decimal Largura { get; set; } = 100;
    public decimal Altura { get; set; } = 50;
    public decimal Gap { get; set; } = 3;
    public int Temperatura { get; set; } = 10;
    public int Velocidade { get; set; } = -1;
    public bool Guilhotina { get; set; }
    public string Conexao { get; set; } = "Arquivo";
    public string Endereco { get; set; } = "etiqueta.prn";
    public int PortaTcp { get; set; } = 9100;
    public int Baud { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public Parity Parity { get; set; }
    public StopBits StopBits { get; set; } = StopBits.One;
    public Handshake Handshake { get; set; }
    public int Timeout { get; set; } = 3000;
}
