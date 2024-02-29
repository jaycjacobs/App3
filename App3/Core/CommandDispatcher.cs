using Cirros.Actions;
using Cirros.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cirros.Core
{
    public class CommandDispatcher
    {
        CommandType _activeCommand = CommandType.none;

        public CommandType ActiveCommand
        {
            get
            {
                return _activeCommand;
            }
            set
            {
#if SIBERIA
                try
                {
                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Finish();
                        Globals.CommandProcessor = null;
                    }

                    _activeCommand = value;

                    Analytics.Trace("CommandDispatcher", _activeCommand.ToString());

                    switch (_activeCommand)
                    {
                        case CommandType.select:
                            Globals.CommandProcessor = new SelectCommand();
                            break;

                        case CommandType.copypaste:
                            Globals.CommandProcessor = new CopyPasteCommand();
                            break;

                        case CommandType.edit:
                            Globals.CommandProcessor = new EditCommandProcessor();
                            break;

                        case CommandType.editgroup:
                            Globals.CommandProcessor = new EditGroupCommandProcessor();
                            break;

                        case CommandType.copyalongline:
                            Globals.CommandProcessor = new CopyAlongLineCommandProcessor(Globals.LinearCopyGroupName);
                            break;

                        case CommandType.copyalongarc:
                            Globals.CommandProcessor = new CopyAlongArcCommandProcessor();
                            break;

                        case CommandType.line:
                            Globals.CommandProcessor = new PolylineCommandProcessor(true);
                            break;

                        case CommandType.polyline:
                            Globals.CommandProcessor = new PolylineCommandProcessor(false);
                            break;

                        case CommandType.fillet:
                            Globals.CommandProcessor = new FilletCommandProcessor();
                            break;

                        case CommandType.freehand:
                            Globals.CommandProcessor = new FreehandCommandProcessor();
                            break;

                        case CommandType.polygon:
                            if (Globals.PolygonCommandType == PolygonCommandType.Regular)
                            {
                                Globals.CommandProcessor = new RegularPolygonCommandProcessor(Globals.PolygonSides);
                            }
                            else
                            {
                                Globals.CommandProcessor = new PolygonCommandProcessor();
                            }
                            break;

                        case CommandType.doubleline:
                            Globals.CommandProcessor = new DoublelineCommandProcessor();
                            break;

                        case CommandType.bspline:
                            Globals.CommandProcessor = new BSplineCommandProcessor();
                            break;

                        case CommandType.rectangle:
                            Globals.CommandProcessor = new RectangleCommandProcessor();
                            break;

                        case CommandType.arc:
                            if (Globals.ArcCommandType == ArcCommandType.CenterRadiusAngles)
                            {
                                Globals.CommandProcessor = new Arc1CommandProcessor();
                            }
                            else
                            {
                                Globals.CommandProcessor = new ArcCommandProcessor();
                            }
                            break;

                        case CommandType.arc2:
                            Globals.CommandProcessor = new Arc2CommandProcessor();
                            break;

                        case CommandType.arc3:
                            Globals.CommandProcessor = new Arc3CommandProcessor();
                            break;

                        case CommandType.arcf:
                            Globals.CommandProcessor = new ArcFilletCommandProcessor();
                            break;

                        case CommandType.arcfr:
                            Globals.CommandProcessor = new ArcFilletCommandProcessor();
                            break;

                        case CommandType.circle:
                            Globals.CommandProcessor = new CircleCommandProcessor();
                            break;

                        case CommandType.circle3:
                            Globals.CommandProcessor = new Circle3CommandProcessor();
                            break;

                        case CommandType.ellipse:
                            if (Globals.EllipseCommandType == EllipseCommandType.Axis)
                            {
                                Globals.CommandProcessor = new EllipseAxisCommandProcessor();
                            }
                            else if (Globals.EllipseCommandType == EllipseCommandType.Center)
                            {
                                Globals.CommandProcessor = new EllipseCenterCommandProcessor();
                            }
                            else
                            {
                                Globals.CommandProcessor = new EllipseBoxCommandProcessor();
                            }
                            break;

                        case CommandType.text:
                            Globals.CommandProcessor = new TextCommandProcessor();
                            break;

                        case CommandType.arrow:
                            Globals.CommandProcessor = new ArrowCommandProcessor();
                            break;

                        case CommandType.dimension:
                            Globals.CommandProcessor = new DimensionCommandProcessor();
                            break;

                        case CommandType.image:
                            Globals.CommandProcessor = new ImageCommandProcessor();
                            break;

                        case CommandType.insertimage:
                            if (Globals.ImageDictionary != null)
                            {
                                Globals.CommandProcessor = new ImageCommandProcessor();
                                Globals.CommandProcessor.Invoke("A_InsertImage", Globals.ImageDictionary);
                                Globals.ImageDictionary = null;
                            }
                            break;

                        case CommandType.insertsymbol:
                            Globals.CommandProcessor = new InsertCommandProcessor(Globals.GroupName);
                            break;

                        case CommandType.insert:
                            Globals.CommandProcessor = new InsertCommandProcessor();
                            break;

                        case CommandType.properties:
                            Globals.CommandProcessor = new PropertiesCommandProcessor();
                            break;
#if KT22
                        case CommandType.ktproperties:
                            Globals.CommandProcessor = new KTPropertiesCommandProcessor();
                            break;
#endif
                        case CommandType.origin:
                            Globals.CommandProcessor = new OriginCommandProcessor();
                            break;

                        case CommandType.distance:
                            Globals.CommandProcessor = new DistanceCommandProcessor();
                            break;

                        case CommandType.angle:
                            Globals.CommandProcessor = new AngleCommandProcessor();
                            break;

                        case CommandType.area:
                            Globals.CommandProcessor = new AreaCommandProcessor();
                            break;

                        case CommandType.window:
                            Globals.CommandProcessor = new WindowCommandProcessor();
                            break;

                        case CommandType.pan:
                            Globals.CommandProcessor = new PanCommandProcessor();
                            break;

                        default:
                            break;
                    }

                    if (Globals.CommandProcessor != null)
                    {
                        Globals.CommandProcessor.Initialize();
                    }

                    Globals.CommandProcessorParameter = null;
                }
                catch (Exception ex)
                {
                    Analytics.ReportError(ex, new Dictionary<string, string> {
                        { "method", "ActiveCommand" },
                        { "command", Globals.CommandProcessor == null ? "none" : Globals.CommandProcessor.Type.ToString() }
                    }, 200);
                }
#endif
            }
        }

        Stack<CAction> _undoActions = new Stack<CAction>();
        Stack<CAction> _redoActions = new Stack<CAction>();

        public event UndoActionHandler OnUndoAction;
        public delegate void UndoActionHandler(object sender, UndoActionEventArgs e);

        public class UndoActionEventArgs : EventArgs
        {
            public ActionID ActionID { get; private set; }

            public UndoActionEventArgs(ActionID actionId)
            {
                ActionID = actionId;
            }
        }

        public string DebugUndoActionList
        {
            get
            {
                StringBuilder sb = new System.Text.StringBuilder();
#if SIBERIA
                foreach (CAction action in _undoActions.ToArray())
                {
                    if (action.ID == ActionID.DeletePrimitive)
                    {
                        Cirros.Primitives.Primitive p = action.Subject as Cirros.Primitives.Primitive;
                        sb.AppendFormat("{0}: {1}\n", action.ID.ToString(), p.TypeName);
                    }
                    else
                    {
                        sb.AppendLine(action.ID.ToString());
                    }
                }
#endif
                return sb.ToString();
            }
        }

        public string DebugRedoActionList
        {
            get
            {
                StringBuilder sb = new System.Text.StringBuilder();
#if SIBERIA
                foreach (CAction action in _redoActions.ToArray())
                {
                    if (action.ID == ActionID.DeletePrimitive)
                    {
                        Cirros.Primitives.Primitive p = action.Subject as Cirros.Primitives.Primitive;
                        sb.AppendFormat("{0}: {1}\n", action.ID.ToString(), p.TypeName);
                    }
                    else
                    {
                        sb.AppendLine(action.ID.ToString());
                    }
                }
#endif
                return sb.ToString();
            }
        }

        public int UndoCount
        {
            get
            {
                return _undoActions.Count;
            }
        }


        public bool CanUndo
        {
            get
            {
                return _undoActions.Count > 0;
            }
        }

        public bool CanRedo
        {
            get
            {
                return _redoActions.Count > 0;
            }
        }

        public void AddUndoableAction(ActionID actionId, object subject = null, object predicate = null, object predicate2 = null)
        {
            //System.Diagnostics.Debug.WriteLine("AddUndoableAction({0}, {1}, {2}, {3})", actionId, subject, predicate, predicate2);
            _undoActions.Push(new CAction(actionId, subject, predicate, predicate2));
            _redoActions.Clear();
            Globals.Events.UndoStackChanged();

            Globals.ActiveDrawing.IsModified = true;
            Globals.ActiveDrawing.ChangeNumber++;
        }

        public void Undo()
        {
            if (_undoActions.Count > 0)
            {
                CAction action = _undoActions.Pop();
                CAction redoAction = action.UndoExecute();

                if (redoAction != null)
                {
                    _redoActions.Push(redoAction);
                }

                Globals.Events.UndoStackChanged();

                if (OnUndoAction != null)
                {
                    UndoActionEventArgs e = new UndoActionEventArgs(action.ID);
                    OnUndoAction(this, e);
                }

                Globals.ActiveDrawing.ChangeNumber++;
            }
        }

        public void Redo()
        {
            if (_redoActions.Count > 0)
            {
                CAction action = _redoActions.Pop();
                CAction undoAction = action.RedoExecute();

                if (undoAction != null)
                {
                    _undoActions.Push(undoAction);
                }

                Globals.Events.UndoStackChanged();

                if (OnUndoAction != null)
                {
                    UndoActionEventArgs e = new UndoActionEventArgs(action.ID);
                    OnUndoAction(this, e);
                }

                Globals.ActiveDrawing.ChangeNumber++;
            }
        }

        public int RemoveAction(ActionID actionId)
        {
            if (_undoActions.Count > 0 && _undoActions.Peek().ID == actionId)
            {
                _undoActions.Pop();
                return 1;
            }

            return 0;
        }

        public void RemoveActions(ActionID actionId)
        {
            RemoveActions(_redoActions, actionId);
            RemoveActions(_undoActions, actionId);
        }

        private void RemoveActions(Stack<CAction> stack, ActionID actionId)
        {
            int count = 0;

            while (stack.Count > 0)
            {
                CAction peek = stack.Peek();
                if (peek.ID == ActionID.MultiUndo)
                {
                    CAction action = stack.Pop();
                    ++count;

                    if (stack.Count > 0 && stack.Peek().ID == actionId)
                    {
                        // Leap of faith: Assume that all actions in a MultiUndo set are the same action.

                        for (int i = 0; i < (int)action.Subject; ++i)
                        {
                            stack.Pop();
                            ++count;
                        }
                        continue;
                    }
                    else
                    {
                        // This is not the action we're looking for.  Put it back.
                        stack.Push(action);
                        count--;
                        break;
                    }
                }
                else if (peek.ID == actionId)
                {
                    stack.Pop();
                    count++;
                    continue;
                }
                break;
            }
        }

        public void ClearRedoActions()
        {
            _redoActions.Clear();
        }

        public void ClearActions()
        {
            _undoActions.Clear();
            _redoActions.Clear();
        }
    }
}
