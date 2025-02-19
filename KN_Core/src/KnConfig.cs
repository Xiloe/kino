using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using BepInEx;
using KN_Loader;
using UnityEngine;

namespace KN_Core {
  internal enum ReadMode {
    None,
    Param,
    Controls
  }

  public enum Quality {
    Low,
    Medium
  }

  public class KnConfig {
    public const string CxUiCanvasName = "Root";
    public const string CxMainCameraTag = "MainCamera";

    public const string ConfigFile = "kn_config.xml";

    public const string FloatRegex = @"^[0-9]*(?:\.[0-9]*)?$";

    public const float Low = 0.0f;
    public const float Medium = 1.0f;

    public static string BaseDir { get; private set; }
    public static string ReplaysDir { get; private set; }
    public static string VisualsDir { get; private set; }
    public static string MapsDir { get; private set; }

    private Dictionary<string, object> params_;
    private readonly Dictionary<string, object> defaultParams_;

    private bool initialized_;

    public KnConfig() {
      params_ = new Dictionary<string, object>();
      defaultParams_ = new Dictionary<string, object>();

      BaseDir = Paths.PluginPath + Path.DirectorySeparatorChar + "KN_Base" + Path.DirectorySeparatorChar;
      if (!Directory.Exists(BaseDir)) {
        Directory.CreateDirectory(BaseDir);
      }

      ReplaysDir = BaseDir + Path.DirectorySeparatorChar + "replays" + Path.DirectorySeparatorChar;
      if (!Directory.Exists(ReplaysDir)) {
        Directory.CreateDirectory(ReplaysDir);
      }

      VisualsDir = BaseDir + Path.DirectorySeparatorChar + "visuals" + Path.DirectorySeparatorChar;
      if (!Directory.Exists(VisualsDir)) {
        Directory.CreateDirectory(VisualsDir);
      }

      MapsDir = BaseDir + Path.DirectorySeparatorChar + "maps" + Path.DirectorySeparatorChar;
      if (Directory.Exists(MapsDir)) {
        //todo: fixme
        Directory.Delete(MapsDir, true);
      }

      Log.Write($"[KN_Core::Config]: Base dir: '{BaseDir}'");
      LoadDefault();
    }

    public T Get<T>(string key) {
      try {
        return (T) params_[key];
      }
      catch (ArgumentNullException) {
        Log.Write($"[KN_Core::Config]: Key '{key}' is null");
      }
      catch (KeyNotFoundException) {
        Log.Write($"[KN_Core::Config]: Key '{key}' does not exists");
      }

      return default;
    }

    public void Set<T>(string key, T value) {
      try {
        params_[key] = value;
      }
      catch (ArgumentNullException) {
        Log.Write($"[KN_Core::Config]: Key '{key}' is null");
      }
      catch (KeyNotFoundException) {
        Log.Write($"[KN_Core::Config]: Key '{key}' does not exists");
      }
    }

    public void Write() {
      if (!initialized_) {
        return;
      }

      Log.Write($"[KN_Core::Config]: Saving config to '{ConfigFile}'");

      try {
        var settings = new XmlWriterSettings {Indent = true, IndentChars = "  ", Encoding = Encoding.UTF8};
        using (var writer = XmlWriter.Create(BaseDir + ConfigFile, settings)) {
          if (writer != null) {
            writer.WriteStartElement("config");

            //config values
            writer.WriteStartElement("params");
            foreach (var item in params_) {
              writer.WriteStartElement("item");

              writer.WriteAttributeString("key", item.Key);
              writer.WriteAttributeString("value", item.Value.ToString());
              writer.WriteAttributeString("type", item.Value.GetType().ToString());

              writer.WriteEndElement();
            }

            writer.WriteEndElement();

            Controls.Save(writer);

            writer.WriteEndElement();
          }
        }
        Log.Write($"[KN_Core::Config]: Config automatically saved to '{ConfigFile}'");
      }
      catch (Exception e) {
        Log.Write($"[KN_Core::Config]: Unable to read config, {e.Message}");
      }
    }

    public void Read() {
      Log.Write($"[KN_Core::Config]: Loading config from '{ConfigFile}'");

      try {
        var readMode = ReadMode.None;

        using (var reader = XmlReader.Create(BaseDir + ConfigFile)) {
          while (reader.Read()) {
            if (reader.NodeType == XmlNodeType.Element) {
              switch (reader.Name) {
                case "params":
                  readMode = ReadMode.Param;
                  break;
                case "controls":
                  readMode = ReadMode.Controls;
                  break;
              }
              Read(reader, readMode);
            }
          }
        }
      }
      catch (Exception e) {
        Log.Write($"[KN_Core::Config]: Unable to read config, {e.Message}");
      }

      Validate();
    }

    private void Read(XmlReader reader, ReadMode mode) {
      if (mode == ReadMode.Param) {
        if (!reader.HasAttributes) {
          return;
        }

        string key = reader.GetAttribute("key");
        string value = reader.GetAttribute("value");
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) {
          return;
        }

        string type = reader.GetAttribute("type");
        switch (type) {
          case "System.Single": {
            float.TryParse(value, out float val);
            params_[key] = val;
            break;
          }
          case "System.Boolean": {
            bool.TryParse(value, out bool val);
            params_[key] = val;
            break;
          }
          case "System.String": {
            params_[key] = value;
            break;
          }
        }
      }
      else if (mode == ReadMode.Controls) {
        Controls.Load(reader);
      }
    }

    private void LoadDefault() {
      defaultParams_["speed"] = 50.0f;
      defaultParams_["speed_multiplier"] = 2.0f;
      defaultParams_["freecam_speed"] = 5.0f;
      defaultParams_["freecam_speed_multiplier"] = 2.0f;

      defaultParams_["vinylcam_zoom"] = 0.0f;
      defaultParams_["vinylcam_shift_z"] = 0.0f;
      defaultParams_["vinylcam_shift_y"] = 0.0f;

      defaultParams_["r_points"] = false;
      defaultParams_["hide_cx_ui"] = true;
      defaultParams_["hide_names"] = false;
      defaultParams_["custom_backfire"] = true;
      defaultParams_["trash_autohide"] = false;
      defaultParams_["trash_autodisable"] = false;
      defaultParams_["grip_autodisable"] = true;
      defaultParams_["custom_tach"] = false;

      defaultParams_["cl_discard_distance"] = 90.0f;
      defaultParams_["lights_quality"] = Medium;

      defaultParams_["force_white_smoke"] = false;
      defaultParams_["subscribe"] = true;

      defaultParams_["use_tag"] = true;
      defaultParams_["tag_front"] = true;
      defaultParams_["collision_manager"] = true;

      // air
      defaultParams_["air_use_controlKey"] = true;
      defaultParams_["air_step_max"] = 3.0f;
      defaultParams_["air_height_max"] = 0.5f;
      defaultParams_["air_height_min"] = 0.01f;

      defaultParams_["save_updater_log"] = false;
      defaultParams_["locale"] = "en";

      Controls.LoadDefault();

      initialized_ = true;
    }

    private void Validate() {
      Log.Write("[KN_Core::Config]: Validating config ...");

      foreach (var p in defaultParams_) {
        if (!params_.ContainsKey(p.Key)) {
          params_[p.Key] = p.Value;
        }
        if (p.Key == "air_step_max") {
          params_[p.Key] = Mathf.Clamp((float) params_[p.Key], 0.01f, 3.0f);
        }
        else if (p.Key == "air_height_max") {
          params_[p.Key] = Mathf.Clamp((float) params_[p.Key], 0.25f, 0.5f);
        }
        else if (p.Key == "air_height_min") {
          params_[p.Key] = Mathf.Clamp((float) params_[p.Key], 0.01f, 0.1f);
        }
        else if (p.Key == "cl_discard_distance") {
          params_[p.Key] = Mathf.Clamp((float) params_[p.Key], 50.0f, 200.0f);
        }
        else if (p.Key == "lights_quality") {
          params_[p.Key] = Mathf.Clamp((float) params_[p.Key], Low, Medium);
        }
      }

      params_ = params_.Where(p => {
        if (defaultParams_.ContainsKey(p.Key)) {
          return true;
        }
        Log.Write($"[KN_Core::Config]: Removed entry '{p.Key}' -> '{p.Value}'");
        return false;
      }).ToDictionary(p => p.Key, p => p.Value);

      Controls.Validate();
    }

    public static string GetQuality(float val) {
      if (val >= Low - 0.5f && val <= Low + 0.5f) {
        return "low";
      }
      if (val >= Medium - 0.5f && val <= Medium + 0.5f) {
        return "medium";
      }
      return "low";
    }

    public static Quality GetQualityEnum(float val) {
      if (val >= Low - 0.5f && val <= Low + 0.5f) {
        return Quality.Low;
      }
      if (val >= Medium - 0.5f && val <= Medium + 0.5f) {
        return Quality.Medium;
      }
      return Quality.Low;
    }

    public static float RoundQuality(float val) {
      if (val >= Low - 0.5f && val <= Low + 0.5f) {
        return Low;
      }
      if (val >= Medium - 0.5f && val <= Medium + 0.5f) {
        return Medium;
      }
      return Low;
    }
  }
}