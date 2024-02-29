using RedDog.HUIApp;
using RedDog;
using Microsoft.UI.Xaml.Controls;

namespace HUI
{
    public class HUIDispatcher
    {
        public static UserControl GetDialogById(string id)
        {
            UserControl control = null;

            switch (id)
            {
                case RedDogGlobals.GS_ArcCommand:
                    control = new HDrawArcDialog();
                    break;

                case RedDogGlobals.GS_ArrowCommand:
                    control = new HAnnotationArrowDialog();
                    break;

                case RedDogGlobals.GS_InsertGroupLinearCommand:
                    control = new HArrayLinearDialog();
                    break;

                case RedDogGlobals.GS_InsertGroupRadialCommand:
                    control = new HArrayRadialDialog();
                    break;

                case RedDogGlobals.GS_CircleCommand:
                    control = new HDrawCircleDialog();
                    break;

                case RedDogGlobals.GS_CurveCommand:
                    control = new HDrawCurveDialog();
                    break;

                case RedDogGlobals.GS_DimensionCommand:
                    control = new HAnnotationDimensionDialog();
                    break;

                case RedDogGlobals.GS_DoublelineCommand:
                    control = new HDrawDoublelineDialog();
                    break;

                case RedDogGlobals.GS_EllipseCommand:
                    control = new HDrawEllipseDialog();
                    break;

                case RedDogGlobals.GS_InsertGroupCommand:
                    control = new HInsertGroupDialog();
                    break;

                case RedDogGlobals.GS_InsertImageCommand:
                    control = new HInsertImageDialog();
                    break;

                case RedDogGlobals.GS_LineCommand:
                    control = new HDrawLineDialog();
                    break;

                case RedDogGlobals.GS_PolygonCommand:
                    control = new HDrawPolygonDialog();
                    break;

                case RedDogGlobals.GS_RectangleCommand:
                    control = new HDrawRectangleDialog();
                    break;

                case RedDogGlobals.GS_TextCommand:
                    control = new HAnnotationTextDialog();
                    break;

                case RedDogGlobals.GS_SettingsApplicationCommand:
                    control = new HSettingsApplication();
                    break;

                case RedDogGlobals.GS_ManageSymbolsCommand:
                    control = new HManageSymbols();
                    break;

                case RedDogGlobals.GS_SettingsSupportCommand:
                    control = new HSettingsPrivacySupport();
                    break;

                case RedDogGlobals.GS_SettingsDrawingCommand:
                    control = new HSettingsDrawing();
                    break;

                case RedDogGlobals.GS_SettingsLayersCommand:
                    control = new HSettingsLayers();
                    break;

                case RedDogGlobals.GS_SettingsLineTypesCommand:
                    control = new HSettingsLineTypes();
                    break;

                case RedDogGlobals.GS_SettingsTextStylesCommand:
                    control = new HSettingsTextStyles();
                    break;

                case RedDogGlobals.GS_SettingsArrowStylesCommand:
                    control = new HSettingsArrowStyles();
                    break;

                case RedDogGlobals.GS_SettingsPatternsCommand:
                    control = new HSettingsPatterns();
                    break;

                default:
                    control = new HUIDialog(id);
                    break;
            }

            return control;
        }
    }
}
