using System.Collections.Generic;
using Cirros.Utility;

namespace Cirros.Core
{
    public enum ColorCode
    {
        ByLayer = 0x00000000,
        ThemeForeground = 0x00000001,
        SameAsOutline = 0x00000002,
        NoFill = 0x00000003,
        SetColor = 0x00000004,
    };

    public static class StandardColors
    {
        private static Dictionary<uint, string> _standardColors = new Dictionary<uint, string>();
        private static Dictionary<string, uint> _standardColorNames = new Dictionary<string, uint>();

        public static Dictionary<uint, string> Colors
        {
            get
            {
                if (_standardColors.Count == 0)
                {
                    init_standardColors();
                }
                return _standardColors;
            }
        }

        public static Dictionary<string, uint> ColorNames
        {
            get
            {
                if (_standardColorNames.Count == 0)
                {
                    Dictionary<uint, string> colors = StandardColors.Colors;
                    foreach (uint key in colors.Keys)
                    {
                        _standardColorNames.Add(colors[key].ToLower(), key);
                    }

                    //_standardColorNames.Add("aqua", _standardColorNames["cyan"]);
                    //_standardColorNames.Add("fuchsia", _standardColorNames["dark magenta"]);
                }
                return _standardColorNames;
            }
        }

        private static void init_standardColors()
        {
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 0, 0), "Black");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 255, 255), "White");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 105, 105, 105), "Dim Gray");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 128, 128, 128), "Gray");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 169, 169, 169), "Dark Gray");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 192, 192, 192), "Silver");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 211, 211, 211), "Light Gray");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 220, 220, 220), "Gainsboro");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 245, 245, 245), "White Smoke");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 128, 0, 0), "Maroon");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 139, 0, 0), "Dark Red");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 0, 0), "Red");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 165, 42, 42), "Brown");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 178, 34, 34), "Firebrick");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 205, 92, 92), "Indian Red");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 250, 250), "Snow");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 240, 128, 128), "Light Coral");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 188, 143, 143), "Rosy Brown");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 228, 225), "Misty Rose");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 250, 128, 114), "Salmon");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 99, 71), "Tomato");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 233, 150, 122), "Dark Salmon");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 127, 80), "Coral");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 69, 0), "Orange Red");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 160, 122), "Light Salmon");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 160, 82, 45), "Sienna");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 245, 238), "Sea Shell");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 210, 105, 30), "Chocolate");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 139, 69, 19), "Saddle Brown");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 244, 164, 96), "Sandy Brown");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 218, 185), "Peach Puff");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 205, 133, 63), "Peru");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 250, 240, 230), "Linen");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 228, 196), "Bisque");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 140, 0), "Dark Orange");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 222, 184, 135), "Burly Wood");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 210, 180, 140), "Tan");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 250, 235, 215), "Antique White");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 222, 173), "Navajo White");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 235, 205), "Blanched Almond");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 239, 213), "Papaya Whip");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 228, 181), "Moccasin");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 165, 0), "Orange");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 245, 222, 179), "Wheat");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 253, 245, 230), "Old Lace");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 250, 240), "Floral White");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 184, 134, 11), "Dark Goldenrod");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 218, 165, 32), "Goldenrod");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 248, 220), "Cornsilk");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 215, 0), "Gold");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 240, 230, 140), "Khaki");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 250, 205), "Lemon Chiffon");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 238, 232, 170), "Pale Goldenrod");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 189, 183, 107), "Dark Khaki");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 245, 245, 220), "Beige");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 250, 250, 210), "Light Goldenrod Yellow");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 128, 128, 0), "Olive");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 255, 0), "Yellow");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 255, 224), "Light Yellow");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 255, 240), "Ivory");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 107, 142, 35), "Olive Drab");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 154, 205, 50), "Yellow Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 85, 107, 47), "Dark Olive Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 173, 255, 47), "Green Yellow");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 127, 255, 0), "Chartreuse");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 124, 252, 0), "Lawn Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 143, 188, 139), "Dark Sea Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 144, 238, 144), "Light Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 34, 139, 34), "Forest Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 50, 205, 50), "Lime Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 152, 251, 152), "Pale Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 100, 0), "Dark Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 128, 0), "Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 255, 0), "Lime");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 240, 255, 240), "Honeydew");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 46, 139, 87), "Sea Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 60, 179, 113), "Medium Sea Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 255, 127), "Spring Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 245, 255, 250), "Mint Cream");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 250, 154), "Medium Spring Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 102, 205, 170), "Medium Aquamarine");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 127, 255, 212), "Aquamarine");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 64, 224, 208), "Turquoise");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 32, 178, 170), "Light Sea Green");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 72, 209, 204), "Medium Turquoise");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 47, 79, 79), "Dark Slate Gray");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 175, 238, 238), "Pale Turquoise");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 128, 128), "Teal");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 139, 139), "Dark Cyan");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 255, 255), "Cyan");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 1, 255, 255), "Aqua");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 224, 255, 255), "Light Cyan");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 240, 255, 255), "Azure");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 206, 209), "Dark Turquoise");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 95, 158, 160), "Cadet Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 176, 224, 230), "Powder Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 173, 216, 230), "Light Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 191, 255), "Deep Sky Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 135, 206, 235), "Sky Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 135, 206, 250), "Light Sky Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 70, 130, 180), "Steel Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 240, 248, 255), "Alice Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 30, 144, 255), "Dodger Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 112, 128, 144), "Slate Gray");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 119, 136, 153), "Light Slate Gray");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 176, 196, 222), "Light Steel Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 100, 149, 237), "Cornflower Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 65, 105, 225), "Royal Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 25, 25, 112), "Midnight Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 230, 230, 250), "Lavender");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 0, 128), "Navy");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 0, 139), "Dark Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 0, 205), "Medium Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 0, 0, 255), "Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 248, 248, 255), "Ghost White");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 106, 90, 205), "Slate Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 72, 61, 139), "Dark Slate Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 123, 104, 238), "Medium Slate Blue");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 147, 112, 219), "Medium Purple");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 138, 43, 226), "Blue Violet");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 75, 0, 130), "Indigo");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 153, 50, 204), "Dark Orchid");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 148, 0, 211), "Dark Violet");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 186, 85, 211), "Medium Orchid");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 216, 191, 216), "Thistle");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 221, 160, 221), "Plum");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 238, 130, 238), "Violet");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 128, 0, 128), "Purple");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 139, 0, 139), "Dark Magenta");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 1, 255), "Fuchsia");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 0, 255), "Magenta");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 218, 112, 214), "Orchid");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 199, 21, 133), "Medium Violet Red");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 20, 147), "Deep Pink");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 105, 180), "Hot Pink");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 240, 245), "Lavender Blush");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 219, 112, 147), "Pale Violet Red");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 220, 20, 60), "Crimson");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 192, 203), "Pink");
            _standardColors.Add(Utilities.ColorSpecFromARGB(255, 255, 182, 193), "Light Pink");
            _standardColors.Add(Utilities.ColorSpecFromARGB(0, 255, 255, 255), "Transparent");
        }

        public static List<uint> AcadColorSpecTable = new List<uint>() {
            0x00000001,    // Theme Default (AutoCAD default color or BYBLOCK)
            0xFFFF0000,
            0xFFFFFF00,
            0xFF00FF00,
            0xFF00FFFF,
            0xFF0000FF,
            0xFFFF00FF,
            0x00000001,     // Also theme default?
            0xFF414141,
            0xFF808080,
            0xFFFF0000,
            0xFFFFAAAA,
            0xFFBD0000,
            0xFFBD7E7E,
            0xFF810000,
            0xFF815656,
            0xFF680000,
            0xFF684545,
            0xFF4F0000,
            0xFF4F3535,
            0xFFFF3F00,
            0xFFFFBFAA,
            0xFFBD2E00,
            0xFFBD8D7E,
            0xFF811F00,
            0xFF816056,
            0xFF681900,
            0xFF684E45,
            0xFF4F1300,
            0xFF4F3B35,
            0xFFFF7F00,
            0xFFFFD4AA,
            0xFFBD5E00,
            0xFFBD9D7E,
            0xFF814000,
            0xFF816B56,
            0xFF683400,
            0xFF685645,
            0xFF4F2700,
            0xFF4F4235,
            0xFFFFBF00,
            0xFFFFEAAA,
            0xFFBD8D00,
            0xFFBDAD7E,
            0xFF816000,
            0xFF817656,
            0xFF684E00,
            0xFF685F45,
            0xFF4F3B00,
            0xFF4F4935,
            0xFFFFFF00,
            0xFFFFFFAA,
            0xFFBDBD00,
            0xFFBDBD7E,
            0xFF818100,
            0xFF818156,
            0xFF686800,
            0xFF686845,
            0xFF4F4F00,
            0xFF4F4F35,
            0xFFBFFF00,
            0xFFEAFFAA,
            0xFF8DBD00,
            0xFFADBD7E,
            0xFF608100,
            0xFF768156,
            0xFF4E6800,
            0xFF5F6845,
            0xFF3B4F00,
            0xFF494F35,
            0xFF7FFF00,
            0xFFD4FFAA,
            0xFF5EBD00,
            0xFF9DBD7E,
            0xFF408100,
            0xFF6B8156,
            0xFF346800,
            0xFF566845,
            0xFF274F00,
            0xFF424F35,
            0xFF3FFF00,
            0xFFBFFFAA,
            0xFF2EBD00,
            0xFF8DBD7E,
            0xFF1F8100,
            0xFF608156,
            0xFF196800,
            0xFF4E6845,
            0xFF134F00,
            0xFF3B4F35,
            0xFF00FF00,
            0xFFAAFFAA,
            0xFF00BD00,
            0xFF7EBD7E,
            0xFF008100,
            0xFF568156,
            0xFF006800,
            0xFF456845,
            0xFF004F00,
            0xFF354F35,
            0xFF00FF3F,
            0xFFAAFFBF,
            0xFF00BD2E,
            0xFF7EBD8D,
            0xFF00811F,
            0xFF568160,
            0xFF006819,
            0xFF45684E,
            0xFF004F13,
            0xFF354F3B,
            0xFF00FF7F,
            0xFFAAFFD4,
            0xFF00BD5E,
            0xFF7EBD9D,
            0xFF008140,
            0xFF56816B,
            0xFF006834,
            0xFF456856,
            0xFF004F27,
            0xFF354F42,
            0xFF00FFBF,
            0xFFAAFFEA,
            0xFF00BD8D,
            0xFF7EBDAD,
            0xFF008160,
            0xFF568176,
            0xFF00684E,
            0xFF45685F,
            0xFF004F3B,
            0xFF354F49,
            0xFF00FFFF,
            0xFFAAFFFF,
            0xFF00BDBD,
            0xFF7EBDBD,
            0xFF008181,
            0xFF568181,
            0xFF006868,
            0xFF456868,
            0xFF004F4F,
            0xFF354F4F,
            0xFF00BFFF,
            0xFFAAEAFF,
            0xFF008DBD,
            0xFF7EADBD,
            0xFF006081,
            0xFF567681,
            0xFF004E68,
            0xFF455F68,
            0xFF003B4F,
            0xFF35494F,
            0xFF007FFF,
            0xFFAAD4FF,
            0xFF005EBD,
            0xFF7E9DBD,
            0xFF004081,
            0xFF566B81,
            0xFF003468,
            0xFF455668,
            0xFF00274F,
            0xFF35424F,
            0xFF003FFF,
            0xFFAABFFF,
            0xFF002EBD,
            0xFF7E8DBD,
            0xFF001F81,
            0xFF566081,
            0xFF001968,
            0xFF454E68,
            0xFF00134F,
            0xFF353B4F,
            0xFF0000FF,
            0xFFAAAAFF,
            0xFF0000BD,
            0xFF7E7EBD,
            0xFF000081,
            0xFF565681,
            0xFF000068,
            0xFF454568,
            0xFF00004F,
            0xFF35354F,
            0xFF3F00FF,
            0xFFBFAAFF,
            0xFF2E00BD,
            0xFF8D7EBD,
            0xFF1F0081,
            0xFF605681,
            0xFF190068,
            0xFF4E4568,
            0xFF13004F,
            0xFF3B354F,
            0xFF7F00FF,
            0xFFD4AAFF,
            0xFF5E00BD,
            0xFF9D7EBD,
            0xFF400081,
            0xFF6B5681,
            0xFF340068,
            0xFF564568,
            0xFF27004F,
            0xFF42354F,
            0xFFBF00FF,
            0xFFEAAAFF,
            0xFF8D00BD,
            0xFFAD7EBD,
            0xFF600081,
            0xFF765681,
            0xFF4E0068,
            0xFF5F4568,
            0xFF3B004F,
            0xFF49354F,
            0xFFFF00FF,
            0xFFFFAAFF,
            0xFFBD00BD,
            0xFFBD7EBD,
            0xFF810081,
            0xFF815681,
            0xFF680068,
            0xFF684568,
            0xFF4F004F,
            0xFF4F354F,
            0xFFFF00BF,
            0xFFFFAAEA,
            0xFFBD008D,
            0xFFBD7EAD,
            0xFF810060,
            0xFF815676,
            0xFF68004E,
            0xFF68455F,
            0xFF4F003B,
            0xFF4F3549,
            0xFFFF007F,
            0xFFFFAAD4,
            0xFFBD005E,
            0xFFBD7E9D,
            0xFF810040,
            0xFF81566B,
            0xFF680034,
            0xFF684556,
            0xFF4F0027,
            0xFF4F3542,
            0xFFFF003F,
            0xFFFFAABF,
            0xFFBD002E,
            0xFFBD7E8D,
            0xFF81001F,
            0xFF815660,
            0xFF680019,
            0xFF68454E,
            0xFF4F0013,
            0xFF4F353B,
            0xFF333333,
            0xFF505050,
            0xFF696969,
            0xFF828282,
            0xFFBEBEBE,
            0xFFFFFFFF,
        };
    }
}
