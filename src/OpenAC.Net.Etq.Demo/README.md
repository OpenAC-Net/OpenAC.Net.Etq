# Demo OpenAC.Net.Etq

Interface Avalonia no padrão do EscPos, com .NET 10 e tema azul OpenAC.

```sh
dotnet run --project src/OpenAC.Net.Etq.Demo
```

Em **Configurações**, selecione protocolo, DPI, página de código, tamanho da
etiqueta e conexão. A conexão inicial grava em arquivo. Os transportes disponíveis
são arquivo, serial, TCP, RAW, USB e Bluetooth.

Em **Operações**, informe texto, código de barras e conteúdo do QR. O layout de
exemplo foi dimensionado para uma etiqueta de 100 × 50 mm. **Gerar etiqueta e
preview** não abre conexão com equipamento. A aba **Comandos** mostra os bytes
escapados; os arquivos PRN exportados contêm os bytes originais, inclusive raster.

**Carregar imagem** adiciona uma imagem opcional ao exemplo. **Salvar PNG** e
**Exportar comandos** permitem escolher os arquivos. **Imprimir etiqueta** usa
a conexão configurada; com transporte arquivo, gera o trabalho no caminho informado.

As configurações podem ser salvas em `OpenAC.Net.Etq/demo.json` no diretório de
dados do usuário. A aplicação as recupera na próxima execução. Os controles da
interface ficam desabilitados durante geração e comunicação; ao fechar a janela,
a operação em andamento é aguardada e os recursos gráficos são liberados.

O preview aproxima as fontes da impressora. Consulte a [matriz de compatibilidade](../../docs/protocolos.md)
para limites e diferenças por protocolo.
