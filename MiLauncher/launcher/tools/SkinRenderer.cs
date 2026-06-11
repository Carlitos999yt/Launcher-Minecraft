using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MiLauncher.launcher.tools
{
    public static class SkinRenderer
    {
        public static ImageSource GetFrontBody(string skinPath, string capePath, bool isSlim)
        {
            try
            {
                string finalPath = skinPath;
                if (string.IsNullOrEmpty(finalPath) || !System.IO.File.Exists(finalPath))
                {
                    finalPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Images", "default_skin.png");
                }
                if (!System.IO.File.Exists(finalPath)) return null;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(finalPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                bool isLegacy = bitmap.PixelHeight == 32;
                int armW = isSlim ? 3 : 4;

                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext dc = visual.RenderOpen())
                {
                    // Capa detrás del cuerpo
                    if (!string.IsNullOrEmpty(capePath) && capePath != "Sin capa")
                    {
                        string realUrl = GetCapeUrl(capePath);
                        if (!string.IsNullOrEmpty(realUrl))
                        {
                            try
                            {
                                BitmapImage capeBmp = new BitmapImage();
                                capeBmp.BeginInit();
                                capeBmp.UriSource = new Uri(realUrl, UriKind.Absolute);
                                capeBmp.CacheOption = BitmapCacheOption.OnLoad;
                                capeBmp.EndInit();
                                capeBmp.Freeze();
                                DrawPart(dc, capeBmp, 1, 1, 10, 16, 3, 8);
                            }
                            catch { }
                        }
                    }

                    // Cabeza (capa base + overlay)
                    DrawPart(dc, bitmap, 8, 8, 8, 8, 4, 0);
                    DrawPart(dc, bitmap, 40, 8, 8, 8, 4, 0);

                    // Torso (capa base + overlay)
                    DrawPart(dc, bitmap, 20, 20, 8, 12, 4, 8);
                    if (!isLegacy) DrawPart(dc, bitmap, 20, 36, 8, 12, 4, 8);

                    // Brazo Derecho
                    DrawPart(dc, bitmap, 44, 20, armW, 12, 4 - armW, 8);
                    if (!isLegacy) DrawPart(dc, bitmap, 44, 36, armW, 12, 4 - armW, 8);

                    // Brazo Izquierdo
                    if (isLegacy) DrawPart(dc, bitmap, 44, 20, armW, 12, 12, 8);
                    else
                    {
                        DrawPart(dc, bitmap, 36, 52, armW, 12, 12, 8);
                        DrawPart(dc, bitmap, 52, 52, armW, 12, 12, 8);
                    }

                    // Pierna Derecha
                    DrawPart(dc, bitmap, 4, 20, 4, 12, 4, 20);
                    if (!isLegacy) DrawPart(dc, bitmap, 4, 36, 4, 12, 4, 20);

                    // Pierna Izquierda
                    if (isLegacy) DrawPart(dc, bitmap, 4, 20, 4, 12, 8, 20);
                    else
                    {
                        DrawPart(dc, bitmap, 20, 52, 4, 12, 8, 20);
                        DrawPart(dc, bitmap, 4, 52, 4, 12, 8, 20);
                    }
                }

                RenderTargetBitmap rtb = new RenderTargetBitmap(16, 32, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(visual);
                rtb.Freeze();
                return rtb;
            }
            catch
            {
                return null;
            }
        }

        private static void DrawPart(DrawingContext dc, BitmapSource source, int sx, int sy, int sw, int sh, int dx, int dy)
        {
            if (sx + sw > source.PixelWidth || sy + sh > source.PixelHeight) return;
            CroppedBitmap crop = new CroppedBitmap(source, new Int32Rect(sx, sy, sw, sh));
            dc.DrawImage(crop, new Rect(dx, dy, sw, sh));
        }

        private static string GetCapeUrl(string capeName)
        {
            if (string.IsNullOrEmpty(capeName) || capeName == "Sin capa") return null;
            string capesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Capes");
            string path = System.IO.Path.Combine(capesDir, capeName + ".png");
            if (System.IO.File.Exists(path))
            {
                return path;
            }
            return null;
        }
    }
}
