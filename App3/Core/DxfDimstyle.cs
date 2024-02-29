
namespace Cirros.Dxf
{
    public class DxfDimstyle
    {
        DxfReader _reader;

        public DxfDimstyle(DxfReader reader)
        {
            _reader = reader;
        }

        // 100	Subclass marker (AcDbDimStyleTableRecord)
        //   2	Dimension style name
        //  70	Standard flag values (bit-coded values):
        //        16 = If set, table entry is externally dependent on an xref.
        //        32 = If this bit and bit 16 are both set, the externally dependent xref has been successfully resolved.
        //        64 = If set, the table entry was referenced by at least one entity in the drawing the last time the drawing was edited. (This flag is for the benefit of AutoCAD commands. It can be ignored by most programs that read DXF files and need not be set by programs that write DXF files.)
        //   3	DIMPOST 
        //   4	DIMAPOST 
        //   5	DIMBLK (obsolete, now object ID) 
        //   6	DIMBLK1 (obsolete, now object ID) 
        //   7	DIMBLK2 (obsolete, now object ID) 
        //  40	DIMSCALE 
        //  41	DIMASZ 
        //  42	DIMEXO 
        //  43	DIMDLI 
        //  44	DIMEXE
        //  45	DIMRND
        //  46	DIMDLE
        //  47	DIMTP
        //  48	DIMTM
        // 140	DIMTXT
        // 141	DIMCEN
        // 142	DIMTSZ
        // 143	DIMALTF
        // 144	DIMLFAC
        // 145	DIMTVP
        // 146	DIMTFAC
        // 147	DIMGAP
        // 148	DIMALTRND
        //  71	DIMTOL
        //  72	DIMLIM
        //  73	DIMTIH
        //  74	DIMTOH
        //  75	DIMSE1
        //  76	DIMSE2
        //  77	DIMTAD
        //  78	DIMZIN
        //  79	DIMAZIN
        // 170	DIMALT
        // 171	DIMALTD
        // 172	DIMTOFL
        // 173	DIMSAH
        // 174	DIMTIX
        // 175	DIMSOXD
        // 176	DIMDLRD
        // 177	DIMCLRE
        // 178	DIMCLRT
        // 179	DIMADEC
        // 270	DIMUNIT (obsolete, now use DIMLUNIT AND DIMFRAC)
        // 271	DIMDEC
        // 272	DIMTDEC
        // 273	DIMALTU
        // 274	DIMALTTD
        // 275	DIMAUNIT
        // 276	DIMKFRAC
        // 277	DIMLUNIT
        // 278	DIMDSEP
        // 279	DIMTMOVE
        // 280	DIMJUST
        // 281	DIMSD1
        // 282	DIMSD2
        // 283	DIMTOLJ
        // 284	DIMTZIN
        // 285	DIMALTZ
        // 286	DIMALTTZ
        // 287	DIMFIT (obsolete, now use DIMATFIT and DIMTMOVE)
        // 288	DIMUPT
        // 340	DIMTXSTY (handle of referenced STYLE)
        // 341	DIMLDRBLK (handle of referenced BLOCK)
        // 342	DIMBLK (handle of referenced BLOCK)
        // 343	DIMBLK1 (handle of referenced BLOCK)
        // 344	DIMBLK2 (handle of referenced BLOCK)
        // 371	DIMLWD (lineweight enum value)
        // 372	DIMLWE (lineweight enum value)

        public string Name;             //  2
        public int Flags;               //  70
        public float Scale;             //  40	DIMSCALE: Overall dimensioning scale factor
        public float ArrowSize;         //  41	DIMASZ: Dimensioning arrow size
        public float ExtOffset;         //  42	DIMEXO: Extension line offset
        public float DimLineIncr;       //  43	DIMDLI: Dimension line increment
        public float ExtExt;            //  44	DIMEXE: Extension line extension
        public float Round;             //  45	DIMRND: Rounding value for dimension distances
        public float DimExt;            //  46	DIMDLE: Dimension line extension

        public void SetGroup(DxfGroup group)
        {
            switch (group.Code)
            {
                case 2:
                    Name = group.Value;
                    break;

                case 70:
                    Flags = int.Parse(group.Value);
                    break;

                case 3:         //   3	DIMPOST 
                    break;

                case 4:         //   4	DIMAPOST 
                    break;

                case 5:         //   5	DIMBLK (obsolete, now object ID) 
                    break;

                case 6:         //   6	DIMBLK1 (obsolete, now object ID) 
                    break;

                case 7:         //   7	DIMBLK2 (obsolete, now object ID) 
                    break;

                case 40:        //  40	DIMSCALE: Overall dimensioning scale factor 
                    Scale = float.Parse(group.Value);
                    break;

                case 41:        //  41	DIMASZ: Dimensioning arrow size 
                    ArrowSize = float.Parse(group.Value);
                    break;

                case 42:        //  42	DIMEXO: Extension line offset
                    ExtOffset = float.Parse(group.Value);
                    break;

                case 43:        //  43	DIMDLI: Dimension line increment
                    DimLineIncr = float.Parse(group.Value);
                    break;

                case 44:        //  44	DIMEXE: Extension line extension
                    ExtExt = float.Parse(group.Value);
                    break;

                case 45:        //  45	DIMRND: Rounding value for dimension distances
                    Round = float.Parse(group.Value);
                    break;

                case 46:        //  46	DIMDLE: Dimension line extension
                    DimExt = float.Parse(group.Value);
                    break;

                case 47:        //  47	DIMTP: Plus tolerance
                    break;

                case 48:        //  48	DIMTM: Minus tolerance
                    break;

                case 140:       // 140	DIMTXT: Dimensioning text height
                    break;

                case 141:        // 141	DIMCEN: Size of center mark/lines
                    break;

                case 142:        // 142	DIMTSZ: Dimensioning tick size: 0 = No ticks
                    break;

                case 143:        // 143	DIMALTF: Alternate unit scale factor
                    break;

                case 144:        // 144	DIMLFAC: Linear measurements scale factor
                    break;

                case 145:        // 145	DIMTVP: Text vertical position
                    break;

                case 146:        // 146	DIMTFAC: Dimension tolerance display scale factor
                    break;

                case 147:        // 147	DIMGAP: Dimension line gap
                    break;

                case 148:        // 148	DIMALTRND: Determines rounding of alternate units
                    break;

                case 71:        //  71	DIMTOL: Dimension tolerances generated if nonzero
                    break;

                case 72:        //  72	DIMLIM: Dimension limits generated if nonzero
                    break;

                case 73:        //  73	DIMTIH: Text inside horizontal if nonzero
                    break;

                case 74:        //  74	DIMTOH: Text outside horizontal if nonzero
                    break;

                case 75:        //  75	DIMSE1: First extension line suppressed if nonzero
                    break;

                case 76:        //  76	DIMSE2: Second extension line suppressed if nonzero
                    break;

                case 77:        //  77	DIMTAD: Text above dimension line if nonzero
                    break;

                case 78:        //  78	DIMZIN: Controls suppression of zeros for primary unit values: 
                                //      0 = Suppresses zero feet and precisely zero inches
                                //      1 = Includes zero feet and precisely zero inches
                                //      2 = Includes zero feet and suppresses zero inches
                                //      3 = Includes zero inches and suppresses zero feet
                    break;

                case 79:        //  79	DIMAZIN: Controls suppression of zeros for angular dimensions:
                                //      0 = Displays all leading and trailing zeros
                                //      1 = Suppresses leading zeros in decimal dimensions
                                //      2 = Suppresses trailing zeros in decimal dimensions 
                                //      3 = Suppresses leading and trailing zeros
                    break;

                case 170:       // 170	DIMALT: Alternate unit dimensioning performed if nonzero
                    break;

                case 171:       // 171	DIMALTD: Alternate unit decimal places
                    break;

                case 172:       // 172	DIMTOFL: If text is outside extensions, force line extensions between extensions if nonzero
                    break;

                case 173:       // 173	DIMSAH: Use separate arrow blocks if nonzero
                    break;

                case 174:       // 174	DIMTIX: Force text inside extensions if nonzero
                    break;

                case 175:       // 175	DIMSOXD: Suppress outside-extensions dimension lines if nonzero
                    break;

                case 176:       // 176	DIMDLRD
                    break;

                case 177:       // 177	DIMCLRE: Dimension extension line color: range is 0 = BYBLOCK, 256 = BYLAYER
                    break;

                case 178:       // 178	DIMCLRT: Dimension text color: range is 0 = BYBLOCK, 256 = BYLAYER
                    break;

                case 179:       // 179	DIMADEC: Number of precision places displayed in angular dimensions
                    break;

                case 270:       // 270	DIMUNIT (obsolete, now use DIMLUNIT AND DIMFRAC)
                    break;

                case 271:       // 271	DIMDEC: Number of decimal places for the tolerance values of a primary units dimension
                    break;

                case 272:       // 272	DIMTDEC: Number of decimal places to display the tolerance values
                    break;

                case 273:       // 273	DIMALTU: Units format for alternate units of all dimension style family members except angular: 
                                //      1 = Scientific; 2 = Decimal; 3 = Engineering; 
                                //      4 = Architectural (stacked); 5 = Fractional (stacked);
                                //      6 = Architectural; 7 = Fractional
                    break;

                case 274:       // 274	DIMALTTD: Number of decimal places for tolerance values of an alternate units dimension 
                    break;

                case 275:       // 275	DIMAUNIT: Angle format for angular dimensions: 0 = Decimal degrees, 1 = Degrees/minutes/seconds, 2 = Gradians, 3 = Radians, 4 = Surveyor's units
                    break;

                case 276:       // 276	DIMKFRAC: Unknown
                    break;

                case 277:       // 277	DIMLUNIT: Sets units for all dimension types except Angular:
                                //      1 = Scientific; 2 = Decimal; 3 = Engineering
                                //      4 = Architectural; 5 = Fractional; 6 = Windows desktop
                    break;

                case 278:       // 278	DIMDSEP: Single-character decimal separator used when creating dimensions whose unit format is decimal
                    break;

                case 279:       // 279	DIMTMOVE: Dimension text movement rules: 
                                //      0 = Moves the dimension line with dimension text
                                //      1 = Adds a leader when dimension text is moved
                                //      2 = Allows text to be moved freely without a leader
                    break;

                case 280:       // 280	DIMJUST: Horizontal dimension text position: 
                                //      0 = Above dimension line and center-justified between extension lines, 
                                //      1 = Above dimension line and next to first extension line, 
                                //      2 = Above dimension line and next to second extension line, 
                                //      3 = Above and center-justified to first extension line, 
                                //      4 = Above and center-justified to second extension line
                    break;

                case 281:       // 281	DIMSD1: Suppression of first extension line: 0 = Not suppressed, 1 = Suppressed
                    break;

                case 282:       // 282	DIMSD2: Suppression of second extension line: 0 = Not suppressed, 1 = Suppressed
                    break;

                case 283:       // 283	DIMTOLJ: Vertical justification for tolerance values: 0 = Top, 1 = Middle, 2 = Bottom
                    break;

                case 284:       // 284	DIMTZIN: Controls suppression of zeros for tolerance values: 
                                //      0 = Suppresses zero feet and precisely zero inches
                                //      1 = Includes zero feet and precisely zero inches
                                //      2 = Includes zero feet and suppresses zero inches
                                //      3 = Includes zero inches and suppresses zero feet
                    break;

                case 285:       // 285	DIMALTZ: Controls suppression of zeros for alternate unit dimension values: 
                                //      0 = Suppresses zero feet and precisely zero inches
                                //      1 = Includes zero feet and precisely zero inches
                                //      2 = Includes zero feet and suppresses zero inches
                                //      3 = Includes zero inches and suppresses zero feet
                    break;

                case 286:       // 286	DIMALTTZ: Controls suppression of zeros for alternate tolerance values: 
                                //      0 = Suppresses zero feet and precisely zero inches
                                //      1 = Includes zero feet and precisely zero inches
                                //      2 = Includes zero feet and suppresses zero inches
                                //      3 = Includes zero inches and suppresses zero feet
                    break;

                case 287:       // 287	DIMFIT (obsolete, now use DIMATFIT and DIMTMOVE)
                    break;

                case 288:       // 288	DIMUPT: Cursor functionality for user positioned text: 
                                //      0 = Controls only the dimension line location
                                //      1 = Controls the text position as well as the dimension line location
                    break;

                case 340:       // 340	DIMTXSTY (handle of referenced STYLE)
                    break;

                case 341:       // 341	DIMLDRBLK (handle of referenced BLOCK)
                    break;

                case 342:       // 342	DIMBLK (handle of referenced BLOCK)
                    break;

                case 343:       // 343	DIMBLK1 (handle of referenced BLOCK)
                    break;

                case 344:       // 344	DIMBLK2 (handle of referenced BLOCK)
                    break;

                case 371:       // 371	DIMLWD (lineweight enum value)
                    break;

                case 372:       // 372	DIMLWE (lineweight enum value)
                    break;

                case 100:       // 100	Subclass marker (AcDbViewportTableRecord)
                    break;

                case 102:       // 102  Start of application-defined group  
                    break;

                case 105:       // 105  Handle (DIMSTYLE table only)
                    break;

                case 330:       // 330  Soft pointer ID/handle to owner dictionary (optional) 
                    break;

                default:
#if DEBUG
                    string msg = string.Format("DXF parse: Unexpected group in DIMSTYLE definition: code={0}, value={1}", group.Code, group.Value);
                    System.Diagnostics.Debug.WriteLine(msg);
#endif
                    break;
            }
        }
    }
}
