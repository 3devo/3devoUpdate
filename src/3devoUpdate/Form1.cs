/*
 * Project: 3devoUpdate - firmware uploader for 3devo devices
 * Author: [Zak Kemble, contact@zakkemble.co.uk], [R&D, 3devo.com]
 * Copyright: (C) 2013 by Zak Kemble, (C) 2016 by 3devo
 * License: GNU GPL v3 (see License.txt)
 * Web: https://blog.zakkemble.net/avrdudess-a-gui-for-avrdude/
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using avrdudess;
using System.Linq;

namespace devoUpdate {
  public partial class Form1 : Form {
    public bool downloadIsReady = false;
    private const string PORT_NOT_SELECTED = "No COM-port selected.\n";
    private const string HEX_FILE_NOT_SELECTED = "No hex file selected.\n";
    private const string NO_MACHINE_CONNECTED = "No machine connected or 3devo driver not installed.\n";
    private const string READY_FOR_UPLOADING = "Ready for uploading!\n";
    private ToolTip ToolTips;
    private AvrCmdLine avrCmdLine;
    private Avrdude avrdude;
    private bool drag = false;
    private bool deviceSelected = false;
    private bool downloadFileSelected = false;
    private Point dragStart;
    private EventHandler combobox_selectedIndexChangedHandler;

    #region Control getters and setters

    public List<USBDeviceList> DeviceInfoList;
    public string flashFile {
      get { return txtFlashFile.Text.Trim(); }
      set { txtFlashFile.Text = value; }
    }
    #endregion

    #region Initializing

    public Form1() {
      InitializeComponent();

      Icon = AssemblyData.icon;
      SetWindowTitle();

      MaximumSize = new Size(Size.Width, int.MaxValue);
      MinimumSize = new Size(Size.Width, Height - this.txtStatusInfo.Height);

      Util.UI = this;
      Util.consoleSet(txtStatusInfo);

      DeviceInfoList = new List<USBDeviceList>();
    }

    private void Form1_Load( object sender, EventArgs e ) {
      // Debug info
      if( Constants.DEBUG_STATUS == true ) {
        System.Diagnostics.Debug.WriteLine("Start initializing....");
      }

      // Load saved configuration
      Config.Prop.load();

      // Persist window location across sessions
      if( Config.Prop.windowLocation != null && Config.Prop.windowLocation != new Point(0, 0) )
        Location = Config.Prop.windowLocation;

      avrCmdLine = new AvrCmdLine(this);
      avrdude = new Avrdude();

      avrdude.OnProcessStart += Avrdude_OnProcessStart;
      avrdude.OnProcessEnd += Avrdude_OnProcessEnd;


      // Setup memory files/usage bars
      EnableClientAreaDrag(Controls);

      // Drag and drop flash file
      gbFlashFile.AllowDrop = true;
      gbFlashFile.DragEnter += Event_DragEnter;
      gbFlashFile.DragDrop += Event_DragDrop;

      // Flash file
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
      avrCmdLine.LoadFilamentMakerDefaults();
      downloadIsReady = false;
      btnUpload.Enabled = false;

      // Update serial ports etc
      UpdateDeviceList();
      SerialPortService.PortsChanged += ( sender1, changedArgs ) => UpdateDeviceList();
      combobox_selectedIndexChangedHandler = new System.EventHandler(this.CmbPort_SelectedIndexChanged);
      cmbPort.SelectedIndexChanged += combobox_selectedIndexChangedHandler;
      cmbPort.DropDownClosed += new System.EventHandler(this.CmbPort_DropDownClosed);

      // Call the port selected index changed handler just once if a device is already connected.
      if (cmbPort.SelectedIndex != -1) {
        combobox_selectedIndexChangedHandler(this, new EventArgs());
      }

      // Debug info
      if( Constants.DEBUG_STATUS == true ) {
        System.Diagnostics.Debug.WriteLine("Ready: done initializing\n");
      }
    }

    private void SetWindowTitle() {
      Text = String.Format("{0} v{1}.{2}", AssemblyData.title, AssemblyData.version.Major, AssemblyData.version.Minor);
    }

    // Click and drag (almost) anywhere to move window
    private void EnableClientAreaDrag( Control.ControlCollection controls ) {
      foreach( Control c in controls ) {
        if( c is GroupBox || c is Label || c is PictureBox ) {
          c.MouseDown += Form1_MouseDown;
          c.MouseUp += Form1_MouseUp;
          c.MouseMove += Form1_MouseMove;
          EnableClientAreaDrag(c.Controls);
        }
      }
    }
    #endregion

    #region Avrdude status

    // Avrdude process has started
    private void Avrdude_OnProcessStart( object sender, EventArgs e ) {
      tssStatus.Text = "Avrdude is running.";
    }

    // Avrdude process has ended
    private void Avrdude_OnProcessEnd( object sender, EventArgs e ) {
      Util.InvokeIfRequired(this, c => { UpdateInterface(); });
      tssStatus.Text = "Ready";
    }

    #endregion

    #region Extra functions

    /* Update connected COM ports 
     * - Retrieve all used USB ports and filter 3devo specific devices
     * 
     * Code derived from: https://stackoverflow.com/questions/3331043/get-list-of-connected-usb-devices
     */
    private void UpdateDeviceList() {
      DeviceInfoList.Clear();

      Util.InvokeIfRequired(this, c => { this.cmbPort.Items.Clear(); });

      // Get all the serial devices which do not make use of DFU programming
      USBDeviceList.GetSerialDevices(DeviceInfoList);

      // Get all the other devices in the list (such as Airid Dryer)
      USBDeviceList.GetDfuDevices(DeviceInfoList, USBDeviceList.HARDWARE_VENDOR_ID_3DEVO, USBDeviceList.HARDWARE_PRODUCT_ID_GD);

      // Also add the devices which don't have a product name yet (ST generic)
      USBDeviceList.GetDfuDevices(DeviceInfoList, USBDeviceList.HARDWARE_VENDOR_ID_ST_GENERIC, USBDeviceList.HARDWARE_PRODUCT_ID_ST_GENERIC);

      // Add all components to the combobox list
      UInt16 DeviceCount = 0;
      string NewHardwareField = "";
      foreach( USBDeviceList Device in DeviceInfoList ) {
        switch( Device.MachineName ) {
          case USBDeviceList.MachineType.FilamentMaker:
            NewHardwareField = (DeviceCount + 1).ToString() + ". " + Device.Name; // Hardware friendly name
            break;
          case USBDeviceList.MachineType.StBootloader:
            NewHardwareField = (DeviceCount + 1).ToString() + ". " + "Generic ST device" + $" ({Device.SerialNumber:X})";
            break;
          case USBDeviceList.MachineType.AiridDryer:
            NewHardwareField = (DeviceCount + 1).ToString() + ". " + "Airid Dryer" + $" ({Device.SerialNumber:X})";
            break;
          case USBDeviceList.MachineType.None: // fall-through
          default:
            Util.InvokeIfRequired(this, c => {
              txtStatusInfo.AppendText($"An unexpected machine found in device list ({Device.MachineName})");
            });
            break;
        }

        Util.InvokeIfRequired(this, c => { this.cmbPort.Items.Add(NewHardwareField); });

        DeviceCount += 1;
      }

      if( this.cmbPort.Items.Count > 0 ) {
        Util.InvokeIfRequired(this, c => { this.cmbPort.SelectedIndex = 0; });
      }

      // update status info text
      Util.InvokeIfRequired(this, c => { UpdateInterface(); });
    }

    /* Update the statusinfo field
     *  -  no filament extruder connected
     *  -  com port selection
     *  -  hex file set
     */
    private void UpdateInterface() {
      // Clear status info text
      txtStatusInfo.Clear();

      // No devices connected/found
      if( cmbPort.Items.Count == 0 ) {
        btnFlashBrowse.Enabled = false;
        txtStatusInfo.AppendText(NO_MACHINE_CONNECTED);
      }
      // 1 or more devices connected
      else {
        btnFlashBrowse.Enabled = true;

        if( deviceSelected == false ) {
          txtStatusInfo.AppendText(PORT_NOT_SELECTED);
        } else if( flashFile.Length == 0 ) {
          txtStatusInfo.AppendText(HEX_FILE_NOT_SELECTED);
        } else {
          txtStatusInfo.AppendText(READY_FOR_UPLOADING);
        }
      }

      if( (deviceSelected == true)
          && (downloadFileSelected == true)
          && (!downloadIsReady) ) {
        downloadIsReady = true;
        btnUpload.Enabled = true;
      }
    }

    private void StartUploadProcess() {
      switch( DeviceInfoList.ElementAt(cmbPort.SelectedIndex).MachineName ) {
        case USBDeviceList.MachineType.FilamentMaker:
          avrCmdLine.generate();
          avrdude.load();
          avrdude.launch(avrCmdLine.cmdBox);
          break;
        case USBDeviceList.MachineType.StBootloader: // Fall through
        case USBDeviceList.MachineType.AiridDryer:
          break;
        case USBDeviceList.MachineType.None:
        default:
          // TODO: Error no compatible upload method found.
          break;
      }
    }
    #endregion

    #region UI Events

    // Drag and drop
    private void Event_DragEnter( object sender, DragEventArgs e ) {
      if( e.Data.GetDataPresent(DataFormats.FileDrop) )
        e.Effect = DragDropEffects.Copy;
    }

    // Drag and drop
    private void Event_DragDrop( object sender, DragEventArgs e ) {
      string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
      if( ((GroupBox)sender).Name == "gbFlashFile" )
        txtFlashFile.Text = files[0];
    }

    // FlashFile is changed event
    private void FlashFile_Changed( object sender, EventArgs e ) {
    }

    // Browse for flash file
    private void BtnFlashBrowse_Click( object sender, EventArgs e ) {
      downloadIsReady = false; // Something has changed in the selection, so let's check if everything is ok later on.
      btnUpload.Enabled = false;

      //openFileDialog1.Filter += "|All files (*.*)|*.*"; // Probably not a necessary option.
      switch( DeviceInfoList.ElementAt(cmbPort.SelectedIndex).MachineName ) {
        case USBDeviceList.MachineType.FilamentMaker:
          openFileDialog1.Title = "Select Filemant Maker or Filament Extruder firmware";
          openFileDialog1.Filter = "Hex files (*.hex)|*.hex";
          break;
        case USBDeviceList.MachineType.StBootloader:
        case USBDeviceList.MachineType.AiridDryer:
          openFileDialog1.Title = "Select Airid Dryer firmware";
          openFileDialog1.Filter = "Binary files (*.bin)|*.bin";
          break;
        case USBDeviceList.MachineType.None:
        default:
          // TODO: Error unsupported model.
          break;
      }

      DialogResult Result = DialogResult.None;
      Result = openFileDialog1.ShowDialog();

      if( Result == DialogResult.OK ) {
        if( openFileDialog1.FileName.Length != 0 ) {
          txtFlashFile.Text = openFileDialog1.FileName;
          downloadFileSelected = true;
        } else {
          downloadFileSelected = false;
        }
      }

      UpdateInterface();
    }

    // Upload button
    private void BtnUpload_Click( object sender, EventArgs e ) {
      if( downloadIsReady ) {
        StartUploadProcess();
        downloadIsReady = false;
      } else {
        Console.Error.Write("Could not perform upload operation, no file or machine selected yet.");
      }
    }

    // Drag client area
    private void Form1_MouseDown( object sender, MouseEventArgs e ) {
      drag = true;

      Point screenPos = PointToScreen(new Point(0, 0));

      dragStart = new Point(e.X + (screenPos.X - Location.X), e.Y + (screenPos.Y - Location.Y));

      Control c = (Control)sender;
      while( c is GroupBox || c is Label || c is PictureBox ) {
        dragStart.X += c.Location.X;
        dragStart.Y += c.Location.Y;
        c = c.Parent;
      }
    }

    private void Form1_MouseUp( object sender, MouseEventArgs e ) {
      drag = false;
    }

    private void Form1_MouseMove( object sender, MouseEventArgs e ) {
      if( drag )
        Location = new Point(Cursor.Position.X - dragStart.X, Cursor.Position.Y - dragStart.Y);
    }

    // Save configuration when closing
    private void Form1_FormClosing( object sender, FormClosingEventArgs e ) {
      // Persist window location across sessions
      if( WindowState != FormWindowState.Minimized )
        Config.Prop.windowLocation = Location;

      Config.Prop.save();

      // Close Serial ports service threads
      SerialPortService.Dispose(true);
    }

    private void CmbPort_DropDownClosed( object sender, EventArgs e ) {
        ComboboxDropdown_Handler();
    }

    // https://stackoverflow.com/questions/16966264/what-event-handler-to-use-for-combobox-item-selected-selected-item-not-necessar //Todo
    private void CmbPort_SelectedIndexChanged( object sender, EventArgs e ) {
        ComboboxDropdown_Handler();
    }

    private static int PrevSelectionIndex = -1;
    private void ComboboxDropdown_Handler() {
      downloadIsReady = false; // Something has changed in the selection, so let's check if everything is ok later on.
      deviceSelected = false;
      btnUpload.Enabled = false;

      if( cmbPort.SelectedIndex == -1 ) {
        PrevSelectionIndex = -1;
        avrCmdLine.port = "";
      } else {
        USBDeviceList usbDevice = DeviceInfoList.ElementAt(cmbPort.SelectedIndex);

        try {
          if( usbDevice.MachineName != DeviceInfoList.ElementAt(PrevSelectionIndex).MachineName ) {
            // Clear the file selection when another device type is selected.
            txtFlashFile.Text = "";
            downloadFileSelected = false;
          }
        }
        catch( ArgumentOutOfRangeException ) {
          // Check fails if the previous selection index is still negative, which is ok for the first time.
        }

        switch( usbDevice.MachineName ) {
          case USBDeviceList.MachineType.FilamentMaker:
            avrCmdLine.port = usbDevice.DeviceId; // COM number
            deviceSelected = true;
            break;
          case USBDeviceList.MachineType.StBootloader:
            txtStatusInfo.AppendText("Generic ST device selected; Carefully select the correct "
              + "binary file for your machine, since we cannot determine which 3devo device is connected!");
            deviceSelected = true;
            break;
          case USBDeviceList.MachineType.AiridDryer:
            deviceSelected = true;
            break;
          case USBDeviceList.MachineType.None:
          default:
            avrCmdLine.port = "";
            break;
        }

        PrevSelectionIndex = cmbPort.SelectedIndex;
      }

      UpdateInterface();
    }

    private void PictureBox1_Click( object sender, EventArgs e ) {
      // website: 3devo.com
      System.Diagnostics.Process.Start("https://www.3devo.com/contact/");
    }

    private void BtnAbout_Click( object sender, EventArgs e ) {
      string about = "";
      about += AssemblyData.title + Environment.NewLine;
      about += "Version " + AssemblyData.version.ToString() + Environment.NewLine;
      about += AssemblyData.copyright + Environment.NewLine;
      about += "Based on Avrdudess by Zak Kemble. Licensed under the GPLv3." + Environment.NewLine;
      about += "Source at github.com/3devo/3devoUpdate";
      about += Environment.NewLine;
      about += "www.3devo.com";
      MessageBox.Show(about, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    #endregion
  }
}
