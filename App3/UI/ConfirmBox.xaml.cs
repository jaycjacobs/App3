using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;


namespace Cirros.Alerts
{
    public sealed partial class ConfirmBox : UserControl
    {
        private bool _shouldContinue = false;
        private string _command = "";

        public ConfirmBox()
        {
            this.InitializeComponent();

            _continueButton.Click += _continueButton_Click;
        }

        void _continueButton_Click(object sender, RoutedEventArgs e)
        {
            _shouldContinue = true;

            if (Parent is Popup)
            {
                ((Popup)Parent).IsOpen = false;
            }
        }

        public bool ShouldContinue
        {
            get
            {
                return _shouldContinue;
            }
        }

        public string Command
        {
            get
            {
                return _command;
            }
            set
            {
                _command = value;
            }
        }

        public string AlertText
        {
            get
            {
                return _alertBox.Text;
            }
            set
            {
                _alertBox.Text = value;
            }
        }

        public string ContinueText
        {
            get
            {
                return _continueButton.Content as string;
            }
            set
            {
                _continueButton.Content = value;
            }
        }
    }
}
