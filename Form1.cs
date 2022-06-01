using BackUpAPP.Config;
using BackUpAPP.CopyProcess;
using BackUpAPP.GetDirectory;
using BackUpAPP.Logger;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace BackUpAPP
{
    public partial class Form1 : Form
    {
        private static ConfigInit? _configinit;
        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // Init Config File
            _configinit = new ConfigInit();
            await _configinit.InitializeAsync();
            
            // Add known Folders in config file
            ConfigInit.Config.Path = KnownFolders.GetPath().ToArray();
            
            // Add folders in listbox
            foreach (var data in ConfigInit.Config.Path)
            {
                var matchingvalues = listBox1.Items.Contains(data);
                if (!matchingvalues)
                    listBox1.Items.Add(data);
            }
            
        }

        // Update config file with new path or removed
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UpdateConfig.UpdateConfigFile(listBox1);
        }

        // Detect delete keycode to remove item in listbox
        private void Form1_KeyPress(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Delete)
            {
                if (listBox1.SelectedIndex != -1)
                {
                    listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                    Thread.Sleep(50);
                }
            }
        }

        // add item
        private void button2_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.InitialDirectory = @"C:\";
                fbd.ShowNewFolderButton = true;

                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    var matchingvalues = listBox1.Items.Contains(fbd.SelectedPath);
                    if(!matchingvalues)
                        listBox1.Items.Add(fbd.SelectedPath);
                    else
                        MessageBox.Show("Path exist already");

                }
            }
        }

        // delete item
        private void button3_Click(object sender, EventArgs e)
        {
            if(listBox1.SelectedIndex != -1)
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
        }

        // Ask to start backup
        public static string backupfolderPath = "";
        private void button1_Click(object sender, EventArgs e)
        {
            const string message = "Do you want start the backup process ?";
            const string caption = "Starting Backup";

            if (listBox1.Items.Count == 0)
                MessageBox.Show("Please select folders befor starting");
            else
            {

                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.InitialDirectory = @"C:\";
                    fbd.ShowNewFolderButton = true;
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        Regex PathValidation = new Regex("^([a-zA-Z]:)?(\\\\[^<>:\"/\\\\|?*]+)+\\\\?$");
                        if (PathValidation.IsMatch(fbd.SelectedPath))
                        {
                            backupfolderPath = fbd.SelectedPath;
                            var msg = MessageBox.Show(message, caption,MessageBoxButtons.YesNo,MessageBoxIcon.Question);

                            if (msg == DialogResult.Yes)
                            {
                                startbachup();                            
                            }
                        }
                        else
                            MessageBox.Show("Invalid path !");
                    }
                }

            }
        }

        // Backup process
        private async void startbachup()
        {
            List<string> tmp = new();
            for (int i = 0; i < listBox1.Items.Count; i++)
                tmp.Add(listBox1.Items[i].ToString());
            await DataCopy.FFCopy(tmp.ToArray());
            RichLogger.Log("Done !");
        }

        // Winget Backup
        private async void button4_Click(object sender, EventArgs e)
        {
            if(backupfolderPath == string.Empty)
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.InitialDirectory = @"C:\";
                    fbd.ShowNewFolderButton = true;
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        Regex PathValidation = new Regex("^([a-zA-Z]:)?(\\\\[^<>:\"/\\\\|?*]+)+\\\\?$");
                        if (PathValidation.IsMatch(fbd.SelectedPath))
                        {
                            backupfolderPath = fbd.SelectedPath;
                        }
                        else
                            MessageBox.Show("Invalid path !");
                    }
                }
            }
            await Exec("cmd.exe", $"winget export -o {backupfolderPath}\\WinGet.json");
        }

        // Chocolatey Backup
        private async void button5_Click(object sender, EventArgs e)
        {
            if (backupfolderPath == string.Empty)
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.InitialDirectory = @"C:\";
                    fbd.ShowNewFolderButton = true;
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        Regex PathValidation = new Regex("^([a-zA-Z]:)?(\\\\[^<>:\"/\\\\|?*]+)+\\\\?$");
                        if (PathValidation.IsMatch(fbd.SelectedPath))
                        {
                            backupfolderPath = fbd.SelectedPath;
                        }
                        else
                            MessageBox.Show("Invalid path !");
                    }
                }
            }
            
            // Chocolatey upgrade
            await Exec("cmd.exe", $"choco upgrade chocolatey -y");
            
            // Make a Chocolatey export 
            await Exec("cmd.exe", $"choco export --output-file-path=\"'{backupfolderPath}\\ChocolateyBackup.config'\"");
        }

        // Exec CMD
        private static Task Exec(string filename, string cmd)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.FileName = filename;
            startInfo.Arguments = $"/C " + cmd;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            return Task.CompletedTask;
        }
        
        // Winget Import .JSON
        private async void button6_Click(object sender, EventArgs e)
        {
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "json files (*.json)|*.json";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                    await Exec("cmd.exe", $"winget -i {filePath} --ignore-unavailable --ignore-versions --accept-package-agreements --accept-source-agreements --verbose-logs");
                }
            }
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "config files (*.config)|*.config";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filePath = openFileDialog.FileName;
                    await Exec("powershell.exe", $"choco install {filePath} -y -n");
                }
            }
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            await Exec("powershell.exe", "Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iwr https://community.chocolatey.org/install.ps1 -UseBasicParsing | iex");
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            await Exec("cmd.exe", $"start ms-windows-store://pdp/?ProductId=9NBLGGH4NNS1");
        }
    }
}