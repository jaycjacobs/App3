using RedDog;
using System.Collections.Generic;

namespace KT22.Console
{
    class KTCommandLanguage
    {
        static List<string> line_construct_options = new List<string> { RedDogGlobals.GS_SingleSegment, RedDogGlobals.GS_MultiSegment, RedDogGlobals.GS_Fillet, RedDogGlobals.GS_Freehand };
        static List<string> arc_construct_options = new List<string> { RedDogGlobals.GS_CenterStartEnd, RedDogGlobals.GS_Radius, RedDogGlobals.GS_RadiusAngles, RedDogGlobals.GS_SemiCircle, RedDogGlobals.GS_3Point, RedDogGlobals.GS_Fillet, RedDogGlobals.GS_FilletRadius };
        static List<string> circle_construct_options = new List<string> { RedDogGlobals.GS_Center, RedDogGlobals.GS_3Point };
        static List<string> ellipse_construct_options = new List<string> { RedDogGlobals.GS_Center, RedDogGlobals.GS_Box, RedDogGlobals.GS_Axis };
        static List<string> rectangle_construct_options = new List<string> { RedDogGlobals.GS_Size, RedDogGlobals.GS_Corners };
        static List<string> text_construct_options = new List<string> { RedDogGlobals.GS_1Point, RedDogGlobals.GS_2Point };
        static List<string> fill_options = new List<string> { RedDogGlobals.GS_None, RedDogGlobals.GS_Layer, RedDogGlobals.GS_Outline, "<color_type>" };
        static List<string> doubleline_close_options = new List<string> { RedDogGlobals.GS_None, RedDogGlobals.GS_Start, RedDogGlobals.GS_End, RedDogGlobals.GS_Both };
        static List<string> arrow_ends_options = new List<string> { RedDogGlobals.GS_Start, RedDogGlobals.GS_End, RedDogGlobals.GS_Both };
        static List<string> text_alignment_options = new List<string> { RedDogGlobals.GS_Left, RedDogGlobals.GS_Center, RedDogGlobals.GS_Right };
        static List<string> text_position_options = new List<string> { RedDogGlobals.GS_Above, RedDogGlobals.GS_On, RedDogGlobals.GS_Below };
        static List<string> dimension_type_options = new List<string> { RedDogGlobals.GS_Baseline, RedDogGlobals.GS_Incremental, RedDogGlobals.GS_PointToPoint, RedDogGlobals.GS_Angular, RedDogGlobals.GS_AngularBaseline };
        static List<string> polygon_type_options = new List<string> { RedDogGlobals.GS_Regular, RedDogGlobals.GS_Irregular };
        static List<string> triangle_type_options = new List<string> { RedDogGlobals.GS_30, RedDogGlobals.GS_45 };
        static List<string> triangle_size_options = new List<string> { RedDogGlobals.GS_Bigger, RedDogGlobals.GS_Smaller };

        static Dictionary<string, object> _line_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Radius, "float_positive_length_type" },
            { RedDogGlobals.GS_Construction, line_construct_options },
        };

        static Dictionary<string, object> _polygon_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Radius, "float_positive_length_type" },
            { RedDogGlobals.GS_Fill, "fill_type" },
            { RedDogGlobals.GS_Sides, "integer_minimum_3" },
            { RedDogGlobals.GS_Type, polygon_type_options },
        };

        static Dictionary<string, object> _curve_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
        };

        static Dictionary<string, object> _doubleline_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Width, "float_positive_length_type" },
            { RedDogGlobals.GS_Cap, doubleline_close_options },
            { RedDogGlobals.GS_Fill, "fill_type" },
        };

        static Dictionary<string, object> _rectangle_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Construction, rectangle_construct_options},
            { RedDogGlobals.GS_Height, "float_positive_type" },
            { RedDogGlobals.GS_Width, "float_positive_type" },
            { RedDogGlobals.GS_Radius, "float_positive_length_type" },
            { RedDogGlobals.GS_Fill, "fill_type" },
        };

        static Dictionary<string, object> _circle_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Construction, circle_construct_options},
            { RedDogGlobals.GS_Radius, "float_positive_length_type" },
            { RedDogGlobals.GS_Fill, "fill_type" },
        };

        static Dictionary<string, object> _arc_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Construction, arc_construct_options},
            { RedDogGlobals.GS_Radius, "float_positive_length_type" },
            { RedDogGlobals.GS_StartAngle, "float_angle_type" },
            { RedDogGlobals.GS_IncludedAngle, "float_angle_type" },
            { RedDogGlobals.GS_Fill, "fill_type" },
        };

        static Dictionary<string, object> _ellipse_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Construction, ellipse_construct_options },
            { RedDogGlobals.GS_Major, "float_positive_length_type" },
            { RedDogGlobals.GS_Ratio, "float_positive_type" },
            { RedDogGlobals.GS_StartAngle, "float_angle_type" },
            { RedDogGlobals.GS_IncludedAngle, "float_angle_type" },
            { RedDogGlobals.GS_AxisAngle, "float_angle_type" },
            { RedDogGlobals.GS_Fill, "fill_type" },
        };

        static Dictionary<string, object> _text_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_Construction, text_construct_options },
            { RedDogGlobals.GS_Alignment, text_alignment_options },
            { RedDogGlobals.GS_Position, text_position_options },
            { RedDogGlobals.GS_Text, "string_type" },
            { RedDogGlobals.GS_Angle, "float_angle_type" },
            { RedDogGlobals.GS_Size, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Spacing, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_TextStyle, "text_style_type" },
        };

        static Dictionary<string, object> _arrow_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Placement, arrow_ends_options },
            { RedDogGlobals.GS_ArrowStyle, "arrow_style_type" },
        };

        static Dictionary<string, object> _dimension_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Layer, "layer_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Type, dimension_type_options },
            { RedDogGlobals.GS_ArrowStyle, "arrow_style_type" },
            { RedDogGlobals.GS_TextStyle, "text_style_type" },
            { RedDogGlobals.GS_ShowText, "bool_type" },
            { RedDogGlobals.GS_ShowExtension, "bool_type" },
            { RedDogGlobals.GS_ShowUnit, "bool_type" },
        };

        static Dictionary<string, object> _creategroup_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _insertgroup_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _move_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _copy_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _resize_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _rotate_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _flip_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _transform_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Angle, "float_angle_type" },
            { RedDogGlobals.GS_Scale, "float_nonzero_type" },
            { RedDogGlobals.GS_XScale, "float_nonzero_type" },
            { RedDogGlobals.GS_YScale, "float_nonzero_type" },
        };

        static Dictionary<string, object> _delete_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _ungroup_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _properties_options = new Dictionary<string, object>()
        {
            { "print", "subcommand_type" },
            { "silent", "subcommand_type" },
       };

        static Dictionary<string, object> _movepoint_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _insertpoint_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _deletepoint_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _gap_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _extendtrim_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _edittext_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _origin_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _distance_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _angle_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _area_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _view_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _layer_add_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "tag_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Visible, "bool_type" },
        };

        static Dictionary<string, object> _layer_update_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "layer_type" },
            { RedDogGlobals.GS_LineType, "linetype_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_Thickness, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_Visible, "bool_type" },
        };

        static Dictionary<string, object> _layer_remove_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "layer_type" },
        };

        static Dictionary<string, object> _layer_list_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "layer_type" },
            { RedDogGlobals.GS_Contains , "string_type" },
        };

        static Dictionary<string, object> _linetype_add_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "tag_type" },
            { RedDogGlobals.GS_Lengths, "float_array_type" },
        };

        static Dictionary<string, object> _linetype_update_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "linetype_type" },
            { RedDogGlobals.GS_Lengths, "float_array_type" },
        };

        static Dictionary<string, object> _linetype_remove_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "linetype_type" },
        };

        static Dictionary<string, object> _linetype_list_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "linetype_type" },
            { RedDogGlobals.GS_Contains , "string_type" },
        };

        static Dictionary<string, object> _settings_drawing_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Width, "float_positive_size_type" },
            { RedDogGlobals.GS_Height, "float_positive_size_type" },
            { RedDogGlobals.GS_Scale, "ratio_type" },
            { RedDogGlobals.GS_PaperUnit, "unit_paper_type" },
            { RedDogGlobals.GS_ModelUnit, "unit_type" },
            { RedDogGlobals.GS_Print, "null_type" },
       };

        static Dictionary<string, object> _settings_grid_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Spacing, "float_positive_size_type" },
            { RedDogGlobals.GS_Divisions, "integer_positive_type" },
            { RedDogGlobals.GS_Intensity, "float_fraction_type" },
            { RedDogGlobals.GS_Visible, "bool_type" },
            { RedDogGlobals.GS_Print, "null_type" },
       };

        static Dictionary<string, object> _settings_color_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Background, "color_type" },
            { RedDogGlobals.GS_Print, "null_type" },
       };

        static Dictionary<string, object> _textstyle_add_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "tag_type" },
            { RedDogGlobals.GS_Font, "font_type" },
            { RedDogGlobals.GS_Size, "float_positive_size_type" },
            { RedDogGlobals.GS_Offset, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_LineSpacing, "float_positive_type" },
            { RedDogGlobals.GS_CharSpacing, "float_positive_type" },
        };

        static Dictionary<string, object> _textstyle_update_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "textstyle_type" },
            { RedDogGlobals.GS_Font, "font_type" },
            { RedDogGlobals.GS_Size, "float_positive_size_type" },
            { RedDogGlobals.GS_Offset, "float_nonnegative_size_type" },
            { RedDogGlobals.GS_LineSpacing, "float_positive_type" },
            { RedDogGlobals.GS_CharSpacing, "float_positive_type" },
        };

        static Dictionary<string, object> _textstyle_remove_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "textstyle_type" },
        };

        static Dictionary<string, object> _textstyle_list_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "textstyle_type" },
            { RedDogGlobals.GS_Contains , "string_type" },
        };

        static Dictionary<string, object> _arrowstyle_add_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "arrowstyle_type" },
            { RedDogGlobals.GS_Type, "arrow_type" },
            { RedDogGlobals.GS_Size, "float_positive_type" },
            { RedDogGlobals.GS_Ratio, "float_positive_type" },
        };

        static Dictionary<string, object> _arrowstyle_update_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "arrowstyle_type" },
            { RedDogGlobals.GS_Type, "arrow_type" },
            { RedDogGlobals.GS_Size, "float_positive_type" },
            { RedDogGlobals.GS_Ratio, "float_positive_type" },
        };

        static Dictionary<string, object> _arrowstyle_remove_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "arrowstyle_type" },
        };

        static Dictionary<string, object> _arrowstyle_list_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Name, "arrowstyle_type" },
            { RedDogGlobals.GS_Contains , "string_type" },
        };

        static Dictionary<string, object> _layer_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Add, _layer_add_options },
            { RedDogGlobals.GS_Update, _layer_update_options },
            { RedDogGlobals.GS_Remove, _layer_remove_options },
            { RedDogGlobals.GS_List, _layer_list_options },
        };

        static Dictionary<string, object> _linetype_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Add, _linetype_add_options },
            { RedDogGlobals.GS_Update, _linetype_update_options },
            { RedDogGlobals.GS_Remove, _linetype_remove_options },
            { RedDogGlobals.GS_List, _linetype_list_options },
        };

        static Dictionary<string, object> _textstyle_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Add, _textstyle_add_options },
            { RedDogGlobals.GS_Update, _textstyle_update_options },
            { RedDogGlobals.GS_Remove, _textstyle_remove_options },
            { RedDogGlobals.GS_List, _textstyle_list_options },
        };

        static Dictionary<string, object> _arrowstyle_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Add, _arrowstyle_add_options },
            { RedDogGlobals.GS_Update, _arrowstyle_update_options },
            { RedDogGlobals.GS_Remove, _arrowstyle_remove_options },
            { RedDogGlobals.GS_List, _arrowstyle_list_options },
        };

        static Dictionary<string, object> _status_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _settings_options = new Dictionary<string, object>()
        {
             { RedDogGlobals.GS_Drawing, _settings_drawing_options },
             { RedDogGlobals.GS_Grid, _settings_grid_options },
             { RedDogGlobals.GS_Colors, _settings_color_options },
        };

        static Dictionary<string, object> _triangle_options = new Dictionary<string, object>()
        {
            { RedDogGlobals.GS_Show, "bool_type" },
            { RedDogGlobals.GS_Flip, "bool_type" },
            { RedDogGlobals.GS_Rotate, "float_angle_type" },
            { RedDogGlobals.GS_Color, "color_type" },
            { RedDogGlobals.GS_Opacity, "float_fraction_type" },
            { RedDogGlobals.GS_Size, triangle_size_options },
            { RedDogGlobals.GS_Type, triangle_type_options },
        };

        static Dictionary<string, object> _console_options = new Dictionary<string, object>()
        {
            { "clear", "subcommand_type" },
            { "buffer-length", "integer_minimum_3" },
        };

        static Dictionary<string, object> _help_options = new Dictionary<string, object>()
        {
            { "*", "subcommand_type" },
            { "**", "subcommand_type" },
        };

        static Dictionary<string, object> _log_options = new Dictionary<string, object>()
        {
            { "start", "subcommand_type" },
            { "stop", "subcommand_type" },
            { "display", "subcommand_type" },
            { "save", "subcommand_type" },
            { "saveas", "subcommand_type" },
            { "print", "subcommand_type" },
            { "clear", "subcommand_type" },
        };

        static Dictionary<string, object> _open_options = new Dictionary<string, object>()
        {
            { "set-folder", "subcommand_type" },
            { "folder", "subcommand_type" },
            { "file", "path_string_type" },
        };

        static Dictionary<string, object> _save_options = new Dictionary<string, object>()
        {
            { "set-folder", "subcommand_type" },
            { "folder", "subcommand_type" },
            { "file", "path_string_type" },
        };

        static Dictionary<string, object> _export_options = new Dictionary<string, object>()
        {
        };

        static Dictionary<string, object> _js_options = new Dictionary<string, object>()
        {
            { "set-folder", "subcommand_type" },
            { "folder", "subcommand_type" },
            { "file", "path_string_type" },
            { "timeout", "float_positive_type" },
        };

        static Dictionary<string, object> _macro_options = new Dictionary<string, object>()
        {
            { "run", "subcommand_type" },
            { "file", "path_string_type" },
            { "verbose", "bool_type" },
        };

        static Dictionary<string, object> _no_options = new Dictionary<string, object>()
        {
        };

        public static Dictionary<string, Dictionary<string, object>> Commands = new Dictionary<string, Dictionary<string, object>>
        {
            { "open", _open_options },
            { "save", _save_options },
            { "export", _export_options },
            { "clear", null },
            { "line", _line_options},
            { "curve", _curve_options},
            { "polygon", _polygon_options },
            { "doubleline", _doubleline_options },
            { "rectangle", _rectangle_options },
            { "circle", _circle_options},
            { "arc", _arc_options},
            { "ellipse", _ellipse_options },
            { "text", _text_options },
            { "arrow", _arrow_options },
            { "dimension", _dimension_options },
            { "creategroup", _creategroup_options },
            { "insertgroup", _insertgroup_options },
            { "move", _move_options },
            { "copy", _copy_options },
            { "resize", _resize_options },
            { "rotate", _rotate_options },
            { "flip", _flip_options },
            { "transform", _transform_options },
            { "delete", _delete_options },
            { "ungroup", _ungroup_options },
            {  RedDogGlobals.GS_PropertiesCommand, _properties_options },
            { "movepoint", _movepoint_options },
            { "insertpoint", _insertpoint_options },
            { "deletepoint", _deletepoint_options },
            { "gap", _gap_options },
            { "extendtrim", _extendtrim_options },
            { "edittext", _edittext_options },
            { "origin", _origin_options },
            { "distance", null },
            { "angle", _angle_options },
            { "area", _area_options },
            { "view", _view_options },
            { RedDogGlobals.GS_Layer, _layer_options },
            { RedDogGlobals.GS_LineType, _linetype_options },
            { "textstyle", _textstyle_options },
            { "arrowstyle", _arrowstyle_options },
            { "status", _status_options },
            { RedDogGlobals.GS_SettingsCommand, _settings_options },
            { "triangle", _triangle_options },
            { "console", _console_options },
            { "script", _js_options },
            { "macro", _macro_options },
            { "help", _help_options },
            { "log", _log_options },
        };
    }
}
