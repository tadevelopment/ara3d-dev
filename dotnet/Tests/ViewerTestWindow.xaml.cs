﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ara3D.Tests
{
    /// <summary>
    /// Interaction logic for ViewerTestWindow.xaml
    /// </summary>
    public partial class ViewerTestWindow : Window
    {
        public ViewerTestWindow()
        {
            InitializeComponent();
        }

        public void FileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
