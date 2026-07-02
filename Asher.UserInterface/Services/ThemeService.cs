using MaterialDesignThemes.Wpf;

namespace Asher.UserInterface.Services
{
    public class ThemeService : IThemeService
    {
        public void Apply(string themeName)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(string.Equals(themeName, "Dark", StringComparison.OrdinalIgnoreCase)
                ? BaseTheme.Dark
                : BaseTheme.Light);
            paletteHelper.SetTheme(theme);
        }
    }
}
