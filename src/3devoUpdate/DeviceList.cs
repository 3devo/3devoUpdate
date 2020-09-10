using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Text.RegularExpressions;

namespace devoUpdate {
  public class USBDeviceList {
    //public static Guid GUID_DEVINTERFACE_USB_3DEVO = new Guid("4bb54eac-7b4e-472d-8400-4b2101b8833e");
    public static Guid GUID_USB_CLASS_BUS_DEVICES = new Guid("36fc9e60-c465-11cf-8056-444553540000");
    public static Guid GUID_USB_CLASS_USB_DEVICE = new Guid("88bae032-5a81-49f0-bc3d-a4ff138216d6");
    public const UInt16 HARDWARE_VENDOR_ID_3DEVO = 0x16D0;
    public const UInt16 HARDWARE_PRODUCT_ID_FE = 0x0C5B;
    public const UInt16 HARDWARE_PRODUCT_ID_GD = 0x0F44;
    public const UInt16 HARDWARE_VENDOR_ID_ST_GENERIC = 0x0483;
    public const UInt16 HARDWARE_PRODUCT_ID_ST_GENERIC = 0xDF11;
    public const string PORT_HARDWARE_ID_FE = "USB\\VID_16D0&PID_0C5B";

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
      StBootloader,
      FilamentMaker,
      AiridDryer,
      EndOfList,
    }

    public static void GetSerialDevices( List<USBDeviceList> devicelist) {
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
            string FilterProduct = $"USB\\VID_{HARDWARE_VENDOR_ID_3DEVO:X4}&PID_{HARDWARE_PRODUCT_ID_FE:X4}";

            for( UInt16 i = 0; i < hardwareIds.Length; i++ ) {
              if( hardwareIds[i].Equals(FilterProduct) ) {
                // Let's save the device information for later
                devicelist.Add(new USBDeviceList(
                  queryObj["DeviceID"].ToString(),
                  queryObj["PNPDeviceID"].ToString(),
                  queryObj["Description"].ToString()));
                devicelist[devicelist.Count - 1].ClassGuid = (string)device.GetPropertyValue("ClassGuid");
                devicelist[devicelist.Count - 1].Name = queryObj["Name"].ToString();
                devicelist[devicelist.Count - 1].MachineName = USBDeviceList.MachineType.FilamentMaker;
              }
            }
          }

        } // Foreach( ManagementObject device in deviceSearch.Get() )
      } // Foreach( ManagementObject queryObj in searcher.Get() )
    }

    public static void GetDfuDevices( List<USBDeviceList> devicelist, UInt16 FilterVid, UInt16 FilterPid ) {
      string DeviceSearchQuery = "SELECT * FROM Win32_USBControllerDevice";
      ManagementObjectSearcher USBControllerDeviceCollection = new ManagementObjectSearcher(DeviceSearchQuery);

      if( USBControllerDeviceCollection != null ) {

        foreach( ManagementObject queryObj in USBControllerDeviceCollection.Get() ) {
          // Get the DeviceID of the device entity
          string Dependent = (queryObj["Dependent"] as string).Split(new Char[] { '=' })[1];

          // Filter out USB devices without VID and PID
          Match match = Regex.Match(Dependent, "VID_[0-9|A-F]{4}&PID_[0-9|A-F]{4}");
          if( match.Success ) {
            UInt16 VendorID = Convert.ToUInt16(match.Value.Substring(4, 4), 16); // Vendor ID
            if( FilterVid != UInt16.MinValue && FilterVid != VendorID ) continue;

            UInt16 ProductID = Convert.ToUInt16(match.Value.Substring(13, 4), 16); // Product Number
            if( FilterPid != UInt16.MinValue && FilterPid != ProductID ) continue;

            // Check to see if our interface number is available for uploads
            Match matchInterface = Regex.Match(Dependent, "VID_[0-9|A-F]{4}&PID_[0-9|A-F]{4}&MI_[0-9|A-F]");
            if( matchInterface.Success ) {
              continue; // No need for the interface sub-nodes
            } else {
              // The match fails if the interface is not available, meaning that this is the "parent" node.
              // When this is the case, the serial number will be available at the end of the string.
              UInt64 SerialNum = Convert.ToUInt64(Dependent.Substring(25, 12), 16);

              string PnPEntrySearchQuery = "SELECT * FROM Win32_PnPEntity WHERE DeviceID=" + Dependent;
              ManagementObjectCollection PnPEntityCollection = new ManagementObjectSearcher(PnPEntrySearchQuery).Get();

              if( PnPEntityCollection != null ) {
                foreach( ManagementObject Entity in PnPEntityCollection ) {
                  USBDeviceList Element = new USBDeviceList(
                      "" as string, // Device ID not available right now
                      Entity["PNPDeviceID"] as string, // PnP Device ID
                      Entity["Description"] as string // Device Description
                    );

                  Guid ClassGuid = new Guid(Entity["ClassGuid"] as String); // Device installation class GUID
                  if( GUID_USB_CLASS_USB_DEVICE != Guid.Empty && ClassGuid == GUID_USB_CLASS_USB_DEVICE ) {
                    Element.MachineName = USBDeviceList.MachineType.StBootloader; // Uses a generic USB device ClassGUID
                  } else if( GUID_USB_CLASS_BUS_DEVICES != Guid.Empty && ClassGuid == GUID_USB_CLASS_BUS_DEVICES ) {
                    Element.MachineName = USBDeviceList.MachineType.AiridDryer; // Makes use of WinUsb driver ClassGuid
                  } else {
                    continue;
                  }

                  Element.Name = Entity["Name"].ToString(); // device name
                  Element.VendorId = VendorID; // Vendor ID
                  Element.ProductId = ProductID; // Product Number
                  Element.ClassGuid = ClassGuid.ToString(); // Device installation class GUID
                  Element.SerialNumber = SerialNum;
                  
                  devicelist.Add(Element);
                }
              }
            }

          }
        } // foreach( ManagementObject queryObj in USBControllerDeviceCollection )
      } // if ( USBControllerDeviceCollection != null )
    }

  } // end of class: USBDeviceList

}
