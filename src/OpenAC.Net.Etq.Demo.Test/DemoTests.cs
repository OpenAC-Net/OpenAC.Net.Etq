using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using OpenAC.Net.Etq.Commom;
using OpenAC.Net.Etq.Demo;

namespace OpenAC.Net.Etq.Demo.Test;

public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).UseSkia().WithInterFont();
}
public class DemoTests
{
    [Test]
    public async Task JanelaGeraComandosEPreviewSemEquipamento()
    {
        await using var session = HeadlessUnitTestSession.StartNew(typeof(TestApp));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resultado = await session.Dispatch(async () =>
        {
            var window = new MainWindow(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));
            window.Show();
            window.FindControl<Button>("GenerateButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var status = window.FindControl<TextBlock>("StatusText")!;
            for (var i = 0; i < 200 && !status.Text!.StartsWith("Etiqueta gerada", StringComparison.Ordinal); i++) await Task.Delay(10);
            var png = window.FindControl<Image>("PreviewImage")!.Source;
            var commands = window.FindControl<TextBox>("CommandsText")!.Text;
            window.FindControl<ListBox>("Navigation")!.SelectedIndex = 1;
            var settingsVisible = window.FindControl<ScrollViewer>("SettingsPage")!.IsVisible;
            window.FindControl<ComboBox>("ProtocolCombo")!.SelectedItem = ProtocoloEtq.Tspl;
            window.FindControl<ListBox>("Navigation")!.SelectedIndex = 0;
            window.FindControl<Button>("GenerateButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            for (var i = 0; i < 200 && window.FindControl<Grid>("WorkPanel")!.IsEnabled == false; i++) await Task.Delay(10);
            var tspl = window.FindControl<TextBox>("CommandsText")!.Text;
            window.CaptureRenderedFrame()?.Save(Path.Combine(Path.GetTempPath(), "openac-etq-demo.png"), PngBitmapEncoderOptions.Default);
            window.Close();
            return (Preview: png != null, Commands: commands, Settings: settingsVisible, Tspl: tspl);
        }, timeout.Token);
        await Assert.That(resultado.Preview).IsTrue();
        await Assert.That(resultado.Commands).Contains("^XA");
        await Assert.That(resultado.Settings).IsTrue();
        await Assert.That(resultado.Tspl).Contains("SIZE 100 mm,50 mm");
    }
}
