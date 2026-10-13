using System;
using System.Collections.Generic;
using System.Globalization;
using Mutiny.Levels;
using Mutiny.Network;
using Mutiny.Persistence;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Presentation
{
    [DisallowMultipleComponent]
    public sealed class MutinyGMManager : MonoBehaviour
    {
        public static MutinyGMManager Instance { get; private set; }
        public const float CanvasReferenceWidth = 550f;
        public const float CanvasReferenceHeight = 400f;
        public const float BaseButtonSize = 30f;
        public const float BaseButtonMargin = 4f;
        public const int MaxRecentSuccessfulCommands = 5;

        // Kept for backward compatibility
        public const float ButtonSize = BaseButtonSize;

        public static float CalculateScale(float screenWidth, float screenHeight)
        {
            return Mathf.Min(screenWidth / CanvasReferenceWidth, screenHeight / CanvasReferenceHeight);
        }

        public static Rect ResolveButtonRect(float screenHeight)
        {
            return ResolveButtonRect(screenHeight * (CanvasReferenceWidth / CanvasReferenceHeight), screenHeight);
        }

        public static Rect ResolveButtonRect(float screenWidth, float screenHeight)
        {
            float scale = CalculateScale(screenWidth, screenHeight);
            float size = BaseButtonSize * scale;
            float canvasLeft = (screenWidth - CanvasReferenceWidth * scale) * 0.5f;
            float canvasTop = (screenHeight - CanvasReferenceHeight * scale) * 0.5f;
            float x = canvasLeft + BaseButtonMargin * scale;
            float y = canvasTop + (CanvasReferenceHeight * scale - size) * 0.5f;
            return new Rect(x, y, size, size);
        }

        private bool m_IsOpen = false;
        public bool IsOpen => m_IsOpen;
        private bool m_LockOpen = true;
        public bool LockOpen { get => m_LockOpen; set => m_LockOpen = value; }
        public bool IsPinned => m_LockOpen;
        private string m_StatusMessage = "Mutiny GM menu ready. Press a button to apply or toggle it.";
        private Color m_StatusColor = new Color(0.4f, 1.0f, 0.5f, 1.0f);
        private readonly List<string> m_RecentSuccessfulCommands = new List<string>();
        public IReadOnlyList<string> RecentSuccessfulCommands => m_RecentSuccessfulCommands;

        // Luck values the AI buttons cycle through. One shared selector drives
        // both the per-level AI override and the one-turn takeover.
        private static readonly float[] AiLuckPresets = { 0f, 5f, 10f, 25f, 50f, 100f, 250f, 1000f, 99999f };
        private int m_AiLuckPresetIndex = 1;

        // GUI Styles and Textures
        private Texture2D m_CircleNormalTex;
        private Texture2D m_CircleHoverTex;
        private Texture2D m_PanelBackgroundTex;
        private Texture2D m_ButtonBackgroundTex;
        private Texture2D m_ButtonActiveBackgroundTex;
        private Texture2D m_LockActiveBackgroundTex;
        private Texture2D m_LockLockedTex;
        private Texture2D m_LockUnlockedTex;

        private GUIStyle m_CircleButtonStyle;
        private GUIStyle m_PanelHeaderStyle;
        private GUIStyle m_ActionButtonStyle;
        private GUIStyle m_ActionButtonActiveStyle;
        private GUIStyle m_LockActionButtonStyle;
        private GUIStyle m_LockActiveButtonStyle;
        private GUIStyle m_StatusLabelStyle;
        private GUIStyle m_HintLabelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null)
                return;

            GameObject host = new GameObject("MutinyGMManager");
            DontDestroyOnLoad(host);
            host.AddComponent<MutinyGMManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void EnsureResources(float scale)
        {
            if (m_CircleNormalTex == null)
            {
                const int circleTexResolution = 128;
                m_CircleNormalTex = CreateCircleTexture(circleTexResolution, new Color(0.2f, 0.2f, 0.2f, 0.70f), new Color(0.6f, 0.6f, 0.6f, 0.85f), 6f);
                m_CircleHoverTex = CreateCircleTexture(circleTexResolution, new Color(0.35f, 0.35f, 0.35f, 0.90f), new Color(1.0f, 0.85f, 0.3f, 1.0f), 6f);

                m_PanelBackgroundTex = CreateSolidTexture(new Color(0.08f, 0.09f, 0.12f, 0.90f));
                m_ButtonBackgroundTex = CreateSolidTexture(new Color(0.24f, 0.26f, 0.34f, 0.95f));
                m_ButtonActiveBackgroundTex = CreateSolidTexture(new Color(0.20f, 0.42f, 0.28f, 0.95f));
                m_LockActiveBackgroundTex = CreateSolidTexture(new Color(0.20f, 0.38f, 0.30f, 0.95f));

                m_LockLockedTex = CreateLockTexture(64, true, new Color(1.0f, 0.86f, 0.35f, 1.0f));
                m_LockUnlockedTex = CreateLockTexture(64, false, new Color(0.72f, 0.76f, 0.84f, 0.90f));

                m_CircleButtonStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                m_CircleButtonStyle.normal.textColor = Color.white;
                m_CircleButtonStyle.hover.textColor = new Color(1f, 0.92f, 0.45f);

                m_PanelHeaderStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold
                };
                m_PanelHeaderStyle.normal.textColor = new Color(0.95f, 0.85f, 0.45f);

                m_ActionButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                m_ActionButtonStyle.normal.background = m_ButtonBackgroundTex;
                m_ActionButtonStyle.normal.textColor = Color.white;
                m_ActionButtonStyle.hover.textColor = new Color(1f, 0.92f, 0.45f);

                m_ActionButtonActiveStyle = new GUIStyle(m_ActionButtonStyle);
                m_ActionButtonActiveStyle.normal.background = m_ButtonActiveBackgroundTex;
                m_ActionButtonActiveStyle.hover.background = m_ButtonActiveBackgroundTex;
                m_ActionButtonActiveStyle.hover.textColor = new Color(0.85f, 1f, 0.85f);

                m_LockActionButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    imagePosition = ImagePosition.ImageOnly
                };
                m_LockActionButtonStyle.normal.background = m_ButtonBackgroundTex;

                m_LockActiveButtonStyle = new GUIStyle(m_LockActionButtonStyle);
                m_LockActiveButtonStyle.normal.background = m_LockActiveBackgroundTex;

                m_StatusLabelStyle = new GUIStyle
                {
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true
                };
                m_StatusLabelStyle.normal.textColor = m_StatusColor;

                m_HintLabelStyle = new GUIStyle(m_StatusLabelStyle);
                m_HintLabelStyle.normal.textColor = new Color(0.65f, 0.70f, 0.80f, 0.9f);
            }

            // Dynamically scale fonts and paddings relative to canvas scale
            m_CircleButtonStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(10f * scale));
            m_PanelHeaderStyle.fontSize = Mathf.Max(11, Mathf.RoundToInt(12f * scale));

            m_ActionButtonStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(11f * scale));
            m_ActionButtonActiveStyle.fontSize = m_ActionButtonStyle.fontSize;
            int lockPad = Mathf.Max(2, Mathf.RoundToInt(2.5f * scale));
            m_LockActionButtonStyle.padding = new RectOffset(lockPad, lockPad, lockPad, lockPad);
            m_LockActiveButtonStyle.padding = new RectOffset(lockPad, lockPad, lockPad, lockPad);

            m_StatusLabelStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(9.5f * scale));
            m_HintLabelStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(8f * scale));
        }

        private void OnGUI()
        {
            // Real-player LAN matches keep the cheat menu closed unless the
            // host's room rule opted in; single-player and local two-player
            // (and every non-match screen) always keep it available.
            if (!MutinyNetworkManager.CheatsAllowedInMatch)
            {
                m_IsOpen = false;
                return;
            }

            float scale = CalculateScale(Screen.width, Screen.height);
            EnsureResources(scale);

            // Save GUI state
            Matrix4x4 prevMatrix = GUI.matrix;
            Color prevColor = GUI.color;

            // Render in screen-space, topmost depth
            GUI.depth = -20000;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;

            // 1. Draw circular floating GM button on the left vertical center, relative to canvas scale
            Rect buttonRect = ResolveButtonRect(Screen.width, Screen.height);
            float btnSize = buttonRect.width;
            float btnX = buttonRect.x;
            float btnY = buttonRect.y;

            bool isHovered = buttonRect.Contains(Event.current.mousePosition);
            GUI.DrawTexture(buttonRect, isHovered ? m_CircleHoverTex : m_CircleNormalTex);
            if (GUI.Button(buttonRect, "GM", m_CircleButtonStyle))
            {
                m_IsOpen = !m_IsOpen;
            }

            // 2. Draw command window if open
            if (m_IsOpen)
            {
                DrawCommandPanel(btnX + btnSize + 8f * scale, btnY + btnSize * 0.5f, scale);
            }

            // Restore GUI state
            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        private void DrawCommandPanel(float originX, float centerY, float scale)
        {
            float panelWidth = Mathf.Min(340f * scale, Screen.width - (originX + 10f * scale));
            float panelHeight = Mathf.Min(320f * scale, Screen.height - 20f * scale);
            float panelY = Mathf.Clamp(centerY - panelHeight * 0.5f, 10f * scale, Screen.height - panelHeight - 10f * scale);
            Rect panelRect = new Rect(originX, panelY, panelWidth, panelHeight);

            // Semi-transparent background
            GUI.DrawTexture(panelRect, m_PanelBackgroundTex);

            // Border
            DrawOutline(panelRect, new Color(0.40f, 0.45f, 0.60f, 0.85f), Mathf.Max(1f, 2f * scale));

            // Inner content layout
            float padding = 10f * scale;
            float contentWidth = panelWidth - padding * 2;

            // Header: Title, Lock [Logo], Close [X]
            float headerHeight = 20f * scale;
            float closeWidth = 22f * scale;
            float lockWidth = 22f * scale;
            float btnGap = 4f * scale;

            // Close button is at the far right; Lock button is placed to the left of Close button
            Rect closeRect = new Rect(panelRect.xMax - padding - closeWidth, panelRect.y + padding, closeWidth, headerHeight);
            Rect lockRect = new Rect(closeRect.x - btnGap - lockWidth, panelRect.y + padding, lockWidth, headerHeight);
            Rect headerRect = new Rect(panelRect.x + padding, panelRect.y + padding, contentWidth - closeWidth - lockWidth - btnGap - 6f * scale, headerHeight);

            GUI.Label(headerRect, "MUTINY GM MENU", m_PanelHeaderStyle);

            Texture2D lockTex = m_LockOpen ? m_LockLockedTex : m_LockUnlockedTex;
            GUIStyle lockStyle = m_LockOpen ? m_LockActiveButtonStyle : m_LockActionButtonStyle;
            string lockTooltip = m_LockOpen ? "Pinned: Menu stays open after each press" : "Unpinned: Auto-closes the menu after each press";
            if (GUI.Button(lockRect, new GUIContent(lockTex, lockTooltip), lockStyle))
            {
                m_LockOpen = !m_LockOpen;
            }

            if (GUI.Button(closeRect, "X", m_ActionButtonStyle))
            {
                m_IsOpen = false;
            }

            // Status / last result, with the toggle grid below it.
            float statusY = headerRect.yMax + 6f * scale;
            Rect statusRect = new Rect(panelRect.x + padding, statusY, contentWidth, 58f * scale);
            m_StatusLabelStyle.normal.textColor = m_StatusColor;
            GUI.Label(statusRect, m_StatusMessage, m_StatusLabelStyle);

            float gridY = statusRect.yMax + 6f * scale;
            float rowHeight = 24f * scale;
            float rowGap = 4f * scale;
            float columnWidth = (contentWidth - btnGap) * 0.5f;

            Rect LeftSlot(int row) => new Rect(panelRect.x + padding,
                gridY + row * (rowHeight + rowGap), columnWidth, rowHeight);
            Rect RightSlot(int row) => new Rect(panelRect.x + padding + columnWidth + btnGap,
                gridY + row * (rowHeight + rowGap), columnWidth, rowHeight);

            // Left column: language and progress.
            bool chinese = MutinyLocalization.Code == MutinyLocalization.SimplifiedChinese;
            DrawMenuButton(LeftSlot(0), chinese ? "Language: Chinese" : "Language: English", false,
                chinese ? "setlanguage en" : "setlanguage zh-cn");
            DrawMenuButton(LeftSlot(1), "Unlock All Levels", false, "unlockalllevels");
            DrawMenuButton(LeftSlot(2), "Reset Progress", false, "resetlevels");
            DrawMenuButton(LeftSlot(3), "Unlock Weapons: Character", false, "unlockweapons");
            DrawMenuButton(LeftSlot(4), "Unlock Weapons: Team 1", false, "unlockweapons allteam");
            DrawMenuButton(LeftSlot(5), "Help", false, "help");

            // Right column: AI and camera tools.
            int weaponId = MutinyAIController.ForcedWeaponId;
            DrawMenuButton(RightSlot(0),
                weaponId == 0 ? "AI Weapon: Off" : $"AI Weapon {weaponId}: {MutinyAIController.ForcedWeaponType}",
                weaponId != 0,
                $"aiweapon {(weaponId + 1) % (MutinyAIController.ForceableWeaponCount + 1)}");
            DrawCycleAiLuckButton(RightSlot(1));
            DrawMenuButton(RightSlot(2), "AI Luck: Reset", false, "airesetluck");
            string luckText = AiLuckPresets[m_AiLuckPresetIndex].ToString("0.###", CultureInfo.InvariantCulture);
            DrawMenuButton(RightSlot(3), $"AI Takeover: Luck {luckText}", false, $"aitakeover {luckText}");
            DrawMenuButton(RightSlot(4),
                MutinyAIController.ActionLogEnabled ? "AI Log: On" : "AI Log: Off",
                MutinyAIController.ActionLogEnabled,
                MutinyAIController.ActionLogEnabled ? "ailog 0" : "ailog 1");
            DrawMenuButton(RightSlot(5),
                MutinyCameraController.ExplosionCameraEnabled ? "Blast Cam: On" : "Blast Cam: Off",
                MutinyCameraController.ExplosionCameraEnabled,
                MutinyCameraController.ExplosionCameraEnabled ? "excamera" : "excamera 1");

            // Room rule hint
            Rect hintRect = new Rect(panelRect.x + padding, gridY + 6 * rowHeight + 5 * rowGap + 4f * scale,
                contentWidth, 14f * scale);
            GUI.Label(hintRect, "LAN matches show this menu only when the host enables cheats in the room.",
                m_HintLabelStyle);
        }

        /// <summary>One toggle/action button; active entries use the green "on" style.</summary>
        private void DrawMenuButton(Rect rect, string label, bool active, string command)
        {
            GUIStyle style = active ? m_ActionButtonActiveStyle : m_ActionButtonStyle;
            if (GUI.Button(rect, label, style))
                RunMenuCommand(command);
        }

        /// <summary>Cycles the shared AI-luck preset and applies it to the level AI.</summary>
        private void DrawCycleAiLuckButton(Rect rect)
        {
            float current = AiLuckPresets[m_AiLuckPresetIndex];
            if (GUI.Button(rect, "AI Luck: " + current.ToString("0.###", CultureInfo.InvariantCulture),
                    m_ActionButtonStyle))
            {
                m_AiLuckPresetIndex = (m_AiLuckPresetIndex + 1) % AiLuckPresets.Length;
                RunMenuCommand("aisetluck " +
                    AiLuckPresets[m_AiLuckPresetIndex].ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        private void RunMenuCommand(string command)
        {
            ExecuteCommand(command);
            if (!m_LockOpen)
            {
                m_IsOpen = false;
            }
        }

        public bool RunRecentCommand(int index)
        {
            if (index < 0 || index >= m_RecentSuccessfulCommands.Count)
                return false;
            return ExecuteCommand(m_RecentSuccessfulCommands[index]);
        }

        public bool ExecuteCommand(string rawCommand)
        {
            string cmd = rawCommand.Trim();
            string lower = cmd.ToLowerInvariant();
            bool succeeded = false;

            Debug.Log($"[MutinyGM] Executing command: '{cmd}'", this);

            if (lower.StartsWith("setlanguage", StringComparison.Ordinal))
            {
                string[] parts = cmd.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                string code = parts.Length == 2 && string.Equals(parts[0], "setlanguage", StringComparison.OrdinalIgnoreCase)
                    ? string.Equals(parts[1], "zh-cn", StringComparison.OrdinalIgnoreCase) ? MutinyLocalization.SimplifiedChinese
                    : string.Equals(parts[1], "en", StringComparison.OrdinalIgnoreCase) ? MutinyLocalization.English : null
                    : null;
                if (code == null)
                {
                    m_StatusColor = new Color(1.0f, 0.45f, 0.45f);
                    m_StatusMessage = "[ERROR] Usage: setlanguage zh-cn | en";
                }
                else
                {
                    MutinyLocalization.Initialize(this);
                    MutinyLocalization.Select(code);
                    m_StatusColor = new Color(0.35f, 1.0f, 0.45f);
                    m_StatusMessage = "[SUCCESS] Language set to " +
                        (code == MutinyLocalization.SimplifiedChinese ? "Simplified Chinese (zh-cn)." : "English (en).") +
                        (MutinyLocalization.IsReady ? string.Empty : "\nBundled translations unavailable; using English fallback.");
                    succeeded = true;
                }
            }
            else if (lower == "unlockalllevels" || lower == "unlockall" || lower == "unlock all")
            {
                MutinySaveSystem.HighestUnlockedLevel = MutinySaveSystem.MaxLevel;
                m_StatusColor = new Color(0.35f, 1.0f, 0.45f);
                m_StatusMessage = $"[SUCCESS] All levels unlocked! (1..{MutinySaveSystem.MaxLevel})\nHighestUnlockedLevel is now {MutinySaveSystem.HighestUnlockedLevel}.";
                succeeded = true;
            }
            else if (lower == "resetlevels" || lower == "resetprogress" || lower == "lockall")
            {
                MutinySaveSystem.ResetProgress();
                m_StatusColor = new Color(1.0f, 0.85f, 0.35f);
                m_StatusMessage = $"[RESET] Level progress reset to default.\nHighestUnlockedLevel is now {MutinySaveSystem.HighestUnlockedLevel}.";
                succeeded = true;
            }
            else if (lower.StartsWith("aitakeover", StringComparison.Ordinal) || lower.StartsWith("aitakeoverwithluck", StringComparison.Ordinal))
            {
                string[] parts = cmd.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                string error = "Usage: aitakeover {luck} (0..99999, current human turn only).";
                float luck = 0f;
                MutinyTurnManager manager = FindAnyObjectByType<MutinyTurnManager>();
                if (parts.Length == 2 &&
                    (string.Equals(parts[0], "aitakeover", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(parts[0], "aitakeoverwithluck", StringComparison.OrdinalIgnoreCase)) &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out luck) &&
                    manager != null)
                    succeeded = manager.TryTakeOverCurrentPlayerTurn(luck, out error);
                m_StatusColor = succeeded ? new Color(0.35f, 1.0f, 0.45f) : new Color(1.0f, 0.45f, 0.45f);
                m_StatusMessage = succeeded
                    ? "[SUCCESS] AI controls the rest of this turn with Luck " +
                      luck.ToString("0.###", CultureInfo.InvariantCulture) + ". Player control returns next turn."
                    : $"[ERROR] {error}";
            }
            else if (lower.StartsWith("aisetluck", StringComparison.Ordinal) ||
                     lower.StartsWith("airesetluck", StringComparison.Ordinal))
            {
                string[] parts = cmd.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                bool reset = parts.Length == 1 && string.Equals(parts[0], "airesetluck", StringComparison.OrdinalIgnoreCase);
                float luck = 0f;
                bool set = parts.Length == 2 && string.Equals(parts[0], "aisetluck", StringComparison.OrdinalIgnoreCase) &&
                           float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out luck);
                string error = "Usage: aisetluck {luck} (0..99999) | airesetluck (single-player level only).";
                MutinyLevelController controller = FindAnyObjectByType<MutinyLevelController>();
                if ((reset || set) && controller != null)
                    succeeded = controller.TrySetCurrentAiLuck(reset ? (float?)null : luck, out error);
                m_StatusColor = succeeded ? new Color(0.35f, 1.0f, 0.45f) : new Color(1.0f, 0.45f, 0.45f);
                m_StatusMessage = succeeded
                    ? reset ? "[SUCCESS] Current level AI Luck restored to each character's default."
                            : "[SUCCESS] Current level AI Luck set to " + luck.ToString("0.###", CultureInfo.InvariantCulture) + "."
                    : $"[ERROR] {error}";
            }
            else if (lower.StartsWith("excamera", StringComparison.Ordinal))
            {
                string[] parts = cmd.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 1 || parts.Length > 2 ||
                    !string.Equals(parts[0], "excamera", StringComparison.OrdinalIgnoreCase) ||
                    (parts.Length == 2 && parts[1] != "0" && parts[1] != "1"))
                {
                    m_StatusColor = new Color(1.0f, 0.45f, 0.45f);
                    m_StatusMessage = "[ERROR] Usage: excamera 1 (on) | excamera (off).";
                }
                else
                {
                    bool enabled = parts.Length == 2 && parts[1] == "1";
                    MutinyCameraController.SetExplosionCameraEnabled(enabled);
                    m_StatusColor = new Color(0.35f, 1.0f, 0.45f);
                    m_StatusMessage = enabled
                        ? "[SUCCESS] Explosion knockback camera enabled."
                        : "[SUCCESS] Explosion knockback camera disabled.";
                    succeeded = true;
                }
            }
            else if (lower.StartsWith("ailog", StringComparison.Ordinal))
            {
                string[] parts = cmd.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 ||
                    !string.Equals(parts[0], "ailog", StringComparison.OrdinalIgnoreCase) ||
                    (parts[1] != "0" && parts[1] != "1"))
                {
                    m_StatusColor = new Color(1.0f, 0.45f, 0.45f);
                    m_StatusMessage = "[ERROR] Usage: ailog 1 | 0.";
                }
                else
                {
                    MutinyAIController.SetActionLogEnabled(parts[1] == "1");
                    m_StatusColor = new Color(0.35f, 1.0f, 0.45f);
                    m_StatusMessage = parts[1] == "1"
                        ? "[SUCCESS] AI action decision log enabled. Read [Mutiny:AI-Action] in Player.log."
                        : "[SUCCESS] AI action decision log disabled.";
                    succeeded = true;
                }
            }
            else if (lower.StartsWith("aiweapon", StringComparison.Ordinal) ||
                     lower.StartsWith("aiforceusewaepon", StringComparison.Ordinal) ||
                     lower.StartsWith("aiforceuseweapon", StringComparison.Ordinal))
            {
                string[] parts = cmd.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 ||
                    (!string.Equals(parts[0], "aiweapon", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(parts[0], "aiforceusewaepon", StringComparison.OrdinalIgnoreCase) &&
                     !string.Equals(parts[0], "aiforceuseweapon", StringComparison.OrdinalIgnoreCase)) ||
                    !int.TryParse(parts[1], out int weaponId) ||
                    !MutinyAIController.TrySetForcedWeaponId(weaponId))
                {
                    m_StatusColor = new Color(1.0f, 0.45f, 0.45f);
                    m_StatusMessage = "[ERROR] Usage: aiweapon {weaponid} (0..15).";
                }
                else
                {
                    m_StatusColor = new Color(0.35f, 1.0f, 0.45f);
                    m_StatusMessage = weaponId == 0
                        ? "[SUCCESS] AI weapon override disabled; actual inventory restored."
                        : $"[SUCCESS] All AI teams now consider only infinite weapon {weaponId} ({MutinyAIController.ForcedWeaponType}). Jump and pass remain available.";
                    succeeded = true;
                }
            }
            else if (lower == "unlockweapons" || lower == "unlockallweapons" || lower == "infiniteweapons" ||
                     lower == "allweapons" || lower == "weapons" || lower == "解锁武器" || lower == "无限武器" ||
                     lower.StartsWith("unlockweapon") || lower.StartsWith("infiniteweapon"))
            {
                bool targetAll = lower.Contains("all") && (lower.Contains("team") || lower.Contains("player"));
                if (targetAll)
                {
                    MutinyCharacter[] allChars = FindObjectsByType<MutinyCharacter>(FindObjectsSortMode.None);
                    int unlockedCount = 0;
                    for (int i = 0; i < allChars.Length; i++)
                    {
                        MutinyCharacter c = allChars[i];
                        if (c.IsAlive && c.TeamIndex == 1)
                        {
                            c.UnlockAllWeapons(infinite: true);
                            unlockedCount++;
                        }
                    }
                    m_StatusColor = new Color(0.35f, 1.0f, 0.45f);
                    m_StatusMessage = $"[SUCCESS] Unlocked all 15 weapons (infinite ammo) for {unlockedCount} characters on Team 1!";
                    succeeded = true;
                }
                else
                {
                    MutinyCharacter target = FindTargetCharacter(out string detail);
                    if (target != null)
                    {
                        target.UnlockAllWeapons(infinite: true);

                        MutinyTurnManager turnManager = FindAnyObjectByType<MutinyTurnManager>();
                        if (turnManager != null && turnManager.CurrentTeam != null && turnManager.CurrentTeam.Characters.Contains(target))
                        {
                            if (turnManager.CurrentTeam.SelectedCharacter != target)
                                turnManager.CurrentTeam.SelectCharacter(target);
                        }

                        m_StatusColor = new Color(0.35f, 1.0f, 0.45f);
                        m_StatusMessage = $"[SUCCESS] All 15 weapons unlocked (infinite ammo) for {detail}!";
                        succeeded = true;
                    }
                    else
                    {
                        m_StatusColor = new Color(1.0f, 0.45f, 0.45f);
                        m_StatusMessage = "[ERROR] No alive character found in the scene.\nPlease enter a game level first.";
                    }
                }
            }
            else if (lower == "help" || lower == "?")
            {
                m_StatusColor = new Color(0.5f, 0.85f, 1.0f);
                m_StatusMessage = "GM menu buttons:\n" +
                                  "• Language - toggle English / Simplified Chinese\n" +
                                  "• Unlock All Levels / Reset Progress - save-file shortcuts\n" +
                                  "• Unlock Weapons - infinite ammo for the current character or team 1\n" +
                                  "• AI Weapon - cycle the forced AI weapon (Off after 15)\n" +
                                  "• AI Luck - cycle the preset; Reset restores each character's default\n" +
                                  "• AI Takeover - AI plays the rest of the current human turn\n" +
                                  "• AI Log / Blast Cam - on/off toggles\n" +
                                  "• LAN matches show this menu only when the host enables cheats in the room";
                succeeded = true;
            }
            else
            {
                m_StatusColor = new Color(1.0f, 0.45f, 0.45f);
                m_StatusMessage = $"[ERROR] Unknown command: '{cmd}'\nType 'help' to view available commands.";
            }

            if (succeeded)
            {
                m_RecentSuccessfulCommands.Insert(0, cmd);
                if (m_RecentSuccessfulCommands.Count > MaxRecentSuccessfulCommands)
                    m_RecentSuccessfulCommands.RemoveAt(MaxRecentSuccessfulCommands);
            }
            return succeeded;
        }

        private MutinyCharacter FindTargetCharacter(out string detail)
        {
            MutinyTurnManager turnManager = FindAnyObjectByType<MutinyTurnManager>();
            if (turnManager != null)
            {
                if (turnManager.CurrentTeam != null && turnManager.CurrentTeam.SelectedCharacter != null && turnManager.CurrentTeam.SelectedCharacter.IsAlive)
                {
                    detail = $"active selected character '{turnManager.CurrentTeam.SelectedCharacter.name}' (Team {turnManager.CurrentTeam.TeamNumber})";
                    return turnManager.CurrentTeam.SelectedCharacter;
                }

                if (turnManager.Team1 != null && turnManager.Team1.SelectedCharacter != null && turnManager.Team1.SelectedCharacter.IsAlive)
                {
                    detail = $"Team 1 selected character '{turnManager.Team1.SelectedCharacter.name}'";
                    return turnManager.Team1.SelectedCharacter;
                }

                if (turnManager.CurrentTeam != null)
                {
                    MutinyCharacter firstAlive = turnManager.CurrentTeam.Characters.Find(c => c != null && c.IsAlive);
                    if (firstAlive != null)
                    {
                        detail = $"current team's character '{firstAlive.name}' (Team {turnManager.CurrentTeam.TeamNumber})";
                        return firstAlive;
                    }
                }

                if (turnManager.Team1 != null)
                {
                    MutinyCharacter firstAlive = turnManager.Team1.Characters.Find(c => c != null && c.IsAlive);
                    if (firstAlive != null)
                    {
                        detail = $"player character '{firstAlive.name}' (Team 1)";
                        return firstAlive;
                    }
                }
            }

            MutinyCharacter[] allChars = FindObjectsByType<MutinyCharacter>(FindObjectsSortMode.None);
            for (int i = 0; i < allChars.Length; i++)
            {
                MutinyCharacter c = allChars[i];
                if (c.IsSelected && c.IsAlive)
                {
                    detail = $"selected character '{c.name}' (Team {c.TeamIndex})";
                    return c;
                }
            }

            for (int i = 0; i < allChars.Length; i++)
            {
                MutinyCharacter c = allChars[i];
                if (c.TeamIndex == 1 && c.IsAlive)
                {
                    detail = $"player character '{c.name}' (Team 1)";
                    return c;
                }
            }

            for (int i = 0; i < allChars.Length; i++)
            {
                MutinyCharacter c = allChars[i];
                if (c.IsAlive)
                {
                    detail = $"character '{c.name}' (Team {c.TeamIndex})";
                    return c;
                }
            }

            detail = null;
            return null;
        }

        private static Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCircleTexture(int size, Color fillColor, Color borderColor, float borderThickness = 6f)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            float radius = center - 2.5f;
            float radiusSqr = radius * radius;
            float innerRadius = Mathf.Max(0f, radius - borderThickness);
            float innerRadiusSqr = innerRadius * innerRadius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float distSqr = dx * dx + dy * dy;

                    if (distSqr > radiusSqr + 2f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (distSqr > radiusSqr)
                    {
                        // Anti-aliased outer edge
                        float t = Mathf.InverseLerp(radiusSqr + 2f, radiusSqr, distSqr);
                        tex.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, borderColor.a * t));
                    }
                    else if (distSqr > innerRadiusSqr)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, fillColor);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D CreateLockTexture(int size, bool isLocked, Color lockColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float scale = size / 64f;

            float shackleOuterR = 12f * scale;
            float shackleInnerR = 7.5f * scale;
            float shackleThick = (shackleOuterR - shackleInnerR) * 0.5f;
            float shackleMidR = (shackleOuterR + shackleInnerR) * 0.5f;

            float archCenterY = isLocked ? 41f * scale : 47f * scale;
            float archCenterX = 32f * scale;
            float bodyTopY = 29f * scale;
            float bodyBottomY = 11f * scale;
            float bodyCenterY = (bodyTopY + bodyBottomY) * 0.5f;
            float bodyHalfW = 16f * scale;
            float bodyHalfH = (bodyTopY - bodyBottomY) * 0.5f;
            float bodyCornerR = 4f * scale;

            Color keyholeColor = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    // 1. Check Body
                    float bdx = Mathf.Max(0f, Mathf.Abs(px - archCenterX) - (bodyHalfW - bodyCornerR));
                    float bdy = Mathf.Max(0f, Mathf.Abs(py - bodyCenterY) - (bodyHalfH - bodyCornerR));
                    float bDist = Mathf.Sqrt(bdx * bdx + bdy * bdy);
                    float bodyAlpha = Mathf.Clamp01(bodyCornerR + 0.5f - bDist);

                    // 2. Check Shackle
                    float shackleAlpha = 0f;
                    if (py >= archCenterY)
                    {
                        // Arch top
                        float sdx = px - archCenterX;
                        float sdy = py - archCenterY;
                        float sDist = Mathf.Sqrt(sdx * sdx + sdy * sdy);
                        float distFromMid = Mathf.Abs(sDist - shackleMidR);
                        shackleAlpha = Mathf.Clamp01(shackleThick + 0.5f - distFromMid);
                    }
                    else
                    {
                        // Legs
                        float leftMidX = archCenterX - shackleMidR;
                        float rightMidX = archCenterX + shackleMidR;
                        float leftDist = Mathf.Abs(px - leftMidX);
                        float rightDist = Mathf.Abs(px - rightMidX);

                        float minYLeft = bodyTopY - 2f * scale;
                        float maxYLeft = archCenterY;
                        if (py >= minYLeft && py <= maxYLeft)
                        {
                            shackleAlpha = Mathf.Max(shackleAlpha, Mathf.Clamp01(shackleThick + 0.5f - leftDist));
                        }

                        float minYRight = isLocked ? (bodyTopY - 2f * scale) : (archCenterY - 6f * scale);
                        float maxYRight = archCenterY;
                        if (py >= minYRight && py <= maxYRight)
                        {
                            shackleAlpha = Mathf.Max(shackleAlpha, Mathf.Clamp01(shackleThick + 0.5f - rightDist));
                        }
                    }

                    // Combine Shackle and Body
                    float combinedAlpha = Mathf.Clamp01(Mathf.Max(bodyAlpha, shackleAlpha));
                    if (combinedAlpha <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color pixelColor = new Color(lockColor.r, lockColor.g, lockColor.b, lockColor.a * combinedAlpha);

                    // 3. Keyhole cutout inside body
                    if (bodyAlpha > 0.5f)
                    {
                        float kCircleX = archCenterX;
                        float kCircleY = bodyCenterY + 2f * scale;
                        float kDist = Mathf.Sqrt((px - kCircleX) * (px - kCircleX) + (py - kCircleY) * (py - kCircleY));
                        bool inKeyholeCircle = kDist <= 2.8f * scale;
                        bool inKeyholeSlot = Mathf.Abs(px - archCenterX) <= 1.4f * scale && py >= (bodyCenterY - 4.5f * scale) && py <= kCircleY;

                        if (inKeyholeCircle || inKeyholeSlot)
                        {
                            pixelColor = Color.Lerp(pixelColor, keyholeColor, 0.9f);
                        }
                    }

                    tex.SetPixel(x, y, pixelColor);
                }
            }
            tex.Apply();
            return tex;
        }

        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            Texture2D tex = Texture2D.whiteTexture;
            Color prev = GUI.color;
            GUI.color = color;
            // Top, Bottom, Left, Right
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), tex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), tex);
            GUI.color = prev;
        }
    }
}
