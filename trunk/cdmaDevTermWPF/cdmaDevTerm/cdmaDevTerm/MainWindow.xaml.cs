﻿//Copyright 2012 Dillon Graham
//GPL v3 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using cdmaDevLib;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;

namespace cdmaDevTerm
{
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Elysium.Theme.Controls.Window
    {
        private ScriptEngine _engine;
        private ScriptScope _scope;

        private string _scriptImports = @"#imports
import clr
from cdmaDevLib import *
from System import Array,Byte,String
";

        private string cdmaDevTermSampleScript = @"#
#cdmaDevTerm sample ironPython script
#copyright 2012 DG, chromableedstudios.com
#
#warning: 
# the cdmaDevLib api is in unstable development 
# and names may change from time to time
#
#cdmaTerm.Connect(""COM9"")
cdmaTerm.Connect(phone.ComPortName)
cdmaTerm.ReadAllNam()
cdmaTerm.ReadNv(NvItems.NvItems.NV_DS_MIP_ACTIVE_PROF_I)
#cdmaTerm.WriteNv(NvItems.NvItems.NV_SEC_CODE_I, Array[Byte]((0x30, 0x30, 0x30, 0x30, 0x30, 0x30)))
cdmaTerm.SendSpc(""000000"")
cdmaTerm.ReadNvList(""900-915"",""nvOut.txt"")
q.Run()";


        public MainWindow()
        {
            InitializeComponent();

            _engine = Python.CreateEngine();
            _scope = _engine.CreateScope();

            var runtime = _engine.Runtime;
            runtime.LoadAssembly(typeof(String).Assembly);
            runtime.LoadAssembly(typeof(Array).Assembly);
            runtime.LoadAssembly(typeof(cdmaDevLib.cdmaTerm).Assembly);
            _scope.SetVariable("phone", cdmaTerm.thePhone);
            _scope.SetVariable("q", cdmaTerm.Q);

            RunScript(_scriptImports);
            CodeTextEditor.SyntaxHighlighting =
            HighlightingLoader.Load(new XmlTextReader("ICSharpCode.PythonBinding.Resources.Python.xshd"),
            HighlightingManager.Instance);
            CodeTextEditor.Text = cdmaDevTermSampleScript;

            #region theme
                dynamic accent;
                switch (Properties.Settings.Default.Accent.ToLower())
                {
                    case "lime":
                        accent = Elysium.Theme.AccentColors.Lime;
                        break;
                    case "blue":
                        accent = Elysium.Theme.AccentColors.Blue;
                        break;
                    case "brown":
                        accent = Elysium.Theme.AccentColors.Brown;
                        break;
                    case "green":
                        accent = Elysium.Theme.AccentColors.Green;
                        break;
                    case "magenta":
                        accent = Elysium.Theme.AccentColors.Magenta;
                        break;
                    case "orange":
                        accent = Elysium.Theme.AccentColors.Orange;
                        break;
                    case "pink":
                        accent = Elysium.Theme.AccentColors.Pink;
                        break;
                    case "purple":
                        accent = Elysium.Theme.AccentColors.Purple;
                        break;
                    case "red":
                        accent = Elysium.Theme.AccentColors.Red;
                        break;
                    case "viridian":
                        accent = Elysium.Theme.AccentColors.Viridian;
                        break;
                    default:
                        accent = Elysium.Theme.AccentColors.Lime;
                        break;
                }

                switch (Properties.Settings.Default.ThemeType)
                {
                    case Elysium.Theme.ThemeType.Dark:
                        Elysium.Theme.ThemeManager.Instance.Dark(accent);
                        break;
                    case Elysium.Theme.ThemeType.Light:
                        Elysium.Theme.ThemeManager.Instance.Light(accent);
                        break;
                    default:
                        Elysium.Theme.ThemeManager.Instance.Dark(accent);
                        break;
                }
            #endregion 

            this.DataContext = cdmaTerm.thePhone;
            cdmaTerm.initSixteenDigitCodes(AppDomain.CurrentDomain.BaseDirectory + "16digitpass.txt");
            cdmaTerm.thePhone.PrlFilename = Properties.Settings.Default.LastPrl;
            cdmaTerm.GetComs();
            comBox.SelectedIndex = 0;

        }
      
        ~MainWindow()
        {
            Properties.Settings.Default.LastPrl = cdmaTerm.thePhone.PrlFilename;
            Properties.Settings.Default.Save();
        }

        private void runScript_Click(object sender, RoutedEventArgs e)
        {
            
            var code = CodeTextEditor.Text;
            RunScript(code);
        }
        private void RunScript(string code)
        {
            try
            {
                var source = _engine.CreateScriptSourceFromString(code, SourceCodeKind.Statements);
                source.Execute(_scope);

            }
            catch (Exception ex)
            {
                var eo = _engine.GetService<ExceptionOperations>();
                var error = eo.FormatException(ex);

                MessageBox.Show(error, "There was an Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Boolean result = true;
            cdmaTerm.Connect(cdmaTerm.thePhone.ComPortName);

            if (Properties.Settings.Default.AutoModeOffline)
            {
                cdmaTerm.Q.Clear();
                cdmaTerm.ModeSwitch(Qcdm.Mode.MODE_RADIO_OFFLINE);
                result = cdmaTerm.Q.Run();
            }
            if (result)
            {
                cdmaTerm.AddAllEvdo();
                cdmaTerm.AddNv(NvItems.NvItems.NV_MEID_I);
                cdmaTerm.AddQc(Qcdm.Cmd.DIAG_ESN_F);
                cdmaTerm.AddNv(NvItems.NvItems.NV_DIR_NUMBER_I);
                cdmaTerm.AddNv(NvItems.NvItems.NV_SEC_CODE_I);
                cdmaTerm.AddNv(NvItems.NvItems.NV_LOCK_CODE_I);
                cdmaTerm.AddNv(NvItems.NvItems.NV_HOME_SID_NID_I);
                cdmaTerm.AddNv(NvItems.NvItems.NV_NAM_LOCK_I);
                result = result && cdmaTerm.Q.Run();
                if(result)
                cdmaTerm.ReadMIN1();
            }
        }

        private void tabControl1_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (Wiki.IsSelected)
            {
                wikiWebBrowser.Navigate(new Uri("http://code.google.com/p/cdmaworkshoptool/wiki/cdmaWorkshopTool?tm=6"));
            }
        }

        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            cdmaTerm.GetComs();
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            cdmaTerm.Disconnect();
        }

        private void readSpc_Click(object sender, RoutedEventArgs e)
        {
            cdmaTerm.readSpcFromPhone(cdmaTerm.thePhone.SpcReadType);
            cdmaTerm.Q.Run(); 
        }

        #region keyPress
            private void key1_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.One);
            }

            private void key2_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Two);
            }

            private void key3_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Three);
            }

            private void key4_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Four);
            }

            private void key5_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Five);
            }

            private void key6_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Six);
            }

            private void key7_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Seven);
            }

            private void key8_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Eight);
            }

            private void key9_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Nine);
            }

            private void keyStar_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Star);
            }

            private void key0_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Zero);
            }

            private void keyPound_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.Pound);
            }
            private void keySend_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.SendKey);
            }

            private void keyEnd_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.KeyPress(cdmaTerm.phoneKeys.EndKey);
            }
        #endregion

            private void sendSpc_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.Q.Clear();
                cdmaTerm.SendSpc(cdmaTerm.thePhone.Spc);
                cdmaTerm.Q.Run();
            }

            private void SendSP_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.SendA16digitCode(cdmaTerm.thePhone.SixteenDigitSP);
            }

            private void writeSpc_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.Q.Clear();
                cdmaTerm.WriteSpc(cdmaTerm.thePhone.Spc);
                cdmaTerm.Q.Run();
            }

            private void SendTerm_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.SendTerminalCommand(cdmaTerm.thePhone.TermCommand);
            }

            private void writeEvdo_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.Q.Clear();
                cdmaTerm.WriteEvdo(cdmaTerm.thePhone.Username, cdmaTerm.thePhone.Password);
                cdmaTerm.Q.Run();
            }

            private void sendModeSwitch_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.Q.Clear();
                cdmaTerm.ModeSwitch(cdmaTerm.thePhone.ModeChangeType);
                cdmaTerm.Q.Run();
            }

            private void sendPrl_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.Q.Clear();
                cdmaTerm.SendPrlFile(cdmaTerm.thePhone.PrlFilename);
                cdmaTerm.Q.Run();
            }

            private void ChoosePrl_Click(object sender, RoutedEventArgs e)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select PRL",
                    DefaultExt = ".prl",
                    Filter = "PRL-file (.prl)|*.prl",
                    CheckFileExists = true
                };
                if((bool)dlg.ShowDialog())
                cdmaTerm.thePhone.PrlFilename = dlg.FileName;
            }

            private void writeNam_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.Q.Clear();
                cdmaTerm.updatePhoneFromViewModel();
            }

            private void readPrl_Click(object sender, RoutedEventArgs e)
            {
                var p = new cdmaDevLib.Prl();
                p.DownloadPrl("prl.prl");
                cdmaTerm.Q.Run();
            }

            private void readPhone_Click(object sender, RoutedEventArgs e)
            {
                Boolean result = true;
                cdmaTerm.AddAllEvdo();
                cdmaTerm.AddNv(NvItems.NvItems.NV_MEID_I);
                cdmaTerm.AddQc(Qcdm.Cmd.DIAG_ESN_F);
                cdmaTerm.AddNv(NvItems.NvItems.NV_DIR_NUMBER_I);
                cdmaTerm.AddNv(NvItems.NvItems.NV_SEC_CODE_I);
                cdmaTerm.AddNv(NvItems.NvItems.NV_LOCK_CODE_I);
                cdmaTerm.AddNv(NvItems.NvItems.NV_HOME_SID_NID_I);
                cdmaTerm.AddNv(NvItems.NvItems.NV_NAM_LOCK_I);
                result = result && cdmaTerm.Q.Run();
                if (result)
                    cdmaTerm.ReadMIN1();
            }

            private void SendSpcZerosMenuItem_Click_1(object sender, RoutedEventArgs e)
            {
                cdmaTerm.SendSpc("000000");
                cdmaTerm.Q.Run();
            }

            private void WriteSpcZerosMenuItem_Click_1(object sender, RoutedEventArgs e)
            {
                cdmaTerm.WriteSpc("000000");
                cdmaTerm.Q.Run();
            }

            private void ReadNvItem_Click(object sender, RoutedEventArgs e)
            {  
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save NV Read as...",
                    DefaultExt = ".txt",
                    Filter = "Text (.txt)|*.txt",
                };
                if ((bool)dlg.ShowDialog())
                    cdmaTerm.ReadNvList(ReadNvItemTextbox.Text, dlg.FileName);
            }

            private void copyEsnConverted_Click_1(object sender, RoutedEventArgs e)
            {
                Clipboard.SetText(cdmaDevLib.esnConverter.ConversionSub(cdmaTerm.thePhone.Esn));
            }

            private void copyMeidConverted_Click_1(object sender, RoutedEventArgs e)
            {
                Clipboard.SetText(cdmaDevLib.esnConverter.ConversionSub(cdmaTerm.thePhone.Meid));
            }

            private void sendMotoUnlock_Click(object sender, RoutedEventArgs e)
            {
                cdmaTerm.UnlockMotoEvdo();
            }

            private void RelockMoto_Click_1(object sender, RoutedEventArgs e)
            {
                cdmaTerm.RelockMotoEvdo();
            }

           

          



    }
}
