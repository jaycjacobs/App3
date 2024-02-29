using Cirros;
using Cirros.Core;
using Cirros.Drawing;
using Cirros.Primitives;
using Cirros.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Cirros.Dxf
{
    public class DxfExport
    {
        static Rect _extents = Rect.Empty;

        static DxfExport()
        {
            for (int i = 0; i < 256; i++)
            {
                uint cs = StandardColors.AcadColorSpecTable[i];
                if (AcadColorTable.ContainsKey(cs) == false)
                {
                    AcadColorTable.Add(cs, i);
                }
            }
        }

        public static Dictionary<int, string> LineTypes = null;
        public static Dictionary<uint, int> AcadColorTable = new Dictionary<uint, int>();

        private static int _nextHandleValue = 1;
        private static int _firstStyleHandle =1;
        private Point _modelOrigin;
        private Point _paperMax;
        private double _scale = 1;

        private static Tesselator _tesselator = null;

        public static string ExportToDxf(bool showFrame)
        {
            _tesselator = new Tesselator();

            _nextHandleValue = 0x20;    // Set next handle to 0x20 to allow for hard-coded handles in OBJECTS

            StringBuilder sb = new StringBuilder();

            DrawingDocument dc = Globals.ActiveDrawing;
            if (dc != null)
            {
                StringBuilder contentSB = new StringBuilder();

                exportClasses(contentSB, dc);
                exportTables(contentSB, dc);
                exportBlocks(contentSB, dc);
                exportEntities(contentSB, dc, showFrame);
                exportObjects(contentSB, dc);

                // The header contains data that is generated during the content export, so the header must be rendered last
                exportHeader(sb, dc);

                // Add the content to the main StringBuilder
                sb.Append(contentSB.ToString());

                writeGroup(sb, 0, "EOF");
            }

            _tesselator = null;

            return sb.ToString();
        }

        private static void exportHeader(StringBuilder sb, DrawingDocument dc)
        {
            bool isMetric = Globals.ActiveDrawing.PaperUnit == Unit.Millimeters;

            writeGroup(sb, 0, "SECTION");
            writeGroup(sb, 2, "HEADER");

            //  9
            //$ACADVER
            //  1
            //AC1014
            writeGroup(sb, 9, "$ACADVER");
            writeGroup(sb, 1, "AC1014");
  
            //  9
            //$ACADMAINTVER
            // 70
            //     0
            writeGroup(sb, 9, "$ACADMAINTVER");
            writeIntGroup(sb, 70, 0);
  
            //  9
            //$DWGCODEPAGE
            //  3
            //ANSI_1252
            //  9
            writeGroup(sb, 9, "$DWGCODEPAGE");
            writeGroup(sb, 3, "ANSI_1252");

            //  9
            //$INSBASE
            // 10
            //0.0
            // 20
            //0.0
            // 30
            //0.0
            writeGroup(sb, 9, "$INSBASE");
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 0);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$EXTMIN
            // 10
            //0.5
            // 20
            //-11.877871
            // 30
            //0.0
            // IMPORTANT: The HEADER section MUST be rendered last to provide valid extents
            Point extMin = Globals.ActiveDrawing.PaperToModel(new Point(_extents.Left, _extents.Bottom));
            writeGroup(sb, 9, "$EXTMIN");
            writeFloatGroup(sb, 10, (float)extMin.X);
            writeFloatGroup(sb, 20, (float)extMin.Y);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$EXTMAX
            // 10
            //147.2
            // 20
            //100.58269
            // 30
            //0.0
            Point extMax = Globals.ActiveDrawing.PaperToModel(new Point(_extents.Right, _extents.Top));
            writeGroup(sb, 9, "$EXTMAX");
            writeFloatGroup(sb, 10, (float)extMax.X);
            writeFloatGroup(sb, 20, (float)extMax.Y);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$LIMMIN
            // 10
            //0.0
            // 20
            //0.0
            writeGroup(sb, 9, "$LIMMIN");
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 0);

            //  9
            //$LIMMAX
            // 10
            //12.0
            // 20
            //9.0
            writeGroup(sb, 9, "$LIMMAX");
            writeFloatGroup(sb, 10, (float)Globals.ActiveDrawing.PaperSize.Width);
            writeFloatGroup(sb, 20, (float)Globals.ActiveDrawing.PaperSize.Height);

            //  9
            //$ORTHOMODE
            // 70
            //     0
            writeGroup(sb, 9, "$ORTHOMODE");
            writeIntGroup(sb, 70, 0);

            //  9
            //$REGENMODE
            // 70
            //     1
            writeGroup(sb, 9, "$REGENMODE");
            writeIntGroup(sb, 70, 1);

            //  9
            //$FILLMODE
            // 70
            //     1
            writeGroup(sb, 9, "$FILLMODE");
            writeIntGroup(sb, 70, 1);

            //  9
            //$QTEXTMODE
            // 70
            //     0
            writeGroup(sb, 9, "$QTEXTMODE");
            writeIntGroup(sb, 70, 0);

            //  9
            //$MIRRTEXT
            // 70
            //     1
            writeGroup(sb, 9, "$MIRRTEXT");
            writeIntGroup(sb, 70, 1);

            //  9
            //$DRAGMODE
            // 70
            //     2
            writeGroup(sb, 9, "$DRAGMODE");
            writeIntGroup(sb, 70, 2);

            //  9
            //$LTSCALE
            // 40
            //1.0
            writeGroup(sb, 9, "$LTSCALE");
            writeFloatGroup(sb, 40, 1F);

            //  9
            //$OSMODE
            // 70
            //     0
            writeGroup(sb, 9, "$OSMODE");
            writeIntGroup(sb, 70, 0);

            //  9
            //$ATTMODE
            // 70
            //     1
            writeGroup(sb, 9, "$ATTMODE");
            writeIntGroup(sb, 70, 1);

            //  9
            //$TEXTSIZE
            // 40
            //0.2
            // NEED VALUE
            writeGroup(sb, 9, "$TEXTSIZE");
            writeFloatGroup(sb, 40, .2F);

            //  9
            //$TRACEWID
            // 40
            //0.05
            writeGroup(sb, 9, "$TRACEWID");
            writeFloatGroup(sb, 40, .05F);

            //  9
            //$TEXTSTYLE
            //  7
            //STANDARD
            // NEED VALUE
            writeGroup(sb, 9, "$TEXTSTYLE");
            writeGroup(sb, 7, FixAcName(Globals.TextStyleTable[0].Name));

            //  9
            //$CLAYER
            //  8
            //0
            // NEED VALUE
            writeGroup(sb, 9, "$CLAYER");
            writeGroup(sb, 8, "0");

            //  9
            //$CELTYPE
            //  6
            //BYLAYER
            writeGroup(sb, 9, "$CELTYPE");
            writeGroup(sb, 6, "BYLAYER");

            //  9
            //$CECOLOR
            // 62
            //   256
            writeGroup(sb, 9, "$CECOLOR");
            writeIntGroup(sb, 62, 256);

            //  9
            //$CELTSCALE
            // 40
            //1.0
            writeGroup(sb, 9, "$CELTSCALE");
            writeFloatGroup(sb, 40, 1F);

            //  9
            //$DELOBJ
            // 70
            //     1
            writeGroup(sb, 9, "$DELOBJ");
            writeIntGroup(sb, 70, 1);

            //  9
            //$DISPSILH
            // 70
            //     0
            writeGroup(sb, 9, "$DISPSILH");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMSCALE
            // 40
            //1.0
            writeGroup(sb, 9, "$DIMSCALE");
            writeFloatGroup(sb, 40, 1F);

            //  9
            //$DIMASZ
            // 40
            //0.18
            writeGroup(sb, 9, "$DIMASZ");
            writeFloatGroup(sb, 40, .18F);

            //  9
            //$DIMEXO
            // 40
            //0.0625
            writeGroup(sb, 9, "$DIMEXO");
            writeFloatGroup(sb, 40, .0625F);

            //  9
            //$DIMDLI
            // 40
            //0.38
            writeGroup(sb, 9, "$DIMDLI");
            writeFloatGroup(sb, 40, .38F);

            //  9
            //$DIMRND
            // 40
            //0.0
            writeGroup(sb, 9, "$DIMRND");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$DIMDLE
            // 40
            //0.0
            writeGroup(sb, 9, "$DIMDLE");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$DIMEXE
            // 40
            //0.18
            writeGroup(sb, 9, "$DIMEXE");
            writeFloatGroup(sb, 40, .18F);

            //  9
            //$DIMTP
            // 40
            //0.0
            writeGroup(sb, 9, "$DIMTP");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$DIMTM
            // 40
            //0.0
            writeGroup(sb, 9, "$DIMTM");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$DIMTXT
            // 40
            //0.18
            writeGroup(sb, 9, "$DIMTXT");
            writeFloatGroup(sb, 40, .18F);

            //  9
            //$DIMCEN
            // 40
            //0.09
            writeGroup(sb, 9, "$DIMCEN");
            writeFloatGroup(sb, 40, .09F);

            //  9
            //$DIMTSZ
            // 40
            //0.0
            writeGroup(sb, 9, "$DIMTSZ");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$DIMTOL
            // 70
            //     0
            writeGroup(sb, 9, "$DIMTOL");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMLIM
            // 70
            //     0
            writeGroup(sb, 9, "$DIMLIM");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMTIH
            // 70
            //     1
            writeGroup(sb, 9, "$DIMTIH");
            writeIntGroup(sb, 70, 1);

            //  9
            //$DIMTOH
            // 70
            //     1
            writeGroup(sb, 9, "$DIMTOH");
            writeIntGroup(sb, 70, 1);

            //  9
            //$DIMSE1
            // 70
            //     0
            writeGroup(sb, 9, "$DIMSE1");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMSE2
            // 70
            //     0
            writeGroup(sb, 9, "$DIMSE2");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMTAD
            // 70
            //     0
            writeGroup(sb, 9, "$DIMTAD");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMZIN
            // 70
            //     0
            writeGroup(sb, 9, "$DIMZIN");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMBLK
            //  1
            //
            writeGroup(sb, 9, "$DIMBLK");
            writeGroup(sb, 1, "");

            //  9
            //$DIMASO
            // 70
            //     1
            writeGroup(sb, 9, "$DIMASO");
            writeIntGroup(sb, 70, 1);

            //  9
            //$DIMSHO
            // 70
            //     1
            writeGroup(sb, 9, "$DIMSHO");
            writeIntGroup(sb, 70, 1);

            //  9
            //$DIMPOST
            //  1
            //
            writeGroup(sb, 9, "$DIMPOST");
            writeGroup(sb, 1, "");

            //  9
            //$DIMAPOST
            //  1
            //
            writeGroup(sb, 9, "$DIMAPOST");
            writeGroup(sb, 1, "");

            //  9
            //$DIMALT
            // 70
            //     0
            writeGroup(sb, 9, "$DIMALT");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMALTD
            // 70
            //     2
            writeGroup(sb, 9, "$DIMALTD");
            writeIntGroup(sb, 70, 2);

            //  9
            //$DIMALTF
            // 40
            //25.4
            writeGroup(sb, 9, "$DIMALTF");
            writeFloatGroup(sb, 40, 25.4F);

            //  9
            //$DIMLFAC
            // 40
            //1.0
            writeGroup(sb, 9, "$DIMLFAC");
            writeFloatGroup(sb, 40, 1F);

            //  9
            //$DIMTOFL
            // 70
            //     0
            writeGroup(sb, 9, "$DIMTOFL");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMTVP
            // 40
            //0.0
            writeGroup(sb, 9, "$DIMTVP");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$DIMTIX
            // 70
            //     0
            writeGroup(sb, 9, "$DIMTIX");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMSOXD
            // 70
            //     0
            writeGroup(sb, 9, "$DIMSOXD");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMSAH
            // 70
            //     0
            writeGroup(sb, 9, "$DIMSAH");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMBLK1
            //  1
            //
            writeGroup(sb, 9, "$DIMBLK1");
            writeGroup(sb, 1, "");

            //  9
            //$DIMBLK2
            //  1
            //
            writeGroup(sb, 9, "$DIMBLK2");
            writeGroup(sb, 1, "");

            //  9
            //$DIMSTYLE
            //  2
            //STANDARD
            writeGroup(sb, 9, "$DIMSTYLE");
            writeGroup(sb, 2, "STANDARD");

            //  9
            //$DIMCLRD
            // 70
            //     0
            writeGroup(sb, 9, "$DIMCLRD");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMCLRE
            // 70
            //     0
            writeGroup(sb, 9, "$DIMCLRE");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMCLRT
            // 70
            //     0
            writeGroup(sb, 9, "$DIMCLRT");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMTFAC
            // 40
            //1.0
            writeGroup(sb, 9, "$DIMTFAC");
            writeFloatGroup(sb, 40, 1F);

            //  9
            //$DIMGAP
            // 40
            //0.09
            writeGroup(sb, 9, "$DIMGAP");
            writeFloatGroup(sb, 40, .09F);

            //  9
            //$DIMJUST
            // 70
            //     0
            writeGroup(sb, 9, "$DIMJUST");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMSD1
            // 70
            //     0
            writeGroup(sb, 9, "$DIMSD1");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMSD2
            // 70
            //     0
            writeGroup(sb, 9, "$DIMSD2");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMTOLJ
            // 70
            //     1
            writeGroup(sb, 9, "$DIMTOLJ");
            writeIntGroup(sb, 70, 1);

            //  9
            //$DIMTZIN
            // 70
            //     0
            writeGroup(sb, 9, "$DIMTZIN");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMALTZ
            // 70
            //     0
            writeGroup(sb, 9, "$DIMALTZ");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMALTTZ
            // 70
            //     0
            writeGroup(sb, 9, "$DIMALTTZ");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMFIT
            // 70
            //     3
            writeGroup(sb, 9, "$DIMFIT");
            writeIntGroup(sb, 70, 3);

            //  9
            //$DIMUPT
            // 70
            //     0
            writeGroup(sb, 9, "$DIMUPT");
            writeIntGroup(sb, 70, 0);

            //  9
            //$DIMUNIT
            // 70
            //     2
            writeGroup(sb, 9, "$DIMUNIT");
            writeIntGroup(sb, 70, 2);

            //  9
            //$DIMDEC
            // 70
            //     4
            writeGroup(sb, 9, "$DIMDEC");
            writeIntGroup(sb, 70, 4);

            //  9
            //$DIMTDEC
            // 70
            //     4
            writeGroup(sb, 9, "$DIMTDEC");
            writeIntGroup(sb, 70, 4);

            //  9
            //$DIMALTU
            // 70
            //     2
            writeGroup(sb, 9, "$DIMALTU");
            writeIntGroup(sb, 70, 2);

            //  9
            //$DIMALTTD
            // 70
            //     2
            writeGroup(sb, 9, "$DIMALTTD");
            writeIntGroup(sb, 70, 2);

            //  9
            //$DIMTXSTY
            //  7
            //STANDARD
            writeGroup(sb, 9, "$DIMTXSTY");
            writeGroup(sb, 7, "STANDARD");

            //  9
            //$DIMAUNIT
            // 70
            //     0
            writeGroup(sb, 9, "$DIMAUNIT");
            writeIntGroup(sb, 70, 0);

            //  9
            //$LUNITS
            // 70
            //     2
            writeGroup(sb, 9, "$LUNITS");
            writeIntGroup(sb, 70, 2);

            //  9
            //$LUPREC
            // 70
            //     4
            writeGroup(sb, 9, "$LUPREC");
            writeIntGroup(sb, 70, 4);

            //  9
            //$SKETCHINC
            // 40
            //0.1
            writeGroup(sb, 9, "$SKETCHINC");
            writeFloatGroup(sb, 40, .1F);

            //  9
            //$FILLETRAD
            // 40
            //0.0
            writeGroup(sb, 9, "$FILLETRAD");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$AUNITS
            // 70
            //     0
            writeGroup(sb, 9, "$AUNITS");
            writeIntGroup(sb, 70, 0);

            //  9
            //$AUPREC
            // 70
            //     0
            writeGroup(sb, 9, "$AUPREC");
            writeIntGroup(sb, 70, 0);

            //  9
            //$MENU
            //  1
            //acad
            writeGroup(sb, 9, "$MENU");
            writeGroup(sb, 1, "acad");

            //  9
            //$ELEVATION
            // 40
            //0.0
            writeGroup(sb, 9, "$ELEVATION");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$PELEVATION
            // 40
            //0.0
            writeGroup(sb, 9, "$PELEVATION");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$THICKNESS
            // 40
            //0.0
            writeGroup(sb, 9, "$THICKNESS");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$LIMCHECK
            // 70
            //     0
            writeGroup(sb, 9, "$LIMCHECK");
            writeIntGroup(sb, 70, 0);

            //  9
            //$BLIPMODE
            // 70
            //     0
            writeGroup(sb, 9, "$BLIPMODE");
            writeIntGroup(sb, 70, 0);

            //  9
            //$CHAMFERA
            // 40
            //0.0
            writeGroup(sb, 9, "$CHAMFERA");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$CHAMFERB
            // 40
            //0.0
            writeGroup(sb, 9, "$CHAMFERB");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$CHAMFERC
            // 40
            //0.0
            writeGroup(sb, 9, "$CHAMFERC");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$CHAMFERD
            // 40
            //0.0
            writeGroup(sb, 9, "$CHAMFERD");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$SKPOLY
            // 70
            //     1
            writeGroup(sb, 9, "$SKPOLY");
            writeIntGroup(sb, 70, 1);

            //  9
            //$TDCREATE
            // 40
            //2449678.746319097
            writeGroup(sb, 9, "$TDCREATE");
            writeDateGroup(sb, 40, DateTime.Now);

            //  9
            //$TDUPDATE
            // 40
            //2450710.765058229
            writeGroup(sb, 9, "$TDUPDATE");
            writeDateGroup(sb, 40, DateTime.Now);

            //  9
            //$TDINDWG
            // 40
            //0.0002864583
            writeGroup(sb, 9, "$TDINDWG");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$TDUSRTIMER
            // 40
            //0.0002864583
            writeGroup(sb, 9, "$TDUSRTIMER");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$USRTIMER
            // 70
            //     1
            writeGroup(sb, 9, "$USRTIMER");
            writeIntGroup(sb, 70, 0);

            //  9
            //$ANGBASE
            // 50
            //0.0
            writeGroup(sb, 9, "$ANGBASE");
            writeFloatGroup(sb, 50, 0);

            //  9
            //$ANGDIR
            // 70
            //     0
            writeGroup(sb, 9, "$ANGDIR");
            writeIntGroup(sb, 70, 0);

            //  9
            //$PDMODE
            // 70
            //     0
            writeGroup(sb, 9, "$PDMODE");
            writeIntGroup(sb, 70, 0);

            //  9
            //$PDSIZE
            // 40
            //0.0
            writeGroup(sb, 9, "$PDSIZE");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$PLINEWID
            // 40
            //0.0
            writeGroup(sb, 9, "$PLINEWID");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$COORDS
            // 70
            //     1
            writeGroup(sb, 9, "$COORDS");
            writeIntGroup(sb, 70, 1);

            //  9
            //$SPLFRAME
            // 70
            //     0
            writeGroup(sb, 9, "$SPLFRAME");
            writeIntGroup(sb, 70, 0);

            //  9
            //$SPLINETYPE
            // 70
            //     6
            writeGroup(sb, 9, "$SPLINETYPE");
            writeIntGroup(sb, 70, 6);

            //  9
            //$SPLINESEGS
            // 70
            //     8
            writeGroup(sb, 9, "$SPLINESEGS");
            writeIntGroup(sb, 70, 8);

            //  9
            //$ATTDIA
            // 70
            //     0
            writeGroup(sb, 9, "$ATTDIA");
            writeIntGroup(sb, 70, 0);

            //  9
            //$ATTREQ
            // 70
            //     1
            writeGroup(sb, 9, "$ATTREQ");
            writeIntGroup(sb, 70, 1);

            //  9
            //$HANDLING
            // 70
            //     1
            writeGroup(sb, 9, "$HANDLING");
            writeIntGroup(sb, 70, 1);

            //  9
            //$HANDSEED
            //  5
            //178
            // IMPORTANT: The HEADER section MUST be rendered last to provide a valid _nextHandleValue
            writeGroup(sb, 9, "$HANDSEED");
            writeGroup(sb, 5, string.Format("{0:X}", _nextHandleValue));

            //  9
            //$SURFTAB1
            // 70
            //     6
            writeGroup(sb, 9, "$SURFTAB1");
            writeIntGroup(sb, 70, 6);

            //  9
            //$SURFTAB2
            // 70
            //     6
            writeGroup(sb, 9, "$SURFTAB2");
            writeIntGroup(sb, 70, 6);

            //  9
            //$SURFTYPE
            // 70
            //     6
            writeGroup(sb, 9, "$SURFTYPE");
            writeIntGroup(sb, 70, 6);

            //  9
            //$SURFU
            // 70
            //     6
            writeGroup(sb, 9, "$SURFU");
            writeIntGroup(sb, 70, 6);

            //  9
            //$SURFV
            // 70
            //     6
            writeGroup(sb, 9, "$SURFV");
            writeIntGroup(sb, 70, 6);

            //  9
            //$UCSNAME
            //  2
            //
            writeGroup(sb, 9, "$UCSNAME");
            writeGroup(sb, 2, "");

            //  9
            //$UCSORG
            // 10
            //0.0
            // 20
            //0.0
            // 30
            //0.0
            writeGroup(sb, 9, "$UCSORG");
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 0);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$UCSXDIR
            // 10
            //1.0
            // 20
            //0.0
            // 30
            //0.0
            writeGroup(sb, 9, "$UCSXDIR");
            writeFloatGroup(sb, 10, 1F);
            writeFloatGroup(sb, 20, 0);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$UCSYDIR
            // 10
            //0.0
            // 20
            //1.0
            // 30
            //0.0
            writeGroup(sb, 9, "$UCSYDIR");
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 1F);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$PUCSNAME
            //  2
            //
            writeGroup(sb, 9, "$PUCSNAME");
            writeGroup(sb, 2, "");

            //  9
            //$PUCSORG
            // 10
            //0.0
            // 20
            //0.0
            // 30
            //0.0
            writeGroup(sb, 9, "$PUCSORG");
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 0);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$PUCSXDIR
            // 10
            //1.0
            // 20
            //0.0
            // 30
            //0.0
            writeGroup(sb, 9, "$PUCSXDIR");
            writeFloatGroup(sb, 10, 1F);
            writeFloatGroup(sb, 20, 0);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$PUCSYDIR
            // 10
            //0.0
            // 20
            //1.0
            // 30
            //0.0
            writeGroup(sb, 9, "$PUCSYDIR");
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 1F);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$USERI1
            // 70
            //     0
            writeGroup(sb, 9, "$USERI1");
            writeIntGroup(sb, 70, 0);

            //  9
            //$USERI2
            // 70
            //     0
            writeGroup(sb, 9, "$USERI2");
            writeIntGroup(sb, 70, 0);

            //  9
            //$USERI3
            // 70
            //     0
            writeGroup(sb, 9, "$USERI3");
            writeIntGroup(sb, 70, 0);

            //  9
            //$USERI4
            // 70
            //     0
            writeGroup(sb, 9, "$USERI4");
            writeIntGroup(sb, 70, 0);

            //  9
            //$USERI5
            // 70
            //     0
            writeGroup(sb, 9, "$USERI5");
            writeIntGroup(sb, 70, 0);

            //  9
            //$USERR1
            // 40
            //0.0
            writeGroup(sb, 9, "$USERR1");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$USERR2
            // 40
            //0.0
            writeGroup(sb, 9, "$USERR2");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$USERR3
            // 40
            //0.0
            writeGroup(sb, 9, "$USERR3");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$USERR4
            // 40
            //0.0
            writeGroup(sb, 9, "$USERR4");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$USERR5
            // 40
            //0.0
            writeGroup(sb, 9, "$USERR5");
            writeFloatGroup(sb, 40, 0);

            //  9
            //$WORLDVIEW
            // 70
            //     1
            writeGroup(sb, 9, "$WORLDVIEW");
            writeIntGroup(sb, 70, 1);

            //  9
            //$SHADEDGE
            // 70
            //     3
            writeGroup(sb, 9, "$SHADEDGE");
            writeIntGroup(sb, 70, 3);

            //  9
            //$SHADEDIF
            // 70
            //    70
            writeGroup(sb, 9, "$SHADEDIF");
            writeIntGroup(sb, 70, 70);

            //  9
            //$TILEMODE
            // 70
            //     1
            writeGroup(sb, 9, "$TILEMODE");
            writeIntGroup(sb, 70, 1);

            //  9
            //$MAXACTVP
            // 70
            //    48
            writeGroup(sb, 9, "$MAXACTVP");
            writeIntGroup(sb, 70, 48);

            //  9
            //$PINSBASE
            // 10
            //0.0
            // 20
            //0.0
            // 30
            //0.0
            writeGroup(sb, 9, "$PINSBASE");
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 0);
            writeFloatGroup(sb, 30, 0);

            //  9
            //$PLIMCHECK
            // 70
            //     0
            writeGroup(sb, 9, "$PLIMCHECK");
            writeIntGroup(sb, 70, 0);

            //  9
            //$PEXTMIN
            // 10
            //1.000000E+20
            // 20
            //1.000000E+20
            // 30
            //1.000000E+20
            writeGroup(sb, 9, "$PEXTMIN");
            writeFloatGroup(sb, 10, float.MaxValue);
            writeFloatGroup(sb, 20, float.MaxValue);
            writeFloatGroup(sb, 30, float.MaxValue);

            //  9
            //$PEXTMAX
            // 10
            //-1.000000E+20
            // 20
            //-1.000000E+20
            // 30
            //-1.000000E+20
            writeGroup(sb, 9, "$PEXTMAX");
            writeFloatGroup(sb, 10, float.MinValue);
            writeFloatGroup(sb, 20, float.MinValue);
            writeFloatGroup(sb, 30, float.MinValue);

            //  9
            //$PLIMMIN
            // 10
            //0.0
            // 20
            //0.0
            writeGroup(sb, 9, "$PLIMMIN");
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 0);

            //  9
            //$PLIMMAX
            // 10
            //12.0
            // 20
            //9.0
            writeGroup(sb, 9, "$PLIMMAX");
            writeFloatGroup(sb, 10, (float)Globals.ActiveDrawing.PaperSize.Width);
            writeFloatGroup(sb, 20, (float)Globals.ActiveDrawing.PaperSize.Height);

            //  9
            //$UNITMODE
            // 70
            //     0
            writeGroup(sb, 9, "$UNITMODE");
            writeIntGroup(sb, 70, 0);

            //  9
            //$VISRETAIN
            // 70
            //     0
            writeGroup(sb, 9, "$VISRETAIN");
            writeIntGroup(sb, 70, 0);

            //  9
            //$PLINEGEN
            // 70
            //     0
            writeGroup(sb, 9, "$PLINEGEN");
            writeIntGroup(sb, 70, 0);

            //  9
            //$PSLTSCALE
            // 70
            //     1
            writeGroup(sb, 9, "$PSLTSCALE");
            writeIntGroup(sb, 70, 1);

            //  9
            //$TREEDEPTH
            // 70
            //  3020
            writeGroup(sb, 9, "$TREEDEPTH");
            writeIntGroup(sb, 70, 3020);

            //  9
            //$PICKSTYLE
            // 70
            //     1
            writeGroup(sb, 9, "$PICKSTYLE");
            writeIntGroup(sb, 70, 1);

            //  9
            //$CMLSTYLE
            //  2
            //STANDARD
            writeGroup(sb, 9, "$CMLSTYLE");
            writeGroup(sb, 2, "STANDARD");

            //  9
            //$CMLJUST
            // 70
            //     0
            writeGroup(sb, 9, "$CMLJUST");
            writeIntGroup(sb, 70, 0);

            //  9
            //$CMLSCALE
            // 40
            //1.0
            writeGroup(sb, 9, "$CMLSCALE");
            writeFloatGroup(sb, 40, 1F);

            //  9
            //$PROXYGRAPHICS
            // 70
            //     1
            writeGroup(sb, 9, "$PROXYGRAPHICS");
            writeIntGroup(sb, 70, 1);

            //  9
            //$MEASUREMENT
            // 70
            //     0
            writeGroup(sb, 9, "$MEASUREMENT");
            writeIntGroup(sb, 70, isMetric ? 1 : 0);

            writeGroup(sb, 0, "ENDSEC");
        }

        private static void exportClasses(StringBuilder sb, DrawingDocument dc)
        {
            writeGroup(sb, 0, "SECTION");
            writeGroup(sb, 2, "CLASSES");
            writeGroup(sb, 0, "ENDSEC");
        }

        private static void exportTables(StringBuilder sb, DrawingDocument dc)
        {
            writeGroup(sb, 0, "SECTION");
            writeGroup(sb, 2, "TABLES");

            exportVportTables(sb);
            exportLtypeTables(sb);
            exportLayerTables(sb);
            exportStyleTables(sb);
            exportViewTables(sb);
            exportUcsTables(sb);
            exportAppIdTables(sb);
            exportDimStyleTables(sb);
            exportBlockRecordTables(sb);

            writeGroup(sb, 0, "ENDSEC");
        }

        private static void exportViewTables(StringBuilder sb)
        {
            //  0
            //TABLE
            //  2
            //VIEW
            //  5
            //6
            //100
            //AcDbSymbolTable
            // 70
            //     0
            writeGroup(sb, 0, "TABLE");
            writeGroup(sb, 2, "VIEW");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTable");
            writeIntGroup(sb, 70, 0);
            //  0
            //ENDTAB
            writeGroup(sb, 0, "ENDTAB");
        }

        private static void exportBlockRecordTables(StringBuilder sb)
        {
            //  0
            //TABLE
            //  2
            //BLOCK_RECORD
            //  5
            //1
            //100
            //AcDbSymbolTable
            // 70
            //     0
            writeGroup(sb, 0, "TABLE");
            writeGroup(sb, 2, "BLOCK_RECORD");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTable");
            writeIntGroup(sb, 70, 0);
            //  0
            //BLOCK_RECORD
            //  5
            //1A
            //100
            //AcDbSymbolTableRecord
            //100
            //AcDbBlockTableRecord
            //  2
            //*MODEL_SPACE
            writeGroup(sb, 0, "BLOCK_RECORD");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTableRecord");
            writeGroup(sb, 100, "AcDbBlockTableRecord");
            writeGroup(sb, 2, "*MODEL_SPACE");
            //  0
            //BLOCK_RECORD
            //  5
            //17
            //100
            //AcDbSymbolTableRecord
            //100
            //AcDbBlockTableRecord
            //  2
            //*PAPER_SPACE
            writeGroup(sb, 0, "BLOCK_RECORD");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTableRecord");
            writeGroup(sb, 100, "AcDbBlockTableRecord");
            writeGroup(sb, 2, "*PAPER_SPACE");
            //  0
            //ENDTAB
            writeGroup(sb, 0, "ENDTAB");
        }

        private static void exportUcsTables(StringBuilder sb)
        {
            //  0
            //TABLE
            //  2
            //UCS
            //  5
            //7
            //100
            //AcDbSymbolTable
            // 70
            //     0
            writeGroup(sb, 0, "TABLE");
            writeGroup(sb, 2, "UCS");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTable");
            writeIntGroup(sb, 70, 0);
            //  0
            //ENDTAB
            writeGroup(sb, 0, "ENDTAB");
        }

        private static void exportAppIdTables(StringBuilder sb)
        {
            //  0
            //TABLE
            //  2
            //APPID
            //  5
            //9
            //100
            //AcDbSymbolTable
            // 70
            //     1
            writeGroup(sb, 0, "TABLE");
            writeGroup(sb, 2, "APPID");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTable");
            writeIntGroup(sb, 70, 1);
            //  0
            //APPID
            //  5
            //11
            //100
            //AcDbSymbolTableRecord
            //100
            //AcDbRegAppTableRecord
            //  2
            //ACAD
            // 70
            //     0
            writeGroup(sb, 0, "APPID");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTableRecord");
            writeGroup(sb, 100, "AcDbRegAppTableRecord");
            writeGroup(sb, 2, "ACAD");
            writeIntGroup(sb, 70, 0);
            //  0
            //ENDTAB
            writeGroup(sb, 0, "ENDTAB");
        }

        private static void exportVportTables(StringBuilder sb)
        {
            //  0
            //TABLE
            //  2
            //VPORT
            //  5
            //8
            //100
            //AcDbSymbolTable
            // 70
            //     1
            writeGroup(sb, 0, "TABLE");
            writeGroup(sb, 2, "VPORT");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTable");
            writeIntGroup(sb, 70, 1);

            {
                // Create VPORT table

                //  0
                //VPORT
                //  5
                //174
                //100
                //AcDbSymbolTableRecord
                //100
                //AcDbViewportTableRecord
                writeGroup(sb, 0, "VPORT");
                writeHandleGroup(sb, 5);
                writeGroup(sb, 100, "AcDbSymbolTableRecord");
                writeGroup(sb, 100, "AcDbViewportTableRecord");
                //  2
                //*ACTIVE
                writeGroup(sb, 2, "*ACTIVE");
                // 70
                //     0
                writeIntGroup(sb, 70, 0);
                // 10
                //0.0
                // 20
                //0.0
                writeFloatGroup(sb, 10, 0);
                writeFloatGroup(sb, 20, 0);
                // 11
                //1.0
                // 21
                //1.0
                writeFloatGroup(sb, 11, 1F);
                writeFloatGroup(sb, 21, 1F);
                // 12
                //73.847071
                // 22
                //44.35241
                Point pc = new Point(Globals.ActiveDrawing.PaperSize.Width / 2, Globals.ActiveDrawing.PaperSize.Height / 2);
                Point mc = Globals.ActiveDrawing.PaperToModel(pc);
                writeFloatGroup(sb, 12, (float)mc.X);
                writeFloatGroup(sb, 22, (float)mc.Y);
                // 13
                //0.0
                // 23
                //0.0
                writeFloatGroup(sb, 13, 0);
                writeFloatGroup(sb, 23, 0);
                // 14
                //0.25
                // 24
                //0.25
                writeFloatGroup(sb, 14, (float)Globals.xSnap);
                writeFloatGroup(sb, 24, (float)Globals.ySnap);
                // 15
                //0.0
                // 25
                //0.0
                writeFloatGroup(sb, 15, 0);     // Grid spacing same as snap spacing?  What does 0 mean?
                writeFloatGroup(sb, 25, 0);
                // 16
                //0.0
                // 26
                //0.0
                // 36
                //1.0
                writeFloatGroup(sb, 16, 0);
                writeFloatGroup(sb, 26, 0);
                writeFloatGroup(sb, 36, 1F);
                // 17
                //0.0
                // 27
                //0.0
                // 37
                //0.0
                writeFloatGroup(sb, 17, 0);
                writeFloatGroup(sb, 27, 0);
                writeFloatGroup(sb, 37, 0);
                // 40
                //113.173287
                writeFloatGroup(sb, 40, (float)Globals.ActiveDrawing.PaperToModel(Globals.ActiveDrawing.PaperSize.Height));
                // 41
                //1.529843
                writeFloatGroup(sb, 41, (float)(Globals.ActiveDrawing.PaperSize.Width / Globals.ActiveDrawing.PaperSize.Height));
                // 42
                //50.0
                writeFloatGroup(sb, 42, 50F);
                // 43
                //0.0
                writeFloatGroup(sb, 43, 0);
                // 44
                //0.0
                writeFloatGroup(sb, 44, 0);
                // 50
                //0.0
                writeFloatGroup(sb, 50, 0);
                // 51
                //0.0
                writeFloatGroup(sb, 51, 0);
                // 71
                //     0
                writeIntGroup(sb, 71, 0);
                // 72
                //   100
                writeIntGroup(sb, 72, 100);
                // 73
                //     1
                writeIntGroup(sb, 73, 1);
                // 74
                //     1
                writeIntGroup(sb, 74, 1);
                // 75
                //     0
                writeIntGroup(sb, 75, 0);
                // 76
                //     0
                writeIntGroup(sb, 76, 0);
                // 77
                //     0
                writeIntGroup(sb, 77, 0);
                // 78
                //     0
                writeIntGroup(sb, 78, 0);
            }

            //  0
            //ENDTAB
            writeGroup(sb, 0, "ENDTAB");
        }

        public static string FixAcName(string name)
        {
            string acname = name.ToUpper();
            acname = acname.Replace("/", "_").Replace("&", "_").Replace(" ", "_").Replace(".", "_");

            return acname;
        }

        private static void exportStyleTables(StringBuilder sb)
        {
            //  0
            //TABLE
            //  2
            //STYLE
            //  5
            //3
            //100
            //AcDbSymbolTable
            // 70
            //     2
            writeGroup(sb, 0, "TABLE");
            writeGroup(sb, 2, "STYLE");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTable");
            writeIntGroup(sb, 70, Globals.TextStyleTable.Count);

            _firstStyleHandle = _nextHandleValue;

            bool containsSTANDARD = false;

            foreach (TextStyle style in Globals.TextStyleTable.Values)
            {
                if (style.Name == "STANDARD")
                {
                    containsSTANDARD = true;
                    break;
                }
            }

            if (containsSTANDARD == false)
            {
                if (Globals.TextStyleTable.ContainsKey(Globals.TextStyleId))
                {
                    float size = (float)Globals.TextStyleTable[Globals.TextStyleId].Size;

                    writeGroup(sb, 0, "STYLE");
                    writeHandleGroup(sb, 5);
                    writeGroup(sb, 100, "AcDbSymbolTableRecord");
                    writeGroup(sb, 100, "AcDbTextStyleTableRecord");
                    writeGroup(sb, 2, "STANDARD");
                    writeIntGroup(sb, 70, 0);
                    writeFloatGroup(sb, 40, size);
                    writeFloatGroup(sb, 41, .8F);
                    writeFloatGroup(sb, 50, 0);
                    writeIntGroup(sb, 71, 0);
                    writeFloatGroup(sb, 42, size);
                    writeGroup(sb, 3, "txt");
                    writeGroup(sb, 4, "");
                }
            }

            foreach (TextStyle style in Globals.TextStyleTable.Values)
            {
                // Create STYLE table

                //  0
                //STYLE
                //  5
                //10
                //100
                //AcDbSymbolTableRecord
                //100
                //AcDbTextStyleTableRecord
                writeGroup(sb, 0, "STYLE");
                writeHandleGroup(sb, 5);
                writeGroup(sb, 100, "AcDbSymbolTableRecord");
                writeGroup(sb, 100, "AcDbTextStyleTableRecord");
                //  2
                //STANDARD
                writeGroup(sb, 2, FixAcName(style.Name));
                // 70
                //     0
                writeIntGroup(sb, 70, 0);
                // 40
                //0.0
                writeFloatGroup(sb, 40, (float)style.Size);
                // 41
                //1.0
                writeFloatGroup(sb, 41, .8F);
                // 50
                //0.0
                writeFloatGroup(sb, 50, 0);
                // 71
                //     0
                writeIntGroup(sb, 71, 0);
                // 42
                //0.2
                writeFloatGroup(sb, 42, (float)style.Size);
                //  3
                //txt
                writeGroup(sb, 3, "txt");
                //  4
                // ""
                writeGroup(sb, 4, "");
            }

            //  0
            //ENDTAB
            writeGroup(sb, 0, "ENDTAB");
        }

        private static void exportDimStyleTables(StringBuilder sb)
        {
            // Create DIMSTYLE table

            //  0
            //TABLE
            //  2
            //DIMSTYLE
            //  5
            //A
            //100
            //AcDbSymbolTable
            // 70
            //     1
            writeGroup(sb, 0, "TABLE");
            writeGroup(sb, 2, "DIMSTYLE");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTable");
            writeIntGroup(sb, 70, 1);

            {
                // Create STYLE table

                //  0
                //DIMSTYLE
                //105
                //1D
                //100
                //AcDbSymbolTableRecord
                //100
                //AcDbDimStyleTableRecord
                writeGroup(sb, 0, "DIMSTYLE");
                writeHandleGroup(sb, 105);
                writeGroup(sb, 100, "AcDbSymbolTableRecord");
                writeGroup(sb, 100, "AcDbDimStyleTableRecord");
                //  2
                //STANDARD
                writeGroup(sb, 2, "STANDARD");
                // 70
                //     0
                writeIntGroup(sb, 70, 0);
                //  3
                // ""
                writeGroup(sb, 3, "");
                //  4
                // ""
                writeGroup(sb, 4, "");
                //  5
                // ""
                writeGroup(sb, 5, "");
                //  6
                // ""
                writeGroup(sb, 6, "");
                //  7
                // ""
                writeGroup(sb, 7, "");
                // 40
                //1.0
                writeFloatGroup(sb, 40, 1F);
                // 41
                //0.18
                writeFloatGroup(sb, 41, .18F);
                // 42
                //0.0625
                writeFloatGroup(sb, 42, .0625F);
                // 43
                //0.38
                writeFloatGroup(sb, 43, .38F);
                // 44
                //0.18
                writeFloatGroup(sb, 44, .18F);
                // 45
                //0.0
                writeFloatGroup(sb, 45, 0);
                // 46
                //0.0
                writeFloatGroup(sb, 46, 0);
                // 47
                //0.0
                writeFloatGroup(sb, 47, 0);
                // 48
                //0.0
                writeFloatGroup(sb, 48, 0);
                //140
                //0.18
                writeFloatGroup(sb, 140, .18F);
                //141
                //0.09
                writeFloatGroup(sb, 141, .09F);
                //142
                //0.0
                writeFloatGroup(sb, 142, 0);
                //143
                //25.4
                writeFloatGroup(sb, 143, 25.4F);
                //144
                //1.0
                writeFloatGroup(sb, 144, 1F);
                //145
                //0.0
                writeFloatGroup(sb, 145, 0);
                //146
                //1.0
                writeFloatGroup(sb, 146, 1F);
                //147
                //0.09
                writeFloatGroup(sb, 147, .09F);
                // 71
                //     0
                writeIntGroup(sb, 71, 0);
                // 72
                //     0
                writeIntGroup(sb, 72, 0);
                // 73
                //     1
                writeIntGroup(sb, 73, 1);
                // 74
                //     1
                writeIntGroup(sb, 74, 1);
                // 75
                //     0
                writeIntGroup(sb, 75, 0);
                // 76
                //     0
                writeIntGroup(sb, 76, 0);
                // 77
                //     0
                writeIntGroup(sb, 77, 0);
                // 78
                //     0
                writeIntGroup(sb, 78, 0);
                //170
                //     0
                writeIntGroup(sb, 170, 0);
                //171
                //     2
                writeIntGroup(sb, 171, 2);
                //172
                //     0
                writeIntGroup(sb, 172, 0);
                //173
                //     0
                writeIntGroup(sb, 173, 0);
                //174
                //     0
                writeIntGroup(sb, 174, 0);
                //175
                //     0
                writeIntGroup(sb, 175, 0);
                //176
                //     0
                writeIntGroup(sb, 176, 0);
                //177
                //     0
                writeIntGroup(sb, 177, 0);
                //178
                //     0
                writeIntGroup(sb, 178, 0);
                //270
                //     2
                writeIntGroup(sb, 270, 2);
                //271
                //     4
                writeIntGroup(sb, 271, 4);
                //272
                //     4
                writeIntGroup(sb, 272, 2);
                //273
                //     2
                writeIntGroup(sb, 273, 2);
                //274
                //     2
                writeIntGroup(sb, 274, 2);
                //340
                //10
                writeGroup(sb, 340, string.Format("{0:X}", _firstStyleHandle));
                //275
                //     0
                writeIntGroup(sb, 275, 0);
                //280
                //     0
                writeIntGroup(sb, 280, 0);
                //281
                //     0
                writeIntGroup(sb, 281, 0);
                //282
                //     0
                writeIntGroup(sb, 282, 0);
                //283
                //     1
                writeIntGroup(sb, 283, 1);
                //284
                //     0
                writeIntGroup(sb, 284, 0);
                //285
                //     0
                writeIntGroup(sb, 285, 0);
                //286
                //     0
                writeIntGroup(sb, 286, 0);
                //287
                //     3
                writeIntGroup(sb, 287, 3);
                //288
                //     0
                writeIntGroup(sb, 288, 0);
            }

            //  0
            //ENDTAB
            writeGroup(sb, 0, "ENDTAB");
        }

        private static void exportLayerTables(StringBuilder sb)
        {
            //  0
            //TABLE
            //  2
            //LAYER
            //  5
            //2
            //100
            //AcDbSymbolTable
            // 70
            //     2
            writeGroup(sb, 0, "TABLE");
            writeGroup(sb, 2, "LAYER");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTable");
            writeIntGroup(sb, 70, Globals.LayerTable.Count);

            bool layer0exists = false;

            foreach (Layer layer in Globals.LayerTable.Values)
            {
                if (layer.Name == "0")
                {
                    layer0exists = true;
                    break;
                }
            }

            if (layer0exists == false)
            {
                writeGroup(sb, 0, "LAYER");
                writeHandleGroup(sb, 5);
                writeGroup(sb, 100, "AcDbSymbolTableRecord");
                writeGroup(sb, 100, "AcDbLayerTableRecord");
                writeGroup(sb, 2, "0");
                writeIntGroup(sb, 70, 0);
                writeIntGroup(sb, 62, 0);
                writeGroup(sb, 6, "CONTINUOUS");
            }

            foreach (Layer layer in Globals.LayerTable.Values)
            {
                // Create LAYER table
                //  0
                //LAYER
                //  5
                //F
                //100
                //AcDbSymbolTableRecord
                //100
                //AcDbLayerTableRecord
                writeGroup(sb, 0, "LAYER");
                writeHandleGroup(sb, 5);
                writeGroup(sb, 100, "AcDbSymbolTableRecord");
                writeGroup(sb, 100, "AcDbLayerTableRecord");

                //  2
                //0
                writeGroup(sb, 2, FixAcName(layer.Name));

                // 70
                //     0
                writeIntGroup(sb, 70, layer.Visible ? 0 : 1);

                // 62
                //     7
                int color = AcColorFromColorSpec(layer.ColorSpec);
                writeIntGroup(sb, 62, color);

                //  6
                //CONTINUOUS
                string ltype = Globals.LineTypeTable.ContainsKey(layer.LineTypeId) ? Globals.LineTypeTable[layer.LineTypeId].Name : "CONTINUOUS";
                writeGroup(sb, 6, FixAcName(ltype));
            }

            //  0
            //ENDTAB
            writeGroup(sb, 0, "ENDTAB");
        }

        private static int AcColorFromPrimitive(Primitive p)
        {
            uint colorSpec = p.ColorSpec;

            //if (p.ColorSpec == (uint)ColorCode.ByLayer)
            //{
            //    Layer layer = Globals.LayerTable[p.LayerId];
            //    colorSpec = layer.ColorSpec;
            //}

            return AcColorFromColorSpec(colorSpec);
        }

        private static int AcColorFromColorSpec(uint colorSpec)
        {
            int color = 0;

            if (colorSpec == (uint)ColorCode.ThemeForeground)
            {
                colorSpec = Utilities.ColorSpecFromColor(Globals.Theme.ForegroundColor);
            }

            if (colorSpec == (uint)ColorCode.ByLayer)
            {
                color = 256;
            }
            else if (AcadColorTable.ContainsKey(colorSpec))
            {
                color = AcadColorTable[colorSpec];
            }
            else
            {
                Color c = Utilities.ColorFromColorSpec(colorSpec);
                int diff = 3 * 255;
                int match = 0;

                foreach (uint cs in AcadColorTable.Keys)
                {
                    Color test = Utilities.ColorFromColorSpec(cs);

                    int dr = c.R > test.R ? c.R - test.R : test.R - c.R;
                    int dg = c.G > test.G ? c.G - test.G : test.G - c.G;
                    int db = c.B > test.B ? c.B - test.B : test.B - c.B;

                    int d = dr + dg + db;
                    if (d < diff)
                    {
                        match = AcadColorTable[cs];
                        diff = d;
                    }
                }

                color = match;
                AcadColorTable.Add(colorSpec, match);
            }

            return color;
        }

        private static void exportLtypeTables(StringBuilder sb)
        {
            //  0
            //TABLE
            //  2
            //LTYPE
            //  5
            //5
            //100
            //AcDbSymbolTable
            // 70
            //     1
            writeGroup(sb, 0, "TABLE");
            writeGroup(sb, 2, "LTYPE");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTable");
            writeIntGroup(sb, 70, Globals.LineTypeTable.Count);

            bool haveBYBLOCK = false;
            bool haveBYLAYER = false;
            bool haveCONTINUOUS = false;

            foreach (LineType type in Globals.LineTypeTable.Values)
            {
                // Create LTYPE table
                haveBYBLOCK = haveBYBLOCK || type.Name == "BYBLOCK";
                haveBYLAYER = haveBYLAYER || type.Name == "BYLAYER";
                haveCONTINUOUS = haveCONTINUOUS || type.Name == "CONTINUOUS";
            }

            if (haveBYBLOCK == false)
            {
                writeSolidLTYPE(sb, "BYBLOCK");
            }

            if (haveBYLAYER == false)
            {
                writeSolidLTYPE(sb, "BYLAYER");
            }

            if (haveCONTINUOUS == false)
            {
                writeSolidLTYPE(sb, "CONTINUOUS");
            }
                
            foreach (LineType type in Globals.LineTypeTable.Values)
            {
                // Create LTYPE table

                //  0
                //LTYPE
                //  5
                //16
                //100
                //AcDbSymbolTableRecord
                //100
                //AcDbLinetypeTableRecord
                writeGroup(sb, 0, "LTYPE");
                writeHandleGroup(sb, 5);
                writeGroup(sb, 100, "AcDbSymbolTableRecord");
                writeGroup(sb, 100, "AcDbLinetypeTableRecord");

                //  2
                //CONTINUOUS
                writeGroup(sb, 2, FixAcName(type.Name));
                // 70
                //     0
                writeIntGroup(sb, 70, 0);
                //  3
                //Solid line
                writeGroup(sb, 3, type.Name);
                // 72
                //    65
                writeIntGroup(sb, 72, 65);      // Always 65

                if (type.StrokeDashArray == null || type.StrokeDashArray.Count == 0)
                {
                    // 73
                    //     0
                    writeIntGroup(sb, 73, 0);
                    // 40
                    //0.0
                    writeFloatGroup(sb, 40, 0);
                }
                else
                {
                    // 73
                    //     4
                    writeIntGroup(sb, 73, type.StrokeDashArray.Count);

                    double length = 0;

                    foreach (double d in type.StrokeDashArray)
                    {
                        length += d;
                    }

                    // 40
                    //2.0
                    writeFloatGroup(sb, 40, (float)length);

                    for (int i = 0; i < type.StrokeDashArray.Count; i += 2)
                    {
                        // 49
                        //1.25
                        // 74
                        //     0
                        writeFloatGroup(sb, 49, (float)type.StrokeDashArray[i]);
                        writeIntGroup(sb, 74, 0);

                        // 49
                        //-0.25
                        // 74
                        //     0
                        // 49
                        writeFloatGroup(sb, 49, -(float)type.StrokeDashArray[i + 1]);
                        writeIntGroup(sb, 74, 0);
                    }
                }
            }

            //  0
            //ENDTAB
            writeGroup(sb, 0, "ENDTAB");
        }

        private static void writeSolidLTYPE(StringBuilder sb, string name)
        {
            writeGroup(sb, 0, "LTYPE");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbSymbolTableRecord");
            writeGroup(sb, 100, "AcDbLinetypeTableRecord");
            writeGroup(sb, 2, FixAcName(name));
            writeIntGroup(sb, 70, 0);
            writeGroup(sb, 3, name);
            writeIntGroup(sb, 72, 65);      // Always 65
            writeIntGroup(sb, 73, 0);
            writeFloatGroup(sb, 40, 0);
        }

        private static void exportBlocks(StringBuilder sb, DrawingDocument dc)
        {
            writeGroup(sb, 0, "SECTION");
            writeGroup(sb, 2, "BLOCKS");

            //  0
            //BLOCK
            //  5
            //1B
            //100
            //AcDbEntity
            //  8
            //0
            //100
            //AcDbBlockBegin
            writeGroup(sb, 0, "BLOCK");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbEntity");
            writeGroup(sb, 8, "0");
            writeGroup(sb, 100, "AcDbBlockBegin");
            //  2
            //*MODEL_SPACE
            writeGroup(sb, 2, "*MODEL_SPACE");
            // 70
            //     0
            writeIntGroup(sb, 70, 0);
            // 10
            //0.0
            // 20
            //0.0
            // 30
            //0.0
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 0);
            writeFloatGroup(sb, 30, 0);
            //  3
            //*MODEL_SPACE
            writeGroup(sb, 3, "*MODEL_SPACE");
            //  1
            //""
            writeGroup(sb, 1, "");
            //  0
            //ENDBLK
            writeGroup(sb, 0, "ENDBLK");
            //  5
            //1C
            writeHandleGroup(sb, 5);
            //100
            //AcDbEntity
            writeGroup(sb, 100, "AcDbEntity");
            //  8
            //0
            writeGroup(sb, 8, "0");
            //100
            //AcDbBlockEnd
            writeGroup(sb, 100, "AcDbBlockEnd");

            //  0
            //BLOCK
            //  5
            //18
            //100
            //AcDbEntity
            writeGroup(sb, 0, "BLOCK");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbEntity");
            // 67
            //     1
            writeIntGroup(sb, 67, 1);
            //  8
            //0
            //100
            //AcDbBlockBegin
            writeGroup(sb, 8, "0");
            writeGroup(sb, 100, "AcDbBlockBegin");
            //  2
            //*PAPER_SPACE
            writeGroup(sb, 2, "*PAPER_SPACE");
            // 70
            //     0
            writeIntGroup(sb, 70, 0);
            // 10
            //0.0
            // 20
            //0.0
            // 30
            //0.0
            writeFloatGroup(sb, 10, 0);
            writeFloatGroup(sb, 20, 0);
            writeFloatGroup(sb, 30, 0);
            //  3
            //*PAPER_SPACE
            writeGroup(sb, 3, "*PAPER_SPACE");
            //  1
            //""
            writeGroup(sb, 1, "");
            //  0
            //ENDBLK
            writeGroup(sb, 0, "ENDBLK");
            //  5
            //19
            writeHandleGroup(sb, 5);
            //100
            //AcDbEntity
            writeGroup(sb, 100, "AcDbEntity");
            // 67
            //     1
            writeIntGroup(sb, 67, 1);
            //  8
            //0
            writeGroup(sb, 8, "0");
            //100
            //AcDbBlockEnd
            writeGroup(sb, 100, "AcDbBlockEnd");

            writeGroup(sb, 0, "ENDSEC");
        }

        private static void exportEntities(StringBuilder sb, DrawingDocument dc, bool showFrame)
        {
            writeGroup(sb, 0, "SECTION");
            writeGroup(sb, 2, "ENTITIES");

            List<Primitive> primitives = new List<Primitive>();

            foreach (Primitive p in dc.PrimitiveList)
            {
                if (Globals.LayerTable[p.LayerId].Visible)
                {
                    primitives.Add(p);
                }
            }

            primitives.Sort();

            VectorContext context = new VectorContext(true, true, true);

            Point ll = Globals.ActiveDrawing.PaperToModel(new Point(0, 0));
            Point ur = Globals.ActiveDrawing.PaperToModel(new Point(Globals.ActiveDrawing.PaperSize.Width, Globals.ActiveDrawing.PaperSize.Height));

            _extents.Union(ll);
            _extents.Union(ur);

            if (showFrame)
            {
                // show the paper frame
                writeGroup(sb, 0, "LWPOLYLINE");
                writeHandleGroup(sb, 5);
                writeGroup(sb, 100, "AcDbEntity");
                writeGroup(sb, 8, "1");
                writeIntGroup(sb, 62, 0);
                writeGroup(sb, 100, "AcDbPolyline");
                writeIntGroup(sb, 90, 5);

                writeIntGroup(sb, 70, 0);
                writeFloatGroup(sb, 43, 0F);

                writeFloatGroup(sb, 10, (float)ll.X);
                writeFloatGroup(sb, 20, (float)ll.Y);

                writeFloatGroup(sb, 10, (float)ur.X);
                writeFloatGroup(sb, 20, (float)ll.Y);

                writeFloatGroup(sb, 10, (float)ur.X);
                writeFloatGroup(sb, 20, (float)ur.Y);

                writeFloatGroup(sb, 10, (float)ll.X);
                writeFloatGroup(sb, 20, (float)ur.Y);

                writeFloatGroup(sb, 10, (float)ll.X);
                writeFloatGroup(sb, 20, (float)ll.Y);
            }

            foreach (Primitive p in primitives)
            {
                Layer layer = Globals.LayerTable[p.LayerId];
                //int acColor = AcColorFromPrimitive(p);

                _extents.Union(p.Box);

                switch (p.TypeName)
                {
                    //case PrimitiveType.Line:
                    //    exportPLine(sb, p as PLine);
                    //    break;

                    case PrimitiveType.Ellipse:
                        exportPEllipse(sb, p as PEllipse);
                        break;

                    default:
                        VectorEntity ve = p.Vectorize(context);
                        //exportVectorEntity(sb, ve, FixAcName(layer.Name), acColor);
                        exportVectorEntity(sb, ve, FixAcName(layer.Name));
                        break;
                }
            }

            if (_extents.IsEmpty)
            {
                _extents = new Rect(0, 0, 0, 0);
            }

            writeGroup(sb, 0, "ENDSEC");
        }

        private static void exportObjects(StringBuilder sb, DrawingDocument dc)
        {
            //  2
            //OBJECTS
            writeGroup(sb, 0, "SECTION");
            writeGroup(sb, 2, "OBJECTS");
            //  0
            //DICTIONARY
            //  5
            //C
            //100
            //AcDbDictionary
            //  3
            //ACAD_GROUP
            //350
            //D
            //  3
            //ACAD_MLINESTYLE
            //350
            //E
            writeGroup(sb, 0, "DICTIONARY");
            writeGroup(sb, 5, "C");                     // Hard-coded handle value
            writeGroup(sb, 100, "AcDbDictionary");
            writeGroup(sb, 3, "ACAD_GROUP");
            writeGroup(sb, 350, "D");                   // Hard-coded handle value
            writeGroup(sb, 3, "ACAD_MLINESTYLE");
            writeGroup(sb, 350, "E");                   // Hard-coded handle value
            //  0
            //DICTIONARY
            //  5
            //D
            //102
            //{ACAD_REACTORS
            //330
            //C
            //102
            //}
            //100
            //AcDbDictionary
            writeGroup(sb, 0, "DICTIONARY");
            writeGroup(sb, 5, "D");                     // Hard-coded handle value
            writeGroup(sb, 102, "{ACAD_REACTORS");
            writeGroup(sb, 330, "C");                   // Hard-coded handle value
            writeGroup(sb, 102, "}");
            writeGroup(sb, 100, "AcDbDictionary");
            //  0
            //DICTIONARY
            //  5
            //E
            //102
            //{ACAD_REACTORS
            //330
            //C
            //102
            //}
            //100
            //AcDbDictionary
            //  3
            //STANDARD
            //350
            //13
            writeGroup(sb, 0, "DICTIONARY");
            writeGroup(sb, 5, "E");                     // Hard-coded handle value
            writeGroup(sb, 102, "{ACAD_REACTORS");
            writeGroup(sb, 330, "C");                   // Hard-coded handle value
            writeGroup(sb, 102, "}");
            writeGroup(sb, 100, "AcDbDictionary");
            writeGroup(sb, 3, "STANDARD");
            writeGroup(sb, 350, "13");                  // Hard-coded handle value
            //  0
            //MLINESTYLE
            //  5
            //13
            //102
            //{ACAD_REACTORS
            //330
            //E
            //102
            //}
            //100
            //AcDbMlineStyle
            //  2
            //STANDARD
            // 70
            //     0
            //  3
            //""
            // 62
            //     0
            // 51
            //90.0
            // 52
            //90.0
            // 71
            //     2
            // 49
            //0.5
            // 62
            //   256
            //  6
            //BYLAYER
            // 49
            //-0.5
            // 62
            //   256
            //  6
            //BYLAYER
            writeGroup(sb, 0, "MLINESTYLE");
            writeGroup(sb, 5, "13");                    // Hard-coded handle value
            writeGroup(sb, 102, "{ACAD_REACTORS");
            writeGroup(sb, 330, "E");                   // Hard-coded handle value
            writeGroup(sb, 102, "}");
            writeGroup(sb, 100, "AcDbMlineStyle");
            writeGroup(sb, 2, "STANDARD");
            writeIntGroup(sb, 70, 0);
            writeGroup(sb, 3, "");
            writeIntGroup(sb, 62, 0);
            writeFloatGroup(sb, 51, 90F);
            writeIntGroup(sb, 71, 2);
            writeFloatGroup(sb, 49, .5F);
            writeIntGroup(sb, 62, 256);
            writeGroup(sb, 6, "BYLAYER");
            writeFloatGroup(sb, 49, -.5F);
            writeIntGroup(sb, 62, 256);
            writeGroup(sb, 6, "BYLAYER");
            //  0
            //ENDSEC
            writeGroup(sb, 0, "ENDSEC");
        }

        private static void exportPEllipse(StringBuilder sb, PEllipse p)
        {
            Layer layer = Globals.LayerTable[p.LayerId];
            int acColor = AcColorFromPrimitive(p);

            Point mOrigin = Globals.ActiveDrawing.PaperToModel(p.Origin);

            //	Endpoint of major axis, relative to the center (in WCS)
            Point ma = Construct.PolarOffset(new Point(), Globals.ActiveDrawing.PaperToModel(p.Major), -p.AxisAngle);

            //  0
            //ELLIPSE
            //  5
            //1605
            //330
            //1602
            //100
            //AcDbEntity
            writeGroup(sb, 0, "ELLIPSE");
            writeHandleGroup(sb, 5);
            writeGroup(sb, 100, "AcDbEntity");
            //  8
            //0
            writeGroup(sb, 8, FixAcName(layer.Name));
            writeIntGroup(sb, 62, acColor);
            //100
            //AcDbEllipse
            writeGroup(sb, 100, "AcDbEllipse");
            // 10
            //74.34613347704726
            // 20
            //408.8868390158023
            // 30
            //0.0
            writeFloatGroup(sb, 10, (float)mOrigin.X);
            writeFloatGroup(sb, 20, (float)mOrigin.Y);
            writeFloatGroup(sb, 30, 0);
            // 11
            //- 21.43529620840515
            // 21
            //- 21.43529620840515
            // 31
            //0.0
            writeFloatGroup(sb, 11, (float)ma.X);
            writeFloatGroup(sb, 21, (float)ma.Y);
            writeFloatGroup(sb, 31, 0);
            //210
            //0.0
            //220
            //0.0
            //230
            //1.0
            // 40 - Ratio of minor axis to major axis
            //0.9999999999995691
            writeFloatGroup(sb, 40, (float)(p.Minor / p.Major));

            // 41 - Start parameter (this value is 0.0 for a full ellipse)
            //- 0.6219259426634751
            // 42 - End parameter (this value is 2pi for a full ellipse)
            //0.775969248128376

            if (p.IncludedAngle == 0)
            {
                writeFloatGroup(sb, 41, 0F);
                writeFloatGroup(sb, 42, (float)(Math.PI * 2));
            }
            else
            {
                double endAngle = p.StartAngle + p.IncludedAngle;

                if (p.IncludedAngle < 0)
                {
                    writeFloatGroup(sb, 41, (float)-p.StartAngle);
                    writeFloatGroup(sb, 42, (float)-endAngle);
                }
                else
                {
                    writeFloatGroup(sb, 41, (float)-endAngle);
                    writeFloatGroup(sb, 42, (float)-p.StartAngle);
                }
            }
        }

        private static void exportPLine(StringBuilder sb, PLine p)
        {
            Layer layer = Globals.LayerTable[p.LayerId];
            int acColor = AcColorFromPrimitive(p);

            Point mOrigin = Globals.ActiveDrawing.PaperToModel(p.Origin);

            if (p.Points.Count == 1)
            {
                Point to = new Point(p.Points[0].X + p.Origin.X, p.Points[0].Y + p.Origin.Y);
                Point mTo = Globals.ActiveDrawing.PaperToModel(to);

                //  0
                //LINE
                //  5
                //54
                //100
                //AcDbEntity
                writeGroup(sb, 0, "LINE");
                writeHandleGroup(sb, 5);
                writeGroup(sb, 100, "AcDbEntity");
                //  8
                //0
                writeGroup(sb, 8, FixAcName(layer.Name));
                // 62
                //     0
                writeIntGroup(sb, 62, acColor);
                //100
                //AcDbLine
                writeGroup(sb, 100, "AcDbLine");
                // 10
                //7.908888
                // 20
                //7.908888
                // 30
                //0.0
                writeFloatGroup(sb, 10, (float)mOrigin.X);
                writeFloatGroup(sb, 20, (float)mOrigin.Y);
                writeFloatGroup(sb, 30, 0);
                // 11
                //8.158888
                // 21
                //8.158888
                // 31
                //0.0
                writeFloatGroup(sb, 11, (float)mTo.X);
                writeFloatGroup(sb, 21, (float)mTo.Y);
                writeFloatGroup(sb, 31, 0);
            }
            else
            {
                Point mFrom = Globals.ActiveDrawing.PaperToModel(p.Origin);

                foreach (Point pt in p.Points)
                {
                    Point to = new Point(pt.X + p.Origin.X, pt.Y + p.Origin.Y);
                    Point mTo = Globals.ActiveDrawing.PaperToModel(to);

                    writeGroup(sb, 0, "LINE");
                    writeHandleGroup(sb, 5);
                    writeGroup(sb, 100, "AcDbEntity");
                    writeGroup(sb, 8, FixAcName(layer.Name));
                    writeIntGroup(sb, 62, acColor);
                    writeGroup(sb, 100, "AcDbLine");
                    writeFloatGroup(sb, 10, (float)mFrom.X);
                    writeFloatGroup(sb, 20, (float)mFrom.Y);
                    writeFloatGroup(sb, 30, 0);
                    writeFloatGroup(sb, 11, (float)mTo.X);
                    writeFloatGroup(sb, 21, (float)mTo.Y);
                    writeFloatGroup(sb, 31, 0);

                    mFrom = mTo;
                }
            }
        }

        private static void exportVectorEntity(StringBuilder sb, VectorEntity ve, string acLayer)
        {
            if (ve.Children != null)
            {
                int acColor = AcColorFromColorSpec(Utilities.ColorSpecFromColor(ve.Color));

                foreach (object o in ve.Children)
                {
                    if (o is List<Point>)
                    {
                        List<Point> pc = o as List<Point>;

                        if (pc.Count == 2)
                        {
                            Point mFrom = Globals.ActiveDrawing.PaperToModel(pc[0]);
                            Point mTo = Globals.ActiveDrawing.PaperToModel(pc[1]);

                            writeGroup(sb, 0, "LINE");
                            writeHandleGroup(sb, 5);
                            writeGroup(sb, 100, "AcDbEntity");
                            writeGroup(sb, 8, acLayer);
                            writeIntGroup(sb, 62, acColor);
                            writeGroup(sb, 100, "AcDbLine");
                            writeFloatGroup(sb, 10, (float)mFrom.X);
                            writeFloatGroup(sb, 20, (float)mFrom.Y);
                            writeFloatGroup(sb, 30, 0);
                            writeFloatGroup(sb, 11, (float)mTo.X);
                            writeFloatGroup(sb, 21, (float)mTo.Y);
                            writeFloatGroup(sb, 31, 0);
                        }
                        else if (pc.Count > 2)
                        {
                            int fillColor = acColor;

                            if (ve.Fill)
                            {
                                fillColor = AcColorFromColorSpec(Utilities.ColorSpecFromColor(ve.FillColor));

                                if (pc[0].X != pc[pc.Count - 1].X || pc[0].Y != pc[pc.Count - 1].Y)
                                {
                                    pc.Add(pc[0]);
                                }

                                if (pc.Count <= 5)
                                {
                                    writeGroup(sb, 0, "SOLID");
                                    writeHandleGroup(sb, 5);
                                    writeGroup(sb, 100, "AcDbEntity");
                                    writeGroup(sb, 8, acLayer);
                                    writeIntGroup(sb, 62, fillColor);
                                    writeGroup(sb, 100, "AcDbTrace");

                                    Point p0 = Globals.ActiveDrawing.PaperToModel(pc[0]);
                                    Point p1 = Globals.ActiveDrawing.PaperToModel(pc[1]);
                                    Point p2 = Globals.ActiveDrawing.PaperToModel(pc[2]);
                                    Point p3 = pc.Count > 3 ? Globals.ActiveDrawing.PaperToModel(pc[3]) : p2;

                                    writeFloatGroup(sb, 10, (float)p0.X);
                                    writeFloatGroup(sb, 20, (float)p0.Y);
                                    writeFloatGroup(sb, 30, 0);

                                    writeFloatGroup(sb, 11, (float)p1.X);
                                    writeFloatGroup(sb, 21, (float)p1.Y);
                                    writeFloatGroup(sb, 31, 0);

                                    writeFloatGroup(sb, 12, (float)p3.X);
                                    writeFloatGroup(sb, 22, (float)p3.Y);
                                    writeFloatGroup(sb, 32, 0);

                                    writeFloatGroup(sb, 13, (float)p2.X);
                                    writeFloatGroup(sb, 23, (float)p2.Y);
                                    writeFloatGroup(sb, 33, 0);
                                }
                                else
                                {
                                    List<Point[]> triangles = _tesselator.TesselatePolygon(pc, ve.FillEvenOdd);

                                    foreach (var t in triangles)
                                    {
                                        writeGroup(sb, 0, "SOLID");
                                        writeHandleGroup(sb, 5);
                                        writeGroup(sb, 100, "AcDbEntity");
                                        writeGroup(sb, 8, acLayer);
                                        writeIntGroup(sb, 62, fillColor);
                                        writeGroup(sb, 100, "AcDbTrace");

                                        Point p0 = Globals.ActiveDrawing.PaperToModel(t[0]);
                                        Point p1 = Globals.ActiveDrawing.PaperToModel(t[1]);
                                        Point p2 = Globals.ActiveDrawing.PaperToModel(t[2]);
                                        Point p3 = p2;

                                        writeFloatGroup(sb, 10, (float)p0.X);
                                        writeFloatGroup(sb, 20, (float)p0.Y);
                                        writeFloatGroup(sb, 30, 0);

                                        writeFloatGroup(sb, 11, (float)p1.X);
                                        writeFloatGroup(sb, 21, (float)p1.Y);
                                        writeFloatGroup(sb, 31, 0);

                                        writeFloatGroup(sb, 12, (float)p3.X);
                                        writeFloatGroup(sb, 22, (float)p3.Y);
                                        writeFloatGroup(sb, 32, 0);

                                        writeFloatGroup(sb, 13, (float)p2.X);
                                        writeFloatGroup(sb, 23, (float)p2.Y);
                                        writeFloatGroup(sb, 33, 0);
                                    }
                                }
                            }

                            if (ve.Fill == false || acColor != fillColor)
                            {
#if true
                                // Use LWPOLYLINE for multisegment lines
                                writeGroup(sb, 0, "LWPOLYLINE");
                                writeHandleGroup(sb, 5);
                                writeGroup(sb, 100, "AcDbEntity");
                                writeGroup(sb, 8, acLayer);
                                writeIntGroup(sb, 62, acColor);
                                writeGroup(sb, 100, "AcDbPolyline");
                                writeIntGroup(sb, 90, pc.Count);

                                writeIntGroup(sb, 70, 0);
                                writeFloatGroup(sb, 43, 0F);

                                for (int i = 0; i < pc.Count; i++)
                                {
                                    Point mTo = Globals.ActiveDrawing.PaperToModel(pc[i]);
                                    writeFloatGroup(sb, 10, (float)mTo.X);
                                    writeFloatGroup(sb, 20, (float)mTo.Y);
                                }
#else
                                // Use LINEs for multi-segment lines
                                Point mFrom = Globals.ActiveDrawing.PaperToModel(pc[0]);

                                for (int i = 1; i < pc.Count; i++)
                                {
                                    Point mTo = Globals.ActiveDrawing.PaperToModel(pc[i]);

                                    writeGroup(sb, 0, "LINE");
                                    writeHandleGroup(sb, 5);
                                    writeGroup(sb, 100, "AcDbEntity");
                                    writeGroup(sb, 8, acLayer);
                                    writeIntGroup(sb, 62, acColor);
                                    writeGroup(sb, 100, "AcDbLine");
                                    writeFloatGroup(sb, 10, (float)mFrom.X);
                                    writeFloatGroup(sb, 20, (float)mFrom.Y);
                                    writeFloatGroup(sb, 30, 0);
                                    writeFloatGroup(sb, 11, (float)mTo.X);
                                    writeFloatGroup(sb, 21, (float)mTo.Y);
                                    writeFloatGroup(sb, 31, 0);

                                    mFrom = mTo;
                                }
#endif
                            }
                        }
                    }
                    else if (o is VectorEntity)
                    {
                        //exportVectorEntity(sb, o as VectorEntity, acLayer, acColor);
                        exportVectorEntity(sb, o as VectorEntity, acLayer);
                    }
                    else if (o is VectorMarkerEntity)
                    {
                        // ignore
                    }
                    else if (o is VectorArcEntity)
                    {
                        VectorArcEntity va = o as VectorArcEntity;

                        //  0
                        //ARC
                        //  5
                        //166
                        //100
                        //AcDbEntity
                        //  8
                        //0
                        //  6
                        //BYBLOCK
                        // 62
                        //     3
                        //100
                        //AcDbCircle
                        // 10
                        //7.959128
                        // 20
                        //3.436889
                        // 30
                        //0.0
                        // 40
                        //3.739497
                        //100
                        //AcDbArc
                        // 50
                        //2.758188
                        // 51
                        //5.495188
                        Point mCenter = Globals.ActiveDrawing.PaperToModel(va.Center);

                        //double endAngle = va.StartAngle < 0 ? -va.StartAngle - va.IncludedAngle : va.StartAngle + va.IncludedAngle;
                        double endAngle = va.StartAngle + va.IncludedAngle;

                        writeGroup(sb, 0, "ARC");
                        writeHandleGroup(sb, 5);
                        writeGroup(sb, 100, "AcDbEntity");
                        writeGroup(sb, 8, acLayer);
                        writeIntGroup(sb, 62, acColor);
                        writeGroup(sb, 100, "AcDbCircle");
                        writeFloatGroup(sb, 10, (float)mCenter.X);
                        writeFloatGroup(sb, 20, (float)mCenter.Y);
                        writeFloatGroup(sb, 30, 0);
                        writeFloatGroup(sb, 40, (float)Globals.ActiveDrawing.PaperToModel(va.Radius));

                        writeGroup(sb, 100, "AcDbArc");

                        if (va.IncludedAngle < 0)
                        {
                            writeFloatGroup(sb, 50, (float)(-va.StartAngle * Construct.cRadiansToDegrees));
                            writeFloatGroup(sb, 51, (float)(-endAngle * Construct.cRadiansToDegrees));
                        }
                        else
                        {
                            writeFloatGroup(sb, 50, (float)(-endAngle * Construct.cRadiansToDegrees));
                            writeFloatGroup(sb, 51, (float)(-va.StartAngle * Construct.cRadiansToDegrees));
                        }
                    }
                    else if (o is VectorTextEntity)
                    {
                        VectorTextEntity vt = o as VectorTextEntity;

                        string[] lines = vt.Text.Split(new[] { '\n' });

                        double tx = vt.Location.X;
                        double ty = vt.Location.Y;
                        double th = Globals.ActiveDrawing.PaperToModel(vt.TextHeight);
                        //double lh = th * vt.LineSpacing;
                        double lh = vt.TextHeight * vt.LineSpacing;

                        //Horizontal text justification type (optional, default = 0) integer codes (not bit-coded)
                        //0 = Left; 1= Center; 2 = Right
                        //3 = Aligned (if vertical alignment = 0)
                        //4 = Middle (if vertical alignment = 0)
                        //5 = Fit (if vertical alignment = 0)
                        //See the Group 72 and 73 integer codes table for clarification.
 
                        int group72 = 0;  // Left

                        if (vt.TextAlignment == TextAlignment.Center)
                        {
                            group72 = 1;
                        }
                        else if (vt.TextAlignment == TextAlignment.Right)
                        {
                            group72 = 2;
                        }

                        if (vt.TextPosition == TextPosition.Above)
                        {
                            if (vt.Angle == 0)
                            {
                                ty -= (lines.Length - 1 + .25) * lh;
                            }
                            else
                            {
                                Point td = Construct.PolarOffset(new Point(0, 0), (lines.Length - 1 + .25) * lh, (vt.Angle + 90) / Construct.cRadiansToDegrees);
                                tx -= td.X;
                                ty -= td.Y;
                            }
                        }
                        else if (vt.TextPosition == TextPosition.On)
                        {
                            if (vt.Angle == 0)
                            {
                                ty -= (lines.Length - 1 + .25) * lh / 2;
                            }
                            else
                            {
                                Point td = Construct.PolarOffset(new Point(0, 0), (lines.Length - 1 + .25) * lh / 2, (vt.Angle + 90) / Construct.cRadiansToDegrees);
                                tx -= td.X;
                                ty -= td.Y;
                            }
                        }
                        else
                        {
                            if (vt.Angle == 0)
                            {
                                ty -= .25 * lh;
                            }
                            else
                            {
                                Point td = Construct.PolarOffset(new Point(0, 0), .25 * lh, (vt.Angle + 90) / Construct.cRadiansToDegrees);
                                tx -= td.X;
                                ty -= td.Y;
                            }
                        }

                        if (false && lines.Length > 1)
                        {
                            foreach (string s in lines)
                            {
                                ty += lh;
                            }
                        }
                        else
                        {
                            Point mOrigin = Globals.ActiveDrawing.PaperToModel(new Point(tx, ty));
                            //float mTxHeight = (float)Globals.ActiveDrawing.PaperToModel(vt.TextHeight);
                            float mLineHeight = (float)Globals.ActiveDrawing.PaperToModel(lh);

                            Point ts = Construct.PolarOffset(new Point(0, 0), -mLineHeight, -(vt.Angle - 90) / Construct.cRadiansToDegrees);
                            //Point ts = Construct.PolarOffset(new Point(0, 0), -lh, -(vt.Angle - 90) / Construct.cRadiansToDegrees);

                            // TEXT
                            foreach (string s in lines)
                            {
                                string text = s.Replace("°", "%%d");

                                //  0
                                //TEXT
                                //  5
                                //A4
                                writeGroup(sb, 0, "TEXT");
                                writeHandleGroup(sb, 5);
                                //100
                                //AcDbEntity
                                //  8
                                //0
                                //  6
                                //BYBLOCK
                                // 62
                                //     4
                                //100
                                writeGroup(sb, 100, "AcDbEntity");
                                writeGroup(sb, 8, acLayer);
                                writeIntGroup(sb, 62, acColor);
                                //AcDbText
                                writeGroup(sb, 100, "AcDbText");
                                // 10
                                //6.710617
                                // 20
                                //10.683942
                                // 30
                                //0.0
                                writeFloatGroup(sb, 10, (float)mOrigin.X);
                                writeFloatGroup(sb, 20, (float)mOrigin.Y);
                                writeFloatGroup(sb, 30, 0);
                                // 40
                                //0.14
                                writeFloatGroup(sb, 40, (float)th);
                                //  1
                                //3.437
                                writeGroup(sb, 1, text);
                                if (vt.Angle != 0)
                                {
                                    writeFloatGroup(sb, 50, (float)-vt.Angle);
                                }
                                writeFloatGroup(sb, 41, (float)(vt.CharacterSpacing * .8));
                                //  7
                                //TEDS
                                writeGroup(sb, 7, "STANDARD");
                                // 72
                                //     1
                                writeIntGroup(sb, 72, group72);
                                // 11
                                //7.010617
                                // 21
                                //10.753942
                                // 31
                                //0.0
                                writeFloatGroup(sb, 11, (float)mOrigin.X);
                                writeFloatGroup(sb, 21, (float)mOrigin.Y);
                                writeFloatGroup(sb, 31, 0);
                                //100
                                //AcDbText
                                // 73
                                //     2
                                writeGroup(sb, 100, "AcDbText");
                                writeIntGroup(sb, 73, 2);

                                mOrigin.X += ts.X;
                                mOrigin.Y += ts.Y;
                            }
                        }
                    }
                }
            }
        }

        private static void writeGroup(StringBuilder sb, int group, string data)
        {
            sb.AppendLine(string.Format("{0,3}", group));
            sb.AppendLine(data);
        }

        private static void writeIntGroup(StringBuilder sb, int group, int value)
        {
            sb.AppendLine(string.Format("{0,3}", group));
            sb.AppendLine(string.Format("{0,6}", value));
        }

        private static void writeFloatGroup(StringBuilder sb, int group, float value)
        {
            sb.AppendLine(string.Format("{0,3}", group));

            if (Math.Abs(value) < 10000 && value == Math.Floor(value))
            {
                sb.AppendLine(string.Format("{0:F1}", value));
            }
            else
            {
                sb.AppendLine(value.ToString());
            }
        }

        private static void writeHandleGroup(StringBuilder sb, int group)
        {
            sb.AppendLine(string.Format("{0,3}", group));
            sb.AppendLine(string.Format("{0:X}", _nextHandleValue++));
        }

        private static void writeDateGroup(StringBuilder sb, int group, DateTime dateTime)
        {
            sb.AppendLine(string.Format("{0,3}", group));
            sb.AppendLine(JulianDate(dateTime).ToString());
        }

        public static double JulianDate(DateTime dt)
        {
            DateTime jan12000 = new DateTime(2000, 1, 1);
            TimeSpan ts = dt - jan12000;
            return 2451545.0 + ts.TotalDays;
        }

        public static DateTime JulianToDateTime(int julianDate)
        {
            int RealJulian = julianDate + 1900000;
            int Year = Convert.ToInt32(RealJulian.ToString().Substring(0, 4));
            int DoY = Convert.ToInt32(RealJulian.ToString().Substring(4));
            DateTime dtOut = new DateTime(Year, 1, 1);
            return dtOut.AddDays(DoY - 1);
        }

        public Point ModelOrigin
        {
            get
            {
                return _modelOrigin;
            }
            set
            {
                _modelOrigin = value;
            }
        }

        public double Scale
        {
            get
            {
                return _scale;
            }
        }

        public float WcsToModel(float f, int space)
        {
            return space == 0 ? (float)(f * _scale) : f;
        }

        public Point WcsToModel(float wx, float wy, int space)
        {
            Point model;

            if (space == 0)
            {
                // Model space
                model = new Point((wx - _modelOrigin.X) * _scale, _paperMax.Y - ((wy - _modelOrigin.Y) * _scale));
            }
            else
            {
                // Paper space
                model = new Point(wx, _paperMax.Y - wy);
            }

            return model;
        }
    }
}
