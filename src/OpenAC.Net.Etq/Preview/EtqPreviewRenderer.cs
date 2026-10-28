using System;
using System.Collections.Generic;
using OpenAC.Net.Etq.Command;
using OpenAC.Net.Etq.Commom;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;

namespace OpenAC.Net.Etq.Preview;

internal static class EtqPreviewRenderer
{
    internal static byte[] Render(IReadOnlyList<IPrintCommand> commands, EtqConfig config, ProtocoloEtq protocolo)
    {
        using var bitmap = new SKBitmap(config.ToDots(config.Largura), config.ToDots(config.Altura));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);
        if (config.OrigemInferior)
        {
            canvas.Translate(bitmap.Width, bitmap.Height);
            canvas.RotateDegrees(180);
        }

        foreach (var command in commands)
        {
            if (command is RawCommand) throw new NotSupportedException("Preview não interpreta comandos brutos.");
            if (command is not PositionedCommand position) continue;
            
            canvas.Save();
            canvas.Translate(config.ToDots(position.Horizontal + config.MargemEsquerda),
                config.ToDots(position.Vertical));
            canvas.RotateDegrees((int)position.Orientacao);
            var foreground = protocolo == ProtocoloEtq.EscLabel ? Color(config.CorFrente) : SKColors.Black;
            
            using var paint = new SKPaint();
            paint.Color = foreground;
            paint.IsAntialias = false;
            switch (command)
            {
                case TextCommand text:
                    using (var face = SKTypeface.FromFamilyName("DejaVu Sans Mono"))
                    using (var font = new SKFont(face, 24 * text.MultiplicadorVertical))
                    {
                        font.ScaleX = (float)text.MultiplicadorHorizontal / text.MultiplicadorVertical;
                        if (text.Reverso)
                        {
                            var width = font.MeasureText(text.Texto);
                            canvas.DrawRect(0, 0, width, font.Size, paint);
                            paint.Color = SKColors.White;
                        }

                        canvas.DrawText(text.Texto, 0, -font.Metrics.Ascent, font, paint);
                    }

                    break;
                case LineCommand line:
                    canvas.DrawRect(0, 0, config.ToDots(line.Largura), config.ToDots(line.Altura), paint);
                    break;
                case BoxCommand box:
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = Math.Max(1, config.ToDots(box.Espessura));
                    var inset = paint.StrokeWidth / 2;
                    canvas.DrawRoundRect(
                        new SKRect(inset, inset, config.ToDots(box.Largura) - inset, config.ToDots(box.Altura) - inset),
                        box.Canto, box.Canto, paint);
                    break;
                case ImageCommand image:
                    using (var decoded = ImageRaster.Decode(image))
                    {
                        if (protocolo == ProtocoloEtq.EscLabel)
                        {
                            using var skImage = SKImage.FromBitmap(decoded);
                            canvas.DrawImage(skImage, 0, 0);
                        }
                        else
                        {
                            var raster = ImageRaster.Create(image);
                            for (var y = 0; y < raster.Height; y++)
                            for (var x = 0; x < decoded.Width; x++)
                                if ((raster.Data[y * raster.BytesPerRow + x / 8] & (128 >> (x % 8))) != 0)
                                    canvas.DrawRect(x, y, 1, 1, paint);
                        }
                    }

                    break;
                case BarcodeCommand barcode:
                    var matrix = BarcodeMatrix(barcode, config.ToDots(barcode.Altura));
                    DrawMatrix(canvas, paint, matrix, barcode.BarraFina, 1);
                    if (barcode.ExibirCodigo)
                    {
                        using var face = SKTypeface.FromFamilyName("DejaVu Sans Mono");
                        using var font = new SKFont(face, 16);
                        canvas.DrawText(barcode.Texto, 0, matrix.Height + 18, font, paint);
                    }

                    break;
                case QrCodeCommand qr:
                    if (qr.Modelo != 2)
                        throw new NotSupportedException("Preview de QR Code suporta somente o modelo 2.");
                    var qrMatrix = new MultiFormatWriter().encode(qr.Texto, BarcodeFormat.QR_CODE, 0, 0,
                        new Dictionary<EncodeHintType, object>
                        {
                            [EncodeHintType.MARGIN] = 4, [EncodeHintType.CHARACTER_SET] = "UTF-8",
                            [EncodeHintType.ERROR_CORRECTION] = ErrorCorrectionLevel.forBits(qr.ErrorLevel switch
                            {
                                QrCodeErrorLevel.L => 1, QrCodeErrorLevel.M => 0, QrCodeErrorLevel.Q => 3, _ => 2
                            })
                        });
                    DrawMatrix(canvas, paint, qrMatrix, qr.LarguraModulo, qr.LarguraModulo);
                    break;
            }

            canvas.Restore();
        }

        using var output = SKImage.FromBitmap(bitmap);
        using var data = output.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    internal static BitMatrix BarcodeMatrix(BarcodeCommand command, int alturaDots = 80)
    {
        var format = command.Tipo switch
        {
            TipoCodigoBarras.Code128 => BarcodeFormat.CODE_128, TipoCodigoBarras.Code39 => BarcodeFormat.CODE_39,
            TipoCodigoBarras.UPCA => BarcodeFormat.UPC_A, TipoCodigoBarras.EAN13 => BarcodeFormat.EAN_13,
            TipoCodigoBarras.EAN8 => BarcodeFormat.EAN_8, TipoCodigoBarras.Interleaved2of5 => BarcodeFormat.ITF,
            TipoCodigoBarras.Code93 => BarcodeFormat.CODE_93, TipoCodigoBarras.Codabar => BarcodeFormat.CODABAR,
            TipoCodigoBarras.MSI => BarcodeFormat.MSI,
            _ => throw new NotSupportedException("Preview não suporta este tipo de código de barras.")
        };
        return new MultiFormatWriter().encode(command.Texto, format, 0, alturaDots,
            new Dictionary<EncodeHintType, object> { [EncodeHintType.MARGIN] = 10 });
    }

    private static void DrawMatrix(SKCanvas canvas, SKPaint paint, BitMatrix matrix, int scaleX, int scaleY)
    {
        for (var y = 0; y < matrix.Height; y++)
        for (var x = 0; x < matrix.Width; x++)
            if (matrix[x, y])
                canvas.DrawRect(x * scaleX, y * scaleY, scaleX, scaleY, paint);
    }

    private static SKColor Color(EtqCor c) => new(c.Vermelho, c.Verde, c.Azul, c.Opacidade);
}