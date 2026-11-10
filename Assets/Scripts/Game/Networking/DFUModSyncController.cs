// Project:         Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Web Site:        http://www.dfworkshop.net
// Author:          Antigravity / DFU-Multiplayer Team
// Description:     Phase 18: Mods Catcher, SHA-256 Parity Validator & Authoritative modsettings.json Replication.

using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using Mirror;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport;

namespace DaggerfallWorkshop.Game.Networking
{
    /// <summary>
    /// Phase 18 Mod Parity Catcher & Configuration Sync Controller.
    /// Performs 1:1 mod manifest handshakes with SHA-256 checksums, diagnoses conflicts
    /// (missing/extra/mismatched mods), and authoritatively replicates host mod settings (e.g. Climates & Calories)
    /// with seamless non-destructive backup and rollback upon session disconnection.
    /// </summary>
    public class DFUModSyncController : MonoBehaviour
    {
        public static DFUModSyncController Instance { get; private set; }

        private const string BackupExtension = ".mp_backup.json";
        private const string SettingsFileName = "modsettings.json";

        // Cached SHA-256 hashes: filePath -> (lastModifiedUtc, computedHash)
        private static readonly Dictionary<string, (DateTime lastWrite, string hash)> hashCache =
            new Dictionary<string, (DateTime, string)>(StringComparer.OrdinalIgnoreCase);

        // Client-Side Parity State
        public bool IsManifestVerified { get; private set; } = true;
        public bool HasModConflicts { get; private set; } = false;
        public List<string> MissingMods { get; } = new List<string>();
        public List<string> ExtraMods { get; } = new List<string>();
        public List<string> VersionMismatches { get; } = new List<string>();
        public string DiagnosticSummary { get; private set; } = "OK";

        public event Action OnModCompatibilityEvaluated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #region Network Handlers Registration

        public void RegisterNetworkHandlers()
        {
            if (NetworkClient.active)
            {
                NetworkClient.ReplaceHandler<MsgModCompatibilityResponse>(OnClientReceiveModCompatibilityResponse);
                NetworkClient.ReplaceHandler<MsgModSettingsSync>(OnClientReceiveModSettingsSync);
            }
            if (NetworkServer.active)
            {
                NetworkServer.ReplaceHandler<MsgModManifestReport>(OnServerReceiveModManifestReport);
            }
        }

        public void UnregisterNetworkHandlers()
        {
            if (NetworkClient.active)
            {
                NetworkClient.UnregisterHandler<MsgModCompatibilityResponse>();
                NetworkClient.UnregisterHandler<MsgModSettingsSync>();
            }
            if (NetworkServer.active)
            {
                NetworkServer.UnregisterHandler<MsgModManifestReport>();
            }
        }

        #endregion

        #region Manifest Generation & SHA-256 Checksums

        /// <summary>
        /// Computes or retrieves cached SHA-256 checksum for a loaded .dfmod bundle.
        /// </summary>
        public static string ComputeModSha256(Mod mod)
        {
            if (mod == null) return "NULL";

            try
            {
                string bundlePath = Path.Combine(mod.DirPath ?? "", (mod.FileName ?? "") + ModManager.MODEXTENSION);

                if (File.Exists(bundlePath))
                {
                    DateTime lastWrite = File.GetLastWriteTimeUtc(bundlePath);

                    if (hashCache.TryGetValue(bundlePath, out var cached) && cached.lastWrite == lastWrite)
                    {
                        return cached.hash;
                    }

                    using (var sha = SHA256.Create())
                    using (var stream = new FileStream(bundlePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        byte[] hashBytes = sha.ComputeHash(stream);
                        string hashStr = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        hashCache[bundlePath] = (lastWrite, hashStr);
                        return hashStr;
                    }
                }
                else
                {
                    // Fallback for virtual mods in Unity Editor
                    string meta = $"{mod.Title}|{mod.ModInfo?.ModVersion ?? "1.0"}|{mod.GUID}";
                    using (var sha = SHA256.Create())
                    {
                        byte[] hashBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(meta));
                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DFU ModSync] Could not compute hash for '{mod.Title}': {ex.Message}");
                return "ERROR";
            }
        }

        /// <summary>
        /// Compiles a full manifest of all currently enabled mods on this machine.
        /// </summary>
        public List<ModManifestEntry> BuildLocalModManifest()
        {
            List<ModManifestEntry> entries = new List<ModManifestEntry>();

            if (ModManager.Instance == null || ModManager.Instance.Mods == null)
                return entries;

            foreach (var mod in ModManager.Instance.Mods)
            {
                if (mod == null || !mod.Enabled)
                    continue;

                string bundlePath = Path.Combine(mod.DirPath ?? "", (mod.FileName ?? "") + ModManager.MODEXTENSION);
                long fileSize = File.Exists(bundlePath) ? new FileInfo(bundlePath).Length : 0;
                string hash = ComputeModSha256(mod);

                entries.Add(new ModManifestEntry
                {
                    fileName = mod.FileName,
                    title = mod.Title,
                    version = mod.ModInfo?.ModVersion ?? "1.0",
                    guid = mod.GUID,
                    sha256Hash = hash,
                    fileSizeBytes = fileSize,
                    hasSettings = mod.HasSettings,
                    isEnabled = mod.Enabled
                });
            }

            return entries;
        }

        #endregion

        #region Client Manifest Dispatch & Verification

        /// <summary>
        /// Transmits this client's full mod manifest to the server for 1:1 parity handshake.
        /// </summary>
        public void SendClientManifestReport()
        {
            if (!NetworkClient.isConnected)
                return;

            List<ModManifestEntry> localManifest = BuildLocalModManifest();
            ModManifestBundle bundle = new ModManifestBundle
            {
                connectionId = DFUNetworkManager.Instance != null ? DFUNetworkManager.Instance.localConnectionId : -1,
                entries = localManifest
            };

            string json = SaveLoadManager.Serialize(typeof(ModManifestBundle), bundle, false);
            NetworkClient.Send(new MsgModManifestReport { manifestBundleJson = json });
        }

        private void OnClientReceiveModCompatibilityResponse(MsgModCompatibilityResponse msg)
        {
            IsManifestVerified = true;
            HasModConflicts = !msg.is100PercentMatch;
            DiagnosticSummary = msg.diagnosticSummary;

            MissingMods.Clear();
            if (!string.IsNullOrEmpty(msg.missingModsJson))
            {
                try
                {
                    var mList = SaveLoadManager.Deserialize(typeof(List<string>), msg.missingModsJson) as List<string>;
                    if (mList != null) MissingMods.AddRange(mList);
                }
                catch { }
            }

            ExtraMods.Clear();
            if (!string.IsNullOrEmpty(msg.extraModsJson))
            {
                try
                {
                    var eList = SaveLoadManager.Deserialize(typeof(List<string>), msg.extraModsJson) as List<string>;
                    if (eList != null) ExtraMods.AddRange(eList);
                }
                catch { }
            }

            VersionMismatches.Clear();
            if (!string.IsNullOrEmpty(msg.versionMismatchesJson))
            {
                try
                {
                    var vList = SaveLoadManager.Deserialize(typeof(List<string>), msg.versionMismatchesJson) as List<string>;
                    if (vList != null) VersionMismatches.AddRange(vList);
                }
                catch { }
            }

            OnModCompatibilityEvaluated?.Invoke();

            if (HasModConflicts)
            {
                DaggerfallUI.AddHUDText($"<color=#FF5555>[MOD CATCHER] ¡Conflicto de Mods! El servidor requiere paridad 1:1 ({DiagnosticSummary}).</color>");
            }
            else
            {
                DaggerfallUI.AddHUDText("<color=#55FF55>[MOD CATCHER] Paridad 1:1 verificada con el Host. Listo para jugar.</color>");
            }
        }

        #endregion

        #region Server-Side Manifest Evaluation

        private void OnServerReceiveModManifestReport(NetworkConnection conn, MsgModManifestReport msg)
        {
            if (conn == null || string.IsNullOrEmpty(msg.manifestBundleJson))
                return;

            ModManifestBundle clientBundle = null;
            try
            {
                clientBundle = SaveLoadManager.Deserialize(typeof(ModManifestBundle), msg.manifestBundleJson) as ModManifestBundle;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DFU ModSync] Failed to deserialize manifest from conn {conn.connectionId}: {ex.Message}");
                return;
            }

            if (clientBundle == null)
                return;

            List<ModManifestEntry> hostManifest = BuildLocalModManifest();
            EvaluateManifestParity(hostManifest, clientBundle.entries, out bool isMatch, out var missing, out var extra, out var mismatched, out string summary);

            // Respond back to client
            MsgModCompatibilityResponse resp = new MsgModCompatibilityResponse
            {
                is100PercentMatch = isMatch,
                missingModsJson = SaveLoadManager.Serialize(typeof(List<string>), missing, false),
                extraModsJson = SaveLoadManager.Serialize(typeof(List<string>), extra, false),
                versionMismatchesJson = SaveLoadManager.Serialize(typeof(List<string>), mismatched, false),
                diagnosticSummary = summary
            };

            conn.Send(resp);

            // Update lobby state for this peer
            if (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.serverLobbyPlayers.TryGetValue(conn.connectionId, out var playerDto))
            {
                playerDto.modsCompatible = isMatch;
                playerDto.modMismatchSummary = summary;
                DFUNetworkManager.Instance.BroadcastLobbyState();
            }

            // If 1:1 match is verified, push host's authoritative mod settings to this client
            if (isMatch)
            {
                BroadcastHostModSettings(conn);
            }
        }

        public static void EvaluateManifestParity(
            List<ModManifestEntry> hostList,
            List<ModManifestEntry> clientList,
            out bool isMatch,
            out List<string> missing,
            out List<string> extra,
            out List<string> mismatched,
            out string summary)
        {
            missing = new List<string>();
            extra = new List<string>();
            mismatched = new List<string>();

            Dictionary<string, ModManifestEntry> hostMap = new Dictionary<string, ModManifestEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in hostList ?? new List<ModManifestEntry>())
            {
                string key = !string.IsNullOrEmpty(h.guid) ? h.guid : h.fileName;
                if (!hostMap.ContainsKey(key))
                    hostMap.Add(key, h);
            }

            Dictionary<string, ModManifestEntry> clientMap = new Dictionary<string, ModManifestEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in clientList ?? new List<ModManifestEntry>())
            {
                string key = !string.IsNullOrEmpty(c.guid) ? c.guid : c.fileName;
                if (!clientMap.ContainsKey(key))
                    clientMap.Add(key, c);
            }

            // 1. Check missing mods (on host, missing on client)
            foreach (var kvp in hostMap)
            {
                if (!clientMap.ContainsKey(kvp.Key))
                {
                    missing.Add($"{kvp.Value.title} (v{kvp.Value.version})");
                }
            }

            // 2. Check extra mods (on client, missing on host)
            foreach (var kvp in clientMap)
            {
                if (!hostMap.ContainsKey(kvp.Key))
                {
                    extra.Add($"{kvp.Value.title} (v{kvp.Value.version})");
                }
            }

            // 3. Check version & SHA-256 hash mismatches
            foreach (var kvp in hostMap)
            {
                if (clientMap.TryGetValue(kvp.Key, out var clientMod))
                {
                    bool versionDiff = !string.Equals(kvp.Value.version, clientMod.version, StringComparison.OrdinalIgnoreCase);
                    bool hashDiff = !string.IsNullOrEmpty(kvp.Value.sha256Hash) &&
                                    !string.IsNullOrEmpty(clientMod.sha256Hash) &&
                                    kvp.Value.sha256Hash != "ERROR" && clientMod.sha256Hash != "ERROR" &&
                                    !string.Equals(kvp.Value.sha256Hash, clientMod.sha256Hash, StringComparison.OrdinalIgnoreCase);

                    if (versionDiff || hashDiff)
                    {
                        string reason = versionDiff ? $"Versión: {clientMod.version} vs Servidor: {kvp.Value.version}" : "Checksum SHA-256 Diferente (Archivo modificado)";
                        mismatched.Add($"{kvp.Value.title} ({reason})");
                    }
                }
            }

            isMatch = (missing.Count == 0 && extra.Count == 0 && mismatched.Count == 0);

            if (isMatch)
            {
                summary = $"1:1 Paridad ({hostList.Count} mods sincronizados)";
            }
            else
            {
                List<string> parts = new List<string>();
                if (missing.Count > 0) parts.Add($"Faltan: {missing.Count}");
                if (extra.Count > 0) parts.Add($"Extra: {extra.Count}");
                if (mismatched.Count > 0) parts.Add($"Mismatches: {mismatched.Count}");
                summary = string.Join(", ", parts.ToArray());
            }
        }

        #endregion

        #region Authoritative modsettings.json Replication & Rollback

        /// <summary>
        /// Reads all local active modsettings.json on the Host and broadcasts them to clients.
        /// </summary>
        public void BroadcastHostModSettings(NetworkConnection targetConn = null)
        {
            if (!NetworkServer.active || ModManager.Instance == null || ModManager.Instance.Mods == null)
                return;

            List<ModConfigPayload> configs = new List<ModConfigPayload>();

            foreach (var mod in ModManager.Instance.Mods)
            {
                if (mod == null || !mod.Enabled || !mod.HasSettings)
                    continue;

                try
                {
                    string cfgDir = mod.ConfigurationDirectory;
                    if (string.IsNullOrEmpty(cfgDir))
                        continue;

                    string settingsPath = Path.Combine(cfgDir, SettingsFileName);
                    if (File.Exists(settingsPath))
                    {
                        string json = File.ReadAllText(settingsPath);
                        configs.Add(new ModConfigPayload
                        {
                            guid = mod.GUID,
                            fileName = mod.FileName,
                            title = mod.Title,
                            settingsJson = json
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DFU ModSync] Could not read settings for '{mod.Title}': {ex.Message}");
                }
            }

            if (configs.Count == 0)
                return;

            string payloadJson = SaveLoadManager.Serialize(typeof(List<ModConfigPayload>), configs, false);
            MsgModSettingsSync syncMsg = new MsgModSettingsSync { configsJson = payloadJson };

            if (targetConn != null)
            {
                targetConn.Send(syncMsg);
            }
            else
            {
                NetworkServer.SendToAll(syncMsg);
            }

            Debug.Log($"[DFU ModSync] Broadcasted {configs.Count} authoritative mod configs to client(s).");
        }

        private void OnClientReceiveModSettingsSync(MsgModSettingsSync msg)
        {
            if (NetworkServer.active)
                return;
            if (string.IsNullOrEmpty(msg.configsJson) || ModManager.Instance == null)
                return;

            List<ModConfigPayload> configs = null;
            try
            {
                configs = SaveLoadManager.Deserialize(typeof(List<ModConfigPayload>), msg.configsJson) as List<ModConfigPayload>;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DFU ModSync] Failed to deserialize mod configs: {ex.Message}");
                return;
            }

            if (configs == null || configs.Count == 0)
                return;

            int appliedCount = 0;
            foreach (var payload in configs)
            {
                Mod targetMod = null;
                if (!string.IsNullOrEmpty(payload.guid))
                    targetMod = ModManager.Instance.GetModFromGUID(payload.guid);
                if (targetMod == null && !string.IsNullOrEmpty(payload.fileName))
                    targetMod = ModManager.Instance.GetMod(payload.fileName);
                if (targetMod == null && !string.IsNullOrEmpty(payload.title))
                    targetMod = ModManager.Instance.GetMod(payload.title);

                if (targetMod == null || string.IsNullOrEmpty(targetMod.ConfigurationDirectory))
                    continue;

                try
                {
                    Directory.CreateDirectory(targetMod.ConfigurationDirectory);
                    string realSettingsPath = Path.Combine(targetMod.ConfigurationDirectory, SettingsFileName);
                    string backupSettingsPath = Path.Combine(targetMod.ConfigurationDirectory, SettingsFileName + BackupExtension);

                    // Create backup of local configuration if not already backed up
                    if (File.Exists(realSettingsPath) && !File.Exists(backupSettingsPath))
                    {
                        File.Copy(realSettingsPath, backupSettingsPath, true);
                    }

                    // Write Host's authoritative configuration
                    File.WriteAllText(realSettingsPath, payload.settingsJson);

                    // Trigger live reload inside mod
                    if (targetMod.LoadSettingsCallback != null)
                    {
                        targetMod.LoadSettings();
                    }

                    appliedCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DFU ModSync] Failed to apply host config for '{payload.title}': {ex.Message}");
                }
            }

            Debug.Log($"[DFU ModSync] Applied {appliedCount} authoritative mod configs from Host.");
            DaggerfallUI.AddHUDText($"<color=#55FF55>[MOD CATCHER] Sincronizadas {appliedCount} reglas de mods autoritativas del Host.</color>");
        }

        /// <summary>
        /// Restores all original local mod configurations backed up during the multiplayer session.
        /// </summary>
        public void RestoreLocalModSettings()
        {
            if (ModManager.Instance == null || ModManager.Instance.Mods == null)
                return;

            int restoredCount = 0;
            foreach (var mod in ModManager.Instance.Mods)
            {
                if (mod == null || string.IsNullOrEmpty(mod.ConfigurationDirectory))
                    continue;

                try
                {
                    string realSettingsPath = Path.Combine(mod.ConfigurationDirectory, SettingsFileName);
                    string backupSettingsPath = Path.Combine(mod.ConfigurationDirectory, SettingsFileName + BackupExtension);

                    if (File.Exists(backupSettingsPath))
                    {
                        File.Copy(backupSettingsPath, realSettingsPath, true);
                        File.Delete(backupSettingsPath);

                        if (mod.LoadSettingsCallback != null)
                        {
                            mod.LoadSettings();
                        }
                        restoredCount++;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DFU ModSync] Could not restore backup config for '{mod.Title}': {ex.Message}");
                }
            }

            if (restoredCount > 0)
            {
                Debug.Log($"[DFU ModSync] Restored {restoredCount} local mod configurations from backup.");
            }
        }

        #endregion
    }
}
