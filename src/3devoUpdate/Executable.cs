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

namespace devoUpdate {
  abstract class Executable {
    private ProcessStartInfo processStartInfo;
    private Process execProcess;
    private Thread stdoutThread;
    private Thread stderrThread;
    private Action<object> onFinish;
    private object param;
    public event EventHandler OnProcessStart;
    public event EventHandler OnProcessEnd;
    private string binary;
    private bool processOutputStreamOpen;
    private bool processErrorStreamOpen;
    protected string outputLog { get; private set; }
    private Action<string> ConsoleOutputCallback = null;

    public enum OutputTo {
      Log,
      Console
    }

    protected bool StartProcessesses() {
      processStartInfo = new ProcessStartInfo {
        FileName = "",
        Arguments = "",
        CreateNoWindow = true,
        UseShellExecute = false,
        ErrorDialog = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
      };

      // Abort other processes if they are still busy
      AbortProcesses();

      if (execProcess == null)
        execProcess = new Process();

      CheckForValidProcess("Unable to start executable process.", false /*CheckForHasExited*/);

      execProcess.EnableRaisingEvents = true;
      execProcess.Exited += p_Exited;

      return true;
    }

    protected void AbortProcesses() {
      try {
        if( stdoutThread != null )
          stdoutThread.Abort();
      }
      catch( Exception ex ) {
        Console.WriteLine("ProcessIoManager.StopProcesses(); Exception caught on stopping stdout thread.\n" +
            "Exception Message:\n" + ex.Message + "\nStack Trace:\n" + ex.StackTrace);
      }

      try {
        if( stderrThread != null )
          stderrThread.Abort();
      }
      catch( Exception ex ) {
        Console.WriteLine("ProcessIoManager.StopProcesses(); Exception caught on stopping stderr thread.\n" +
            "Exception Message:\n" + ex.Message + "\nStack Trace:\n" + ex.StackTrace);
      }
      stdoutThread = null;
      stderrThread = null;
    }

    protected bool Launch( string args, Action<object> onFinish, object param, OutputTo outputTo ) {
      outputLog = "";

      this.onFinish = onFinish;
      this.param = param;

      return Launch(args, outputTo);
    }

    private bool Launch( string args, OutputTo outputTo ) {
      outputLog = "";

      if( binary == null ) 
        throw new Exception("Launch(): No executable set, forgot to call SetExecuteable()?");

      processStartInfo.FileName = binary;
      processStartInfo.Arguments = args;
      execProcess.StartInfo = processStartInfo;

      try {
        execProcess.Start();
      }
      catch( Exception ex ) {
        MsgBox.error("Unable to start process for console output", ex);
        return false;
      }

      OnProcessStart?.Invoke(this, EventArgs.Empty);

      if (outputTo == OutputTo.Console) {
        if( execProcess.StartInfo.RedirectStandardError ) {
          stderrThread = new Thread(() => ThreadConsoleUpdate(execProcess.StandardError));
          stderrThread.IsBackground = true;
          stderrThread.Start();
        }

        if( execProcess.StartInfo.RedirectStandardOutput ) {
          stdoutThread = new Thread(() => ThreadConsoleUpdate(execProcess.StandardOutput));
          stdoutThread.IsBackground = true;
          stdoutThread.Start();
        }
      }

      if( outputTo == OutputTo.Log ) {
        execProcess.OutputDataReceived += new DataReceivedEventHandler(OutputLogHandler);
        execProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorLogHandler);
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

    private const int OUTPUT_BUFFER_SIZE = 256;
    private void ThreadConsoleUpdate(StreamReader sr) {
      try {
        while ( execProcess != null ) {
          char[] buffer = new char[OUTPUT_BUFFER_SIZE];

          if( sr.Read(buffer, 0, buffer.Length) > 0 ) {
            string s = new string(buffer);

            ConsoleOutputCallback(s);
          } else {
            // END OF FILE
            break;
          }
        }
      }
      catch( Exception e ) {
        Console.WriteLine("ThreadConsoleUpdate(); Exception caught:" + e.Message + "\nStack Trace:" + e.StackTrace);
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

    private void CheckForValidProcess( string errorTextMessage, bool checkForHasExited ) {
      errorTextMessage = (errorTextMessage == null) ? "" : errorTextMessage.Trim();
      if( execProcess == null )
        throw new Exception("CheckForValidProcess(); The executable Process is not available. " + errorTextMessage);

      if( checkForHasExited && execProcess.HasExited )
        throw new Exception("CheckForValidProcess(); The executable Process has exited. " + errorTextMessage);
    }

    protected void WaitForExit() {
      try {
        CheckForValidProcess("Unable to start executable process.", true /*CheckForHasExited*/);
        execProcess.WaitForExit();
      } catch(Exception e) {
        Console.WriteLine("WaitForExit(); Process Exception caught: " + e.Message + "\nStack Trace:" + e.StackTrace);
      }

      // There might still be data in a buffer somewhere that needs to be read by the
      // output handler even after the process has ended.
      try {
        stderrThread.Join();
        stdoutThread.Join();
      } catch (Exception e) {
        Console.WriteLine("WaitForExit(); Thread Exception caught: " + e.Message + "\nStack Trace:" + e.StackTrace);
      }
    }

    protected bool SetExecutable(string binaryName, string directory) {
      if( binaryName.Length == 0)
        return false;

      binary = SearchForBinary(binaryName, directory);
      if( binary == null ) {
        MsgBox.error(binaryName + " is missing!");
        throw new System.IO.FileNotFoundException("File does not exist: ", binaryName);
      }

      return true;
    }

    public string SearchForBinary( string binaryName, string directory ) {
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
