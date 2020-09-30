﻿/*
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

namespace devoUpdate {
  abstract class Executable {
    private ProcessStartInfo processStartInfo;
    private Process execProcess;
    private Action<object> onFinish;
    private object param;
    public event EventHandler OnProcessStart;
    public event EventHandler OnProcessEnd;
    private string binary;
    private bool processOutputStreamOpen;
    private bool processErrorStreamOpen;
    private bool enableConsoleUpdate;
    protected string outputLog { get; private set; }
    private Action<string> ConsoleOutputCallback = null;

    public enum OutputTo {
      Log,
      Console
    }

    protected void Load( string binaryName, string directory, bool enableConsoleWrite = true ) {
      binary = SearchForBinary(binaryName, directory);
      processStartInfo = new ProcessStartInfo {
        FileName = "",
        Arguments = "",
        CreateNoWindow = true,
        UseShellExecute = false,
        ErrorDialog = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
      };

      if( binary == null )
        MsgBox.error(binaryName + " is missing!");
      else if( enableConsoleWrite ) {
        Thread t = new Thread(new ThreadStart(tConsoleUpdate));
        t.IsBackground = true;
        t.Start();
      }
    }

    protected bool Launch( string args, Action<object> onFinish, object param, OutputTo outputTo ) {
      // Clear log
      outputLog = "";
      Util.consoleClear();

      // Binary is missing
      if( binary == null || !File.Exists(binary) )
        return false;

      this.onFinish = onFinish;
      this.param = param;

      return Launch(args, outputTo);
    }

    private bool Launch( string args, OutputTo outputTo ) {
      execProcess = new Process();

      processStartInfo.FileName = binary;
      processStartInfo.Arguments = args;
      execProcess.StartInfo = processStartInfo;

      if( outputTo == OutputTo.Log ) {
        execProcess.OutputDataReceived += new DataReceivedEventHandler(OutputLogHandler);
        execProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorLogHandler);
      }
      execProcess.Exited += p_Exited;

      try {
        execProcess.Start();
        execProcess.EnableRaisingEvents = true;
      }
      catch( Exception ex ) {
        MsgBox.error("Error starting process", ex);
        return false;
      }

      OnProcessStart?.Invoke(this, EventArgs.Empty);

      enableConsoleUpdate = (outputTo == OutputTo.Console);

      if( outputTo == OutputTo.Log ) {
        processOutputStreamOpen = true;
        processErrorStreamOpen = true;
        execProcess.BeginOutputReadLine();
        execProcess.BeginErrorReadLine();
      } else {
        processOutputStreamOpen = false;
        processErrorStreamOpen = false;
      }

      return true;
    }

    private void p_Exited( object sender, EventArgs e ) {
      OnProcessEnd?.Invoke(this, EventArgs.Empty);

      onFinish?.Invoke(param);
      onFinish = null;
    }

    public void SetConsoleOutputHandler(Action<string> CallbackFunction) {
      if (CallbackFunction != null)
        ConsoleOutputCallback = CallbackFunction;
    }

    // Progress bars don't work using async output, since it only fires when a new line is received
    // Problem: Slow if the process outputs a lot of text
    private void tConsoleUpdate() {
      while( true ) {
        Thread.Sleep(15);

        if( !enableConsoleUpdate )
          continue;

        try {
          if( execProcess != null ) {
            char[] buff = new char[256];

            // TODO: read from stdError AND stdOut (AVRDUDE outputs stuff through stdError)
            if( execProcess.StandardError.Read(buff, 0, buff.Length) > 0 ) {
              string s = new string(buff);

              ConsoleOutputCallback(s);
            }
          }
        }
        catch( Exception e ) {
          // TODO: handle these errors properly
        }
      }
    }

    // These methods are needed to properly capture the process output for logging
    private bool Logger( string s ) {
      if( s != null ) { // A null is sent when the stream is closed
        outputLog += s.Replace("\0", String.Empty) + Environment.NewLine;
        return true;
      }

      return false;
    }

    private void OutputLogHandler( object sender, DataReceivedEventArgs e ) {
      processOutputStreamOpen = Logger(e.Data);
    }

    private void ErrorLogHandler( object sender, DataReceivedEventArgs e ) {
      processErrorStreamOpen = Logger(e.Data);
    }

    private string SearchForBinary( string binaryName, string directory ) {
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
  }
}
