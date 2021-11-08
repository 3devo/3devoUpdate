using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace devoUpdate {
  class DfuSe_FileValidation {
    private readonly Form1 mainForm;
    private BinaryReader sr;

    // Variables used for firmware file validation in DfuSe format.
    // For more info, see ST's UM0391(non-official ST link): http://rc.fdr.hu/UM0391.pdf
    private static readonly char[] DFUSE_PREFIX_szSignature = { 'D', 'f', 'u', 'S', 'e'};
    private const byte DFUSE_PREFIX_bVersion = 0x01;
    private const int DFUSE_PREFIX_SIZE = 11;

    private static readonly char[] DFUSE_TARGET_PREFIX_szSignature = { 'T', 'a', 'r', 'g', 'e', 't' };
    private const int DFUSE_TARGET_PREFIX_SIZE = 274;

    private const int DFUSE_IMAGE_ELEMENT_SIZE = 8; // Minimum image size, this may be expanded by the amount of the dwElementSize

    private static readonly char[] DFUSE_SUFFIX_ucSignature = { 'U', 'F', 'D' }; // as in "DFU" in reverse order
    private const int DFUSE_SUFFIX_bLength = 16;
    private const int DFUSE_SUFFIX_CRC_SIZE = 4;

    private const UInt16 DFUSE_SUPPORTED_DFU_VERSION = 0x011A;

    private UInt32 dfuFileSize = 0;

    public DfuSe_FileValidation( Form1 mainForm ) {
      this.mainForm = mainForm;
    }

    public void FirmwareFileValidation( in USBDeviceList dfuDeviceInfo, in int DfuTargetAlternateSetting, in uint DfuElementAdress, in bool isBootloaderDevice ) {
      byte bTargets;

      if( Constants.DEBUG_STATUS == true )
        System.Diagnostics.Debug.WriteLine("FirmwareFileValidation(); Starting firmware file validation.");

      try {
        sr = new BinaryReader(File.Open(mainForm.flashFile, FileMode.Open, FileAccess.Read));
      }
      catch( Exception e ) {
        throw new Exception("Could not open Flash file: " + Environment.NewLine + e.Message);
      }

      try {
        // Check the DFU file version, size and amount of targets
        CheckPrefix(out bTargets);

        // Check the suffix for the file format information
        CheckSuffix(dfuDeviceInfo.VendorId, dfuDeviceInfo.ProductId, isBootloaderDevice);

        // We only support one target at the moment.
        if( bTargets == 1 ) {
          CheckTarget(1, DfuTargetAlternateSetting, DfuElementAdress);
        } else {
          throw new Exception("Too much or no programmable targets in file. Targets:" + bTargets.ToString());
        }

        if( Constants.DEBUG_STATUS == true )
          System.Diagnostics.Debug.WriteLine("FirmwareFileValidation(); Finished file validation succesfully.");

      } finally {
        try {
          sr.Close();
          sr.Dispose();
        }
        catch( Exception ex ) {
          Util.consoleWrite("Could not close and dispose the BinaryReader. Exception:" + ex.Message);
        }
      }
    }

    private void CheckPrefix( out byte targets ) {
      byte[] buffer = new byte[DFUSE_PREFIX_SIZE];
      Array.Clear(buffer, 0, buffer.Length);
      targets = 0; 

      // Check if the file contains the mandatory minimum amount of data following the spec.
      const int minimumFileSize = DFUSE_PREFIX_SIZE + DFUSE_TARGET_PREFIX_SIZE + DFUSE_IMAGE_ELEMENT_SIZE + DFUSE_SUFFIX_bLength;
      if( sr.BaseStream.Length < minimumFileSize ) {
        throw new Exception("File size too small, this DfuSe file might not be correct.");
      }

      try {
        sr.BaseStream.Position = 0; // Reset the file position to the beginning, just to be sure.
        buffer = sr.ReadBytes(DFUSE_PREFIX_SIZE);

        // Check if the DFU format version is supported.
        if( buffer[5] != DFUSE_PREFIX_bVersion ) {
          throw new Exception("File format is not the same, this DfuSe file might not be supported.");
        }

        // Check the file Signature in the file.
        for (int i = 0; i < DFUSE_PREFIX_szSignature.Length; i++) {
          if (DFUSE_PREFIX_szSignature[i] != buffer[i])
            throw new Exception("File signature is incorrect, this DfuSe file might be corrupt or incorrect.");
        }

        dfuFileSize = (uint)(buffer[9] << 24 | buffer[8] << 16 | buffer[7] << 8 | buffer[6]);
        targets = buffer[10];
      }
      catch( ArgumentOutOfRangeException ex ) {
        throw new Exception("Could not read the prefix size and signature, argument out of range. Exception: " + Environment.NewLine + ex.Message);
      }
      catch( Exception ex ) {
        throw new Exception(ex.Message);
      }
    }

    private void CheckSuffix( in ushort VID, in ushort PID, in bool isBootloaderDevice ) {
      byte[] buffer = new byte[DFUSE_SUFFIX_bLength];
      Array.Clear(buffer, 0, buffer.Length);

      int suffixSize;
      try {
        // Set the file cursor position before the actual bytes to read.
        // This is positioned from the EOF backwards to before the DfuSignature bytes.
        sr.BaseStream.Position = dfuFileSize - DFUSE_SUFFIX_ucSignature.Length - 1 /*bLength byte*/ - DFUSE_SUFFIX_CRC_SIZE;
        buffer = sr.ReadBytes(DFUSE_SUFFIX_ucSignature.Length + 1 /*bLength byte*/);

        // Check if the file signature is present in the file.
        for (int i = 0; i < DFUSE_SUFFIX_ucSignature.Length; i++) {
          if (DFUSE_SUFFIX_ucSignature[i] != buffer[i])
            throw new Exception("Selected file is not a valid DFU file, suffix signature incorrect.");
        }

        suffixSize = buffer[3];
        if( Constants.DEBUG_STATUS == true )
          System.Diagnostics.Debug.WriteLine("Suffix size: 0x{0:X}", suffixSize);

        // Check if the suffix size is what we would expect.
        if( suffixSize != DFUSE_SUFFIX_bLength ) {
          throw new Exception("Suffix size is not as expected. Expected:"
            + DFUSE_SUFFIX_bLength.ToString() + "; Received: " + suffixSize.ToString());
        }

        // TODO: Check CRC?
      } 
      catch( ArgumentOutOfRangeException ex ) {
        throw new Exception("Could not read the suffix size and signature, argument out of range. Exception: " + Environment.NewLine + ex.Message);
      }
      catch( Exception ex ) {
        throw new Exception(ex.Message);
      }

      try {
        Array.Clear(buffer, 0, buffer.Length);

        // Read the whole suffix array for later.
        sr.BaseStream.Position = dfuFileSize - DFUSE_SUFFIX_bLength;
        buffer = sr.ReadBytes(DFUSE_SUFFIX_bLength);

        // Verifying the rest of the suffix.
        ushort DfuVersion = (ushort)(buffer[7] << 8 | buffer[6]);
        if( DfuVersion != DFUSE_SUPPORTED_DFU_VERSION ) {
          throw new Exception("This application does not support this DFU file. Supported DFU version=" + DFUSE_SUPPORTED_DFU_VERSION + "; From file=" + DfuVersion);
        }

        // Firmware version bytes 0 and 1 are ignored, since those are not used right now (are zero).

        ushort vendorId = (ushort)(buffer[5] << 8 | buffer[4]);
        if( !isBootloaderDevice && vendorId != VID ) {
          throw new Exception("The machines's vendor ID does not match the file vendor ID. From COM-port=" + Convert.ToString(VID, 16).PadLeft(4, '0') + "; From file=" + Convert.ToString(vendorId, 16).PadLeft(4, '0'));
        }

        ushort productId = (ushort)(buffer[3] << 8 | buffer[2]);
        if ( !isBootloaderDevice && productId != PID) {
          throw new Exception("The machines's product ID does not match the file product ID. From COM-port=" + Convert.ToString(PID, 16).PadLeft(4, '0') + "; From file=" + Convert.ToString(productId, 16).PadLeft(4, '0'));
        }
      }
      catch( ArgumentOutOfRangeException ex ) {
        throw new Exception("Could not verify the file suffix, argument out of range. Exception: " + ex.Message);
      }
      catch( Exception ex ) {
        throw new Exception(ex.Message);
      }
    }

    private void CheckTarget(in byte target, in int DfuTargetAlternateSetting, in uint DfuElementAdress ) {
      byte[] buffer = new byte[DFUSE_TARGET_PREFIX_SIZE];
      Array.Clear(buffer, 0, buffer.Length);

      try {
        // Start at the end of the Prefix and read the first target prefix.
        // TODO: If used for muliple targets, this function need quite some rework in order
        // to iterate through the file until the correct target is found by using the dwTargetSize field.
        sr.BaseStream.Position = DFUSE_PREFIX_SIZE;
        buffer = sr.ReadBytes(DFUSE_TARGET_PREFIX_SIZE);

        // Check if the Target Prefix starts with the correct signature.
        for (int i = 0; i < DFUSE_TARGET_PREFIX_szSignature.Length; i++) {
          if (DFUSE_TARGET_PREFIX_szSignature[i] != buffer[i])
            throw new Exception("File signature is incorrect, this DfuSe file might be corrupt or incorrect.");
        }

        // Check if the alternate setting is zero.
        if( buffer[6] != DfuTargetAlternateSetting) {
          throw new Exception("This file Alternate Setting is not supported at the moment, this firmware file might be corrupt or incorrect.");
        }

        // The Target name in the target prefix section is not checked, it's not sure whether this name has any use at all.
        // The target name is not specified for verification purpuses in the DfuSe specification manual and Dfu-Util also
        // doesn't use the name for verification either, so we might as well omit this check.

        uint numberOfImageElements = (uint)(buffer[273] << 24 | buffer[272] << 16 | buffer[271] << 8 | buffer[270]);
        if(numberOfImageElements == 0)
          throw new Exception("Firmware images not found, this firmware file might be corrupt or incorrect.");
        else if( numberOfImageElements > 1)
          throw new Exception("Too many firmware images found which are not supported at the moment.");

        // First image section element check
        Array.Clear(buffer, 0, buffer.Length);
        sr.BaseStream.Position = DFUSE_PREFIX_SIZE + DFUSE_TARGET_PREFIX_SIZE;
        buffer = sr.ReadBytes(4); // Get the Image Address

        uint bufferImageElementAdress = (uint)(buffer[3] << 24 | buffer[2] << 16 | buffer[1] << 8 | buffer[0]);
        if( bufferImageElementAdress != DfuElementAdress ) {
          throw new Exception("The firmware flash address is incorrect, this firmware file might be corrupt or incorrect.");
        }
      }
      catch(ArgumentOutOfRangeException ex ) {
        throw new Exception("Could not read the suffix size and signature, argument out of range. Exception: " + Environment.NewLine + ex.Message);
      }
      catch(Exception ex ) {
        throw new Exception(ex.Message);
      }
    }

  } // Class DfuSe_FileValidation
} // Namespace devoUpdate
