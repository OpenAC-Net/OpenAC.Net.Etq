using System.IO.Ports;
using System.Text;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using OpenAC.Net.Devices;
using OpenAC.Net.Etq.Commom;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace OpenAC.Net.Etq.Demo;

public partial class MainWindow : Window
{
    private readonly string configurationPath;
    private byte[]? imageBytes;
    private byte[]? previewBytes;
    private byte[]? commandBytes;
    private Bitmap? previewBitmap;
    private Task? currentOperation;
    private bool canClose;
    private bool closing;

    public MainWindow() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenAC.Net.Etq", "demo.json")) { }
    internal MainWindow(string configPath)
    {
        configurationPath = configPath;
        InitializeComponent();
        ProtocolCombo.ItemsSource = Enum.GetValues<ProtocoloEtq>();
        DpiCombo.ItemsSource = new[] { 203, 300, 600 };
        EncodingCombo.ItemsSource = Enum.GetValues<PaginaCodigo>();
        BarcodeCombo.ItemsSource = Enum.GetValues<TipoCodigoBarras>();
        BarcodeCombo.SelectedItem = TipoCodigoBarras.Code128;
        ConnectionCombo.ItemsSource = new[] { "Arquivo", "Serial", "TCP", "RAW", "USB", "Bluetooth" };
        ParityCombo.ItemsSource = Enum.GetValues<Parity>();
        StopCombo.ItemsSource = new[] { StopBits.One, StopBits.Two, StopBits.OnePointFive };
        FlowCombo.ItemsSource = Enum.GetValues<Handshake>();
        Apply(new DemoConfig());
        try
        {
            if (File.Exists(configurationPath)) Apply(JsonSerializer.Deserialize<DemoConfig>(File.ReadAllText(configurationPath)) ?? new DemoConfig());
        }
        catch (Exception ex) { StatusText.Text = $"Configuração não carregada: {ex.Message}"; }
        Closing += OnClosing;
    }
    private void Apply(DemoConfig c)
    {
        ProtocolCombo.SelectedItem = c.Protocolo; DpiCombo.SelectedItem = c.DPI; EncodingCombo.SelectedItem = c.PaginaCodigo;
        WidthInput.Value = c.Largura; HeightInput.Value = c.Altura; GapInput.Value = c.Gap;
        DensityInput.Value = c.Temperatura; SpeedInput.Value = c.Velocidade; CutterCheck.IsChecked = c.Guilhotina;
        ConnectionCombo.SelectedItem = c.Conexao; AddressText.Text = c.Endereco; TcpPortInput.Value = c.PortaTcp;
        BaudInput.Value = c.Baud; BitsInput.Value = c.DataBits; ParityCombo.SelectedItem = c.Parity;
        StopCombo.SelectedItem = c.StopBits; FlowCombo.SelectedItem = c.Handshake; TimeoutInput.Value = c.Timeout;
    }
    private DemoConfig Capture() => new()
    {
        Protocolo = (ProtocoloEtq)(ProtocolCombo.SelectedItem ?? ProtocoloEtq.ZplII),
        DPI = (int)(DpiCombo.SelectedItem ?? 203), PaginaCodigo = (PaginaCodigo)(EncodingCombo.SelectedItem ?? PaginaCodigo.pc850),
        Largura = WidthInput.Value ?? 100, Altura = HeightInput.Value ?? 50, Gap = GapInput.Value ?? 3,
        Temperatura = (int)(DensityInput.Value ?? 10), Velocidade = (int)(SpeedInput.Value ?? -1), Guilhotina = CutterCheck.IsChecked == true,
        Conexao = ConnectionCombo.SelectedItem?.ToString() ?? "Arquivo", Endereco = AddressText.Text?.Trim() ?? "",
        PortaTcp = (int)(TcpPortInput.Value ?? 9100), Baud = (int)(BaudInput.Value ?? 9600), DataBits = (int)(BitsInput.Value ?? 8),
        Parity = (Parity)(ParityCombo.SelectedItem ?? Parity.None), StopBits = (StopBits)(StopCombo.SelectedItem ?? StopBits.One),
        Handshake = (Handshake)(FlowCombo.SelectedItem ?? Handshake.None), Timeout = (int)(TimeoutInput.Value ?? 3000)
    };
    private EtqPrinter CreateLabel(bool forPrinting = false)
    {
        var c = Capture();
        EtqPrinter printer = !forPrinting ? new EtqPrinter<FileConfig>() : c.Conexao switch
        {
            "Serial" => new EtqPrinter<SerialConfig>(new SerialConfig { Porta = c.Endereco, Baud = c.Baud, DataBits = c.DataBits, Parity = c.Parity, StopBits = c.StopBits, Handshake = c.Handshake }),
            "TCP" => new EtqPrinter<TCPConfig>(new TCPConfig(c.Endereco, c.PortaTcp)),
            "RAW" => new EtqPrinter<RawConfig>(new RawConfig(c.Endereco)),
            "USB" => new EtqPrinter<UsbConfig>(new UsbConfig(c.Endereco)),
            "Bluetooth" => new EtqPrinter<BluetoothConfig>(new BluetoothConfig(c.Endereco)),
            _ => new EtqPrinter<FileConfig>(new FileConfig(c.Endereco) { CreateIfNotExists = true })
        };
        printer.Protocolo = c.Protocolo;
        printer.Device.TimeOut = c.Timeout;
        printer.Configuracoes.Largura = c.Largura; printer.Configuracoes.Altura = c.Altura; printer.Configuracoes.EspacoEntreEtiquetas = c.Gap;
        printer.Configuracoes.DPI = c.DPI; printer.Configuracoes.PaginaCodigo = c.PaginaCodigo; printer.Configuracoes.Temperatura = c.Temperatura;
        printer.Configuracoes.Velocidade = c.Velocidade; printer.Configuracoes.Guilhotina = c.Guilhotina;
        printer.ImprimirCaixa(2, 2, c.Largura - 4, c.Altura - 4);
        printer.ImprimirTexto(LabelText.Text ?? "", 5, 5);
        printer.ImprimirLinha(5, 13, Math.Max(1, c.Largura - 40), 0.3M);
        printer.ImprimirBarras(BarcodeText.Text ?? "", (TipoCodigoBarras)(BarcodeCombo.SelectedItem ?? TipoCodigoBarras.Code128), 5, 19, 12);
        printer.ImprimirQRCode(QrText.Text ?? "", Math.Max(5, c.Largura - 27), 5);
        if (imageBytes != null) printer.ImprimirImagem(imageBytes, Math.Max(5, c.Largura - 27), 30, 120);
        return printer;
    }
    private async Task RenderAsync()
    {
        using var printer = CreateLabel();
        var copies = (int)(CopiesInput.Value ?? 1);
        var result = await Task.Run(() => (Commands: printer.GerarComandos(copies), Preview: printer.GerarPreview()));
        if (closing) return;
        commandBytes = result.Commands; previewBytes = result.Preview;
        var next = new Bitmap(new MemoryStream(previewBytes));
        PreviewImage.Source = next;
        previewBitmap?.Dispose(); previewBitmap = next;
        CommandsText.Text = Escape(commandBytes);
        DimensionsText.Text = $"{printer.Configuracoes.Largura} × {printer.Configuracoes.Altura} mm • {printer.Configuracoes.DPI} DPI • {commandBytes.Length} bytes";
        StatusText.Text = "Etiqueta gerada. O preview usa fontes aproximadas; valide o resultado no equipamento.";
    }
    private static string Escape(byte[] data)
    {
        var text = new StringBuilder();
        foreach (var b in data)
            text.Append(b == 13 ? "\\r" : b == 10 ? "\\n\n" : b < 32 || b > 126 ? $"\\x{b:X2}" : ((char)b).ToString());
        return text.ToString();
    }
    private async Task Run(Func<Task> action)
    {
        if (currentOperation != null || closing) return;
        WorkPanel.IsEnabled = false;
        StatusText.Text = "Aguarde…";
        try { currentOperation = Execute(action); await currentOperation; }
        catch (Exception ex) { StatusText.Text = ex.Message; }
        finally { currentOperation = null; WorkPanel.IsEnabled = !closing; }
    }
    private static async Task Execute(Func<Task> action) { await Task.Yield(); await action(); }
    private async void Generate(object? sender, RoutedEventArgs e) => await Run(RenderAsync);
    private async void Print(object? sender, RoutedEventArgs e) => await Run(async () =>
    {
        using var printer = CreateLabel(true);
        var copies = (int)(CopiesInput.Value ?? 1);
        // Gera todos os comandos antes de abrir a conexão para falhar sem escrita parcial.
        printer.GerarComandos(copies);
        await Task.Run(() => { printer.Conectar(); printer.Imprimir(copies); });
        StatusText.Text = "Trabalho enviado à conexão configurada.";
    });
    private async void LoadImage(object? sender, RoutedEventArgs e) => await Run(async () =>
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Imagem da etiqueta", AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Imagens") { Patterns = new[] { "*.png", "*.jpg", "*.bmp" } } } });
        if (files.Count == 0) return;
        await using var input = await files[0].OpenReadAsync();
        using var output = new MemoryStream();
        await input.CopyToAsync(output);
        imageBytes = output.ToArray();
        await RenderAsync();
    });
    private async Task SaveAsync(bool png)
    {
        await RenderAsync();
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = png ? "Salvar preview" : "Exportar comandos",
            SuggestedFileName = png ? "etiqueta.png" : "etiqueta.prn", DefaultExtension = png ? "png" : "prn" });
        if (file == null) return;
        await using var output = await file.OpenWriteAsync();
        var bytes = png ? previewBytes! : commandBytes!;
        output.SetLength(0);
        await output.WriteAsync(bytes);
        StatusText.Text = $"Arquivo salvo: {file.Name}";
    }
    private async void SavePreview(object? sender, RoutedEventArgs e) => await Run(() => SaveAsync(true));
    private async void SaveCommands(object? sender, RoutedEventArgs e) => await Run(() => SaveAsync(false));
    private void SaveSettings(object? sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configurationPath)!);
            File.WriteAllText(configurationPath, JsonSerializer.Serialize(Capture(), new JsonSerializerOptions { WriteIndented = true }));
            StatusText.Text = $"Configuração salva: {configurationPath}";
        }
        catch (Exception ex) { StatusText.Text = ex.Message; }
    }
    private void Navigate(object? sender, SelectionChangedEventArgs e)
    {
        if (OperationsPage == null || SettingsPage == null) return;
        OperationsPage.IsVisible = Navigation.SelectedIndex == 0;
        SettingsPage.IsVisible = Navigation.SelectedIndex == 1;
    }
    private void ConnectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AddressLabel == null) return;
        AddressLabel.Text = ConnectionCombo.SelectedItem?.ToString() switch
        { "Serial" => "Porta serial (COM1 ou /dev/ttyUSB0)", "TCP" => "Endereço IP", "RAW" => "Nome da fila de impressão", "USB" => "Caminho do dispositivo USB", "Bluetooth" => "Endereço MAC ou porta RFCOMM", _ => "Arquivo de saída" };
    }
    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (canClose) return;
        if (currentOperation != null)
        {
            e.Cancel = true;
            if (closing) return;
            closing = true;
            try { await currentOperation; } catch { }
            canClose = true;
            previewBitmap?.Dispose();
            Close();
            return;
        }
        closing = true;
        previewBitmap?.Dispose();
    }
}
