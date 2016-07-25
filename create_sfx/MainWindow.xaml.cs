using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace create_sfx
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> files;        
        string tmpPath = "pack.7z";
        string sfxCfg;
        string outFileName = "";        
        string RLO = "\u202e";
        string LRO = "\u202d";

        SfxConfigWindow sfxWindow;

        public MainWindow()
        {
            EmbeddedAssembly.Load("create_sfx.Resources.SevenZipSharp.dll", "SevenZipSharp.dll");
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            InitializeComponent();

            files = new List<string>();            
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //return EmbeddedAssembly.Get(args.Name);
            //return LoadManaged();
            return LoadUnManagedDll();
        }

        private static Assembly LoadManagedDll()
        {
            byte[] ba = null;
            string resource = "create_sfx.Resources.SevenZipSharp.dll";
            Assembly curAsm = Assembly.GetExecutingAssembly();
            using (Stream stm = curAsm.GetManifestResourceStream(resource))
            {
                ba = new byte[(int)stm.Length];
                stm.Read(ba, 0, (int)stm.Length);

                return Assembly.Load(ba);
            }
        }

        private static Assembly LoadUnManagedDll()
        {
            // Get the byte[] of the DLL
            byte[] ba = null;
            string resource = "create_sfx.Resources.SevenZipSharp.dll";
            Assembly curAsm = Assembly.GetExecutingAssembly();
            using (Stream stm = curAsm.GetManifestResourceStream(resource))
            {
                ba = new byte[(int)stm.Length];
                stm.Read(ba, 0, (int)stm.Length);
            }

            bool fileOk = false;
            string tempFile = "";

            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                // Get the hash value of the Embedded DLL
                string fileHash = BitConverter.ToString(sha1.ComputeHash(ba)).Replace("-", string.Empty);

                // The full path of the DLL that will be saved
                tempFile = Path.GetTempPath() + "System.Data.SQLite.dll";

                // Check if the DLL is already existed or not?
                if (File.Exists(tempFile))
                {
                    // Get the file hash value of the existed DLL
                    byte[] bb = File.ReadAllBytes(tempFile);
                    string fileHash2 = BitConverter.ToString(sha1.ComputeHash(bb)).Replace("-", string.Empty);

                    // Compare the existed DLL with the Embedded DLL
                    if (fileHash == fileHash2)
                    {
                        // Same file
                        fileOk = true;
                    }
                    else
                    {
                        // Not same
                        fileOk = false;
                    }
                }
                else
                {
                    // The DLL is not existed yet
                    fileOk = false;
                }
            }

            // Create the file on disk
            if (!fileOk)
            {
                System.IO.File.WriteAllBytes(tempFile, ba);
            }

            // Load it into memory    
            return Assembly.LoadFile(tempFile);
        }   

        private void btnMake_Click(object sender, RoutedEventArgs e)
        {            
            if(lvFiles.Items == null || cmbxExecFileBack.Text.Length == 0)
            {
                return;
            }
            CompressByManaged7z(files.ToArray(), tmpPath);
            CreateSFX(tmpPath, tbTargetName.Text);
            Process.Start("explorer.exe", @"/select, " + Path.Combine(Directory.GetCurrentDirectory(), tbTargetName.Text));
        }

        private void CompressByManaged7z(string[] srcfiles, string outFile)
        {
            string dll = string.Empty;
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion"))
            {
                if (key != null)
                {
                    Object o = key.GetValue("BuildLabEx");
                    if (o != null)
                    {
                        if ((o as String).ToLower().Contains("amd64"))
                        {
                            CreateFileFromResource("create_sfx.Resources.7zx64.dll", "7zx64.dll");
                            dll = "7zx64.dll";
                        }
                        else
                        {
                            CreateFileFromResource("create_sfx.Resources.7zx86.dll", "7zx86.dll");
                            dll = "7zx86.dll";
                        }
                    }
                }
            }

            SevenZip.SevenZipCompressor.SetLibraryPath(Path.Combine(Directory.GetCurrentDirectory(), dll));
            SevenZip.SevenZipCompressor compressor = new SevenZip.SevenZipCompressor();
            compressor.ArchiveFormat = SevenZip.OutArchiveFormat.SevenZip;
            compressor.CompressionLevel = SevenZip.CompressionLevel.Ultra;
            compressor.CompressionMethod = SevenZip.CompressionMethod.Lzma2;
            compressor.CompressionMode = SevenZip.CompressionMode.Create;
            compressor.CompressFiles(outFile, srcfiles);
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), dll));
        }

        private void CreateSFX(string srcFile, string dstFile)
        {
            using (Stream OutFileStream = new FileStream(dstFile, FileMode.OpenOrCreate, FileAccess.Write))
            {
                CreateFileFromResource("create_sfx.Resources.7z.sfx", "7z.sfx");

                if(tbIcon.Text.Length>0)
                {
                    IconChanger.ChangeIcon("7z.sfx", tbIcon.Text);                    
                }

                byte[] sfxMod = File.ReadAllBytes("7z.sfx");
                OutFileStream.Write(sfxMod, 0, sfxMod.Length);
                               
                byte[] sfxConf = Encoding.UTF8.GetBytes(sfxCfg);
                OutFileStream.Write(sfxConf, 0, sfxConf.Length);

                using (Stream arhive7z = File.OpenRead(Path.Combine(Directory.GetCurrentDirectory(), tmpPath)))
                {
                    arhive7z.CopyTo(OutFileStream);
                }                
            }
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "7z.sfx"));
            File.Delete(Path.Combine(Directory.GetCurrentDirectory(), tmpPath));
        }

        private void GenerateSfxCFG()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(";!@Install@!UTF-8!");                    //header
            sb.AppendLine();

            if (cmbxExecFileFront.Text.Length > 0)
            {
                sb.Append(string.Format(@"RunProgram=""forcenowait:hidcon:\""{0}\""""",
                            Path.GetFileName(cmbxExecFileFront.Text)));
                sb.AppendLine();
            }
            sb.Append(string.Format(@"RunProgram=""forcenowait:hidcon:\""{0}\""""",
                        Path.GetFileName(cmbxExecFileBack.Text)));
            sb.AppendLine();

            sb.Append(@"GUIMode=""2""");                        //1 - show form, 2 - silentMode, 
            sb.AppendLine();
            sb.Append(@"SelfDelete=""1""");                     //archive will be removed after extracting
            sb.AppendLine();
            sb.Append(";!@InstallEnd@!");                       //bottom
            sfxCfg = sb.ToString();           
        }

        private static void CreateFileFromResource(string fullFileNamePath, string FileName)
        {
            Assembly curAsm = Assembly.GetExecutingAssembly();
            using (Stream stm = curAsm.GetManifestResourceStream(fullFileNamePath))
            {
                byte[] assamblyBytes = new byte[stm.Length];
                stm.Read(assamblyBytes, 0, assamblyBytes.Length);
                using (Stream newDllFile = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), FileName), FileMode.OpenOrCreate, FileAccess.Write))
                {
                    newDllFile.Write(assamblyBytes, 0, assamblyBytes.Length);
                }
            }
        }

        private void BtnChooseIcon_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "ico (*.ico)|*.ico";
            ofd.Multiselect = false;
            ofd.InitialDirectory = Directory.GetCurrentDirectory();
            ofd.RestoreDirectory = true;
            
            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                tbIcon.Text = ofd.FileName;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);
            
            foreach (string i in items as string[])
            {
                if (File.Exists(i) && !Exist(i))
                {
                    files.Add(i);

                    string[] exts = i.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    string ext = exts[exts.Length - 1].ToLower();
                    if (ext == "exe" ||
                        ext == "js" ||
                        ext == "wsf" ||
                        ext == "cmd" ||
                        ext == "bat" ||
                        ext == "jar" ||
                        ext == "com" ||
                        ext == "vbs" ||
                        ext == "msi" ||
                        ext == "py")
                    {
                        cmbxExecFileBack.Items.Add(i);
                        cmbxExecFileFront.Items.Add(i);
                    }
                    else
                    {
                        cmbxExecFileFront.Items.Add(i);
                    }
                }
            }
            lvFiles.ItemsSource = null;
            lvFiles.ItemsSource = files;                
        }

        private bool Exist(string f)
        {
            return cmbxExecFileFront.Items.Contains(f);
        }

        private void btnFineTune_Click(object sender, RoutedEventArgs e)
        {
            if (cmbxExecFileBack.SelectedItem.ToString() != string.Empty)
            {
                sfxWindow = new SfxConfigWindow("7zSD_RU.chm");
                sfxWindow.Owner = this;
                sfxWindow.DataContext = sfxCfg;
                CreateFileFromResource("create_sfx.Resources.7zSD_RU.chm", "7zSD_RU.chm");
                sfxWindow.Show();                
            }
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if(sfxWindow != null)
            {
                sfxCfg = sfxWindow.DataContext.ToString();                
            }
        }

        private void cmbxExecFileFront_DropDownClosed(object sender, EventArgs e)
        {
            if (((ComboBox)sender).SelectedItem == null)
            {
                return;
            }
            if (((ComboBox)sender).SelectedItem.ToString() != string.Empty)
            {
                GenerateSfxCFG();                
            }
        }

        private void cmbxExecFileBack_DropDownClosed(object sender, EventArgs e)
        {
            if (((ComboBox)sender).SelectedItem == null)
            {
                return;
            }
            if (((ComboBox)sender).SelectedItem.ToString() != string.Empty)
            {
                GenerateSfxCFG();
                btnFineTune.IsEnabled = true;
            }
            else
            {
                btnFineTune.IsEnabled = false;
            }
        }

        private void chbxReverse_Checked(object sender, RoutedEventArgs e)
        {
            if (tbTargetName.Text.Length == 0)
                return;

            outFileName = "";
            string[] pth = tbTargetName.Text.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            outFileName += LRO;
            for (int i=0;i<pth.Length-2;i++)
            {
                outFileName += pth[i];
            }
            char[] secExt = pth[pth.Length - 2].ToCharArray();
            Array.Reverse(secExt);
            outFileName += "." + RLO + Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(secExt)) + "." + pth[pth.Length - 1];
            tbTargetName.Text = outFileName;
            tbTargetName.IsEnabled = false;
        }

        private void chbxReverse_Unchecked(object sender, RoutedEventArgs e)
        {
            if (tbTargetName.Text.Length == 0)
                return;

            outFileName = "";
            string[] pth = tbTargetName.Text.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pth.Length - 2; i++)
            {
                outFileName += pth[i];
            }
            char[] secExt = pth[pth.Length - 2].ToCharArray();
            Array.Reverse(secExt);
            outFileName += "." + Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(secExt)) + "." + pth[pth.Length - 1];
            tbTargetName.Text = outFileName;
            tbTargetName.IsEnabled = true;
        }

        private void lvFiles_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (lvFiles.Items.Count == 0)
                return;

            if(e.Key == System.Windows.Input.Key.Delete && lvFiles.SelectedIndex != -1)
            {                
                files.RemoveAt(lvFiles.SelectedIndex);
                lvFiles.ItemsSource = null;
                lvFiles.ItemsSource = files;
            }
        }
    }
}
