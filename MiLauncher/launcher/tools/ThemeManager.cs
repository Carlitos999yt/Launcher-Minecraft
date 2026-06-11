using System.Windows;
using System.Windows.Media;
using MiLauncher.launcher.settings;
using Color = System.Windows.Media.Color;

namespace MiLauncher.launcher.tools
{
    public static class ThemeManager
    {
        public static void ApplyTheme()
        {
            var config = SettingsManager.LoadSettings();
            ApplyColors(config.PrimaryColor, config.SecondaryColor, config.BorderColor, config.TextColor);
        }

        public static void ApplyColors(string primaryHex, string secondaryHex, string borderHex, string textHex)
        {
            if (string.IsNullOrEmpty(borderHex)) borderHex = "#33FFFFFF";
            if (string.IsNullOrEmpty(textHex)) textHex = "#FFFFFF";

            var primary = (Color)ColorConverter.ConvertFromString(primaryHex);
            var secondary = (Color)ColorConverter.ConvertFromString(secondaryHex);
            var border = (Color)ColorConverter.ConvertFromString(borderHex);
            var text = (Color)ColorConverter.ConvertFromString(textHex);

            var res = Application.Current.Resources;

            // Colores base
            UpdateOrCreateBrush(res, "PrimaryBrush", primary);
            UpdateOrCreateBrush(res, "SecondaryBrush", secondary);

            // Derivados del primario (fondo principal, ventana)
            UpdateOrCreateBrush(res, "WindowBrush", primary);

            // Derivados del secundario (paneles, sidebar, headers)
            UpdateOrCreateBrush(res, "PanelBrush", secondary);

            // Input fields: un poco más claro que el secundario
            var inputColor = BlendColor(secondary, Colors.White, 0.05);
            UpdateOrCreateBrush(res, "InputBrush", inputColor);

            // Bordes (personalizado)
            UpdateOrCreateBrush(res, "BorderBrush", border);

            // Hover (un poco más claro que el primario)
            var hoverColor = BlendColor(primary, Colors.White, 0.08);
            UpdateOrCreateBrush(res, "HoverBrush", hoverColor);

            // Texto (personalizado)
            UpdateOrCreateBrush(res, "TextBrush", text);
        }

        private static void UpdateOrCreateBrush(ResourceDictionary res, string key, Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            res[key] = brush;
        }

        private static Color BlendColor(Color baseColor, Color blendColor, double amount)
        {
            byte r = (byte)(baseColor.R + (blendColor.R - baseColor.R) * amount);
            byte g = (byte)(baseColor.G + (blendColor.G - baseColor.G) * amount);
            byte b = (byte)(baseColor.B + (blendColor.B - baseColor.B) * amount);
            return Color.FromRgb(r, g, b);
        }
    }
}
