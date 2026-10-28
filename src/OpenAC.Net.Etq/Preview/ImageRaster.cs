using System;
using System.Globalization;
using System.IO;
using System.Text;
using OpenAC.Net.Etq.Command;
using SkiaSharp;

namespace OpenAC.Net.Etq.Preview;

internal sealed class ImageRaster
{
    internal byte[] Data { get; private set; } = [];
    
    internal int BytesPerRow { get; private set; }
    
    internal int Height { get; private set; }

    internal string Hex
    {
        get
        {
            var builder = new StringBuilder(Data.Length * 2);
            foreach (var b in Data) builder.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }
    }

    internal static SKBitmap Decode(ImageCommand command)
    {
        using var stream = new MemoryStream(command.Imagem, false);
        using var codec = SKCodec.Create(stream) ?? throw new ArgumentException("Formato de imagem inválido.");
        CheckSize(codec.Info.Width, codec.Info.Height);
        using var original = SKBitmap.Decode(codec) ?? throw new ArgumentException("Imagem não pôde ser decodificada.");
        var width = command.LarguraDots;
        var height = command.AlturaDots;
        if (width == 0 && height == 0)
        {
            width = original.Width;
            height = original.Height;
        }
        else if (height == 0) height = Math.Max(1, (int)((long)original.Height * width / original.Width));
        else if (width == 0) width = Math.Max(1, (int)((long)original.Width * height / original.Height));

        CheckSize(width, height);
        var result = new SKBitmap(width, height);
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.White);
        using var image = SKImage.FromBitmap(original);
        canvas.DrawImage(image, new SKRect(0, 0, width, height));
        return result;
    }

    internal static ImageRaster Create(ImageCommand command)
    {
        using var bitmap = Decode(command);
        var raster = new ImageRaster { BytesPerRow = (bitmap.Width + 7) / 8, Height = bitmap.Height };
        raster.Data = new byte[raster.BytesPerRow * raster.Height];
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width; x++)
        {
            var color = bitmap.GetPixel(x, y);
            if ((color.Red * 299 + color.Green * 587 + color.Blue * 114) / 1000 < command.Limiar)
                raster.Data[y * raster.BytesPerRow + x / 8] |= (byte)(128 >> (x % 8));
        }

        return raster;
    }

    private static void CheckSize(int width, int height)
    {
        if (width < 1 || height < 1 || width > 32000 || height > 32000 || (long)width * height > 64000000)
            throw new ArgumentOutOfRangeException(nameof(width),
                "Imagem limitada a 32000 dots por lado e 64 megapixels.");
    }
}