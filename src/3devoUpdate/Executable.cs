/*
 * Project: AVRDUDESS - A GUI for AVRDUDE
 * Author: Zak Kemble, contact@zakkemble.co.uk
 * Copyright: (C) 2014 by Zak Kemble
 * License: GNU GPL v3 (see License.txt)
 * Web: https://blog.zakkemble.net/avrdudess-a-gui-for-avrdude/
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace avrdudess {
  abstract class Executable {
    private Process p;
    private Action<object> onFinish;
    private object param;
    public event EventHandler OnProcessStart;
    public event EventHandler OnProcessEnd;
    private string binary;
    private bool processOutputStreamOpen;
    private bool processErrorStreamOpen;
    private bool enableConsoleUpdate;
    protected string outputLog { get; private set; }
    private UInt16 upload_status = 0;

    public enum OutputTo {
      Log,
      Console
    }

    protected void load( string binaryName, string directory, bool enableConsoleWrite = true ) {

      binary = searchForBinary(binaryName, directory);

      if( binary == null )
        MsgBox.error(binaryName + " is missing!");
      else if( enableConsoleWrite ) {
        Thread t = new Thread(new ThreadStart(tConsoleUpdate));
        t.IsBackground = true;
        t.Start();
      }
    }

    private string searchForBinary( string binaryName, string directory ) {
      PlatformID os = Environment.OSVersion.Platform;
      if( os != PlatformID.MacOSX && os != PlatformID.Unix )
        binaryName += ".exe";

      string app;

      // Check user defined directory
      if( !String.IsNullOrEmpty(directory) ) {
        app = Path.Combine(directory, binaryName);
        if( File.Exists(app) )
          return app;
        return null;
      }

      // File exists in application directory (mainly for Windows)
      app = Path.Combine(AssemblyData.directory, binaryName);
      if( File.Exists(app) )
        return app;

      // File exists in working directory
      app = Path.Combine(Directory.GetCurrentDirectory(), binaryName);
      if( File.Exists(app) )
        return app;

      // Search PATHs
      string[] paths = Environment.GetEnvironmentVariable("PATH").Split(new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
      foreach( string path in paths ) {
        app = Path.Combine(path, binaryName);
        if( File.Exists(app) )
          return app;
      }

      return null;
    }

    protected bool launch( string args, Action<object> onFinish, object param, OutputTo outputTo ) {
      // Another process is active
      if( isActive() )
        return false;

      // Clear log
      outputLog = "";
      Util.consoleClear();

      // Binary is missing
      if( binary == null || !File.Exists(binary) )
        return false;

      this.onFinish = onFinish;
      this.param = param;

      return launch(args, outputTo);
    }

    private bool launch( string args, OutputTo outputTo ) {
      Process tmp = new Process();
      tmp.StartInfo.FileName = binary;
      tmp.StartInfo.Arguments = args;
      tmp.StartInfo.CreateNoWindow = true;
      tmp.StartInfo.UseShellExecute = false;
      tmp.StartInfo.RedirectStandardOutput = true;
      tmp.StartInfo.RedirectStandardError = true;
      tmp.EnableRaisingEvents = true;
      if( outputTo == OutputTo.Log ) {
        tmp.OutputDataReceived += new DataReceivedEventHandler(outputLogHandler);
        tmp.ErrorDataReceived += new DataReceivedEventHandler(errorLogHandler);
      }
      tmp.Exited += new EventHandler(p_Exited);

      try {
        tmp.Start();
      }
      catch( Exception ex ) {
        MsgBox.error("Error starting process", ex);
        return false;
      }

      if( OnProcessStart != null )
        OnProcessStart(this, EventArgs.Empty);

      enableConsoleUpdate = (outputTo == OutputTo.Console);
      p = tmp;

      if( outputTo == OutputTo.Log ) {
        processOutputStreamOpen = true;
        processErrorStreamOpen = true;
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
      } else {
        processOutputStreamOpen = false;
        processErrorStreamOpen = false;
      }

      return true;
    }

    private void p_Exited( object sender, EventArgs e ) {
      if( upload_status == 0 )
        Util.consoleWrite("Uploading file failed!");

      if( OnProcessEnd != null )
        OnProcessEnd(this, EventArgs.Empty);

      if( onFinish != null )
        onFinish(param);
      onFinish = null;
    }

    // Progress bars don't work using async output, since it only fires when a new line is received
    // Problem: Slow if the process outputs a lot of text
    private void tConsoleUpdate() {
      upload_status = 0;
      while( true ) {
        Thread.Sleep(15);

        if( !enableConsoleUpdate )
          continue;

        try {
          if( p != null ) {
            char[] buff = new char[256];

            // TODO: read from stdError AND stdOut (AVRDUDE outputs stuff through stdError)
            if( p.StandardError.Read(buff, 0, buff.Length) > 0 ) {
              string s = new string(buff);
              //Util.consoleWrite(s);
              // Debug info
              if( Constants.DEBUG_STATUS == true ) {
                System.Diagnostics.Debug.Write(s);
              }

              // Connection problem occured 
              if( s.Contains("ser_open():")
                      || s.Contains("stk500")
                      || s.Contains("ser_send()")
                      || s.Contains("can't open device")
                  ) {
                Util.consoleClear();
                Util.consoleWrite("Connection problem...\n");
                Util.consoleWrite("\n");
                Util.consoleWrite("Possible problems:\n");
                Util.consoleWrite("  - Usb cable is disconnected.\n");
                Util.consoleWrite("  - COM port is already taken (possibly another program)\n");
                Util.consoleWrite("  - Not properly working usb cable.\n");
                Util.consoleWrite("\n");
                Util.consoleWrite("Before contacting service:\n");
                Util.consoleWrite("  - Disconnect and reconnect the filament extruder.\n");
                Util.consoleWrite("  - Try to upload again.\n");
                Util.consoleWrite("  - If this keeps on failing, contact service for support.\n");
                Util.consoleWrite("  - You can click on the 3devo logo to go directly to the contact page of our website.\n\n");
                upload_status = 0;
              }

              /* To do: more specific problem handling
              // COM port conection failed
              if (s.Contains("can't open device"))
              {
                  Util.consoleWrite("\tIs the filament extruder still connected?\n");
                  Util.consoleWrite("\tYes? Disconnect and reconnect the filament extruder.\n");
                  Util.consoleWrite("\tTry to upload again..\n");
                  Util.consoleWrite("\tIf it keeps on happening, contact service support..\n");
                  upload_status = 0;
              }

              // COM port conection failed
              if (s.Contains("programmer is not responding"))
              {
                  Util.consoleWrite("\tProgrammer is not responding..\n\n");
                  Util.consoleWrite("\tPossible problem is a not properly working usb cable..\n\n");
                  Util.consoleWrite("\tNeed help? Contact service for more support..\n");
                  upload_status = 0;
              }
              */

              // Writing selected file
              if( s.Contains("Writing |") ) {
                Util.consoleWrite("Uploading file");
                upload_status = 1;
              }

              // Writing selected file
              else if( (s.Contains("#")) && (upload_status == 1) ) {
                Util.consoleWrite(".");
              }

              if( (s.Contains("| 100%")) && (upload_status == 1) ) {
                upload_status = 2;
              }

              // Uploading was succesful
              if( s.Contains("avrdude.exe done.  Thank you.")
              && (upload_status == 2)
              ) {
                Util.consoleWrite("\n");
                Util.consoleWrite("Uploading file was successful!");
                upload_status = 3;

                // Debug info
                if( Constants.DEBUG_STATUS == true ) {
                  System.Diagnostics.Debug.Write("~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ \n");
                  System.Diagnostics.Debug.Write("~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ \n\n");
                }
              }
            }
          }
        }
        catch( Exception ) {

        }
      }
    }

    // These methods are needed to properly capture the process output for logging
    private bool logger( string s ) {
      if( s != null ) // A null is sent when the stream is closed
      {
        outputLog += s.Replace("\0", String.Empty) + Environment.NewLine;
        return true;
      }

      return false;
    }

    private void outputLogHandler( object sender, DataReceivedEventArgs e ) {
      processOutputStreamOpen = logger(e.Data);
    }

    private void errorLogHandler( object sender, DataReceivedEventArgs e ) {
      processErrorStreamOpen = logger(e.Data);
    }

    protected bool isActive() {
      return (p != null && !p.HasExited);
    }

    public bool kill() {
      if( !isActive() )
        return false;
      p.Kill();
      return true;
    }

    protected void waitForExit() {
      if( isActive() )
        p.WaitForExit();

      // There might still be data in a buffer somewhere that needs to be read by the output handler even after the process has ended
      while( processOutputStreamOpen && processErrorStreamOpen )
        Thread.Sleep(15);
    }
  }
}
