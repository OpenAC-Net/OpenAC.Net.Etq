# Protocolos e compatibilidade

A implementação foi organizada a partir do padrão de comandos/interpretadores do
OpenAC.Net.EscPos. As unidades ACBrETQClass, ACBrETQPpla, ACBrETQEpl2,
ACBrETQZplII, ACBrETQEscLabel e ACBrETQTspl do ACBrSerial foram consultadas como
referência de linguagem. PPLB utiliza o interpretador EPL2, como em ACBrETQ.pas.

## Recursos

| Recurso | PPLA | EPL2/PPLB | ZPL II | EscLabel | TSPL |
| --- | --- | --- | --- | --- | --- |
| Texto, barras, QR, linha e caixa | Sim | Sim | Sim | Sim | Sim |
| Raster de imagem | Upload HEX | GW binário | ^GF hexadecimal | PNG colorido | BITMAP binário |
| Cópias máximas | 9999 | 65535 | 999 | 99999999 | 65535 |
| Densidade | 0–20 | 0–15 | 0–30 | Não se aplica | 0–15 |
| Velocidade | 1–4 | 0–6 | 2–12 | 1–14 | 1–6, 8, 10, 12 |
| Dimensões enviadas | No equipamento | q/Q | ^PW/^LL | ^S(CLS | SIZE/GAP/BLINE |
| Cores RGBA | Não | Não | Não | Sim | Não |

`Velocidade = -1` preserva o padrão da impressora. PPLA usa as dimensões do
objeto para o preview; tamanho físico, sensor e orientação de origem precisam
ser configurados no equipamento. Para PPLA, backfeed/origem não padrão e
sensor diferente de Gap produzem erro explícito. EPL2 aceita Gap/contínuo;
marca preta depende da configuração do modelo e gera erro nesta API.

Os enums indicam os formatos implementados; a disponibilidade real de QR Code,
guilhotina, velocidades e outros recursos depende do modelo e firmware.

## Páginas de código

| Protocolo | Páginas implementadas |
| --- | --- |
| PPLA | CP437, CP850 |
| EPL2/PPLB | CP437, CP850, CP852, CP860, CP1250, CP1252 |
| ZPL II | CP437, CP850, CP1252, UTF-8 |
| EscLabel | CP437, CP850, CP1250, CP1252, UTF-8 |
| TSPL | CP437, CP850, CP852, CP860, CP1250, CP1252, UTF-8 |

A página de código solicitada deve corresponder à capacidade da impressora.
Combinações não implementadas produzem `NotSupportedException`. Caracteres
não representáveis na codificação escolhida produzem erro de codificação,
em vez de serem substituídos silenciosamente.

## Coordenadas e comandos

- Todos os métodos públicos recebem horizontal (X) antes de vertical (Y).
- Posições, dimensões e alturas seguem `Configuracoes.Unidade`.
- Barras fina/larga, módulo de QR e dimensões explícitas de imagem usam dots.
- A conversão física usa 25,4 mm por polegada e o DPI selecionado. O arredondamento
  é consistente e independente da cultura.
- PPLA converte coordenadas para décimos de milímetro; os demais usam dots.
- PPLA possui campos fixos de 4 dígitos para coordenadas e 3 para dimensões
  gráficas e altura das barras. Valores fora desses limites geram exceção.
- QR Code PPLA e EPL2 usam orientação normal. Texto TSPL reverso com rotação
  não é implementado. Rotação de imagens deve ser aplicada nos bytes de entrada.
- A origem inferior inverte a etiqueta inteira no preview quando suportada.

Cada trabalho é gerado por completo antes da escrita. Imagens armazenadas de
PPLA/EscLabel são enviadas antes da abertura do formato e têm nomes distintos.
Raster binário não é convertido em texto nem passa por substituição de quebras
de linha. Bits excedentes na última coluna são brancos.

## Diferenças e decisões

- Não são copiados os erros de conversão de décimos de milímetro para milímetros
  encontrados na referência Pascal.
- TSPL emite o nível L/M/Q/H solicitado no QR Code e Code93 usa o identificador
  `93`; os trechos de referência usam valores divergentes nesses pontos.
- ZPL escapa os bytes reservados e não ASCII com `^FH`. EPL/TSPL protegem aspas;
  texto com controles de linha é rejeitado para não quebrar o formato do campo.
- O buffer guarda cópias dos comandos e dos arrays de imagem/dados. Alterações
  posteriores nos objetos fornecidos não alteram o trabalho já montado.
- `GerarComandos`, `GerarPreview` e `GerarStreamBase64` preservam o buffer por
  padrão. `Imprimir` limpa somente após escrita bem-sucedida.
- Não há repetição automática do trabalho na camada Etq. A política de
  transporte pertence ao OpenAC.Net.Devices; após falha, pode existir envio
  parcial e o chamador decide sobre uma nova tentativa.

## Preview

O preview PNG representa uma etiqueta, mesmo quando o trabalho solicita várias
cópias. Dimensões, DPI, coordenadas, orientação, cores e imagens são aplicados
localmente. Fontes nativas e parâmetros específicos do firmware são aproximados.
ZXing.Net gera os códigos de barras e QR Codes; não são desenhados símbolos
fictícios. Code11 e Standard 2 of 5 têm impressão ZPL mas não preview nesta versão.
O renderizador de QR Code suporta somente o modelo 2; modelo 1 gera erro no preview.
Comandos brutos também não têm preview e geram `NotSupportedException`.

O preview não é um emulador de firmware nem uma leitura dos comandos gerados:
ele renderiza os comandos tipados do buffer. Fontes, quiet zones e posição do
texto HRI podem variar no papel. A homologação física continua necessária.

## Escopo

Incluídos: montagem de uma etiqueta, cópias, texto, barras, QR, linhas, caixas,
imagens, geração de trabalho/PNG/Base64 e comunicação pelos dispositivos OpenAC.
Não incluídos: leitura de status, RFID, armazenamento persistente de formatos,
comandos de calibração e APIs específicas de um modelo. Para uma necessidade
específica, os bytes podem ser enviados com `ImprimirComando`.

Os testes usam vetores de comandos e transportes simulados/arquivo. Nenhuma
impressão física foi realizada automaticamente.
