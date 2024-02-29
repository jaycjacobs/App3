using Cirros;
using Cirros.Primitives;
using CirrosUI;
using CirrosUI.Context_Menu;
using CirrosUWP.CirrosUI.Context_Menu;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KT22
{
    public sealed partial class KTDrawingPage : Page
    {
        void Events_OnShowContextMenu(object sender, ShowContextMenuEventArgs e)
        {
            if (e.Target == null)
            {
                if (_contextMenuPopup != null)
                {
                    _contextMenuPopup.IsOpen = false;
                    _contextMenuPopup.Child = null;
                }
            }
            else if (e.Target == "copyalongline")
            {
                CopyAlongLineContextMenuPanel panel;

                if (_contextMenuPopup.Child is CopyAlongLineContextMenuPanel)
                {
                    panel = _contextMenuPopup.Child as CopyAlongLineContextMenuPanel;
                }
                else
                {
                    _contextMenuPopup.Child = panel = new CopyAlongLineContextMenuPanel();
                }

                panel.Select(e.Selection as Primitive);

                _contextMenuPopup.IsOpen = true;
                _contextMenuPopup.Visibility = Visibility.Visible;
            }
            else if (e.Target == "copyalongarc")
            {
                CopyAlongArcContextMenuPanel panel;

                if (_contextMenuPopup.Child is CopyAlongArcContextMenuPanel)
                {
                    panel = _contextMenuPopup.Child as CopyAlongArcContextMenuPanel;
                }
                else
                {
                    _contextMenuPopup.Child = panel = new CopyAlongArcContextMenuPanel();
                }

                panel.Select(e.Selection as Primitive);

                _contextMenuPopup.IsOpen = true;
                _contextMenuPopup.Visibility = Visibility.Visible;
            }
            else if (e.Target == "edit")
            {
                EditContextMenuPanel panel;

                if (_contextMenuPopup.Child is EditContextMenuPanel)
                {
                    panel = _contextMenuPopup.Child as EditContextMenuPanel;
                    if (e.Selection == null)
                    {
                        panel.WillClose();
                    }
                }
                else
                {
                    if (_contextMenuPopup.Child != null)
                    {
                        _contextMenuPopup.IsOpen = false;
                        _contextMenuPopup.Child = null;
                    }

                    _contextMenuPopup.Child = panel = new EditContextMenuPanel();
                }

                panel.Select(e.Selection as Primitive);

                _contextMenuPopup.IsOpen = true;
                _contextMenuPopup.Visibility = Visibility.Visible;
            }
            else if (e.Target == "select")
            {
                SelectContextMenuPanel panel;

                if (_contextMenuPopup.Child is SelectContextMenuPanel)
                {
                    panel = _contextMenuPopup.Child as SelectContextMenuPanel;
                    if (e.Selection == null)
                    {
                        panel.WillClose();
                    }
                }
                else
                {
                    if (_contextMenuPopup.Child != null)
                    {
                        _contextMenuPopup.IsOpen = false;
                        _contextMenuPopup.Child = null;
                    }

                    _contextMenuPopup.Child = panel = new SelectContextMenuPanel();
                }

                panel.Select(e.Selection as List<Primitive>);

                _contextMenuPopup.IsOpen = true;
                _contextMenuPopup.Visibility = Visibility.Visible;
            }
            else if (e.Target == "copypaste")
            {
                CopyPasteContextMenuPanel panel;

                if (_contextMenuPopup.Child is CopyPasteContextMenuPanel)
                {
                    panel = _contextMenuPopup.Child as CopyPasteContextMenuPanel;
                    if (e.Selection == null)
                    {
                        panel.WillClose();
                    }
                }
                else
                {
                    if (_contextMenuPopup.Child != null)
                    {
                        _contextMenuPopup.IsOpen = false;
                        _contextMenuPopup.Child = null;
                    }

                    _contextMenuPopup.Child = panel = new CopyPasteContextMenuPanel();
                }

                panel.Select(e.Selection as List<Primitive>);

                _contextMenuPopup.IsOpen = true;
                _contextMenuPopup.Visibility = Visibility.Visible;
            }
            else if (e.Target == "editgroup")
            {
                EditGroupContextMenuPanel panel;

                if (_contextMenuPopup.Child is EditGroupContextMenuPanel)
                {
                    panel = _contextMenuPopup.Child as EditGroupContextMenuPanel;
                    if (e.Selection == null)
                    {
                        panel.WillClose();
                    }
                }
                else
                {
                    _contextMenuPopup.Child = panel = new EditGroupContextMenuPanel();
                }

                //if (e.Selection is Primitive)
                {
                    panel.Select(e.Selection as Primitive, (int)e.MemberIndex);
                }

                _contextMenuPopup.IsOpen = true;
                _contextMenuPopup.Visibility = Visibility.Visible;
            }
            else if (e.Target == "properties")
            {
                if (e.Selection is Primitive)
                {
                    PropertiesContextPanel panel;

                    if (_contextMenuPopup.Child is PropertiesContextPanel)
                    {
                        panel = _contextMenuPopup.Child as PropertiesContextPanel;
                        panel.Primitive = e.Selection as Primitive;
                    }
                    else
                    {
                        _contextMenuPopup.Child = panel = new PropertiesContextPanel(e.Selection as Primitive);
                    }

                    _contextMenuPopup.IsOpen = true;
                    _contextMenuPopup.Visibility = Visibility.Visible;
                }
                else
                {
                    _contextMenuPopup.IsOpen = false;
                    _contextMenuPopup.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
