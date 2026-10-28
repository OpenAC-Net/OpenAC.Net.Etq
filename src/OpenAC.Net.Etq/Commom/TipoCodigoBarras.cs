namespace OpenAC.Net.Etq.Commom;

/// <summary>Opções de TipoCodigoBarras para impressão de etiquetas.</summary>
public enum TipoCodigoBarras
{
    /// <summary>Code39.</summary>
    Code39 = 0,
    /// <summary>UPCA.</summary>
    UPCA = 1,
    /// <summary>Interleaved2of5.</summary>
    Interleaved2of5 = 2,
    /// <summary>Code128.</summary>
    Code128 = 3,
    /// <summary>EAN13.</summary>
    EAN13 = 4,
    /// <summary>EAN8.</summary>
    EAN8 = 5,
    /// <summary>Codabar.</summary>
    Codabar = 6,
    /// <summary>Code93.</summary>
    Code93 = 7,
    /// <summary>MSI.</summary>
    MSI = 8,
    /// <summary>Code11.</summary>
    Code11 = 9,
    /// <summary>Standard2of5.</summary>
    Standard2of5 = 10
}
