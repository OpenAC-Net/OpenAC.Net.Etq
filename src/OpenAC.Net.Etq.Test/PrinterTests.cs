using OpenAC.Net.Core;
using OpenAC.Net.Devices;
using OpenAC.Net.Etq.Commom;

namespace OpenAC.Net.Etq.Test;

public class PrinterTests
{
    [Test]
    public async Task ImprimeEmArquivoComBytesExatos()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".prn");
        try
        {
            using var printer = new EtqPrinter<FileConfig>(new FileConfig(file) { CreateIfNotExists = true });
            printer.ImprimirTexto("TESTE", 5, 5);
            var esperado = printer.GerarComandos(2);
            printer.Conectar();
            printer.Imprimir(2);
            printer.Desconectar();
            await Assert.That((await File.ReadAllBytesAsync(file)).SequenceEqual(esperado)).IsTrue();
            await Assert.That(printer.QuantidadeComandos).IsEqualTo(0);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Test]
    public async Task FalhaDeEscritaPreservaBufferParaDecisaoDoChamador()
    {
        using var device = new FakeDevice { FalharEscrita = true };
        using var printer = new TestPrinter(device);
        printer.ImprimirTexto("TESTE", 0, 0);
        printer.Conectar();
        await Assert.That(() => printer.Imprimir()).Throws<IOException>();
        await Assert.That(printer.QuantidadeComandos).IsEqualTo(1);
        device.FalharEscrita = false;
        printer.Imprimir(limparBuffer: false);
        await Assert.That(printer.QuantidadeComandos).IsEqualTo(1);
        printer.Imprimir();
        await Assert.That(printer.QuantidadeComandos).IsEqualTo(0);
    }

    [Test]
    public async Task FalhaDeAberturaLiberaDispositivo()
    {
        using var device = new FakeDevice { FalharAbertura = true };
        using var printer = new TestPrinter(device);
        await Assert.That(printer.Conectar).Throws<OpenException>();
        await Assert.That(device.Liberado).IsTrue();
        await Assert.That(printer.Conectado).IsFalse();
    }

    [Test]
    public async Task ConexaoValidaEstadoELiberaNoDispose()
    {
        using var device = new FakeDevice();
        var printer = new TestPrinter(device);
        await Assert.That(() => printer.Imprimir()).Throws<OpenException>();
        printer.Conectar();
        await Assert.That(printer.Conectar).Throws<OpenException>();
        await Assert.That(() => printer.Protocolo = ProtocoloEtq.Tspl).Throws<OpenException>();
        printer.Dispose();
        await Assert.That(device.Liberado).IsTrue();
        await Assert.That(printer.Conectado).IsFalse();
        await Assert.That(printer.Conectar).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task ComandoInvalidoNaoEscreveNadaNoDispositivo()
    {
        using var device = new FakeDevice();
        using var printer = new TestPrinter(device);
        printer.ImprimirTexto("\r^XZ", 0, 0);
        printer.Conectar();
        await Assert.That(() => printer.Imprimir()).Throws<ArgumentException>();
        await Assert.That(device.Envios).IsEqualTo(0);
    }

    private sealed class TestPrinter(FakeDevice stream) : EtqPrinter(stream.Config)
    {
        protected override OpenDeviceStream CriarDispositivo() => stream;
    }

    private sealed class FakeDevice() : OpenDeviceStream(new FileConfig())
    {
        public bool FalharAbertura { get; set; }
        public bool FalharEscrita { get; set; }
        public bool Liberado { get; private set; }
        public int Envios { get; private set; }
        protected override int Available => 0;
        protected override bool OpenInternal() => !FalharAbertura;
        protected override bool CloseInternal() => true;

        public override void Write(byte[] dados)
        {
            if (FalharEscrita) throw new IOException("Falha simulada.");
            Envios++;
        }

        protected override void DisposeManaged()
        {
            Liberado = true;
            base.DisposeManaged();
        }
    }
}