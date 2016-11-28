using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Linq;

namespace SkypeAds
{

    public partial class MainWindow : Window
    {

        private string _system32Location = Environment.SystemDirectory;
        private readonly string _systemPath = Path.GetPathRoot(Environment.SystemDirectory);

        public MainWindow()
        {
            InitializeComponent();
            lblMensaje.Visibility = Visibility.Hidden;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            AddToHosts();
            ChangeConfigFile();
            lblMensaje.Visibility = Visibility.Visible;
            btnAplicar.IsEnabled = false;
        }

        private void ProcStartargs(string name, string args)
        {
            try
            {
                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = name,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.GetEncoding(866)
                    }
                };
                proc.Start();
                // ReSharper disable once NotAccessedVariable
                string line = null;
                while (!proc.StandardOutput.EndOfStream)
                {
                    line += Environment.NewLine + proc.StandardOutput.ReadLine();
                }
                proc.WaitForExit();
            }
            // ReSharper disable once UnusedVariable
            catch (Exception ex)
            {

            }
        }

        public void RunCmd(string args)
        {
            ProcStartargs(Path.Combine(_system32Location, @"cmd.exe"), args);
        }

        public void DeleteFile(string filepath)
        {
            RunCmd(string.Format("/c del /F /Q \"{0}\"", filepath));
        }

        private void AddToHosts()
        {
            try
            {
                string[] hostsdomains = {"apps.skype.com", "g.msn.com"};
                var hostslocation = Path.Combine(_system32Location, @"drivers\etc\hosts");
                string hosts = null;
                if (File.Exists(hostslocation))
                {
                    hosts = File.ReadAllText(hostslocation);
                    File.SetAttributes(hostslocation, FileAttributes.Normal);
                    DeleteFile(hostslocation);
                }
                File.Create(hostslocation).Close();
                File.WriteAllText(hostslocation, hosts + Environment.NewLine);
                foreach (var currentHostsDomain in hostsdomains.Where(
                         currentHostsDomain =>
                         hosts != null && hosts.IndexOf(currentHostsDomain, StringComparison.Ordinal) == -1))
                {
                    RunCmd(string.Format("/c echo 0.0.0.0 {0} >> \"{1}\"", currentHostsDomain, hostslocation));
                }
            }
            catch (Exception ex)
            {

            }
            RunCmd("/c ipconfig /flushdns");
        }

        private void ChangeConfigFile()
        {
            //Change content to xml config file
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = Path.Combine(appDataPath, @"Skype\");
            if (Directory.Exists(path))
            {
                // Only get files that begin with the "live"
                string[] dirs = Directory.GetDirectories(path, "live*");
                foreach (string dir in dirs)
                {                  
                    File.SetAttributes(Path.Combine(dir, @"config.xml"), ~FileAttributes.ReadOnly);
                    //Add or edit line for xml config file to hide panels of ads
                    var xmlDoc = XDocument.Load(Path.Combine(dir, @"config.xml"));
                    string[] options = {"AdvertPlaceholder", "AdvertEastRailsEnabled"};
                    foreach (string option in options)
                    {
                        var item = (from ad in xmlDoc.Descendants(option)
                                    select ad).SingleOrDefault();
                        string element = (string)item;
                        if (!String.IsNullOrEmpty(element))
                        {
                            item.SetValue("0");
                        }
                        else
                        {
                            var addItem = (from ad in xmlDoc.Descendants("General")
                                           select ad).SingleOrDefault();
                            addItem.Add(new XElement(option, "0"));
                        }
                    }
                    xmlDoc.Save(Path.Combine(dir, @"config.xml"));
                    //Set the xml config file attributes to readonly to prevent changes
                    File.SetAttributes(Path.Combine(dir, @"config.xml"), FileAttributes.ReadOnly);
                }
            }
        }
    }
}
