﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Cirros.Drawing
{
    public sealed partial class TriangleFirstRunPanel : UserControl
    {
        public TriangleFirstRunPanel()
        {
            this.InitializeComponent();
        }

        private async void Clicked(object sender, PointerRoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("http://drawingboardapp.com/help/triangle"));
        }
    }
}