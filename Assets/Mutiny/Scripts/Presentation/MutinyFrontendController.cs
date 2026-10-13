using System;
using System.Collections.Generic;
using Mutiny.Levels;
using Mutiny.Network;
using Mutiny.Persistence;
using Mutiny.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mutiny.Presentation
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class MutinyFrontendController : MonoBehaviour
    {
        public const int SinglePlayerLevelCount = 15;
        private const float CanvasWidth = 550f;
        private const float CanvasHeight = 400f;

        private readonly MutinyFrontendFlow m_Flow = new MutinyFrontendFlow();
        private readonly Texture2D[] m_LevelPreviews = new Texture2D[SinglePlayerLevelCount];
        private readonly Texture2D[] m_TwoPlayerPreviews = new Texture2D[18];
        private static readonly string[] TwoPlayerLevelNames =
        {
            "the docks", "island hopping", "about turn", "battleships", "boom boom beach", "king of the hill",
            "boulder dash", "marooned", "tower of terror", "the galleon", "mountain madness", "one on one",
            "flatlands", "skull island", "stepping stones", "death bowl", "mega beach", "blast caverns"
        };
        private Texture2D m_TwoPlayerPrevUp;
        private Texture2D m_TwoPlayerPrevOver;
        private Texture2D m_TwoPlayerNextUp;
        private Texture2D m_TwoPlayerNextOver;
        private string m_TwoPlayerLoadError;
        private const int HelpTutorialFrameCount = 160;
        private readonly Texture2D[] m_HelpTutorialFrames = new Texture2D[HelpTutorialFrameCount];
        private float m_HelpOpenTime;
        // Flash menu_background_anim: bg children in display-depth order, then water.
        private readonly Texture2D[] m_BackgroundLayers = new Texture2D[7];
        private readonly float[] m_BackgroundOffsets = new float[5];
        private float m_BackgroundTickAccumulator;
        private bool m_HasAnimatedBackground;
        private Texture2D m_EndingShip;
        private readonly Texture2D[][] m_EndingShipParts =
            new Texture2D[MutinyEndingSequence.ShipSymbols.Length][];
        private bool m_HasAnimatedEndingShip;
        private int m_EndingShipTick;
        private Texture2D m_EndingBubble;
        private Texture2D m_EndingKrakenBubble;
        private Texture2D m_EndingScorePanel;
        private Texture2D m_EndingBackButton;
        private Texture2D m_EndingBackButtonOver;
        private int m_EndingFrame = 1;
        private int m_EndingScore;

        private MutinyLevelController m_LevelController;
        private Texture2D m_Background;
        private Texture2D m_TitleLogo;
        private Texture2D m_GameSelectPanel;
        private Texture2D m_HelpPanel;
        private Texture2D m_CreditsPanel;
        private Texture2D m_CreditsNitromeLogo;
        private Texture2D m_CreditsNitromeHit;
        private Texture2D m_CreditsAvatar;
        private Texture2D m_CreditsCopyright;
        private Texture2D m_CreditsCopyrightHit;
        private Texture2D m_ScoresRows;
        private bool m_CreditsLogoPressed;
        private bool m_CreditsCopyrightPressed;
        private bool m_CreditsGithubPressed;
        private bool m_CreditsNitromeTextPressed;
        private bool m_CreditsAvatarPressed;
        private static readonly Color LinkHoverColor = new Color(1f, 0.88f, 0.25f);
        private Texture2D m_LevelSelectPanel;
        private Texture2D m_TwoPlayerPanel;
        private Texture2D m_TwoPlayerScorePirates;
        private Texture2D m_TwoPlayerPreviewFrame;
        private Texture2D m_GameTypePirates;
        private Texture2D m_ButtonSmall;
        private Texture2D m_ButtonSmallOver;
        private Texture2D m_ButtonWide;
        private Texture2D m_ButtonWideOver;
        private Texture2D m_ButtonBack;
        private Texture2D m_ButtonBackOver;
        private Texture2D m_LevelSlot;
        private Texture2D m_LevelSlotOver;
        private Texture2D m_MusicCornerOnUpTexture;
        private Texture2D m_MusicCornerOnOverTexture;
        private Texture2D m_MusicCornerOffUpTexture;
        private Texture2D m_MusicCornerOffOverTexture;
        private Texture2D m_SfxCornerOnUpTexture;
        private Texture2D m_SfxCornerOnOverTexture;
        private Texture2D m_SfxCornerOffUpTexture;
        private Texture2D m_SfxCornerOffOverTexture;
        private bool m_MusicHovered;
        private bool m_SfxHovered;
        private GUIStyle m_ButtonStyle;
        private GUIStyle m_HeadingStyle;
        private GUIStyle m_LevelNumberStyle;
        private GUIStyle m_QuestionStyle;

        // LAN lobby state. The host reuses the versus level picker; the client
        // stays in the lobby until MatchStart arrives.
        private bool m_NetworkHooked;
        private bool m_OnlineHostPickingLevel;
        private string m_LanAddressInput = "127.0.0.1";
        private string m_LanStatusText = string.Empty;
        private GUIStyle m_LanFieldStyle;

        public MutinyFrontendPage CurrentPage => m_Flow.CurrentPage;
        public Texture2D CreditsAvatar => m_CreditsAvatar;
        public int CurrentEndingFrame => m_EndingFrame;
        public int CurrentEndingShipTick => m_EndingShipTick;
        public bool HasAnimatedEndingShip => m_HasAnimatedEndingShip;
        public int CurrentEndingScore => m_EndingScore;
        public bool AreCornerAudioControlsVisible => m_Flow.CurrentPage != MutinyFrontendPage.Gameplay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapAfterSceneLoad()
        {
            if (SceneManager.GetActiveScene().name != "Main" ||
                FindAnyObjectByType<MutinyFrontendController>() != null)
            {
                return;
            }

            MutinyLevelController levelController = FindAnyObjectByType<MutinyLevelController>();
            if (levelController == null)
                return;

            GameObject host = new GameObject("MutinyFrontend");
            MutinyFrontendController frontend = host.AddComponent<MutinyFrontendController>();
            frontend.Initialize(levelController);
        }

        public void Initialize(MutinyLevelController levelController)
        {
            m_LevelController = levelController;
            MutinyLocalization.Initialize(this);
            LoadResources();

            if (m_LevelController != null && m_LevelController.CurrentLevel != null)
                m_LevelController.CurrentLevel.gameObject.SetActive(false);

            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.GetComponent<MutinyCameraController>() == null)
            {
                mainCam.gameObject.AddComponent<MutinyCameraController>();
            }

            MutinyAudioManager.Instance?.PlayMusic("menu_music");
            Debug.Log($"[MutinyFrontend] FRONT-01 title shown; highestUnlocked={MutinySaveSystem.HighestUnlockedLevel}", this);
        }

        private void LoadResources()
        {
            m_Background = Resources.Load<Texture2D>("UI/Frontend/background");
            string[] layerNames = { "sky", "backClouds1", "hills1", "frontClouds1",
                "cloudBase1", "waterBackground1", "water" };
            m_HasAnimatedBackground = true;
            for (int i = 0; i < layerNames.Length; i++)
            {
                m_BackgroundLayers[i] = LoadPointTexture(MutinyOriginalBackground.ResourcePath + layerNames[i]);
                if (m_BackgroundLayers[i] == null)
                    m_HasAnimatedBackground = false;
            }
            m_TitleLogo = Resources.Load<Texture2D>("UI/Frontend/title_logo");
            m_GameSelectPanel = Resources.Load<Texture2D>("UI/Frontend/game_select_panel");
            m_HelpPanel = LoadPointTexture("UI/Frontend/help_panel");
            m_CreditsPanel = LoadPointTexture("UI/Frontend/credits_panel");
            m_CreditsNitromeLogo = LoadPointTexture("UI/Frontend/credits_nitrome_logo");
            m_CreditsNitromeHit = LoadPointTexture("UI/Frontend/credits_nitrome_hit");
            m_CreditsAvatar = LoadAvatarTexture();
            m_CreditsCopyright = LoadPointTexture("UI/Frontend/credits_copyright");
            m_CreditsCopyrightHit = LoadPointTexture("UI/Frontend/credits_copyright_hit");
            m_ScoresRows = LoadPointTexture("UI/Frontend/scores_rows");
            m_LevelSelectPanel = Resources.Load<Texture2D>("UI/Frontend/level_select_panel");
            m_TwoPlayerPanel = LoadPointTexture("UI/Frontend/two_player_panel");
            m_TwoPlayerScorePirates = LoadPointTexture("UI/Frontend/two_player_score_pirates");
            m_TwoPlayerPreviewFrame = LoadPointTexture("UI/Frontend/two_player_preview_frame");
            m_GameTypePirates = Resources.Load<Texture2D>("UI/Frontend/game_type_pirates");
            m_ButtonSmall = Resources.Load<Texture2D>("UI/Frontend/button_small");
            m_ButtonSmallOver = Resources.Load<Texture2D>("UI/Frontend/button_small_over");
            m_ButtonWide = Resources.Load<Texture2D>("UI/Frontend/button_wide");
            m_ButtonWideOver = Resources.Load<Texture2D>("UI/Frontend/button_wide_over");
            m_ButtonBack = Resources.Load<Texture2D>("UI/Frontend/button_back");
            m_ButtonBackOver = Resources.Load<Texture2D>("UI/Frontend/button_back_over");
            m_LevelSlot = Resources.Load<Texture2D>("UI/Frontend/level_slot");
            m_LevelSlotOver = Resources.Load<Texture2D>("UI/Frontend/level_slot_over");
            m_EndingShip = LoadPointTexture("UI/Ending/ship");
            m_HasAnimatedEndingShip = true;
            for (int symbolIndex = 0; symbolIndex < MutinyEndingSequence.ShipSymbols.Length; symbolIndex++)
            {
                MutinyEndingSequence.ShipSymbol symbol = MutinyEndingSequence.ShipSymbols[symbolIndex];
                Texture2D[] frames = new Texture2D[symbol.FrameCount];
                for (int frame = 1; frame <= frames.Length; frame++)
                {
                    frames[frame - 1] = LoadPointTexture($"UI/Ending/ShipParts/{symbol.Id}_{frame:D2}");
                    if (frames[frame - 1] == null)
                        m_HasAnimatedEndingShip = false;
                }
                m_EndingShipParts[symbolIndex] = frames;
            }
            m_EndingBubble = LoadPointTexture("UI/Ending/bubble");
            m_EndingKrakenBubble = LoadPointTexture("UI/Ending/bubble_kraken");
            m_EndingScorePanel = LoadPointTexture("UI/Ending/score_panel");
            m_EndingBackButton = LoadPointTexture("UI/Ending/back_button");
            m_EndingBackButtonOver = CreateOriginalHoveredTexture(m_EndingBackButton);
            for (int i = 0; i < SinglePlayerLevelCount; i++)
                m_LevelPreviews[i] = Resources.Load<Texture2D>($"UI/Frontend/LevelPreviews/{i + 1:D2}");
            for (int i = 0; i < m_TwoPlayerPreviews.Length; i++)
                m_TwoPlayerPreviews[i] = LoadPointTexture($"UI/Frontend/TwoPlayerPreviews/{i + 1:D2}");
            m_TwoPlayerPrevUp = LoadPointTexture("UI/Frontend/TwoPlayerPreviews/prev_up");
            m_TwoPlayerPrevOver = LoadPointTexture("UI/Frontend/TwoPlayerPreviews/prev_over");
            m_TwoPlayerNextUp = LoadPointTexture("UI/Frontend/TwoPlayerPreviews/next_up");
            m_TwoPlayerNextOver = LoadPointTexture("UI/Frontend/TwoPlayerPreviews/next_over");
            for (int i = 0; i < HelpTutorialFrameCount; i++)
                m_HelpTutorialFrames[i] = LoadPointTexture($"UI/Help/tutorial_{i + 1:D3}");

            m_MusicCornerOnUpTexture = LoadPointTexture("UI/CornerControls/music_on_up");
            m_MusicCornerOnOverTexture = LoadPointTexture("UI/CornerControls/music_on_over");
            m_MusicCornerOffUpTexture = LoadPointTexture("UI/CornerControls/music_off_up");
            m_MusicCornerOffOverTexture = LoadPointTexture("UI/CornerControls/music_off_over");
            m_SfxCornerOnUpTexture = LoadPointTexture("UI/CornerControls/sfx_on_up");
            m_SfxCornerOnOverTexture = LoadPointTexture("UI/CornerControls/sfx_on_over");
            m_SfxCornerOffUpTexture = LoadPointTexture("UI/CornerControls/sfx_off_up");
            m_SfxCornerOffOverTexture = LoadPointTexture("UI/CornerControls/sfx_off_over");
        }

        private static Texture2D LoadPointTexture(string path)
        {
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture != null)
                texture.filterMode = FilterMode.Point;
            return texture;
        }

        private static Texture2D LoadAvatarTexture()
        {
            // Use the same bundled asset in the Editor and every Player platform.
            return Resources.Load<Texture2D>("UI/Frontend/XingTong");
        }

        private static void DrawLinkUnderline(Rect textRect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(textRect.x, textRect.y + 12f, textRect.width, 1f), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static Texture2D CreateOriginalHoveredTexture(Texture2D original)
        {
            if (original == null || !original.isReadable)
                return null;
            Color32[] pixels = original.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                pixels[i] = new Color32((byte)(pixel.r / 2 + 128),
                    (byte)(pixel.g / 2 + 128), (byte)(pixel.b / 2 + 128), pixel.a);
            }
            Texture2D hovered = new Texture2D(original.width, original.height,
                TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            hovered.SetPixels32(pixels);
            hovered.Apply(false, true);
            return hovered;
        }

        private void Update()
        {
            HookNetworkIfPresent();
            if (m_Flow.CurrentPage == MutinyFrontendPage.Gameplay)
                return;

            m_BackgroundTickAccumulator += Time.unscaledDeltaTime;
            while (m_BackgroundTickAccumulator >= MutinyPhysics.TimeStep)
            {
                m_BackgroundTickAccumulator -= MutinyPhysics.TimeStep;
                // MenuBackgroundAnim.onEnterFrame: water, back clouds, hills,
                // front clouds and cloud base have independent Flash wrap widths.
                AdvanceBackgroundOffset(0, 8f, 64f);
                AdvanceBackgroundOffset(1, 1f, 900f);
                AdvanceBackgroundOffset(2, 2f, 840f);
                AdvanceBackgroundOffset(3, 4f, 1000f);
                AdvanceBackgroundOffset(4, 2f, 550f);
                if (m_Flow.CurrentPage == MutinyFrontendPage.Ending)
                    AdvanceEndingTick();
            }
        }

        // This is the production timeline clock used by Update's 25 Hz tick.
        public void AdvanceEndingTick()
        {
            if (m_Flow.CurrentPage != MutinyFrontendPage.Ending)
                return;
            m_EndingShipTick++;
            if (m_EndingFrame < MutinyEndingSequence.FrameCount)
                m_EndingFrame++;
        }

        private void AdvanceBackgroundOffset(int index, float speed, float width)
        {
            m_BackgroundOffsets[index] -= speed;
            if (m_BackgroundOffsets[index] <= -width)
                m_BackgroundOffsets[index] = 0f;
        }

        private void DrawAnimatedBackground()
        {
            if (!m_HasAnimatedBackground)
            {
                DrawTexture(new Rect(0f, 0f, CanvasWidth, CanvasHeight), m_Background);
                return;
            }

            GUI.BeginGroup(new Rect(0f, 0f, CanvasWidth, CanvasHeight));
            DrawBackgroundLayer(0, 0f, 0f);
            // Original sprite registration offsets, from sprite-origins.csv.
            DrawBackgroundLayer(1, m_BackgroundOffsets[1] + 197f, 12f);
            DrawBackgroundLayer(2, m_BackgroundOffsets[2], 53f);
            DrawBackgroundLayer(3, m_BackgroundOffsets[3] + 71f, 6f);
            DrawBackgroundLayer(4, m_BackgroundOffsets[4], 275.95f);
            DrawBackgroundLayer(5, 0f, 360f);
            if (m_Flow.CurrentPage != MutinyFrontendPage.Ending)
                DrawBackgroundLayer(6, m_BackgroundOffsets[0], 332f);
            GUI.EndGroup();
        }

        private void DrawBackgroundLayer(int index, float x, float y)
        {
            Texture2D texture = m_BackgroundLayers[index];
            DrawTexture(new Rect(x, y, texture.width, texture.height), texture);
        }

        private void OnGUI()
        {
            if (m_Flow.CurrentPage == MutinyFrontendPage.Gameplay)
                return;

            MutinyControllerUI.BeginScope("front:" + m_Flow.CurrentPage);
            GUI.depth = -10000;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            float scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
            float left = (Screen.width - CanvasWidth * scale) * 0.5f;
            float top = (Screen.height - CanvasHeight * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(left, top, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            try
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(-left / scale, -top / scale, Screen.width / scale, Screen.height / scale), Texture2D.whiteTexture);
                GUI.color = Color.white;
                DrawAnimatedBackground();

                switch (m_Flow.CurrentPage)
                {
                    case MutinyFrontendPage.Title:
                        DrawTitle();
                        break;
                    case MutinyFrontendPage.GameSelect:
                        DrawGameSelect();
                        break;
                    case MutinyFrontendPage.LanLobby:
                        DrawLanLobby();
                        break;
                    case MutinyFrontendPage.LevelSelect:
                        DrawLevelSelect();
                        break;
                    case MutinyFrontendPage.TwoPlayerLevelSelect:
                        DrawTwoPlayerLevelSelect();
                        break;
                    case MutinyFrontendPage.Help:
                        DrawHelp();
                        break;
                    case MutinyFrontendPage.Credits:
                        DrawCredits();
                        break;
                    case MutinyFrontendPage.Scores:
                        DrawScores();
                        break;
                    case MutinyFrontendPage.Ending:
                        DrawEnding();
                        break;
                }

                DrawCornerAudioControls();
            }
            finally
            {
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
            }
        }

        private void DrawTitle()
        {
            DrawTexture(new Rect(54f, 36f, 452f, 154f), m_TitleLogo);
            if (DrawOriginalButton(new Rect(193f, 187f, 163f, 24f), "frontend.play", "play", m_ButtonSmall, m_ButtonSmallOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_Flow.PressPlay();
                    LogPage("FRONT-01 play", m_Flow.CurrentPage);
                }, showLoading: false);
            }

            if (DrawOriginalButton(new Rect(193f, 216f, 163f, 24f), "frontend.scores", "scores", m_ButtonSmall, m_ButtonSmallOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_Flow.PressScores();
                    LogPage("FRONT-SCORES-01 scores", m_Flow.CurrentPage);
                }, showLoading: false);
            }
            if (DrawOriginalButton(new Rect(193f, 245f, 163f, 24f), "frontend.help", "help", m_ButtonSmall, m_ButtonSmallOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_HelpOpenTime = Time.unscaledTime;
                    m_Flow.PressHelp();
                    LogPage("FRONT-03 help", m_Flow.CurrentPage);
                }, showLoading: false);
            }
            if (DrawOriginalButton(new Rect(193f, 274f, 163f, 24f), "frontend.credits", "credits", m_ButtonSmall, m_ButtonSmallOver))
            {
                OpenCredits();
            }
        }

        private void OpenCredits()
        {
            MutinyTransitionManager.RequestTransition(() =>
            {
                m_Flow.PressCredits();
                LogPage("FRONT-CRED-01 credits", m_Flow.CurrentPage);
            }, showLoading: false);
        }

        private void DrawCredits()
        {
            // Root credits frame 51: shape 1930 is the frame only. Text and
            // clickable elements remain separate, as on the Flash timeline.
            DrawTexture(new Rect(44f, 24f, 462f, 352f), m_CreditsPanel);
            MutinyLocalizedText.Pirate(new Rect(44f, 32f, 462f, 23f), "frontend.credits", "credits");

            Vector2 mousePos = GetCanvasMousePosition();

            // Nitrome Logo (Left)
            Rect logoRect = new Rect(72f, 71f, 99f, 74f);
            bool logoHovered = !MutinyTransitionManager.IsTransitionActive &&
                CreditsLinkContains(logoRect, m_CreditsNitromeHit, mousePos);
            DrawTexture(logoRect, m_CreditsNitromeLogo);
            HandleCreditsLink(logoRect, m_CreditsNitromeHit, ref m_CreditsLogoPressed,
                "http://www.nitrome.com");

            // Porter Avatar & Name (Right, symmetric to Nitrome Logo)
            bool avatarHovered = false;
            if (m_CreditsAvatar != null)
            {
                Rect avatarRect = new Rect(408f, 71f, 58f, 58f);
                Rect avatarClickRect = new Rect(avatarRect.x - 1f, avatarRect.y - 1f, avatarRect.width + 2f, avatarRect.height + 15f);
                avatarHovered = !MutinyTransitionManager.IsTransitionActive && avatarClickRect.Contains(mousePos);

                DrawTexture(avatarRect, m_CreditsAvatar);

                float avatarCenterX = avatarRect.x + avatarRect.width * 0.5f;
                Color nameColor = avatarHovered ? LinkHoverColor : Color.white;
                MutinyBitmapFont.DrawDangleText(new Rect(avatarCenterX - 50f, 132f, 100f, 11f),
                    "Richard Xue", nameColor, TextAnchor.UpperCenter);

                HandleCreditsLink(avatarClickRect, null, ref m_CreditsAvatarPressed,
                    "https://github.com/RichardXue123/Mutiny-X");
            }

            // Line 1: game by nitrome
            DrawCreditsLine(71f, "game by nitrome", "frontend.credit_game_by");

            // Line 2: www.nitrome.com (link with underline)
            float nitromeWidth = MutinyBitmapFont.MeasureDangleText("www.nitrome.com");
            Rect nitromeTextRect = new Rect(44f + (462f - nitromeWidth) * 0.5f, 85f, nitromeWidth, 11f);
            Rect nitromeClickRect = new Rect(nitromeTextRect.x, nitromeTextRect.y, nitromeTextRect.width, 14f);
            bool nitromeHovered = !MutinyTransitionManager.IsTransitionActive && nitromeClickRect.Contains(mousePos);
            Color nitromeColor = nitromeHovered ? LinkHoverColor : Color.white;
            MutinyBitmapFont.DrawDangleText(nitromeTextRect, "www.nitrome.com", nitromeColor, TextAnchor.UpperCenter);
            DrawLinkUnderline(nitromeTextRect, nitromeColor);
            HandleCreditsLink(nitromeClickRect, null, ref m_CreditsNitromeTextPressed,
                "http://www.nitrome.com");

            // Line 3: Ported to Unity by Richard Xue
            DrawCreditsLine(99f, "Ported to Unity by Richard Xue", "frontend.credit_ported");

            // Line 4: github.com/RichardXue123/Mutiny-X (link with underline)
            float ghWidth = MutinyBitmapFont.MeasureDangleText("github.com/RichardXue123/Mutiny-X");
            Rect ghRect = new Rect(44f + (462f - ghWidth) * 0.5f, 113f, ghWidth, 11f);
            Rect ghClickRect = new Rect(ghRect.x, ghRect.y, ghRect.width, 14f);
            bool ghHovered = !MutinyTransitionManager.IsTransitionActive && ghClickRect.Contains(mousePos);
            Color ghColor = ghHovered ? LinkHoverColor : Color.white;
            MutinyBitmapFont.DrawDangleText(ghRect, "github.com/RichardXue123/Mutiny-X", ghColor, TextAnchor.UpperCenter);
            DrawLinkUnderline(ghRect, ghColor);
            HandleCreditsLink(ghClickRect, null, ref m_CreditsGithubPressed,
                "https://github.com/RichardXue123/Mutiny-X");

            MutinyLocalizedText.Pirate(new Rect(44f, 142f, 462f, 23f), "frontend.credit_programming", "programming");
            DrawCreditsLine(176.3f, "chris burt-brown");
            DrawCreditsLine(193.3f, "heather stancliffe");
            MutinyLocalizedText.Pirate(new Rect(44f, 218f, 462f, 23f), "frontend.credit_artwork", "artwork");
            DrawCreditsLine(248.3f, "jon annal");
            DrawCreditsLine(265.3f, "mat annal");
            MutinyLocalizedText.Pirate(new Rect(44f, 288f, 462f, 23f), "frontend.credit_music", "music");
            DrawCreditsLine(317.3f, "dave cowen");

            Rect backRect = new Rect(205f, 334f, 140f, 24f);
            bool backHovered = !MutinyTransitionManager.IsTransitionActive && backRect.Contains(mousePos);
            if (DrawOriginalButton(backRect, "frontend.back", "back", m_ButtonBack, m_ButtonBackOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_CreditsLogoPressed = false;
                    m_CreditsCopyrightPressed = false;
                    m_CreditsGithubPressed = false;
                    m_CreditsNitromeTextPressed = false;
                    m_CreditsAvatarPressed = false;
                    m_Flow.PressCreditsBack();
                    LogPage("FRONT-CRED-03 back", m_Flow.CurrentPage);
                }, showLoading: false);
            }

            // Button 1925 persists from the title frame at the bottom of the stage.
            Rect copyrightRect = new Rect(204.5f, 388f, 141f, 11f);
            bool copyrightHovered = !MutinyTransitionManager.IsTransitionActive &&
                CreditsLinkContains(copyrightRect, m_CreditsCopyrightHit, mousePos);
            DrawTexture(copyrightRect, m_CreditsCopyright);
            HandleCreditsLink(copyrightRect, m_CreditsCopyrightHit, ref m_CreditsCopyrightPressed,
                "http://www.nitrome.com/");

            // Interactive clickable pointer feedback
            if (logoHovered || nitromeHovered || ghHovered || avatarHovered || backHovered || copyrightHovered)
                MutinyCursorManager.NotifyHoverInteractable();
        }

        private void DrawScores()
        {
            // The original view_scores frame uses the 462x352 panel and
            // stage-level Back/copyright objects.
            DrawTexture(new Rect(44f, 24f, 462f, 352f), m_CreditsPanel);
            MutinyLocalizedText.Pirate(new Rect(44f, 32f, 462f, 23f), "frontend.scores", "scores");

            MutinySaveSystem.CompletedScoreEntry[] entries = MutinySaveSystem.GetTopCompletedScoreEntries();

            if (entries.Length == 0)
            {
                // Clean empty state: no row frames, stripes, or dummy items are drawn.
                // Centered pirate guidance message in the middle of the clean panel.
                MutinyLocalizedText.Dangle(new Rect(44f, 160f, 462f, 60f), "frontend.scores_empty",
                    "no completed records yet| |defeat all 15 campaign levels|to record your high score!",
                    new Color(1f, 0.88f, 0.25f), TextAnchor.MiddleCenter, 0, 14);
            }
            else
            {
                // Only render the row boxes and text for valid, existing records.
                for (int i = 0; i < entries.Length; i++)
                {
                    float rowY = 66f + i * 26f;

                    // Draw individual red-bordered box for this valid row from scores_rows texture
                    if (m_ScoresRows != null)
                    {
                        float vMin = 1f - (float)((i + 1) * 26) / 261f;
                        float vHeight = 26f / 261f;
                        GUI.DrawTextureWithTexCoords(new Rect(76.5f, 62f + i * 26f, 397f, 26f),
                            m_ScoresRows, new Rect(0f, vMin, 1f, vHeight), true);
                    }

                    bool isChampion = i == 0;
                    Color textColor = isChampion ? new Color(1f, 0.88f, 0.25f) : Color.white;
                    string timeText = string.IsNullOrEmpty(entries[i].Timestamp)
                        ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        : entries[i].Timestamp;

                    // Column 1: Rank (e.g. 01.)
                    MutinyBitmapFont.DrawDangleText(new Rect(95f, rowY, 35f, 14f),
                        (i + 1).ToString("D2") + ".", textColor, TextAnchor.MiddleLeft);

                    // Column 2: Timestamp (accurate to seconds)
                    MutinyBitmapFont.DrawDangleText(new Rect(145f, rowY, 185f, 14f),
                        timeText, textColor, TextAnchor.MiddleLeft);

                    // Column 3: Score with PTS unit
                    MutinyLocalizedText.Dangle(new Rect(335f, rowY, 120f, 14f), null,
                        MutinyLocalization.Text("frontend.points", "{0} pts", entries[i].Score),
                        textColor, TextAnchor.MiddleRight);
                }
            }

            if (DrawOriginalButton(new Rect(205f, 334f, 140f, 24f), "frontend.back", "back", m_ButtonBack, m_ButtonBackOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_CreditsCopyrightPressed = false;
                    m_Flow.PressScoresBack();
                    LogPage("FRONT-SCORES-03 back", m_Flow.CurrentPage);
                }, showLoading: false);
            }

            Rect copyrightRect = new Rect(204.5f, 388f, 141f, 11f);
            bool copyrightHovered = !MutinyTransitionManager.IsTransitionActive &&
                CreditsLinkContains(copyrightRect, m_CreditsCopyrightHit, GetCanvasMousePosition());
            if (copyrightHovered)
                MutinyCursorManager.NotifyHoverInteractable();
            DrawTexture(copyrightRect, m_CreditsCopyright);
            HandleCreditsLink(copyrightRect, m_CreditsCopyrightHit, ref m_CreditsCopyrightPressed,
                "http://www.nitrome.com/");
        }

        private static void DrawCreditsLine(float y, string text, string key = null, int lineSpacing = 13)
        {
            if (key == null)
                MutinyBitmapFont.DrawDangleText(new Rect(44f, y, 462f, 11f), text,
                    Color.white, TextAnchor.UpperCenter, 0, lineSpacing);
            else
                MutinyLocalizedText.Dangle(new Rect(44f, y, 462f, 15f), key, text,
                    Color.white, TextAnchor.UpperCenter, 0, lineSpacing);
        }

        private static void HandleCreditsLink(Rect rect, Texture2D mask, ref bool pressed, string url)
        {
            if (MutinyTransitionManager.IsTransitionActive)
            {
                pressed = false;
                return;
            }

            Event guiEvent = Event.current;
            if (guiEvent == null || guiEvent.button != 0)
                return;

            if (guiEvent.type == EventType.MouseDown && CreditsLinkContains(rect, mask, guiEvent.mousePosition))
            {
                pressed = true;
                guiEvent.Use();
            }
            else if (guiEvent.type == EventType.MouseUp && pressed)
            {
                pressed = false;
                bool releasedOnLink = CreditsLinkContains(rect, mask, guiEvent.mousePosition);
                guiEvent.Use();
                if (releasedOnLink)
                    Application.OpenURL(url);
            }
        }

        private static bool CreditsLinkContains(Rect rect, Texture2D mask, Vector2 point)
        {
            if (!rect.Contains(point))
                return false;
            if (mask == null)
                return true;
            int x = Mathf.Clamp(Mathf.FloorToInt(point.x - rect.x), 0, mask.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(rect.yMax - point.y), 0, mask.height - 1);
            return mask.GetPixel(x, y).a > 0.5f;
        }

        private void DrawGameSelect()
        {
            DrawTexture(new Rect(44f, 24f, 460f, 350f), m_GameSelectPanel);
            MutinyLocalizedText.Pirate(new Rect(44f, 32f, 460f, 23f), "frontend.select_game", "select game");
            MutinyLocalizedText.Dangle(new Rect(161f, 76f, 300f, 60f), "frontend.game_select_hint", "click one of the buttons below.||play against the computer or|against a friend!", Color.white, TextAnchor.UpperLeft, 0, 13);
            DrawTexture(new Rect(127f, 169.5f, 345f, 80f), m_GameTypePirates);

            if (DrawOriginalButton(new Rect(63f, 263f, 200f, 24f), "frontend.one_player", "1 player", m_ButtonWide, m_ButtonWideOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_Flow.PressOnePlayer();
                    LogPage("FRONT-02 one player", m_Flow.CurrentPage);
                }, showLoading: false);
            }

            if (DrawOriginalButton(new Rect(287f, 263f, 200f, 24f), "frontend.two_players", "2 players", m_ButtonWide, m_ButtonWideOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    if (m_LevelController == null)
                        m_LevelController = FindAnyObjectByType<MutinyLevelController>();
                    m_LevelController?.ConfigureSession(MutinyGameMode.LocalTwoPlayer, resetVersusWins: true);
                    m_TwoPlayerLoadError = null;
                    m_Flow.PressTwoPlayer();
                    LogPage("2P-NAV-01 two player", m_Flow.CurrentPage);
                }, showLoading: false);
            }
            if (DrawOriginalButton(new Rect(205f, 298f, 140f, 24f), "frontend.lan_play", "lan battle", m_ButtonBack, m_ButtonBackOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_LanStatusText = string.Empty;
                    m_Flow.PressLanMultiplayer();
                    LogPage("LAN-01 lan lobby", m_Flow.CurrentPage);
                }, showLoading: false);
            }
            if (DrawOriginalButton(new Rect(205f, 334f, 140f, 24f), "frontend.back", "back", m_ButtonBack, m_ButtonBackOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_Flow.PressGameSelectBack();
                    LogPage("FRONT-02 back", m_Flow.CurrentPage);
                }, showLoading: false);
            }
        }

        private void DrawLanLobby()
        {
            DrawTexture(new Rect(44f, 24f, 460f, 350f), m_GameSelectPanel);
            MutinyLocalizedText.Pirate(new Rect(44f, 32f, 460f, 23f), "frontend.lan_play", "lan battle");

            // No scene owns a MutinyNetworkManager, so the lobby spawns it here;
            // a null manager would make the host/join buttons report "network unavailable".
            MutinyNetworkManager manager = MutinyNetworkManager.EnsureInstance();
            MutinyNetworkManager.SessionState state = manager != null
                ? manager.State
                : MutinyNetworkManager.SessionState.Idle;

            if (state == MutinyNetworkManager.SessionState.Idle)
            {
                if (m_LanFieldStyle == null)
                {
                    m_LanFieldStyle = new GUIStyle(GUI.skin.textField)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 14,
                        fontStyle = FontStyle.Bold
                    };
                }

                // Browsing is idempotent: the lobby keeps polling the LAN while
                // the page stays idle, and the manager stops it on state changes.
                manager?.StartLanRoomSearch();
                DrawLanRoomList(manager);

                if (DrawOriginalButton(new Rect(63f, 220f, 200f, 24f), "frontend.lan_host", "create host", m_ButtonWide, m_ButtonWideOver))
                {
                    if (manager == null || !manager.StartLanHost())
                        m_LanStatusText = manager != null ? manager.LastError : "network unavailable";
                    else
                        m_LanStatusText = string.Empty;
                }

                GUI.Label(new Rect(287f, 200f, 200f, 16f), MutinyLocalization.Text("frontend.ip", "host ip"));
                m_LanAddressInput = GUI.TextField(new Rect(287f, 220f, 200f, 24f), m_LanAddressInput, 64, m_LanFieldStyle);
                if (DrawOriginalButton(new Rect(287f, 252f, 200f, 24f), "frontend.lan_join", "join", m_ButtonWide, m_ButtonWideOver))
                {
                    if (manager == null || !manager.StartLanClient(m_LanAddressInput))
                        m_LanStatusText = manager != null ? manager.LastError : "network unavailable";
                    else
                        m_LanStatusText = string.Empty;
                }

                DrawFirstPlayerToggle(manager, 194f);
                DrawQuickBattleToggle(manager);
                DrawAllowCheatsToggle(manager);

                if (!string.IsNullOrEmpty(m_LanStatusText))
                    MutinyLocalizedText.Dangle(new Rect(94f, 310f, 360f, 12f), null,
                        m_LanStatusText, Color.white, TextAnchor.MiddleCenter, 0, 13);
            }
            else
            {
                MutinyLocalizedText.Dangle(new Rect(94f, 170f, 360f, 40f), null,
                    LanStatusMessage(state, manager), Color.white, TextAnchor.MiddleCenter, 0, 13);
                // While the host is still waiting for its peer the match rules can
                // still be changed; once a match is live they are fixed for both peers.
                if (state == MutinyNetworkManager.SessionState.Hosting)
                {
                    DrawFirstPlayerToggle(manager, 220f);
                    DrawQuickBattleToggle(manager);
                    DrawAllowCheatsToggle(manager);
                }
            }

            if (DrawOriginalButton(new Rect(205f, 334f, 140f, 24f), "frontend.back", "back", m_ButtonBack, m_ButtonBackOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    if (MutinyNetworkManager.Exists)
                        MutinyNetworkManager.Instance.LeaveSession();
                    // LeaveSession early-returns while idle, so browsing is
                    // stopped here too: the LAN should not be polled from
                    // other pages.
                    MutinyNetworkManager.Instance?.StopLanRoomSearch();
                    m_LanStatusText = string.Empty;
                    m_Flow.PressLanLobbyBack();
                    LogPage("LAN-02 back", m_Flow.CurrentPage);
                }, showLoading: false);
            }
        }

        /// <summary>
        /// Quick-battle switch. The host owns the choice; it travels inside
        /// MatchStartMessage so both peers drop chests at the same speed while the
        /// camera stays on the ordinary turn view.
        /// </summary>
        private void DrawQuickBattleToggle(MutinyNetworkManager manager)
        {
            bool quickBattle = manager != null && manager.QuickBattle;
            string key = quickBattle ? "frontend.quick_battle_on" : "frontend.quick_battle_off";
            string english = quickBattle ? "quick battle: on" : "quick battle: off";
            if (DrawOriginalButton(new Rect(63f, 252f, 200f, 24f), key, english, m_ButtonWide, m_ButtonWideOver))
                manager?.SetQuickBattle(!quickBattle);
        }

        /// <summary>
        /// Room cheats rule. The host owns the choice; it travels inside
        /// MatchStartMessage so both peers show or hide the GM menu for the whole
        /// match alike. Real-player matches keep cheats off by default.
        /// </summary>
        private void DrawAllowCheatsToggle(MutinyNetworkManager manager)
        {
            bool allowCheats = manager != null && manager.AllowCheats;
            string key = allowCheats ? "frontend.cheats_on" : "frontend.cheats_off";
            string english = allowCheats ? "cheats: on" : "cheats: off";
            if (DrawOriginalButton(new Rect(63f, 284f, 200f, 24f), key, english, m_ButtonWide, m_ButtonWideOver))
                manager?.SetAllowCheats(!allowCheats);
        }

        /// <summary>
        /// Opening-side rule. The host owns the choice; it travels inside
        /// MatchStartMessage and a forced side skips the seeded coin flip, so
        /// both peers start with the same team without consuming match RNG.
        /// </summary>
        private void DrawFirstPlayerToggle(MutinyNetworkManager manager, float y)
        {
            MutinyFirstPlayerRule rule = manager != null
                ? manager.FirstPlayer
                : MutinyFirstPlayerRule.Random;
            string key = rule == MutinyFirstPlayerRule.Player1 ? "frontend.first_player_p1"
                : rule == MutinyFirstPlayerRule.Player2 ? "frontend.first_player_p2"
                : "frontend.first_player_random";
            string english = rule == MutinyFirstPlayerRule.Player1 ? "first player: 1P"
                : rule == MutinyFirstPlayerRule.Player2 ? "first player: 2P"
                : "first player: random";
            if (DrawOriginalButton(new Rect(63f, y, 200f, 24f), key, english, m_ButtonWide, m_ButtonWideOver))
                manager?.SetFirstPlayer(NextFirstPlayerRule(rule));
        }

        /// <summary>Random -> 1P -> 2P -> random, the three states of the opening rule.</summary>
        internal static MutinyFirstPlayerRule NextFirstPlayerRule(MutinyFirstPlayerRule current)
        {
            switch (current)
            {
                case MutinyFirstPlayerRule.Player1: return MutinyFirstPlayerRule.Player2;
                case MutinyFirstPlayerRule.Player2: return MutinyFirstPlayerRule.Random;
                default: return MutinyFirstPlayerRule.Player1;
            }
        }

        /// <summary>Discovered LAN rooms, one per red-bordered frame row; clicking a row joins that host.</summary>
        private void DrawLanRoomList(MutinyNetworkManager manager)
        {
            MutinyLocalizedText.Pirate(new Rect(63f, 60f, 300f, 16f), "frontend.lan_rooms",
                "rooms on this network", centered: false);

            List<MutinyLanDiscovery.Room> rooms = manager != null && manager.Discovery != null
                ? manager.Discovery.GetRooms()
                : null;
            bool hasRooms = rooms != null && rooms.Count > 0;

            // One 26px frame row per room, sliced from the same scores_rows
            // texture the high-scores page draws its table rows from. The
            // empty state keeps one frame visible so the room box is always
            // recognizable while the LAN is polled.
            const int visibleRows = 4;
            int drawnRows = hasRooms ? Mathf.Min(rooms.Count, visibleRows) : 1;
            for (int i = 0; i < drawnRows; i++)
            {
                Rect rowRect = new Rect(63f, 80f + i * 28f, 424f, 26f);
                if (m_ScoresRows != null)
                {
                    float vMin = 1f - (float)((i + 1) * 26) / 261f;
                    float vHeight = 26f / 261f;
                    GUI.DrawTextureWithTexCoords(rowRect, m_ScoresRows,
                        new Rect(0f, vMin, 1f, vHeight), true);
                }

                if (!hasRooms)
                {
                    if (i == 0)
                        MutinyLocalizedText.Pirate(rowRect, "frontend.lan_no_rooms",
                            "searching for rooms...");
                    continue;
                }

                MutinyLanDiscovery.Room room = rooms[i];
                bool hovered = !MutinyTransitionManager.IsTransitionActive &&
                               rowRect.Contains(GetCanvasMousePosition());
                if (hovered)
                    MutinyCursorManager.NotifyHoverInteractable();
                MutinyLocalizedText.Pirate(rowRect, null, room.Address, hovered);

                bool clicked = !MutinyTransitionManager.IsTransitionActive &&
                               MutinyControllerUI.Button(rowRect, GUIContent.none, GUIStyle.none,
                                   "button:room:" + room.Address);
                if (clicked)
                {
                    Debug.Log($"[MutinyFrontend] LAN-07 join discovered room {room.Address}:{room.Port}", this);
                    m_LanAddressInput = room.Address;
                    if (manager == null || !manager.StartLanClient(room.Address))
                        m_LanStatusText = manager != null ? manager.LastError : "network unavailable";
                    else
                        m_LanStatusText = string.Empty;
                }
            }
        }

        private static string LanStatusMessage(MutinyNetworkManager.SessionState state, MutinyNetworkManager manager)
        {
            switch (state)
            {
                case MutinyNetworkManager.SessionState.Hosting:
                    return MutinyLocalization.Text("frontend.lan_waiting", "waiting for a player...") +
                           $"\n{GetLocalLanAddress()}:{MutinyNetworkManager.DefaultPort}";
                case MutinyNetworkManager.SessionState.Connecting:
                    return MutinyLocalization.Text("frontend.connecting", "connecting...") +
                           $"\n{(manager != null ? manager.networkAddress : string.Empty)}";
                case MutinyNetworkManager.SessionState.Connected:
                    return manager != null && manager.IsHost
                        ? MutinyLocalization.Text("frontend.lan_host_ready", "player joined! pick a level")
                        : MutinyLocalization.Text("frontend.lan_client_ready", "connected! waiting for the host...");
                case MutinyNetworkManager.SessionState.InMatch:
                    return MutinyLocalization.Text("frontend.lan_in_match", "match in progress");
                default:
                    return string.Empty;
            }
        }

        private static string GetLocalLanAddress()
        {
            // Dns.GetHostEntry lists virtual adapters first on many dev machines
            // (VMware/Hyper-V/WSL), whose addresses peers cannot reach. Prefer an
            // interface that owns a default gateway, then any usable IPv4.
            try
            {
                string anyCandidate = null;
                foreach (System.Net.NetworkInformation.NetworkInterface nic in
                         System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up ||
                        nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback ||
                        nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Tunnel)
                        continue;

                    foreach (System.Net.NetworkInformation.UnicastIPAddressInformation info in
                             nic.GetIPProperties().UnicastAddresses)
                    {
                        if (info.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                            System.Net.IPAddress.IsLoopback(info.Address))
                            continue;
                        if (nic.GetIPProperties().GatewayAddresses != null &&
                            nic.GetIPProperties().GatewayAddresses.Count > 0)
                            return info.Address.ToString();
                        anyCandidate ??= info.Address.ToString();
                    }
                }
                if (anyCandidate != null)
                    return anyCandidate;
            }
            catch (Exception)
            {
                // Interface enumeration can fail on locked-down machines; fall
                // through to the loopback hint.
            }
            return "127.0.0.1";
        }

        private void HookNetworkIfPresent()
        {
            if (m_NetworkHooked || !MutinyNetworkManager.Exists)
                return;
            MutinyNetworkManager manager = MutinyNetworkManager.Instance;
            manager.OnStateChanged += OnNetworkStateChanged;
            manager.OnMatchStartReceived += OnMatchStartReceived;
            manager.OnSessionEnded += OnNetworkSessionEnded;
            manager.OnMatchControlRequested += OnMatchControlRequested;
            manager.OnLobbyReturn += OnLobbyReturnReceived;
            m_NetworkHooked = true;
        }

        private void UnhookNetwork()
        {
            if (!m_NetworkHooked)
                return;
            if (MutinyNetworkManager.Exists)
            {
                MutinyNetworkManager manager = MutinyNetworkManager.Instance;
                manager.OnStateChanged -= OnNetworkStateChanged;
                manager.OnMatchStartReceived -= OnMatchStartReceived;
                manager.OnSessionEnded -= OnNetworkSessionEnded;
                manager.OnMatchControlRequested -= OnMatchControlRequested;
                manager.OnLobbyReturn -= OnLobbyReturnReceived;
            }
            m_NetworkHooked = false;
        }

        private void OnDestroy() => UnhookNetwork();

        private void OnNetworkStateChanged(MutinyNetworkManager.SessionState state)
        {
            if (state == MutinyNetworkManager.SessionState.Connected)
            {
                MutinyNetworkManager manager = MutinyNetworkManager.Instance;
                if (manager.IsHost && m_Flow.CurrentPage == MutinyFrontendPage.LanLobby &&
                    !m_OnlineHostPickingLevel)
                {
                    m_OnlineHostPickingLevel = true;
                    if (m_LevelController == null)
                        m_LevelController = FindAnyObjectByType<MutinyLevelController>();
                    m_LevelController?.ConfigureSession(MutinyGameMode.OnlineTwoPlayer, resetVersusWins: true);
                    m_TwoPlayerLoadError = null;
                    m_Flow.EnterOnlineLevelSelect();
                    LogPage("LAN-03 host level select", m_Flow.CurrentPage);
                }
            }
            else if (state == MutinyNetworkManager.SessionState.Idle)
            {
                m_OnlineHostPickingLevel = false;
            }
        }

        private void OnMatchStartReceived(int levelIndex, int matchSeed)
        {
            if (StartLevel(levelIndex, MutinyGameMode.OnlineTwoPlayer))
            {
                m_Flow.EnterOnlineGameplay();
                if (MutinyNetworkManager.Exists)
                    MutinyNetworkManager.Instance.NotifyMatchActive();
                LogPage($"LAN-04 match start level={levelIndex:D2} seed={matchSeed}", m_Flow.CurrentPage);
            }
            else
            {
                if (MutinyNetworkManager.Exists)
                    MutinyNetworkManager.Instance.LeaveSession();
                m_LanStatusText = MutinyLocalization.Text("frontend.level_load_failed", "level failed to load");
            }
        }

        private void OnNetworkSessionEnded(string reason)
        {
            m_OnlineHostPickingLevel = false;
            Debug.Log($"[MutinyFrontend] LAN-05 session ended: {reason}", this);
            m_LanStatusText = MutinyLocalization.Text("frontend.disconnected", "disconnected");
            if (m_Flow.CurrentPage == MutinyFrontendPage.Gameplay)
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    if (m_LevelController == null)
                        m_LevelController = FindAnyObjectByType<MutinyLevelController>();
                    m_LevelController?.ClearLevel();
                    m_Flow.ForceReturnToTitle();
                    MutinyAudioManager.Instance?.PlayMusic("menu_music");
                    LogPage("LAN-06 disconnect to title", m_Flow.CurrentPage);
                }, showLoading: false);
            }
        }

        /// <summary>HUD quit corner during an online match: leave and go to title.</summary>
        public void QuitOnlineMatchToTitle()
        {
            if (MutinyNetworkManager.Exists)
                MutinyNetworkManager.Instance.LeaveSession();
            m_LevelController?.ClearLevel();
            m_Flow.ForceReturnToTitle();
            MutinyAudioManager.Instance?.PlayMusic("menu_music");
        }

        /// <summary>
        /// End-of-match "restart level" during an online match: the host re-seeds
        /// the same level on both peers; the client asks the host to do it. The
        /// local board is rebuilt by the MatchStart that follows, never directly.
        /// </summary>
        public void RequestOnlineRestart()
        {
            MutinyNetworkManager manager = MutinyNetworkManager.Instance;
            if (manager == null || !MutinyNetworkManager.IsOnlineMatch)
                return;
            if (m_LevelController == null)
                m_LevelController = FindAnyObjectByType<MutinyLevelController>();
            if (manager.IsHost)
            {
                if (m_LevelController != null)
                    manager.HostStartMatch(m_LevelController.CurrentLevelIndex);
            }
            else
            {
                manager.SendMatchControlRequest(MutinyMatchControlAction.RestartLevel);
            }
            Debug.Log("[MutinyFrontend] LAN-08 online restart requested", this);
        }

        /// <summary>
        /// End-of-match "back to menu" during an online match: the host re-opens
        /// the versus picker, the client waits in the lobby, and the session stays
        /// connected for the next MatchStart.
        /// </summary>
        public void OnlineBackToLevelSelect()
        {
            if (!MutinyNetworkManager.IsOnlineMatch)
                return;
            ApplyOnlineLobbyReturn();
            MutinyNetworkManager.Instance?.ReturnToLobby();
        }

        // The local half of the lobby return; the peer is notified by the caller
        // so a received LobbyReturn never echoes back over the wire.
        private void ApplyOnlineLobbyReturn()
        {
            MutinyNetworkManager manager = MutinyNetworkManager.Instance;
            bool host = manager != null && manager.IsHost;
            if (m_LevelController == null)
                m_LevelController = FindAnyObjectByType<MutinyLevelController>();
            m_LevelController?.ClearLevel();
            if (host)
            {
                m_OnlineHostPickingLevel = true;
                m_Flow.ReturnToOnlineLevelSelect();
            }
            else
            {
                m_Flow.ReturnToLanLobby();
            }
            MutinyAudioManager.Instance?.PlayMusic("menu_music");
            LogPage("LAN-07 online match back to lobby", m_Flow.CurrentPage);
        }

        // Host: the remote player pressed an end-of-match popup button.
        private void OnMatchControlRequested(MutinyMatchControlAction action)
        {
            if (action == MutinyMatchControlAction.RestartLevel)
                RequestOnlineRestart();
            else if (action == MutinyMatchControlAction.BackToLevelSelect)
                OnlineBackToLevelSelect();
        }

        // Client: the host left the finished match for the level picker.
        private void OnLobbyReturnReceived()
        {
            ApplyOnlineLobbyReturn();
        }

        private void DrawLevelSelect()
        {
            DrawTexture(new Rect(44f, 24f, 460f, 350f), m_LevelSelectPanel);
            MutinyLocalizedText.Pirate(new Rect(44f, 32f, 460f, 23f), "frontend.select_level", "select level");

            for (int level = 1; level <= SinglePlayerLevelCount; level++)
            {
                int column = (level - 1) % 5;
                int row = (level - 1) / 5;
                DrawLevelButton(level, new Rect(110f + column * 70f, 68f + row * 90f, 51f, 77f));
            }

            if (DrawOriginalButton(new Rect(205f, 334f, 140f, 24f), "frontend.back", "back", m_ButtonBack, m_ButtonBackOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_Flow.PressLevelSelectBack();
                    LogPage("FRONT-03 back", m_Flow.CurrentPage);
                }, showLoading: false);
            }
        }

        private void DrawTwoPlayerLevelSelect()
        {
            // Root frame 111 uses its own panel (shape 1959). It contains the
            // score border and the divider above the two bottom buttons.
            DrawTexture(new Rect(44f, 24f, 460f, 350f), m_TwoPlayerPanel);
            MutinyLocalizedText.Pirate(new Rect(44f, 32f, 460f, 23f), "frontend.select_level", "select level");
            int level = m_Flow.SelectedTwoPlayerLevel;

            // Sprite 629 places shape 578 at (275,201). Sprite 628's exported
            // image also contains a white dynamic-text placeholder below its map.
            // Draw only the map into the frame interior so neither placeholder
            // nor white map pixels cover the red border and name strip.
            DrawTexture(new Rect(174f, 125f, 202f, 152f), m_TwoPlayerPreviewFrame);
            DrawTwoPlayerMapPreview(m_TwoPlayerPreviews[level - 16]);
            MutinyBitmapFont.DrawDangleText(new Rect(175f, 260f, 200f, 16f),
                TwoPlayerLevelNames[level - 16], Color.white, TextAnchor.MiddleCenter, 0, 13);
            if (level > 16 && DrawTwoPlayerArrow(new Rect(117.5f, 182f, 35f, 38f), m_TwoPlayerPrevUp, m_TwoPlayerPrevOver))
            {
                m_Flow.StepTwoPlayerLevel(-1);
                m_TwoPlayerLoadError = null;
            }
            if (level < 33 && DrawTwoPlayerArrow(new Rect(394.5f, 182f, 35f, 38f), m_TwoPlayerNextUp, m_TwoPlayerNextOver))
            {
                m_Flow.StepTwoPlayerLevel(1);
                m_TwoPlayerLoadError = null;
            }

            int p1 = m_LevelController != null ? m_LevelController.Player1Wins : 0;
            int p2 = m_LevelController != null ? m_LevelController.Player2Wins : 0;
            DrawTexture(new Rect(186f, 81f, 177f, 23f), m_TwoPlayerScorePirates);
            MutinyBitmapFont.DrawDangleText(new Rect(228f, 85f, 34f, 14f),
                p1.ToString(), Color.white, TextAnchor.MiddleCenter, 0, 13);
            MutinyLocalizedText.Dangle(new Rect(262f, 85f, 26f, 14f), "frontend.versus",
                "vs", Color.white, TextAnchor.MiddleCenter, 0, 13);
            MutinyBitmapFont.DrawDangleText(new Rect(287f, 85f, 34f, 14f),
                p2.ToString(), Color.white, TextAnchor.MiddleCenter, 0, 13);
            if (DrawOriginalButton(new Rect(205f, 296f, 140f, 24f), "frontend.play", "play", m_ButtonBack, m_ButtonBackOver))
            {
                if (!MutinyLevelController.HasNumberedLevelData(level))
                    m_TwoPlayerLoadError = "frontend.level_unavailable";
                else
                {
                    MutinyTransitionManager.RequestTransition(() =>
                    {
                        if (m_OnlineHostPickingLevel)
                        {
                            // MatchStart drives StartLevel on both peers, so the
                            // host only publishes the level choice here.
                            if (MutinyNetworkManager.Exists)
                                MutinyNetworkManager.Instance.HostStartMatch(level);
                            return;
                        }
                        if (StartLevel(level, MutinyGameMode.LocalTwoPlayer))
                            m_Flow.TrySelectTwoPlayerLevel(MutinyLevelController.HasNumberedLevelData);
                        else
                            m_TwoPlayerLoadError = "frontend.level_load_failed";
                    }, showLoading: true);
                }
            }
            if (!string.IsNullOrEmpty(m_TwoPlayerLoadError))
                MutinyLocalizedText.Dangle(new Rect(94f, 320f, 360f, 12f), null,
                    MutinyLocalization.Text(m_TwoPlayerLoadError,
                        m_TwoPlayerLoadError == "frontend.level_unavailable"
                            ? "level {0:D2} data unavailable" : "level {0:D2} failed to load", level),
                    Color.white, TextAnchor.MiddleCenter, 0, 13);
            if (DrawOriginalButton(new Rect(205f, 334f, 140f, 24f), "frontend.back", "back", m_ButtonBack, m_ButtonBackOver))
            {
                MutinyTransitionManager.RequestTransition(() => m_Flow.PressTwoPlayerLevelSelectBack(), showLoading: false);
            }
        }

        private static void DrawTwoPlayerMapPreview(Texture2D preview)
        {
            if (preview == null)
                return;

            // FFDec's sprite 628 raster has the 192x129 map at pixels
            // x=82..273, y=0..128. The remaining pixels are transparent or
            // a dynamic text placeholder, which the original Flash replaces.
            Rect source = new Rect(82f / preview.width,
                1f - 129f / preview.height, 192f / preview.width, 129f / preview.height);
            GUI.DrawTextureWithTexCoords(new Rect(179f, 130f, 192f, 129f), preview, source, true);
        }

        private static bool DrawTwoPlayerArrow(Rect rect, Texture2D up, Texture2D over)
        {
            bool active = !MutinyTransitionManager.IsTransitionActive;
            bool hovered = active && rect.Contains(GetCanvasMousePosition());
            if (hovered)
                MutinyCursorManager.NotifyHoverInteractable();
            Texture2D texture = hovered && over != null ? over : up;
            if (texture != null)
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            return active && MutinyControllerUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void DrawHelp()
        {
            DrawTexture(new Rect(44f, 24f, 460f, 350f), m_HelpPanel != null ? m_HelpPanel : m_GameSelectPanel);
            MutinyLocalizedText.Pirate(new Rect(44f, 32f, 460f, 23f), "frontend.help", "help");

            float elapsed = Mathf.Max(0f, Time.unscaledTime - m_HelpOpenTime);
            int frameIndex = (int)(elapsed * 25f) % HelpTutorialFrameCount;
            if (frameIndex < 0)
                frameIndex += HelpTutorialFrameCount;

            Texture2D frameTex = m_HelpTutorialFrames[frameIndex];
            if (frameTex != null)
            {
                DrawTexture(new Rect(114f, 94f, 514f, 351f), frameTex);
            }

            string tutorialText = (frameIndex < 64)
                ? "use your weapons to fire at your opponents|click and drag and then let go to throw them"
                : "click on a character and throw him to move him";

            MutinyLocalizedText.Dangle(new Rect(44f, 286f, 460f, 40f),
                frameIndex < 64 ? "frontend.help_fire" : "frontend.help_move",
                tutorialText, Color.white, TextAnchor.UpperCenter, 0, 13);

            if (DrawOriginalButton(new Rect(205f, 334f, 140f, 24f), "frontend.back", "back", m_ButtonBack, m_ButtonBackOver))
            {
                MutinyTransitionManager.RequestTransition(() =>
                {
                    m_Flow.PressHelpBack();
                    LogPage("FRONT-HELP back", m_Flow.CurrentPage);
                }, showLoading: false);
            }
        }

        private void DrawEnding()
        {
            // Sprite 802 places sprite 801 at (210,140), which places ship
            // 780 at (-49,-33). Its nested clips run independently of 801.
            DrawEndingShip();
            if (m_HasAnimatedBackground)
            {
                GUI.BeginGroup(new Rect(0f, 0f, CanvasWidth, CanvasHeight));
                DrawBackgroundLayer(6, m_BackgroundOffsets[0], 332f);
                GUI.EndGroup();
            }

            int dialogueIndex = MutinyEndingSequence.DialogueIndexAtFrame(m_EndingFrame);
            if (dialogueIndex >= 0)
            {
                MutinyEndingSequence.Dialogue dialogue = MutinyEndingSequence.Dialogues[dialogueIndex];
                DrawTexture(dialogue.Bubble,
                    dialogue.Symbol == 792 ? m_EndingKrakenBubble : m_EndingBubble);
                if (!MutinyLocalization.UseOriginalFont)
                {
                    string text = MutinyEndingSequence.LocalizedDialogue(dialogueIndex);
                    // Preserve the original segment's reveal duration and end frame.
                    // CJK wraps naturally instead of reusing English's manual lines.
                    float reveal = MutinyEndingSequence.DialogueRevealAtFrame(m_EndingFrame, dialogueIndex);
                    int visibleCharacters = Mathf.Min(text.Length, Mathf.CeilToInt(text.Length * reveal));
                    MutinyLocalizedText.Speech(MutinyGameHUD.ResolveSpeechTextRect(dialogue.Bubble),
                        null, text.Substring(0, visibleCharacters));
                }
                else
                {
                    string[] lines = dialogue.Text.Split('|');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        float reveal = MutinyEndingSequence.LineRevealAtFrame(m_EndingFrame, dialogueIndex, i);
                        if (reveal <= 0f)
                            continue;
                        float textWidth = Mathf.Min(212f, MutinyBitmapFont.MeasureDangleText(lines[i]));
                        float lineY = dialogue.TextOrigin.y + i * 13f;
                        GUI.BeginGroup(new Rect(dialogue.TextOrigin.x, lineY,
                            Mathf.Max(1f, textWidth * reveal), 13f));
                        MutinyBitmapFont.DrawSpeechText(new Rect(0f, 0f, textWidth, 13f),
                            lines[i], TextAnchor.UpperLeft, 0, 13);
                        GUI.EndGroup();
                    }
                }
            }

            if (!MutinyEndingSequence.IsScoreVisible(m_EndingFrame))
                return;

            float top = MutinyEndingSequence.ScorePanelTop(m_EndingFrame);
            DrawTexture(new Rect(101f, top, 360f, 190f), m_EndingScorePanel);
            MutinyLocalizedText.Pirate(new Rect(101f, top + 22f, 360f, 24f),
                "ending.congratulations", "congratulations");
            MutinyLocalizedText.Dangle(new Rect(139f, top + 73f, 165f, 20f),
                "ending.final_score", "final score", Color.white, TextAnchor.MiddleLeft);
            MutinyBitmapFont.DrawDangleText(new Rect(314f, top + 73f, 95f, 20f),
                m_EndingScore.ToString(), Color.white, TextAnchor.MiddleLeft);

            Rect backRect = MutinyEndingSequence.BackButtonRect(m_EndingFrame);
            bool canReturn = MutinyEndingSequence.CanReturnToTitle(m_EndingFrame) &&
                !MutinyTransitionManager.IsTransitionActive;
            bool hovered = canReturn && backRect.Contains(GetCanvasMousePosition());
            if (hovered)
                MutinyCursorManager.NotifyHoverInteractable();
            DrawTexture(backRect, hovered && m_EndingBackButtonOver != null
                ? m_EndingBackButtonOver : m_EndingBackButton);
            MutinyLocalizedText.PirateButton(backRect, "ending.back_title", "back to title", hovered);
            if (canReturn && MutinyControllerUI.Button(backRect, GUIContent.none, GUIStyle.none, "ending-back", true))
                MutinyTransitionManager.RequestTransition(() => ReturnFromEndingToTitle(), showLoading: false);
        }

        private void DrawEndingShip()
        {
            if (!m_HasAnimatedEndingShip)
            {
                DrawTexture(new Rect(161f, 107f, 228f, 287f), m_EndingShip);
                return;
            }

            for (int i = 0; i < MutinyEndingSequence.ShipLayers.Length; i++)
            {
                MutinyEndingSequence.ShipLayer layer = MutinyEndingSequence.ShipLayers[i];
                int frame = MutinyEndingSequence.ShipFrameAt(m_EndingShipTick, layer.SymbolIndex);
                Texture2D texture = m_EndingShipParts[layer.SymbolIndex][frame - 1];
                DrawTexture(new Rect(161f + layer.Offset.x, 107f + layer.Offset.y,
                    texture.width, texture.height), texture);
            }
        }

        private void DrawLevelButton(int level, Rect rect)
        {
            bool isTransitioning = MutinyTransitionManager.IsTransitionActive;
            bool unlocked = MutinySaveSystem.IsLevelUnlocked(level);
            bool hovered = !isTransitioning && unlocked && rect.Contains(GetCanvasMousePosition());
            bool pressed = !isTransitioning && unlocked && IsPointerDown(rect);

            if (hovered)
                MutinyCursorManager.NotifyHoverInteractable();

            GUI.color = Color.white;
            DrawTexture(rect, m_LevelSlot);

            Texture2D preview = m_LevelPreviews[level - 1];
            GUI.color = unlocked ? Color.white : Color.black;
            DrawTexture(new Rect(rect.x + 5.25f, rect.y + 3f, 39f, 56f), preview);
            GUI.color = Color.white;

            if (unlocked)
            {
                MutinyBitmapFont.DrawDangleText(new Rect(rect.x, rect.y + 60f, rect.width, 14f), level.ToString("D2"), Color.white, TextAnchor.MiddleCenter);
                if (hovered && m_LevelSlotOver != null)
                {
                    Color prev = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 102f / 255f);
                    DrawTexture(rect, m_LevelSlotOver);
                    GUI.color = prev;
                }

                bool clicked = !isTransitioning && MutinyControllerUI.Button(rect, GUIContent.none, GUIStyle.none, "level:" + level);
                if ((clicked || pressed) && !isTransitioning)
                {
                    int targetLevel = level;
                    MutinyTransitionManager.RequestTransition(() =>
                    {
                        if (MutinySaveSystem.IsLevelUnlocked(targetLevel) &&
                            StartLevel(targetLevel, MutinyGameMode.SinglePlayer))
                            m_Flow.TrySelectLevel(targetLevel, MutinySaveSystem.IsLevelUnlocked);
                    }, showLoading: true);
                }
            }
            else
            {
                MutinyBitmapFont.DrawDangleText(new Rect(rect.x, rect.y + 20f, rect.width, 18f), "?", Color.white, TextAnchor.MiddleCenter);
            }
        }

        private bool DrawOriginalButton(Rect rect, string key, string english, Texture2D texture, Texture2D hoverTexture = null, bool activateOnPress = false)
        {
            bool isTransitioning = MutinyTransitionManager.IsTransitionActive;
            bool hovered = !isTransitioning && rect.Contains(GetCanvasMousePosition());
            bool pressed = !isTransitioning && activateOnPress && IsPointerDown(rect);

            if (hovered)
                MutinyCursorManager.NotifyHoverInteractable();

            Texture2D texToDraw = (hovered && hoverTexture != null) ? hoverTexture : texture;
            DrawTexture(rect, texToDraw);
            MutinyLocalizedText.PirateButton(rect, key, english, hovered);

            bool clicked = !isTransitioning && MutinyControllerUI.Button(rect, GUIContent.none, GUIStyle.none,
                "button:" + english, key == "frontend.back");
            return activateOnPress ? (pressed || clicked) : clicked;
        }

        private static bool IsPointerDown(Rect rect)
        {
            Event guiEvent = Event.current;
            return guiEvent != null && guiEvent.type == EventType.MouseDown && guiEvent.button == 0 &&
                   rect.Contains(GetCanvasMousePosition());
        }

        private static Vector2 GetCanvasMousePosition()
        {
            // IMGUI transforms Event.current.mousePosition into the active GUI.matrix
            // coordinate space before controls and custom drawing are evaluated. Applying
            // ScreenToCanvasPoint or GUI.matrix.inverse here transforms the pointer a second time,
            // so the original Flash-style up/over hit tests fail when scaled or letterboxed.
            return MutinyInputHub.GuiPointerPosition;
        }

        private bool StartLevel(int level, MutinyGameMode mode)
        {
            Debug.Log($"[MutinyFrontend] FRONT-05 select level={level:D2}; loading production level", this);
            if (m_LevelController == null)
                m_LevelController = FindAnyObjectByType<MutinyLevelController>();

            if (m_LevelController == null)
            {
                Debug.LogError("[MutinyFrontend] Cannot enter gameplay: MutinyLevelController was not found.", this);
                return false;
            }

            // LevelSelectButton.doPress clears `_root.score` before entering a
            // new one-player game.  Advancing inside an active game does not.
            m_LevelController.ConfigureSession(mode);
            if (mode == MutinyGameMode.SinglePlayer)
                m_LevelController.ResetSinglePlayerScore();
            if (!m_LevelController.TryLoadLevel(level))
                return false;
            if (m_LevelController.CurrentLevel != null)
                m_LevelController.CurrentLevel.gameObject.SetActive(true);
            MutinyAudioManager.Instance?.PlayMusic("game_music");
            return true;
        }

        /// <summary>
        /// Production counterpart of QuitGameButton's level_select_1p callback.
        /// QuitGameButton calls Controller.endGame/unloadLevel before the selector.
        /// </summary>
        public bool ReturnToSinglePlayerLevelSelect()
        {
            if (!m_Flow.ReturnToSinglePlayerLevelSelect())
                return false;

            if (m_LevelController == null)
                m_LevelController = FindAnyObjectByType<MutinyLevelController>();
            m_LevelController?.ClearLevel();

            MutinyAudioManager.Instance?.PlayMusic("menu_music");
            Debug.Log("[MutinyFrontend] HUD-CORNER-04 back to menu -> level select 1p", this);
            return true;
        }

        public bool ShowEnding(int finalScore, MutinyLevelController levelController)
        {
            if (levelController == null)
                return false;
            if (!m_Flow.ShowEnding())
                return false;

            m_LevelController = levelController;
            m_LevelController.ClearLevel();
            m_EndingFrame = 1;
            m_EndingShipTick = 0;
            m_EndingScore = finalScore;
            m_BackgroundTickAccumulator = 0f;
            // Root frame 131 does not change music. Preserve the active
            // AudioSource itself so the original game track keeps its position.
            // Recover the logical target only if this page was reached with a
            // different track (for example, a restored session).
            MutinyAudioManager audio = MutinyAudioManager.Instance;
            if (audio != null &&
                (audio.MusicSource == null || audio.MusicSource.clip == null ||
                 audio.MusicSource.clip.name != "game_music"))
                audio.PlayMusic("game_music");
            Debug.Log($"[MutinyFrontend] END-SEQ-01 congratulations -> ending score={finalScore}", this);
            return true;
        }

        public bool ReturnFromEndingToTitle()
        {
            if (!m_Flow.ReturnFromEndingToTitle())
                return false;
            MutinyAudioManager.Instance?.PlayMusic("menu_music");
            Debug.Log("[MutinyFrontend] END-SEQ-04 ending -> title", this);
            return true;
        }

        public bool ReturnToTwoPlayerLevelSelect()
        {
            if (!m_Flow.ReturnToTwoPlayerLevelSelect())
                return false;
            if (m_LevelController == null)
                m_LevelController = FindAnyObjectByType<MutinyLevelController>();
            m_LevelController?.ClearLevel();
            MutinyAudioManager.Instance?.PlayMusic("menu_music");
            return true;
        }

        private void DrawCornerAudioControls()
        {
            Rect sfxVisualRect = MutinyGameHUD.ResolveOriginalCornerVisualRect(MutinyCornerControl.Sfx);
            Rect musicVisualRect = MutinyGameHUD.ResolveOriginalCornerVisualRect(MutinyCornerControl.Music);
            Rect sfxHitRect = MutinyGameHUD.ResolveOriginalCornerHitRect(MutinyCornerControl.Sfx);
            Rect musicHitRect = MutinyGameHUD.ResolveOriginalCornerHitRect(MutinyCornerControl.Music);
            MutinyAudioManager audio = MutinyAudioManager.Instance;

            Vector2 mousePosition = GetCanvasMousePosition();
            bool sfxHovered = MutinyGameHUD.IsCornerHovered(MutinyCornerControl.Sfx, mousePosition, m_SfxHovered);
            bool musicHovered = MutinyGameHUD.IsCornerHovered(MutinyCornerControl.Music, mousePosition, m_MusicHovered);
            UpdateCornerHover(ref m_SfxHovered, sfxHovered);
            UpdateCornerHover(ref m_MusicHovered, musicHovered);

            if (!MutinyTransitionManager.IsTransitionActive && (sfxHovered || musicHovered))
                MutinyCursorManager.NotifyHoverInteractable();

            // Draw unhovered first, hovered last so active tooltip bubble is always on top
            if (!musicHovered)
                DrawCornerSprite(musicVisualRect,
                    ResolveCornerToggleTexture(true, audio != null && audio.MusicEnabled, false), false, MutinyCornerControl.Music);
            if (!sfxHovered)
                DrawCornerSprite(sfxVisualRect,
                    ResolveCornerToggleTexture(false, audio != null && audio.SfxEnabled, false), false, MutinyCornerControl.Sfx);

            if (musicHovered)
                DrawCornerSprite(musicVisualRect,
                    ResolveCornerToggleTexture(true, audio != null && audio.MusicEnabled, true), true, MutinyCornerControl.Music);
            if (sfxHovered)
                DrawCornerSprite(sfxVisualRect,
                    ResolveCornerToggleTexture(false, audio != null && audio.SfxEnabled, true), true, MutinyCornerControl.Sfx);

            Rect sfxBubbleRect = MutinyGameHUD.ResolveOriginalCornerBubbleRect(MutinyCornerControl.Sfx);
            Rect musicBubbleRect = MutinyGameHUD.ResolveOriginalCornerBubbleRect(MutinyCornerControl.Music);

            if (!MutinyTransitionManager.IsTransitionActive)
            {
                if (MutinyControllerUI.Button(sfxHitRect, GUIContent.none, GUIStyle.none) ||
                    (sfxHovered && MutinyControllerUI.Button(sfxBubbleRect, GUIContent.none, GUIStyle.none, controllerEnabled: false)))
                    audio?.ToggleSFX();
                if (MutinyControllerUI.Button(musicHitRect, GUIContent.none, GUIStyle.none) ||
                    (musicHovered && MutinyControllerUI.Button(musicBubbleRect, GUIContent.none, GUIStyle.none, controllerEnabled: false)))
                    audio?.ToggleMusic();
            }
        }

        private static void DrawCornerSprite(Rect rect, Texture2D texture, bool hovered, MutinyCornerControl control)
        {
            MutinyGameHUD.DrawLocalizedCornerSprite(rect, texture, hovered, control);
        }

        private void UpdateCornerHover(ref bool previous, bool current)
        {
            if (current && !previous)
                MutinyAudioManager.Instance?.PlaySFX("rollover");
            previous = current;
        }

        private Texture2D ResolveCornerToggleTexture(bool isMusic, bool enabled, bool hovered)
        {
            MutinyCornerToggleVisualState state = MutinyGameHUD.ResolveCornerToggleVisualState(enabled, hovered);
            if (isMusic)
            {
                switch (state)
                {
                    case MutinyCornerToggleVisualState.OnUp: return m_MusicCornerOnUpTexture;
                    case MutinyCornerToggleVisualState.OnOver: return m_MusicCornerOnOverTexture;
                    case MutinyCornerToggleVisualState.OffUp: return m_MusicCornerOffUpTexture;
                    default: return m_MusicCornerOffOverTexture;
                }
            }

            switch (state)
            {
                case MutinyCornerToggleVisualState.OnUp: return m_SfxCornerOnUpTexture;
                case MutinyCornerToggleVisualState.OnOver: return m_SfxCornerOnOverTexture;
                case MutinyCornerToggleVisualState.OffUp: return m_SfxCornerOffUpTexture;
                default: return m_SfxCornerOffOverTexture;
            }
        }

        private static void DrawTexture(Rect rect, Texture2D texture)
        {
            if (texture != null)
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        }

        private static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style)
        {
            Color textColor = style.normal.textColor;
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = textColor;
            GUI.Label(rect, text, style);
        }

        private void LogPage(string action, MutinyFrontendPage page)
        {
            Debug.Log($"[MutinyFrontend] {action} -> {page}", this);
        }
    }
}
