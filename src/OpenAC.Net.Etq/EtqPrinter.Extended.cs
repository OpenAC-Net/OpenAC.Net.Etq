using System;
using OpenAC.Net.Devices;
namespace OpenAC.Net.Etq;
/// <summary>Impressora de etiquetas com configuração de dispositivo fortemente tipada.</summary>
public sealed class EtqPrinter<TConfig> : EtqPrinter where TConfig : IDeviceConfig
{
    public EtqPrinter() : base(Activator.CreateInstance<TConfig>()) { }
    public EtqPrinter(TConfig device) : base(device) { }
    public new TConfig Device => (TConfig)base.Device;
}
