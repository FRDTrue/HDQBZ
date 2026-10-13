using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mutiny.Verification.Editor
{
    [InitializeOnLoad]
    public static class MutinyParityValidationMenu
    {
        private const string CameraMovementVerificationKey = "Mutiny.CameraMovementPlayModeVerification";
        private const string CameraInitializationVerificationKey = "Mutiny.CameraInitializationPlayModeVerification";
        private const string SeagullPresentationVerificationKey = "Mutiny.SeagullPresentationPlayModeVerification";
        private const string BottomNoticesVerificationKey = "Mutiny.BottomNoticesPlayModeVerification";
        private const string AirDropPresentationVerificationKey = "Mutiny.AirDropPresentationPlayModeVerification";
        private const string CannonPresentationVerificationKey = "Mutiny.CannonPresentationPlayModeVerification";
        private const string AiBoxCameraVerificationKey = "Mutiny.AiBoxCameraPlayModeVerification";
        private const string ExplosionCameraVerificationKey = "Mutiny.ExplosionCameraPlayModeVerification";
        private const string CannonEffectsVerificationKey = "Mutiny.CannonEffectsPlayModeVerification";
        private const string WeaponLifecycleVerificationKey = "Mutiny.WeaponLifecyclePlayModeVerification";
        private const string ScrollArrowVerificationKey = "Mutiny.ScrollArrowPlayModeVerification";
        private const string LevelLifecycleVerificationKey = "Mutiny.LevelLifecyclePlayModeVerification";
        private const string AnchorAnimationVerificationKey = "Mutiny.AnchorAnimationPlayModeVerification";
        private const string PiecesOfEightPresentationVerificationKey = "Mutiny.PiecesOfEightPresentationPlayModeVerification";
        private const string AimCancelTouchVerificationKey = "Mutiny.AimCancelTouchPlayModeVerification";
        private const string CharacterAimOverlayVerificationKey = "Mutiny.CharacterAimOverlayPlayModeVerification";
        private const string MobileGesturesVerificationKey = "Mutiny.MobileGesturesPlayModeVerification";

        static MutinyParityValidationMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;

            bool verifyCamera = SessionState.GetBool(CameraMovementVerificationKey, false);
            bool verifyCameraInitialization = SessionState.GetBool(CameraInitializationVerificationKey, false);
            bool verifySeagullPresentation = SessionState.GetBool(SeagullPresentationVerificationKey, false);
            bool verifyBottomNotices = SessionState.GetBool(BottomNoticesVerificationKey, false);
            bool verifyAirDropPresentation = SessionState.GetBool(AirDropPresentationVerificationKey, false);
            bool verifyCannonPresentation = SessionState.GetBool(CannonPresentationVerificationKey, false);
            bool verifyAiBoxCamera = SessionState.GetBool(AiBoxCameraVerificationKey, false);
            bool verifyExplosionCamera = SessionState.GetBool(ExplosionCameraVerificationKey, false);
            bool verifyCannon = SessionState.GetBool(CannonEffectsVerificationKey, false);
            bool verifyWeaponLifecycle = SessionState.GetBool(WeaponLifecycleVerificationKey, false);
            bool verifyScrollArrow = SessionState.GetBool(ScrollArrowVerificationKey, false);
            bool verifyLevelLifecycle = SessionState.GetBool(LevelLifecycleVerificationKey, false);
            bool verifyAnchorAnimation = SessionState.GetBool(AnchorAnimationVerificationKey, false);
            bool verifyPiecesOfEightPresentation = SessionState.GetBool(PiecesOfEightPresentationVerificationKey, false);
            bool verifyAimCancelTouch = SessionState.GetBool(AimCancelTouchVerificationKey, false);
            bool verifyCharacterAimOverlay = SessionState.GetBool(CharacterAimOverlayVerificationKey, false);
            bool verifyMobileGestures = SessionState.GetBool(MobileGesturesVerificationKey, false);
            if (!verifyExplosionCamera && !verifyAiBoxCamera && !verifyCannonPresentation && !verifyAirDropPresentation && !verifyBottomNotices && !verifySeagullPresentation && !verifyCamera && !verifyCameraInitialization && !verifyCannon && !verifyWeaponLifecycle && !verifyScrollArrow &&
                !verifyLevelLifecycle && !verifyAnchorAnimation && !verifyPiecesOfEightPresentation &&
                !verifyAimCancelTouch && !verifyCharacterAimOverlay && !verifyMobileGestures)
                return;

            SessionState.EraseBool(CameraMovementVerificationKey);
            SessionState.EraseBool(CameraInitializationVerificationKey);
            SessionState.EraseBool(SeagullPresentationVerificationKey);
            SessionState.EraseBool(BottomNoticesVerificationKey);
            SessionState.EraseBool(AirDropPresentationVerificationKey);
            SessionState.EraseBool(CannonPresentationVerificationKey);
            SessionState.EraseBool(AiBoxCameraVerificationKey);
            SessionState.EraseBool(ExplosionCameraVerificationKey);
            SessionState.EraseBool(CannonEffectsVerificationKey);
            SessionState.EraseBool(WeaponLifecycleVerificationKey);
            SessionState.EraseBool(ScrollArrowVerificationKey);
            SessionState.EraseBool(LevelLifecycleVerificationKey);
            SessionState.EraseBool(AnchorAnimationVerificationKey);
            SessionState.EraseBool(PiecesOfEightPresentationVerificationKey);
            SessionState.EraseBool(AimCancelTouchVerificationKey);
            SessionState.EraseBool(CharacterAimOverlayVerificationKey);
            SessionState.EraseBool(MobileGesturesVerificationKey);
            bool passed = false;
            try
            {
                MutinyLevel1VerificationResult result =
                    verifyMobileGestures ? MutinyTurnActionUiVerificationTest.RunMobileGestures() :
                    verifyExplosionCamera ? MutinyTurnActionUiVerificationTest.RunExplosionCamera() :
                    verifyAiBoxCamera ? MutinyTurnActionUiVerificationTest.RunAiBoxCamera() :
                    verifyCannonPresentation ? MutinyTurnActionUiVerificationTest.RunCannonPresentation() :
                    verifyAirDropPresentation ? MutinyTurnActionUiVerificationTest.RunAirDropPresentation() :
                    verifyBottomNotices ? MutinyTurnActionUiVerificationTest.RunBottomNotices() :
                    verifySeagullPresentation ? MutinyTurnActionUiVerificationTest.RunSeagullPresentation() :
                    verifyCameraInitialization ? MutinyTurnActionUiVerificationTest.RunCameraInitialization() :
                    verifyCharacterAimOverlay ? MutinyTurnActionUiVerificationTest.RunCharacterAimOverlay() :
                    verifyAimCancelTouch ? MutinyTurnActionUiVerificationTest.RunAimCancelTouch() :
                    verifyPiecesOfEightPresentation ? MutinyTurnActionUiVerificationTest.RunPiecesOfEightPresentation() :
                    verifyAnchorAnimation ? MutinyTurnActionUiVerificationTest.RunAnchorAnimation() :
                    verifyLevelLifecycle ? MutinyTurnActionUiVerificationTest.RunLevelLifecycle() :
                    verifyScrollArrow ? MutinyTurnActionUiVerificationTest.RunScrollArrows() :
                    verifyCannon ? MutinyTurnActionUiVerificationTest.RunCannonSmokeTrail() :
                        verifyWeaponLifecycle ? MutinyTurnActionUiVerificationTest.RunWeaponLifecycle() :
                            MutinyTurnActionUiVerificationTest.RunCameraMovement();
                passed = result.Passed;
                string label = verifyMobileGestures ? "Mobile gestures" :
                    verifyExplosionCamera ? "Explosion camera" :
                    verifyAiBoxCamera ? "AI box camera" :
                    verifyCannonPresentation ? "Cannon presentation" :
                    verifyAirDropPresentation ? "Air-drop presentation" :
                    verifyBottomNotices ? "Bottom notices" :
                    verifySeagullPresentation ? "Seagull presentation" :
                    verifyCameraInitialization ? "Camera initialization" :
                    verifyCharacterAimOverlay ? "Character aim overlay" :
                    verifyAimCancelTouch ? "Aim cancel touch" :
                    verifyPiecesOfEightPresentation ? "Pieces of Eight presentation" :
                    verifyAnchorAnimation ? "Anchor animation" :
                    verifyLevelLifecycle ? "Level lifecycle" :
                    verifyScrollArrow ? "Scroll arrows" :
                    verifyCannon ? "Cannon effects" :
                    verifyWeaponLifecycle ? "Weapon lifecycle" : "Camera movement";
                if (passed)
                    Debug.Log($"[Mutiny Parity] {label} Play Mode verification passed: {result.PassedAssertions}/{result.TotalAssertions} assertions.");
                else
                    Debug.LogError($"[Mutiny Parity] {label} Play Mode verification failed:\n" +
                                   string.Join("\n", result.Failures));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            if (Application.isBatchMode)
                EditorApplication.Exit(passed ? 0 : 1);
            else
                EditorApplication.ExitPlaymode();
        }

        private static readonly string[] CharacterTypes =
        {
            "blindPirate", "blindPirateCaptain", "bluePirate", "bluePirateCaptain",
            "bossGuy", "bossGuyZombie", "cabinBoy", "cabinBoyCaptain", "crab",
            "femalePirate", "femalePirateCaptain", "monkey", "oldPirate",
            "oldPirateCaptain", "parrot", "rainbowBeard", "rainbowBeardCaptain",
            "redPirate", "redPirateCaptain", "shark", "skeletonPirate",
            "skeletonPirateCaptain", "soldier", "soldierCaptain", "squid", "tribe",
            "tribeChief"
        };

        [MenuItem("Mutiny/Parity/Validate Runtime Resources")]
        public static void ValidateRuntimeResources()
        {
            var failures = new List<string>();
            for (int level = 1; level <= 18; level++)
            {
                string path = $"Data/Levels/level_{level:D2}";
                if (Resources.Load<TextAsset>(path) == null)
                    failures.Add(path);
            }

            if (Resources.Load<TextAsset>("Data/Tiles/tile-mapping") == null)
                failures.Add("Data/Tiles/tile-mapping");
            if (Resources.LoadAll<AudioClip>("Audio/SFX").Length != 37)
                failures.Add("Audio/SFX (expected 37 clips)");
            if (Resources.LoadAll<AudioClip>("Audio/Music").Length != 2)
                failures.Add("Audio/Music (expected 2 clips)");

            for (int i = 0; i < CharacterTypes.Length; i++)
            {
                string path = $"Art/Characters/Animations/{CharacterTypes[i]}";
                if (Resources.LoadAll<Texture2D>(path).Length != 35)
                    failures.Add($"{path} (expected 35 frames)");
            }

            if (Resources.LoadAll<Texture2D>("Art/Objects/TreasureChest").Length != 90)
                failures.Add("Art/Objects/TreasureChest (expected 90 frames)");

            if (failures.Count == 0)
                Debug.Log("[Mutiny Parity] Runtime resources validated: 18 levels, tile catalog, 39 audio clips, 945 character frames and 90 chest frames.");
            else
                Debug.LogError("[Mutiny Parity] Missing runtime resources:\n" + string.Join("\n", failures));
        }

        [MenuItem("Mutiny/Parity/Validate Turn Action UI")]
        public static void ValidateTurnActionUi()
        {
            MutinyLevel1VerificationResult result = MutinyTurnActionUiVerificationTest.Run();
            if (result.Passed)
                Debug.Log($"[Mutiny Parity] Turn action UI verification passed: {result.PassedAssertions}/{result.TotalAssertions} assertions.");
            else
                Debug.LogError("[Mutiny Parity] Turn action UI verification failed:\n" +
                               string.Join("\n", result.Failures));
        }

        [MenuItem("Mutiny/Parity/Validate Cannon Smoke Trail")]
        public static void ValidateCannonSmokeTrail()
        {
            MutinyLevel1VerificationResult result =
                MutinyTurnActionUiVerificationTest.RunCannonSmokeTrail();
            if (result.Passed)
                Debug.Log($"[Mutiny Parity] Cannon smoke trail verification passed: {result.PassedAssertions}/{result.TotalAssertions} assertions.");
            else
                Debug.LogError("[Mutiny Parity] Cannon smoke trail verification failed:\n" +
                               string.Join("\n", result.Failures));
        }

        [MenuItem("Mutiny/Parity/Validate Level Lifecycle Play Mode")]
        public static void ValidateLevelLifecyclePlayMode()
        {
            SessionState.SetBool(LevelLifecycleVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Anchor Animation Play Mode")]
        public static void ValidateAnchorAnimationPlayMode()
        {
            SessionState.SetBool(AnchorAnimationVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Two Player Mode")]
        public static void ValidateTwoPlayerMode()
        {
            MutinyLevel1VerificationResult result = MutinyTwoPlayerVerificationTest.Run();
            if (result.Passed)
                Debug.Log($"[Mutiny Parity] Two-player mode verification passed: {result.PassedAssertions}/{result.TotalAssertions} assertions.");
            else
                Debug.LogError("[Mutiny Parity] Two-player mode verification failed:\n" +
                               string.Join("\n", result.Failures));
        }

        [MenuItem("Mutiny/Parity/Validate Cannon Effects Play Mode")]
        public static void ValidateCannonEffectsPlayMode()
        {
            SessionState.SetBool(CannonEffectsVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Scroll Arrows Play Mode")]
        public static void ValidateScrollArrowsPlayMode()
        {
            SessionState.SetBool(ScrollArrowVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Weapon Lifecycle Play Mode")]
        public static void ValidateWeaponLifecyclePlayMode()
        {
            SessionState.SetBool(WeaponLifecycleVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Pieces of Eight Presentation Play Mode")]
        public static void ValidatePiecesOfEightPresentationPlayMode()
        {
            SessionState.SetBool(PiecesOfEightPresentationVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate GM Commands")]
        public static void ValidateGMCommands()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Mutiny Parity] Enter Play Mode before validating GM commands; weapon components require Awake.");
                return;
            }
            MutinyLevel1VerificationResult result = MutinyTurnActionUiVerificationTest.RunGM();
            if (result.Passed)
                Debug.Log($"[Mutiny Parity] GM command verification passed: {result.PassedAssertions}/{result.TotalAssertions} assertions.");
            else
                Debug.LogError("[Mutiny Parity] GM command verification failed:\n" +
                               string.Join("\n", result.Failures));
        }

        [MenuItem("Mutiny/Parity/Validate Camera Movement")]
        public static void ValidateCameraMovement()
        {
            SessionState.SetBool(CameraMovementVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Weapon Idle Animations")]
        public static void ValidateWeaponIdleAnimations()
        {
            MutinyLevel1VerificationResult result =
                MutinyTurnActionUiVerificationTest.RunWeaponIdleAnimations();
            if (result.Passed)
                Debug.Log($"[Mutiny Parity] Weapon idle animation verification passed: {result.PassedAssertions}/{result.TotalAssertions} assertions.");
            else
                Debug.LogError("[Mutiny Parity] Weapon idle animation verification failed:\n" +
                               string.Join("\n", result.Failures));
        }

        [MenuItem("Mutiny/Parity/Validate Battle HUD")]
        public static void ValidateBattleHud()
        {
            MutinyLevel1VerificationResult result = MutinyTurnActionUiVerificationTest.RunBattleHud();
            if (result.Passed)
                Debug.Log($"[Mutiny Parity] Battle HUD verification passed: {result.PassedAssertions}/{result.TotalAssertions} assertions.");
            else
                Debug.LogError("[Mutiny Parity] Battle HUD verification failed:\n" +
                               string.Join("\n", result.Failures));
        }

        [MenuItem("Mutiny/Parity/Validate Weapon Ready And Cancel")]
        public static void ValidateWeaponReadyAndCancel()
        {
            MutinyLevel1VerificationResult result =
                MutinyTurnActionUiVerificationTest.RunWeaponReadyAndCancel();
            if (result.Passed)
                Debug.Log($"[Mutiny Parity] Weapon ready/cancel verification passed: {result.PassedAssertions}/{result.TotalAssertions} assertions.");
            else
                Debug.LogError("[Mutiny Parity] Weapon ready/cancel verification failed:\n" +
                               string.Join("\n", result.Failures));
        }

        [MenuItem("Mutiny/Parity/Validate Mobile Gestures Play Mode")]
        public static void ValidateMobileGesturesPlayMode()
        {
            SessionState.SetBool(MobileGesturesVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Aim Cancel Touch Play Mode")]
        public static void ValidateAimCancelTouchPlayMode()
        {
            SessionState.SetBool(AimCancelTouchVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Camera Initialization Play Mode")]
        public static void ValidateCameraInitializationPlayMode()
        {
            SessionState.SetBool(CameraInitializationVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Seagull Presentation Play Mode")]
        public static void ValidateSeagullPresentationPlayMode()
        {
            SessionState.SetBool(SeagullPresentationVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Bottom Notices Play Mode")]
        public static void ValidateBottomNoticesPlayMode()
        {
            SessionState.SetBool(BottomNoticesVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Air Drop Presentation Play Mode")]
        public static void ValidateAirDropPresentationPlayMode()
        {
            SessionState.SetBool(AirDropPresentationVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Cannon Presentation Play Mode")]
        public static void ValidateCannonPresentationPlayMode()
        {
            SessionState.SetBool(CannonPresentationVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate AI Box Camera Play Mode")]
        public static void ValidateAiBoxCameraPlayMode()
        {
            SessionState.SetBool(AiBoxCameraVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Explosion Camera Play Mode")]
        public static void ValidateExplosionCameraPlayMode()
        {
            SessionState.SetBool(ExplosionCameraVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Mutiny/Parity/Validate Character Aim Overlay Play Mode")]
        public static void ValidateCharacterAimOverlayPlayMode()
        {
            SessionState.SetBool(CharacterAimOverlayVerificationKey, true);
            if (EditorApplication.isPlaying)
                OnPlayModeStateChanged(PlayModeStateChange.EnteredPlayMode);
            else
                EditorApplication.EnterPlaymode();
        }
    }
}
