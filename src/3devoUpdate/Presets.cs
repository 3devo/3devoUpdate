/*
 * Project: AVRDUDESS - A GUI for AVRDUDE
 * Author: Zak Kemble, contact@zakkemble.co.uk
 * Copyright: (C) 2013 by Zak Kemble
 * License: GNU GPL v3 (see License.txt)
 * Web: https://blog.zakkemble.net/avrdudess-a-gui-for-avrdude/
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace avrdudess {

  class Presets : XmlFile<List<PresetData>> {
    private const string FILE_PRESETS = "presets.xml";

    /* 3devo default settings */
    private const string PROGRAMMER = "arduino";
    private const string MCU = "m2560";
    private const string PORT = "";
    private const string BAUD = "115200";
    private const string FLASH_FORMAT = "i";
    private const string FLASH_FILE_OPERATION = "w";
    private const bool FORCE = false;
    private const bool DISABLE_VERIFY = true;
    private const bool DISABLE_FLASH_ERASE = false;
    private const bool ERASE_FLASH_AND_EEPROM = false;
    private const bool DO_NOT_WRITE = false;

    private Form1 mainForm;
    private List<PresetData> presetList;

    protected override object data {
      get { return presetList; }
      set { presetList = (List<PresetData>)value; }
    }

    // This should return a readonly list...
    public List<PresetData> presets {
      get { return presetList; }
    }

    public Presets( Form1 mainForm, string xmlFile = FILE_PRESETS )
        : base(xmlFile, "presets") {
      this.mainForm = mainForm;
      presetList = new List<PresetData>();
    }

    public void setDataSource( ComboBox cb, EventHandler handler ) {
      cb.SelectedIndexChanged -= handler;
      setDataSource(cb);
      cb.SelectedIndexChanged += handler;
    }

    public void setDataSource( ComboBox cb ) {
      cb.DataSource = null;
      cb.ValueMember = null;
      cb.BindingContext = new BindingContext();
      cb.DataSource = presetList;
      cb.DisplayMember = "name";
      cb.SelectedIndex = -1;
    }

    // New preset
    public void add( string name ) {
      presetList.Add(new PresetData(mainForm, name));
      bumpDefault();
    }

    // Delete preset
    public void remove( PresetData preset ) {
      presetList.Remove(preset);
      bumpDefault();
    }

    // Make sure default is at the top
    private void bumpDefault() {
      int idx = presetList.FindIndex(s => s.name == "Default");
      if( idx > 0 ) {
        PresetData p = presetList[idx];
        presetList.RemoveAt(idx);
        presetList.Insert(0, p);
      }
    }

    // Save presets
    public void save() {
      write();
    }

    // Load presets
    public void load() {
      // If file doesn't exist then make it
      if( !File.Exists(fileLocation) ) {
        add("Default");
        save();
      }

      // Load presets from XML
      read();
      if( presetList == null ) // Failed to load
      {
        presetList = new List<PresetData>();
      }
    }

    //Load 3devo Default settings
    public void load_3devo() {
      /* 3devo Default */
      mainForm.prog = new Programmer(PROGRAMMER);
      mainForm.mcu = new MCU(MCU);
      mainForm.port = PORT;
      mainForm.baudRate = BAUD;
      mainForm.flashFileFormat = FLASH_FORMAT;
      mainForm.flashFileOperation = FLASH_FILE_OPERATION;

      mainForm.force = FORCE;
      mainForm.disableVerify = DISABLE_VERIFY;
      mainForm.disableFlashErase = DISABLE_FLASH_ERASE;
      mainForm.eraseFlashAndEEPROM = ERASE_FLASH_AND_EEPROM;
      mainForm.doNotWrite = DO_NOT_WRITE;
    }
  }

  [XmlType(TypeName = "Preset")] // For backwards compatability with old (<v2.0) presets.xml
  public class PresetData {
    public string name { get; set; }

    public string programmer;
    public string mcu;
    public string port;
    public string baud;
    public string bitclock;
    public string flashFile;
    public string flashFormat;
    public string flashOp;
    public string EEPROMFile;
    public string EEPROMFormat;
    public string EEPROMOp;
    public bool force;
    public bool disableVerify;
    public bool disableFlashErase;
    public bool eraseFlashAndEEPROM;
    public bool doNotWrite;
    public string lfuse;
    public string hfuse;
    public string efuse;
    public bool setFuses;
    public string lockBits;
    public bool setLock;
    public string additional;
    public byte verbosity;

    public PresetData() {
    }

    public PresetData( Form1 mainForm, string name ) {
      this.name = name;
      programmer = (mainForm.prog != null) ? mainForm.prog.name : "";
      mcu = (mainForm.mcu != null) ? mainForm.mcu.name : "";
      port = mainForm.port;
      baud = mainForm.baudRate;
      flashFile = mainForm.flashFile;
      flashFormat = mainForm.flashFileFormat;
      flashOp = mainForm.flashFileOperation;
      force = mainForm.force;
      disableVerify = mainForm.disableVerify;
      disableFlashErase = mainForm.disableFlashErase;
      eraseFlashAndEEPROM = mainForm.eraseFlashAndEEPROM;
      doNotWrite = mainForm.doNotWrite;
      verbosity = mainForm.verbosity;
    }

    public void load( Form1 mainForm ) {
      mainForm.prog = new Programmer(programmer);
      mainForm.mcu = new MCU(mcu);
      mainForm.port = port;
      mainForm.baudRate = baud;
      mainForm.flashFile = flashFile;
      mainForm.flashFileOperation = flashOp;

      mainForm.force = force;
      mainForm.disableVerify = disableVerify;
      mainForm.disableFlashErase = disableFlashErase;
      mainForm.eraseFlashAndEEPROM = eraseFlashAndEEPROM;
      mainForm.doNotWrite = doNotWrite;
    }
  }
}


