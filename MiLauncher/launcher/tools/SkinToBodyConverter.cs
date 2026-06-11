using System;
using System.Globalization;
using System.Windows.Data;
using MiLauncher.Models;

namespace MiLauncher.launcher.tools
{
    public class SkinToBodyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlayerSkin skin)
            {
                return SkinRenderer.GetFrontBody(skin.SkinPath, skin.CapePath, skin.IsSlimModel);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
