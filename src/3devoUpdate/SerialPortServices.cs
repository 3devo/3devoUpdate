using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Contracts;
using System.IO.Ports;
using System.Management;

namespace avrdudess {
  public static class SerialPortService {
    private static string[] serial_ports;
    private static ManagementEventWatcher arrival;
    private static ManagementEventWatcher removal;



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
        System.Diagnostics.Debug.Write("\n\n");
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
    public static void CleanUp() {
      arrival.Stop();
      removal.Stop();
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
      }
      catch( ManagementException err ) {
        // debug info
        if( Constants.DEBUG_STATUS == true ) {
          System.Diagnostics.Debug.WriteLine("Failed to start MonitorDeviceChanges: " + err.ToString());
        }
      }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity")]
    private static void RaisePortsChangedIfNecessary( EventType eventType ) {
      lock( SerialPortService.serial_ports ) {
        // debug info
        if( Constants.DEBUG_STATUS == true ) {
          System.Diagnostics.Debug.WriteLine(
          "Ports Changed:\t" + eventType);
        }

        var availableSerialPorts = GetAvailableSerialPorts();
        if( !serial_ports.SequenceEqual(availableSerialPorts) ) {
          serial_ports = availableSerialPorts;
          PortsChanged.Raise(null, new PortsChangedArgs(eventType, serial_ports));

          // debug info
          if( Constants.DEBUG_STATUS == true ) {
            System.Diagnostics.Debug.Write("SerialPorts:");
            foreach( string port in serial_ports ) {
              System.Diagnostics.Debug.Write("\t");
              System.Diagnostics.Debug.Write(port);
            }
            System.Diagnostics.Debug.Write("\n\n");
          }
        }
      }
    }

    public static string[] GetAvailableSerialPorts() {
      return SerialPort.GetPortNames();
    }
  }

  public enum EventType {
    Insertion,
    Removal,
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
