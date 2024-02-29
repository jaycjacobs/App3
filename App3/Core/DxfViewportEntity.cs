
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Cirros.Dxf
{
    public class DxfViewportEntity : DxfEntity
    {
        public DxfViewportEntity(DxfReader reader)
            : base(reader)
        {
            Type = "VIEWPORT";
        }

        //100 Subclass marker (AcDbViewport) 
        // 10 Center point (in WCS)DXF: X value; APP: 3D point
        // 20, 30 DXF: Y and Z values of center point (in WCS)
        // 40 Width in paper space units
        // 41 Height in paper space units
        // 68 Viewport status field:
        //    -1 = On, but is fully off screen, or is one of the viewports that is not active because the $MAXACTVP count is currently being exceeded.
        //    0 = Off
        //    <positive value > = On and active. The value indicates the order of stacking for the viewports, where 1 is the active viewport, 2 is the next, and so forth.
        // 69 Viewport ID. 
        // 12 View center point (in DCS)DXF: X value; APP: 2D point
        // 22 DXF: View center point Y value (in DCS)
        // 13 Snap base point DXF: X value; APP: 2D point
        // 23 DXF: Snap base point Y value
        // 14 Snap spacingDXF: X value; APP: 2D point
        // 24 DXF: Snap spacing Y value
        // 15 Grid spacing DXF: X value; APP: 2D point
        // 25 DXF: Grid spacing Y value
        // 16 View direction vector (in WCS)DXF: X value; APP: 3D vector
        // 26, 36 DXF: Y and Z values of view direction vector (in WCS)
        // 17 View target point (in WCS)DXF: X value; APP: 3D vector
        // 27, 37 DXF: Y and Z values of view target point (in WCS)
        // 42 Perspective lens length
        // 43 Front clip plane Z value
        // 44 Back clip plane Z value
        // 45 View height (in model space units)
        // 50 Snap angle
        // 51 View twist angle 
        // 72 Circle zoom percent
        // 341 Frozen layer object ID/handle (multiple entries may exist) (optional)
        // 90
        //     Viewport status bit coded flags: 
        //    1 (0x1) = Enables perspective mode 
        //    2 (0x2) = Enables front clipping 
        //    4 (0x4) = Enables back clipping 
        //    8 (0x8) = Enables UCS follow 
        //    16 (0x10) = Enables front clip not at eye 
        //    32 (0x20) = Enables UCS icon visibility 
        //    64 (0x40) = Enables UCS icon at origin 
        //    128 (0x80) = Enables fast zoom 
        //    256 (0x100) = Enables snap mode 
        //    512 (0x200) = Enables grid mode 
        //    1024 (0x400) = Enables isometric snap style 
        //    2048 (0x800) = Enables hide plot mode 
        //    4096 (0x1000) = kIsoPairTop. If set and kIsoPairRight is not set, then isopair top is enabled. If both kIsoPairTop and kIsoPairRight are set, then isopair left is enabled. 
        //    8192 (0x2000) = kIsoPairRight. If set and kIsoPairTop is not set, then isopair right is enabled. 
        //    16384 (0x4000) = Enables viewport zoom locking 
        //    32768 (0x8000) = Currently always enabled 
        //    65536 (0x10000) = Enables non-rectangular clipping 
        //    131072 (0x20000) = Turns the viewport off
        // 340 Hard-pointer ID/handle to entity that serves as the viewport's clipping boundary (only present if viewport is non-rectangular)
        // 1 Plot style sheet name assigned to this viewport
        // 281
        //     Render mode:
        //    0 = 2D Optimized (classic 2D)
        //    1 = Wireframe
        //    2 = Hidden line
        //    3 = Flat shaded
        //    4 = Gouraud shaded
        //    5 = Flat shaded with wireframe
        //    6 = Gouraud shaded with wireframe

        //    All rendering modes other than 2D Optimized engage the new 3D graphics pipeline. These values directly correspond to the SHADEMODE command and the AcDbAbstractViewTableRecord::RenderMode enum
        // 71 UCS per viewport flag:
        //    0 = The UCS will not change when this viewport becomes active.
        //    1 = This viewport stores its own UCS which will become the current UCS whenever the viewport is activated.
        // 74 Display UCS icon at UCS origin flag: Controls whether UCS icon represents viewport UCS or current UCS (these will be different if UCSVP is 1 and viewport is not active).  However, this field is currently being ignored and the icon always represents the viewport UCS.
        // 110 UCS origin DXF: X value; APP: 3D point
        // 120, 130 DXF: Y and Z values of UCS origin
        // 111 UCS X-axis DXF: X value; APP: 3D vector
        // 121, 131 DXF: Y and Z values of UCS X-axis
        // 112 UCS Y-axis DXF: X value; APP: 3D vector
        // 122, 132 DXF: Y and Z values of UCS Y-axis
        // 345 ID/handle of AcDbUCSTableRecord if UCS is a named UCS.  If not present, then UCS is unnamed.
        // 346 ID/handle of AcDbUCSTableRecord of base UCS if UCS is orthographic (79 code is non-zero).  If not present and 79 code is non-zero, then base UCS is taken to be WORLD.
        // 79 Orthographic type of UCS
        //    0 = UCS is not orthographic;
        //    1 = Top; 2 = Bottom;
        //    3 = Front; 4 = Back;
        //    5 = Left; 6 = Right
        // 146 Elevation

        public float Width;             //40
        public float Height;            //41
        public float ModelHeight;       //45
        public int Status;              //68
        public int Id;                  //69
        public float X2;                // 12
        public float Y2;                // 22
        public float X3;                // 13
        public float Y3;                // 23
        public float X4;                // 14
        public float Y4;                // 24
        public float X5;                // 15
        public float Y5;                // 25
        public float X6;                // 16
        public float Y6;                // 26
        public float Z6;                // 36
        public float X7;                // 17
        public float Y7;                // 27
        public float Z7;                // 37
        public string Name;             // 1

        public override void SetValue(DxfGroup group)
        {
            switch (group.Code)
            {
                case 40:
                    // 40 Width in paper space units
                    Width = float.Parse(group.Value);
                    break;
                case 41:
                    // 41 Height in paper space units
                    Height = float.Parse(group.Value);
                    break;
                case 68:
                    // 68 Viewport status field:
                    //    -1 = On, but is fully off screen, or is one of the viewports that is not active because the $MAXACTVP count is currently being exceeded.
                    //    0 = Off
                    //    <positive value > = On and active. The value indicates the order of stacking for the viewports, where 1 is the active viewport, 2 is the next, and so forth.
                    Status = int.Parse(group.Value);
                    break;
                case 69:
                    // 69 Viewport ID. 
                    Id = int.Parse(group.Value);
                    break;
                case 12:
                    // 12 View center point (in DCS)DXF: X value; APP: 2D point
                    X2 = float.Parse(group.Value);
                    break;
                case 22:
                    // 22 DXF: View center point Y value (in DCS)
                    Y2 = float.Parse(group.Value);
                    break;
                case 13:
                    // 13 Snap base point DXF: X value; APP: 2D point
                    X3 = float.Parse(group.Value);
                    break;
                case 23:
                    // 23 DXF: Snap base point Y value
                    Y3 = float.Parse(group.Value);
                    break;
                case 14:
                    // 14 Snap spacingDXF: X value; APP: 2D point
                    X4 = float.Parse(group.Value);
                    break;
                case 24:
                    // 24 DXF: Snap spacing Y value
                    Y4 = float.Parse(group.Value);
                    break;
                case 15:
                    // 15 Grid spacing DXF: X value; APP: 2D point
                    X5 = float.Parse(group.Value);
                    break;
                case 25:
                    // 25 DXF: Grid spacing Y value
                    Y5 = float.Parse(group.Value);
                    break;
                case 16:
                    // 16 View direction vector (in WCS)DXF: X value; APP: 3D vector
                    X6 = float.Parse(group.Value);
                    break;
                case 26:
                    // 26, 36 DXF: Y and Z values of view direction vector (in WCS)
                    Y6 = float.Parse(group.Value);
                    break;
                case 36:
                    // 26, 36 DXF: Y and Z values of view direction vector (in WCS)
                    Z6 = float.Parse(group.Value);
                    break;
                case 17:
                    // 17 View target point (in WCS)DXF: X value; APP: 3D vector
                    X7 = float.Parse(group.Value);
                    break;
                case 27:
                    // 27, 37 DXF: Y and Z values of view target point (in WCS)
                    Y7 = float.Parse(group.Value);
                    break;
                case 37:
                    // 27, 37 DXF: Y and Z values of view target point (in WCS)
                    Z7 = float.Parse(group.Value);
                    break;
                case 42:
                    // 42 Perspective lens length
                    break;
                case 43:
                    // 43 Front clip plane Z value
                    break;
                case 44:
                    // 44 Back clip plane Z value
                    break;
                case 45:
                    // 45 View height (in model space units)
                    ModelHeight = float.Parse(group.Value);
                    break;
                case 50:
                    // 50 Snap angle
                    break;
                case 51:
                    // 51 View twist angle 
                    break;
                case 72:
                    // 72 Circle zoom percent
                    break;
                case 341:
                    // 341 Frozen layer object ID/handle (multiple entries may exist) (optional)
                    break;
                case 90:
                    // 90
                    //     Viewport status bit coded flags: 
                    //    1 (0x1) = Enables perspective mode 
                    //    2 (0x2) = Enables front clipping 
                    //    4 (0x4) = Enables back clipping 
                    //    8 (0x8) = Enables UCS follow 
                    //    16 (0x10) = Enables front clip not at eye 
                    //    32 (0x20) = Enables UCS icon visibility 
                    //    64 (0x40) = Enables UCS icon at origin 
                    //    128 (0x80) = Enables fast zoom 
                    //    256 (0x100) = Enables snap mode 
                    //    512 (0x200) = Enables grid mode 
                    //    1024 (0x400) = Enables isometric snap style 
                    //    2048 (0x800) = Enables hide plot mode 
                    //    4096 (0x1000) = kIsoPairTop. If set and kIsoPairRight is not set, then isopair top is enabled. If both kIsoPairTop and kIsoPairRight are set, then isopair left is enabled. 
                    //    8192 (0x2000) = kIsoPairRight. If set and kIsoPairTop is not set, then isopair right is enabled. 
                    //    16384 (0x4000) = Enables viewport zoom locking 
                    //    32768 (0x8000) = Currently always enabled 
                    //    65536 (0x10000) = Enables non-rectangular clipping 
                    //    131072 (0x20000) = Turns the viewport off
                    break;
                case 340:
                    // 340 Hard-pointer ID/handle to entity that serves as the viewport's clipping boundary (only present if viewport is non-rectangular)
                    break;
                case 1:
                    // 1 Plot style sheet name assigned to this viewport
                    Name = group.Value;
                    break;
                case 281:
                    // 281
                    //     Render mode:
                    //    0 = 2D Optimized (classic 2D)
                    //    1 = Wireframe
                    //    2 = Hidden line
                    //    3 = Flat shaded
                    //    4 = Gouraud shaded
                    //    5 = Flat shaded with wireframe
                    //    6 = Gouraud shaded with wireframe
                    //    All rendering modes other than 2D Optimized engage the new 3D graphics pipeline. These values directly correspond to the SHADEMODE command and the AcDbAbstractViewTableRecord::RenderMode enum
                    break;
                case 71:
                    // 71 UCS per viewport flag:
                    //    0 = The UCS will not change when this viewport becomes active.
                    //    1 = This viewport stores its own UCS which will become the current UCS whenever the viewport is activated.
                    break;
                case 74:
                    // 74 Display UCS icon at UCS origin flag: Controls whether UCS icon represents viewport UCS or current UCS (these will be different if UCSVP is 1 and viewport is not active).  However, this field is currently being ignored and the icon always represents the viewport UCS.
                    break;
                case 110:
                    // 110 UCS origin DXF: X value; APP: 3D point
                    break;
                case 120:
                    // 120, 130 DXF: Y and Z values of UCS origin
                    break;
                case 130:
                    // 120, 130 DXF: Y and Z values of UCS origin
                    break;
                case 111:
                    // 111 UCS X-axis DXF: X value; APP: 3D vector
                    break;
                case 121:
                    // 121, 131 DXF: Y and Z values of UCS X-axis
                    break;
                case 131:
                    // 121, 131 DXF: Y and Z values of UCS X-axis
                    break;
                case 112:
                    // 112 UCS Y-axis DXF: X value; APP: 3D vector
                    break;
                case 122:
                    // 122, 132 DXF: Y and Z values of UCS Y-axis
                    break;
                case 132:
                    // 122, 132 DXF: Y and Z values of UCS Y-axis
                    break;
                case 345:
                    // 345 ID/handle of AcDbUCSTableRecord if UCS is a named UCS.  If not present, then UCS is unnamed.
                    break;
                case 346:
                    // 346 ID/handle of AcDbUCSTableRecord of base UCS if UCS is orthographic (79 code is non-zero).  If not present and 79 code is non-zero, then base UCS is taken to be WORLD.
                    break;
                case 79:
                    // 79 Orthographic type of UCS
                    //    0 = UCS is not orthographic;
                    //    1 = Top; 2 = Bottom;
                    //    3 = Front; 4 = Back;
                    //    5 = Left; 6 = Right
                    break;
                case 146:
                    // 146 Elevation
                    break;
                default:
                    base.SetValue(group);
                    break;
            }
        }

        public string ExVersion = "";
        public double ExViewTargetX = 0;
        public double ExViewTargetY = 0;
        public double ExViewTargetZ = 0;
        public double ExViewVectorX = 0;
        public double ExViewVectorY = 0;
        public double ExViewVectorZ = 0;
        public double ExViewTwistAngle = 0;
        public double ExViewHeight = 0;
        public double ExViewCenterX = 0;
        public double ExViewCenterY = 0;
        public double ExPerspectiveLensLength = 0;
        public double ExViewFrontClipZ = 0;
        public double ExViewBackClipZ = 0;
        public int ExViewMode = 0;
        public int ExCircleZoom = 0;
        public int ExFastZoom = 0;
        public int ExUCSICON = 0;
        public int ExSnapMode = 0;
        public int ExGridMode = 0;
        public int ExSnapStyle = 0;
        public int ExSnapISOPAIR = 0;
        public double ExSnapAngle = 0;
        public double ExSnapXBase = 0;
        public double ExSnapYBase = 0;
        public double ExSnapXSpace = 0;
        public double ExSnapYSpace = 0;
        public double ExGridXSpace = 0;
        public double ExGridYSpace = 0;
        public int ExHiddenInPlot = 0;

        public double ExViewScale = 1;
        public Rect ExViewRect = Rect.Empty;

        public List<string> ExFrozenLayers = new List<string>();

        public bool ParseExtendedEntityData()
        {
            bool valid = false;

            if (ExtendedEntityData != null)
            {
                try
                {
                    if (ExtendedEntityData[0].Code == 1070)
                    {
                        ExVersion = ExtendedEntityData[0].Value;

                        if (ExtendedEntityData[1].Code == 1010 && ExtendedEntityData[2].Code == 1020 && ExtendedEntityData[3].Code == 1030)
                        {
                            ExViewTargetX = double.Parse(ExtendedEntityData[1].Value);
                            ExViewTargetY = double.Parse(ExtendedEntityData[2].Value);
                            ExViewTargetZ = double.Parse(ExtendedEntityData[3].Value);

                            if (ExtendedEntityData[4].Code == 1010 && ExtendedEntityData[5].Code == 1020 && ExtendedEntityData[6].Code == 1030)
                            {
                                ExViewVectorX = double.Parse(ExtendedEntityData[4].Value);
                                ExViewVectorY = double.Parse(ExtendedEntityData[5].Value);
                                ExViewVectorZ = double.Parse(ExtendedEntityData[6].Value);

                                for (int i = 7; i <= 13; i++)
                                {
                                    if (ExtendedEntityData[i].Code != 1040)
                                    {
                                        throw new Exception("MVIEW format error");
                                    }
                                }

                                ExViewTwistAngle = double.Parse(ExtendedEntityData[7].Value);
                                ExViewHeight = double.Parse(ExtendedEntityData[8].Value);
                                ExViewCenterX = double.Parse(ExtendedEntityData[9].Value);
                                ExViewCenterY = double.Parse(ExtendedEntityData[10].Value);
                                ExPerspectiveLensLength = double.Parse(ExtendedEntityData[11].Value);
                                ExViewFrontClipZ = double.Parse(ExtendedEntityData[12].Value);
                                ExViewBackClipZ = double.Parse(ExtendedEntityData[13].Value);

                                for (int i = 14; i <= 21; i++)
                                {
                                    if (ExtendedEntityData[i].Code != 1070)
                                    {
                                        throw new Exception("MVIEW format error");
                                    }
                                }

                                ExViewMode = int.Parse(ExtendedEntityData[14].Value);
                                ExCircleZoom = int.Parse(ExtendedEntityData[15].Value);
                                ExFastZoom = int.Parse(ExtendedEntityData[16].Value);
                                ExUCSICON = int.Parse(ExtendedEntityData[17].Value);
                                ExSnapMode = int.Parse(ExtendedEntityData[18].Value);
                                ExGridMode = int.Parse(ExtendedEntityData[19].Value);
                                ExSnapStyle = int.Parse(ExtendedEntityData[20].Value);
                                ExSnapISOPAIR = int.Parse(ExtendedEntityData[21].Value);

                                for (int i = 22; i <= 28; i++)
                                {
                                    if (ExtendedEntityData[i].Code != 1040)
                                    {
                                        throw new Exception("MVIEW format error");
                                    }
                                }

                                ExSnapAngle = double.Parse(ExtendedEntityData[22].Value);
                                ExSnapXBase = double.Parse(ExtendedEntityData[23].Value);
                                ExSnapYBase = double.Parse(ExtendedEntityData[24].Value);
                                ExSnapXSpace = double.Parse(ExtendedEntityData[25].Value);
                                ExSnapYSpace = double.Parse(ExtendedEntityData[26].Value);
                                ExGridXSpace = double.Parse(ExtendedEntityData[27].Value);
                                ExGridYSpace = double.Parse(ExtendedEntityData[28].Value);

                                if (ExtendedEntityData[29].Code != 1070)
                                {
                                    throw new Exception("MVIEW format error");
                                }

                                ExHiddenInPlot = int.Parse(ExtendedEntityData[29].Value);

                                for (int i = 30; i < ExtendedEntityData.Count; i++)
                                {
                                    if (ExtendedEntityData[i].Code == 1003)
                                    {
                                        ExFrozenLayers.Add(ExtendedEntityData[i].Value);
                                    }
                                }

                                ExViewScale = Height / ExViewHeight;
                                if (Math.Round(ExViewScale) == 1)
                                {
                                    ExViewScale = 1;
                                }
                                ExViewRect = new Rect(ExViewCenterX - (Width / 2) / ExViewScale, ExViewCenterY - (ExViewHeight / 2), Width / ExViewScale, ExViewHeight);
                            }
                        }
                    }
                    valid = true;
                }
                catch
                {
                    valid = false;
                }
            }

            return valid;
        }
    }
}
