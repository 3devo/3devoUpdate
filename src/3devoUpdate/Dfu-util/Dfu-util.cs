using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using avrdudess;

namespace devoUpdate {
  class DfuUtil : Executable {
    public const string EXECUTABLE_NAME_DFU_UTIL = "dfu-util";

    private UInt16 upload_status = 0;
    public enum CommandType : int {
      NONE = 0,
      GET_VERSION,
      FILE_UPLOAD,
    }

    public class DfuSeCommands {
      public string option { get; private set; }

      public DfuSeCommands( string option) {
        this.option = option;
      }

      public static DfuSeCommands NONE = new DfuSeCommands("");
      public static DfuSeCommands FORCE = new DfuSeCommands("force");
      public static DfuSeCommands LEAVE = new DfuSeCommands("leave");
      public static DfuSeCommands UNPROTECTED = new DfuSeCommands("unprotected");
      public static DfuSeCommands MASS_ERASE = new DfuSeCommands("mass-erase");
      public static DfuSeCommands WILL_RESET = new DfuSeCommands("will-reset");
    }

    private CommandType commandTypeDfu;
    #region Getters and setters

    public CommandType GetCommandType() {
      return commandTypeDfu;
    }

    public void SetCommandType( CommandType value ) {
      commandTypeDfu = value;
    }

    public string Version { get; private set; }
    public event EventHandler OnVersionChange;

    public string log {
      get { return outputLog; }
    }

    #endregion

    public void Init() {
      Version = "";

      base.SetConsoleOutputHandler(ConsoleOutputHandler);
      base.SetExecutable(EXECUTABLE_NAME_DFU_UTIL, Config.Prop.dfuUtilLoc);
    }

    public void ConsoleOutputHandler(string outputInfo) {
      // Debug info
      if( Constants.DEBUG_STATUS == true ) {
        System.Diagnostics.Debug.Write(outputInfo);
      }

      // HACK: This method raises at a very fast pace during the download process, preventing the 
      //       outputInfo text from being printed in the UI textbox. Adding a slight delay helps
      //       to slow down the method handling a bit.
      System.Threading.Thread.Sleep(1);

      switch( commandTypeDfu ) {
        case CommandType.FILE_UPLOAD:
          // Writing selected file
          if( upload_status == 0 ) {
            if( outputInfo.Contains("Download") && outputInfo.Contains("0%") ) {
              Util.consoleWrite("Uploading file: |");
              upload_status = 1;
            }
          }

          if( upload_status == 1 ) {
            if( outputInfo.Contains("=") )
              Util.consoleWrite("#");

            if( outputInfo.Contains("100%") )
              upload_status = 2;
          }

          if( outputInfo.Contains("File downloaded successfully") && (upload_status == 2) ) {
            Util.consoleWrite("| File upload successful!");
            upload_status = 3;
          }
          break;
        case CommandType.GET_VERSION:
          break;
        case CommandType.NONE:
          throw new System.InvalidOperationException("CommandType not supported");
          break;
      }
    }

    private void GetVersion() {
      Version = "";

      Launch("-V", CommandType.GET_VERSION, OutputTo.Log);
      WaitForExit();

      if( outputLog != null ) {
        string log = outputLog;
        int pos = log.IndexOf(EXECUTABLE_NAME_DFU_UTIL);
        if( pos > -1 ) {
          log = log.Substring(pos);
          Version = log.Substring(0, log.IndexOf('\n'));
        }
      }

      OnVersionChange?.Invoke(this, EventArgs.Empty);
    }

    public new void Launch( string args, Action<object> onFinish, object param, OutputTo outputTo = OutputTo.Console ) {
      if( outputTo == OutputTo.Console ) {
        // Debug info
        if( Constants.DEBUG_STATUS == true ) {
          System.Diagnostics.Debug.Write("\n\n~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ \n");
          System.Diagnostics.Debug.Write("~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ \n");
        }
      }

      upload_status = 0;
      base.StartProcessesses();
      base.Launch(args, onFinish, param, outputTo);
    }

    public void Launch( string args, CommandType commandType, OutputTo outputTo = OutputTo.Console ) {
      SetCommandType(commandType);
      Launch(args, null, null, outputTo);
    }

  }
}
