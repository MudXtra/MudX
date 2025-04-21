using MudBlazor;

namespace MudX.Docs
{
    public static class CustomThemes
    {
        /* 
    Created by Versile2
        */
        public static readonly MudTheme SilverTheme = new()
        {
            PaletteLight = new PaletteLight()
            {
                WarningLighten = "rgb(255,167,36)",
                ErrorDarken = "rgb(242,28,13)",
                ErrorLighten = "rgb(246,96,85)",
                DarkDarken = "rgb(46,46,46)",
                DarkLighten = "#ebebeb",
                HoverOpacity = 0.06,
                RippleOpacity = 0.1,
                RippleOpacitySecondary = 0.2,
                GrayDefault = "#9E9E9E",
                GrayLight = "#BDBDBD",
                GrayLighter = "#E0E0E0",
                GrayDark = "#757575",
                GrayDarker = "#616161",
                OverlayDark = "rgba(33,33,33,0.4980392156862745)",
                OverlayLight = "rgba(255,255,255,0.4980392156862745)",
                Black = "rgba(89,74,226,1)",
                White = "rgba(255,255,255,1)",
                Primary = "#1984c8ff",
                PrimaryContrastText = "#FFFFFF",
                Secondary = "#3b3b3dff",
                SecondaryContrastText = "#FFFFFF",
                Tertiary = "#2adfbbff",
                TertiaryContrastText = "#FFFFFF",
                Info = "#007bc3",
                InfoContrastText = "rgba(255,255,255,1)",
                Success = "#3ea44e",
                SuccessContrastText = "rgba(255,255,255,1)",
                Warning = "#ff9800",
                WarningContrastText = "rgba(255,255,255,1)",
                Error = "#d92800",
                ErrorContrastText = "rgba(255,255,255,1)",
                Dark = "#404040",
                DarkContrastText = "rgba(255,255,255,1)",
                TextPrimary = "#000000ff",
                TextSecondary = "#e0e0e0",
                TextDisabled = "#d0d0d0",
                ActionDefault = "rgba(0,0,0,0.5372549019607843)",
                ActionDisabled = "rgba(0,0,0,0.25882352941176473)",
                ActionDisabledBackground = "rgba(0,0,0,0.11764705882352941)",
                Background = "#ddddddff",
                BackgroundGray = "rgba(245,245,245,1)",
                Surface = "#fdf6f6ff",
                DrawerBackground = "#798eab",
                DrawerText = "#000000ff",
                DrawerIcon = "#000000ff",
                AppbarBackground = "#364258",
                AppbarText = "#ffffffff",
                LinesDefault = "rgba(0,0,0,0.11764705882352941)",
                LinesInputs = "rgba(189,189,189,1)",
                TableLines = "rgba(224,224,224,1)",
                TableStriped = "rgba(0,0,0,0.0196078431372549)",
                TableHover = "rgba(0,0,0,0.0392156862745098)",
                Divider = "#9a9696ff",
                DividerLight = "#dfdedeff",
                PrimaryDarken = "#014f7bff",
                PrimaryLighten = "#66c7feff",
                SecondaryDarken = "#060608ff",
                SecondaryLighten = "#7e7e7ee3",
                TertiaryDarken = "rgb(25,169,140)",
                TertiaryLighten = "#59ffdeda",
                InfoDarken = "rgb(12,128,223)",
                InfoLighten = "rgb(71,167,245)",
                SuccessDarken = "rgb(0,163,68)",
                SuccessLighten = "rgb(0,235,98)",
                WarningDarken = "rgb(214,129,0)",
            },
            PaletteDark = new PaletteDark()
            {
                WarningLighten = "rgb(255,182,36)",
                ErrorDarken = "rgb(244,47,70)",
                ErrorLighten = "rgb(248,119,134)",
                DarkDarken = "rgb(23,23,28)",
                DarkLighten = "#ebebeb",
                Black = "rgba(39,39,47,1)",
                Primary = "#1984c8",
                Secondary = "#3b3b3dff",
                Tertiary = "#2adfbbff",
                Info = "#007bc3",
                Success = "#3ea44e",
                Warning = "#ff9800",
                Error = "#d92800",
                Dark = "#404040",
                TextPrimary = "#ffffff",
                TextSecondary = "#e0e0e0",
                TextDisabled = "#d0d0d0",
                ActionDefault = "rgba(173,173,177,1)",
                ActionDisabled = "rgba(255,255,255,0.25882352941176473)",
                ActionDisabledBackground = "rgba(255,255,255,0.11764705882352941)",
                Background = "#000000",
                BackgroundGray = "rgba(39,39,47,1)",
                Surface = "rgba(55,55,64,1)",
                DrawerBackground = "#798eab",
                DrawerText = "#ffffffff",
                DrawerIcon = "#ffffffff",
                AppbarBackground = "#364258",
                AppbarText = "#000000ff",
                LinesDefault = "rgba(255,255,255,0.11764705882352941)",
                LinesInputs = "rgba(255,255,255,0.2980392156862745)",
                TableLines = "rgba(255,255,255,0.11764705882352941)",
                TableStriped = "rgba(255,255,255,0.2)",
                Divider = "#9a9696ff",
                DividerLight = "#dfdedeff",
                PrimaryDarken = "#015284ff",
                PrimaryLighten = "#59bdfaff",
                SecondaryDarken = "#010103ff",
                SecondaryLighten = "#8e8e8eff",
                TertiaryLighten = "#65fee0ff",
                InfoDarken = "rgb(10,133,255)",
                InfoLighten = "rgb(92,173,255)",
                SuccessDarken = "rgb(9,154,108)",
                SuccessLighten = "rgb(13,222,156)",
                WarningDarken = "rgb(214,143,0)",
            },
            LayoutProperties = new LayoutProperties()
            {
                DefaultBorderRadius = "4px",
                DrawerMiniWidthLeft = "56px",
                DrawerMiniWidthRight = "56px",
                DrawerWidthLeft = "240px",
                DrawerWidthRight = "240px",
                AppbarHeight = "64px",
            },
            Typography = new Typography()
            {
                Default = new DefaultTypography
                {
                    FontFamily = ["Public Sans", "Roboto", "Arial", "sans-serif"],
                    FontWeight = "400",
                    FontSize = ".875rem",
                    LineHeight = "1.43",
                    LetterSpacing = ".01071em",
                    TextTransform = "none",
                },
                H1 = new H1Typography
                {
                    FontWeight = "300",
                    FontSize = "6rem",
                    LineHeight = "1.167",
                    LetterSpacing = "-.01562em",
                    TextTransform = "none",
                },
                H2 = new H2Typography
                {
                    FontWeight = "300",
                    FontSize = "3.75rem",
                    LineHeight = "1.2",
                    LetterSpacing = "-.00833em",
                    TextTransform = "none",
                },
                H3 = new H3Typography
                {
                    FontWeight = "400",
                    FontSize = "3rem",
                    LineHeight = "1.167",
                    LetterSpacing = "0",
                    TextTransform = "none",
                },
                H4 = new H4Typography
                {
                    FontWeight = "400",
                    FontSize = "2.125rem",
                    LineHeight = "1.235",
                    LetterSpacing = ".00735em",
                    TextTransform = "none",
                },
                H5 = new H5Typography
                {
                    FontWeight = "400",
                    FontSize = "1.5rem",
                    LineHeight = "1.334",
                    LetterSpacing = "0",
                    TextTransform = "none",
                },
                H6 = new H6Typography
                {
                    FontWeight = "500",
                    FontSize = "1.25rem",
                    LineHeight = "1.6",
                    LetterSpacing = ".0075em",
                    TextTransform = "none",
                },
                Subtitle1 = new Subtitle1Typography
                {
                    FontWeight = "400",
                    FontSize = "1rem",
                    LineHeight = "1.75",
                    LetterSpacing = ".00938em",
                    TextTransform = "none",
                },
                Subtitle2 = new Subtitle2Typography
                {
                    FontWeight = "500",
                    FontSize = ".875rem",
                    LineHeight = "1.57",
                    LetterSpacing = ".00714em",
                    TextTransform = "none",
                },
                Body1 = new Body1Typography
                {
                    FontWeight = "400",
                    FontSize = "1rem",
                    LineHeight = "1.5",
                    LetterSpacing = ".00938em",
                    TextTransform = "none",
                },
                Body2 = new Body2Typography
                {
                    FontWeight = "400",
                    FontSize = ".875rem",
                    LineHeight = "1.43",
                    LetterSpacing = ".01071em",
                    TextTransform = "none",
                },
                Button = new ButtonTypography
                {
                    FontWeight = "500",
                    FontSize = ".875rem",
                    LineHeight = "1.75",
                    LetterSpacing = ".02857em",
                    TextTransform = "uppercase",
                },
                Caption = new CaptionTypography
                {
                    FontWeight = "400",
                    FontSize = ".75rem",
                    LineHeight = "1.66",
                    LetterSpacing = ".03333em",
                    TextTransform = "none",
                },
                Overline = new OverlineTypography
                {
                    FontWeight = "400",
                    FontSize = ".75rem",
                    LineHeight = "2.66",
                    LetterSpacing = ".08333em",
                    TextTransform = "none",
                },
            },
            ZIndex = new ZIndex()
            {
                Drawer = 1100,
                Popover = 1200,
                AppBar = 1100,
                Dialog = 1400,
                Snackbar = 1500,
                Tooltip = 1600,
            },
        };
    }
}
