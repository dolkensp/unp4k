using Microsoft.Extensions.Logging;

using UraniumUI;
using CommunityToolkit.Maui;

namespace unp4k;
internal static class MauiProgram
{
    internal static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseUraniumUI().UseUraniumUIMaterial().UseUraniumUIBlurs().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans_Condensed-Bold.ttf", "OpenSansCondensedBold");
            fonts.AddFont("OpenSans_Condensed-BoldItalic.ttf", "OpenSansCondensedBoldItalic");
            fonts.AddFont("OpenSans_Condensed-ExtraBold.ttf", "OpenSansCondensedExtraBold");
            fonts.AddFont("OpenSans_Condensed-ExtraBoldItalic.ttf", "OpenSansCondensedExtraBoldItalic");
            fonts.AddFont("OpenSans_Condensed-Italic.ttf", "OpenSansCondensedItalic");
            fonts.AddFont("OpenSans_Condensed-Light.ttf", "OpenSansCondensedLight");
            fonts.AddFont("OpenSans_Condensed-LightItalic.ttf", "OpenSansCondensedLightItalic");
            fonts.AddFont("OpenSans_Condensed-Medium.ttf", "OpenSansCondensedMedium");
            fonts.AddFont("OpenSans_Condensed-MediumItalic.ttf", "OpenSansCondensedMediumItalic");
            fonts.AddFont("OpenSans_Condensed-Regular.ttf", "OpenSansCondensed");
            fonts.AddFont("OpenSans_Condensed-SemiBoldItalic.ttf", "OpenSansCondensedSemiBoldItalic");
            fonts.AddFont("OpenSans_Condensed-SemiBold.ttf", "OpenSansCondensedSemiBoldItalic");
            fonts.AddFont("OpenSans_SemiCondensed-Bold.ttf", "OpenSansSemiCondensedBold");
            fonts.AddFont("OpenSans_SemiCondensed-BoldItalic.ttf", "OpenSansSemiCondensedBoldItalic");
            fonts.AddFont("OpenSans_SemiCondensed-ExtraBold.ttf", "OpenSansSemiCondensedExtraBold");
            fonts.AddFont("OpenSans_SemiCondensed-ExtraBoldItalic.ttf", "OpenSansSemiCondensedExtraBoldItalic");
            fonts.AddFont("OpenSans_SemiCondensed-Italic.ttf", "OpenSansSemiCondensedItalic");
            fonts.AddFont("OpenSans_SemiCondensed-Light.ttf", "OpenSansSemiCondensedLight");
            fonts.AddFont("OpenSans_SemiCondensed-LightItalic.ttf", "OpenSansSemiCondensedLightItalic");
            fonts.AddFont("OpenSans_SemiCondensed-Medium.ttf", "OpenSansSemiCondensedMedium");
            fonts.AddFont("OpenSans_SemiCondensed-MediumItalic.ttf", "OpenSansSemiCondensedMediumItalic");
            fonts.AddFont("OpenSans_SemiCondensed-Regular.ttf", "OpenSansSemiCondensed");
            fonts.AddFont("OpenSans_SemiCondensed-SemiBoldItalic.ttf", "OpenSansSemiCondensedSemiBoldItalic");
            fonts.AddFont("OpenSans_SemiCondensed-SemiBold.ttf", "OpenSansSemiCondensedSemiBoldItalic");
            fonts.AddFont("OpenSans-Bold.ttf", "OpenSansBold");
            fonts.AddFont("OpenSans-BoldItalic.ttf", "OpenSansBoldItalic");
            fonts.AddFont("OpenSans-ExtraBold.ttf", "OpenSansExtraBold");
            fonts.AddFont("OpenSans-ExtraBoldItalic.ttf", "OpenSansExtraBoldItalic");
            fonts.AddFont("OpenSans-Italic.ttf", "OpenSansItalic");
            fonts.AddFont("OpenSans-Light.ttf", "OpenSansLight");
            fonts.AddFont("OpenSans-LightItalic.ttf", "OpenSansLightItalic");
            fonts.AddFont("OpenSans-Medium.ttf", "OpenSansMedium");
            fonts.AddFont("OpenSans-MediumItalic.ttf", "OpenSansMediumItalic");
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSans");
            fonts.AddFont("OpenSans-SemiBoldItalic.ttf", "OpenSansSemiBoldItalic");
            fonts.AddFont("OpenSans-SemiBold.ttf", "OpenSansSemiBoldItalic");
            fonts.AddFont("fa-solid-900.ttf", "FontAwesome");
            fonts.AddFont("fa-brands-400.ttf", "FontAwesomeBrands");
        }).UseMauiCommunityToolkit();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}