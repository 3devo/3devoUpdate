﻿/*
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
    private const string PORT_NOT_SELECTED = "No COM-port selected.";
    private const string FILE_NOT_SELECTED = "No file selected.";
    private const string NO_MACHINE_CONNECTED = "No machine connected or 3devo driver not installed.";
    private const string READY_FOR_UPLOADING = "Ready for uploading!";
    private ToolTip ToolTips;
    private AvrCmdLine avrCmdLine;
    private Avrdude avrdude;
    private DfuUtilCmdLine dfuUtilCmdLine;
    private DfuSe_FileValidation dfuSeFileValidation;
    private DfuUtil dfuUtil;
    private bool drag = false;
    private bool deviceSelected = false;
    private bool bootloaderDevice = false;
    private bool downloadFileSelected = false;
    private Point dragStart;
    private EventHandler combobox_selectedIndexChangedHandler;
    private EventHandler<PortsChangedArgs> Portchanged_event;
    public List<USBDeviceList> DeviceInfoList;

    #region Control getters and setters

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
      try {
        avrdude.Init();
      }
      catch(Exception ex) {
        // Close the application when Avrdude is not found.
        MsgBox.error(ex.Message);
        this.Close();
      }

      avrdude.OnProcessStart += Application_OnProcessStart;
      avrdude.OnProcessEnd += Application_OnProcessEnd;

      dfuUtilCmdLine = new DfuUtilCmdLine(this);
      dfuUtil = new DfuUtil();
      try {
        dfuUtil.Init();
      }
      catch( Exception ex ) {
        // Close the application when dfu-util is not found.
        MsgBox.error(ex.Message);
        this.Close();
      }

      dfuSeFileValidation = new DfuSe_FileValidation(this);

      dfuUtil.OnProcessStart += Application_OnProcessStart;
      dfuUtil.OnProcessEnd += Application_OnProcessEnd;

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
      ToolTips.SetToolTip(cmbPort, "Select your machine here first.");
      ToolTips.SetToolTip(txtFlashFile, "File used to program your machine." + Environment.NewLine 
        + "You can also drag and drop firmware files in here.");
      ToolTips.Active = Config.Prop.toolTips;

      downloadIsReady = false;
      btnUpload.Enabled = false;

      // Update serial ports etc
      UpdateDeviceList();
      Portchanged_event = ( sender1, changedArgs ) => UpdateDeviceList();
      SerialPortService.PortsChanged += Portchanged_event;
      combobox_selectedIndexChangedHandler = new System.EventHandler(this.CmbPort_SelectedIndexChanged);
      cmbPort.SelectedIndexChanged += combobox_selectedIndexChangedHandler;
      cmbPort.DropDownClosed += new System.EventHandler(this.CmbPort_DropDownClosed);

      // Call the port selected index changed handler just once if a device is already connected.
      if (cmbPort.SelectedIndex != -1) {
        combobox_selectedIndexChangedHandler(this, new EventArgs());
      }

      if( Constants.DEBUG_STATUS == true ) {
        System.Diagnostics.Debug.WriteLine("Ready: done initializing\r\n");
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

    #region Executable process events

    private void Application_OnProcessStart(object sender, EventArgs e) {
      tssStatus.Text = "Upload process busy";
    }

    private void Application_OnProcessEnd( object sender, EventArgs e ) {
      if (sender is DfuUtil)
        dfuUtil.AwaitAndAbortProcess();
      else if (sender is Avrdude)
        avrdude.AwaitAndAbortProcess();

      if( Constants.DEBUG_STATUS )
        Console.WriteLine("Application_OnProcessEnd(); Postponing of Form process during enumeration is finished (delay expired).");

      // Enable the PortChanged event again after uploading is finished.
      SerialPortService.PortsChanged += Portchanged_event;

      // Update the devicelist manually this time in case we missed some insert/removal events.
      Util.InvokeIfRequired(this, c => { UpdateDeviceList(); });

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
      DisableInterface();

      DeviceInfoList.Clear();

      Util.InvokeIfRequired(this, c => { this.cmbPort.Items.Clear(); });

      // Get all the serial devices which do not make use of DFU programming
      USBDeviceList.GetSerialDevices(DeviceInfoList, DevoHardware.SerialDevices);

      // Get all the other devices in the list (such as Airid Dryer)
      USBDeviceList.GetDfuDevices(DeviceInfoList, DevoHardware.DfuDevices);

      // Add all components to the combobox list
      bool valid_device = false;
      UInt16 DeviceCount = 0;
      string NewHardwareField = "";
      foreach( USBDeviceList Device in DeviceInfoList ) {
        valid_device = true;
        NewHardwareField = "";

        switch ( Device.MachineName ) {
          case USBDeviceList.MachineType.AtmelDevice:
            NewHardwareField = (DeviceCount + 1).ToString() + ". " + Device.Name; // Hardware friendly name
            break;
          case USBDeviceList.MachineType.StBootloader:
            NewHardwareField = (DeviceCount + 1).ToString() + ". " + "Generic ST device" + $" ({Device.SerialNumber:X})";
            break;
          case USBDeviceList.MachineType.StDevice:
            NewHardwareField = (DeviceCount + 1).ToString() + ". " + Device.Name + $" ({Device.SerialNumber:X})";
            break;
          case USBDeviceList.MachineType.None:
            NewHardwareField = (DeviceCount + 1).ToString() + ". " + "Unsupported device";
            break;
          default:
            valid_device = false;
            Util.InvokeIfRequired(this, c => {
              txtStatusInfo.AppendText($"An unexpected machine found in device list ({Device.MachineName})");
            });
            break;
        }

        if (valid_device) {
          Util.InvokeIfRequired(this, c => { this.cmbPort.Items.Add(NewHardwareField); });

          DeviceCount += 1;
        }
      }

      // Pre-select the first item in the list if only one device is selectable. Changing the selected index
      // when no device is preselected raises the comboboxDropdown_Handler a second time.
      Util.InvokeIfRequired(this, c => {
        if( this.cmbPort.Items.Count > 0 && this.cmbPort.SelectedIndex == -1 ) {
          this.cmbPort.SelectedIndex = 0;
        } else {
          // Refresh the selected combobox index manually.
          // Note that this assumes that the interface will also be refreshed in this handler.
          ComboboxDropdown_Handler();
        }
      });
    }

    private void EnableInterface(bool enableUpload = true) {
      Util.InvokeIfRequired(this, c => {
        if( enableUpload )
          btnUpload.Enabled = true;

        btnFlashBrowse.Enabled = true;
        cmbPort.Enabled = true;
      });
    }

    private void DisableInterface() {
      Util.InvokeIfRequired(this, c => {
        btnUpload.Enabled = false;
        btnFlashBrowse.Enabled = false;
        cmbPort.Enabled = false;
      });
    }

    private void UpdateInterface() {
      cmbPort.Enabled = true;

      // No devices connected/found
      if( cmbPort.Items.Count == 0 ) {
        btnFlashBrowse.Enabled = false;
        tssStatus.Text = NO_MACHINE_CONNECTED;
      }
      // 1 or more devices connected
      else {
        btnFlashBrowse.Enabled = true;

        if( deviceSelected == false ) {
          tssStatus.Text = PORT_NOT_SELECTED;
        } else if( flashFile.Length == 0 ) {
          tssStatus.Text = FILE_NOT_SELECTED;
        } else {
          tssStatus.Text = READY_FOR_UPLOADING;
        }
      }

      // User warning when the machine is detected in bootloader mode.
      if (bootloaderDevice == true) {
        txtStatusInfo.AppendText("\r\nGeneric ST device selected;\r\n");
        txtStatusInfo.AppendText("Carefully select the correct binary file for your machine, since we cannot determine which 3devo device is connected!\r\n");
        txtStatusInfo.AppendText("Uploading the wrong firmware may cause hardware damage or the machine might stop responding.\r\n");
        txtStatusInfo.AppendText("Only use this when recovering from a failed firmware update using the BOOT pin.\r\n");
      }

      if( (deviceSelected == true)
          && (downloadFileSelected == true)
          && (!downloadIsReady) ) {
        downloadIsReady = true;
        btnUpload.Enabled = true;
      }
    }

    private void StartUploadProcess() {
      try {
        switch( DeviceInfoList.ElementAt(cmbPort.SelectedIndex).MachineName ) {
          case USBDeviceList.MachineType.AtmelDevice:
            avrCmdLine.generate();
            avrdude.Launch(avrCmdLine.cmdBox, Avrdude.CommandType.FILE_UPLOAD);
            break;
          case USBDeviceList.MachineType.StBootloader: // Fall through
          case USBDeviceList.MachineType.StDevice:
            USBDeviceList usbDevice;
            // This assumes that the selected combobox index doesn't change (at least not before the upload process).
            usbDevice = DeviceInfoList.ElementAt(cmbPort.SelectedIndex);

            // TODO: Place the Target information somewhere else and make it less hardcoded.
            dfuSeFileValidation.FirmwareFileValidation(usbDevice, dfuUtilCmdLine.alternateInterfaceNr, dfuUtilCmdLine.dfuseAddress, bootloaderDevice);

            dfuUtilCmdLine.Generate();
            dfuUtil.Launch(dfuUtilCmdLine.command, DfuUtil.CommandType.FILE_UPLOAD);
            break;
          case USBDeviceList.MachineType.None:
          default:
            throw new Exception("No uploadable device selected.");
            break;
        }
      }
      catch( Exception ex ) {
        Util.consoleWrite(ex.Message);
        EnableInterface(false /*upload button still disabled*/);
        return;
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

    // Browse for flash file
    private void BtnFlashBrowse_Click( object sender, EventArgs e ) {
      downloadIsReady = false; // Something has changed in the selection, so let's check if everything is ok later on.
      btnUpload.Enabled = false;

      try {
        //openFileDialog1.Filter += "|All files (*.*)|*.*"; // Probably not a necessary option.
        switch( DeviceInfoList.ElementAt(cmbPort.SelectedIndex).MachineName ) {
          case USBDeviceList.MachineType.AtmelDevice:
            openFileDialog1.Title = "Select Filemant Maker or Filament Extruder firmware";
            openFileDialog1.Filter = "Hex files (*.hex)|*.hex";
            break;
          case USBDeviceList.MachineType.StBootloader:
          case USBDeviceList.MachineType.StDevice:
            openFileDialog1.Title = "Select Airid Dryer firmware";
            openFileDialog1.Filter = "Binary files (*.DfuSe)|*.DfuSe";
            break;
          case USBDeviceList.MachineType.None:
          default:
            throw new Exception("This model is not yet supported.");
            break;
        }
      }
      catch( ArgumentOutOfRangeException ) {
        Console.WriteLine("Pressed while no combobox items are available.");
        return;
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

    private void BtnUpload_Click( object sender, EventArgs e ) {
      if( downloadIsReady ) {
        downloadIsReady = false;

        // Clear status info box text.
        txtStatusInfo.Clear();

        // Disable the PortsChanged eventhandler to prevent multiple insertion/removal events of devices.
        // This is caused by the selected download device, which will reset during the upload process and
        // continue on in bootloader mode. This resetting behaviour causes multiple events and refreshed
        // the devicelist again, which is not necessary at the moment of downloading.
        SerialPortService.PortsChanged -= Portchanged_event;

        DisableInterface();

        StartUploadProcess();
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

    // https://stackoverflow.com/questions/16966264/what-event-handler-to-use-for-combobox-item-selected-selected-item-not-necessar
    private void CmbPort_SelectedIndexChanged( object sender, EventArgs e ) {
        ComboboxDropdown_Handler();
    }

    private static USBDeviceList.MachineType prevMachineType = USBDeviceList.MachineType.None;
    private void ComboboxDropdown_Handler() {
      downloadIsReady = false; // Something has changed in the selection, so let's check if everything is ok later on.
      deviceSelected = false;
      bootloaderDevice = false;
      btnUpload.Enabled = false;

      if( cmbPort.SelectedIndex == -1 ) {
        avrCmdLine.port = "";
        dfuUtilCmdLine.vid = 0;
        dfuUtilCmdLine.pid = 0;
        dfuUtilCmdLine.serialNumber = 0;
      } else {
        USBDeviceList usbDevice;
        try {
          usbDevice = DeviceInfoList.ElementAt(cmbPort.SelectedIndex);

          if( usbDevice.MachineName != prevMachineType ) {
            // Clear the file selection when another device type is selected.
            txtFlashFile.Text = "";
            downloadFileSelected = false;
          }
        }
        catch( ArgumentOutOfRangeException ) {
          throw new ArgumentOutOfRangeException("Combobox index out of range: ", cmbPort.SelectedIndex.ToString());
        }

        switch( usbDevice.MachineName ) {
          case USBDeviceList.MachineType.AtmelDevice:
            avrCmdLine.LoadFilamentMakerDefaults();

            avrCmdLine.port = usbDevice.DeviceId; // COM number
            deviceSelected = true;
            break;
          case USBDeviceList.MachineType.StBootloader:
            dfuUtilCmdLine.LoadStm32f4Defaults();

            dfuUtilCmdLine.vid = usbDevice.VendorId;
            dfuUtilCmdLine.pid = usbDevice.ProductId;
            dfuUtilCmdLine.serialNumber = usbDevice.SerialNumber;
            bootloaderDevice = true;
            deviceSelected = true;
            break;
          case USBDeviceList.MachineType.StDevice:
            dfuUtilCmdLine.LoadStm32f4Defaults();

            dfuUtilCmdLine.vid = usbDevice.VendorId;
            dfuUtilCmdLine.pid = usbDevice.ProductId;
            dfuUtilCmdLine.serialNumber = usbDevice.SerialNumber;
            deviceSelected = true;
            break;
          case USBDeviceList.MachineType.None:
          default:
            avrCmdLine.port = "";
            dfuUtilCmdLine.vid = 0;
            dfuUtilCmdLine.pid = 0;
            dfuUtilCmdLine.serialNumber = 0;
            break;
        }

        prevMachineType = usbDevice.MachineName;
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
