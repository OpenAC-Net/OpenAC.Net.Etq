using OpenAC.Net.Etq.Commom;

namespace OpenAC.Net.Etq.Command;

/// <summary>Elemento BarcodeCommand da etiqueta.</summary>
public sealed class BarcodeCommand : PositionedCommand
{
    public string Texto { get; set; } = "";
    
    public TipoCodigoBarras Tipo { get; set; } = TipoCodigoBarras.Code128;
    
    public decimal Altura { get; set; } = 10;
    
    public int BarraFina { get; set; } = 2;
    
    public int BarraLarga { get; set; } = 4;
    
    public bool ExibirCodigo { get; set; } = true;
}