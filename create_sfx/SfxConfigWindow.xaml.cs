using System;
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
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace create_sfx
{
    /// <summary>
    /// Логика взаимодействия для SfxConfigWindow.xaml
    /// </summary>
    public partial class SfxConfigWindow : Window
    {
        private Window parentWin;
        public SfxConfigWindow(Window parentWin)
        {
            InitializeComponent();
            this.parentWin = parentWin;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            tbxCFG.Text = this.DataContext.ToString();
            tbxCFG.Focus();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            string pathToCHM = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "7zSD_RU.chm");
            pathToCHM = "mk:@MSITStore:" + pathToCHM + "::/examples.html";
            Process.Start("hh.exe", pathToCHM);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Owner.Focus();
        }
    }
}
