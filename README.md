# OpenAC.Net.Etq

[![NuGet Version](https://img.shields.io/nuget/v/OpenAC.Net.Etq.svg)](https://www.nuget.org/packages/OpenAC.Net.Etq/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OpenAC.Net.Etq.svg)](https://www.nuget.org/packages/OpenAC.Net.Etq/)
[![Discord](https://img.shields.io/badge/Chat%20on-Discord-purple.svg)](https://discord.com/invite/brdmJ7Yv6w)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.6.2%20%7C%204.8-blue.svg)](https://dotnet.microsoft.com/)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0%20%7C%20net9.0%20%7C%20net10.0-blue.svg)](https://dotnet.microsoft.com/)

Biblioteca .NET multiplataforma para **impressão e pré-visualização de etiquetas**, com protocolos **PPLA, PPLB, EPL2, ZPL II, EscLabel e TSPL**.

Desenvolvida no padrão do **OpenAC.Net.EscPos**, com comandos tipados em buffer, fábrica de interpretadores, comunicação pelo **OpenAC.Net.Devices** e processamento de imagens com **SkiaSharp**. O **ACBrETQ** é a referência dos formatos de impressão.

---

## 🚀 Funcionalidades

- **Texto** com posicionamento, fonte nativa, multiplicadores, orientação e impressão reversa conforme o protocolo.
- **Códigos de barras** Code39, UPC-A, Interleaved 2 of 5, Code128, EAN13, EAN8, Codabar, Code93 e MSI, conforme a linguagem. ZPL II também oferece Code11 e Standard 2 of 5.
- **QR Code** com controle de módulo, modelo e correção de erro.
- **Linhas e caixas** por coordenadas, largura, altura e espessura.
- **Imagens** PNG/JPEG/BMP convertidas para raster monocromático; EscLabel preserva imagens coloridas e oferece cores RGBA.
- **Dimensões e unidades**: milímetros, polegadas, dots e décimos de milímetro; resolução de 203, 300 ou 600 DPI.
- **Configuração** de densidade, velocidade, sensor, guilhotina e backfeed, conforme o protocolo.
- **Geração sem hardware** de comandos em bytes, Base64 e preview PNG.
- **Comunicação** Serial, TCP, RAW, arquivo, USB e Bluetooth pelos dispositivos OpenAC.
- **Demo Avalonia** com conteúdo configurável, preview, exportação e impressão.
- **Testes TUnit** de protocolos, comunicação, imagens e interface headless.

---

## 🖨️ Protocolos Suportados

| Protocolo | Interpretador | Observações |
| --- | --- | --- |
| PPLA | `PplaInterpreter` | Coordenadas métricas; cópias até 9999 |
| PPLB | `Epl2Interpreter` | Compartilha a linguagem EPL2, como no ACBr |
| EPL2 | `Epl2Interpreter` | Texto, barras, QR Code e raster GW |
| ZPL II | `ZplIIInterpreter` | Campos escapados, raster ^GF e cópias até 999 |
| EscLabel | `EscLabelInterpreter` | Extensão ZPL com cores, resolução e uploads PNG |
| TSPL | `TsplInterpreter` | Dimensões SIZE/GAP/BLINE, texto, barras, QR e BITMAP |

O suporte de cada recurso depende do firmware do equipamento. Consulte a [matriz de compatibilidade](docs/protocolos.md) para páginas de código, limites e diferenças em relação ao ACBr.

---

## 📦 Instalação

O projeto está preparado para gerar o pacote `OpenAC.Net.Etq` versão `1.0.0`. Enquanto ele não for publicado, referencie o projeto diretamente:

```xml
<ProjectReference Include="../OpenAC.Net.Etq/src/OpenAC.Net.Etq/OpenAC.Net.Etq.csproj" />
```

Após a publicação no NuGet:

```bash
dotnet add package OpenAC.Net.Etq
```

---

## 🛠️ Como Usar

### 1. Configurando a Impressora

```csharp
using OpenAC.Net.Devices;
using OpenAC.Net.Etq;
using OpenAC.Net.Etq.Commom;

using var printer = new EtqPrinter<TCPConfig>(new TCPConfig("192.168.0.100", 9100)
{
    TimeOut = 3000,
    ControlePorta = true
});

printer.Protocolo = ProtocoloEtq.ZplII;
printer.Configuracoes.Unidade = UnidadeEtiqueta.Milimetros;
printer.Configuracoes.Largura = 100;
printer.Configuracoes.Altura = 50;
printer.Configuracoes.DPI = 203;
printer.Configuracoes.PaginaCodigo = PaginaCodigo.pc850;
```

Também é possível usar `EtqPrinter<SerialConfig>`, `EtqPrinter<RawConfig>`, `EtqPrinter<FileConfig>`, `EtqPrinter<UsbConfig>` e `EtqPrinter<BluetoothConfig>`.

### 2. Montando a Etiqueta

As coordenadas são informadas na ordem **horizontal (X), vertical (Y)**, na unidade configurada. Larguras de módulos e barras finas/largas são sempre em **dots**.

```csharp
printer.IniciarEtiqueta(); // Limpa o buffer local.
printer.ImprimirCaixa(2, 2, 95, 45, espessura: 0.3M);
printer.ImprimirTexto("PRODUTO OPENAC", 5, 5);
printer.ImprimirLinha(5, 13, 60, 0.3M);
printer.ImprimirBarras("123456789012", TipoCodigoBarras.Code128, 5, 20, altura: 12);
printer.ImprimirQRCode("https://openac.net.br", 72, 5, larguraModulo: 4);
```

As opções adicionais podem ser passadas aos métodos ou por comandos tipados:

```csharp
using OpenAC.Net.Etq.Command;

printer.AdicionarComando(new TextCommand
{
    Texto = "LOTE 123",
    Horizontal = 5,
    Vertical = 40,
    MultiplicadorHorizontal = 1,
    MultiplicadorVertical = 1,
    Orientacao = OrientacaoEtiqueta.Normal,
    Reverso = false
});
```

### 3. Preview e Exportação

```csharp
using System.IO;

// Não exige conexão e preserva o buffer por padrão.
File.WriteAllBytes("etiqueta.png", printer.GerarPreview());
File.WriteAllBytes("etiqueta.prn", printer.GerarComandos(copias: 2));
string base64 = printer.GerarStreamBase64(copias: 2);
```

O preview usa as dimensões da etiqueta e aproxima as fontes do firmware. Códigos de barras e QR Codes suportados pelo renderizador são gerados com **ZXing.Net**, sem padrões fictícios. Code11 e Standard 2 of 5 não têm preview nesta versão. Comandos brutos também não são interpretados pelo renderizador.

### 4. Imprimindo

```csharp
printer.Conectar();
printer.Imprimir(copias: 2);
printer.Desconectar();
```

`Imprimir()` limpa o buffer somente depois que a escrita termina com sucesso. Utilize `limparBuffer: false` para mantê-lo. Uma falha de transporte pode ocorrer após envio parcial; o chamador deve decidir se é adequado repetir o trabalho.

### 5. Imagens

```csharp
printer.ImprimirImagem(File.ReadAllBytes("logo.png"), 70, 30, larguraDots: 120);
```

Quando somente uma dimensão é informada, a outra é calculada preservando a proporção. As imagens são convertidas para monocromático nos protocolos térmicos; EscLabel preserva as cores. Imagens que exigem armazenamento recebem nomes distintos no trabalho.

---

## 🖥️ Aplicação de Demonstração

O projeto **OpenAC.Net.Etq.Demo** utiliza **Avalonia 12.1.4 e .NET 10**, com o padrão visual do EscPos.

```bash
dotnet run --project src/OpenAC.Net.Etq.Demo
```

O demo permite escolher protocolo, DPI, dimensões, página de código e conexão. A tela de operações gera uma etiqueta com texto, barras, QR Code, caixa e imagem opcional, com exportação PNG/PRN. A conexão padrão é **arquivo**; o botão de impressão usa a conexão configurada.

Consulte o [README do demo](src/OpenAC.Net.Etq.Demo/README.md).

---

## 🧪 Compilação e Testes

Utilize o **.NET SDK 10** para a solução completa. O demo e os testes executam em .NET 10.

```bash
cd src
dotnet restore OpenAC.Net.Etq.sln
dotnet build OpenAC.Net.Etq.sln -c Release
dotnet test --solution OpenAC.Net.Etq.sln -c Release --no-build
dotnet pack OpenAC.Net.Etq/OpenAC.Net.Etq.csproj -c Release -o ../nuget
```

A biblioteca possui os mesmos alvos do EscPos: `net462`, `net48`, `netstandard2.0`, `net8.0`, `net9.0` e `net10.0`.

Os testes TUnit verificam comandos de cada protocolo, coordenadas, culturas, estados de conexão, preservação do buffer em falhas, impressão em arquivo, raster, PNG, decodificação de QR Code e execução headless do demo. Não acessam impressoras físicas.

---

## 🤝 Como Contribuir

1. Faça um Fork do repositório.
2. Crie uma Branch (`git checkout -b feature/novo-recurso`).
3. Adicione a implementação e testes com comandos esperados.
4. Faça o commit e envie sua Branch.
5. Abra um Pull Request.

Para alterações de protocolo, inclua modelo, firmware e amostras de comandos/retornos que permitam reproduzir o comportamento.

---

## 💖 Ajude o Projeto OpenAC .Net

- Participe da comunidade no [Discord](https://discord.com/invite/brdmJ7Yv6w).
- Contribua com código, testes e documentação no [GitHub](https://github.com/OpenAC-Net/OpenAC.Net.Etq).

---

## 🙏 Referências

- [OpenAC.Net.EscPos](https://github.com/OpenAC-Net/OpenAC.Net.EscPos): padrão de organização, API e comunicação.
- [ACBr](https://www.projetoacbr.com.br/): referência dos protocolos de etiquetas.
- [Zebra Programming Guide](https://www.zebra.com/content/dam/support-dam/en/documentation/unrestricted/guide/software/zpl-zbi2-pg-en.pdf): comandos ZPL II.
- [ZXing.Net](https://github.com/micjahn/ZXing.Net): geração de códigos no preview.

---

## 📄 Licença

Este projeto é distribuído sob a licença **MIT**. Consulte [LICENSE](LICENSE).
