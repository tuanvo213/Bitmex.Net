using System;
using System.Windows;
using System.Windows.Data;
using System.Configuration;


namespace Bitmex.NET.Example
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Properties.Settings.Default["IsTest"]= chbIsTest.IsChecked;
            Properties.Settings.Default.Save();
        }
    }
}
