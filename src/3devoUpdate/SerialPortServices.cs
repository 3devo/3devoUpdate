using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Contracts;
using System.IO.Ports;
using System.Management;
using System.Threading;
using System.Timers;
using ManagementException = avrdudess.ManagementException;

namespace devoUpdate {
  public static class SerialPortService {
    private static string[] serial_ports;
    private static ManagementEventWatcher arrival;
    private static ManagementEventWatcher removal;
    private static readonly object _syncObject = new object();
    private static volatile bool _Disposed = false;
    private static System.Timers.Timer timer;
    private static ElapsedEventHandler ElapsedHandler;

    // Set serial port service active
    static SerialPortService() {
      serial_ports = GetAvailableSerialPorts();
      MonitorDeviceChanges();

      // debug info
      if( Constants.DEBUG_STATUS == true ) {
        System.Diagnostics.Debug.Write("SerialPorts:");
        foreach( string port in serial_ports ) {
          System.Diagnostics.Debug.Write("\t");
          System.Diagnostics.Debug.Write(port);
        }
        System.Diagnostics.Debug.Write("\r\n\r\n");
      }
    }

    /// <summary>
    /// If this method isn't called, an InvalidComObjectException will be thrown (like below):
    /// System.Runtime.InteropServices.InvalidComObjectException was unhandled
    ///Message=COM object that has been separated from its underlying RCW cannot be used.
    ///Source=mscorlib
    ///StackTrace:
    ///     at System.StubHelpers.StubHelpers.StubRegisterRCW(Object pThis, IntPtr pThread)
    ///     at System.Management.IWbemServices.CancelAsyncCall_(IWbemObjectSink pSink)
    ///     at System.Management.SinkForEventQuery.Cancel()
    ///     at System.Management.ManagementEventWatcher.Stop()
    ///     at System.Management.ManagementEventWatcher.Finalize()
    ///InnerException:
    /// </summary>
    public static void Dispose( bool disposing ) {
      arrival.Stop();
      removal.Stop();

      lock( _syncObject ) {
        if( _Disposed ) {
          return;
        }

        if( disposing ) {
          _Disposed = disposing;
          timer.Stop();
          timer.Elapsed -= ElapsedHandler;
          timer.Dispose();
        }
      }
    }

    public static event EventHandler<PortsChangedArgs> PortsChanged;

    /// <summary>
    /// Tell subscribers, if any, that this event has been raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler">The generic event handler</param>
    /// <param name="sender">this or null, usually</param>
    /// <param name="args">Whatever you want sent</param>
    public static void Raise<T>( this EventHandler<T> handler, object sender, T args ) where T : EventArgs {
      // Copy to temp var to be thread-safe (taken from C# 3.0 Cookbook - don't know if it's true)
      EventHandler<T> copy = handler;
      if( copy != null ) {
        copy(sender, args);
      }
    }

    private static void MonitorDeviceChanges() {
      try {
        var deviceArrivalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
        var deviceRemovalQuery = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 3");

        arrival = new ManagementEventWatcher(deviceArrivalQuery);
        removal = new ManagementEventWatcher(deviceRemovalQuery);

        arrival.EventArrived += ( o, args ) => RaisePortsChangedIfNecessary(EventType.Insertion);
        removal.EventArrived += ( sender, eventArgs ) => RaisePortsChangedIfNecessary(EventType.Removal);

        // Start listening for events
        arrival.Start();
        removal.Start();

        lock( _syncObject ) {
            timer = new System.Timers.Timer();
            timer.AutoReset = false;
            timer.Interval = 1000 * 0.5;
            ElapsedHandler = new ElapsedEventHandler(Timer_Elapsed);
            timer.Elapsed += ElapsedHandler;
        }
      } catch( ManagementException err ) {
        // debug info
        if( Constants.DEBUG_STATUS == true ) {
          System.Diagnostics.Debug.WriteLine("Failed to start MonitorDeviceChanges: " + err.ToString());
        }
      }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity")]
    private static void RaisePortsChangedIfNecessary( EventType eventType ) {
      // debug info
      if( Constants.DEBUG_STATUS == true ) {
        System.Diagnostics.Debug.WriteLine(
        "Ports Changed:\t" + eventType);
      }

      lock( _syncObject ) {
        try {
          // Raise events can happen multiple times per device, depending if the device has multiple
          // interfaces when it's a composite device. To prevent too much workload when refreshing the 
          // device list of connected devices, only refresh the list after x milliseconds has elapsed.
          if( timer.Enabled == false )
            timer.Start();
        }
        catch( ObjectDisposedException ) {
          // Possible race condition with Dispose can cause an exception to trigger when underlying
          // timer is being disposed. Starting the timer fail in this case.
          // https://msdn.microsoft.com/en-us/library/b97tkt95(v=vs.110).aspx#Anchor_2
          if( _Disposed ) {
            // We still want to throw the exception in case someone really tries to start the timer
            // after disposal has finished. There's a slight race condition here where we might not
            // throw even though disposal is already done.
            // Since the offending code would most likely already be "failing" unreliably, it's 
            // probably ok in that sense to increase the "unreliable failure" time-window slightly.
            throw;
          }
        }

      }
    }

    public static string[] GetAvailableSerialPorts() {
      return SerialPort.GetPortNames();
    }

    private static void Timer_Elapsed( object sender, ElapsedEventArgs e ) {
      lock( _syncObject ) {
        try {
          timer.Stop();
        }
        catch( ObjectDisposedException ) {
          // See the try-catch construction at timer.Start() for more information.
          if( _Disposed ) {
            throw;
          }
        }
      }

      lock( SerialPortService.serial_ports ) {
        var availableSerialPorts = GetAvailableSerialPorts();
        if( !serial_ports.SequenceEqual(availableSerialPorts) ) {
          serial_ports = availableSerialPorts;

          // debug info
          if( Constants.DEBUG_STATUS == true ) {
            System.Diagnostics.Debug.Write("SerialPorts:");
            foreach( string port in serial_ports ) {
              System.Diagnostics.Debug.Write("\t");
              System.Diagnostics.Debug.Write(port);
            }
            System.Diagnostics.Debug.Write("\r\n\r\n");
          }
        }

        // Always fire this event, since we now need access to other device without 
        // a serial port as well (such as STM32 bootloader).
        PortsChanged.Raise(null, new PortsChangedArgs(EventType.All, serial_ports));
      }
    }
  }

  public enum EventType {
    Insertion,
    Removal,
    All,
  }

  public class PortsChangedArgs : EventArgs {
    private readonly EventType _eventType;
    private readonly string[] serial_ports;

    public PortsChangedArgs( EventType eventType, string[] serialPorts ) {
      _eventType = eventType;
      serial_ports = serialPorts;
    }

    public EventType EventType {
      get {
        return _eventType;
      }
    }

    public string[] SerialPorts {
      get {
        return serial_ports;
      }
    }


  }
}
