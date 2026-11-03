// Project:         Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Web Site:        http://www.dfworkshop.net
// Author:          Antigravity / DFU-Multiplayer Team
// Description:     Multi-Channel Chat, Whispers & RPG Language Decryption Controller (Phase 17).

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Mirror;
using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace DaggerfallWorkshop.Game.Networking
{
    public enum ChatChannel : byte
    {
        Say = 0,
        Party = 1,
        Whisper = 2,
        DM = 3,
        OOC = 4,
        Roll = 5,
        System = 6
    }

    /// <summary>
    /// Phase 17 Chat Controller providing multi-channel communication, whisper, secret DM notes,
    /// command parsing (/say, /p, /w, /dm, /ooc, /roll), and Daggerfall RPG language skill scrambling.
    /// </summary>
    public class DMChatController : MonoBehaviour
    {
        public static DMChatController Instance { get; private set; }

        public bool IsChatOpen { get; private set; } = false;

        private string inputMessage = "";
        private ChatChannel activeChannel = ChatChannel.Party;
        private int selectedLanguageIndex = 0;
        private Vector2 logScrollPosition = Vector2.zero;
        private bool requestFocus = false;

        // Auto-fadeout tracking
        private float lastActivityTime = -100f;
        private const float VisibleDuration = 7.0f;
        private const float FadeDuration = 2.5f;

        private Texture2D chatBgTextureOpen;
        private Texture2D chatBgTextureClosed;
        private GUIStyle chatBoxStyleOpen;
        private GUIStyle chatBoxStyleClosed;

        public struct ChatEntry
        {
            public ChatChannel channel;
            public string senderName;
            public string targetName;
            public string message;
            public byte languageId;
            public bool isScrambled;
            public string originalLanguageName;
            public float timestamp;
        }

        private readonly List<ChatEntry> chatHistory = new List<ChatEntry>();
        private const int MaxHistoryEntries = 80;

        // Supported Daggerfall Languages
        public static readonly string[] LanguageNames = new string[]
        {
            "Común", "Orco", "Dragón", "Daédrico", "Gigante",
            "Arpía", "Duendecillo", "Centauro", "Ninfa", "Spriggan"
        };

        private static readonly DFCareer.Skills[] LanguageSkills = new DFCareer.Skills[]
        {
            DFCareer.Skills.None,
            DFCareer.Skills.Orcish,
            DFCareer.Skills.Dragonish,
            DFCareer.Skills.Daedric,
            DFCareer.Skills.Giantish,
            DFCareer.Skills.Harpy,
            DFCareer.Skills.Impish,
            DFCareer.Skills.Centaurian,
            DFCareer.Skills.Nymph,
            DFCareer.Skills.Spriggan
        };

        // Phonetic word banks for fantasy scrambling
        private static readonly Dictionary<byte, string[]> LanguagePhonemes = new Dictionary<byte, string[]>
        {
            { 1, new string[] { "ghor", "lagash", "mog", "durat", "thok", "bagronk", "krag", "drakh", "uruk", "skab" } }, // Orcish
            { 2, new string[] { "dovah", "fus", "ro", "dah", "krah", "viing", "od", "ah", "zah", "krii" } },             // Dragonish
            { 3, new string[] { "molag", "bal", "mehrunes", "dagon", "vaer", "kyn", "drem", "chaurus", "xivilai" } },     // Daedric
            { 4, new string[] { "garr", "thrum", "bolg", "krag", "bront", "gorm", "krun", "valg" } },                     // Giantish
            { 5, new string[] { "kree", "shree", "skra", "chirr", "kraw", "pyrr", "skreek" } },                             // Harpy
            { 6, new string[] { "pip", "zix", "fizz", "glik", "snark", "blik", "skrit" } },                                 // Impish
            { 7, new string[] { "thul", "khor", "vane", "gall", "phos", "tar", "trot" } },                                 // Centaurian
            { 8, new string[] { "sylv", "aera", "fae", "liri", "elua", "mira", "vanya" } },                                 // Nymph
            { 9, new string[] { "bark", "ygg", "thorn", "root", "sylvan", "bough", "bloom" } }                             // Spriggan
        };

        private GUIStyle chatBoxStyle;
        private GUIStyle chatEntryStyle;
        private GUIStyle channelBtnStyle;

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

        public void RegisterNetworkHandlers()
        {
            if (NetworkClient.active)
            {
                NetworkClient.RegisterHandler<MsgChatMessage>(OnClientReceiveChatMessage);
            }
            if (NetworkServer.active)
            {
                NetworkServer.RegisterHandler<MsgChatMessage>(OnServerReceiveChatMessage);
            }
        }

        public void UnregisterNetworkHandlers()
        {
            if (NetworkClient.active)
            {
                NetworkClient.UnregisterHandler<MsgChatMessage>();
            }
            if (NetworkServer.active)
            {
                NetworkServer.UnregisterHandler<MsgChatMessage>();
            }
        }

        private void Update()
        {
            // Don't allow opening chat if playing game is false or another modal UI window is open
            bool canUseChat = GameManager.Instance != null && GameManager.Instance.IsPlayingGame() &&
                              (DaggerfallUI.UIManager == null || DaggerfallUI.UIManager.TopWindow == null || DaggerfallUI.UIManager.TopWindow is DaggerfallHUD);

            if (!IsChatOpen)
            {
                if (canUseChat && Input.GetKeyDown(KeyCode.Y))
                {
                    OpenChat();
                }
            }
            else
            {
                // Lock player motor and input manager while chat input is active
                if (InputManager.Instance != null && !InputManager.Instance.IsPaused)
                    InputManager.Instance.IsPaused = true;

                if (GameManager.Instance != null && GameManager.Instance.PlayerMotor != null)
                    GameManager.Instance.PlayerMotor.CancelMovement = true;

                // Ensure cursor stays completely unlocked and visible while chat is open
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (InputManager.Instance != null)
                    InputManager.Instance.CursorVisible = true;

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseChat();
                }
            }
        }

        private void OnDisable()
        {
            if (IsChatOpen)
                CloseChat();
        }

        public void OpenChat()
        {
            IsChatOpen = true;
            requestFocus = true;
            lastActivityTime = Time.time;
            logScrollPosition.y = float.MaxValue;

            if (InputManager.Instance != null)
                InputManager.Instance.IsPaused = true;

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.PlayerMouseLook != null)
                {
                    GameManager.Instance.PlayerMouseLook.enableMouseLook = false;
                    GameManager.Instance.PlayerMouseLook.cursorActive = false;
                }
                if (GameManager.Instance.PlayerMotor != null)
                    GameManager.Instance.PlayerMotor.CancelMovement = true;
            }

            if (DaggerfallUI.Instance != null)
                DaggerfallUI.Instance.timeClosedInputMessageBox = Time.realtimeSinceStartup;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void CloseChat()
        {
            IsChatOpen = false;
            lastActivityTime = Time.time;

            if (InputManager.Instance != null)
                InputManager.Instance.IsPaused = false;

            if (DaggerfallUI.Instance != null)
                DaggerfallUI.Instance.timeClosedInputMessageBox = Time.realtimeSinceStartup;

            bool isOtherUiOpen = DaggerfallUI.UIManager != null && DaggerfallUI.UIManager.TopWindow != null && !(DaggerfallUI.UIManager.TopWindow is DaggerfallHUD);

            if (GameManager.Instance != null && GameManager.Instance.PlayerMouseLook != null)
            {
                GameManager.Instance.PlayerMouseLook.enableMouseLook = !isOtherUiOpen;
                GameManager.Instance.PlayerMouseLook.cursorActive = false;
            }

            if (!isOtherUiOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        #region Outgoing Message Parsing & Dispatch

        public void SendMessageFromInput(string rawText)
        {
            lastActivityTime = Time.time;
            ChatChannel channel = activeChannel;
            string targetPlayer = "";
            string payload = rawText;

            // Command prefix parsing
            if (rawText.StartsWith("/"))
            {
                string[] parts = rawText.Split(new char[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                string cmd = parts[0].ToLowerInvariant();

                if (cmd == "/s" || cmd == "/say")
                {
                    channel = ChatChannel.Say;
                    payload = rawText.Length > parts[0].Length ? rawText.Substring(parts[0].Length).Trim() : "";
                }
                else if (cmd == "/p" || cmd == "/party")
                {
                    channel = ChatChannel.Party;
                    payload = rawText.Length > parts[0].Length ? rawText.Substring(parts[0].Length).Trim() : "";
                }
                else if (cmd == "/ooc")
                {
                    channel = ChatChannel.OOC;
                    payload = rawText.Length > parts[0].Length ? rawText.Substring(parts[0].Length).Trim() : "";
                }
                else if (cmd == "/w" || cmd == "/whisper")
                {
                    channel = ChatChannel.Whisper;
                    if (parts.Length >= 3)
                    {
                        targetPlayer = parts[1];
                        payload = parts[2];
                    }
                    else
                    {
                        AddLocalSystemMessage("Uso: /w [NombreJugador] [Mensaje]");
                        return;
                    }
                }
                else if (cmd == "/dm" || cmd == "/gm")
                {
                    channel = ChatChannel.DM;
                    bool isHostDM = NetworkSession.IsHost || (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.localLobbyIsDm);
                    if (isHostDM)
                    {
                        // DM sending secret note to specific player: /dm [Jugador] [Mensaje]
                        if (parts.Length >= 3)
                        {
                            targetPlayer = parts[1];
                            payload = parts[2];
                        }
                        else
                        {
                            AddLocalSystemMessage("Uso DM: /dm [NombreJugador] [Mensaje]");
                            return;
                        }
                    }
                    else
                    {
                        // Player sending secret note to DM: /dm [Mensaje]
                        payload = rawText.Length > parts[0].Length ? rawText.Substring(parts[0].Length).Trim() : "";
                    }
                }
                else if (cmd == "/roll")
                {
                    string formula = parts.Length > 1 ? parts[1] : "1d20";
                    ExecuteDiceRollCommand(formula);
                    return;
                }
                else
                {
                    AddLocalSystemMessage($"Comando desconocido '{cmd}'. Canales: /say, /party, /w [nombre], /dm, /ooc, /roll [formula]");
                    return;
                }
            }

            if (string.IsNullOrEmpty(payload))
                return;

            string sName = GetLocalPlayerName();
            int localConnId = NetworkSession.IsHost ? 0 : DFUNetworkManager.Instance.localConnectionId;
            Vector3 worldPos = GameManager.Instance != null && GameManager.Instance.PlayerObject != null
                ? GameManager.Instance.PlayerObject.transform.position
                : Vector3.zero;

            int targetConnId = -1;
            if (channel == ChatChannel.Whisper && !string.IsNullOrEmpty(targetPlayer))
            {
                targetConnId = ResolveConnectionIdByName(targetPlayer);
                if (targetConnId == -1)
                {
                    AddLocalSystemMessage($"No se encontró al jugador '{targetPlayer}' en la sesión.");
                    return;
                }
            }
            else if (channel == ChatChannel.DM)
            {
                bool isHostDM = NetworkSession.IsHost || (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.localLobbyIsDm);
                if (isHostDM)
                {
                    targetConnId = ResolveConnectionIdByName(targetPlayer);
                    if (targetConnId == -1)
                    {
                        AddLocalSystemMessage($"No se encontró al jugador '{targetPlayer}' para enviarle el mensaje secreto.");
                        return;
                    }
                }
                else
                {
                    targetConnId = 0; // Route to DM Host
                }
            }

            MsgChatMessage msg = new MsgChatMessage
            {
                channel = (byte)channel,
                senderName = sName,
                senderConnectionId = localConnId,
                targetConnectionId = targetConnId,
                text = payload,
                languageId = (byte)selectedLanguageIndex,
                worldPosition = worldPos
            };

            // If local say, trigger overhead dialogue speech bubble
            if (channel == ChatChannel.Say && DFUNetworkManager.Instance != null)
            {
                DFUNetworkManager.Instance.BroadcastDialogueSpeech(0, sName, worldPos + Vector3.up * 1.8f, payload);
            }

            // Route packet
            if (NetworkServer.active)
            {
                RouteServerChatMessage(msg);
            }
            else if (NetworkClient.active)
            {
                NetworkClient.Send(msg);
            }
            else
            {
                // Singleplayer loopback
                DeliverLocalChatMessage(msg);
            }
        }

        private void ExecuteDiceRollCommand(string formula)
        {
            string clean = formula.Trim().ToLowerInvariant();
            Match match = Regex.Match(clean, @"^(\d+)?d(\d+)([+-]\d+)?$");
            if (!match.Success)
            {
                AddLocalSystemMessage("Formato de dado inválido. Ejemplos válidos: 1d20, 1d20+5, 3d6, 1d100");
                return;
            }

            int numDice = string.IsNullOrEmpty(match.Groups[1].Value) ? 1 : int.Parse(match.Groups[1].Value);
            int dieSides = int.Parse(match.Groups[2].Value);
            int modifier = 0;
            if (!string.IsNullOrEmpty(match.Groups[3].Value))
            {
                modifier = int.Parse(match.Groups[3].Value);
            }

            numDice = Mathf.Clamp(numDice, 1, 20);
            dieSides = Mathf.Clamp(dieSides, 2, 100);

            List<int> individualRolls = new List<int>();
            int sum = 0;
            for (int i = 0; i < numDice; i++)
            {
                int r = UnityEngine.Random.Range(1, dieSides + 1);
                individualRolls.Add(r);
                sum += r;
            }

            int finalTotal = sum + modifier;
            string rollsStr = individualRolls.Count > 1 ? $"[{string.Join(", ", individualRolls)}]" : $"[{individualRolls[0]}]";
            string modStr = modifier != 0 ? (modifier > 0 ? $" + {modifier}" : $" - {Mathf.Abs(modifier)}") : "";

            string critNote = "";
            if (numDice == 1 && dieSides == 20)
            {
                if (individualRolls[0] == 20) critNote = " <color=#FFDF78><b>¡ÉXITO CRÍTICO!</b></color>";
                else if (individualRolls[0] == 1) critNote = " <color=#FF5555><b>¡FALLO CRÍTICO!</b></color>";
            }

            string rollResultText = $"{formula}: {rollsStr}{modStr} = <b>{finalTotal}</b>{critNote}";

            MsgChatMessage rollMsg = new MsgChatMessage
            {
                channel = (byte)ChatChannel.Roll,
                senderName = GetLocalPlayerName(),
                senderConnectionId = NetworkSession.IsHost ? 0 : DFUNetworkManager.Instance.localConnectionId,
                targetConnectionId = -1,
                text = rollResultText,
                languageId = 0,
                worldPosition = Vector3.zero
            };

            if (NetworkServer.active)
                RouteServerChatMessage(rollMsg);
            else if (NetworkClient.active)
                NetworkClient.Send(rollMsg);
            else
                DeliverLocalChatMessage(rollMsg);
        }

        #endregion

        #region Server Routing & Language Scrambling

        private void OnServerReceiveChatMessage(NetworkConnection conn, MsgChatMessage msg)
        {
            RouteServerChatMessage(msg);
        }

        private void RouteServerChatMessage(MsgChatMessage msg)
        {
            ChatChannel channel = (ChatChannel)msg.channel;

            switch (channel)
            {
                case ChatChannel.Say:
                    // Broadcast to players within 25m distance (DM conn 0 always hears)
                    foreach (var clientConn in NetworkServer.connections.Values)
                    {
                        if (clientConn == null) continue;
                        Vector3 clientPos = GetPlayerPositionByConnection(clientConn.connectionId);
                        if (Vector3.Distance(clientPos, msg.worldPosition) <= 25f || clientConn.connectionId == 0)
                        {
                            clientConn.Send(msg);
                        }
                    }
                    break;

                case ChatChannel.Party:
                case ChatChannel.OOC:
                case ChatChannel.Roll:
                case ChatChannel.System:
                    // Broadcast to everyone in party (includes host connection 0 via Mirror)
                    NetworkServer.SendToAll(msg);
                    break;

                case ChatChannel.Whisper:
                    // Bilateral send to target and back to sender
                    if (NetworkServer.connections.TryGetValue(msg.targetConnectionId, out var targetConn) && targetConn != null)
                        targetConn.Send(msg);
                    if (NetworkServer.connections.TryGetValue(msg.senderConnectionId, out var senderConn) && senderConn != null && msg.senderConnectionId != msg.targetConnectionId)
                        senderConn.Send(msg);
                    break;

                case ChatChannel.DM:
                    // Bilateral secret communication between DM (0) and target/sender player
                    // 1. Send to DM (conn 0)
                    if (NetworkServer.connections.TryGetValue(0, out var dmConn) && dmConn != null)
                        dmConn.Send(msg);
                    // 2. Send to target player if target > 0
                    if (msg.targetConnectionId > 0 && NetworkServer.connections.TryGetValue(msg.targetConnectionId, out var tConn) && tConn != null)
                        tConn.Send(msg);
                    // 3. Send back to sender if sender > 0 and sender != target
                    if (msg.senderConnectionId > 0 && msg.senderConnectionId != msg.targetConnectionId && NetworkServer.connections.TryGetValue(msg.senderConnectionId, out var sConn) && sConn != null)
                        sConn.Send(msg);
                    break;
            }
        }

        #endregion

        #region Client Delivery & Language Decryption

        private void OnClientReceiveChatMessage(MsgChatMessage msg)
        {
            // Mirror delivers Send(msg) to host and remote clients uniformly without duplicates
            DeliverLocalChatMessage(msg);
        }

        private void DeliverLocalChatMessage(MsgChatMessage msg)
        {
            ChatChannel ch = (ChatChannel)msg.channel;
            bool isDM = NetworkSession.IsHost || (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.localLobbyIsDm);

            string displayedMessage = msg.text;
            bool isScrambled = false;
            string langName = (msg.languageId < LanguageNames.Length) ? LanguageNames[msg.languageId] : "Desconocido";

            // Evaluate exotic language decryption if not Common (0)
            if (msg.languageId > 0 && msg.languageId < LanguageSkills.Length)
            {
                DFCareer.Skills langSkill = LanguageSkills[msg.languageId];

                if (isDM)
                {
                    // DM always translates with tag
                    displayedMessage = $"<color=#FFDF78>[{langName} -> Traductor DM]</color> {msg.text}";
                }
                else
                {
                    int skillValue = 0;
                    if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                    {
                        skillValue = GameManager.Instance.PlayerEntity.Skills.GetPermanentSkillValue(langSkill);
                    }

                    if (skillValue >= 50)
                    {
                        // Fluent: clear text
                        displayedMessage = $"<color=#66DDFF>[{langName}]</color> {msg.text}";
                    }
                    else if (skillValue >= 20)
                    {
                        // Partial comprehension
                        isScrambled = true;
                        displayedMessage = $"<color=#EEDD88>[{langName} (Parcial)]</color> {ScrambleText(msg.text, msg.languageId, 0.5f)}";
                    }
                    else
                    {
                        // Incomprehensible: 100% scrambled
                        isScrambled = true;
                        displayedMessage = $"<color=#FF8866>[{langName}]</color> {ScrambleText(msg.text, msg.languageId, 1.0f)}";
                    }
                }
            }

            string resolvedTarget = "";
            if (ch == ChatChannel.Whisper || ch == ChatChannel.DM)
            {
                resolvedTarget = ResolvePlayerNameByConnection(msg.targetConnectionId);
            }

            ChatEntry entry = new ChatEntry
            {
                channel = ch,
                senderName = msg.senderName,
                targetName = resolvedTarget,
                message = displayedMessage,
                languageId = msg.languageId,
                isScrambled = isScrambled,
                originalLanguageName = langName,
                timestamp = Time.time
            };

            chatHistory.Add(entry);
            if (chatHistory.Count > MaxHistoryEntries)
                chatHistory.RemoveAt(0);

            lastActivityTime = Time.time;
            logScrollPosition.y = float.MaxValue;
            DaggerfallUI.Instance?.PlayOneShot(SoundClips.ButtonClick);
        }

        private string ScrambleText(string source, byte languageId, float scrambleRatio)
        {
            if (!LanguagePhonemes.TryGetValue(languageId, out string[] bank) || bank == null || bank.Length == 0)
                bank = LanguagePhonemes[1]; // Fallback to Orcish

            string[] words = source.Split(' ');
            List<string> result = new List<string>();

            for (int i = 0; i < words.Length; i++)
            {
                if (UnityEngine.Random.value <= scrambleRatio)
                {
                    int pick = UnityEngine.Random.Range(0, bank.Length);
                    string syllable = bank[pick];
                    if (char.IsUpper(words[i][0]))
                        syllable = char.ToUpper(syllable[0]) + syllable.Substring(1);
                    result.Add(syllable);
                }
                else
                {
                    result.Add(words[i]);
                }
            }

            return string.Join(" ", result);
        }

        public void AddLocalSystemMessage(string text)
        {
            chatHistory.Add(new ChatEntry
            {
                channel = ChatChannel.System,
                senderName = "Sistema",
                targetName = "",
                message = text,
                languageId = 0,
                isScrambled = false,
                originalLanguageName = "Común",
                timestamp = Time.time
            });
            lastActivityTime = Time.time;
            logScrollPosition.y = float.MaxValue;
        }

        #endregion

        #region Helpers

        private string GetLocalPlayerName()
        {
            string pName = "";
            if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                pName = GameManager.Instance.PlayerEntity.Name;

            if (string.IsNullOrEmpty(pName) || pName == "Nameless" || pName == "Adventurer")
            {
                if (DFUNetworkManager.Instance != null)
                {
                    if (!string.IsNullOrEmpty(DFUNetworkManager.Instance.localLobbyCharacterName) && DFUNetworkManager.Instance.localLobbyCharacterName != "Nameless")
                        pName = DFUNetworkManager.Instance.localLobbyCharacterName;
                    else if (!string.IsNullOrEmpty(DFUNetworkManager.Instance.localLobbyPlayerName))
                        pName = DFUNetworkManager.Instance.localLobbyPlayerName;
                }

                if (string.IsNullOrEmpty(pName) || pName == "Nameless" || pName == "Adventurer")
                {
                    string pref = PlayerPrefs.GetString("DFU_LastPlayerName", "");
                    if (!string.IsNullOrEmpty(pref) && pref != "Nameless")
                        pName = pref;
                }

                if (string.IsNullOrEmpty(pName) || pName == "Nameless" || pName == "Adventurer")
                {
                    pName = (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.localLobbyIsDm) ? "Dungeon Master" : "Aventurero";
                }

                if (GameManager.Instance != null && GameManager.Instance.PlayerEntity != null)
                    GameManager.Instance.PlayerEntity.Name = pName;
            }

            return pName;
        }

        private int ResolveConnectionIdByName(string pName)
        {
            if (string.IsNullOrEmpty(pName)) return -1;
            string clean = pName.Trim();

            // DM alias resolution
            if (string.Equals(clean, "dm", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(clean, "gm", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(clean, "master", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(clean, "host", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            // Connection ID direct lookup
            if (int.TryParse(clean, out int directId))
            {
                if (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.connectedPlayerProfiles.ContainsKey(directId))
                    return directId;
            }

            if (DFUNetworkManager.Instance == null) return -1;

            // 1. Exact match on connectedPlayerProfiles
            foreach (var kvp in DFUNetworkManager.Instance.connectedPlayerProfiles)
            {
                if (string.Equals(kvp.Value.playerName, clean, StringComparison.OrdinalIgnoreCase))
                    return kvp.Key;
            }

            // 2. Prefix match on connectedPlayerProfiles
            foreach (var kvp in DFUNetworkManager.Instance.connectedPlayerProfiles)
            {
                if (kvp.Value.playerName != null && kvp.Value.playerName.StartsWith(clean, StringComparison.OrdinalIgnoreCase))
                    return kvp.Key;
            }

            // 3. Match in serverLobbyPlayers
            if (DFUNetworkManager.Instance.serverLobbyPlayers != null)
            {
                foreach (var lp in DFUNetworkManager.Instance.serverLobbyPlayers.Values)
                {
                    if (string.Equals(lp.playerName, clean, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(lp.characterName, clean, StringComparison.OrdinalIgnoreCase) ||
                        (lp.characterName != null && lp.characterName.StartsWith(clean, StringComparison.OrdinalIgnoreCase)) ||
                        (lp.playerName != null && lp.playerName.StartsWith(clean, StringComparison.OrdinalIgnoreCase)))
                    {
                        return lp.connectionId;
                    }
                }
            }

            return -1;
        }

        private string ResolvePlayerNameByConnection(int connId)
        {
            if (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.connectedPlayerProfiles.TryGetValue(connId, out var p))
                return p.playerName;
            return connId == 0 ? "Dungeon Master" : $"Jugador {connId}";
        }

        private Vector3 GetPlayerPositionByConnection(int connId)
        {
            if (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.remotePlayerPuppets.TryGetValue(connId, out var puppet) && puppet != null)
                return puppet.transform.position;
            return Vector3.zero;
        }

        #endregion

        #region GUI Rendering

        private void OnGUI()
        {
            GUI.depth = -1015;
            InitStyles();

            // Calculate fadeout alpha
            float currentAlpha = 0f;
            if (IsChatOpen)
            {
                currentAlpha = 1f;
                lastActivityTime = Time.time;
            }
            else
            {
                float elapsed = Time.time - lastActivityTime;
                if (elapsed < VisibleDuration)
                {
                    currentAlpha = 1f;
                }
                else if (elapsed < VisibleDuration + FadeDuration)
                {
                    currentAlpha = 1f - ((elapsed - VisibleDuration) / FadeDuration);
                }
                else
                {
                    currentAlpha = 0f;
                }
            }

            // Completely skip drawing if fully faded out and chat is closed
            if (!IsChatOpen && currentAlpha <= 0.005f)
                return;

            // Handle Key events inside OnGUI for seamless textfield submission
            if (IsChatOpen && Event.current != null && Event.current.isKey)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    if (Event.current.type == EventType.KeyDown)
                    {
                        if (!string.IsNullOrEmpty(inputMessage.Trim()))
                        {
                            SendMessageFromInput(inputMessage.Trim());
                            inputMessage = "";
                        }
                        CloseChat();
                    }
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    if (Event.current.type == EventType.KeyDown)
                    {
                        CloseChat();
                    }
                    Event.current.Use();
                }
            }

            // Calculate layout and bottom margin accounting for docked Large HUD
            float chatWidth = 460f;
            float chatHeight = IsChatOpen ? 230f : 140f;
            float chatX = 14f;

            float bottomMargin = 16f;
            if (DaggerfallUI.Instance != null && DaggerfallUI.Instance.DaggerfallHUD != null &&
                DaggerfallUI.Instance.DaggerfallHUD.LargeHUD != null &&
                DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.Enabled)
            {
                float hudH = DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.ScreenHeight;
                if (hudH <= 0 && DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.Rectangle.height > 0)
                    hudH = DaggerfallUI.Instance.DaggerfallHUD.LargeHUD.Rectangle.height;
                bottomMargin += hudH;
            }

            float chatY = Screen.height - chatHeight - bottomMargin;
            Rect chatRect = new Rect(chatX, chatY, chatWidth, chatHeight);

            // Modulate GUI colors by fade alpha
            Color origColor = GUI.color;
            Color origContentColor = GUI.contentColor;
            GUI.color = new Color(origColor.r, origColor.g, origColor.b, origColor.a * currentAlpha);
            GUI.contentColor = new Color(origContentColor.r, origContentColor.g, origContentColor.b, origContentColor.a * currentAlpha);

            GUIStyle activeBoxStyle = IsChatOpen ? chatBoxStyleOpen : chatBoxStyleClosed;
            GUI.Box(chatRect, GUIContent.none, activeBoxStyle ?? GUI.skin.box);

            GUILayout.BeginArea(new Rect(chatRect.x + 6f, chatRect.y + 6f, chatWidth - 12f, chatHeight - 12f));

            // Chat Log Scroll View
            float logHeight = IsChatOpen ? (chatHeight - 48f) : (chatHeight - 12f);
            if (IsChatOpen)
            {
                logScrollPosition = GUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(logHeight));
            }
            else
            {
                // Clean transparent log without scrollbars when closed
                logScrollPosition = GUILayout.BeginScrollView(logScrollPosition, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none, GUILayout.Height(logHeight));
            }

            foreach (var entry in chatHistory)
            {
                string channelTag;
                string colorHex;

                switch (entry.channel)
                {
                    case ChatChannel.Say:
                        channelTag = "[Local]";
                        colorHex = "#EDE2C8"; // Light ivory
                        break;
                    case ChatChannel.Party:
                        channelTag = "[Grupo]";
                        colorHex = "#66DDFF"; // Cyan
                        break;
                    case ChatChannel.Whisper:
                        channelTag = $"[Susurro]";
                        colorHex = "#FF77DD"; // Pink
                        break;
                    case ChatChannel.DM:
                        channelTag = "[Secreto DM]";
                        colorHex = "#FFDF78"; // Amber
                        break;
                    case ChatChannel.OOC:
                        channelTag = "[OOC]";
                        colorHex = "#AAAAAA"; // Gray
                        break;
                    case ChatChannel.Roll:
                        channelTag = "[Tirada]";
                        colorHex = "#55FF55"; // Lime
                        break;
                    case ChatChannel.System:
                    default:
                        channelTag = "[Sistema]";
                        colorHex = "#FFEE55"; // Yellow
                        break;
                }

                string line;
                if (entry.channel == ChatChannel.Whisper)
                {
                    bool amSender = string.Equals(entry.senderName, GetLocalPlayerName(), StringComparison.OrdinalIgnoreCase);
                    string whisperTag = amSender ? $"[Susurro a {entry.targetName}]" : $"[Susurro de {entry.senderName}]";
                    line = $"<color={colorHex}><b>{whisperTag}:</b> {entry.message}</color>";
                }
                else if (entry.channel == ChatChannel.DM)
                {
                    bool amSender = string.Equals(entry.senderName, GetLocalPlayerName(), StringComparison.OrdinalIgnoreCase);
                    bool isDM = NetworkSession.IsHost || (DFUNetworkManager.Instance != null && DFUNetworkManager.Instance.localLobbyIsDm);
                    string dmTag;
                    if (isDM && amSender)
                        dmTag = $"[DM a {entry.targetName}]";
                    else if (isDM && !amSender)
                        dmTag = $"[DM de {entry.senderName}]";
                    else if (!isDM && amSender)
                        dmTag = "[Secreto al DM]";
                    else
                        dmTag = "[Secreto del DM]";
                    line = $"<color={colorHex}><b>{dmTag}:</b> {entry.message}</color>";
                }
                else
                {
                    line = $"<color={colorHex}><b>{channelTag} {entry.senderName}:</b> {entry.message}</color>";
                }
                GUILayout.Label(line, chatEntryStyle);
            }

            GUILayout.EndScrollView();

            // Bottom Input Bar (only when chat is open)
            if (IsChatOpen)
            {
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();

                // Active Channel Dropdown Button
                if (GUILayout.Button(activeChannel.ToString(), channelBtnStyle, GUILayout.Width(55), GUILayout.Height(22)))
                {
                    activeChannel = (ChatChannel)(((int)activeChannel + 1) % 5);
                }

                // Active Language Dropdown Button
                if (GUILayout.Button(LanguageNames[selectedLanguageIndex], channelBtnStyle, GUILayout.Width(75), GUILayout.Height(22)))
                {
                    selectedLanguageIndex = (selectedLanguageIndex + 1) % LanguageNames.Length;
                }

                // Text Input Field
                GUI.SetNextControlName("DMChatInput");
                inputMessage = GUILayout.TextField(inputMessage, GUILayout.ExpandWidth(true), GUILayout.Height(22));

                if (requestFocus)
                {
                    GUI.FocusControl("DMChatInput");
                    requestFocus = false;
                }

                // Send Button
                if (GUILayout.Button("Enviar", DarkFantasyPixelArtTheme.GoldButtonStyle, GUILayout.Width(50), GUILayout.Height(22)))
                {
                    if (!string.IsNullOrEmpty(inputMessage.Trim()))
                    {
                        SendMessageFromInput(inputMessage.Trim());
                        inputMessage = "";
                    }
                    CloseChat();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();

            // Restore GUI colors
            GUI.color = origColor;
            GUI.contentColor = origContentColor;
        }

        private void InitStyles()
        {
            if (chatBgTextureOpen == null)
                chatBgTextureOpen = CreateChatBackgroundTexture(32, 32, 0.65f, 0.85f);

            if (chatBgTextureClosed == null)
                chatBgTextureClosed = CreateChatBackgroundTexture(32, 32, 0.35f, 0.50f);

            if (chatBoxStyleOpen == null)
            {
                chatBoxStyleOpen = new GUIStyle(GUI.skin.box);
                chatBoxStyleOpen.normal.background = chatBgTextureOpen;
                chatBoxStyleOpen.border = new RectOffset(4, 4, 4, 4);
            }

            if (chatBoxStyleClosed == null)
            {
                chatBoxStyleClosed = new GUIStyle(GUI.skin.box);
                chatBoxStyleClosed.normal.background = chatBgTextureClosed;
                chatBoxStyleClosed.border = new RectOffset(4, 4, 4, 4);
            }

            if (chatEntryStyle == null)
            {
                chatEntryStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontSize = 11,
                    wordWrap = true
                };
            }

            if (channelBtnStyle == null)
            {
                channelBtnStyle = new GUIStyle(DarkFantasyPixelArtTheme.EmeraldButtonStyle)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold
                };
            }
        }

        private static Texture2D CreateChatBackgroundTexture(int width, int height, float fillAlpha, float borderAlpha)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color bgFill = new Color(0.06f, 0.05f, 0.08f, fillAlpha);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, bgFill);
                }
            }

            // Outer subtle bronze border
            Color border = new Color(0.55f, 0.42f, 0.24f, borderAlpha);
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, 0, border);
                tex.SetPixel(x, height - 1, border);
            }
            for (int y = 0; y < height; y++)
            {
                tex.SetPixel(0, y, border);
                tex.SetPixel(width - 1, y, border);
            }

            // Corner rivets
            Color goldStud = new Color(0.92f, 0.78f, 0.42f, borderAlpha);
            if (width > 4 && height > 4)
            {
                tex.SetPixel(1, 1, goldStud);
                tex.SetPixel(width - 2, 1, goldStud);
                tex.SetPixel(1, height - 2, goldStud);
                tex.SetPixel(width - 2, height - 2, goldStud);
            }

            tex.Apply();
            return tex;
        }

        #endregion
    }
}
