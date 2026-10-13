using System;
using System.Collections.Generic;
using Mutiny.Presentation;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Levels
{
    public static class MutinyLevelBuilder
    {
        public const float PixelsPerUnit = 32f;
        public const float CellSize = 1.0f; // 32 pixels / 32 PPU = 1 Unity unit

        public const int BackgroundSortingOrder = -10;
        public const int ObjectSortingOrder = 0;
        public const int TerrainSortingOrder = 10;
        // Clip.show() places each Character holder at characterLayer's next
        // highest Flash depth. Reserve eight Unity orders for each holder: the
        // body plus characterOverlay's five child levels, with one spare order.
        public const int CharacterSortingOrder = 20;
        public const int CharacterSortingStride = 8;
        public const int CharacterOverlaySortingOffset = 1;
        public const int CharacterHealthBarSortingOrder = 210;
        public const int CharacterHealthBarSortingStride = 2;
        public const int TidalWaveSortingOrder = 250;
        public const int SeagullSortingOrder = 260;
        public const int WaterSortingOrder = 300;
        // Solid.splashCheck shows the splash, then hides and re-shows Water in
        // the same Flash waterLayer. Water therefore ends up above the splash.
        public const int SplashSortingOrder = WaterSortingOrder - 1;

        public static int GetCharacterSortingOrder(int originalCreationIndex)
        {
            return CharacterSortingOrder + Mathf.Max(0, originalCreationIndex) * CharacterSortingStride;
        }

        public static int GetCharacterHealthBarSortingOrder(int originalCreationIndex)
        {
            return CharacterHealthBarSortingOrder + Mathf.Max(0, originalCreationIndex) * CharacterHealthBarSortingStride;
        }

        public static GameObject BuildLevel(MutinyLevelData levelData, Transform parent = null, int levelIndex = 1,
            MutinyGameMode? menuMode = null)
        {
            if (levelData == null)
                throw new ArgumentNullException(nameof(levelData));

            // TileSystem.readXML resets Controller.boxes before constructing every
            // level. Do this synchronously because the prior Unity root is normally
            // destroyed at end-of-frame during restart/next-level transitions.
            MutinyBoxRegistry.ClearForLevelUnload();
            MutinyMine.ClearForLevelUnload();

            string rootName = string.IsNullOrEmpty(levelData.Name) || levelData.Name == "undefined"
                ? "MutinyLevel"
                : $"MutinyLevel_{levelData.Name}";

            GameObject levelRootObj = new GameObject(rootName);
            if (parent != null)
                levelRootObj.transform.SetParent(parent, false);

            MutinyLevelRoot levelRoot = levelRootObj.AddComponent<MutinyLevelRoot>();
            levelRoot.LevelName = levelData.Name;
            levelRoot.Width = levelData.Width;
            levelRoot.Height = levelData.Height;
            levelRoot.Players = levelData.Players;
            levelRoot.SkyColour = MutinyOriginalBackground.SkyColourForLevel(levelIndex);
            MutinyAnimatedTiles animatedTiles = levelRootObj.AddComponent<MutinyAnimatedTiles>();

            // 1. Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(levelRootObj.transform, false);
            levelRoot.BackgroundHolder = bgObj.transform;
            BuildBackground(levelData, bgObj.transform, animatedTiles);

            // 2. Terrain
            GameObject terrainObj = new GameObject("Terrain");
            terrainObj.transform.SetParent(levelRootObj.transform, false);
            levelRoot.TerrainHolder = terrainObj.transform;
            BuildTerrain(levelData, terrainObj.transform, animatedTiles);

            // 3. Water
            GameObject waterObj = new GameObject("Water");
            waterObj.transform.SetParent(levelRootObj.transform, false);
            levelRoot.WaterHolder = waterObj.transform;
            levelRoot.WaterLevelY = BuildWater(levelData, waterObj.transform, levelRoot.SkyColour);
            bgObj.AddComponent<MutinyBattleBackground>().Initialize(levelRoot,
                levelRoot.SkyColour);

            // 4. Objects (Characters & Items)
            GameObject objectsObj = new GameObject("Objects");
            objectsObj.transform.SetParent(levelRootObj.transform, false);
            levelRoot.ObjectsHolder = objectsObj.transform;
            BuildObjects(levelData, objectsObj.transform, levelRoot, menuMode);

            var chestManager = levelRootObj.AddComponent<MutinyTreasureChestManager>();
            chestManager.Initialize(levelData, levelRoot);

            // 5. Turn Management & Input & HUD
            var turnManager = levelRootObj.AddComponent<MutinyTurnManager>();
            turnManager.Team1 = levelRoot.Team1;
            turnManager.Team2 = levelRoot.Team2;

            var playerInput = levelRootObj.AddComponent<MutinyPlayerInput>();
            playerInput.TurnManager = turnManager;

            var hud = levelRootObj.AddComponent<MutinyGameHUD>();
            hud.TurnManager = turnManager;
            hud.PlayerInput = playerInput;

            var speech = levelRootObj.AddComponent<MutinySpeechController>();
            speech.Initialize(turnManager);
            hud.Speech = speech;

            var ingameText = levelRootObj.AddComponent<MutinyIngameTextArea>();
            ingameText.Initialize(turnManager, speech);
            hud.IngameText = ingameText;

            // Ensure AudioManager instance exists
            _ = MutinyAudioManager.Instance;

            return levelRootObj;
        }

        private static void BuildBackground(MutinyLevelData levelData, Transform parent,
            MutinyAnimatedTiles animatedTiles)
        {
            if (levelData.Background == null)
                return;

            for (int y = 0; y < levelData.Height; y++)
            {
                for (int x = 0; x < levelData.Width; x++)
                {
                    string tileName = levelData.Background[y, x];
                    if (string.IsNullOrEmpty(tileName) || tileName == "-" || tileName == "antichest")
                        continue;

                    Sprite sprite = ResolveTileSprite(tileName);
                    if (sprite == null)
                        continue;

                    GameObject tileObj = new GameObject($"bg_{x:D2}_{y:D2}_{tileName}");
                    tileObj.transform.SetParent(parent, false);
                    tileObj.transform.localPosition = new Vector3(x * CellSize, -y * CellSize, 0f);

                    SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = BackgroundSortingOrder;
                    animatedTiles.Register(sr, tileName, x, y);
                }
            }
        }

        private static void BuildTerrain(MutinyLevelData levelData, Transform parent,
            MutinyAnimatedTiles animatedTiles)
        {
            if (levelData.Terrain == null)
                return;

            for (int y = 0; y < levelData.Height; y++)
            {
                for (int x = 0; x < levelData.Width; x++)
                {
                    string tileName = levelData.Terrain[y, x];
                    if (string.IsNullOrEmpty(tileName) || tileName == "-")
                        continue;

                    Sprite sprite = ResolveTileSprite(tileName);
                    if (sprite == null)
                        continue;

                    GameObject tileObj = new GameObject($"tile_{x:D2}_{y:D2}_{tileName}");
                    tileObj.transform.SetParent(parent, false);
                    tileObj.transform.localPosition = new Vector3(x * CellSize, -y * CellSize, 0f);

                    SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = TerrainSortingOrder;
                    animatedTiles.Register(sr, tileName, x, y);

                    // Physical collision for solid terrain (exclude water ripples)
                    if (!tileName.Contains("ripple"))
                    {
                        BoxCollider2D collider = tileObj.AddComponent<BoxCollider2D>();
                        collider.size = new Vector2(CellSize, CellSize);
                        collider.offset = new Vector2(CellSize * 0.5f, -CellSize * 0.5f);
                    }
                }
            }
        }

        private static float BuildWater(MutinyLevelData levelData, Transform parent, int skyColour)
        {
            float waterY = -14f; // Default for level 1
            if (levelData.Objects != null)
            {
                foreach (var obj in levelData.Objects)
                {
                    if (obj.Type == "water")
                    {
                        waterY = -obj.Y * CellSize;
                        break;
                    }
                }
            }

            GameObject triggerObj = new GameObject("WaterTrigger");
            triggerObj.transform.SetParent(parent, false);
            triggerObj.transform.localPosition = new Vector3(levelData.Width * 0.5f * CellSize, waterY - 5f, 0f);

            BoxCollider2D collider = triggerObj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(levelData.Width * CellSize + 20f, 10f);

            MutinyWaterSurface surface = parent.gameObject.AddComponent<MutinyWaterSurface>();
            surface.Initialize(levelData.Width * CellSize, waterY, skyColour);

            return waterY;
        }

        private static void BuildObjects(MutinyLevelData levelData, Transform parent, MutinyLevelRoot levelRoot,
            MutinyGameMode? menuMode)
        {
            if (levelData.Objects == null)
                return;

            GameObject team1Group = new GameObject("Team1_Player");
            team1Group.transform.SetParent(parent, false);
            levelRoot.Team1 = team1Group.AddComponent<MutinyTeam>();
            levelRoot.Team1.TeamNumber = 1;
            levelRoot.Team1.IsAiControlled = false;

            GameObject team2Group = new GameObject("Team2_Enemy");
            team2Group.transform.SetParent(parent, false);
            levelRoot.Team2 = team2Group.AddComponent<MutinyTeam>();
            levelRoot.Team2.TeamNumber = 2;
            levelRoot.Team2.IsAiControlled = menuMode.HasValue
                ? menuMode.Value == MutinyGameMode.SinglePlayer
                : levelData.Players == 1;
            if (levelRoot.Team2.IsAiControlled)
            {
                team2Group.AddComponent<MutinyAIController>();
            }

            GameObject otherGroup = new GameObject("Misc");
            otherGroup.transform.SetParent(parent, false);

            int charIndex = 0;
            foreach (var obj in levelData.Objects)
            {
                if (obj.Type == "water")
                    continue;

                if (obj.Type == "potentialWeapons")
                {
                    GameObject weaponPoolObj = new GameObject("PotentialWeapons");
                    weaponPoolObj.transform.SetParent(otherGroup.transform, false);
                    weaponPoolObj.transform.localPosition = new Vector3(obj.X * CellSize, -obj.Y * CellSize, 0f);
                    continue;
                }

                // Character
                int teamIndex = (obj.Type == "redPirate" || obj.Type == "redPirateCaptain") ? 1 : 2;
                Transform teamParent = teamIndex == 1 ? team1Group.transform : team2Group.transform;

                float posX = (obj.X + 0.5f) * CellSize;
                float posY = -(obj.Y + 0.75f) * CellSize;

                GameObject charObj = new GameObject($"Char_{charIndex:D2}_{obj.Type}_T{teamIndex}");
                charObj.transform.SetParent(teamParent, false);
                charObj.transform.localPosition = new Vector3(posX, posY, 0f);

                var character = charObj.AddComponent<MutinyCharacter>();
                character.Initialize(obj.Type, teamIndex, obj.X, obj.Y, obj.Properties);
                levelRoot.Characters.Add(character);

                if (teamIndex == 1 && levelRoot.Team1 != null)
                {
                    levelRoot.Team1.RegisterCharacter(character);
                }
                else if (teamIndex == 2 && levelRoot.Team2 != null)
                {
                    levelRoot.Team2.RegisterCharacter(character);
                }

                // Add visual preview
                Sprite charSprite = ResolveCharacterPreview(obj.Type);
                SpriteRenderer sr = charObj.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    sr = charObj.AddComponent<SpriteRenderer>();
                }
                // TileSystem reads XML objects sequentially. Character's Solid
                // constructor calls characterLayer.getNextHighestDepth(), so a
                // later XML character must be entirely above an earlier one.
                sr.sortingOrder = GetCharacterSortingOrder(charIndex);
                if (charSprite != null)
                {
                    sr.sprite = charSprite;
                }

                MutinyCharacterAnimator characterAnimator = charObj.AddComponent<MutinyCharacterAnimator>();
                characterAnimator.Initialize(obj.Type);
                charObj.AddComponent<MutinyCharacterOverlay>();

                charIndex++;
            }
        }

        public static Sprite ResolveTileSprite(string tileName, int frame = 1)
        {
            MutinyTileCatalog.EnsureInitialized();
            if (!MutinyTileCatalog.TryGetDefinition(tileName, out var def) || def.IsLogic)
                return null;

            string path = def.FrameCount <= 1
                ? $"Art/Tiles/Single/{def.TileName}"
                : $"Art/Tiles/Animated/{def.TileName}/{def.TileName}_{frame:D2}";

            return Resources.Load<Sprite>(path);
        }

        public static Sprite ResolveCharacterPreview(string characterType)
        {
            Texture2D tex = Resources.Load<Texture2D>($"Art/Characters/Preview/{characterType}");
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                    MutinyCharacterAnimator.GetCharacterPivot(characterType), MutinyPhysics.PixelsPerUnit);
            }
            return Resources.Load<Sprite>($"Art/Characters/Preview/{characterType}");
        }
    }

    [DisallowMultipleComponent]
    public sealed class MutinyWaterSurface : MonoBehaviour
    {
        private const int FramesPerColour = 10;
        // Symbol 1651 is exported to 1024 px, but the underwater fill ends at
        // x=980 (20 original 49 px wave periods). The final 44 px are transparent.
        public const int VisibleWidthPixels = 980;
        private const int WaterBackgroundWidthPixels = 550;
        private readonly List<SpriteRenderer> m_Renderers = new List<SpriteRenderer>();
        private Sprite[] m_Frames = Array.Empty<Sprite>();
        private SpriteRenderer m_Backdrop;
        private float m_Accumulator;
        private int m_FrameIndex;

        public int LoadedFrameCount => m_Frames.Length;
        public int CurrentSkyColour { get; private set; } = 1;
        public SpriteRenderer BackdropRenderer => m_Backdrop;

        public void Initialize(float levelWidth, float waterUnityY, int skyColour)
        {
            CurrentSkyColour = Mathf.Clamp(skyColour, 1, 3);
            m_Frames = LoadFrameRange("Art/Effects/Water", (CurrentSkyColour - 1) * FramesPerColour + 1,
                FramesPerColour, new Vector2(0f, 1f - 30f / 384f), VisibleWidthPixels);
            if (m_Frames.Length == 0)
            {
                Debug.LogError("[Mutiny:Water] Original water frames are missing from Resources.", this);
                return;
            }

            CreateOriginalBackdrop(levelWidth, waterUnityY);
            float spriteWidth = m_Frames[0].rect.width / MutinyPhysics.PixelsPerUnit;
            float startX = -spriteWidth;
            int rendererCount = Mathf.CeilToInt((levelWidth + spriteWidth * 2f) / spriteWidth);
            for (int i = 0; i < rendererCount; i++)
            {
                GameObject segment = new GameObject($"WaterSurface_{i:D2}");
                segment.transform.SetParent(transform, false);
                segment.transform.localPosition = new Vector3(startX + i * spriteWidth, waterUnityY, 0f);
                SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
                renderer.sprite = m_Frames[0];
                renderer.sortingOrder = MutinyLevelBuilder.WaterSortingOrder;
                m_Renderers.Add(renderer);
            }

            Mutiny.Diagnostics.MutinyDebugLog.Info("Water",
                $"surface initialized waterY={-waterUnityY * MutinyPhysics.PixelsPerUnit:F0}px sky={CurrentSkyColour} frames={m_Frames.Length} segments={rendererCount}", this);
        }

        private void CreateOriginalBackdrop(float levelWidth, float waterUnityY)
        {
            Texture2D texture = Resources.Load<Texture2D>($"Art/Background/waterBackground{CurrentSkyColour}");
            if (texture == null)
            {
                Debug.LogError("[Mutiny:Water] Original water background is missing from Resources.", this);
                return;
            }

            texture.filterMode = FilterMode.Point;
            GameObject backdrop = new GameObject("WaterBackdrop");
            backdrop.transform.SetParent(transform, false);
            // The original 550x400 background is a single solid colour and follows
            // Water.y. Extend it across the level for views wider than Flash's
            // 550 px stage; the visible game viewport retains the original colour.
            float margin = WaterBackgroundWidthPixels / MutinyPhysics.PixelsPerUnit;
            backdrop.transform.localPosition = new Vector3(-margin, waterUnityY, 0f);
            backdrop.transform.localScale = new Vector3((levelWidth + 2f * margin) / margin, 1f, 1f);
            m_Backdrop = backdrop.AddComponent<SpriteRenderer>();
            m_Backdrop.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0f, 1f), MutinyPhysics.PixelsPerUnit);
            m_Backdrop.sortingOrder = MutinyLevelBuilder.BackgroundSortingOrder - 9;
        }

        private void Update()
        {
            if (m_Frames.Length <= 1)
                return;

            m_Accumulator += Time.deltaTime;
            while (m_Accumulator >= MutinyPhysics.TimeStep)
            {
                m_Accumulator -= MutinyPhysics.TimeStep;
                m_FrameIndex = (m_FrameIndex + 1) % m_Frames.Length;
                for (int i = 0; i < m_Renderers.Count; i++)
                    m_Renderers[i].sprite = m_Frames[m_FrameIndex];
            }
        }

        // Solid.splashCheck compares the object's registration Y, not its bottom
        // extent or the one-shot drowning/contact event. Equality is underwater.
        public static bool CheckSplashCrossing(float pixelX, float pixelY, float waterPixelY,
            ref bool overWater, int skyColour = 0)
        {
            if (float.IsInfinity(waterPixelY) || float.IsNaN(waterPixelY))
                return false;

            bool nowOverWater = pixelY < waterPixelY;
            if (nowOverWater == overWater)
                return false;

            overWater = nowOverWater;
            SpawnSplash(pixelX, waterPixelY, skyColour);
            MutinyAudioManager.Instance?.PlaySFX("splash");
            return true;
        }

        public static void SpawnSplash(float pixelX, float waterPixelY, int skyColour = 0)
        {
            var levelRoot = UnityEngine.Object.FindAnyObjectByType<MutinyLevelRoot>();
            if (float.IsInfinity(waterPixelY) || float.IsNaN(waterPixelY))
            {
                if (levelRoot != null)
                    waterPixelY = -levelRoot.WaterLevelY * MutinyPhysics.PixelsPerUnit;
            }
            if (skyColour == 0)
                skyColour = levelRoot != null ? levelRoot.SkyColour : 1;

            GameObject splash = new GameObject("WaterSplash");
            splash.transform.position = MutinyPhysics.PixelToUnity(pixelX, waterPixelY);
            splash.AddComponent<MutinySplashEffect>().Initialize(skyColour);
        }

        internal static Sprite[] LoadFrameRange(string resourcePath, int firstFrame, int frameCount,
            Vector2 pivot, int visibleWidth = 0)
        {
            var sprites = new List<Sprite>(frameCount);
            for (int i = 0; i < frameCount; i++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"{resourcePath}/{firstFrame + i}");
                if (texture == null)
                    continue;

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                int width = visibleWidth > 0 ? Mathf.Min(visibleWidth, texture.width) : texture.width;
                sprites.Add(Sprite.Create(texture, new Rect(0f, 0f, width, texture.height), pivot,
                    MutinyPhysics.PixelsPerUnit));
            }
            return sprites.ToArray();
        }
    }

    [DisallowMultipleComponent]
    public sealed class MutinySplashEffect : MonoBehaviour
    {
        // The symbol has 24 exported frames per sky colour. At frames 19/43/67
        // cl.destroy() runs before rendering, leaving only the first 18 visible.
        private const int FramesPerColour = 24;
        private const int VisibleFramesPerColour = 18;
        private SpriteRenderer m_Renderer;
        private Sprite[] m_Frames = Array.Empty<Sprite>();
        private float m_Accumulator;
        private int m_FrameIndex;
        public int CurrentSourceFrame { get; private set; }

        public void Initialize(int skyColour)
        {
            m_Frames = MutinyWaterSurface.LoadFrameRange("Art/Effects/Splash",
                (Mathf.Clamp(skyColour, 1, 3) - 1) * FramesPerColour + 1,
                VisibleFramesPerColour, new Vector2(0.5f, 0f));
            if (m_Frames.Length == 0)
            {
                Debug.LogError("[Mutiny:Water] Original splash frames are missing from Resources.", this);
                Destroy(gameObject);
                return;
            }

            m_Renderer = gameObject.AddComponent<SpriteRenderer>();
            m_Renderer.sortingOrder = MutinyLevelBuilder.SplashSortingOrder;
            m_Renderer.sprite = m_Frames[0];
            CurrentSourceFrame = (Mathf.Clamp(skyColour, 1, 3) - 1) * FramesPerColour + 1;
        }

        private void Update()
        {
            if (m_Frames.Length == 0)
                return;

            m_Accumulator += Time.deltaTime;
            AdvanceFrames();
        }

        private void AdvanceFrames()
        {
            while (m_Accumulator >= MutinyPhysics.TimeStep)
            {
                m_Accumulator -= MutinyPhysics.TimeStep;
                m_FrameIndex++;
                if (m_FrameIndex >= m_Frames.Length)
                {
                    // Flash destroys the clip on its 19th tick. Hide it now,
                    // since Unity defers Destroy until the end of the frame.
                    m_Renderer.enabled = false;
                    Destroy(gameObject);
                    return;
                }
                m_Renderer.sprite = m_Frames[m_FrameIndex];
                CurrentSourceFrame++;
            }
        }

        public void AdvanceOriginalTickForVerification()
        {
            if (m_Frames.Length == 0 || m_Renderer == null || !m_Renderer.enabled)
                return;
            m_Accumulator += MutinyPhysics.TimeStep;
            AdvanceFrames();
        }
    }
}
