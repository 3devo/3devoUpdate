/*
 * Project: AVRDUDESS - A GUI for AVRDUDE
 * Author: Zak Kemble, contact@zakkemble.co.uk
 * Copyright: (C) 2013 by Zak Kemble
 * License: GNU GPL v3 (see License.txt)
 * Web: https://blog.zakkemble.net/avrdudess-a-gui-for-avrdude/
 */

using System;
using System.Text;
using devoUpdate;

namespace avrdudess {
  // Move this class to Avrdude class or somthing instead of it being on its own?
  // Maybe have stuff like setMCU("m328") in Avrdude class and do away with this one?
  // TODO: Improve this class
  class AvrCmdLine {
    private Form1 mainForm;
    private StringBuilder sb = new StringBuilder();

    public Programmer prog;
    public MCU mcu;
    public string baudRate;
    public bool force;
    public bool disableVerify;
    public bool disableFlashErase;
    public bool eraseFlashAndEEPROM;
    public bool doNotWrite;
    public string flashFileFormat;
    public string flashFileOperation;
    public byte verbosity;
    public string port;
    public string cmdBox;

    public AvrCmdLine( Form1 mainForm ) {
      this.mainForm = mainForm;
    }

    //Load 3devo Default settings
    public void LoadFilamentMakerDefaults() {
      prog = new Programmer("arduino");
      mcu = new MCU("m2560");
      port = "";
      baudRate = "115200";
      flashFileFormat = "i";
      flashFileOperation = "w";

      force = false;
      disableVerify = true;
      disableFlashErase = false;
      eraseFlashAndEEPROM = false;
      doNotWrite = false;
      verbosity = 1;
    }

    private void generateMain( bool addMCU = true ) {
      sb.Clear(); // .NET 4.0+ only
      sb.Length = 0;
      sb.Capacity = 0;

      if( prog != null && prog.name.Length > 0 )
        cmdLineOption("c", prog.name);

      if( mcu != null && mcu.name.Length > 0 && addMCU )
        cmdLineOption("p", mcu.name);

      if( port.Length > 0 )
        cmdLineOption("P", port);

      if( baudRate.Length > 0 )
        cmdLineOption("b", baudRate);

      if( force )
        cmdLineOption("F");

      for( byte i = 0; i < verbosity; i++ )
        cmdLineOption("v");
    }

    public void generate() {
      if( !mainForm.IsReady )
        return;

      generateMain();

      if( disableVerify )
        cmdLineOption("V");

      if( disableFlashErase )
        cmdLineOption("D");

      if( eraseFlashAndEEPROM )
        cmdLineOption("e");

      if( doNotWrite )
        cmdLineOption("n");

      if( mainForm.flashFile.Length > 0 )
        cmdLineOption("U", "flash:" + flashFileOperation + ":\"" + mainForm.flashFile + "\":" + flashFileFormat);

      cmdBox = sb.ToString();
      // Debug info
      if( Constants.DEBUG_STATUS == true ) {
        System.Diagnostics.Debug.WriteLine("------------------------------------");
        System.Diagnostics.Debug.WriteLine("------------------------------------\n");
        System.Diagnostics.Debug.WriteLine("cmdLine generated:\n");
        System.Diagnostics.Debug.WriteLine(cmdBox);
        System.Diagnostics.Debug.WriteLine("\n------------------------------------");
        System.Diagnostics.Debug.WriteLine("------------------------------------\n");
      }
    }

    private void cmdLineOption( string arg, string val ) {
      sb.Append("-" + arg + " " + val + " ");
    }

    private void cmdLineOption( string arg ) {
      sb.Append("-" + arg + " ");
    }
  }
}
