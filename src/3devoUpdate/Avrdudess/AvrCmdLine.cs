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

    public AvrCmdLine( Form1 mainForm ) {
      this.mainForm = mainForm;
    }

    private void generateMain( bool addMCU = true ) {
      sb.Clear(); // .NET 4.0+ only
      sb.Length = 0;
      sb.Capacity = 0;

      if( mainForm.prog != null && mainForm.prog.name.Length > 0 )
        cmdLineOption("c", mainForm.prog.name);

      if( mainForm.mcu != null && mainForm.mcu.name.Length > 0 && addMCU )
        cmdLineOption("p", mainForm.mcu.name);

      if( mainForm.port.Length > 0 )
        cmdLineOption("P", mainForm.port);

      if( mainForm.baudRate.Length > 0 )
        cmdLineOption("b", mainForm.baudRate);

      if( mainForm.force )
        cmdLineOption("F");

      for( byte i = 0; i < mainForm.verbosity; i++ )
        cmdLineOption("v");
    }

    public void generate() {
      if( !mainForm.IsReady )
        return;

      generateMain();

      if( mainForm.disableVerify )
        cmdLineOption("V");

      if( mainForm.disableFlashErase )
        cmdLineOption("D");

      if( mainForm.eraseFlashAndEEPROM )
        cmdLineOption("e");

      if( mainForm.doNotWrite )
        cmdLineOption("n");

      if( mainForm.flashFile.Length > 0 )
        cmdLineOption("U", "flash:" + mainForm.flashFileOperation + ":\"" + mainForm.flashFile + "\":" + mainForm.flashFileFormat);

      mainForm.cmdBox = sb.ToString();
      // Debug info
      if( Constants.DEBUG_STATUS == true ) {
        System.Diagnostics.Debug.WriteLine("------------------------------------");
        System.Diagnostics.Debug.WriteLine("------------------------------------\n");
        System.Diagnostics.Debug.WriteLine("cmdLine generated:\n");
        System.Diagnostics.Debug.WriteLine(mainForm.cmdBox);
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
