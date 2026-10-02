using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Google Play release settings (identity, version, target API, AdMob app id). Applied by every Build Everything
    /// (<c>ConfigureProject</c>) and on demand from <b>Juice King ▸ Release ▸ Apply Play Store Settings</b>.
    /// Signing (keystore) stays manual: Player Settings ▸ Android ▸ Publishing Settings (RELEASE_CHECKLIST.md).
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        /// <summary>Google Play's target-API rule: within a year of the newest Android release (Android 16 = API 36).
        /// Raise it each year when Play Console announces the next deadline.</summary>
        const AndroidSdkVersions PlayTargetSdk = AndroidSdkVersions.AndroidApiLevel36;
        const string ReleaseVersion = "1.0.0";

        [MenuItem("Juice King/Release/Apply Play Store Settings")]
        public static void ApplyPlayStoreSettings()
        {
            ConfigureRelease();
            AssetDatabase.SaveAssets();
            Debug.Log("[JuiceKing] Play Store settings applied: " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)
                      + " v" + PlayerSettings.bundleVersion + " (" + PlayerSettings.Android.bundleVersionCode + "), target API "
                      + (int)PlayerSettings.Android.targetSdkVersion);
        }

        [MenuItem("Juice King/Release/Check Play Store Readiness")]
        public static void LogReleaseCheck()
        {
            var issues = PlayStoreBuildCheck.Issues(false);
            Debug.Log(issues.Count == 0 ? "[JuiceKing] Play Store check: ready"
                : "[JuiceKing] Play Store check:\n- " + string.Join("\n- ", issues));
        }

        static void ConfigureRelease()
        {
            PlayerSettings.companyName = ReleaseConfig.CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ReleaseConfig.BundleIdentifier);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ReleaseConfig.BundleIdentifier);
            // Only ever raise the version: the code must grow with every upload, so never reset it.
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion.StartsWith("0."))
                PlayerSettings.bundleVersion = ReleaseVersion;
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;
            if (string.IsNullOrEmpty(PlayerSettings.iOS.buildNumber) || PlayerSettings.iOS.buildNumber == "0") PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.Android.targetSdkVersion = PlayTargetSdk;
            ConfigureAdMobSettings();
            ConfigureAndroidIcons();
        }

        /// <summary>
        /// Android adaptive icon (Android 8+ launchers mask it to a circle / squircle): the full-bleed key art is the
        /// background layer, the foreground is empty. Only the centre 2/3 is guaranteed visible, which keeps the
        /// character. Needs Android Build Support; without it the icon kinds are not available and this does nothing.
        /// Legacy and round icons use the default icon (GameIcon.png).
        /// </summary>
        static void ConfigureAndroidIcons()
        {
            var back = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/UI/Icon/IconAdaptiveBack.png");
            var fore = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/UI/Icon/IconAdaptiveFore.png");
            if (back == null || fore == null) return;
            foreach (var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                if (!kind.ToString().Contains("Adaptive")) continue;
                var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
                foreach (var icon in icons)
                {
                    icon.SetTexture(back, 0);
                    if (icon.layerCount > 1) icon.SetTexture(fore, 1);
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
            }
        }

        /// <summary>
        /// Writes the AdMob app ids into the plugin's settings asset (Assets/GoogleMobileAds/Resources). The plugin puts the
        /// Android one in the manifest (<c>com.google.android.gms.ads.APPLICATION_ID</c>); a build without it crashes on launch.
        /// The settings class is internal to the plugin, so it is created through reflection and edited as a serialized object.
        /// </summary>
        static void ConfigureAdMobSettings()
        {
            var type = System.Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            var load = type?.GetMethod("LoadInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (load == null)
            {
                Debug.LogWarning("[JuiceKing] Google Mobile Ads plugin not found: AdMob app id not written");
                return;
            }
            var settings = load.Invoke(null, null) as ScriptableObject;
            if (settings == null) return;
            var so = new SerializedObject(settings);
            so.FindProperty("adMobAndroidAppId").stringValue = ReleaseConfig.AndroidAdAppKey;
            so.FindProperty("adMobIOSAppId").stringValue = ReleaseConfig.IosAdAppKey;
            // The game asks for no tracking permission (no ATT prompt); iOS builds that add it must fill this in.
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }
    }

    /// <summary>
    /// Runs before every Android build. Blocks a <b>release</b> build that Google Play would reject or that would crash
    /// (template package name, no AdMob app id, target API too low), and warns about what must change before the
    /// production rollout (Google test ad ids, no privacy policy URL, debug keystore). Development builds only warn.
    /// </summary>
    public class PlayStoreBuildCheck : IPreprocessBuildWithReport
    {
        const int PlayTargetSdkLevel = 36;

        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            bool dev = (report.summary.options & BuildOptions.Development) != 0;
            var blockers = Issues(true);
            foreach (var w in Issues(false))
                if (!blockers.Contains(w)) Debug.LogWarning("[Play Store] " + w);
            if (blockers.Count == 0) return;
            string msg = "[Play Store] release build blocked:\n- " + string.Join("\n- ", blockers);
            if (dev) Debug.LogWarning(msg);
            else throw new BuildFailedException(msg);
        }

        /// <summary>Blockers (<paramref name="blockersOnly"/>) or every open item.</summary>
        public static List<string> Issues(bool blockersOnly)
        {
            var list = new List<string>();
            string id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            if (string.IsNullOrEmpty(id) || id.Contains("DefaultCompany") || id.Contains("UnityTechnologies") || id.Contains("Unity-Technologies"))
                list.Add("package name is a template id (" + id + "): run Juice King ▸ Release ▸ Apply Play Store Settings");
            if (PlayerSettings.companyName == "DefaultCompany") list.Add("company name is DefaultCompany");
            if ((int)PlayerSettings.Android.targetSdkVersion != 0 && (int)PlayerSettings.Android.targetSdkVersion < 35)
                list.Add("target API " + (int)PlayerSettings.Android.targetSdkVersion + " is below Google Play's minimum");
            if (string.IsNullOrEmpty(ReleaseConfig.AndroidAdAppKey)) list.Add("AdMob Android app id missing (the app would crash on launch)");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0) list.Add("ARM64 is not enabled (64-bit requirement)");
            if (blockersOnly) return list;

            if ((int)PlayerSettings.Android.targetSdkVersion < (int)PlayTargetSdkLevel)
                list.Add("target API is not " + PlayTargetSdkLevel + " (Juice King ▸ Release ▸ Apply Play Store Settings)");
            if (ReleaseConfig.UsingTestAdIds(true)) list.Add("Google TEST AdMob ids in ReleaseConfig: fine for internal testing, replace before production");
            if (string.IsNullOrEmpty(ReleaseConfig.PrivacyPolicyUrl)) list.Add("ReleaseConfig.PrivacyPolicyUrl is empty (required in the app and in Play Console)");
            if (!PlayerSettings.Android.useCustomKeystore) list.Add("no upload keystore: Player Settings ▸ Android ▸ Publishing Settings ▸ Keystore Manager");
            if (!EditorUserBuildSettings.buildAppBundle) list.Add("Build App Bundle (Google Play) is off: Play only accepts .aab");
            return list;
        }
    }
}
