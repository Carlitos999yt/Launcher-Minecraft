using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MiLauncher.launcher.tools
{
    public static class SkinProcessor
    {
        public static ImageSource GetFaceFromSkin(string skinPath)
        {
            try
            {
                if (string.IsNullOrEmpty(skinPath) || !File.Exists(skinPath)) return null;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(skinPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // Cara base de Steve (sin la capa del sombrero): X=8, Y=8, Ancho=8, Alto=8
                Int32Rect faceRect = new Int32Rect(8, 8, 8, 8);
                CroppedBitmap croppedFace = new CroppedBitmap(bitmap, faceRect);
                croppedFace.Freeze();

                return croppedFace;
            }
            catch
            {
                return null;
            }
        }

        public static byte[] CropTransparentMarginsAndSave(byte[] pngBytes)
        {
            try
            {
                using (var ms = new MemoryStream(pngBytes))
                {
                    var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    var frame = decoder.Frames[0];
                    
                    // Convertir a Bgra32 si no está en ese formato para asegurar la lectura del canal Alpha
                    FormatConvertedBitmap formatted = new FormatConvertedBitmap();
                    formatted.BeginInit();
                    formatted.Source = frame;
                    formatted.DestinationFormat = PixelFormats.Bgra32;
                    formatted.EndInit();
                    formatted.Freeze();

                    int width = formatted.PixelWidth;
                    int height = formatted.PixelHeight;
                    int stride = width * 4;
                    byte[] pixels = new byte[height * stride];
                    formatted.CopyPixels(pixels, stride, 0);

                    int minX = width;
                    int maxX = 0;
                    int minY = height;
                    int maxY = 0;
                    bool foundPixel = false;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = y * stride + x * 4;
                            byte alpha = pixels[index + 3]; // Canal Alpha es el índice 3 en BGRA32
                            if (alpha > 5) // Umbral mínimo de opacidad
                            {
                                if (x < minX) minX = x;
                                if (x > maxX) maxX = x;
                                if (y < minY) minY = y;
                                if (y > maxY) maxY = y;
                                foundPixel = true;
                            }
                        }
                    }

                    if (foundPixel)
                    {
                        // Añadir un margen interno (padding) de 10 píxeles para que no quede pegado al borde
                        int padding = 10;
                        minX = Math.Max(0, minX - padding);
                        minY = Math.Max(0, minY - padding);
                        maxX = Math.Min(width - 1, maxX + padding);
                        maxY = Math.Min(height - 1, maxY + padding);

                        int cropWidth = maxX - minX + 1;
                        int cropHeight = maxY - minY + 1;

                        if (cropWidth > 0 && cropHeight > 0)
                        {
                            var cropped = new CroppedBitmap(formatted, new Int32Rect(minX, minY, cropWidth, cropHeight));
                            cropped.Freeze();

                            var encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(cropped));
                            using (var outMs = new MemoryStream())
                            {
                                encoder.Save(outMs);
                                return outMs.ToArray();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error cropping transparent margins: " + ex.Message);
            }
            return pngBytes; // Si falla por cualquier motivo, devolvemos los bytes originales
        }
    }
}
