using Cirros;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KT22.Console
{
    public class ModalCommand
    {
        string _prompt = "";
        //string _response = "";

        public ModalCommand()
        {
        }

        public ModalCommand(string prompt)
        {
            _prompt = prompt;
        }

        public string Prompt { get { return _prompt; } set { _prompt = value; } }

        public virtual bool Finished { get { return true; } }

        public virtual async Task DoCommand(string command)
        {
            await Task.Delay(1);
        }
    }

    public class ClearCommand : ModalCommand
    {
        bool _finished = false;

        public ClearCommand() : base()
        {
            if (Globals.ActiveDrawing != null)
            {
                if (Globals.ActiveDrawing.IsModified == false)
                {
                    Globals.ActiveDrawing.Clear();
                    _finished = true;
                }
                else
                {
                    Prompt = "Do you want to save your changes (yes/no)? ";
                }
            }
            else
            {
                _finished = true;
            }
        }

        public override bool Finished
        {
            get
            {
                return _finished;
            }
        }

        public override async Task DoCommand(string command)
        {
            bool okToClear = false;

            if (Globals.ActiveDrawing != null)
            {
                if (command == "no")
                {
                    okToClear = true;
                    _finished = true;
                }
                else if (command == "yes")
                {
                    string name = null;

                    Prompt = "";

                    if (!string.IsNullOrEmpty(FileHandling.CurrentDrawingName) && FileHandling.CurrentDrawingName.Length > 4)
                    {
                        name = FileHandling.CurrentDrawingName.Substring(0, FileHandling.CurrentDrawingName.Length - 4);
                    }

                    if (string.IsNullOrEmpty(name))
                    {
                        // save as

                        if (await FileHandling.SaveDrawingAsAsync())
                        {
                            okToClear = true;
                            _finished = true;
                        }
                        else
                        {
                            // cancelled - don't clear
                            _finished = true;
                        }
                    }
                    else
                    {
                        // save

                        await FileHandling.SaveDrawingAsync();
                        _finished = true;
                    }
                }
                else
                {
                    // invalid response
                    _finished = false;
                }
            }
            else
            {
                // no active drawing - I guess we're done
                _finished = true;
           }

            if (okToClear)
            {
                Globals.ActiveDrawing.Clear();
            }
        }
    }
}
