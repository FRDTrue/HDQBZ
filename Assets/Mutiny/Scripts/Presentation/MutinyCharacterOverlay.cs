using System;
using Mutiny.Diagnostics;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Presentation
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MutinyCharacter))]
    public sealed class MutinyCharacterOverlay : MonoBehaviour
    {
        private const float PixelsPerUnit = MutinyPhysics.PixelsPerUnit;
        private const int HealthSegments = 27;
        internal const float OriginalIndicatorTopY = -38.05f;
        internal const float OriginalHealthCenterY = 18f;
        internal const float OriginalCancelWeaponCenterY = 33f;

        private MutinyCharacter m_Character;
        private MutinyTeam m_Team;
        private MutinySpeechController m_Speech;
        private GameObject m_OverlayRoot;
        private GameObject m_Indicator;
        private SpriteRenderer m_IndicatorRenderer;
        private Transform m_HealthFill;
        private SpriteRenderer m_HealthFillRenderer;
        private SpriteRenderer m_HealthBarBackgroundRenderer;
        private int m_LastCharacterSortingOrder = -1;
        private GameObject m_HealthBar;
        private GameObject m_SelectionCorners;
        private GameObject m_VoodooTarget;
        private GameObject m_CancelWeapon;
        private SpriteRenderer m_CancelWeaponRenderer;
        private bool m_LastIndicatorVisible;
        private bool m_LastHealthVisible;
        private int m_LastHealthSegments = -1;
        private string m_IndicatorResource;
        private string m_CancelWeaponResource;

        private static Sprite s_WhitePixel;

        public bool IsTurnIndicatorVisible => m_Indicator != null && m_Indicator.activeInHierarchy;
        public bool IsHealthBarVisible => m_HealthBar != null && m_HealthBar.activeInHierarchy;
        public bool IsSelectionCornersVisible => m_SelectionCorners != null && m_SelectionCorners.activeInHierarchy;
        public bool IsVoodooTargetVisible => m_VoodooTarget != null && m_VoodooTarget.activeInHierarchy;
        public bool IsCancelWeaponVisible => m_CancelWeapon != null && m_CancelWeapon.activeInHierarchy;
        public int CurrentHealthFrame => 1 + Mathf.Max(0, m_LastHealthSegments);

        private static Sprite WhitePixel
        {
            get
            {
                if (s_WhitePixel == null)
                {
                    s_WhitePixel = Sprite.Create(Texture2D.whiteTexture,
                        new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), PixelsPerUnit);
                }
                return s_WhitePixel;
            }
        }

        private void Awake()
        {
            m_Character = GetComponent<MutinyCharacter>();
            CreateOverlayUI();
        }

        private void OnEnable()
        {
            if (m_Character != null)
            {
                m_Character.OnHealthChanged += UpdateHealthBar;
                m_Character.OnDeath += OnCharacterDeath;
            }
        }

        private void OnDisable()
        {
            if (m_Character != null)
            {
                m_Character.OnHealthChanged -= UpdateHealthBar;
                m_Character.OnDeath -= OnCharacterDeath;
            }
        }

        private void LateUpdate()
        {
            KeepOverlayUpright();

            SpriteRenderer characterRenderer = m_Character != null ? m_Character.GetComponent<SpriteRenderer>() : null;
            int currentCharacterSortingOrder = characterRenderer != null ? characterRenderer.sortingOrder : -1;
            if (currentCharacterSortingOrder != m_LastCharacterSortingOrder)
            {
                m_LastCharacterSortingOrder = currentCharacterSortingOrder;
                ApplyHealthBarSortingOrder();
            }

            if (m_Character == null || !m_Character.IsAlive)
            {
                SetOverlayVisible(false);
                return;
            }

            ResolveTeam();
            bool isCurrentTeam = m_Team != null &&
                                 FindAnyObjectByType<MutinyTurnManager>()?.CurrentTeam == m_Team;
            bool isSelfThrown = m_Character.IsSelfThrown;

            // Character is twangable, not draggable (Character.as constructor).
            // Aiming maps to Controller.twanging, which updateOverlay never
            // suppresses. Hide only after the self-throw has actually committed;
            // passive physics motion does not set Character.thrown either.
            if (m_Speech == null)
                m_Speech = GetComponentInParent<MutinySpeechController>();
            bool hideForCharacterAction = isSelfThrown;
            bool showIndicator = isCurrentTeam && !hideForCharacterAction &&
                                 (m_Speech == null || !m_Speech.HasActiveBubble || m_Speech.Speaker != m_Character);
            bool showHealth = !hideForCharacterAction;
            MutinyVoodooDoll armedDoll = FindAnyObjectByType<MutinyPlayerInput>()?.ArmedVoodooDoll;
            bool isVoodooTargeting = armedDoll != null;
            bool showVoodooTarget = isVoodooTargeting &&
                                    (armedDoll.TargetCharacter == m_Character ||
                                     (armedDoll.TargetCharacter == null && m_Character.IsHovered));
            MutinyPlayerInput playerInput = FindAnyObjectByType<MutinyPlayerInput>();
            bool showCancelWeapon = playerInput != null && playerInput.ShouldShowCancelWeapon(m_Character);
            SetActive(m_Indicator, showIndicator, ref m_LastIndicatorVisible, "indicator");
            SetActive(m_HealthBar, showHealth, ref m_LastHealthVisible, "health");

            if (m_VoodooTarget != null && m_VoodooTarget.activeSelf != showVoodooTarget)
                m_VoodooTarget.SetActive(showVoodooTarget);
            if (m_CancelWeapon != null && m_CancelWeapon.activeSelf != showCancelWeapon)
            {
                m_CancelWeapon.SetActive(showCancelWeapon);
                MutinyDebugLog.Info("Overlay",
                    $"cancel weapon visible={showCancelWeapon} character={m_Character.name}", this);
            }

            if (m_SelectionCorners != null)
            {
                // Character.updateOverlay forces corners off for every character
                // while the selected player has an unfired Voodoo Doll.
                bool showCorners = !isVoodooTargeting &&
                                   (m_Character.IsSelected || m_Character.IsHovered) && !hideForCharacterAction;
                if (m_SelectionCorners.activeSelf != showCorners)
                    m_SelectionCorners.SetActive(showCorners);
            }

            UpdateIndicatorSprite();
            UpdateCancelWeaponSprite();
            UpdateHealthBar();
        }

        public void RefreshVisualStateForVerification()
        {
            LateUpdate();
        }

        private void KeepOverlayUpright()
        {
            if (m_OverlayRoot == null)
                return;

            // Flash Clip.update() moves mcHolder but applies rotation only to
            // mc._rotation. characterOverlay is attached to the unrotated
            // mcHolder, so it follows position without inheriting character spin.
            m_OverlayRoot.transform.rotation = Quaternion.identity;
        }

        public static int CalculateOriginalHealthFrame(float shownHealth, float maxHealth)
        {
            return 1 + Mathf.Clamp(Mathf.CeilToInt(HealthSegments * shownHealth / Mathf.Max(1f, maxHealth)),
                0, HealthSegments);
        }

        private void ResolveTeam()
        {
            if (m_Team != null && m_Team.Characters.Contains(m_Character))
                return;

            MutinyTeam[] teams = FindObjectsByType<MutinyTeam>(FindObjectsSortMode.None);
            for (int i = 0; i < teams.Length; i++)
            {
                if (teams[i] != null && teams[i].Characters.Contains(m_Character))
                {
                    m_Team = teams[i];
                    return;
                }
            }
        }

        private void CreateOverlayUI()
        {
            int overlaySortingOrder = ResolveOverlaySortingOrder();
            m_OverlayRoot = new GameObject("OriginalCharacterOverlay");
            m_OverlayRoot.transform.SetParent(transform, false);
            // Character.show() attaches characterOverlay directly to mcHolder at
            // (0, 0). The (0.5, -2.4) placement in the SWF root is only the
            // library-preview instance and must not be applied at runtime.
            m_OverlayRoot.transform.localPosition = Vector3.zero;

            m_Indicator = new GameObject("TurnIndicator");
            m_Indicator.transform.SetParent(m_OverlayRoot.transform, false);
            // triangle is placed at y=-761 twips and its artwork begins at that
            // registration point. The sprite therefore uses a top-centre pivot.
            m_Indicator.transform.localPosition = new Vector3(0f, -OriginalIndicatorTopY / PixelsPerUnit, 0f);
            m_IndicatorRenderer = m_Indicator.AddComponent<SpriteRenderer>();
            m_IndicatorRenderer.sortingOrder = overlaySortingOrder + 2;
            m_Indicator.SetActive(false);

            m_HealthBar = new GameObject("HealthBar");
            m_HealthBar.transform.SetParent(m_OverlayRoot.transform, false);
            // characterOverlay.health is centred at source y=360 twips (+18px).
            m_HealthBar.transform.localPosition = new Vector3(0f, -OriginalHealthCenterY / PixelsPerUnit, 0f);
            var barBackground = new GameObject("OriginalFrame");
            barBackground.transform.SetParent(m_HealthBar.transform, false);
            m_HealthBarBackgroundRenderer = barBackground.AddComponent<SpriteRenderer>();
            m_HealthBarBackgroundRenderer.sprite = LoadSprite("UI/CharacterOverlay/health_background", new Vector2(0.5f, 0.5f));

            var fill = new GameObject("DiscreteFill");
            fill.transform.SetParent(m_HealthBar.transform, false);
            fill.transform.localPosition = new Vector3(-13f / PixelsPerUnit, 0f, 0f);
            m_HealthFill = fill.transform;
            m_HealthFillRenderer = fill.AddComponent<SpriteRenderer>();
            m_HealthFillRenderer.sprite = WhitePixel;
            ApplyHealthBarSortingOrder();

            m_VoodooTarget = new GameObject("VoodooTarget");
            m_VoodooTarget.transform.SetParent(m_OverlayRoot.transform, false);
            // characterOverlay.target matrix is translateX=-20 twips: -1 px.
            m_VoodooTarget.transform.localPosition = new Vector3(-1f / PixelsPerUnit, 0f, 0f);
            SpriteRenderer targetRenderer = m_VoodooTarget.AddComponent<SpriteRenderer>();
            // symbol 1871 spans x=-20..21 px and y=-21..21 px.  Its x
            // registration is therefore pixel 20 of a 41px export, not 20.5.
            targetRenderer.sprite = LoadSprite("UI/CharacterOverlay/voodoo_target", new Vector2(20f / 41f, 0.5f));
            targetRenderer.sortingOrder = overlaySortingOrder + 4;
            m_VoodooTarget.SetActive(false);

            m_CancelWeapon = new GameObject("CancelWeapon");
            m_CancelWeapon.transform.SetParent(m_OverlayRoot.transform, false);
            m_CancelWeapon.transform.localPosition =
                new Vector3(0f, -OriginalCancelWeaponCenterY / PixelsPerUnit, 0f);
            m_CancelWeaponRenderer = m_CancelWeapon.AddComponent<SpriteRenderer>();
            // This is UI artwork, so keep its transparent perimeter independent
            // of the scene's default lit sprite material and 2D lighting passes.
            m_CancelWeaponRenderer.sharedMaterial =
                Resources.Load<Material>("UI/cancel_button_transparent");
            m_CancelWeaponRenderer.sortingOrder = overlaySortingOrder + 5;
            m_CancelWeapon.SetActive(false);

            CreateSelectionCorners();
            UpdateIndicatorSprite();
            UpdateHealthBar();
        }

        private void UpdateCancelWeaponSprite()
        {
            if (m_CancelWeaponRenderer == null)
                return;

            string resource = m_Character.TeamIndex == 2
                ? "UI/button_cancel_blue"
                : "UI/button_cancel_red";
            if (string.Equals(m_CancelWeaponResource, resource, StringComparison.Ordinal))
                return;

            Sprite sprite = LoadSprite(resource, new Vector2(0.5f, 0.5f));
            if (sprite != null)
            {
                m_CancelWeaponRenderer.sprite = sprite;
                m_CancelWeaponResource = resource;
            }
        }

        private void UpdateIndicatorSprite()
        {
            if (m_IndicatorRenderer == null)
                return;

            string resource = m_Team != null && m_Team.IsAiControlled
                ? "UI/CharacterOverlay/cpu_indicator"
                : m_Character.TeamIndex == 2
                    ? "UI/CharacterOverlay/p2_indicator"
                    : "UI/CharacterOverlay/p1_indicator";
            if (string.Equals(m_IndicatorResource, resource, StringComparison.Ordinal))
                return;

            Sprite sprite = LoadSprite(resource, new Vector2(0.5f, 1f));
            if (sprite != null)
            {
                m_IndicatorRenderer.sprite = sprite;
                m_IndicatorResource = resource;
            }
        }

        private void UpdateHealthBar()
        {
            if (m_Character == null || m_HealthFill == null || m_HealthFillRenderer == null)
                return;

            float maximum = Mathf.Max(1f, m_Character.MaxHealth);
            int segments = CalculateOriginalHealthFrame(m_Character.ShownHealth, maximum) - 1;
            if (segments != m_LastHealthSegments)
            {
                // The Flash health movie has one initial empty state plus 27
                // discrete morph positions. Scaling in whole source pixels keeps
                // its change cadence tied to shownHealth's 25 Hz progression.
                m_HealthFill.localScale = new Vector3(segments, 4f, 1f);
                m_LastHealthSegments = segments;
                MutinyDebugLog.Info("Overlay",
                    $"health frame character={m_Character.name} frame={1 + segments} shown={m_Character.ShownHealth:0} health={m_Character.Health:0}", this);
            }

            // health (1864) is red; the p2 overlay substitutes the blue health movie (1877).
            m_HealthFillRenderer.color = m_Character.TeamIndex == 2
                ? new Color32(51, 95, 255, 255)
                : new Color32(230, 49, 19, 255);
        }

        private static Sprite LoadSprite(string resourcePath, Vector2 pivot)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, PixelsPerUnit);
        }

        private void CreateSelectionCorners()
        {
            m_SelectionCorners = new GameObject("SelectionCorners");
            m_SelectionCorners.transform.SetParent(m_OverlayRoot.transform, false);
            m_SelectionCorners.transform.localPosition = Vector3.zero;
            CreateCorner("TopLeft", new Vector2(-14f, 14f), new Vector2(1f, -1f));
            CreateCorner("TopRight", new Vector2(14f, 14f), new Vector2(-1f, -1f));
            CreateCorner("BottomLeft", new Vector2(-14f, -14f), new Vector2(1f, 1f));
            CreateCorner("BottomRight", new Vector2(14f, -14f), new Vector2(-1f, 1f));
            m_SelectionCorners.SetActive(false);
        }

        private void CreateCorner(string cornerName, Vector2 pixelCorner, Vector2 direction)
        {
            GameObject cornerObject = new GameObject(cornerName);
            cornerObject.transform.SetParent(m_SelectionCorners.transform, false);
            LineRenderer line = cornerObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 3;
            line.startWidth = 2f / PixelsPerUnit;
            line.endWidth = 2f / PixelsPerUnit;
            line.sortingOrder = ResolveOverlaySortingOrder() + 3;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = Color.white;
            line.endColor = Color.white;
            const float length = 6f;
            line.SetPosition(0, ToUnityPixel(pixelCorner + new Vector2(0f, direction.y * length)));
            line.SetPosition(1, ToUnityPixel(pixelCorner));
            line.SetPosition(2, ToUnityPixel(pixelCorner + new Vector2(direction.x * length, 0f)));
        }

        private static Vector3 ToUnityPixel(Vector2 sourcePixel)
        {
            return new Vector3(sourcePixel.x / PixelsPerUnit, -sourcePixel.y / PixelsPerUnit, 0f);
        }

        private int ResolveOverlaySortingOrder()
        {
            SpriteRenderer characterRenderer = m_Character != null
                ? m_Character.GetComponent<SpriteRenderer>()
                : null;
            int characterOrder = characterRenderer != null
                ? characterRenderer.sortingOrder
                : Mutiny.Levels.MutinyLevelBuilder.CharacterSortingOrder;
            return characterOrder + Mutiny.Levels.MutinyLevelBuilder.CharacterOverlaySortingOffset;
        }

        private void ApplyHealthBarSortingOrder()
        {
            int baseOrder = ResolveHealthBarSortingOrder();
            if (m_HealthBarBackgroundRenderer != null)
                m_HealthBarBackgroundRenderer.sortingOrder = baseOrder;
            if (m_HealthFillRenderer != null)
                m_HealthFillRenderer.sortingOrder = baseOrder + 1;
        }

        private int ResolveHealthBarSortingOrder()
        {
            SpriteRenderer characterRenderer = m_Character != null
                ? m_Character.GetComponent<SpriteRenderer>()
                : null;
            int charIndex = 0;
            if (characterRenderer != null && characterRenderer.sortingOrder >= Mutiny.Levels.MutinyLevelBuilder.CharacterSortingOrder)
            {
                charIndex = (characterRenderer.sortingOrder - Mutiny.Levels.MutinyLevelBuilder.CharacterSortingOrder) / Mutiny.Levels.MutinyLevelBuilder.CharacterSortingStride;
            }
            return Mutiny.Levels.MutinyLevelBuilder.GetCharacterHealthBarSortingOrder(charIndex);
        }

        private void SetActive(GameObject target, bool active, ref bool previous, string label)
        {
            if (target == null || previous == active)
                return;
            target.SetActive(active);
            previous = active;
            MutinyDebugLog.Info("Overlay", $"{label} visible={active} character={m_Character.name}", this);
        }

        private void SetOverlayVisible(bool visible)
        {
            if (m_OverlayRoot != null && m_OverlayRoot.activeSelf != visible)
                m_OverlayRoot.SetActive(visible);
        }

        private void OnCharacterDeath()
        {
            SetOverlayVisible(false);
            MutinyDebugLog.Info("Overlay", $"hidden on death character={m_Character.name}", this);
        }
    }
}
