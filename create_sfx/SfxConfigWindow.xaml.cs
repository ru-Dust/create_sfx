using System;
using System.Windows;
using System.IO;
using System.Diagnostics;

namespace create_sfx
{
    /// <summary>
    /// Логика взаимодействия для SfxConfigWindow.xaml
    /// </summary>
    public partial class SfxConfigWindow : Window
    {        
        private string helpFile;

        public SfxConfigWindow(string helpFile)
        {
            InitializeComponent();            
            this.helpFile = helpFile;
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
            try
            {
                File.Delete(System.IO.Path.Combine(Directory.GetCurrentDirectory(), helpFile));
            }
            catch(Exception)
            {

            }            
            this.Owner.Focus();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DataContext = tbxCFG.Text;
            this.Close();
        }
    }
}
