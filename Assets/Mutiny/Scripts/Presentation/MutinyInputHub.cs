using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mutiny.Presentation
{
    /// <summary>One controller sample per frame; never synthesizes a Mouse or IMGUI event.</summary>
    [DefaultExecutionOrder(-20000)]
    [DisallowMultipleComponent]
    public sealed class MutinyInputHub : MonoBehaviour
    {
        public readonly struct ControllerFrame
        {
            public readonly Vector2 Stick;
            public readonly Vector2 Pan;
            public readonly Vector2 Navigate;
            public readonly float IncreasePower;
            public readonly float DecreasePower;
            public readonly float DeltaTime;
            public readonly bool Confirm;
            public readonly bool Back;
            public readonly bool Menu;

            public ControllerFrame(Vector2 stick, Vector2 pan, Vector2 navigate,
                float increasePower, float decreasePower, float deltaTime,
                bool confirm, bool back, bool menu)
            {
                Stick = stick;
                Pan = pan;
                Navigate = navigate;
                IncreasePower = increasePower;
                DecreasePower = decreasePower;
                DeltaTime = deltaTime;
                Confirm = confirm;
                Back = back;
                Menu = menu;
            }
        }

        public static MutinyInputHub Instance { get; private set; }
        public bool IsControllerActive { get; private set; }
        public Vector2 PointerPosition { get; private set; }
        public ControllerFrame Frame { get; private set; }
        public string CapturedContext { get; private set; }
        public bool IsAwaitingNeutral => m_NeedsNeutral;
        public bool IsConfirmHeld => IsControllerActive && m_Confirm != null && m_Confirm.IsPressed();
        public float PointerPixelsPerSecond = 260f;
        public const float TriggerDeadzone = 0.08f;

        private InputActionAsset m_Actions;
        private InputAction m_Stick, m_Pan, m_Navigate, m_IncreasePower, m_DecreasePower, m_Confirm, m_Back, m_Menu;
        private Gamepad m_Pad;
        private Vector2 m_PreviousMouse;
        private Vector2 m_LastNavigation;
        private float m_NextNavigationTime;
        private bool m_NeedsNeutral;
        private bool m_HasFocus = true;
        private bool m_GameplayConsumed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null)
                new GameObject("MutinyInputHub").AddComponent<MutinyInputHub>();
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
            PointerPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (Mouse.current != null)
                m_PreviousMouse = Mouse.current.position.ReadValue();
            InputActionAsset source = Resources.Load<InputActionAsset>("Input/MutinyController");
            if (source == null)
            {
                Debug.LogError("[Mutiny Controller] Missing Input/MutinyController action asset.", this);
                enabled = false;
                return;
            }
            m_Actions = Instantiate(source);
            m_Stick = m_Actions.FindAction("LeftStick", true);
            m_Pan = m_Actions.FindAction("Pan", true);
            m_Navigate = m_Actions.FindAction("Navigate", true);
            m_IncreasePower = m_Actions.FindAction("IncreasePower", true);
            m_DecreasePower = m_Actions.FindAction("DecreasePower", true);
            m_Confirm = m_Actions.FindAction("Confirm", true);
            m_Back = m_Actions.FindAction("Back", true);
            m_Menu = m_Actions.FindAction("Menu", true);
            m_Actions.Enable();
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void Update() => CaptureInput(Time.unscaledDeltaTime);

        internal void CaptureInput(float deltaTime)
        {
            Frame = default;
            m_GameplayConsumed = false;
            MutinyControllerUI.ClearActivation();
            if (m_Actions == null || !m_HasFocus)
                return;

            MutinyPlayerInput player = FindAnyObjectByType<MutinyPlayerInput>();
            bool controllerAim = player != null && player.IsControllerAiming;
            bool pointerGesture = (player != null && player.IsAiming && !controllerAim) ||
                                  (!IsControllerActive && IsPointerHeld());
            Vector2 stick = m_Stick.ReadValue<Vector2>();
            Vector2 pan = m_Pan.ReadValue<Vector2>();
            Vector2 navigation = m_Navigate.ReadValue<Vector2>();
            float increasePower = Mathf.Clamp01(m_IncreasePower.ReadValue<float>());
            float decreasePower = Mathf.Clamp01(m_DecreasePower.ReadValue<float>());
            bool confirm = m_Confirm.WasPressedThisFrame();
            bool back = m_Back.WasPressedThisFrame();
            bool menu = m_Menu.WasPressedThisFrame();
            bool meaningful = stick.sqrMagnitude > 0.01f || pan.sqrMagnitude > 0.01f ||
                              navigation.sqrMagnitude > 0.25f || increasePower > TriggerDeadzone ||
                              decreasePower > TriggerDeadzone ||
                              confirm || back || menu;
            bool pointerIntent = ReadDesktopOrTouchIntent();

            if (m_NeedsNeutral)
            {
                if (!meaningful && !m_Confirm.IsPressed() && !m_Back.IsPressed() && !m_Menu.IsPressed())
                    m_NeedsNeutral = false;
                return;
            }
            if (!controllerAim && pointerIntent)
            {
                IsControllerActive = false;
                m_Pad = null;
                m_Actions.devices = null;
                MutinyControllerUI.ClearFocus();
                return;
            }
            if (!pointerGesture && meaningful && Gamepad.current != null)
            {
                if (!IsControllerActive)
                {
                    m_Pad = m_Confirm.activeControl?.device as Gamepad ??
                            m_Stick.activeControl?.device as Gamepad ??
                            m_IncreasePower.activeControl?.device as Gamepad ??
                            m_DecreasePower.activeControl?.device as Gamepad ?? Gamepad.current;
                    m_Actions.devices = new InputDevice[] { m_Pad };
                }
                IsControllerActive = true;
            }
            if (!IsControllerActive)
                return;

            // Establish the turn's character focus before routing A or moving
            // the pointer, including the first frame that activates a gamepad.
            player?.PrepareControllerCharacterFocus(this);
            string context = CurrentContext;
            if (CapturedContext != context)
                MutinyControllerUI.ClearFocus();
            CapturedContext = context;
            Vector2 step = NavigationStep(navigation);
            Frame = new ControllerFrame(stick, pan, step, increasePower, decreasePower,
                Mathf.Max(0f, deltaTime), confirm, back, menu);
            if (!controllerAim && !(player != null && player.IsControllerCannonReady) &&
                stick.sqrMagnitude > 0.0001f)
            {
                float scale = Mathf.Min(Screen.width / 550f, Screen.height / 400f);
                PointerPosition += stick * (PointerPixelsPerSecond * scale * deltaTime);
                ClampPointer();
                MutinyControllerUI.ClearFocus();
            }

            if (CapturedContext == "blocked")
            {
                player?.CancelControllerAim();
                Frame = default;
                return;
            }
            MutinyGameHUD hud = FindAnyObjectByType<MutinyGameHUD>();
            if (menu && hud != null && hud.TryOpenControllerMenu())
            {
                player?.CancelControllerAim();
                m_GameplayConsumed = true;
                return;
            }
            if (CapturedContext == "speech")
            {
                player?.CancelControllerAim();
                if (confirm && !back)
                    hud?.Speech?.Click();
                m_GameplayConsumed = true;
                return;
            }
            if (CapturedContext != "board")
            {
                MutinyControllerUI.RouteFrame(this, step, confirm && !back, back);
                m_GameplayConsumed = true;
            }
            else if (!controllerAim && confirm && !back &&
                !(player != null && player.InteractionState == MutinyPlayerInteractionState.CharacterSelection &&
                  player.ControllerFocusedCharacter != null) && MutinyControllerUI.RouteBoardPointer(this))
                m_GameplayConsumed = true;
        }

        internal bool TryConsumeGameplay(out ControllerFrame frame)
        {
            frame = Frame;
            if (!IsControllerActive || m_GameplayConsumed || CapturedContext != CurrentContext ||
                CapturedContext != "board")
                return false;
            m_GameplayConsumed = true;
            return true;
        }

        public string CurrentContext
        {
            get
            {
                if (MutinyTransitionManager.IsTransitionActive ||
                    (FindAnyObjectByType<MutinyGMManager>()?.IsOpen ?? false))
                    return "blocked";
                MutinyFrontendController frontend = FindAnyObjectByType<MutinyFrontendController>();
                if (frontend != null && frontend.CurrentPage != MutinyFrontendPage.Gameplay)
                    return "front:" + frontend.CurrentPage;
                MutinyGameHUD hud = FindAnyObjectByType<MutinyGameHUD>();
                if (hud != null)
                {
                    if (hud.IsQuitPromptShowRequested || hud.IsQuitPromptVisible) return "quit";
                    if (hud.Speech != null && hud.Speech.HasActiveBubble) return "speech";
                    if (hud.TurnManager != null && hud.TurnManager.CurrentPhase == Simulation.TurnPhase.GameOver)
                        return "result";
                    if (hud.PlayerInput != null && hud.PlayerInput.IsActionMenuOpen)
                        return "actions:" + hud.PlayerInput.GetInstanceID();
                }
                return "board";
            }
        }

        private Vector2 NavigationStep(Vector2 value)
        {
            Vector2 direction = Vector2.zero;
            if (value.sqrMagnitude > 0.25f)
                direction = Mathf.Abs(value.x) >= Mathf.Abs(value.y)
                    ? new Vector2(Mathf.Sign(value.x), 0f) : new Vector2(0f, Mathf.Sign(value.y));
            if (direction == Vector2.zero)
            {
                m_LastNavigation = Vector2.zero;
                return Vector2.zero;
            }
            float now = Time.unscaledTime;
            if (direction != m_LastNavigation)
            {
                m_LastNavigation = direction;
                m_NextNavigationTime = now + 0.35f;
                return direction;
            }
            if (now < m_NextNavigationTime) return Vector2.zero;
            m_NextNavigationTime = now + 0.12f;
            return direction;
        }

        private bool ReadDesktopOrTouchIntent()
        {
            bool intent = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 position = mouse.position.ReadValue();
                intent |= (position - m_PreviousMouse).sqrMagnitude > 4f ||
                          mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame;
                m_PreviousMouse = position;
            }
            Touchscreen touch = Touchscreen.current;
            if (touch != null)
                foreach (var contact in touch.touches)
                    intent |= contact.press.wasPressedThisFrame;
            return intent;
        }

        private static bool IsPointerHeld()
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            if (Touchscreen.current != null)
                foreach (var contact in Touchscreen.current.touches)
                    if (contact.press.isPressed) return true;
            return false;
        }

        internal void SetPointerFromGui(Vector2 screenTopLeft)
        {
            PointerPosition = new Vector2(screenTopLeft.x, Screen.height - screenTopLeft.y);
            ClampPointer();
        }

        private void ClampPointer()
        {
            Rect viewport = MutinyCameraController.CalculateViewportRect(Screen.width, Screen.height);
            PointerPosition = new Vector2(
                Mathf.Clamp(PointerPosition.x, viewport.xMin * Screen.width, viewport.xMax * Screen.width),
                Mathf.Clamp(PointerPosition.y, viewport.yMin * Screen.height, viewport.yMax * Screen.height));
        }

        public static Vector2 GuiPointerPosition
        {
            get
            {
                if (Instance == null || !Instance.IsControllerActive)
                    return Event.current != null ? Event.current.mousePosition : Vector2.zero;
                Vector2 point = new Vector2(Instance.PointerPosition.x, Screen.height - Instance.PointerPosition.y);
                return GUI.matrix.inverse.MultiplyPoint3x4(point);
            }
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device == m_Pad && (change == InputDeviceChange.Disconnected ||
                change == InputDeviceChange.Removed || change == InputDeviceChange.Disabled))
                ResetDeviceOwnership();
        }

        private void ResetDeviceOwnership()
        {
            FindAnyObjectByType<MutinyPlayerInput>()?.CancelControllerAim();
            Frame = default;
            IsControllerActive = false;
            m_Pad = null;
            if (m_Actions != null) m_Actions.devices = null;
            m_NeedsNeutral = true;
            m_LastNavigation = Vector2.zero;
            MutinyControllerUI.ClearFocus();
            MutinyControllerUI.ClearActivation();
        }

        private void OnApplicationFocus(bool focused)
        {
            m_HasFocus = focused;
            if (!focused) ResetDeviceOwnership();
        }

        private void OnGUI()
        {
            if (!IsControllerActive || CurrentContext == "blocked" || Event.current.type != EventType.Repaint)
                return;
            MutinyPlayerInput player = FindAnyObjectByType<MutinyPlayerInput>();
            if (player != null && (player.IsControllerAiming || player.HasVisibleSpecialCursor)) return;
            Matrix4x4 matrix = GUI.matrix;
            int depth = GUI.depth;
            Color color = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.depth = -20000;
            GUI.color = new Color(1f, 0.88f, 0.25f);
            float scale = Mathf.Min(Screen.width / 550f, Screen.height / 400f);
            Vector2 p = new Vector2(PointerPosition.x, Screen.height - PointerPosition.y);
            GUI.DrawTexture(new Rect(p.x - 5f * scale, p.y - scale, 10f * scale, 2f * scale), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(p.x - scale, p.y - 5f * scale, 2f * scale, 10f * scale), Texture2D.whiteTexture);
            GUI.color = color;
            GUI.depth = depth;
            GUI.matrix = matrix;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            InputSystem.onDeviceChange -= OnDeviceChange;
            if (m_Actions != null)
            {
                m_Actions.Disable();
                Destroy(m_Actions);
            }
            Instance = null;
            MutinyControllerUI.Clear();
        }
    }
}
