using System;
using System.Collections.Generic;

namespace devoUpdate
{
  public static class DevoHardware {
    public enum DEVO_HARDWARE_NAME
    {
      UNKNOWN = 0,
      ST_GENERIC,
      FM14,
      GD17,
      GP20,
      LAST_DEVICE,
    }

    public static IReadOnlyList<HardwareIds> SerialDevices { get; private set; } = (new List<HardwareIds>() {
      new HardwareIds {
        Name= DEVO_HARDWARE_NAME.UNKNOWN,
        Vid= 0x0000,
        Pid= 0x0000,
      },
      new HardwareIds {
        Name= DEVO_HARDWARE_NAME.FM14,
        Vid= HARDWARE_VENDOR_ID_3DEVO,
        Pid= 0x0C5B,
      },
    }).AsReadOnly();

    public static IReadOnlyList<HardwareIds> DfuDevices { get; private set; } = (new List<HardwareIds>() {
      new HardwareIds {
        Name= DEVO_HARDWARE_NAME.UNKNOWN,
        Vid= 0x0000,
        Pid= 0x0000,
      },
      new HardwareIds {
        Name= DEVO_HARDWARE_NAME.GD17,
        Vid= HARDWARE_VENDOR_ID_3DEVO,
        Pid= 0x0F44,
      },
      new HardwareIds {
        Name= DEVO_HARDWARE_NAME.GP20,
        Vid= HARDWARE_VENDOR_ID_3DEVO,
        Pid= 0x102A,
      },
      new HardwareIds {
        Name= DEVO_HARDWARE_NAME.ST_GENERIC,
        Vid= HARDWARE_VENDOR_ID_ST_GENERIC,
        Pid= HARDWARE_PRODUCT_ID_ST_GENERIC,
      },
    }).AsReadOnly();

    public static String GetString(HardwareIds device)
    {
      return device.Name switch
      {
        DEVO_HARDWARE_NAME.FM14 => "Filament Maker",
        DEVO_HARDWARE_NAME.GD17 => "Airid Dryer",
        DEVO_HARDWARE_NAME.GP20 => "Granulate Processor",
        _ => "Unknown device",
      };
    }

    public const UInt16 HARDWARE_VENDOR_ID_3DEVO = 0x16D0;

    public const UInt16 HARDWARE_VENDOR_ID_ST_GENERIC = 0x0483;
    public const UInt16 HARDWARE_PRODUCT_ID_ST_GENERIC = 0xDF11;
  }

  public class HardwareIds {
    public DevoHardware.DEVO_HARDWARE_NAME Name { get; set; }
    public UInt16 Vid { get; set; }
    public UInt16 Pid { get; set; }
  }

}
