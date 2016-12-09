﻿/*
 * Project: 3devoUpdate - uploader for filament extuder
 * Author: [Zak Kemble, contact@zakkemble.co.uk], [Roland van Eeden, 3devo.com]
 * Copyright: (C) 2013 by Zak Kemble, (C) 2016 by 3devo
 * License: GNU GPL v3 (see License.txt)
 * Web: http://blog.zakkemble.co.uk/avrdudess-a-gui-for-avrdude/
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Media;
using System.Windows.Forms;
using System.Management;

namespace avrdudess
{
    public partial class Form1 : Form
    {
        
        public bool ready = false;

        private const string WEB_ADDR_FUSE_SETTINGS = "http://www.engbedded.com/fusecalc";
        private const string PORT_DESCRIPTION_FE01 = "Next 1.0 filament extruder";
        private const string PORT_NOT_SELECTED = "No COM port selected...\n";
        private const string HEX_FILE_NOT_SELECTED = "No hex file selected...\n";
        private const string NO_EXTRUDER_CONNECTED = "No filament extruder connected.." 
                                                   + "\t Or 3devo driver not installed..\n";
        private const string READY_FOR_UPLOADING = "Ready for uploading!\n";
        private ToolTip ToolTips;
        private Avrdude avrdude;
        private Presets presets;
        private CmdLine cmdLine;
        private bool drag               = false;
        private bool port_selected      = false;
        private bool hex_file_selected  = false;
        private string presetToLoad;
        private string selected_port_name = "";
        private Point dragStart;     
        
       
        #region Control getters and setters

        public Programmer prog;
        public MCU mcu;
        public string baudRate;
        public bool force;
        public bool disableVerify;
        public bool disableFlashErase;
        public bool eraseFlashAndEEPROM;
        public bool doNotWrite;
        public string cmdBox;
        public string flashFileFormat;
        public string flashFileOperation;
        public byte verbosity;
        public string port
        {
            get { return cmbPort.Text.Trim(); }
            set { cmbPort.Text = value; }
        }
        public string flashFile
        {
            get { return txtFlashFile.Text.Trim(); }
            set { txtFlashFile.Text = value; }
        }
        #endregion

        #region Initializing

        public Form1(string[] args)
        {
            InitializeComponent();
          
            if (args.Length > 0)
                presetToLoad = args[0];

            Icon = AssemblyData.icon;
            setWindowTitle();

            // Make sure console is the right size
            Form1_Resize(this, null);

            MaximumSize = new Size(Size.Width, int.MaxValue);
            MinimumSize = new Size(Size.Width, Height - this.txtStatusInfo.Height);

            Util.UI = this;
            Util.consoleSet(txtStatusInfo);            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Debug info
            if (Constants.DEBUG_STATUS == true)
            {
                System.Diagnostics.Debug.WriteLine("Start initializing....");
            }

            // Load saved configuration
            Config.Prop.load();

            // Persist window location across sessions
            // Credits:
            // gl.tter
            if (Config.Prop.windowLocation != null && Config.Prop.windowLocation != new Point(0, 0))
                Location = Config.Prop.windowLocation;

            cmdLine = new CmdLine(this);
            avrdude = new Avrdude();

            avrdude.OnProcessStart += avrdude_OnProcessStart;
            avrdude.OnProcessEnd += avrdude_OnProcessEnd;
            avrdude.OnVersionChange += avrdude_OnVersionChange;
            avrdude.OnDetectedMCU += avrdude_OnDetectedMCU;

            avrdude.load();

            // Setup memory files/usage bars
            enableClientAreaDrag(Controls);
                        
            // Drag and drop flash file
            gbFlashFile.AllowDrop = true;
            gbFlashFile.DragEnter += event_DragEnter;
            gbFlashFile.DragDrop += event_DragDrop;
            
            // Flash file
            openFileDialog1.Filter = "Hex files (*.hex)|*.hex";
            openFileDialog1.Filter += "|All files (*.*)|*.*";
            openFileDialog1.CheckFileExists = false;
            openFileDialog1.FileName = "";
            openFileDialog1.Title = "Open flash file";

            // Tool tips
            ToolTips = new ToolTip();
            ToolTips.ReshowDelay = 100;
            ToolTips.UseAnimation = false;
            ToolTips.UseFading = false;
            ToolTips.SetToolTip(cmbPort, "Set COM port");
            ToolTips.SetToolTip(txtFlashFile, "Hex file (.hex)" + Environment.NewLine + "You can also drag and drop files here");
            ToolTips.Active = Config.Prop.toolTips;

            // Load saved presets
            // EDITED: load 3Devo presets and set cmd line for avrdude
            presets = new Presets(this);
            presets.load_3Devo();
            ready = false;
            btnUpload.Enabled = false;

            // Update serial ports etc
            //cmbPort.DropDown += cbPort_DropDown;
            update_com_ports();
            selected_port_name = "";           
            SerialPortService.PortsChanged += (sender1, changedArgs) => update_com_ports();

            // Debug info
            if (Constants.DEBUG_STATUS == true)
            {
                System.Diagnostics.Debug.WriteLine("Ready: done initializing\n");
            }
        }

        // Show AVRDUDE version etc
        private void setWindowTitle()
        {
            string avrdudeVersion = (avrdude != null) ? avrdude.version : "";
            if (avrdudeVersion == "")
                avrdudeVersion = "?";
            Text = String.Format("{0} {1}.{2} ({3})", AssemblyData.title, AssemblyData.version.Major, AssemblyData.version.Minor, avrdudeVersion);
        }
               
        // Click and drag (almost) anywhere to move window
        private void enableClientAreaDrag(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c is GroupBox || c is Label || c is PictureBox)
                {
                    c.MouseDown += Form1_MouseDown;
                    c.MouseUp   += Form1_MouseUp;
                    c.MouseMove += Form1_MouseMove;
                    enableClientAreaDrag(c.Controls);
                }
            }
        }
        #endregion

        #region Avrdude status

        // AVRDUDE process has started
        private void avrdude_OnProcessStart(object sender, EventArgs e)
        {
            tssStatus.Text = "AVRDUDE is running...";
        }

        // AVRDUDE process has ended
        private void avrdude_OnProcessEnd(object sender, EventArgs e)
        {
            SerialPort connectPort;
            if (selected_port_name.Length > 0)
            {
                connectPort = new SerialPort(selected_port_name);
                connectPort.Close();
            }    
            tssStatus.Text = "Ready";
        }

        // Found MCU
        private void avrdude_OnDetectedMCU(object sender, DetectedMCUEventArgs e)
        {
            if (e.mcu != null)
            {
                System.Diagnostics.Debug.WriteLine("Detected " + e.mcu.signature + " = " + e.mcu.fullName);

                Invoke(new MethodInvoker(() =>
                {
                    // Select the MCU that was found
                    //cmbMCU.SelectedItem = e.mcu;
                }));
            }
            else
            {
                // Failed to detect MCU, show log so we can see what went wrong
                System.Diagnostics.Debug.WriteLine("Unable to detect MCU");
                System.Diagnostics.Debug.WriteLine("\n" + avrdude.log );
            }
        }

        // Version change
        private void avrdude_OnVersionChange(object sender, EventArgs e)
        {
            setWindowTitle();
        }
        #endregion

        #region Extra functions

        /* Driver installed
         * 
         */
        /*
        private void driver_installed()
        {
            ManagementObjectCollection drivers;

            System.Diagnostics.Debug.WriteLine("Searching for driver...");

            System.Management.SelectQuery query = new System.Management.SelectQuery);
            //query.Condition = "Name = 3devo serial driver";
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(query);
            drivers = searcher.Get();

            System.Diagnostics.Debug.WriteLine("\ndrivers: " + drivers.Count + "\n");

            foreach (ManagementObject driver in drivers)
            {
                System.Diagnostics.Debug.WriteLine( driver );                
            }
            
            
            if (drivers.Count > 0)
            {
                Console.WriteLine("Driver exists.");
                //return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Driver could not be found.");
                //return false;
            } 
            
        }*/


        /* Update connected COM ports 
         * - Only store COM ports connected with filament extruder!  
         * 
         */
        private void update_com_ports()
        {
            if (this.cmbPort.InvokeRequired)
            {
                BeginInvoke(new Action(() => this.cmbPort.Items.Clear()));
            }
            else
            {
                this.cmbPort.Items.Clear();
            }
            ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_SerialPort");

            foreach (ManagementObject queryObj in searcher.Get())
            {
                if (queryObj["Description"].ToString().Equals(PORT_DESCRIPTION_FE01))
                {
                    if (this.cmbPort.InvokeRequired)
                    {
                        BeginInvoke(new Action(() => this.cmbPort.Items.Add(queryObj["DeviceID"].ToString())));                        
                    }
                    else
                    {
                        this.cmbPort.Items.Add(queryObj["DeviceID"].ToString());
                    }
                }
            }
            //System.Diagnostics.Debug.Write( "update_com_ports(): " + this.cmbPort.Items.Count + "\n");

            // update status info text
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => update_status_info()));
            }
            else
            {
                update_status_info();
            }                   
                     
        }

        /* Update the statusinfo field
         *  -  no filament extruder connected
         *  -  com port selection
         *  -  hex file set
         */
        private void update_status_info()
        {
            // Clear status info text
            txtStatusInfo.Clear();

            /* COM port connection */
            // No extruder connected/found
            if (cmbPort.Items.Count == 0)
            {
                txtStatusInfo.AppendText(NO_EXTRUDER_CONNECTED);
                port = "";
                selected_port_name = port;
                port_selected = false;
            }
            // 1 or more COM ports connected with filament extruder
            else
            {                
                // Only 1 connected filament extruder found
                if (cmbPort.Items.Count == 1)
                {
                    selected_port_name = cmbPort.Items[0].ToString();
                    port = selected_port_name;
                    port_selected = true;
                }
                // More than 1 connected filament extruder found
                else if (selected_port_name.Length == 0)
                {
                    port_selected = false;                    
                }
                else
                {
                    port_selected = true;
                    port = selected_port_name;
                }

                // update status info for com port
                if (port_selected == false)
                {
                    txtStatusInfo.AppendText(PORT_NOT_SELECTED);
                }                
            }
            selected_port_name = port;

            /* Hex file imported */
            if (flashFile.Length > 0)
            {
                hex_file_selected = true;
            }
            else
            {
                hex_file_selected = false;
                txtStatusInfo.AppendText(HEX_FILE_NOT_SELECTED);
            }            

            // Set upload button
            Set_btUpload();
        }

        /* Set status upload button
         * - Deoends on status of selected com port and hex file
         */
        private void Set_btUpload()
        {
            if (    (prog != null)
                    && (mcu != null)
                    && (port != "")
                    && (port_selected == true)
                    && (hex_file_selected == true)
                    )
            {
                ready = true;
                cmdLine.generate();
                btnUpload.Enabled = true;
                txtStatusInfo.AppendText(READY_FOR_UPLOADING);
            }
            else
            {
                btnUpload.Enabled = false;
            }            
        }
        #endregion

        #region UI Events

        // Drag and drop
        private void event_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        // Drag and drop
        private void event_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (((GroupBox)sender).Name == "gbFlashFile")
                txtFlashFile.Text = files[0];            
        }

        // Port drop down, refresh available ports
        private void cbPort_DropDown(object sender, EventArgs e)
        {
            //this.cmbPort.Items.Clear();
            PlatformID os = Environment.OSVersion.Platform;
            if (os == PlatformID.Unix || os == PlatformID.MacOSX)
            {
                string[] devPrefixs = new string[]
                {
                    "ttyS", // Normal serial port
                    "ttyUSB", // USB <-> serial converter
                    "ttyACM", // USB <-> serial converter (usually an Arduino)
                    "lp" // Parallel port
                };

                // http://stackoverflow.com/questions/434494/serial-port-rs232-in-mono-for-multiple-platforms
                string[] devs;
                try
                {
                    devs = Directory.GetFiles("/dev/", "*", SearchOption.TopDirectoryOnly);
                }
                catch (Exception)
                {
                    return;
                }

                Array.Sort(devs);

                // Loop through each device
                foreach (string dev in devs)
                {
                    // See if device starts with one of the prefixes
                    foreach (string prefix in devPrefixs)
                    {
                        if (dev.StartsWith("/dev/" + prefix))
                        {
                            cmbPort.Items.Add(dev);
                            break;
                        }
                    }
                }
            }
            else // Windows
            {
                //update_com_ports();
            }
        }

        // FlashFile is changed event
        private void FlashFile_Changed(object sender, EventArgs e)
        {
            update_status_info();
        }

        // Browse for flash file
        private void btnFlashBrowse_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFlashFile.Text = openFileDialog1.FileName;                
            }                
        }

        // Upload!
        private void btnUpload_Click(object sender, EventArgs e)
        {
            selected_port_name = this.port;
            avrdude.launch(this.cmdBox);     
        }
                
        // Resize console when form resizes
        private void Form1_Resize(object sender, EventArgs e)
        {
            //txtConsole.Height = Height - txtConsole.Top - 64;
        }

        // Drag client area
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            drag = true;

            Point screenPos = PointToScreen(new Point(0, 0));

            dragStart = new Point(e.X + (screenPos.X - Location.X), e.Y + (screenPos.Y - Location.Y));

            Control c = (Control)sender;
            while (c is GroupBox || c is Label || c is PictureBox)
            {
                dragStart.X += c.Location.X;
                dragStart.Y += c.Location.Y;
                c = c.Parent;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
                Location = new Point(Cursor.Position.X - dragStart.X, Cursor.Position.Y - dragStart.Y);
        }

        // Save configuration when closing
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Persist window location across sessions
            // Credits:
            // gl.tter
            
            if (WindowState != FormWindowState.Minimized)
                Config.Prop.windowLocation = Location;

            Config.Prop.save();

            // Close Serial ports service threads
            SerialPortService.CleanUp();
        }                    

        // COM port index changed by mouse click
        private void cmbPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selected_port_name.Equals(port) == false)
            {
                selected_port_name = port;
                //System.Diagnostics.Debug.WriteLine(
                //    "SelectedIndexChanged: selected port = " + selected_port_name);
                update_status_info();
            }            
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // website: 3devo.com
            System.Diagnostics.Process.Start("http://www.3devo.com/contact/");
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            string about = "";
            about += AssemblyData.title + Environment.NewLine;
            about += "Version " + AssemblyData.version.ToString() + Environment.NewLine;
            about += AssemblyData.copyright + Environment.NewLine;
            about += Environment.NewLine;
            about += "www.3devo.com";
            MessageBox.Show(about, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion
    }
}
