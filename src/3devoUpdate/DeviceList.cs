using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Text.RegularExpressions;
using static devoUpdate.DevoHardware;

namespace devoUpdate {
  public class USBDeviceList {
    //public static Guid GUID_DEVINTERFACE_USB_3DEVO = new Guid("4bb54eac-7b4e-472d-8400-4b2101b8833e");
    public static Guid GUID_USB_CLASS_BUS_DEVICES = new Guid("36fc9e60-c465-11cf-8056-444553540000");
    public static Guid GUID_USB_CLASS_USB_DEVICE = new Guid("88bae032-5a81-49f0-bc3d-a4ff138216d6");

    public USBDeviceList( string _DeviceId, string _PnpDeviceId, string _Description ) {
      this.ClassGuid = "";
      this.Description = _Description;
      this.DeviceId = _DeviceId;
      this.Name = "";
      this.PnpDeviceId = _PnpDeviceId;
      this.SerialNumber = 0x0;
      this.VendorId = 0x0;
      this.ProductId = 0x0;
      this.MachineName = MachineType.None;
    }

    public string ClassGuid { get; set; }
    public string Description { get; set; }
    public string DeviceId { get; set; }
    public string Name { get; set; }
    public string PnpDeviceId { get; set; }
    public UInt64 SerialNumber { get; set; }
    public MachineType MachineName { get; set; }
    public UInt16 VendorId { get; set; }
    public UInt16 ProductId { get; set; }

    public enum MachineType : ushort {
      None = 0,
      AtmelDevice,
      StBootloader,
      StDevice,
      EndOfList,
    }

    public static void GetSerialDevices( List<USBDeviceList> devicelist, IReadOnlyList<HardwareIds> FilterDevices ) {
      string SearchQuery = "SELECT * FROM Win32_SerialPort";
      ManagementObjectSearcher Searcher = new ManagementObjectSearcher(SearchQuery); // In default scope

      foreach( ManagementObject queryObj in Searcher.Get() ) {
        string pnpDeviceId = (string)queryObj["PNPDeviceID"];
        Console.WriteLine("Description  : {0}", queryObj["Description"]);
        Console.WriteLine(" PNPDeviceID : {0}", pnpDeviceId);

        if( string.IsNullOrEmpty(pnpDeviceId) )
          continue;

        string SearchQueryPNP = "SELECT * FROM win32_PNPEntity where DeviceID='" + pnpDeviceId.Replace("\\", "\\\\") + "'";
        ManagementObjectSearcher deviceSearch = new ManagementObjectSearcher(SearchQueryPNP); // In default scope

        foreach( ManagementObject device in deviceSearch.Get() ) {
          string[] hardwareIds = (string[])device["HardWareID"];

          if( (hardwareIds != null) && (hardwareIds.Length > 0) ) {
            foreach (HardwareIds FilterDevice in FilterDevices) {
              if (FilterDevice.Name == DevoHardware.DEVO_HARDWARE_NAME.UNKNOWN)
                continue;

              string FilterProduct = $"USB\\VID_{FilterDevice.Vid:X4}&PID_{FilterDevice.Pid:X4}";

              for (UInt16 i = 0; i < hardwareIds.Length; i++) {
                if (hardwareIds[i].Equals(FilterProduct)) {
                  // Let's save the device information for later
                  devicelist.Add(new USBDeviceList(
                    queryObj["DeviceID"].ToString(),
                    queryObj["PNPDeviceID"].ToString(),
                    queryObj["Description"].ToString()));
                  devicelist[devicelist.Count - 1].ClassGuid = (string)device.GetPropertyValue("ClassGuid");
                  devicelist[devicelist.Count - 1].Name = queryObj["Name"].ToString();
                  devicelist[devicelist.Count - 1].MachineName = USBDeviceList.MachineType.AtmelDevice;
                }
              }
            }
          }

        } // Foreach( ManagementObject device in deviceSearch.Get() )
      } // Foreach( ManagementObject queryObj in searcher.Get() )
    }

    public static void GetDfuDevices( List<USBDeviceList> devicelist, IReadOnlyList<HardwareIds> FilterDevices) {
      string DeviceSearchQuery = "SELECT * FROM Win32_USBControllerDevice";
      ManagementObjectSearcher USBControllerDeviceCollection = new ManagementObjectSearcher(DeviceSearchQuery);

      if (USBControllerDeviceCollection == null)
        return;

      foreach( ManagementObject queryObj in USBControllerDeviceCollection.Get() ) {
        // Get the DeviceID of the device entity
        string Dependent = (queryObj["Dependent"] as string).Split(new Char[] { '=' })[1];

        foreach (HardwareIds FilterDevice in FilterDevices) {
          // Filter out USB devices without VID and PID
          Match match = Regex.Match(Dependent, "VID_[0-9|A-F]{4}&PID_[0-9|A-F]{4}");
          if (!match.Success)
            continue;

          UInt16 VendorID = Convert.ToUInt16(match.Value.Substring(4, 4), 16); // Vendor ID
          if (FilterDevice.Vid == UInt16.MinValue || FilterDevice.Vid != VendorID)
            continue;

          UInt16 ProductID = Convert.ToUInt16(match.Value.Substring(13, 4), 16); // Product Number
          if (FilterDevice.Pid == UInt16.MinValue || FilterDevice.Pid != ProductID)
            continue;

          // Check to see if our interface number is available for uploads
          Match matchInterface = Regex.Match(Dependent, "VID_[0-9|A-F]{4}&PID_[0-9|A-F]{4}&MI_[0-9|A-F]");
          if (matchInterface.Success)
            continue; // Skip interface nodes, since we are only interested in the main composite device.

          UInt64 SerialNum = 0;

          // The match fails if the interface is not available, meaning that this is the "parent" node.
          // When this is the case, the serial number will be available at the end of the string.
          try {
            SerialNum = Convert.ToUInt64(Dependent.Substring(25, 12), 16);
          }
          catch (Exception) {
            // Could not retrieve the serial number, perhaps this device does not advertise one.
          }

          string PnPEntrySearchQuery = "SELECT * FROM Win32_PnPEntity WHERE DeviceID=" + Dependent;
          ManagementObjectCollection PnPEntityCollection = new ManagementObjectSearcher(PnPEntrySearchQuery).Get();

          if (PnPEntityCollection == null)
            continue;
            
          foreach (ManagementObject Entity in PnPEntityCollection) {
            USBDeviceList Element = new USBDeviceList(
                "" as string, // Device ID not available right now
                Entity["PNPDeviceID"] as string, // PnP Device ID
                Entity["Description"] as string // Device Description
              );

            Guid ClassGuid = new Guid(Entity["ClassGuid"] as String); // Device installation class GUID
            if (GUID_USB_CLASS_USB_DEVICE != Guid.Empty && ClassGuid == GUID_USB_CLASS_USB_DEVICE) {
              Element.MachineName = USBDeviceList.MachineType.StBootloader; // Uses a generic USB device ClassGUID
            }
            else if (GUID_USB_CLASS_BUS_DEVICES != Guid.Empty && ClassGuid == GUID_USB_CLASS_BUS_DEVICES) {
              Element.MachineName = USBDeviceList.MachineType.StDevice; // Makes use of WinUsb driver ClassGuid
            }
            else
            {
              continue;
            }

            Element.VendorId = VendorID; // Vendor ID
            Element.ProductId = ProductID; // Product Number

            // Get a user-friendly name from the filter device.
            Element.Name = GetString(FilterDevice);

            Element.ClassGuid = ClassGuid.ToString(); // Device installation class GUID
            Element.SerialNumber = SerialNum;

            devicelist.Add(Element);
          }

        } // foreach (HardwareIds FilterDevice in FilterDevices)
      } // foreach( ManagementObject queryObj in USBControllerDeviceCollection )

    }

  } // end of class: USBDeviceList

}
