/*
 * Project: AVRDUDESS - A GUI for AVRDUDE
 * Author: Zak Kemble, contact@zakkemble.co.uk
 * Copyright: (C) 2013 by Zak Kemble
 * License: GNU GPL v3 (see License.txt)
 * Web: https://blog.zakkemble.net/avrdudess-a-gui-for-avrdude/
 */

using System;
using System.Windows.Forms;

namespace devoUpdate {
  static class Constants {
    public const bool DEBUG_STATUS = false;
  }

  static class Program {

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main( ) {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run(new Form1());
    }
  }
}
