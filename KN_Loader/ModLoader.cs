using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx;
using UnityEngine;

namespace KN_Loader {
  [BepInPlugin("1trbflxr.0kn_loader", "KN_Loader", StringVersion)]
  public class ModLoader : BaseUnityPlugin {
    public const int ClientVersion = 273;
    public const int ModVersion = 202;
    public const int Patch = 0;
    public const string StringVersion = "2.0.2";

    private const float UpdateCheckTime = 600.0f;

    public static ModLoader Instance { get; private set; }

    public ICore Core { get; private set; }

    public bool NewPatch { get; private set; }
    public bool BadVersion { get; private set; }
    public int LatestVersion { get; private set; }
    public int LatestPatch { get; private set; }
    public int LatestUpdater { get; private set; }
    public List<string> Changelog { get; private set; }
    public List<string> PatchNotes { get; private set; }

    public string LatestVersionString { get; private set; }

    public bool SaveUpdateLog { get; set; }
    public bool ForceUpdate { get; set; }
    public bool DevMode { get; set; }

    public bool ForceCheck { get; set; }

    public bool ShowUpdateWarn { get; set; }

    private float updateTimer_;

    public ModLoader() {
      if (!File.Exists(Paths.PluginPath + Path.DirectorySeparatorChar + "KN_Updater.dll")) {
        Log.Write("[KN_Loader]: Unable to locate KN_Updater.dll");
      }

#if !KN_DEV_TOOLS
      InitVersion();
      CheckVersion();
#endif

      UpdateVersion();

      Log.Write($"[KN_Loader]: Core status version: {ModVersion} / {LatestVersion}, patch: {Patch} / {LatestPatch}, " +
                $"updater: {LatestUpdater}, update: {ForceUpdate}");

      Updater.CheckForNewUpdater(LatestUpdater);

      Instance = this;
    }

    public static void SetCore(ICore core) {
      Instance.Core = core;
      if (core == null) {
        Instance.ForceUpdate = true;
      }
      else {
        core.OnInit();
      }
    }

    private void OnDestroy() {
      Updater.StartUpdater(LatestUpdater, ForceUpdate, DevMode, SaveUpdateLog, false);

      Core?.OnDeinit();
    }

    private void FixedUpdate() {
      Core?.FixedUpdate();
    }

    private void Update() {
      updateTimer_ += Time.deltaTime;
      if (updateTimer_ >= UpdateCheckTime || ForceCheck) {
        updateTimer_ = 0.0f;
        ForceCheck = false;

        Log.Write("[KN_Loader]: Checking mod version ...");
        new Task(() => {
#if !KN_DEV_TOOLS
          InitVersion();
          CheckVersion();
#endif
          UpdateVersion();
        }).Start();
      }

      Core?.Update();
    }

    private void LateUpdate() {
      Core?.LateUpdate();
    }

    private void OnGUI() {
      Core?.OnGui();
    }

    private void InitVersion() {
      Version.Initialize();
      LatestVersion = Version.GetVersion();
      LatestPatch = Version.GetPatch();
      LatestUpdater = Version.GetUpdaterVersion();
      Changelog = Version.GetChangelog();
      PatchNotes = Version.GetPatchNotes();
    }

    private void CheckVersion() {
      BadVersion = ClientVersion != GameVersion.version;
      ShowUpdateWarn = LatestVersion != 0 && ModVersion != LatestVersion;
      ForceUpdate = LatestPatch != Patch || BadVersion || ShowUpdateWarn;
      NewPatch = LatestPatch != Patch && ModVersion == LatestVersion;
    }

    private void UpdateVersion() {
      LatestVersionString = $"{LatestVersion}.{LatestPatch}";
      if (LatestVersionString.Length > 4) {
        LatestVersionString = LatestVersionString.Insert(1, ".");
        LatestVersionString = LatestVersionString.Insert(3, ".");
      }
      else {
        LatestVersionString = "unknown";
      }
    }
  }
}