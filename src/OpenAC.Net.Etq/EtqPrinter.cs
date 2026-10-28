using System;
using System.Collections.Generic;
using System.IO;
using OpenAC.Net.Core;
using OpenAC.Net.Core.Logging;
using OpenAC.Net.Devices;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;
using OpenAC.Net.Etq.Preview;

namespace OpenAC.Net.Etq;

/// <summary>Monta etiquetas em buffer, gera comandos/preview e imprime usando dispositivos OpenAC.</summary>
public abstract class EtqPrinter : OpenDisposable, IOpenLog
{
    private readonly object sincronizacao = new();
    private readonly List<IPrintCommand> commands = new();
    private OpenDeviceStream? device;
    private bool disposed;
    private ProtocoloEtq protocolo = ProtocoloEtq.ZplII;
    protected EtqPrinter(IDeviceConfig device) => Device = device ?? throw new ArgumentNullException(nameof(device));
    /// <summary>Configuração de comunicação.</summary>
    public IDeviceConfig Device { get; }
    /// <summary>Dimensões, unidade, resolução e opções da etiqueta. Configure antes de gerar ou imprimir.</summary>
    public EtqConfig Configuracoes { get; } = new();
    /// <summary>Protocolo de impressão. Só pode ser alterado com o dispositivo desconectado.</summary>
    public ProtocoloEtq Protocolo
    {
        get { lock (sincronizacao) return protocolo; }
        set
        {
            lock (sincronizacao)
            {
                if (device != null) throw new OpenException("Desconecte a impressora antes de alterar o protocolo.");
                if (!Enum.IsDefined(typeof(ProtocoloEtq), value)) throw new ArgumentOutOfRangeException(nameof(value));
                protocolo = value;
            }
        }
    }
    public bool Conectado { get { lock (sincronizacao) return device?.Conectado == true; } }
    public int QuantidadeComandos { get { lock (sincronizacao) return commands.Count; } }

    protected virtual OpenDeviceStream CriarDispositivo() => OpenDeviceFactory.Create(Device);
    /// <summary>Abre a conexão sem imprimir ou apagar o buffer.</summary>
    public void Conectar()
    {
        lock (sincronizacao)
        {
            if (disposed) throw new ObjectDisposedException(GetType().Name);
            if (device != null) throw new OpenException("A conexão já está aberta.");
            var novo = CriarDispositivo();
            try
            {
                novo.Open();
                if (!novo.Conectado) throw new OpenException("Não foi possível conectar à impressora.");
                device = novo;
            }
            catch { novo.Dispose(); throw; }
        }
    }
    /// <summary>Fecha e libera a conexão, preservando os comandos.</summary>
    public void Desconectar()
    {
        lock (sincronizacao)
        {
            if (device == null) throw new OpenException("A conexão não está aberta.");
            var atual = device;
            device = null;
            try { atual.Close(); }
            finally { atual.Dispose(); }
        }
    }
    /// <summary>Limpa apenas o buffer local de comandos.</summary>
    public void Clear() { lock (sincronizacao) commands.Clear(); }
    public void IniciarEtiqueta() => Clear();
    /// <summary>Acrescenta um comando tipado; seu estado é copiado para o buffer.</summary>
    public void AdicionarComando(IPrintCommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        lock (sincronizacao) commands.Add(CommandSnapshot.Clone(command));
    }
    public void ImprimirTexto(string texto, decimal horizontal, decimal vertical, string? fonte = null,
        int multiplicadorHorizontal = 1, int multiplicadorVertical = 1, OrientacaoEtiqueta orientacao = OrientacaoEtiqueta.Normal, bool reverso = false) =>
        AdicionarComando(new TextCommand { Texto = texto, Horizontal = horizontal, Vertical = vertical, Fonte = fonte,
            MultiplicadorHorizontal = multiplicadorHorizontal, MultiplicadorVertical = multiplicadorVertical, Orientacao = orientacao, Reverso = reverso });
    public void ImprimirBarras(string texto, TipoCodigoBarras tipo, decimal horizontal, decimal vertical, decimal altura = 10,
        int barraFina = 2, int barraLarga = 4, bool exibirCodigo = true, OrientacaoEtiqueta orientacao = OrientacaoEtiqueta.Normal) =>
        AdicionarComando(new BarcodeCommand { Texto = texto, Tipo = tipo, Horizontal = horizontal, Vertical = vertical, Altura = altura,
            BarraFina = barraFina, BarraLarga = barraLarga, ExibirCodigo = exibirCodigo, Orientacao = orientacao });
    public void ImprimirQRCode(string texto, decimal horizontal, decimal vertical, int larguraModulo = 4,
        QrCodeErrorLevel errorLevel = QrCodeErrorLevel.M, OrientacaoEtiqueta orientacao = OrientacaoEtiqueta.Normal) =>
        AdicionarComando(new QrCodeCommand { Texto = texto, Horizontal = horizontal, Vertical = vertical, LarguraModulo = larguraModulo, ErrorLevel = errorLevel, Orientacao = orientacao });
    public void ImprimirLinha(decimal horizontal, decimal vertical, decimal largura, decimal altura) =>
        AdicionarComando(new LineCommand { Horizontal = horizontal, Vertical = vertical, Largura = largura, Altura = altura });
    public void ImprimirCaixa(decimal horizontal, decimal vertical, decimal largura, decimal altura, decimal espessura = 0.3M) =>
        AdicionarComando(new BoxCommand { Horizontal = horizontal, Vertical = vertical, Largura = largura, Altura = altura, Espessura = espessura });
    public void ImprimirImagem(byte[] imagem, decimal horizontal, decimal vertical, int larguraDots = 0, int alturaDots = 0) =>
        AdicionarComando(new ImageCommand { Imagem = imagem, Horizontal = horizontal, Vertical = vertical, LarguraDots = larguraDots, AlturaDots = alturaDots });
    /// <summary>Adiciona bytes do protocolo. Comandos brutos não têm representação no preview.</summary>
    public void ImprimirComando(byte[] dados) => AdicionarComando(new RawCommand { Dados = dados });

    /// <summary>Gera o trabalho completo sem acessar o dispositivo. O buffer é preservado por padrão.</summary>
    public byte[] GerarComandos(int copias = 1, bool limparBuffer = false)
    {
        lock (sincronizacao)
        {
            var result = EtqInterpreterFactory.Create(Protocolo, Configuracoes.PaginaCodigo).GerarComandos(commands, Configuracoes, copias);
            if (limparBuffer) commands.Clear();
            return result;
        }
    }
    public string GerarStreamBase64(int copias = 1, bool limparBuffer = false) => Convert.ToBase64String(GerarComandos(copias, limparBuffer));
    /// <summary>Produz um PNG do layout sem equipamento. A imagem aproxima as fontes do firmware.</summary>
    public byte[] GerarPreview(bool limparBuffer = false)
    {
        lock (sincronizacao)
        {
            var config = Configuracoes.Snapshot();
            // As mesmas restrições de protocolo se aplicam ao preview e à impressão.
            EtqInterpreterFactory.Create(Protocolo, config.PaginaCodigo).GerarComandos(commands, config);
            var png = EtqPreviewRenderer.Render(commands, config, Protocolo);
            if (limparBuffer) commands.Clear();
            return png;
        }
    }
    public void GerarPreview(Stream stream, bool limparBuffer = false)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        lock (sincronizacao)
        {
            var png = GerarPreview();
            stream.Write(png, 0, png.Length);
            if (limparBuffer) commands.Clear();
        }
    }
    /// <summary>Envia um trabalho completo. O buffer só é limpo após escrita bem-sucedida.</summary>
    public void Imprimir(int copias = 1, bool limparBuffer = true)
    {
        lock (sincronizacao)
        {
            if (device?.Conectado != true) throw new OpenException("A conexão não está ativa.");
            var dados = GerarComandos(copias);
            this.Log().Debug($"Imprimindo etiqueta {Protocolo}: {dados.Length} bytes, {copias} cópia(s).");
            device.Write(dados);
            if (limparBuffer) commands.Clear();
        }
    }
    protected override void DisposeManaged()
    {
        lock (sincronizacao)
        {
            try { if (device != null) Desconectar(); }
            finally { disposed = true; }
        }
    }
}
