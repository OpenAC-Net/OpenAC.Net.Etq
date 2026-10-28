using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;
using OpenAC.Net.Etq.Interpreter.Resolver;
using OpenAC.Net.Etq.Preview;

namespace OpenAC.Net.Etq.Interpreter;

/// <summary>Base dos interpretadores: comandos tipados, codificação e enquadramento da etiqueta.</summary>
public abstract class EtqInterpreter
{
    private readonly Dictionary<Type, ICommandResolver> resolvers = new();

    protected EtqInterpreter(Encoding encoding)
    {
        Encoding = encoding;
        Register<TextCommand>((c, s) => Encode(Text(c, s)));
        Register<BarcodeCommand>((c, s) => Encode(Barcode(c, s)));
        Register<QrCodeCommand>((c, s) => Encode(QrCode(c, s)));
        Register<LineCommand>((c, s) => Encode(Line(c, s)));
        Register<BoxCommand>((c, s) => Encode(Box(c, s)));
        Register<ImageCommand>(Image);
        Register<RawCommand>((c, _) => (byte[])c.Dados.Clone());
    }

    public Encoding Encoding { get; }

    public abstract int LimiteCopias { get; }

    public virtual string QuebraLinha => "\n";

    public bool HasResolver<TCommand>() where TCommand : IPrintCommand => resolvers.ContainsKey(typeof(TCommand));

    protected void Register<TCommand>(Func<TCommand, EtqConfig, byte[]> resolver) where TCommand : IPrintCommand =>
        resolvers[typeof(TCommand)] = new CommandResolver<TCommand>(resolver);

    /// <summary>Gera um trabalho completo sem abrir nem escrever no dispositivo.</summary>
    public byte[] GerarComandos(IReadOnlyList<IPrintCommand> commands, EtqConfig configuracao, int copias = 1)
    {
        if (commands == null) throw new ArgumentNullException(nameof(commands));
        var config = configuracao?.Snapshot() ?? throw new ArgumentNullException(nameof(configuracao));
        Range(copias, 1, LimiteCopias, nameof(copias));
        foreach (var command in commands) Validate(command);
        using var stream = new MemoryStream();
        Append(stream, Preparar(commands, config));
        Append(stream, Encode(Iniciar(config)));
        foreach (var command in commands)
        {
            if (!resolvers.TryGetValue(command.GetType(), out var resolver))
                throw new NotSupportedException(
                    $"Comando {command.GetType().Name} não suportado por {GetType().Name}.");
            Append(stream, resolver.Resolve(command, config));
        }

        Append(stream, Encode(Finalizar(config, copias)));
        return stream.ToArray();
    }

    protected virtual byte[] Preparar(IReadOnlyList<IPrintCommand> commands, EtqConfig config) => [];

    protected abstract string Iniciar(EtqConfig config);

    protected abstract string Finalizar(EtqConfig config, int copias);

    protected abstract string Text(TextCommand command, EtqConfig config);

    protected abstract string Barcode(BarcodeCommand command, EtqConfig config);

    protected abstract string QrCode(QrCodeCommand command, EtqConfig config);

    protected abstract string Line(LineCommand command, EtqConfig config);

    protected abstract string Box(BoxCommand command, EtqConfig config);

    protected abstract byte[] Image(ImageCommand command, EtqConfig config);

    protected byte[] Encode(string value) => Encoding.GetBytes(value);

    protected string Lines(params string[] lines) =>
        string.Join(QuebraLinha, lines.Where(s => s.Length > 0)) + QuebraLinha;

    protected static void Append(Stream stream, byte[] data) => stream.Write(data, 0, data.Length);

    protected static byte[] Concat(params byte[][] chunks)
    {
        using var stream = new MemoryStream();
        foreach (var chunk in chunks) Append(stream, chunk);
        return stream.ToArray();
    }

    protected static string N(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    protected static int X(PositionedCommand c, EtqConfig s) => s.ToDots(c.Horizontal + s.MargemEsquerda);

    protected static int Y(PositionedCommand c, EtqConfig s) => s.ToDots(c.Vertical);

    protected static int Range(int value, int min, int max, string name)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(name, value, $"Valor deve estar entre {min} e {max}.");
        return value;
    }

    protected static string Quoted(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static void Validate(IPrintCommand command)
    {
        switch (command)
        {
            case null:
                throw new ArgumentNullException(nameof(command));

            case PositionedCommand position when position.Horizontal < 0 || position.Vertical < 0:
                throw new ArgumentOutOfRangeException(nameof(position.Horizontal));

            case PositionedCommand position when !Enum.IsDefined(typeof(OrientacaoEtiqueta), position.Orientacao):
                throw new ArgumentOutOfRangeException(nameof(position.Orientacao));
        }

        switch (command)
        {
            case TextCommand text:
                Texto(text.Texto);
                Range(text.MultiplicadorHorizontal, 1, 24, nameof(text.MultiplicadorHorizontal));
                Range(text.MultiplicadorVertical, 1, 24, nameof(text.MultiplicadorVertical));
                if (text.Fonte != null && (text.Fonte.Length != 1 || !char.IsLetterOrDigit(text.Fonte[0])))
                    throw new ArgumentException("Fonte deve ser um único caractere alfanumérico.");
                break;
            case BarcodeCommand barcode:
                Texto(barcode.Texto);
                if (barcode.Texto.Length == 0 || barcode.Altura <= 0)
                    throw new ArgumentException("Código de barras vazio ou altura inválida.");
                Range(barcode.BarraFina, 1, 10, nameof(barcode.BarraFina));
                Range(barcode.BarraLarga, barcode.BarraFina, 30, nameof(barcode.BarraLarga));
                if (!Enum.IsDefined(typeof(TipoCodigoBarras), barcode.Tipo))
                    throw new ArgumentOutOfRangeException(nameof(barcode.Tipo));
                if (barcode.Tipo is TipoCodigoBarras.Code11 or TipoCodigoBarras.Standard2of5)
                {
                    if (barcode.Texto.Any(c => c < '0' || c > '9'))
                        throw new ArgumentException("Este código de barras exige dados numéricos.");
                }
                else EtqPreviewRenderer.BarcodeMatrix(barcode, 1);

                break;

            case QrCodeCommand qr:
                Texto(qr.Texto);
                if (qr.Texto.Length == 0) throw new ArgumentException("Conteúdo do QR Code vazio.");
                Range(qr.LarguraModulo, 1, 10, nameof(qr.LarguraModulo));
                Range(qr.Modelo, 1, 2, nameof(qr.Modelo));
                if (!Enum.IsDefined(typeof(QrCodeErrorLevel), qr.ErrorLevel))
                    throw new ArgumentOutOfRangeException(nameof(qr.ErrorLevel));
                break;

            case LineCommand line when line.Largura <= 0 || line.Altura <= 0:
                throw new ArgumentOutOfRangeException(nameof(line.Largura));

            case BoxCommand box when box.Largura <= 0 || box.Altura <= 0 || box.Espessura <= 0:
                throw new ArgumentOutOfRangeException(nameof(box.Largura));

            case ImageCommand image:
                if (string.IsNullOrEmpty(image.NomeImagem) || image.NomeImagem.Length > 8 ||
                    image.NomeImagem.Any(c => !((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))))
                    throw new ArgumentException("Nome da imagem deve ter de 1 a 8 caracteres A-Z ou 0-9.");
                if (image.Imagem == null || image.Imagem.Length == 0 || image.LarguraDots < 0 || image.AlturaDots < 0)
                    throw new ArgumentException("Imagem inválida.");
                if (image.Orientacao != OrientacaoEtiqueta.Normal)
                    throw new NotSupportedException(
                        "Rotação de imagem deve ser aplicada antes de adicioná-la ao buffer.");
                break;
        }

        return;

        void Texto(string texto)
        {
            if (texto == null) throw new ArgumentNullException(nameof(texto));
            if (texto.Any(char.IsControl))
                throw new ArgumentException("O texto não pode conter caracteres de controle.", nameof(texto));
        }
    }
}