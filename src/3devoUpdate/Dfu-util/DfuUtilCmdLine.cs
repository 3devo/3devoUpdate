using System;
using System.Text;

namespace devoUpdate {
  class DfuUtilCmdLine {
    private Form1 mainForm;
    private StringBuilder sb = new StringBuilder();

    public DfuUtilCmdLine( Form1 mainForm ) {
      this.mainForm = mainForm;
    }

    public UInt16 vid;
    public UInt16 pid;
    public UInt64 serialNumber;
    public Int16 alternateInterfaceNr;
    public UInt32 dfuseAddress;
    public DfuUtil.DfuSeCommands dfuseCommandOptions;
    public uint verbosity;
    public string command;

    public void LoadAiridDryerDefaults() {
      alternateInterfaceNr = 0;
      dfuseAddress = 0x08000000;
      dfuseCommandOptions = DfuUtil.DfuSeCommands.LEAVE;
      verbosity = 0;
    }

    public void Generate() {
      ClearCommandlineOptions();

      // Dfu-Util doesn't look in the DfuSe file for the correct alternate setting number, so
      // for now we just add the alternate setting number to the parameters.
      if ( alternateInterfaceNr != -1)
        CmdLineOption("a", alternateInterfaceNr.ToString() );

      // Go out of the bootloader when finished.
      CmdLineOption("s", ":leave");

      // The BinaryFormatter method also pads the zero numbers at the start of the 
      // string (actually the end of the byte order), it is therefore necessary to
      // remove the first 4 characters of this string to get the 12 individual numbers.
      // Note: The serial number must be in capital letters, since Dfu-util aborts
      // the upload process otherwise (a lower case serial number is not seen as valid)
      if( serialNumber > 0 )
        CmdLineOption("S", "\"" + String.Format(new BinaryFormatter(), "{0:H}", serialNumber).Substring(4).ToUpper() + "\"");

      for( byte i = 0; i < verbosity; i++ )
        CmdLineOption("v");

      if (mainForm.flashFile.Length > 0)
        CmdLineOption("D", "\"" + mainForm.flashFile + "\"");

      command = GetCommand();

      // Debug info
      if( Constants.DEBUG_STATUS == true ) {
        System.Diagnostics.Debug.WriteLine("------------------------------------");
        System.Diagnostics.Debug.WriteLine("------------------------------------\n");
        System.Diagnostics.Debug.WriteLine("cmdLine generated:\n");
        System.Diagnostics.Debug.WriteLine(command);
        System.Diagnostics.Debug.WriteLine("\n------------------------------------");
        System.Diagnostics.Debug.WriteLine("------------------------------------\n");
      }
    }

    private void ClearCommandlineOptions() {
      sb.Clear(); // .NET 4.0+ only
      sb.Length = 0;
      sb.Capacity = 0;
    }

    private string GetCommand() {
      return sb.ToString();
    }

    private void CmdLineOption( string arg, string val ) {
      sb.Append(" -" + arg + " " + val);
    }

    private void CmdLineOption( string arg ) {
      sb.Append(" -" + arg);
    }

    private void CmdLineOptionAppend(string arg) {
      sb.Append(arg);
    }
  }
}
