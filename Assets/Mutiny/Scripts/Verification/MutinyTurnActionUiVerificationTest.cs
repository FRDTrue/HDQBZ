using System.Collections.Generic;
using Mutiny.Levels;
using Mutiny.Persistence;
using Mutiny.Presentation;
using Mutiny.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Mutiny.Verification
{
    public static class MutinyTurnActionUiVerificationTest
    {
        private static readonly string[] ThrowButtonResources =
        {
            "UI/button_throw_disabled",
            "UI/button_throw_red_up",
            "UI/button_throw_red_over",
            "UI/button_throw_blue_up",
            "UI/button_throw_blue_over"
        };

        private static readonly string[] WeaponSlotResources =
        {
            "UI/weapon_slot_disabled",
            "UI/weapon_slot_red_up",
            "UI/weapon_slot_blue_up",
            "UI/weapon_slot_over"
        };

        private static readonly string[] EndTurnButtonResources =
        {
            "UI/button_end_turn_red_up",
            "UI/button_end_turn_red_over",
            "UI/button_end_turn_blue_up",
            "UI/button_end_turn_blue_over"
        };

        private static readonly string[] WeaponIconResources =
        {
            "cherryBomb", "boulder", "dynamite", "piecesOfEight", "rumBottle",
            "banana", "parachuteBomb", "woodenCrate", "gunpowderBarrel", "seagull",
            "mine", "cannon", "anchor", "voodooDoll", "tidalWave"
        };

        public static MutinyLevel1VerificationResult Run()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyVisualStateMapping(result);
            VerifyPanelTiming(result);
            VerifyRuntimeResources(result);
            VerifyOriginalWaterSurface(result);
            VerifyOriginalPanelCopy(result);
            VerifyOriginalRotation(result);
            VerifyCharacterCollisionAudio(result);
            VerifyCharacterThrowAudio(result);
            VerifyAirDrops(result);
            VerifyAirDropPresentation(result);
            VerifyTurnNotice(result);
            VerifyLandDeath(result);
            VerifyCharacterTimeline(result);
            VerifyFrontendFlow(result);
            VerifyAndroidAdaptation(result);
            VerifyScrollArrows(result);
            VerifyBattleHud(result);
            VerifyCornerLevelControls(result);
            VerifyMutedMusicToggleStartsRequestedTrack(result);
            VerifyGameEndPopup(result);
            VerifyEndingSequence(result);
            VerifyWeaponReadyAndCancel(result);
            VerifyCancelCrossPressOnly(result);
            VerifyProductionActionMethods(result);
            VerifyWeaponVelocityLimits(result);
            VerifyOriginalSplashTimeline(result);
            VerifyCherryBombWaterSplash(result);
            VerifyAiDecisionFlow(result);
            VerifyAiSpecialWeaponCandidates(result);
            VerifyAiOriginalPredictionParity(result);
            VerifyWeaponIdleAnimations(result);
            VerifyCannonSmokeTrail(result);
            VerifyCannonImpactEffects(result);
            VerifyCannon(result);
            VerifyCannonPresentation(result);
            VerifyAiBoxCamera(result);
            VerifyExplosionCamera(result);
            VerifyAiCannonTurnSettlement(result);
            VerifyBoulder(result);
            VerifyBanana(result);
            VerifyBananaLongFlight(result);
            VerifySharedWeaponLifecycle(result);
            VerifyParachuteBomb(result);
            VerifyPiecesOfEight(result);
            VerifyPiecesOfEightPresentation(result);
            VerifyRumBottle(result);
            VerifyMineCameraAndLevelCleanup(result);
            VerifySeagull(result);
            VerifySeagullPresentation(result);
            VerifyTidalWave(result);
            VerifyLongRunningWaveTurn(result);
            VerifyVoodooDoll(result);
            VerifyGunpowderBarrel(result);
            VerifyWoodenCrate(result);
            VerifyAnchor(result);
            VerifyCharacterLayering(result);
            VerifyCharacterOverlay(result);
            VerifyCharacterAimOverlay(result);
            VerifyGMManager(result);
            VerifySpritePivots(result);
            VerifyScreenTransitions(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunCannonSmokeTrail()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyCannonSmokeTrail(result);
            VerifyCannonImpactEffects(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunEndingSequence()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyEndingSequence(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunLevelLifecycle()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyGameEndPopup(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunAnchorAnimation()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyAnchor(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunWeaponLifecycle()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyBananaLongFlight(result);
            VerifySharedWeaponLifecycle(result);
            VerifyLongRunningWaveTurn(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunPiecesOfEightPresentation()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyPiecesOfEightPresentation(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunGM()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyGMManager(result, includeLegacyChecks: false);
            return result;
        }

        public static MutinyLevel1VerificationResult RunGMLanguage(MutinyGMManager gm)
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyGMSetLanguage(gm, result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunWeaponIdleAnimations()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyWeaponIdleAnimations(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunCameraMovement()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyCameraMovement(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunSeagullPresentation()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifySeagull(result);
            VerifySeagullPresentation(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunCannon()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyCannon(result);
            VerifyAiCannonTurnSettlement(result);
            VerifyCannonSmokeTrail(result);
            VerifyCannonImpactEffects(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunCannonPresentation()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyCannonPresentation(result);
            VerifyCannon(result);
            VerifyAiCannonTurnSettlement(result);
            VerifyCannonSmokeTrail(result);
            VerifyCannonImpactEffects(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunAiBoxCamera()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyAiBoxCamera(result);
            VerifyGunpowderBarrel(result);
            VerifyWoodenCrate(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunExplosionCamera()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyExplosionCamera(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunCameraInitialization()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyCameraInitialization(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunScrollArrows()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyScrollArrows(result);
            return result;
        }

        private static void VerifyWeaponVelocityLimits(MutinyLevel1VerificationResult result)
        {
            GameObject ordinaryTwangObject = null;
            GameObject directFireObject = null;
            GameObject specialDirectFireObject = null;
            try
            {
                ordinaryTwangObject = new GameObject("WeaponVelocityVerification_OrdinaryTwang");
                MutinyCherryBomb ordinaryTwang = ordinaryTwangObject.AddComponent<MutinyCherryBomb>();
                ordinaryTwang.Initialize(null);
                ordinaryTwang.Twang(Vector2.zero, new Vector2(-200f, 0f));

                directFireObject = new GameObject("WeaponVelocityVerification_DirectFire");
                MutinyCherryBomb directFire = directFireObject.AddComponent<MutinyCherryBomb>();
                directFire.Initialize(null);
                directFire.Fire(new Vector2(27f, -36f));

                specialDirectFireObject = new GameObject("WeaponVelocityVerification_SpecialDirectFire");
                MutinyBanana specialDirectFire = specialDirectFireObject.AddComponent<MutinyBanana>();
                specialDirectFire.Initialize(null);
                specialDirectFire.Fire(new Vector2(36f, -27f));

                result.Assert(Mathf.Approximately(ordinaryTwang.PhysicsBody.State.VelocityX, 20f) &&
                              Mathf.Approximately(ordinaryTwang.PhysicsBody.State.VelocityY, 0f) &&
                              Mathf.Approximately(MutinyWeaponFactory.GetTwangMaxForce("banana"), 30f) &&
                              Mathf.Approximately(MutinyWeaponFactory.GetTwangMaxForce("parachuteBomb"), 30f) &&
                              Mathf.Approximately(MutinyWeaponFactory.GetTwangMaxForce("rumBottle"), 30f) &&
                              Mathf.Approximately(MutinyWeaponFactory.GetTwangMaxForce("voodooDoll"), 20f),
                    "WPN-VEL-01 production twang limits ordinary weapons to 20 and preserves the three original 30-force exceptions");
                result.Assert(Mathf.Approximately(directFire.PhysicsBody.State.VelocityX, 27f) &&
                              Mathf.Approximately(directFire.PhysicsBody.State.VelocityY, -36f) &&
                              Mathf.Approximately(specialDirectFire.PhysicsBody.State.VelocityX, 36f) &&
                              Mathf.Approximately(specialDirectFire.PhysicsBody.State.VelocityY, -27f),
                    "WPN-VEL-02 production Weapon.fire preserves supplied velocities above the twang limit");
            }
            finally
            {
                DestroyNow(specialDirectFireObject);
                DestroyNow(directFireObject);
                DestroyNow(ordinaryTwangObject);
            }
        }

        private static void VerifyWeaponIdleAnimations(MutinyLevel1VerificationResult result)
        {
            GameObject dynamiteObject = new GameObject("IdleAnimationVerification_Dynamite");
            try
            {
                MutinyDynamite dynamite = dynamiteObject.AddComponent<MutinyDynamite>();
                if (dynamite.PhysicsBody == null)
                    dynamite.SendMessage("Awake");
                dynamite.Initialize(null);
                dynamite.PhysicsBody.SetTerrain(new string[32, 32], 32, 32);
                PhysicsBodyState state = dynamite.PhysicsBody.State;
                state.X = 320f;
                state.Y = 192f;
                state.Weight = 0f;
                dynamite.PhysicsBody.State = state;

                bool startsReadyAndLit = !dynamite.IsFired && dynamite.IsLit &&
                                         dynamite.LitFrames != null &&
                                         dynamite.LitFrames.Length == MutinyDynamite.OriginalLitVisibleFrameCount &&
                                         dynamite.CurrentAnimationFrame == 1 &&
                                         dynamite.SpriteRenderer.sprite == dynamite.LitFrames[0];

                int[] expectedFrames = { 2, 3, 4, 1 };
                bool loopMatches = true;
                for (int i = 0; i < expectedFrames.Length; i++)
                {
                    dynamite.PhysicsBody.AdvanceSimulationTick();
                    loopMatches &= !dynamite.IsFired &&
                                   dynamite.CurrentAnimationFrame == expectedFrames[i] &&
                                   dynamite.SpriteRenderer.sprite == dynamite.LitFrames[expectedFrames[i] - 1];
                }
                result.Assert(startsReadyAndLit && loopMatches,
                    "DYN-ANI-01 production ready-state ticks loop visible lit frames 1-4 without presenting action frame 5");

                dynamite.PhysicsBody.WaterPixelY = 100f;
                state = dynamite.PhysicsBody.State;
                state.Y = 101f;
                dynamite.PhysicsBody.State = state;
                dynamite.PhysicsBody.EvaluateWaterState();
                Sprite unlitSprite = dynamite.UnlitFrame;
                dynamite.PhysicsBody.AdvanceSimulationTick();
                result.Assert(!dynamite.IsLit &&
                              dynamite.CurrentAnimationFrame == MutinyDynamite.OriginalUnlitFrame &&
                              unlitSprite != null && dynamite.SpriteRenderer.sprite == unlitSprite,
                    "DYN-ANI-02 production water entry stops ready/fired animation on original unlit frame 6");
            }
            finally
            {
                DestroyNow(dynamiteObject);
                DestroySmokeTrails();
            }
        }

        private static void VerifyCannonSmokeTrail(MutinyLevel1VerificationResult result)
        {
            MutinyRumBottleSmokeTrail preview = null;
            try
            {
                bool resourcesMatch = true;
                for (int frame = 1; frame <= MutinyRumBottleSmokeTrail.OriginalFrameCount; frame++)
                {
                    Texture2D texture = Resources.Load<Texture2D>($"Art/Effects/CannonSmokeTrail/{frame}");
                    resourcesMatch &= texture != null && texture.width == 17 && texture.height == 17;
                }
                result.Assert(resourcesMatch,
                    "VIS-SMOKE-01 original cannonSmokeTrail has all 19 exported 17x17 frames");

                Vector2 spawnPixel = new Vector2(320f, 192f);
                preview = MutinyRumBottleSmokeTrail.Spawn(spawnPixel);
                if (preview != null && preview.GetComponent<SpriteRenderer>() == null)
                    preview.SendMessage("Awake");
                SpriteRenderer renderer = preview != null ? preview.GetComponent<SpriteRenderer>() : null;
                Vector3 stationaryPosition = preview != null ? preview.transform.position : Vector3.zero;
                bool startsOnFrameOne = preview != null && renderer != null && renderer.sprite != null &&
                                        preview.CurrentFrame == 1 && preview.FrameCount == 19 &&
                                        renderer.sortingOrder == MutinyRumBottleSmokeTrail.SmokeSortingOrder &&
                                        Vector2.Distance(renderer.sprite.pivot,
                                            new Vector2(8f, 9f)) < 0.01f;
                for (int tick = 1; tick < 18 && preview != null; tick++)
                    preview.AdvanceOriginalTickForVerification();
                bool reachesLastVisibleFrame = preview != null && preview.CurrentFrame == 18 &&
                                               !preview.IsComplete && preview.transform.position == stationaryPosition;
                preview?.AdvanceOriginalTickForVerification();
                result.Assert(startsOnFrameOne && reachesLastVisibleFrame &&
                              preview != null && preview.IsComplete && renderer != null && !renderer.enabled,
                    "VIS-SMOKE-01 production trail stays at each sampled path point, shows frames 1-18, and executes destroy on frame 19");

                DestroySmokeTrails();
                result.Assert(WeaponTickCreatesSmoke<MutinyCannonball>(true),
                    "VIS-SMOKE-01 production cannonball physics tick emits the original trail");
                result.Assert(WeaponTickCreatesSmoke<MutinyCherryBomb>(true),
                    "VIS-SMOKE-01 production cherry bomb physics tick emits the original trail");
                result.Assert(WeaponTickCreatesSmoke<MutinyDynamite>(true),
                    "VIS-SMOKE-01 production dynamite physics tick emits the original trail");
                result.Assert(WeaponTickCreatesSmoke<MutinyRumBottle>(true),
                    "VIS-SMOKE-01 production rum bottle physics tick emits the original trail");
                result.Assert(WeaponTickCreatesSmoke<MutinyParachuteBomb>(true),
                    "VIS-SMOKE-01 production closed parachute bomb physics tick emits the original trail");
                result.Assert(WeaponTickCreatesSmoke<MutinyParachuteBomb>(true, openParachute: true),
                    "VIS-SMOKE-01 production parachute bomb stops emitting smoke when its chute opens");
                DestroySmokeTrails();
                result.Assert(WeaponTickCreatesSmoke<MutinyCherryBomb>(false),
                    "VIS-SMOKE-01 equipped cherry bomb emits smoke before firing");
                result.Assert(WeaponTickCreatesSmoke<MutinyDynamite>(false),
                    "VIS-SMOKE-01 equipped dynamite emits smoke before firing");
                result.Assert(WeaponTickCreatesSmoke<MutinyRumBottle>(false),
                    "VIS-SMOKE-01 equipped rum bottle emits smoke before firing");
                result.Assert(WeaponTickCreatesSmoke<MutinyParachuteBomb>(false),
                    "VIS-SMOKE-01 equipped closed parachute bomb emits smoke before firing");

                result.Assert(WeaponSmokeFollowsVisibleTrajectory<MutinyCherryBomb>(true) &&
                              WeaponSmokeFollowsVisibleTrajectory<MutinyCherryBomb>(false),
                    "VIS-SMOKE-02 cherry bomb flight and ready smoke starts at its visible pose, then trails behind");
                result.Assert(WeaponSmokeFollowsVisibleTrajectory<MutinyDynamite>(true) &&
                              WeaponSmokeFollowsVisibleTrajectory<MutinyDynamite>(false),
                    "VIS-SMOKE-02 dynamite flight and ready smoke starts at its visible pose, then trails behind");
                result.Assert(WeaponSmokeFollowsVisibleTrajectory<MutinyRumBottle>(true) &&
                              WeaponSmokeFollowsVisibleTrajectory<MutinyRumBottle>(false),
                    "VIS-SMOKE-02 rum bottle flight and ready smoke starts at its visible pose, then trails behind");
                result.Assert(WeaponSmokeFollowsVisibleTrajectory<MutinyParachuteBomb>(true) &&
                              WeaponSmokeFollowsVisibleTrajectory<MutinyParachuteBomb>(false),
                    "VIS-SMOKE-02 closed parachute bomb flight and ready smoke starts at its visible pose, then trails behind");
            }
            finally
            {
                if (preview != null)
                    DestroyNow(preview.gameObject);
                DestroySmokeTrails();
            }
        }

        private static void VerifyCannonImpactEffects(MutinyLevel1VerificationResult result)
        {
            GameObject ballObject = null;
            GameObject targetObject = null;
            GameObject teamObject = null;
            MutinyAudioManager audio = MutinyAudioManager.Instance;
            bool previousSfxEnabled = audio.SfxEnabled;
            var playedSounds = new List<string>();
            System.Action<string> recordSfx = sound => playedSounds.Add(sound);
            try
            {
                DestroySmokeTrails();
                audio.SfxEnabled = true;
                audio.SfxPlayed += recordSfx;

                ballObject = new GameObject("CannonImpactVerification_Flight");
                MutinyCannonball ball = ballObject.AddComponent<MutinyCannonball>();
                ball.Initialize(null);
                ball.PhysicsBody.SetTerrain(new string[32, 32], 32, 32);
                ball.SetLaunchPosition(new Vector2(320f, 192f));
                ball.Fire(new Vector2(30f, 0f));
                ball.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                ball.PhysicsBody.ApplyPresentationPoseForVerification();
                MutinyRumBottleSmokeTrail[] trails = Object.FindObjectsByType<MutinyRumBottleSmokeTrail>(FindObjectsSortMode.None);
                Vector2 firstPuff = trails.Length == 1
                    ? MutinyPhysics.UnityToPixel(trails[0].transform.position)
                    : Vector2.zero;
                Vector2 firstVisibleBall = MutinyPhysics.UnityToPixel(ball.transform.position);
                bool smokeStartsAtVisibleBall = trails.Length == 1 &&
                    Mathf.Abs(firstPuff.x - 320f) < 0.001f &&
                    Vector2.Distance(firstPuff, firstVisibleBall) < 0.001f &&
                    Mathf.Abs(ball.PhysicsBody.State.X - 350f) < 0.001f;

                ball.PhysicsBody.AdvanceSimulationFrameForVerification(0.02f);
                ball.PhysicsBody.ApplyPresentationPoseForVerification();
                Vector2 halfwayBall = MutinyPhysics.UnityToPixel(ball.transform.position);
                bool halfFrameStaysBehind = Mathf.Abs(halfwayBall.x - 335f) < 0.001f &&
                    firstPuff.x < halfwayBall.x && trails.Length == 1;
                result.Assert(smokeStartsAtVisibleBall && halfFrameStaysBehind,
                    "CAN-SMOKE-02 production cannonball tick places its new puff at the visible ball, not 30 px ahead of it");

                DestroyNow(ballObject);
                ballObject = null;
                DestroySmokeTrails();

                ballObject = new GameObject("CannonImpactVerification_Wall");
                ball = ballObject.AddComponent<MutinyCannonball>();
                ball.Initialize(null);
                string[,] wall = new string[4, 4];
                wall[2, 2] = "solid";
                ball.PhysicsBody.SetTerrain(wall, 4, 4);
                ball.SetLaunchPosition(new Vector2(64f, 48f));
                ball.Fire(new Vector2(0f, 20f));
                playedSounds.Clear();
                ball.PhysicsBody.AdvanceSimulationTick();
                MutinyExplosion[] explosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                bool wallContact = ball.IsFinished && explosions.Length == 1 &&
                    playedSounds.Count == 1 && playedSounds[0] == "pop" &&
                    !explosions[0].PlayPopOnHit;
                if (explosions.Length == 1)
                    explosions[0].ApplyHit();
                ball.Explode(); // A second contact/finish callback must be inert.
                result.Assert(wallContact && playedSounds.Count == 1,
                    "CAN-AUD-03 production wall contact plays one pop; explosion hit and repeated finish add none");
                for (int i = 0; i < explosions.Length; i++)
                    DestroyNow(explosions[i].gameObject);
                DestroyNow(ballObject);
                ballObject = null;
                DestroySmokeTrails();

                teamObject = new GameObject("CannonImpactVerification_TargetTeam");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                targetObject = new GameObject("CannonImpactVerification_Target");
                MutinyCharacter target = targetObject.AddComponent<MutinyCharacter>();
                target.PhysicsBody.State = PhysicsBodyState.CreateDefault(350f, 192f);
                target.PhysicsBody.State.Weight = 0f;
                target.transform.position = MutinyPhysics.PixelToUnity(350f, 192f);
                team.RegisterCharacter(target);

                ballObject = new GameObject("CannonImpactVerification_Character");
                ball = ballObject.AddComponent<MutinyCannonball>();
                ball.Initialize(null);
                ball.PhysicsBody.SetTerrain(new string[32, 32], 32, 32);
                ball.SetLaunchPosition(new Vector2(320f, 192f));
                ball.Fire(new Vector2(30f, 0f));
                playedSounds.Clear();
                ball.PhysicsBody.AdvanceSimulationTick();
                explosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                bool characterContact = ball.IsFinished && explosions.Length == 1 &&
                    playedSounds.Count == 1 && playedSounds[0] == "pop" &&
                    !explosions[0].PlayPopOnHit;
                if (explosions.Length == 1)
                    explosions[0].ApplyHit();
                ball.Explode();
                result.Assert(characterContact && playedSounds.Count == 1,
                    "CAN-AUD-03 production direct character impact plays one pop; explosion hit and repeated finish add none");
                for (int i = 0; i < explosions.Length; i++)
                    DestroyNow(explosions[i].gameObject);
            }
            finally
            {
                audio.SfxPlayed -= recordSfx;
                audio.SfxEnabled = previousSfxEnabled;
                DestroyNow(ballObject);
                DestroyNow(targetObject);
                DestroyNow(teamObject);
                DestroySmokeTrails();
            }
        }

        private static bool WeaponTickCreatesSmoke<T>(bool fire, bool openParachute = false) where T : MutinyWeapon
        {
            GameObject weaponObject = new GameObject($"SmokeVerification_{typeof(T).Name}");
            try
            {
                T weapon = weaponObject.AddComponent<T>();
                if (weapon.PhysicsBody == null)
                    weapon.SendMessage("Awake");
                weapon.Initialize(null);
                weapon.PhysicsBody.SetTerrain(new string[32, 32], 32, 32);
                PhysicsBodyState state = weapon.PhysicsBody.State;
                state.X = 320f;
                state.Y = 192f;
                state.Weight = 0f;
                weapon.PhysicsBody.State = state;
                if (fire)
                {
                    // At vy=-2 the parachute opens before its smoke callback;
                    // keep this case closed to exercise the original smoke gate.
                    Vector2 launchVelocity = weapon is MutinyParachuteBomb && !openParachute
                        ? new Vector2(4f, -20f)
                        : new Vector2(4f, -2f);
                    weapon.Fire(launchVelocity);
                }

                int before = Object.FindObjectsByType<MutinyRumBottleSmokeTrail>(FindObjectsSortMode.None).Length;
                weapon.PhysicsBody.AdvanceSimulationTick();
                int after = Object.FindObjectsByType<MutinyRumBottleSmokeTrail>(FindObjectsSortMode.None).Length;
                int expectedSmokeCount = before + (openParachute ? 0 : 1);
                MutinyParachuteBomb parachute = weapon as MutinyParachuteBomb;
                bool chuteMatches = !openParachute || (parachute != null && parachute.ChuteOpen);
                if (after != expectedSmokeCount || !chuteMatches)
                {
                    string chute = parachute != null ? $" chute={parachute.ChuteOpen}" : string.Empty;
                    Debug.LogError($"[Smoke Verification] {typeof(T).Name} before={before} after={after} expected={expectedSmokeCount} fired={weapon.IsFired} finished={weapon.IsFinished} vy={weapon.PhysicsBody.State.VelocityY}{chute}");
                }
                return after == expectedSmokeCount && chuteMatches;
            }
            finally
            {
                DestroyNow(weaponObject);
            }
        }

        private static void DestroySmokeTrails()
        {
            MutinyRumBottleSmokeTrail[] trails = Object.FindObjectsByType<MutinyRumBottleSmokeTrail>(FindObjectsSortMode.None);
            for (int i = 0; i < trails.Length; i++)
                DestroyNow(trails[i].gameObject);
        }

        private static void VerifyScrollArrows(MutinyLevel1VerificationResult result)
        {
            GameObject levelObject = null;
            GameObject cameraObject = null;
            GameObject cursorObject = null;
            GameObject frontendObject = null;
            GameObject turnObject = null;
            GameObject teamObject = null;
            GameObject inputObject = null;
            Mouse testMouse = null;
            bool previousCursorVisible = Cursor.visible;
            try
            {
                levelObject = new GameObject("ScrollArrowVerification_Level");
                MutinyLevelRoot level = levelObject.AddComponent<MutinyLevelRoot>();
                level.Width = 100;
                level.Height = 100;
                level.WaterLevelY = -80f;
                cameraObject = new GameObject("ScrollArrowVerification_Camera");
                cameraObject.AddComponent<Camera>();
                MutinyCameraController camera = cameraObject.AddComponent<MutinyCameraController>();
                camera.SetLevelRootForVerification(level);

                Texture2D cardinal = camera.ScrollArrowTextureForVerification(false);
                Texture2D diagonal = camera.ScrollArrowTextureForVerification(true);
                result.Assert(cardinal != null && diagonal != null &&
                              cardinal.width == 31 && cardinal.height == 22 &&
                              diagonal.width == 31 && diagonal.height == 22 &&
                              cardinal.filterMode == FilterMode.Point &&
                              diagonal.filterMode == FilterMode.Point,
                    "CUR-SCROLL-01 production camera decodes the original scroll1/scroll2 31x22 pixel-art frames");

                Vector2[] directions =
                {
                    Vector2.right, new Vector2(0f, 1f), Vector2.left, new Vector2(0f, -1f),
                    new Vector2(1f, -1f), new Vector2(1f, 1f),
                    new Vector2(-1f, 1f), new Vector2(-1f, -1f)
                };
                float[] rotations = { 0f, 90f, 180f, 270f, 0f, 90f, 180f, 270f };
                bool matchesOriginalMatrix = true;
                for (int i = 0; i < directions.Length; i++)
                {
                    bool valid = MutinyCameraController.ResolveScrollArrow(
                        directions[i], out bool isDiagonal, out float rotation);
                    matchesOriginalMatrix &= valid && isDiagonal == (i >= 4) &&
                        Mathf.Approximately(rotation, rotations[i]);
                }
                matchesOriginalMatrix &= !MutinyCameraController.ResolveScrollArrow(
                    Vector2.zero, out _, out _);
                result.Assert(matchesOriginalMatrix,
                    "CUR-SCROLL-01 production scroll arrow mapping matches all eight original cursor rotations and clears at zero direction");

                bool left = false, rightEdge = false, down = false, up = false;
                MutinyCameraController.CalculateMouseEdgeScroll(
                    new Vector2(100f, 540f), new Rect(217.5f, 0f, 1485f, 1080f),
                    1920f, 1080f, 108f, ref left, ref rightEdge, ref down, ref up);
                result.Assert(left && !rightEdge && !down && !up,
                    "CAM-EDGE-05 pointer in the left pillarbox continues scrolling left and activates the arrow");

                left = false; rightEdge = false; down = false; up = false;
                MutinyCameraController.CalculateMouseEdgeScroll(
                    new Vector2(1820f, 540f), new Rect(217.5f, 0f, 1485f, 1080f),
                    1920f, 1080f, 108f, ref left, ref rightEdge, ref down, ref up);
                result.Assert(!left && rightEdge && !down && !up,
                    "CAM-EDGE-05 pointer in the right pillarbox continues scrolling right");

                Rect letterboxedViewport = new Rect(0f, 12f, 1024f, 744f);
                left = false; rightEdge = false; down = false; up = false;
                MutinyCameraController.CalculateMouseEdgeScroll(
                    new Vector2(512f, 762f), letterboxedViewport,
                    1024f, 768f, 40f, ref left, ref rightEdge, ref down, ref up);
                bool upperBarScrollsUp = !left && !rightEdge && !down && up;
                left = false; rightEdge = false; down = false; up = false;
                MutinyCameraController.CalculateMouseEdgeScroll(
                    new Vector2(512f, 6f), letterboxedViewport,
                    1024f, 768f, 40f, ref left, ref rightEdge, ref down, ref up);
                result.Assert(upperBarScrollsUp && !left && !rightEdge && down && !up,
                    "CAM-EDGE-05 upper and lower letterbox bars scroll toward their corresponding edges");

                left = false; rightEdge = false; down = false; up = false;
                MutinyCameraController.CalculateMouseEdgeScroll(
                    new Vector2(100f, 1030f), new Rect(200f, 50f, 1520f, 980f),
                    1920f, 1080f, 40f, ref left, ref rightEdge, ref down, ref up);
                result.Assert(left && !rightEdge && !down && up,
                    "CAM-EDGE-05 a black corner combines horizontal and vertical scroll directions");

                left = false; rightEdge = false; down = false; up = false;
                MutinyCameraController.CalculateMouseEdgeScroll(
                    new Vector2(-10f, 540f), new Rect(217.5f, 0f, 1485f, 1080f),
                    1920f, 1080f, 108f, ref left, ref rightEdge, ref down, ref up);
                result.Assert(!left && !rightEdge && !down && !up,
                    "CAM-EDGE-05 pointer outside the game window never scrolls");

                result.Assert(!MutinyCameraController.ShouldDrawScrollArrow(true, Vector2.right) &&
                              !MutinyCameraController.ShouldDrawScrollArrow(true, new Vector2(-1f, 1f)) &&
                              MutinyCameraController.ShouldDrawScrollArrow(false, Vector2.right) &&
                              !MutinyCameraController.ShouldDrawScrollArrow(false, Vector2.zero),
                    "AND-CUR-SCROLL-02 production drawing gate never shows a mobile arrow while preserving desktop manual-scroll arrows");

                cameraObject.transform.position = new Vector3(20f, -20f, -10f);
                camera.ApplyMobileTouchPanForVerification(new Vector2(40f, 20f));
                bool panStillWorks = cameraObject.transform.position.x < 20f &&
                    cameraObject.transform.position.y < -20f;
                cameraObject.transform.position = new Vector3(0f, -20f, -10f);
                camera.ApplyMobileTouchPanForVerification(new Vector2(40f, 0f));
                result.Assert(panStillWorks && Mathf.Approximately(cameraObject.transform.position.x, 0f),
                    "AND-CUR-SCROLL-02 removing mobile arrows preserves production touch pan and level-edge clamping");

                cursorObject = new GameObject("ScrollArrowVerification_SpecialCursor");
                MutinySpecialWeaponCursor specialCursor =
                    cursorObject.AddComponent<MutinySpecialWeaponCursor>();
                specialCursor.SetMode(MutinySpecialWeaponCursor.Mode.None, Vector2.zero);
                Cursor.visible = false; // Camera's production scroll ownership.
                specialCursor.SetMode(MutinySpecialWeaponCursor.Mode.None, Vector2.zero);
                specialCursor.Clear();
                bool repeatedNoneKeepsHidden = !Cursor.visible;
                specialCursor.SetMode(MutinySpecialWeaponCursor.Mode.Anchor, Vector2.zero);
                bool specialModeHidesSystemCursor = !Cursor.visible;
                specialCursor.Clear();
                result.Assert(repeatedNoneKeepsHidden && specialModeHidesSystemCursor &&
                              Cursor.visible,
                    "CUR-SCROLL-02 production cursor mode updates do not re-show the OS cursor every frame, and restore it on a real mode transition");

                MutinyFrontendController frontend = Object.FindAnyObjectByType<MutinyFrontendController>();
                if (frontend == null)
                {
                    frontendObject = new GameObject("ScrollArrowVerification_Frontend");
                    frontend = frontendObject.AddComponent<MutinyFrontendController>();
                }
                turnObject = new GameObject("ScrollArrowVerification_Turn");
                MutinyTurnManager turn = turnObject.AddComponent<MutinyTurnManager>();
                teamObject = new GameObject("ScrollArrowVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                turn.CurrentTeam = team;
                turn.CurrentPhase = TurnPhase.TurnActive;
                inputObject = new GameObject("ScrollArrowVerification_Input");
                MutinyPlayerInput player = inputObject.AddComponent<MutinyPlayerInput>();
                player.TurnManager = turn;
                camera.TurnManager = turn;
                camera.PlayerInput = player;
                testMouse = InputSystem.AddDevice<Mouse>();
                InputState.Change(testMouse.position, new Vector2(1f, Screen.height * 0.5f));
                bool canScrollIfInGameplay = camera.CanUseManualScrollingForVerification() &&
                    MutinyCameraController.ShouldUseMouseEdgeScrolling(false, Mouse.current != null);
                Cursor.visible = false; // Simulate a scroll arrow still owning the cursor at menu entry.
                camera.AdvanceCameraForVerification(MutinyPhysics.TimeStep);
                MutinyCursorManager.EnsureInstance();
                MutinyCursorManager cursorManager = Object.FindAnyObjectByType<MutinyCursorManager>();
                cursorManager.UpdateSystemCursorVisibility();
                result.Assert(frontend.CurrentPage == MutinyFrontendPage.Title &&
                              canScrollIfInGameplay &&
                              camera.DesktopScrollDirectionForVerification == Vector2.zero &&
                              !camera.IsDesktopScrollArrowVisible && Cursor.visible,
                    "CUR-FRONT-01 production title page rejects mouse-edge scrolling and restores the OS cursor despite a retained active turn");
            }
            finally
            {
                if (testMouse != null)
                    InputSystem.RemoveDevice(testMouse);
                DestroyNow(inputObject);
                DestroyNow(teamObject);
                DestroyNow(turnObject);
                DestroyNow(frontendObject);
                DestroyNow(cursorObject);
                DestroyNow(cameraObject);
                DestroyNow(levelObject);
                Cursor.visible = previousCursorVisible;
            }
        }

        /// <summary>
        /// Two-finger pinch zoom: the pure ratio/bounds math plus the production
        /// gesture path driving a real camera fixture. The mobile-only gate lives
        /// on the touch layer, so the scripted path drives the same production
        /// zoom application the device pinch uses.
        /// </summary>
        private static void VerifyMobilePinchZoom(MutinyLevel1VerificationResult result)
        {
            result.Assert(
                Mathf.Approximately(MutinyCameraController.ClampZoomScale(0.1f),
                    MutinyCameraController.MinimumZoomScale) &&
                Mathf.Approximately(MutinyCameraController.ClampZoomScale(99f),
                    MutinyCameraController.MaximumZoomScale) &&
                Mathf.Approximately(MutinyCameraController.ClampZoomScale(1.25f), 1.25f),
                "PINCH-ZOOM-01 zoom scale clamps to the 0.6x-2.0x range");

            result.Assert(
                Mathf.Approximately(MutinyCameraController.ResolveOrthographicSize(1f), 6.25f) &&
                Mathf.Approximately(MutinyCameraController.ResolveOrthographicSize(2f), 3.125f) &&
                Mathf.Approximately(MutinyCameraController.ResolveOrthographicSize(0.5f), 12.5f),
                "PINCH-ZOOM-02 zoom maps onto the 6.25 orthographic size of the authored 550x400 view");

            result.Assert(
                !MutinyPlayerInput.ShouldStartPinch(6f, 6f) &&
                MutinyPlayerInput.ShouldStartPinch(6.01f, 6f) &&
                MutinyPlayerInput.ShouldStartPinch(-12f, 6f),
                "PINCH-INP-01 only a finger-distance change past the deadzone starts a pinch");

            result.Assert(
                Mathf.Approximately(MutinyPlayerInput.ResolvePinchDistanceRatio(100f, 150f), 1.5f) &&
                Mathf.Approximately(MutinyPlayerInput.ResolvePinchDistanceRatio(0f, 150f), 1f),
                "PINCH-INP-02 spreading fingers yields a >1 ratio while a missing baseline stays neutral");

            GameObject levelObject = null;
            GameObject cameraObject = null;
            GameObject turnObject = null;
            GameObject teamObject = null;
            GameObject inputObject = null;
            try
            {
                levelObject = new GameObject("PinchZoom_Level");
                MutinyLevelRoot level = levelObject.AddComponent<MutinyLevelRoot>();
                level.Width = 100;
                level.Height = 100;
                level.WaterLevelY = -80f;

                cameraObject = new GameObject("PinchZoom_Camera");
                Camera unityCamera = cameraObject.AddComponent<Camera>();
                unityCamera.orthographic = true;
                unityCamera.orthographicSize = MutinyCameraController.OriginalOrthographicSize;
                cameraObject.transform.position = new Vector3(20f, -20f, -10f);
                MutinyCameraController camera = cameraObject.AddComponent<MutinyCameraController>();
                camera.SetLevelRootForVerification(level);

                turnObject = new GameObject("PinchZoom_Turn");
                MutinyTurnManager turn = turnObject.AddComponent<MutinyTurnManager>();
                teamObject = new GameObject("PinchZoom_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                turn.CurrentTeam = team;
                turn.CurrentPhase = TurnPhase.TurnActive;
                inputObject = new GameObject("PinchZoom_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = turn;
                input.GameCamera = unityCamera;
                camera.TurnManager = turn;
                camera.PlayerInput = input;

                result.Assert(Mathf.Approximately(camera.ZoomScale, 1f) &&
                              Mathf.Approximately(unityCamera.orthographicSize,
                                  MutinyCameraController.OriginalOrthographicSize),
                    "PINCH-ZOOM-03 a fresh board starts at the authored 1x framing");

                bool baselineNeutral =
                    !input.AdvancePinchGesture(new Vector2(100f, 100f), new Vector2(200f, 100f));
                bool driftStaysUnzoomed =
                    !input.AdvancePinchGesture(new Vector2(100f, 100f), new Vector2(203f, 100f));
                result.Assert(baselineNeutral && driftStaysUnzoomed && !input.IsPinching &&
                              Mathf.Approximately(camera.ZoomScale, 1f),
                    "PINCH-ZOOM-04 the first two-finger frame is a baseline and sub-deadzone drift never zooms");

                bool spread = input.AdvancePinchGesture(
                    new Vector2(100f, 100f), new Vector2(220f, 100f));
                result.Assert(spread && input.IsPinching && camera.ZoomScale > 1f &&
                              unityCamera.orthographicSize <
                                  MutinyCameraController.OriginalOrthographicSize,
                    "PINCH-ZOOM-05 spreading the fingers zooms the view in");

                float distance = 220f;
                for (int i = 0; i < 25; i++)
                {
                    distance += 40f;
                    input.AdvancePinchGesture(new Vector2(100f, 100f),
                        new Vector2(100f + distance, 100f));
                }
                result.Assert(Mathf.Approximately(camera.ZoomScale,
                                  MutinyCameraController.MaximumZoomScale),
                    "PINCH-ZOOM-06 zooming in stops at the 2x limit");

                for (int i = 0; i < 60; i++)
                {
                    distance = Mathf.Max(40f, distance - 45f);
                    input.AdvancePinchGesture(new Vector2(100f, 100f),
                        new Vector2(100f + distance, 100f));
                }
                result.Assert(Mathf.Approximately(camera.ZoomScale,
                                  MutinyCameraController.MinimumZoomScale) &&
                              unityCamera.orthographicSize >
                                  MutinyCameraController.OriginalOrthographicSize,
                    "PINCH-ZOOM-07 pinching together zooms out and stops at the 0.6x limit");

                Vector3 positionBeforePinch = cameraObject.transform.position;
                input.AdvancePinchGesture(new Vector2(100f, 100f), new Vector2(400f, 60f));
                result.Assert(input.IsPinching &&
                              Vector3.Distance(cameraObject.transform.position, positionBeforePinch) < 0.0001f,
                    "PINCH-ZOOM-08 an active pinch never pans the camera");

                // Releasing must end the gesture, and the frame that re-establishes
                // the baseline may not zoom: that is what keeps a fresh two-finger
                // touch from jumping the view before the fingers actually move.
                float zoomBeforeRelease = camera.ZoomScale;
                input.ResetMobileGestureOwnershipForVerification();
                bool released = !input.IsPinching;
                bool rebased = !input.AdvancePinchGesture(
                    new Vector2(100f, 100f), new Vector2(300f, 100f));
                result.Assert(released && rebased &&
                              Mathf.Approximately(camera.ZoomScale, zoomBeforeRelease),
                    "PINCH-ZOOM-09 releasing the fingers ends the pinch and the next gesture only rebases");

                camera.ResetForLevel(level);
                result.Assert(Mathf.Approximately(camera.ZoomScale, 1f) &&
                              Mathf.Approximately(unityCamera.orthographicSize,
                                  MutinyCameraController.OriginalOrthographicSize),
                    "PINCH-ZOOM-10 entering a level restores the authored 1x framing");
            }
            finally
            {
                DestroyNow(inputObject);
                DestroyNow(teamObject);
                DestroyNow(turnObject);
                DestroyNow(cameraObject);
                DestroyNow(levelObject);
            }
        }

        private static void VerifyAndroidAdaptation(MutinyLevel1VerificationResult result)
        {
            result.Assert(
                !MutinyCameraController.ShouldUseMouseEdgeScrolling(true, true) &&
                MutinyCameraController.ShouldUseMouseEdgeScrolling(false, true) &&
                !MutinyCameraController.ShouldUseMouseEdgeScrolling(false, false),
                "AND-CAM-01 mobile pointer state cannot trigger desktop hover-edge scrolling");

            MutinyPlayerInput.PointerFrameState began =
                MutinyPlayerInput.TouchPhaseToPointerStateForVerification(
                    UnityEngine.InputSystem.TouchPhase.Began);
            MutinyPlayerInput.PointerFrameState moved =
                MutinyPlayerInput.TouchPhaseToPointerStateForVerification(
                    UnityEngine.InputSystem.TouchPhase.Moved);
            MutinyPlayerInput.PointerFrameState stationary =
                MutinyPlayerInput.TouchPhaseToPointerStateForVerification(
                    UnityEngine.InputSystem.TouchPhase.Stationary);
            MutinyPlayerInput.PointerFrameState ended =
                MutinyPlayerInput.TouchPhaseToPointerStateForVerification(
                    UnityEngine.InputSystem.TouchPhase.Ended);
            MutinyPlayerInput.PointerFrameState canceled =
                MutinyPlayerInput.TouchPhaseToPointerStateForVerification(
                    UnityEngine.InputSystem.TouchPhase.Canceled);
            result.Assert(
                began.PressedThisFrame && began.IsPressed && !began.ReleasedThisFrame &&
                !began.CanceledThisFrame && !moved.PressedThisFrame && moved.IsPressed &&
                stationary.IsPressed && ended.ReleasedThisFrame && !ended.IsPressed &&
                !canceled.ReleasedThisFrame && canceled.CanceledThisFrame,
                "AND-INP-01/03 touch phases map to press, hold, release, and non-firing cancellation");
            result.Assert(
                MutinyPlayerInput.ShouldAcquireTouchForVerification(false, true) &&
                !MutinyPlayerInput.ShouldAcquireTouchForVerification(true, true) &&
                !MutinyPlayerInput.ShouldAcquireTouchForVerification(false, false),
                "AND-INP-02 only a fresh touch can acquire an otherwise unowned gesture");

            Vector2 worldDrag = MutinyCameraController.ScreenDeltaToWorldDelta(
                new Vector2(100f, 50f), 1000f, 500f, 10f, 2f);
            result.Assert(
                Mathf.Approximately(worldDrag.x, 4f) &&
                Mathf.Approximately(worldDrag.y, 2f),
                "AND-INP-04/AND-CAM-02 touch drag uses the orthographic camera's visible width and height");
            result.Assert(
                !MutinyPlayerInput.ShouldConvertPendingTapToCameraDrag(24f, 24f) &&
                MutinyPlayerInput.ShouldConvertPendingTapToCameraDrag(24.01f, 24f),
                "AND-INP-05 a click weapon commits at or below the tap threshold and becomes camera drag only above it");
            result.Assert(
                !MutinyCameraController.IsAimingCompatibleWithMobilePan(true, false) &&
                MutinyCameraController.IsAimingCompatibleWithMobilePan(true, true) &&
                MutinyCameraController.IsAimingCompatibleWithMobilePan(false, false),
                "AND-INP-06/AND-CAM-03 only the explicit secondary-touch camera role can pan during aiming");

            // Aspect ratio & Letterbox viewport rect tests
            Rect exact11x8Rect = MutinyCameraController.CalculateViewportRect(1100, 800);
            result.Assert(
                Mathf.Approximately(exact11x8Rect.x, 0f) &&
                Mathf.Approximately(exact11x8Rect.y, 0f) &&
                Mathf.Approximately(exact11x8Rect.width, 1f) &&
                Mathf.Approximately(exact11x8Rect.height, 1f),
                "CAM-ASPECT-01 exact 11:8 (1100x800) aspect uses full viewport (0, 0, 1, 1)");

            Rect widescreen16x9Rect = MutinyCameraController.CalculateViewportRect(1920, 1080);
            result.Assert(
                Mathf.Approximately(widescreen16x9Rect.x, 0.11328125f) &&
                Mathf.Approximately(widescreen16x9Rect.y, 0f) &&
                Mathf.Approximately(widescreen16x9Rect.width, 0.7734375f) &&
                Mathf.Approximately(widescreen16x9Rect.height, 1f),
                "CAM-ASPECT-02 16:9 widescreen creates horizontal pillarbox bars maintaining exact 11:8 aspect");

            Rect taller4x3Rect = MutinyCameraController.CalculateViewportRect(1024, 768);
            result.Assert(
                Mathf.Approximately(taller4x3Rect.x, 0f) &&
                Mathf.Approximately(taller4x3Rect.y, 0.015151515f) &&
                Mathf.Approximately(taller4x3Rect.width, 1f) &&
                Mathf.Approximately(taller4x3Rect.height, 0.96969697f),
                "CAM-ASPECT-03 4:3 display creates vertical letterbox bars maintaining exact 11:8 aspect");

            // Edge scrolling boundary tests
            Rect viewportPixelRect = new Rect(217.5f, 0f, 1485f, 1080f);
            float edgePixels = 108f;
            bool sLeft = false, sRight = false, sDown = false, sUp = false;
            MutinyCameraController.CalculateMouseEdgeScroll(
                new Vector2(300f, 540f), viewportPixelRect, 1920f, 1080f, edgePixels,
                ref sLeft, ref sRight, ref sDown, ref sUp);
            result.Assert(sLeft && !sRight && !sDown && !sUp,
                "CAM-EDGE-01 mouse inside left edge of letterboxed viewport triggers scrollLeft");

            sLeft = false; sRight = false; sDown = false; sUp = false;
            MutinyCameraController.CalculateMouseEdgeScroll(
                new Vector2(1650f, 540f), viewportPixelRect, 1920f, 1080f, edgePixels,
                ref sLeft, ref sRight, ref sDown, ref sUp);
            result.Assert(!sLeft && sRight && !sDown && !sUp,
                "CAM-EDGE-02 mouse inside right edge of letterboxed viewport triggers scrollRight");

            sLeft = false; sRight = false; sDown = false; sUp = false;
            MutinyCameraController.CalculateMouseEdgeScroll(
                new Vector2(1000f, 540f), viewportPixelRect, 1920f, 1080f, edgePixels,
                ref sLeft, ref sRight, ref sDown, ref sUp);
            result.Assert(!sLeft && !sRight && !sDown && !sUp,
                "CAM-EDGE-03 mouse in center of viewport does not trigger edge scrolling");

            sLeft = false; sRight = false; sDown = false; sUp = false;
            MutinyCameraController.CalculateMouseEdgeScroll(
                new Vector2(-10f, 540f), viewportPixelRect, 1920f, 1080f, edgePixels,
                ref sLeft, ref sRight, ref sDown, ref sUp);
            result.Assert(!sLeft && !sRight && !sDown && !sUp,
                "CAM-EDGE-04 mouse outside window boundaries does not trigger edge scrolling");
        }

        public static MutinyLevel1VerificationResult RunBattleHud()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyBattleHud(result);
            VerifyTurnNotice(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunBottomNotices()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyTurnNotice(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunAirDropPresentation()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyAirDropPresentation(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunWeaponReadyAndCancel()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyWeaponReadyAndCancel(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunMobileGestures()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyAndroidAdaptation(result);
            VerifyMobilePinchZoom(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunAimCancelTouch()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyAimCancelTouch(result);
            VerifyCancelCrossPressOnly(result);
            return result;
        }

        public static MutinyLevel1VerificationResult RunCharacterAimOverlay()
        {
            var result = new MutinyLevel1VerificationResult();
            VerifyCharacterOverlay(result);
            VerifyCharacterAimOverlay(result);
            VerifyAimCancelTouch(result);
            VerifyCancelCrossPressOnly(result);
            return result;
        }

        private static void VerifyCancelCrossPressOnly(MutinyLevel1VerificationResult result)
        {
            for (int action = 0; action < 2; action++)
            {
                bool isJump = action == 1;
                GameObject fixture = new GameObject("CancelCrossPressOnly_" + (isJump ? "Jump" : "Weapon"));
                GameObject firedWeaponObject = null;
                try
                {
                    var managerObject = new GameObject("Manager");
                    managerObject.transform.SetParent(fixture.transform, false);
                    var teamObject = new GameObject("Team");
                    teamObject.transform.SetParent(fixture.transform, false);
                    var characterObject = new GameObject("Character");
                    characterObject.transform.SetParent(fixture.transform, false);
                    var inputObject = new GameObject("Input");
                    inputObject.transform.SetParent(fixture.transform, false);
                    MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                    MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                    MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                    MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                    team.TeamNumber = 1;
                    team.RegisterCharacter(character);
                    team.SelectCharacter(character);
                    manager.CurrentTeam = team;
                    manager.CurrentPhase = TurnPhase.TurnActive;
                    input.TurnManager = manager;
                    Vector2 origin = new Vector2(120f, 160f);
                    Vector2 cross = origin + new Vector2(0f, 33f);
                    character.PhysicsBody.State = PhysicsBodyState.CreateDefault(origin.x, origin.y);
                    character.transform.position = MutinyPhysics.PixelToUnity(origin.x, origin.y);
                    character.AddWeapon("cherryBomb");
                    int ammunition = character.GetAmmunition("cherryBomb");
                    bool selected = isJump ? input.SelectCharacterThrow() : input.SelectWeapon("cherryBomb");
                    bool noPressIgnored = !input.TryHandleCancelOverlayPrimaryPointerForVerification(character, cross, false);
                    bool newPressCancelled = input.TryHandleCancelOverlayPrimaryPointerForVerification(character, cross, true);
                    result.Assert(selected && noPressIgnored && newPressCancelled && input.IsActionMenuOpen &&
                                  character.CanThrow && character.GetAmmunition("cherryBomb") == ammunition,
                        $"WPN-CAN-PRESS-01 {(isJump ? "jump" : "weapon")} ready cross requires a new press and cancels without spending action");

                    selected = isJump ? input.SelectCharacterThrow() : input.SelectWeapon("cherryBomb");
                    MutinyWeapon weapon = input.EquippedWeapon;
                    Vector2 aimOrigin = isJump ? origin : new Vector2(weapon.PhysicsBody.State.X, weapon.PhysicsBody.State.Y);
                    bool began = input.TryBeginAimFromPrimaryPointerForVerification(character, aimOrigin);
                    bool heldIgnored = !input.TryHandleCancelOverlayPrimaryPointerForVerification(character, cross, false);
                    result.Assert(selected && began && heldIgnored && input.IsAiming &&
                                  input.ShouldShowCancelWeapon(character) && character.CanThrow &&
                                  character.GetAmmunition("cherryBomb") == ammunition,
                        $"WPN-CAN-PRESS-01 {(isJump ? "jump" : "weapon")} entering cross while held does not cancel the aim");

                    if (weapon != null)
                        firedWeaponObject = weapon.gameObject;
                    input.AdvanceAimPointerForVerification(character, cross, false, true);
                    bool committed = isJump
                        ? character.IsSelfThrown && !character.CanThrow
                        : weapon.IsFired && input.EquippedWeapon == null &&
                          character.GetAmmunition("cherryBomb") == ammunition - 1;
                    result.Assert(committed && !input.IsActionMenuOpen && !input.IsAiming,
                        $"WPN-CAN-PRESS-01 {(isJump ? "jump" : "weapon")} production release on cross commits normally instead of activating cancel");
                }
                finally
                {
                    DestroyNow(firedWeaponObject);
                    DestroyNow(fixture);
                }
            }
        }

        private static void VerifyCharacterAimOverlay(MutinyLevel1VerificationResult result)
        {
            GameObject managerObject = null;
            GameObject teamObject = null;
            GameObject characterObject = null;
            GameObject teammateObject = null;
            GameObject inputObject = null;
            GameObject levelObject = null;
            TextAsset levelXml = null;
            try
            {
                managerObject = new GameObject("CharacterAimOverlay_Manager");
                teamObject = new GameObject("CharacterAimOverlay_Team");
                characterObject = new GameObject("CharacterAimOverlay_Character");
                teammateObject = new GameObject("CharacterAimOverlay_Teammate");
                inputObject = new GameObject("CharacterAimOverlay_Input");
                levelObject = new GameObject("CharacterAimOverlay_Level");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                MutinyCharacterOverlay overlay = characterObject.AddComponent<MutinyCharacterOverlay>();
                MutinyCharacter teammate = teammateObject.AddComponent<MutinyCharacter>();
                MutinyCharacterOverlay teammateOverlay = teammateObject.AddComponent<MutinyCharacterOverlay>();
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                var trajectoryObject = new GameObject("CharacterAimOverlay_Trajectory");
                trajectoryObject.transform.SetParent(inputObject.transform, false);
                input.TrajectoryRenderer = trajectoryObject.AddComponent<MutinyTrajectoryRenderer>();
                LineRenderer pullLine = trajectoryObject.GetComponent<LineRenderer>();
                MutinyLevelController levelController = levelObject.AddComponent<MutinyLevelController>();
                levelXml = new TextAsset("<level width=\"1\" height=\"1\" players=\"1\"><row>-</row><bgRow>-</bgRow></level>");
                levelController.LevelXml = levelXml;
                input.CacheTerrain();
                team.TeamNumber = 1;
                team.RegisterCharacter(character);
                team.RegisterCharacter(teammate);
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;
                input.TurnManager = manager;
                Vector2 origin = new Vector2(120f, 160f);
                character.PhysicsBody.State = PhysicsBodyState.CreateDefault(origin.x, origin.y);
                character.transform.position = MutinyPhysics.PixelToUnity(origin.x, origin.y);
                teammate.PhysicsBody.State = PhysicsBodyState.CreateDefault(240f, 160f);
                teammate.transform.position = MutinyPhysics.PixelToUnity(240f, 160f);
                bool selected = input.TrySelectCharacterForVerification(team, origin);
                bool selectedJump = input.SelectCharacterThrow();
                overlay.RefreshVisualStateForVerification();
                result.Assert(selected && selectedJump && overlay.IsTurnIndicatorVisible &&
                              overlay.IsHealthBarVisible && overlay.IsSelectionCornersVisible && overlay.IsCancelWeaponVisible,
                    "CHAR-OVR-AIM-01 production jump selection displays marker, health, corners and cross");

                SpriteRenderer cancelRenderer = characterObject.transform
                    .Find("OriginalCharacterOverlay/CancelWeapon").GetComponent<SpriteRenderer>();
                Material cancelMaterial = Resources.Load<Material>("UI/cancel_button_transparent");
                result.Assert(cancelMaterial != null && cancelRenderer.sharedMaterial == cancelMaterial &&
                              cancelMaterial.shader.name == "Universal Render Pipeline/2D/Sprite-Unlit-Default" &&
                              cancelMaterial.GetTag("RenderType", false) == "Transparent" &&
                              Mathf.Approximately(cancelMaterial.GetFloat("_ZWrite"), 0f) &&
                              cancelRenderer.sprite.texture.filterMode == FilterMode.Point,
                    "CHAR-OVR-CANCEL-ALPHA-01 production ready cross uses its transparent unlit material without depth writes");

                bool began = input.TryBeginAimFromPrimaryPointerForVerification(character, origin);
                Vector2 pulled = origin + new Vector2(-50f, 60f);
                input.AdvanceAimPointerForVerification(character, pulled, true, false);
                overlay.RefreshVisualStateForVerification();
                teammateOverlay.RefreshVisualStateForVerification();
                result.Assert(began && input.IsAiming && !character.IsSelfThrown && character.CanThrow &&
                              pullLine.enabled && pullLine.positionCount == 2 &&
                              overlay.IsTurnIndicatorVisible && overlay.IsHealthBarVisible &&
                              overlay.IsSelectionCornersVisible && overlay.IsCancelWeaponVisible,
                    "CHAR-OVR-AIM-01 held production jump aim renders trajectory without hiding any of the four overlay elements");
                result.Assert(teammateOverlay.IsTurnIndicatorVisible && teammateOverlay.IsHealthBarVisible &&
                              !teammateOverlay.IsSelectionCornersVisible && !teammateOverlay.IsCancelWeaponVisible,
                    "CHAR-OVR-AIM-01 aiming only affects the selected character's action; teammate marker and health remain visible");

                bool rightCancelled = input.TryCancelAimFromSecondaryPointerForVerification();
                overlay.RefreshVisualStateForVerification();
                result.Assert(rightCancelled && !input.IsAiming && !pullLine.enabled && character.CanThrow &&
                              overlay.IsTurnIndicatorVisible && overlay.IsHealthBarVisible &&
                              overlay.IsSelectionCornersVisible && overlay.IsCancelWeaponVisible,
                    "CHAR-OVR-AIM-01 right-cancel hides trajectory but retains all ready jump overlay elements");

                input.TryBeginAimFromPrimaryPointerForVerification(character, origin);
                input.AdvanceAimPointerForVerification(character, pulled, true, false);
                bool crossCancelled = input.TryCancelWeaponFromOverlayForVerification(
                    character, origin + new Vector2(0f, 33f));
                overlay.RefreshVisualStateForVerification();
                result.Assert(crossCancelled && input.IsActionMenuOpen && !pullLine.enabled && character.CanThrow &&
                              overlay.IsTurnIndicatorVisible && overlay.IsHealthBarVisible &&
                              overlay.IsSelectionCornersVisible && !overlay.IsCancelWeaponVisible,
                    "CHAR-OVR-AIM-01 cross cancel returns to action menu without hiding marker, health or selection corners");

                input.SelectCharacterThrow();
                input.TryBeginAimFromPrimaryPointerForVerification(character, origin);
                input.AdvanceAimPointerForVerification(character, pulled, false, true);
                overlay.RefreshVisualStateForVerification();
                result.Assert(character.IsSelfThrown && !character.CanThrow && !pullLine.enabled &&
                              manager.CurrentPhase == TurnPhase.ActionExecuting &&
                              !overlay.IsTurnIndicatorVisible && !overlay.IsHealthBarVisible &&
                              !overlay.IsSelectionCornersVisible && !overlay.IsCancelWeaponVisible,
                    "CHAR-OVR-AIM-01 only production release commit marks thrown and hides all four overlay elements");

                team.ContinueSelectedCharacterAfterAction();
                overlay.RefreshVisualStateForVerification();
                result.Assert(!character.IsSelfThrown && overlay.IsTurnIndicatorVisible &&
                              overlay.IsHealthBarVisible && overlay.IsSelectionCornersVisible,
                    "CHAR-OVR-AIM-01 production action continuation restores marker, health and selected corners");
            }
            finally
            {
                DestroyNow(inputObject);
                DestroyNow(characterObject);
                DestroyNow(teammateObject);
                DestroyNow(teamObject);
                DestroyNow(managerObject);
                DestroyNow(levelObject);
                if (levelXml != null)
                    Object.DestroyImmediate(levelXml);
            }
        }

        private static void VerifyAimCancelTouch(MutinyLevel1VerificationResult result)
        {
            GameObject managerObject = null;
            GameObject teamObject = null;
            GameObject characterObject = null;
            GameObject inputObject = null;
            GameObject launchedWeaponObject = null;
            try
            {
                managerObject = new GameObject("AimCancelTouch_Manager");
                teamObject = new GameObject("AimCancelTouch_Team");
                characterObject = new GameObject("AimCancelTouch_Character");
                inputObject = new GameObject("AimCancelTouch_Input");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                MutinyCharacterOverlay overlay = characterObject.AddComponent<MutinyCharacterOverlay>();
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                team.TeamNumber = 1;
                team.RegisterCharacter(character);
                team.SelectCharacter(character);
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;
                input.TurnManager = manager;
                PhysicsBodyState state = character.PhysicsBody.State;
                state.X = 120f;
                state.Y = 160f;
                character.PhysicsBody.State = state;
                character.transform.position = MutinyPhysics.PixelToUnity(state.X, state.Y);
                character.AddWeapon("cherryBomb");
                int ammunition = character.GetAmmunition("cherryBomb");
                Vector2 cross = new Vector2(state.X, state.Y + 33f);

                bool selected = input.SelectWeapon("cherryBomb");
                MutinyWeapon cancelCandidate = input.EquippedWeapon;
                Vector2 weaponPosition = cancelCandidate != null
                    ? new Vector2(cancelCandidate.PhysicsBody.State.X, cancelCandidate.PhysicsBody.State.Y)
                    : Vector2.zero;
                bool started = input.TryBeginAimFromPrimaryPointerForVerification(character, weaponPosition);
                overlay.RefreshVisualStateForVerification();
                bool visibleDuringWeaponAim = overlay.IsCancelWeaponVisible;
                bool offCrossIgnored = !input.TryCancelAimFromSecondaryTouchForVerification(
                    cross + new Vector2(40f, 0f));
                bool secondTouchCancelled = input.TryCancelAimFromSecondaryTouchForVerification(cross);
                input.ResolveAimReleaseForVerification(character, weaponPosition + new Vector2(20f, 10f));
                result.Assert(selected && started && visibleDuringWeaponAim && offCrossIgnored &&
                              secondTouchCancelled && input.IsActionMenuOpen && input.EquippedWeapon == null &&
                              cancelCandidate != null && !cancelCandidate.IsFired &&
                              character.GetAmmunition("cherryBomb") == ammunition &&
                              character.CanThrow && character.CanShoot,
                    "EXT-AIM-CAN-02 weapon aiming cross cancels via second touch; later first-finger release is inert");

                bool selectedJump = input.SelectCharacterThrow();
                bool startedJump = input.TryBeginAimFromPrimaryPointerForVerification(
                    character, new Vector2(state.X, state.Y));
                overlay.RefreshVisualStateForVerification();
                bool visibleDuringJumpAim = overlay.IsCancelWeaponVisible;
                bool pressedJumpCross = input.TryHandleCancelOverlayPrimaryPointerForVerification(character, cross, true);
                result.Assert(selectedJump && startedJump && visibleDuringJumpAim && pressedJumpCross &&
                              input.IsActionMenuOpen && !character.IsSelfThrown && character.CanThrow,
                    "WPN-CAN-PRESS-01 new press on jump aiming cross cancels without spending throw");

                bool selectedJumpAgain = input.SelectCharacterThrow();
                bool startedJumpAgain = input.TryBeginAimFromPrimaryPointerForVerification(
                    character, new Vector2(state.X, state.Y));
                bool secondTouchCancelledJump = input.TryCancelAimFromSecondaryTouchForVerification(cross);
                input.ResolveAimReleaseForVerification(character, cross + new Vector2(40f, 0f));
                result.Assert(selectedJumpAgain && startedJumpAgain && secondTouchCancelledJump &&
                              input.IsActionMenuOpen && !character.IsSelfThrown && character.CanThrow,
                    "EXT-AIM-CAN-02 second touch also cancels jump aim; later first-finger release is inert");

                input.SelectWeapon("cherryBomb");
                MutinyWeapon retained = input.EquippedWeapon;
                Vector2 retainedPosition = new Vector2(retained.PhysicsBody.State.X, retained.PhysicsBody.State.Y);
                bool startedAgain = input.TryBeginAimFromPrimaryPointerForVerification(character, retainedPosition);
                bool rightCancelled = input.TryCancelAimFromSecondaryPointerForVerification();
                result.Assert(startedAgain && rightCancelled && input.IsWeaponReady &&
                              input.EquippedWeapon == retained && character.GetAmmunition("cherryBomb") == ammunition,
                    "EXT-JUMP-CAN-01 desktop right-click still returns to ready without unequipping");

                bool startedToFire = input.TryBeginAimFromPrimaryPointerForVerification(character, retainedPosition);
                launchedWeaponObject = retained.gameObject;
                input.ResolveAimReleaseForVerification(character, retainedPosition + new Vector2(20f, 10f));
                result.Assert(startedToFire && retained.IsFired && input.EquippedWeapon == null &&
                              character.GetAmmunition("cherryBomb") == ammunition - 1,
                    "EXT-AIM-CAN-02 release away from cross still uses production weapon launch");
            }
            finally
            {
                DestroyNow(launchedWeaponObject);
                DestroyNow(inputObject);
                DestroyNow(characterObject);
                DestroyNow(teamObject);
                DestroyNow(managerObject);
            }
        }

        private static void VerifyWeaponReadyAndCancel(MutinyLevel1VerificationResult result)
        {
            GameObject managerObject = null;
            GameObject teamObject = null;
            GameObject characterObject = null;
            GameObject inputObject = null;
            GameObject launchedWeaponObject = null;
            try
            {
                managerObject = new GameObject("WeaponReadyVerification_Manager");
                teamObject = new GameObject("WeaponReadyVerification_Team");
                characterObject = new GameObject("WeaponReadyVerification_Character");
                inputObject = new GameObject("WeaponReadyVerification_Input");

                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                MutinyCharacterOverlay overlay = characterObject.AddComponent<MutinyCharacterOverlay>();
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                team.TeamNumber = 1;
                team.RegisterCharacter(character);
                team.SelectCharacter(character);
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;
                input.TurnManager = manager;
                PhysicsBodyState ownerState = character.PhysicsBody.State;
                ownerState.X = 120f;
                ownerState.Y = 160f;
                character.PhysicsBody.State = ownerState;

                bool allWeaponsEquipped = true;
                bool allWeaponPivotsMatch = true;
                for (int i = 0; i < WeaponIconResources.Length; i++)
                {
                    string weaponType = WeaponIconResources[i];
                    character.AddWeapon(weaponType);
                    bool selected = input.SelectWeapon(weaponType);
                    MutinyWeapon equipped = input.EquippedWeapon;
                    Vector2 expectedOffset = MutinyWeapon.GetOriginalEquipOffsetPixels(weaponType);
                    float expectedY = ownerState.Y + expectedOffset.y;
                    bool rendererStateValid = equipped.SpriteRenderer.enabled == equipped.IsBodyVisibleWhileReady;
                    allWeaponsEquipped &= selected && equipped != null && equipped.Owner == character &&
                                          equipped.SpriteRenderer != null && rendererStateValid &&
                                          Mathf.Approximately(equipped.PhysicsBody.State.X, ownerState.X + expectedOffset.x) &&
                                          Mathf.Approximately(equipped.PhysicsBody.State.Y, expectedY) &&
                                          equipped.PhysicsBody.IsActive == equipped.AdvancesMotionWhileReady;

                    if (equipped != null && equipped.SpriteRenderer != null && equipped.SpriteRenderer.sprite != null)
                    {
                        Sprite sp = equipped.SpriteRenderer.sprite;
                        Vector2 actualPivot = new Vector2(sp.pivot.x / sp.rect.width, sp.pivot.y / sp.rect.height);
                        Vector2 expectedPivot = GetExpectedWeaponPivot(weaponType);
                        allWeaponPivotsMatch &= Vector2.Distance(actualPivot, expectedPivot) < 0.005f;
                    }
                    input.CancelWeaponSelection();
                }
                result.Assert(allWeaponsEquipped && allWeaponPivotsMatch,
                    "WRDY-T01 every menu weapon starts at Character.equip's original coordinate, uses its Flash registration pivot, and matches both pre-fire motion and constructor show() visibility");

                character.AddWeapon("banana");
                input.SelectWeapon("banana");
                MutinyBanana readyBanana = input.EquippedWeapon as MutinyBanana;
                float bananaInitialY = ownerState.Y - 10f;
                readyBanana.PhysicsBody.SetTerrain(new string[8, 8], 8, 8);
                readyBanana.PhysicsBody.AdvanceSimulationTick();
                result.Assert(readyBanana.PhysicsBody.IsActive &&
                              Mathf.Approximately(readyBanana.PhysicsBody.State.Y, bananaInitialY + 1f),
                    "WRDY-T01A an unfired Banana runs the first original gravity tick instead of hovering at its initial -10px equipment coordinate");
                input.CancelWeaponSelection();

                character.AddWeapon("piecesOfEight");
                input.SelectWeapon("piecesOfEight");
                MutinyPiecesOfEight readyPieces = input.EquippedWeapon as MutinyPiecesOfEight;
                readyPieces.PhysicsBody.SetTerrain(new string[8, 8], 8, 8);
                readyPieces.PhysicsBody.AdvanceSimulationTick();
                result.Assert(Mathf.Approximately(readyPieces.PhysicsBody.State.X, ownerState.X) &&
                              Mathf.Approximately(readyPieces.PhysicsBody.State.Y, ownerState.Y + 6f),
                    "WRDY-T01B PiecesOfEight resets to owner +5px before motion, then applies the same tick's +1 gravity exactly like PiecesOfEight.advance");
                input.CancelWeaponSelection();

                int ammoBeforeCancel = character.GetAmmunition("cherryBomb");
                result.Assert(input.SelectWeapon("cherryBomb"),
                    "WRDY-T02 production selection enters WeaponReady");
                MutinyWeapon retained = input.EquippedWeapon;
                overlay.RefreshVisualStateForVerification();
                bool readyShowsCancel = input.InteractionState == MutinyPlayerInteractionState.WeaponReady &&
                                        overlay.IsCancelWeaponVisible;
                Vector2 readyWeaponPosition = new Vector2(
                    retained.PhysicsBody.State.X, retained.PhysicsBody.State.Y);
                bool startedWeaponAim = input.TryBeginAimFromPrimaryPointerForVerification(
                    character, readyWeaponPosition);
                overlay.RefreshVisualStateForVerification();
                bool aimingShowsCancel = input.IsAiming && overlay.IsCancelWeaponVisible;
                input.CancelCurrentAim();
                overlay.RefreshVisualStateForVerification();
                result.Assert(readyShowsCancel && startedWeaponAim && aimingShowsCancel &&
                              input.InteractionState == MutinyPlayerInteractionState.WeaponReady &&
                              input.EquippedWeapon == retained && overlay.IsCancelWeaponVisible,
                    "EXT-AIM-CAN-02 aiming retains the cross; right-cancel still returns to the same ready weapon");

                bool crossCancelled = input.TryCancelWeaponFromOverlayForVerification(
                    character, new Vector2(ownerState.X, ownerState.Y + 33f));
                result.Assert(crossCancelled && input.IsActionMenuOpen && input.EquippedWeapon == null &&
                              character.GetAmmunition("cherryBomb") == ammoBeforeCancel,
                    "WRDY-T02 clicking the original 20px cross unequips without consuming ammunition");

                input.SelectWeapon("cherryBomb");
                MutinyWeapon touchCancelledWeapon = input.EquippedWeapon;
                Vector2 touchWeaponPosition = new Vector2(
                    touchCancelledWeapon.PhysicsBody.State.X, touchCancelledWeapon.PhysicsBody.State.Y);
                bool touchAimStarted = input.TryBeginAimFromPrimaryPointerForVerification(
                    character, touchWeaponPosition);
                overlay.RefreshVisualStateForVerification();
                bool offCrossIgnored = !input.TryCancelAimFromSecondaryTouchForVerification(
                    new Vector2(ownerState.X + 40f, ownerState.Y + 33f));
                bool secondTouchCancelled = input.TryCancelAimFromSecondaryTouchForVerification(
                    new Vector2(ownerState.X, ownerState.Y + 33f));
                input.ResolveAimReleaseForVerification(character, touchWeaponPosition + new Vector2(30f, 10f));
                result.Assert(touchAimStarted && overlay.IsCancelWeaponVisible && offCrossIgnored &&
                              secondTouchCancelled && input.IsActionMenuOpen && input.EquippedWeapon == null &&
                              !touchCancelledWeapon.IsFired &&
                              character.GetAmmunition("cherryBomb") == ammoBeforeCancel &&
                              character.CanThrow && character.CanShoot,
                    "EXT-AIM-CAN-02 second touch on cross cancels weapon aim; later primary release cannot fire or spend action");

                input.SelectCharacterThrow();
                bool jumpAimStarted = input.TryBeginAimFromPrimaryPointerForVerification(
                    character, new Vector2(ownerState.X, ownerState.Y));
                overlay.RefreshVisualStateForVerification();
                bool jumpCrossVisible = overlay.IsCancelWeaponVisible;
                bool newJumpCrossPress = input.TryHandleCancelOverlayPrimaryPointerForVerification(character,
                    new Vector2(ownerState.X, ownerState.Y + 33f), true);
                result.Assert(jumpAimStarted && jumpCrossVisible && newJumpCrossPress && input.IsActionMenuOpen &&
                              !character.IsSelfThrown && character.CanThrow,
                    "WPN-CAN-PRESS-01 new press on cross cancels jump aim without throwing");

                input.SelectWeapon("cherryBomb");
                MutinyWeapon launchedInstance = input.EquippedWeapon;
                launchedWeaponObject = launchedInstance.gameObject;
                Vector2 start = new Vector2(
                    launchedInstance.PhysicsBody.State.X, launchedInstance.PhysicsBody.State.Y);
                bool releaseAimStarted = input.TryBeginAimFromPrimaryPointerForVerification(character, start);
                input.ResolveAimReleaseForVerification(character, start + new Vector2(20f, 10f));
                bool launched = releaseAimStarted && launchedInstance.IsFired;
                result.Assert(launched && launchedInstance.IsFired && input.EquippedWeapon == null &&
                              character.GetAmmunition("cherryBomb") == ammoBeforeCancel - 1,
                    "WRDY-T04 production non-cross release fires the already equipped instance and consumes ammunition only on commit");
            }
            finally
            {
                DestroyNow(launchedWeaponObject);
                DestroyNow(inputObject);
                DestroyNow(characterObject);
                DestroyNow(teamObject);
                DestroyNow(managerObject);
            }
        }

        private static void VerifyGunpowderBarrel(MutinyLevel1VerificationResult result)
        {
            GameObject teamObject = null;
            GameObject managerObject = null;
            GameObject characterObject = null;
            GameObject inputObject = null;
            try
            {
                teamObject = new GameObject("GunpowderBarrelVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                team.TeamNumber = 1;

                managerObject = new GameObject("GunpowderBarrelVerification_TurnManager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;

                characterObject = new GameObject("GunpowderBarrelVerification_Character");
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                character.TeamIndex = 1;
                character.AddWeapon("gunpowderBarrel");
                PhysicsBodyState characterState = PhysicsBodyState.CreateDefault(256f, 96f);
                characterState.Weight = 0f;
                character.PhysicsBody.State = characterState;
                team.RegisterCharacter(character);
                team.SelectCharacter(character);

                inputObject = new GameObject("GunpowderBarrelVerification_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = manager;
                bool selected = input.SelectWeapon("gunpowderBarrel");
                MutinyGunpowderBarrel root = input.ArmedGunpowderBarrel;
                string[,] terrain = new string[6, 12];
                for (int row = 0; row < 6; row++)
                    for (int column = 0; column < 12; column++)
                        terrain[row, column] = "-";
                for (int column = 0; column < 12; column++)
                    terrain[4, column] = "ground";
                root.PhysicsBody.SetTerrain(terrain, 12, 6);

                bool rejected = input.TryActivateClickWeaponForVerification(character, new Vector2(64f, 112f));
                result.Assert(selected && rejected && root.PlacedCount == 0 && character.HasWeapon("gunpowderBarrel"),
                    "WPN-06-INT-02 production click rejects a terrain-overlapping barrel without consuming inventory");

                bool first = input.TryActivateClickWeaponForVerification(character, new Vector2(64f, 96f));
                bool barrelWaitHasNoSafetyTimeout = !root.IsFinished;
                input.UpdateBoxPlacementCursorForVerification(new Vector2(64f, 96f));
                bool placedRootNowShowsCross = input.SpecialWeaponCursorModeForVerification == "Cross";
                int countBeforeOverlapClick = root.PlacedCount;
                bool overlapClickConsumed = input.TryActivateClickWeaponForVerification(
                    character, new Vector2(64f, 96f));
                bool overlapClickRejected = overlapClickConsumed && root.PlacedCount == countBeforeOverlapClick;
                MutinyGunpowderBarrel child = root.NextBox;
                bool second = input.TryActivateClickWeaponForVerification(character, new Vector2(128f, 96f));
                // BoxWeapon.advance propagates nextBox.finished to the root.
                // A render-frame click alone has not run that production tick.
                root.AdvancePlacementTickForVerification();
                result.Assert(first && barrelWaitHasNoSafetyTimeout && placedRootNowShowsCross && overlapClickRejected && second &&
                              root.PlacedCount == MutinyGunpowderBarrel.OriginalPlacementCount &&
                              child != null && root.IsFinished && !character.HasWeapon("gunpowderBarrel") &&
                              !character.CanThrow && !character.CanShoot && manager.CurrentPhase == TurnPhase.ActionExecuting,
                    "WPN-06-INT-01/03/BOX-PLC-01 cursor and click both reject the placed root, then one inventory item places exactly two barrels");
                result.Assert(MutinyGunpowderBarrel.GetPhysicsObstacles(null).Count == 2 &&
                              Mathf.Approximately(root.PhysicsBody.State.LeftExtent, 16f) &&
                              Mathf.Approximately(root.PhysicsBody.State.RightExtent, 15f) &&
                              root.PhysicsBody.State.HitsBoxes,
                    "WPN-06-EFF-01 placed barrels retain BoxWeapon 16/15 extents and shared box collision registration");

                MutinyExplosion trigger = MutinyExplosion.Spawn(new Vector2(64f, 96f), 40f, 1f, null, false);
                trigger.ApplyHit();
                MutinyExplosion rootBlast = null;
                foreach (MutinyExplosion explosion in Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None))
                {
                    if (explosion != null && explosion != trigger && Mathf.Approximately(explosion.Size, 150f))
                    {
                        rootBlast = explosion;
                        break;
                    }
                }
                rootBlast?.ApplyHit();
                result.Assert(root.IsExploding && child.IsExploding && root.CurrentAnimationFrame == MutinyGunpowderBarrel.OriginalExplodeLabelFrame &&
                              rootBlast != null && Mathf.Approximately(rootBlast.MaxDamage, 30f) && rootBlast.Caster == null,
                    "WPN-06-EFF-02/ANI-01 production explosion starts at label frame 11 and chains a null-caster 150/30 blast to the second barrel");
            }
            finally
            {
                DestroyNow(inputObject);
                DestroyNow(characterObject);
                DestroyNow(managerObject);
                DestroyNow(teamObject);
                foreach (MutinyGunpowderBarrel barrel in Object.FindObjectsByType<MutinyGunpowderBarrel>(FindObjectsSortMode.None))
                    DestroyNow(barrel != null ? barrel.gameObject : null);
                foreach (MutinyExplosion explosion in Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None))
                    DestroyNow(explosion != null ? explosion.gameObject : null);
            }
        }

        private static void VerifyWoodenCrate(MutinyLevel1VerificationResult result)
        {
            GameObject teamObject = null;
            GameObject managerObject = null;
            GameObject characterObject = null;
            GameObject inputObject = null;
            GameObject explosionObject = null;
            GameObject cameraObject = null;
            try
            {
                teamObject = new GameObject("WoodenCrateVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                team.TeamNumber = 1;

                managerObject = new GameObject("WoodenCrateVerification_TurnManager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;

                characterObject = new GameObject("WoodenCrateVerification_Character");
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                character.TeamIndex = 1;
                character.AddWeapon("woodenCrate");
                PhysicsBodyState characterState = PhysicsBodyState.CreateDefault(256f, 96f);
                characterState.Weight = 0f;
                character.PhysicsBody.State = characterState;
                team.RegisterCharacter(character);
                team.SelectCharacter(character);

                inputObject = new GameObject("WoodenCrateVerification_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = manager;
                bool selected = input.SelectWeapon("woodenCrate");
                MutinyWoodenCrate root = input.ArmedWoodenCrate;
                string[,] terrain = new string[6, 12];
                for (int r = 0; r < 6; r++)
                    for (int c = 0; c < 12; c++)
                        terrain[r, c] = "-";
                for (int c = 0; c < 12; c++)
                    terrain[4, c] = "ground";
                root.PhysicsBody.SetTerrain(terrain, 12, 6);

                string[,] partialSupportTerrain = new string[6, 12];
                for (int r = 0; r < 6; r++)
                    for (int c = 0; c < 12; c++)
                        partialSupportTerrain[r, c] = "-";
                // (192,80) spans columns 5..6, but only column 5 has support.
                partialSupportTerrain[4, 5] = "ground";
                bool partialSupportIsLegal = root.CanPlace(
                    new Vector2(192f, 80f), partialSupportTerrain, 12, 6);

                bool illegalClick = input.TryActivateClickWeaponForVerification(character, new Vector2(64f, 112f));
                result.Assert(selected && illegalClick && root.PlacedCount == 0 && character.HasWeapon("woodenCrate"),
                    "WPN-14-INT-02 production click rejects terrain overlap without consuming the crate inventory");

                bool first = input.TryActivateClickWeaponForVerification(
                    character, new Vector2(64f, 96f));
                for (int waitTick = 0; waitTick <= 150; waitTick++)
                    manager.AdvanceSimulationTick();
                bool pendingPlacementSurvivedSafetyThreshold =
                    !root.IsFinished && root.HasPendingPlacement &&
                    input.ArmedWoodenCrate == root;
                cameraObject = new GameObject("WoodenCrateVerification_Camera");
                cameraObject.AddComponent<Camera>();
                MutinyCameraController camera = cameraObject.AddComponent<MutinyCameraController>();
                camera.TurnManager = manager;
                camera.PlayerInput = input;
                bool pendingPlacementAllowsManualCamera =
                    camera.FindActionTargetForVerification() == null &&
                    camera.CanUseManualScrollingForVerification() &&
                    camera.CanAcceptManualScrollingForVerification();
                input.UpdateBoxPlacementCursorForVerification(new Vector2(64f, 96f));
                bool placedRootNowShowsCross = input.SpecialWeaponCursorModeForVerification == "Cross";
                int countBeforeInvalidRequest = root.PlacedCount;
                bool invalidRequestConsumed = input.TryActivateClickWeaponForVerification(
                    character, new Vector2(64f, 96f));
                bool invalidRequestPreservedPlacedBoxes = invalidRequestConsumed &&
                    root.PlacedCount == countBeforeInvalidRequest &&
                    MutinyBoxRegistry.Count == countBeforeInvalidRequest &&
                    root.NextBox != null;
                bool heldButtonCannotRetryPlacement =
                    !MutinyPlayerInput.ShouldHandleWeaponReadyPrimaryInput(false, true) &&
                    MutinyPlayerInput.ShouldHandleWeaponReadyPrimaryInput(true, true);
                bool second = input.TryActivateClickWeaponForVerification(character, new Vector2(96f, 96f));
                bool third = input.TryActivateClickWeaponForVerification(character, new Vector2(128f, 96f));
                result.Assert(partialSupportIsLegal && first && placedRootNowShowsCross &&
                              pendingPlacementSurvivedSafetyThreshold &&
                              pendingPlacementAllowsManualCamera &&
                              invalidRequestPreservedPlacedBoxes && heldButtonCannotRetryPlacement &&
                              second && third && root.PlacedCount == 3 &&
                              !character.HasWeapon("woodenCrate") &&
                              !character.CanThrow && !character.CanShoot && manager.CurrentPhase == TurnPhase.ActionExecuting,
                    "WPN-14-INT-01/03/BOX-PLC-01/02/BOX-SUP-01/BOX-WAIT-01/BOX-CAM-01 pending placement survives 150 ticks, permits manual camera scrolling, preserves invalid input, and later valid clicks place exactly three crates");
                result.Assert(Mathf.Approximately(root.PhysicsBody.State.LeftExtent, 16f) &&
                              Mathf.Approximately(root.PhysicsBody.State.RightExtent, 15f) &&
                              root.PhysicsBody.State.HitsBoxes,
                    "WPN-14-EFF-01 crate retains original 16/15 extents and participates in BoxWeapon collisions");

                // Reproduce the corner case behind characters occasionally being
                // pushed underground: a settled crate overlaps a floor-standing
                // character that begins an upward tick. The raw box correction is
                // y=136, inside terrain row 4; production physics must retain the
                // terrain-safe axis start while still reporting the ceiling hit.
                PhysicsBodyState rootStateBeforePenetrationCheck = root.PhysicsBody.State;
                PhysicsBodyState settledRootState = rootStateBeforePenetrationCheck;
                settledRootState.Y = 112.9f;
                root.PhysicsBody.State = settledRootState;
                PhysicsBodyState pinchedCharacterState = PhysicsBodyState.CreateDefault(64f, 119.9f);
                pinchedCharacterState.Weight = 0f;
                pinchedCharacterState.VelocityY = -4f;
                pinchedCharacterState.HitsBoxes = true;
                character.PhysicsBody.State = pinchedCharacterState;
                character.PhysicsBody.SetTerrain(terrain, 12, 6);
                StepResult pinchedResult = character.PhysicsBody.AdvanceSimulationTick();
                bool boxCorrectionStayedAboveTerrain =
                    pinchedResult.HitCeiling &&
                    Mathf.Approximately(character.PhysicsBody.State.Y, 119.9f) &&
                    character.PhysicsBody.State.Y + character.PhysicsBody.State.BottomExtent < 128f;
                root.PhysicsBody.State = rootStateBeforePenetrationCheck;
                result.Assert(boxCorrectionStayedAboveTerrain,
                    "BOX-COL-02/CRT-BOX-02 production box correction cannot push an overlapped character into its supporting terrain");

                explosionObject = new GameObject("WoodenCrateVerification_Explosion");
                MutinyExplosion explosion = explosionObject.AddComponent<MutinyExplosion>();
                explosion.PixelX = 48f;
                explosion.PixelY = 128f;
                explosion.Radius = 20f;
                explosion.PlayPopOnHit = false;
                PhysicsBodyState blastTargetState = character.PhysicsBody.State;
                blastTargetState.X = 48f;
                blastTargetState.Y = 112f;
                blastTargetState.VelocityX = 0f;
                blastTargetState.VelocityY = 0f;
                character.PhysicsBody.State = blastTargetState;
                float targetYBeforeBlastMotion = blastTargetState.Y;
                int boxesBeforeBlast = MutinyBoxRegistry.Count;
                explosion.ApplyHit();
                bool hitSettledInOriginalOrder =
                    character.PhysicsBody.State.VelocityY < 0f &&
                    root.IsExploding &&
                    root.TimelineFrameForVerification == MutinyWoodenCrate.OriginalExplodeFirstFrame &&
                    MutinyBoxRegistry.Count == boxesBeforeBlast - 1 &&
                    root.NextBox != null && !root.NextBox.IsExploding;
                character.PhysicsBody.AdvanceSimulationTick();
                root.AdvanceExplosionTimelineFrameForVerification();
                bool movementAndBreakRunTogether =
                    character.PhysicsBody.State.Y < targetYBeforeBlastMotion &&
                    root.TimelineFrameForVerification == MutinyWoodenCrate.OriginalExplodeFirstFrame + 1;
                for (int frame = MutinyWoodenCrate.OriginalExplodeFirstFrame + 1;
                     frame < MutinyWoodenCrate.OriginalDestroyFrame;
                     frame++)
                {
                    root.AdvanceExplosionTimelineFrameForVerification();
                }
                result.Assert(hitSettledInOriginalOrder && movementAndBreakRunTogether &&
                              root.TimelineFrameForVerification == MutinyWoodenCrate.OriginalDestroyFrame &&
                              !root.IsVisibleForVerification,
                    "CRT-EXP-01/02 frame-3 hit launches the character, unregisters only the struck crate, starts crate frame 11 immediately, and runs movement alongside frames 11..18");
            }
            finally
            {
                DestroyNow(explosionObject);
                DestroyNow(cameraObject);
                DestroyNow(inputObject);
                DestroyNow(characterObject);
                DestroyNow(managerObject);
                DestroyNow(teamObject);
                foreach (MutinyWoodenCrate crate in Object.FindObjectsByType<MutinyWoodenCrate>(FindObjectsSortMode.None))
                    DestroyNow(crate != null ? crate.gameObject : null);
            }
        }

        private static void VerifyPiecesOfEight(MutinyLevel1VerificationResult result)
        {
            GameObject teamObject = null;
            GameObject managerObject = null;
            GameObject ownerObject = null;
            GameObject inputObject = null;
            try
            {
                teamObject = new GameObject("PiecesOfEightVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                team.TeamNumber = 1;
                managerObject = new GameObject("PiecesOfEightVerification_Manager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;
                ownerObject = new GameObject("PiecesOfEightVerification_Owner");
                MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();
                owner.AddWeapon("piecesOfEight");
                PhysicsBodyState ownerState = PhysicsBodyState.CreateDefault(64f, 64f);
                ownerState.Weight = 0f;
                owner.PhysicsBody.State = ownerState;
                owner.transform.position = MutinyPhysics.PixelToUnity(64f, 64f);
                team.RegisterCharacter(owner);
                team.SelectCharacter(owner);
                inputObject = new GameObject("PiecesOfEightVerification_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = manager;

                bool selected = input.SelectWeapon("piecesOfEight");
                MutinyPiecesOfEight coins = input.ArmedPiecesOfEight;
                bool firstLaunch = input.TryLaunchWeaponForVerification(owner, new Vector2(64f, 69f), new Vector2(-36f, 69f));
                coins.ResolveCoinForVerification(explode: true);
                bool firstResolution = coins.TimesFired == 1 && coins.ShotsRemaining == 7 &&
                                       coins.IsAwaitingNextCoin && owner.WeaponLocked &&
                                       !owner.HasWeapon("piecesOfEight") && !owner.CanShoot && !owner.CanThrow &&
                                       manager.CurrentPhase == TurnPhase.ActionExecuting && !manager.CheckAllBodiesAtRest();

                bool continuousInput = true;
                for (int coin = 2; coin <= MutinyPiecesOfEight.TotalCoins; coin++)
                {
                    continuousInput &= input.TryLaunchWeaponForVerification(
                        owner, new Vector2(64f, 69f), new Vector2(-36f, 69f));
                    // The third coin exercises the water transition; all others use
                    // the same terminal transition contacted by production physics.
                    coins.ResolveCoinForVerification(explode: coin != 3);
                }

                result.Assert(selected && coins != null && firstLaunch && firstResolution && continuousInput &&
                              coins.TimesFired == MutinyPiecesOfEight.TotalCoins && coins.IsFinished &&
                              !coins.CanFireNextCoin,
                    "WPN-09-INT-01/02/03 production input consumes one inventory item then reuses the locked PiecesOfEight instance for exactly eight coins");
                result.Assert(Mathf.Approximately(coins.PhysicsBody.State.LeftExtent, MutinyPiecesOfEight.OriginalExtentPixels) &&
                              Mathf.Approximately(coins.PhysicsBody.State.RightExtent, MutinyPiecesOfEight.OriginalExtentPixels) &&
                              coins.PhysicsBody.State.HitsBoxes,
                    "WPN-09-EFF-01 production PiecesOfEight retains 7px Solid extents and box contacts");
            }
            finally
            {
                DestroyNow(inputObject);
                DestroyNow(ownerObject);
                DestroyNow(managerObject);
                DestroyNow(teamObject);
                foreach (MutinyPiecesOfEight coins in Object.FindObjectsByType<MutinyPiecesOfEight>(FindObjectsSortMode.None))
                    DestroyNow(coins != null ? coins.gameObject : null);
            }
        }

        private static void VerifyPiecesOfEightPresentation(MutinyLevel1VerificationResult result)
        {
            GameObject ownerObject = null;
            MutinyPiecesOfEight coins = null;
            try
            {
                ownerObject = new GameObject("PiecesOfEightPresentation_Owner");
                MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();
                PhysicsBodyState ownerState = PhysicsBodyState.CreateDefault(64f, 64f);
                ownerState.Weight = 0f;
                owner.PhysicsBody.State = ownerState;
                owner.PhysicsBody.SetTerrain(new string[32, 32], 32, 32);
                owner.transform.position = MutinyPhysics.PixelToUnity(64f, 64f);

                coins = MutinyWeaponFactory.SpawnWeapon("piecesOfEight", owner) as MutinyPiecesOfEight;
                MutinyPhysicsBody body = coins.PhysicsBody;
                body.SetTerrain(new string[32, 32], 32, 32);
                body.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                body.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep * 0.5f);
                body.ApplyPresentationPoseForVerification();
                float firstMidpoint = MutinyPhysics.UnityToPixel(coins.transform.position).y;
                body.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep * 0.5f);
                body.ApplyPresentationPoseForVerification();
                float secondStart = MutinyPhysics.UnityToPixel(coins.transform.position).y;
                result.Assert(!coins.IsFired && Mathf.Approximately(body.State.Y, 70f) &&
                              Mathf.Approximately(firstMidpoint, 70f) &&
                              Mathf.Approximately(secondStart, 70f) &&
                              Mathf.Approximately(MutinyPhysics.UnityToPixel(body.SamplePresentationPosition(0f)).y, 70f) &&
                              Mathf.Approximately(MutinyPhysics.UnityToPixel(body.SamplePresentationPosition(0.99f)).y, 70f),
                    "POE-PRES-01 consecutive ready ticks retain original +1 gravity result without interpolating a repeated one-pixel fall");

                owner.PhysicsBody.SetVelocity(10f, 0f);
                owner.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                owner.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep * 0.5f);
                body.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                body.ApplyPresentationPoseForVerification();
                Vector2 movingCoin = MutinyPhysics.UnityToPixel(coins.transform.position);
                result.Assert(Mathf.Approximately(owner.PhysicsBody.State.X, 74f) &&
                              Mathf.Approximately(body.State.X, 74f) &&
                              Mathf.Approximately(movingCoin.x, 69f) &&
                              Mathf.Approximately(movingCoin.y, 70f),
                    "POE-PRES-01 ready coin follows the owner's smooth presentation displacement without falling sawtooth");

                coins.SetAimingState(true);
                body.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                float aimingStart = MutinyPhysics.UnityToPixel(body.SamplePresentationPosition(0f)).y;
                float aimingMiddle = MutinyPhysics.UnityToPixel(body.SamplePresentationPosition(0.5f)).y;
                float aimingEnd = MutinyPhysics.UnityToPixel(body.SamplePresentationPosition(1f)).y;
                result.Assert(coins.IsBeingAimed && aimingStart < aimingMiddle && aimingMiddle < aimingEnd,
                    "POE-PRES-01 aiming coin restores ordinary gravity-path interpolation");

                coins.SetAimingState(false);
                coins.Fire(new Vector2(10f, -5f));
                body.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                Vector2 flightStart = MutinyPhysics.UnityToPixel(body.SamplePresentationPosition(0f));
                Vector2 flightMiddle = MutinyPhysics.UnityToPixel(body.SamplePresentationPosition(0.5f));
                Vector2 flightEnd = MutinyPhysics.UnityToPixel(body.SamplePresentationPosition(1f));
                result.Assert(coins.IsFired && flightStart.x < flightMiddle.x && flightMiddle.x < flightEnd.x,
                    "POE-PRES-01 fired coin keeps high-frame-rate flight interpolation");
            }
            finally
            {
                DestroyNow(coins != null ? coins.gameObject : null);
                DestroyNow(ownerObject);
            }
        }

        private static void VerifyAiBoxCamera(MutinyLevel1VerificationResult result)
        {
            foreach (string type in new[] { "woodenCrate", "gunpowderBarrel" })
            foreach (int fps in new[] { 60, 120 })
            foreach (bool stack in new[] { false, true })
            {
                var objects = new List<GameObject>();
                TextAsset xml = null;
                MutinyCharacter owner = null;
                Random.State randomState = Random.state;
                string label = type + " " + fps + " FPS " + (stack ? "stack" : "different positions");
                try
                {
                    GameObject rootObject = new GameObject("AiBoxCamera_Level");
                    objects.Add(rootObject);
                    MutinyLevelRoot level = rootObject.AddComponent<MutinyLevelRoot>();
                    level.Width = 64;
                    level.Height = 20;
                    level.WaterLevelY = -20f;
                    var source = new System.Text.StringBuilder("<level width='64' height='20' players='1'>");
                    for (int row = 0; row < 20; row++)
                    {
                        // Block the optional upward branch without replacing the
                        // original random/legality selection formula in the test.
                        string tiles = row == 12 ? "solid:64" :
                            !stack && row == 8 ? "-:16,solid,-:11,solid,-:35" : "-:64";
                        source.Append("<row>").Append(tiles).Append("</row><bgRow>-:64</bgRow>");
                    }
                    source.Append("</level>");
                    xml = new TextAsset(source.ToString());
                    rootObject.AddComponent<MutinyLevelController>().LevelXml = xml;

                    GameObject teamObject = new GameObject("AiBoxCamera_Team");
                    objects.Add(teamObject);
                    MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                    team.TeamNumber = 2;
                    team.IsAiControlled = true;
                    MutinyAIController ai = teamObject.AddComponent<MutinyAIController>();
                    GameObject ownerObject = new GameObject("AiBoxCamera_Owner");
                    objects.Add(ownerObject);
                    owner = ownerObject.AddComponent<MutinyCharacter>();
                    owner.PhysicsBody.State = PhysicsBodyState.CreateDefault(64f, 320f);
                    owner.PhysicsBody.IsActive = false;
                    owner.PhysicsBody.State.Weight = 0f;
                    owner.AddWeapon(type);
                    team.RegisterCharacter(owner);
                    team.SelectCharacter(owner);
                    GameObject turnObject = new GameObject("AiBoxCamera_Turn");
                    objects.Add(turnObject);
                    MutinyTurnManager turn = turnObject.AddComponent<MutinyTurnManager>();
                    turn.CurrentTeam = team;
                    turn.CurrentPhase = TurnPhase.TurnActive;
                    GameObject cameraObject = new GameObject("AiBoxCamera_Camera");
                    objects.Add(cameraObject);
                    cameraObject.AddComponent<Camera>();
                    MutinyCameraController camera = cameraObject.AddComponent<MutinyCameraController>();
                    camera.TurnManager = turn;
                    camera.SetLevelRootForVerification(level);
                    cameraObject.transform.position = MutinyPhysics.PixelToUnity(100f, 270f) + Vector3.back * 10f;

                    ai.ExecuteMoveForVerification(new AIMove
                    {
                        MoveType = AIMoveType.ShootWeapon,
                        Character = owner,
                        WeaponType = type,
                        BoxPossibilities = new[] { new Vector2(128f, 320f), new Vector2(512f, 320f), new Vector2(896f, 320f) }
                    }, turn);
                    MutinyWeapon root = null;
                    foreach (MutinyWeapon weapon in Object.FindObjectsByType<MutinyWeapon>(FindObjectsSortMode.None))
                        if (weapon.Owner == owner && weapon.WeaponType == type)
                            root = weapon;
                    result.Assert(root != null && !root.IsFired &&
                                  root.AiPlacementCameraTargetPixels == new Vector2(512f, 320f) &&
                                  camera.AiBoxPlacementTargetForVerification.HasValue &&
                                  turn.CurrentPhase == TurnPhase.ActionExecuting && !owner.HasWeapon(type),
                        "AI-BOX-CAM-01 " + label + " production AI execution starts planned-point tracking before the first box is placed");
                    if (root == null)
                        continue;
                    team.SelectCharacter(null);
                    result.Assert(camera.AiBoxPlacementTargetForVerification == null,
                        "EXT-AI-BOX-CAM-02 " + label + " no selected owner means no planned focus");
                    team.SelectCharacter(owner);
                    result.Assert(camera.AiBoxPlacementTargetForVerification.HasValue,
                        "EXT-AI-BOX-CAM-02 " + label + " reselecting the owning character restores its live plan");
                    MutinyWoodenCrate crate = root as MutinyWoodenCrate;
                    MutinyGunpowderBarrel barrel = root as MutinyGunpowderBarrel;
                    System.Action tick = () =>
                    {
                        if (crate != null) crate.AdvanceAiPlacementTickForVerification();
                        else barrel.AdvancePlacementTickForVerification();
                    };
                    System.Func<int> count = () => crate != null ? crate.PlacedCount : barrel.PlacedCount;
                    int total = crate != null ? 3 : 2;
                    float xBefore = 100f;
                    camera.AdvanceCameraForVerification(1f / fps);
                    float firstX = MutinyPhysics.UnityToPixel(cameraObject.transform.position).x;
                    camera.AdvanceCameraForVerification(1f / fps);
                    float secondX = MutinyPhysics.UnityToPixel(cameraObject.transform.position).x;
                    result.Assert(Mathf.Abs(firstX - xBefore - 750f / fps) < 0.001f &&
                                  Mathf.Abs(secondX - firstX - 750f / fps) < 0.001f && count() == 0,
                        "AI-BOX-CAM-01 " + label + " camera advances on consecutive render frames at original 30 px/tick speed without advancing placement");

                    GameObject chestObject = new GameObject("AiBoxCamera_HigherPriorityChest");
                    objects.Add(chestObject);
                    MutinyTreasureChest chest = chestObject.AddComponent<MutinyTreasureChest>();
                    chest.Initialize(null, 1000f, 1000f, new List<string> { "banana" });
                    Vector3 beforeChestPan = cameraObject.transform.position;
                    camera.AdvanceCameraForVerification(MutinyPhysics.TimeStep);
                    result.Assert(camera.IsTrackingAirDrop && cameraObject.transform.position.y > beforeChestPan.y &&
                                  Mathf.Abs(Vector2.Distance(cameraObject.transform.position, beforeChestPan) *
                                      MutinyPhysics.PixelsPerUnit - 50f) < 0.001f && count() == 0,
                        "AI-BOX-CAM-01 " + label + " falling chest retains its higher 50 px/tick priority without changing box timing");
                    DestroyNow(chestObject);

                    for (int placed = 0; placed < total; placed++)
                    {
                        Vector2 planned = root.AiPlacementCameraTargetPixels.Value;
                        for (int step = 0; step < 39; step++)
                        {
                            tick();
                            camera.AdvanceCameraForVerification(MutinyPhysics.TimeStep);
                        }
                        result.Assert(count() == placed &&
                                      Vector2.Distance(MutinyPhysics.UnityToPixel(cameraObject.transform.position),
                                          planned + Vector2.down * 50f) < 0.001f,
                            "AI-BOX-CAM-01 " + label + " placement " + (placed + 1) + " camera reaches planned point before unchanged 40-tick spawn");
                        tick();
                        result.Assert(count() == placed + 1,
                            "AI-BOX-CAM-01 " + label + " placement " + (placed + 1) + " occurs at tick 40");
                        if (placed == total - 1)
                            break;
                        for (int step = 0; step < 9; step++) tick();
                        result.Assert(root.AiPlacementCameraTargetPixels == planned && count() == placed + 1,
                            "EXT-AI-BOX-CAM-02 " + label + " keeps focus during post-place gap and does not spawn early");
                        // A deterministic seed chooses the real >= .4 stack
                        // branch. No private target/flag or formula is replaced.
                        if (stack)
                        {
                            for (int seed = 1; seed < 100; seed++)
                            {
                                Random.InitState(seed);
                                if (Random.value >= 0.4f) { Random.InitState(seed); break; }
                            }
                        }
                        tick();
                        Vector2 expected = stack ? planned + Vector2.down * 48f :
                            placed == 0 ? new Vector2(896f, 320f) : new Vector2(128f, 288f);
                        result.Assert(root.AiPlacementCameraTargetPixels == expected &&
                                      camera.AiBoxPlacementTargetForVerification.HasValue &&
                                      Vector2.Distance(MutinyPhysics.UnityToPixel(camera.AiBoxPlacementTargetForVerification.Value), expected) < 0.001f &&
                                      Mathf.Abs(root.PhysicsBody.State.X - 512f) < 0.001f,
                            "AI-BOX-CAM-01/EXT-AI-BOX-CAM-02 " + label + " tenth gap tick retargets next plan, never the persistent first-box Transform");
                    }
                    camera.TrackWeapon(root);
                    result.Assert(root.AiPlacementCameraTargetPixels == null &&
                                  camera.AiBoxPlacementTargetForVerification == null &&
                                  camera.FindActionTargetForVerification() == null,
                        "EXT-AI-BOX-CAM-02 " + label + " completion releases planned focus and never falls back to a stored box");

                    if (fps == 60 && !stack)
                    {
                        MutinyWeapon rejected = MutinyWeaponFactory.SpawnWeapon(type, owner);
                        Vector2[] blocked = { new Vector2(64f, 320f), new Vector2(64f, 320f), new Vector2(64f, 320f) };
                        bool armed = rejected is MutinyWoodenCrate rejectedCrate
                            ? rejectedCrate.BeginAiPlacement(blocked)
                            : ((MutinyGunpowderBarrel)rejected).BeginAiPlacement(blocked);
                        result.Assert(!armed && rejected.AiPlacementCameraTargetPixels == null &&
                                      camera.AiBoxPlacementTargetForVerification == null,
                            "EXT-AI-BOX-CAM-02 illegal candidates under the owner cannot leave a stale AI camera plan");
                        owner.AddWeapon(type);
                        ai.ExecuteMoveForVerification(new AIMove
                        {
                            MoveType = AIMoveType.ShootWeapon, Character = owner, WeaponType = type,
                            BoxPossibilities = new[] { new Vector2(1536f, 320f), new Vector2(1664f, 320f), new Vector2(1792f, 320f) }
                        }, turn);
                        bool livePlan = camera.AiBoxPlacementTargetForVerification.HasValue;
                        owner.TakeDamage(100f);
                        result.Assert(livePlan && !owner.IsAlive && camera.AiBoxPlacementTargetForVerification == null,
                            "EXT-AI-BOX-CAM-02 production lethal damage releases a pending box owner's camera plan");
                    }
                }
                finally
                {
                    foreach (MutinyWeapon weapon in Object.FindObjectsByType<MutinyWeapon>(FindObjectsSortMode.None))
                        if (weapon != null && weapon.Owner == owner &&
                            (weapon is MutinyWoodenCrate || weapon is MutinyGunpowderBarrel))
                            DestroyNow(weapon.gameObject);
                    for (int index = objects.Count - 1; index >= 0; index--) DestroyNow(objects[index]);
                    if (xml != null) Object.DestroyImmediate(xml);
                    Random.state = randomState;
                }
            }
        }

        private static void VerifyCannonPresentation(MutinyLevel1VerificationResult result)
        {
            foreach (int fps in new[] { 25, 60, 120 })
            {
                GameObject ownerObject = null;
                MutinyCannon cannon = null;
                try
                {
                    ownerObject = new GameObject("CannonPresentation_Owner_" + fps);
                    MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();
                    owner.PhysicsBody.State = PhysicsBodyState.CreateDefault(100f, 200f);
                    owner.PhysicsBody.IsActive = false;
                    cannon = MutinyWeaponFactory.SpawnWeapon("cannon", owner) as MutinyCannon;
                    cannon.PhysicsBody.SetTerrain(new string[10, 8], 8, 10);
                    bool grabbed = cannon.TryBeginBodyDrag(new Vector2(100f, 190f));
                    cannon.DragBodyTo(new Vector2(140f, 150f));
                    cannon.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                    cannon.ApplyPlacementPresentationForVerification();
                    result.Assert(grabbed && !cannon.PhysicsBody.IsActive &&
                                  cannon.PhysicsBody.SimulationTickCount == 1 &&
                                  Mathf.Abs(cannon.PhysicsBody.State.X - 120f) < 0.001f &&
                                  Mathf.Abs(cannon.PhysicsBody.State.Y - 170f) < 0.001f &&
                                  Vector2.Distance(MutinyPhysics.UnityToPixel(cannon.transform.position),
                                      new Vector2(100f, 190f)) < 0.001f,
                        "CAN-PRES-01/02 " + fps + " FPS drag uses one half-distance collision step and starts display at prior pose");

                    float previousX = 100f;
                    bool intermediateFrames = true;
                    int samples = fps == 25 ? 0 : Mathf.FloorToInt(MutinyPhysics.TimeStep * fps);
                    for (int frame = 0; frame < samples; frame++)
                    {
                        cannon.AdvanceSimulationFrameForVerification(1f / fps);
                        cannon.ApplyPlacementPresentationForVerification();
                        float visibleX = MutinyPhysics.UnityToPixel(cannon.transform.position).x;
                        intermediateFrames &= visibleX > previousX && visibleX < 120f &&
                            cannon.PhysicsBody.SimulationTickCount == 1 &&
                            Mathf.Abs(cannon.PhysicsBody.State.X - 120f) < 0.001f &&
                            Vector3.Distance(cannon.transform.position,
                                cannon.PhysicsBody.PresentationPosition) < 0.00001f;
                        previousX = visibleX;
                    }
                    result.Assert(intermediateFrames &&
                                  Vector2.Distance(MutinyPhysics.UnityToPixel(
                                      cannon.RangeCircleRenderer.transform.position),
                                      new Vector2(100f, 100f)) < 0.001f &&
                                  cannon.PinRenderer.transform.parent == cannon.transform,
                        "CAN-PRES-02 " + fps + " FPS body and pin share a smooth pose while range guide stays independent and authority stays 25 Hz");

                    cannon.ReleasePointer(null);
                    cannon.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                    cannon.ApplyPlacementPresentationForVerification();
                    result.Assert(!cannon.IsDraggingBody &&
                                  cannon.PhysicsBody.SimulationTickCount == 1 &&
                                  Mathf.Abs(MutinyPhysics.UnityToPixel(cannon.transform.position).x - 120f) < 0.001f,
                        "CAN-PRES-02 pointer release completes display segment without another consumed drag step");

                    cannon.PlaceAtEquipmentPosition();
                    cannon.TryBeginBodyDrag(new Vector2(100f, 190f));
                    cannon.DragBodyTo(new Vector2(120f, 190f));
                    cannon.ReleasePointer(null);
                    cannon.ApplyPlacementPresentationForVerification();
                    bool shortRelease = Mathf.Abs(cannon.PhysicsBody.State.X - 110f) < 0.001f &&
                        Mathf.Abs(MutinyPhysics.UnityToPixel(cannon.transform.position).x - 100f) < 0.001f;
                    cannon.AdvanceSimulationFrameForVerification(0.02f);
                    cannon.ApplyPlacementPresentationForVerification();
                    result.Assert(shortRelease &&
                                  Mathf.Abs(MutinyPhysics.UnityToPixel(cannon.transform.position).x - 105f) < 0.001f,
                        "CAN-PRES-02 short drag release consumes final target through collision then renders its halfway pose");

                    cannon.PlaceAtEquipmentPosition();
                    var wall = new string[10, 8];
                    for (int row = 0; row < 10; row++)
                        wall[row, 4] = "solid";
                    cannon.PhysicsBody.SetTerrain(wall, 8, 10);
                    cannon.TryBeginBodyDrag(new Vector2(100f, 190f));
                    cannon.DragBodyTo(new Vector2(160f, 190f));
                    cannon.AdvanceOriginalTickForVerification();
                    cannon.ApplyPlacementPresentationForVerification();
                    result.Assert(Mathf.Abs(cannon.PhysicsBody.State.X - 117.9f) < 0.001f &&
                                  MutinyPhysics.UnityToPixel(cannon.transform.position).x <= 117.9f + 0.001f,
                        "CAN-PRES-01/02 actual wall sweep resolves to 117.9 px and interpolation never displays through wall");
                    cannon.CancelPointer();
                    cannon.ApplyPlacementPresentationForVerification();
                    result.Assert(!cannon.IsDraggingBody &&
                                  Mathf.Abs(cannon.PhysicsBody.State.X - 100f) < 0.001f &&
                                  Mathf.Abs(MutinyPhysics.UnityToPixel(cannon.transform.position).x - 100f) < 0.001f,
                        "CAN-PRES-02 cancel immediately restores original placement without replaying old display segment");

                    cannon.PhysicsBody.SetTerrain(new string[10, 8], 8, 10);
                    cannon.TryBeginBodyDrag(new Vector2(100f, 190f));
                    cannon.DragBodyTo(new Vector2(140f, 190f));
                    cannon.AdvanceOriginalTickForVerification();
                    cannon.ReleasePointer(null);
                    cannon.TryBeginPinDrag(new Vector2(99f, 190f));
                    cannon.DragPinTo(new Vector2(89f, 190f));
                    cannon.ReleasePointer(null);
                    cannon.AdvanceOriginalTickForVerification();
                    cannon.ApplyPlacementPresentationForVerification();
                    result.Assert(cannon.IsFired && cannon.Cannonball != null &&
                                  Mathf.Abs(cannon.Cannonball.PhysicsBody.State.X - 120f) < 0.001f &&
                                  Mathf.Abs(MutinyPhysics.UnityToPixel(cannon.transform.position).x - 120f) < 0.001f,
                        "CAN-PRES-02 firing clears display lag and spawns the separate ball from authoritative muzzle");
                }
                finally
                {
                    DestroyNow(cannon != null && cannon.Cannonball != null ? cannon.Cannonball.gameObject : null);
                    DestroyNow(cannon != null ? cannon.gameObject : null);
                    DestroyNow(ownerObject);
                }
            }
        }

        private static void VerifyCannon(MutinyLevel1VerificationResult result)
        {
            GameObject teamObject = null;
            GameObject managerObject = null;
            GameObject ownerObject = null;
            GameObject cannonObject = null;
            try
            {
                teamObject = new GameObject("CannonVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                managerObject = new GameObject("CannonVerification_Manager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;
                ownerObject = new GameObject("CannonVerification_Owner");
                MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();
                PhysicsBodyState ownerState = PhysicsBodyState.CreateDefault(100f, 200f);
                ownerState.Weight = 0f;
                owner.PhysicsBody.State = ownerState;
                team.RegisterCharacter(owner);

                MutinyCannon cannon = MutinyWeaponFactory.SpawnWeapon("cannon", owner) as MutinyCannon;
                cannonObject = cannon != null ? cannon.gameObject : null;
                bool equipmentPosition = cannon != null &&
                    Mathf.Approximately(cannon.PhysicsBody.State.Y, 190f) && cannon.PhysicsBody.State.HitsBoxes;
                bool rangeUsesOwnerAsCircleBottom = cannon != null &&
                    Vector2.Distance(cannon.PlacementCenterPixels, new Vector2(100f, 100f)) < 0.001f &&
                    Vector2.Distance(
                        MutinyPhysics.UnityToPixel(cannon.RangeCircleRenderer.transform.position),
                        new Vector2(100f, 100f)) < 0.001f;
                cannon.TryBeginBodyDrag(new Vector2(100f, 190f));
                cannon.DragBodyTo(new Vector2(100f, 400f));
                cannon.AdvanceOriginalTickForVerification();
                bool bodyClampUsesBottomAnchoredCircle = cannon.IsDraggingBody &&
                    Mathf.Approximately(cannon.PhysicsBody.State.X, 100f) &&
                    Mathf.Approximately(cannon.PhysicsBody.State.Y, 205f);
                cannon.CancelPointer();
                cannon.TryBeginPinDrag(new Vector2(cannon.PhysicsBody.State.X + cannon.PinX, cannon.PhysicsBody.State.Y));
                cannon.DragPinTo(new Vector2(cannon.PhysicsBody.State.X - 40f, cannon.PhysicsBody.State.Y));
                cannon.CancelPointer();
                bool canceledPinDoesNotCommit = !cannon.IsDraggingPin &&
                    Mathf.Approximately(cannon.PinX, MutinyCannon.PinRestX) &&
                    owner.CanShoot && owner.CanThrow && manager.CurrentPhase == TurnPhase.TurnActive;
                cannon.TryBeginPinDrag(new Vector2(cannon.PhysicsBody.State.X + cannon.PinX, cannon.PhysicsBody.State.Y));
                cannon.DragPinTo(new Vector2(cannon.PhysicsBody.State.X - 30f, cannon.PhysicsBody.State.Y));
                bool equalThresholdDoesNotCommit = !cannon.ReleasePointer(manager) && owner.CanShoot;
                cannon.TryBeginPinDrag(new Vector2(cannon.PhysicsBody.State.X + cannon.PinX, cannon.PhysicsBody.State.Y));
                cannon.DragPinTo(new Vector2(cannon.PhysicsBody.State.X - 31f, cannon.PhysicsBody.State.Y));
                bool committed = cannon.ReleasePointer(manager);
                cannon.AdvanceOriginalTickForVerification();
                bool fullForceBall = cannon.Cannonball != null &&
                                     Mathf.Approximately(cannon.Cannonball.PhysicsBody.State.VelocityX, 30f) &&
                                     cannon.Cannonball.SpriteRenderer.sortingOrder == cannon.SpriteRenderer.sortingOrder + 1 &&
                                     owner.CanShoot == false && owner.CanThrow == false;
                cannon.Cannonball?.Finish();
                for (int tick = 0; tick < 19; tick++)
                    cannon.AdvanceOriginalTickForVerification();
                bool waitsForCannonballAndFades = !cannon.IsFinished;
                cannon.AdvanceOriginalTickForVerification();
                bool finishesAfterBallAndTwentyFadeTicks = cannon.IsFinished;

                MutinyCannon aiCannon = MutinyWeaponFactory.SpawnWeapon("cannon", owner) as MutinyCannon;
                GameObject aiCannonObject = aiCannon != null ? aiCannon.gameObject : null;
                aiCannon?.BeginAiFire(new Vector2(150f, 110f), 90, new Vector2(0f, 30f));
                for (int tick = 0; tick < 24; tick++)
                    aiCannon?.AdvanceOriginalTickForVerification();
                bool aiWaitsTwentyFourTicks = aiCannon != null && !aiCannon.IsFired;
                aiCannon?.AdvanceOriginalTickForVerification();
                bool aiFiresOnTwentyFifthTick = aiCannon != null && aiCannon.IsFired &&
                                                 aiCannon.Cannonball != null &&
                                                 Mathf.Approximately(aiCannon.Cannonball.PhysicsBody.State.VelocityY, 30f);
                DestroyNow(aiCannonObject);
                result.Assert(equipmentPosition && rangeUsesOwnerAsCircleBottom && bodyClampUsesBottomAnchoredCircle &&
                              canceledPinDoesNotCommit && equalThresholdDoesNotCommit && committed &&
                              fullForceBall && waitsForCannonballAndFades && finishesAfterBallAndTwentyFadeTicks &&
                              aiWaitsTwentyFourTicks && aiFiresOnTwentyFifthTick,
                    "CAN-PLACE-01/03 WPN-05-INT/ANI/AI and AND-INP-03 production Cannon anchors its range above the owner, clamps body drag to that circle, cancels without firing, completes its lifecycle, and AI fires after 25 ticks");
            }
            finally
            {
                DestroyNow(cannonObject);
                foreach (MutinyCannonball ball in Object.FindObjectsByType<MutinyCannonball>(FindObjectsSortMode.None))
                    DestroyNow(ball != null ? ball.gameObject : null);
                DestroyNow(ownerObject);
                DestroyNow(managerObject);
                DestroyNow(teamObject);
            }
        }

        private static void VerifyAiCannonTurnSettlement(MutinyLevel1VerificationResult result)
        {
            GameObject aiTeamObject = null;
            GameObject opposingTeamObject = null;
            GameObject aiOwnerObject = null;
            GameObject opposingCharacterObject = null;
            GameObject turnObject = null;
            GameObject cameraObject = null;
            MutinyCannon cannon = null;
            MutinyCannonball ball = null;
            try
            {
                aiTeamObject = new GameObject("AiCannonTurnVerification_Team");
                MutinyTeam aiTeam = aiTeamObject.AddComponent<MutinyTeam>();
                aiTeam.TeamNumber = 1;
                aiTeam.IsAiControlled = true;
                MutinyAIController ai = aiTeamObject.AddComponent<MutinyAIController>();

                opposingTeamObject = new GameObject("AiCannonTurnVerification_OpposingTeam");
                MutinyTeam opposingTeam = opposingTeamObject.AddComponent<MutinyTeam>();
                opposingTeam.TeamNumber = 2;
                // StartGame selects Team 1 deterministically when Team 2 is AI.
                opposingTeam.IsAiControlled = true;

                aiOwnerObject = new GameObject("AiCannonTurnVerification_Owner");
                MutinyCharacter owner = aiOwnerObject.AddComponent<MutinyCharacter>();
                PhysicsBodyState ownerState = PhysicsBodyState.CreateDefault(100f, 200f);
                ownerState.Weight = 0f;
                owner.PhysicsBody.State = ownerState;
                owner.AddWeapon("cannon");
                aiTeam.RegisterCharacter(owner);

                opposingCharacterObject = new GameObject("AiCannonTurnVerification_Opponent");
                MutinyCharacter opponent = opposingCharacterObject.AddComponent<MutinyCharacter>();
                // This synchronous fixture verifies original boundary completion.
                // Keep its opponent outside the horizontal shot, since impact
                // explosions and health presentation require rendered Updates.
                // Those contact paths are covered by VerifyCannonImpactEffects.
                PhysicsBodyState opponentState = PhysicsBodyState.CreateDefault(500f, 300f);
                opponentState.Weight = 0f;
                opponent.PhysicsBody.State = opponentState;
                opposingTeam.RegisterCharacter(opponent);

                turnObject = new GameObject("AiCannonTurnVerification_TurnManager");
                MutinyTurnManager manager = turnObject.AddComponent<MutinyTurnManager>();
                manager.Initialize(aiTeam, opposingTeam);
                aiTeam.SelectCharacter(owner);

                cameraObject = new GameObject("AiCannonTurnVerification_Camera");
                cameraObject.AddComponent<Camera>();
                MutinyCameraController camera = cameraObject.AddComponent<MutinyCameraController>();
                camera.TurnManager = manager;

                ai.ExecuteMoveForVerification(new AIMove
                {
                    MoveType = AIMoveType.ShootWeapon,
                    Character = owner,
                    WeaponType = "cannon",
                    TargetPosition = new Vector2(120f, 190f),
                    CannonRotationDegrees = 0,
                    LaunchVelocity = new Vector2(30f, 0f)
                }, manager);
                foreach (MutinyCannon candidate in Object.FindObjectsByType<MutinyCannon>(FindObjectsSortMode.None))
                {
                    if (candidate != null && candidate.Owner == owner)
                    {
                        cannon = candidate;
                        break;
                    }
                }
                if (cannon == null)
                {
                    result.Assert(false, "CAN-AI-TURN-01 production AI cannon was created");
                    return;
                }

                bool waitsForShot = manager.ActionCommittedThisTurn &&
                    manager.CurrentTeam == aiTeam && cannon.IsAiFirePending;
                for (int tick = 0; tick < 24; tick++)
                {
                    manager.AdvanceSimulationTick();
                    cannon.AdvanceOriginalTickForVerification();
                    waitsForShot &= manager.CurrentTeam == aiTeam && manager.TurnCount == 0 &&
                        !cannon.IsFired && cannon.IsAiFirePending &&
                        manager.InactivityTicks == 0;
                }
                result.Assert(waitsForShot && !manager.CheckAllBodiesAtRest(),
                    "CAN-AI-TURN-01 AI action keeps the turn active through all 24 pre-fire ticks");

                manager.AdvanceSimulationTick();
                cannon.AdvanceOriginalTickForVerification();
                ball = cannon.Cannonball;
                result.Assert(cannon.IsFired && ball != null &&
                              camera.FindActionTargetForVerification() == ball.transform &&
                              manager.CurrentTeam == aiTeam && manager.TurnCount == 0,
                    "CAN-AI-TURN-01 the 25th tick fires and the action camera selects the cannonball");
                if (ball == null)
                    return;

                bool waitsForBall = true;
                for (int tick = 0; tick < 151; tick++)
                {
                    manager.AdvanceSimulationTick();
                    cannon.AdvanceOriginalTickForVerification();
                    waitsForBall &= manager.CurrentTeam == aiTeam && !cannon.IsFinished;
                }
                result.Assert(waitsForBall && ball != null && !ball.IsFinished,
                    "CAN-AI-TURN-01 a faded cannon and unresolved ball still hold the turn past the 150-tick safety threshold");

                ball.PhysicsBody.SetTerrain(new string[20, 20], 20, 20);
                for (int tick = 0; tick < 60 && !ball.IsFinished; tick++)
                {
                    ball.PhysicsBody.AdvanceSimulationTick();
                    manager.AdvanceSimulationTick();
                    cannon.AdvanceOriginalTickForVerification();
                }
                bool waitForInactivity = ball.IsFinished && cannon.IsFinished &&
                    manager.CurrentTeam == aiTeam;
                for (int tick = 0; tick < MutinyTurnManager.InactivitySettlingThreshold; tick++)
                {
                    manager.AdvanceSimulationTick();
                    waitForInactivity &= manager.CurrentTeam == aiTeam;
                }
                manager.AdvanceSimulationTick();
                result.Assert(waitForInactivity && manager.CurrentTeam == opposingTeam &&
                              manager.TurnCount == 1,
                    "CAN-AI-TURN-01 turn switches only after cannonball resolution, cannon completion and inactivity");
            }
            finally
            {
                DestroyNow(ball != null ? ball.gameObject : null);
                DestroyNow(cannon != null ? cannon.gameObject : null);
                DestroyNow(cameraObject);
                DestroyNow(turnObject);
                DestroyNow(aiOwnerObject);
                DestroyNow(opposingCharacterObject);
                DestroyNow(aiTeamObject);
                DestroyNow(opposingTeamObject);
            }
        }

        private static void VerifyBoulder(MutinyLevel1VerificationResult result)
        {
            GameObject teamObject = null;
            GameObject managerObject = null;
            GameObject ownerObject = null;
            GameObject rightTargetObject = null;
            GameObject leftTargetObject = null;
            GameObject edgeTargetObject = null;
            GameObject outsideTargetObject = null;
            GameObject inputObject = null;
            try
            {
                teamObject = new GameObject("BoulderVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                team.TeamNumber = 1;

                managerObject = new GameObject("BoulderVerification_TurnManager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;

                ownerObject = CreateBoulderCharacter("BoulderVerification_Owner", 0f, 0f);
                MutinyCharacter owner = ownerObject.GetComponent<MutinyCharacter>();
                owner.TeamIndex = 1;
                owner.AddWeapon("boulder");
                team.RegisterCharacter(owner);
                team.SelectCharacter(owner);

                // The production tick below starts at x=0 with vx=10.  Solid moves
                // it to x=10 before Boulder.as applies its character loop.
                rightTargetObject = CreateBoulderCharacter("BoulderVerification_Right", 20f, 0f);
                leftTargetObject = CreateBoulderCharacter("BoulderVerification_Left", 0f, 0f);
                edgeTargetObject = CreateBoulderCharacter("BoulderVerification_Edge", 42f, 0f);
                outsideTargetObject = CreateBoulderCharacter("BoulderVerification_Outside", 42.1f, 0f);
                MutinyCharacter rightTarget = rightTargetObject.GetComponent<MutinyCharacter>();
                MutinyCharacter leftTarget = leftTargetObject.GetComponent<MutinyCharacter>();
                MutinyCharacter edgeTarget = edgeTargetObject.GetComponent<MutinyCharacter>();
                MutinyCharacter outsideTarget = outsideTargetObject.GetComponent<MutinyCharacter>();
                team.RegisterCharacter(rightTarget);
                team.RegisterCharacter(leftTarget);
                team.RegisterCharacter(edgeTarget);
                team.RegisterCharacter(outsideTarget);

                inputObject = new GameObject("BoulderVerification_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = manager;
                bool selected = input.SelectWeapon("boulder");
                bool launched = input.TryLaunchWeaponForVerification(owner, Vector2.zero, new Vector2(-100f, 0f));
                MutinyBoulder boulder = Object.FindAnyObjectByType<MutinyBoulder>();
                bool originalBoulderLayersLoaded =
                    Resources.Load<Sprite>("Art/Weapons/Boulder/rotating") != null &&
                    Resources.Load<Sprite>("Art/Weapons/Boulder/overlay") != null &&
                    boulder != null && boulder.RotatingVisual != null &&
                    boulder.RotatingVisual.GetComponent<SpriteRenderer>() != null &&
                    boulder.SpriteRenderer != null &&
                    boulder.RotatingVisual.GetComponent<SpriteRenderer>().sortingOrder == MutinyWeapon.WeaponSortingOrder &&
                    boulder.SpriteRenderer.sortingOrder == MutinyWeapon.WeaponSortingOrder + 1;
                result.Assert(originalBoulderLayersLoaded,
                    "WPN-04-ANI-01 production Boulder loads original rotating depth-1 art and non-rotating depth-3 overlay separately");

                Sprite rotatingSprite = Resources.Load<Sprite>("Art/Weapons/Boulder/rotating");
                Sprite overlaySprite = Resources.Load<Sprite>("Art/Weapons/Boulder/overlay");
                bool originalBoulderScaleAndRegistration =
                    rotatingSprite != null && overlaySprite != null && boulder != null &&
                    Mathf.Approximately(rotatingSprite.rect.width, 64f) &&
                    Mathf.Approximately(overlaySprite.rect.width, 64f) &&
                    Mathf.Approximately(rotatingSprite.pixelsPerUnit, MutinyPhysics.PixelsPerUnit) &&
                    Mathf.Approximately(overlaySprite.pixelsPerUnit, MutinyPhysics.PixelsPerUnit) &&
                    Vector2.Distance(rotatingSprite.pivot, new Vector2(32f, 32f)) < 0.01f &&
                    Vector2.Distance(overlaySprite.pivot, new Vector2(32f, 32f)) < 0.01f &&
                    Mathf.Approximately(rotatingSprite.bounds.size.x, 2f) &&
                    Mathf.Approximately(rotatingSprite.bounds.size.y, 2f) &&
                    Mathf.Approximately(overlaySprite.bounds.size.x, 2f) &&
                    Mathf.Approximately(overlaySprite.bounds.size.y, 2f) &&
                    boulder.RotatingVisual.GetComponent<SpriteRenderer>().sprite == rotatingSprite &&
                    boulder.SpriteRenderer.sprite == overlaySprite;
                result.Assert(originalBoulderScaleAndRegistration,
                    "WPN-04-ANI-02 production Boulder keeps both 64px source layers centered and at the 32px-per-unit simulation scale");
                result.Assert(selected && launched && boulder != null && boulder.IsFired &&
                              Mathf.Approximately(boulder.PhysicsBody.State.VelocityX, 20f) &&
                              Mathf.Approximately(boulder.PhysicsBody.State.VelocityY, 0f) &&
                              Mathf.Approximately(boulder.PhysicsBody.State.LeftExtent, MutinyBoulder.ExtentPixels) &&
                              Mathf.Approximately(boulder.PhysicsBody.State.TopExtent, MutinyBoulder.ExtentPixels) &&
                              Mathf.Approximately(boulder.PhysicsBody.State.Weight, MutinyBoulder.WeightPerTick) &&
                              Mathf.Approximately(boulder.PhysicsBody.State.Friction, MutinyBoulder.FrictionPerFloorContact) &&
                              boulder.PhysicsBody.State.HitsBoxes && !owner.HasWeapon("boulder") &&
                              !owner.CanThrow && !owner.CanShoot && manager.CurrentPhase == TurnPhase.ActionExecuting,
                    "WPN-04-INT-01/EFF-01 production twang clamps at 20 without an invented Boulder .5 multiplier and retains original Solid parameters");

                PhysicsBodyState boulderState = boulder.PhysicsBody.State;
                boulderState.X = 0f;
                boulderState.Y = 0f;
                boulderState.VelocityX = 10f;
                // The normal 1.5 weight makes post-motion vy exactly zero, while
                // preserving a real Solid tick instead of invoking Boulder logic
                // directly.
                boulderState.VelocityY = -MutinyBoulder.WeightPerTick;
                boulder.PhysicsBody.State = boulderState;
                boulder.AdvanceOriginalTickForVerification();
                float rootRotation = Mathf.DeltaAngle(0f, boulder.transform.eulerAngles.z);
                float boulderRotation = boulder.RotatingVisual == null
                    ? 0f
                    : Mathf.DeltaAngle(0f, boulder.RotatingVisual.eulerAngles.z);
                result.Assert(Mathf.Approximately(rightTarget.PhysicsBody.State.X, 42f) &&
                              Mathf.Approximately(rightTarget.PhysicsBody.State.VelocityX, 10f) &&
                              Mathf.Approximately(leftTarget.PhysicsBody.State.X, -22f) &&
                              Mathf.Approximately(leftTarget.PhysicsBody.State.VelocityX, 0f) &&
                              Mathf.Approximately(edgeTarget.PhysicsBody.State.X, 42f) &&
                              Mathf.Approximately(outsideTarget.PhysicsBody.State.X, 42.1f) &&
                              Mathf.Approximately(rightTarget.Health, 85f) &&
                              Mathf.Approximately(leftTarget.Health, 85f) &&
                              Mathf.Approximately(edgeTarget.Health, 85f) &&
                              Mathf.Approximately(outsideTarget.Health, 100f) &&
                              Mathf.Approximately(rootRotation, 0f) &&
                              Mathf.Approximately(boulderRotation, -25f),
                    "WPN-04-EFF-01/02 production Boulder advances Solid before inclusive 32px contacts, then applies directional shove, abs(vx)*1.5 damage, and vx*2.5 only to its rotating child layer");

                boulderState = boulder.PhysicsBody.State;
                boulderState.X = 100f;
                boulderState.Y = 19f;
                boulderState.VelocityX = 0f;
                boulderState.VelocityY = 0f;
                boulder.PhysicsBody.State = boulderState;
                boulder.PhysicsBody.WaterPixelY = 20f;
                boulder.AdvanceOriginalTickForVerification();
                result.Assert(!boulder.IsOverWater && !boulder.PhysicsBody.ApplyWaterPhysics &&
                              Mathf.Approximately(boulder.PhysicsBody.State.VelocityY, 1.5f),
                    "WPN-04-EFF-03 production Boulder crosses water with a splash state change but keeps original Solid velocity without character water drag");

                GameObject bottomBoulderObject = new GameObject("BoulderVerification_MapBottom");
                MutinyBoulder bottomBoulder = bottomBoulderObject.AddComponent<MutinyBoulder>();
                bottomBoulder.Initialize(owner);
                bottomBoulder.Fire(Vector2.zero);
                string[,] bottomTerrain = new string[3, 1];
                for (int row = 0; row < 3; row++)
                    bottomTerrain[row, 0] = "-";
                bottomBoulder.PhysicsBody.SetTerrain(bottomTerrain, 1, 3);
                PhysicsBodyState bottomState = bottomBoulder.PhysicsBody.State;
                bottomState.X = 100f;
                bottomState.Y = 95f;
                bottomState.VelocityX = 0f;
                bottomState.VelocityY = 0f;
                bottomBoulder.PhysicsBody.State = bottomState;
                bottomBoulder.AdvanceOriginalTickForVerification();
                result.Assert(bottomBoulder.IsFinished,
                    "WPN-04-EFF-03 production Boulder finishes after a downward tick crosses tileSystem levelHeight");

                boulderState = boulder.PhysicsBody.State;
                boulderState.X = 100f;
                boulderState.Y = 0f;
                boulderState.VelocityX = 0f;
                boulderState.VelocityY = -MutinyBoulder.WeightPerTick;
                boulder.PhysicsBody.State = boulderState;
                boulder.AdvanceOriginalTickForVerification();
                result.Assert(Mathf.Approximately(boulder.Visibility, 1.9f) && boulder.IsFinished,
                    "WPN-04-ANI-01 production Boulder decrements visibility at the .5 threshold, then inherits Weapon's same-tick .2 rest finish");
            }
            finally
            {
                DestroyNow(inputObject);
                DestroyNow(outsideTargetObject);
                DestroyNow(edgeTargetObject);
                DestroyNow(leftTargetObject);
                DestroyNow(rightTargetObject);
                DestroyNow(ownerObject);
                DestroyNow(managerObject);
                DestroyNow(teamObject);
                foreach (MutinyBoulder boulder in Object.FindObjectsByType<MutinyBoulder>(FindObjectsSortMode.None))
                    DestroyNow(boulder != null ? boulder.gameObject : null);
            }
        }

        private static GameObject CreateBoulderCharacter(string name, float x, float y)
        {
            GameObject characterObject = new GameObject(name);
            MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
            PhysicsBodyState state = PhysicsBodyState.CreateDefault(x, y);
            state.Weight = 0f;
            character.PhysicsBody.State = state;
            return characterObject;
        }

        private static void VerifyAnchor(MutinyLevel1VerificationResult result)
        {
            GameObject teamObject = null;
            GameObject managerObject = null;
            GameObject ownerObject = null;
            GameObject insideTargetObject = null;
            GameObject horizontalBoundaryTargetObject = null;
            GameObject verticalBoundaryTargetObject = null;
            GameObject inputObject = null;
            try
            {
                teamObject = new GameObject("AnchorVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                team.TeamNumber = 1;

                managerObject = new GameObject("AnchorVerification_TurnManager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;

                ownerObject = new GameObject("AnchorVerification_Owner");
                MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();
                owner.TeamIndex = 1;
                owner.AddWeapon("anchor");
                PhysicsBodyState ownerState = PhysicsBodyState.CreateDefault(256f, 96f);
                ownerState.Weight = 0f;
                owner.PhysicsBody.State = ownerState;
                team.RegisterCharacter(owner);
                team.SelectCharacter(owner);

                insideTargetObject = CreateAnchorTarget("AnchorVerification_Inside", 96f, 64f);
                horizontalBoundaryTargetObject = CreateAnchorTarget("AnchorVerification_HorizontalBoundary", 144f, 64f);
                verticalBoundaryTargetObject = CreateAnchorTarget("AnchorVerification_VerticalBoundary", 96f, 31.9f);
                MutinyCharacter insideTarget = insideTargetObject.GetComponent<MutinyCharacter>();
                MutinyCharacter horizontalBoundaryTarget = horizontalBoundaryTargetObject.GetComponent<MutinyCharacter>();
                MutinyCharacter verticalBoundaryTarget = verticalBoundaryTargetObject.GetComponent<MutinyCharacter>();

                inputObject = new GameObject("AnchorVerification_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = manager;
                bool selected = input.SelectWeapon("anchor");
                MutinyAnchor anchor = input.ArmedAnchor;
                input.UpdateSpecialWeaponCursorForVerification(owner, Vector2.zero, false);
                result.Assert(input.SpecialWeaponCursorModeForVerification == "Anchor",
                    "ANC-CUR-01 production selected Anchor shows its special cursor before placement");
                bool committed = input.TryActivateClickWeaponForVerification(owner, new Vector2(96f, 300f));
                input.UpdateSpecialWeaponCursorForVerification(owner, Vector2.zero, false);
                result.Assert(input.SpecialWeaponCursorModeForVerification == "None",
                    "ANC-CUR-01 production Anchor placement restores the ordinary cursor");
                result.Assert(selected && committed && anchor != null && anchor.IsFired &&
                              Mathf.Approximately(anchor.PhysicsBody.State.X, 96f) &&
                              Mathf.Approximately(anchor.PhysicsBody.State.Y, MutinyAnchor.DropStartYPixels) &&
                              Mathf.Approximately(anchor.PhysicsBody.State.VelocityX, 0f) &&
                              Mathf.Approximately(anchor.PhysicsBody.State.VelocityY, MutinyAnchor.DropSpeedPixelsPerTick) &&
                              !owner.HasWeapon("anchor") && !owner.CanThrow && !owner.CanShoot &&
                              manager.CurrentPhase == TurnPhase.ActionExecuting,
                    "WPN-15-INT-01/02 production selection consumes once and drops at raw click x with forced y=-200");

                Sprite fallingSprite = anchor.SpriteRenderer.sprite;
                anchor.AdvanceOriginalTickForVerification();
                result.Assert(anchor.CurrentAnimationFrame == 1 && anchor.CurrentImpactFrame == 0 &&
                              !anchor.AreImpactParticlesVisible &&
                              anchor.SpriteRenderer.sprite == fallingSprite &&
                              Mathf.Approximately(anchor.PhysicsBody.State.Y, -160f),
                    "ANC-ANI-02 falling anchor retains stopped main frame 1 with no impact children");

                string[,] terrain = new string[6, 12];
                for (int r = 0; r < 6; r++)
                    for (int c = 0; c < 12; c++)
                        terrain[r, c] = "-";
                for (int c = 0; c < 12; c++)
                    terrain[3, c] = "ground";
                anchor.PhysicsBody.SetTerrain(terrain, 12, 6);

                // Start one tick above row 3. The production Anchor tick sets vy=40
                // and invokes its own Solid step; that must trigger the floor path.
                PhysicsBodyState anchorState = anchor.PhysicsBody.State;
                anchorState.X = 96f;
                anchorState.Y = 80f;
                anchorState.VelocityX = 0f;
                anchorState.VelocityY = 0f;
                anchor.PhysicsBody.State = anchorState;
                anchor.AdvanceOriginalTickForVerification();
                result.Assert(anchor.HasHitBottom && Mathf.Approximately(anchor.PhysicsBody.State.Y, 95.9f) &&
                              Mathf.Approximately(anchor.PhysicsBody.State.VelocityY, 0f) &&
                              Mathf.Approximately(insideTarget.Health, 40f) &&
                              Mathf.Approximately(horizontalBoundaryTarget.Health, 100f) &&
                              Mathf.Approximately(verticalBoundaryTarget.Health, 100f),
                    "WPN-15-EFF-01/02 production floor contact keeps the 48/96/0 Solid and applies 60 HP only inside strict anchor bounds");

                anchor.AdvanceOriginalTickForVerification();
                bool secondFrameHasNoParticles = anchor.CurrentAnimationFrame == 2 &&
                    anchor.CurrentImpactFrame == 0 && !anchor.AreImpactParticlesVisible;
                anchor.AdvanceOriginalTickForVerification();
                Transform leftImpact = anchor.transform.Find("AnchorImpact_Left");
                Transform rightImpact = anchor.transform.Find("AnchorImpact_Right");
                SpriteRenderer leftRenderer = leftImpact != null ? leftImpact.GetComponent<SpriteRenderer>() : null;
                SpriteRenderer rightRenderer = rightImpact != null ? rightImpact.GetComponent<SpriteRenderer>() : null;
                bool thirdFrameStartsMirroredChildren = anchor.CurrentAnimationFrame == 3 &&
                    anchor.CurrentImpactFrame == 1 && anchor.AreImpactParticlesVisible &&
                    anchor.SpriteRenderer.sprite == fallingSprite &&
                    leftRenderer != null && rightRenderer != null &&
                    leftRenderer.flipX && !rightRenderer.flipX &&
                    Mathf.Approximately(leftImpact.localPosition.x, -25f / MutinyPhysics.PixelsPerUnit) &&
                    Mathf.Approximately(rightImpact.localPosition.x, 25f / MutinyPhysics.PixelsPerUnit) &&
                    Mathf.Approximately(leftImpact.localPosition.y, 12f / MutinyPhysics.PixelsPerUnit) &&
                    Mathf.Approximately(rightImpact.localPosition.y, 12f / MutinyPhysics.PixelsPerUnit) &&
                    leftRenderer.sprite == rightRenderer.sprite &&
                    Mathf.Approximately(leftRenderer.color.a, 166f / 256f);
                Sprite firstImpactSprite = leftRenderer != null ? leftRenderer.sprite : null;
                for (int tick = 0; tick < 9; tick++)
                    anchor.AdvanceOriginalTickForVerification();
                bool childContinuesAfterMainStops = anchor.CurrentAnimationFrame == 12 &&
                    anchor.CurrentImpactFrame == 10 && anchor.AreImpactParticlesVisible &&
                    leftRenderer != null && leftRenderer.sprite != firstImpactSprite;
                for (int tick = 0; tick < 6; tick++)
                    anchor.AdvanceOriginalTickForVerification();
                bool childStillVisibleAtSixteen = anchor.CurrentAnimationFrame == 12 &&
                    anchor.CurrentImpactFrame == 16 && anchor.AreImpactParticlesVisible;
                anchor.AdvanceOriginalTickForVerification();
                bool childRemovedAtSeventeen = anchor.CurrentImpactFrame == 17 &&
                    !anchor.AreImpactParticlesVisible;
                for (int tick = 0; tick < 11; tick++)
                    anchor.AdvanceOriginalTickForVerification();
                result.Assert(secondFrameHasNoParticles && thirdFrameStartsMirroredChildren &&
                              childContinuesAfterMainStops && childStillVisibleAtSixteen &&
                              childRemovedAtSeventeen,
                    "ANC-ANI-02 production anchor spawns mirrored child timelines at main frame 3 and removes their contents at child frame 17");
                result.Assert(anchor.HoldTicksRemaining == 0 && anchor.FadeTicksRemaining == MutinyAnchor.FadeTicks &&
                              anchor.CurrentAnimationFrame == 12,
                    "WPN-15-ANI-01 production anchor reaches the stopped twelfth frame during the original 30-tick hold");

                anchor.AdvanceOriginalTickForVerification();
                Material whiteOut = anchor.SpriteRenderer.sharedMaterial;
                bool brighteningStarts = whiteOut != null &&
                    whiteOut.shader.name == "Mutiny/SpriteColorTransform" &&
                    Mathf.Approximately(whiteOut.GetFloat("_ColorMultiplier"), 0.8f) &&
                    Mathf.Approximately(whiteOut.GetFloat("_ColorAdd"), 0.2f) &&
                    Mathf.Approximately(whiteOut.GetFloat("_AlphaMultiplier"), 1f);
                for (int tick = 0; tick < 4; tick++)
                    anchor.AdvanceOriginalTickForVerification();
                bool whiteMidpoint = Mathf.Approximately(whiteOut.GetFloat("_ColorMultiplier"), 0f) &&
                    Mathf.Approximately(whiteOut.GetFloat("_ColorAdd"), 1f) &&
                    Mathf.Approximately(whiteOut.GetFloat("_AlphaMultiplier"), 1f);
                for (int tick = 0; tick < 4; tick++)
                    anchor.AdvanceOriginalTickForVerification();
                bool fadingWhite = Mathf.Approximately(whiteOut.GetFloat("_AlphaMultiplier"), 0.2f);
                anchor.AdvanceOriginalTickForVerification();
                bool fullyTransparentAtTenthTick = Mathf.Approximately(whiteOut.GetFloat("_AlphaMultiplier"), 0f);
                result.Assert(brighteningStarts && whiteMidpoint && fadingWhite && fullyTransparentAtTenthTick,
                    "ANC-ANI-03 production whiteOut brightens RGB, then fades white alpha over ten ticks");
                anchor.AdvanceOriginalTickForVerification();
                anchor.AdvanceOriginalTickForVerification();
                result.Assert(anchor.IsFinished,
                    "WPN-15-ANI-01 production anchor runs the original 10 white-out ticks then finishes");
            }
            finally
            {
                DestroyNow(inputObject);
                DestroyNow(verticalBoundaryTargetObject);
                DestroyNow(horizontalBoundaryTargetObject);
                DestroyNow(insideTargetObject);
                DestroyNow(ownerObject);
                DestroyNow(managerObject);
                DestroyNow(teamObject);
                foreach (MutinyAnchor anchor in Object.FindObjectsByType<MutinyAnchor>(FindObjectsSortMode.None))
                    DestroyNow(anchor != null ? anchor.gameObject : null);
            }
        }

        private static GameObject CreateAnchorTarget(string name, float x, float y)
        {
            GameObject targetObject = new GameObject(name);
            MutinyCharacter target = targetObject.AddComponent<MutinyCharacter>();
            PhysicsBodyState state = PhysicsBodyState.CreateDefault(x, y);
            state.Weight = 0f;
            target.PhysicsBody.State = state;
            return targetObject;
        }

        private static void VerifyCharacterLayering(MutinyLevel1VerificationResult result)
        {
            GameObject levelObject = null;
            try
            {
                // Build through the same level production entry that preserves
                // TileSystem's XML object order. Both bodies intentionally share
                // a position, so equal sorting orders would reproduce the flicker.
                var level = new MutinyLevelData
                {
                    Name = "CharacterLayeringVerification",
                    Width = 2,
                    Height = 2,
                    Players = 2,
                    Terrain = new string[2, 2],
                    Background = new string[2, 2]
                };
                level.Objects.Add(new MutinyLevelObject
                {
                    Type = "redPirate",
                    X = 0,
                    Y = 0,
                    Properties = new Dictionary<string, string>()
                });
                level.Objects.Add(new MutinyLevelObject
                {
                    Type = "cabinBoy",
                    X = 0,
                    Y = 0,
                    Properties = new Dictionary<string, string>()
                });

                levelObject = MutinyLevelBuilder.BuildLevel(level);
                MutinyLevelRoot root = levelObject.GetComponent<MutinyLevelRoot>();
                MutinyCharacter first = root != null && root.Characters.Count > 0 ? root.Characters[0] : null;
                MutinyCharacter second = root != null && root.Characters.Count > 1 ? root.Characters[1] : null;
                SpriteRenderer firstBody = first != null ? first.GetComponent<SpriteRenderer>() : null;
                SpriteRenderer secondBody = second != null ? second.GetComponent<SpriteRenderer>() : null;
                SpriteRenderer[] firstRenderers = first != null
                    ? first.GetComponentsInChildren<SpriteRenderer>(true)
                    : System.Array.Empty<SpriteRenderer>();

                bool firstSlotMatchesOriginalCreationOrder = firstBody != null &&
                    firstBody.sortingOrder == MutinyLevelBuilder.GetCharacterSortingOrder(0);
                bool secondSlotIsStableAndAboveFirst = secondBody != null && firstBody != null &&
                    secondBody.sortingOrder == MutinyLevelBuilder.GetCharacterSortingOrder(1) &&
                    secondBody.sortingOrder - firstBody.sortingOrder == MutinyLevelBuilder.CharacterSortingStride;
                bool firstOverlayStaysInsideItsHolderSlot = firstBody != null && secondBody != null &&
                    firstRenderers.Length > 1;
                bool healthBarLayeringValid = false;
                for (int i = 0; i < firstRenderers.Length; i++)
                {
                    SpriteRenderer renderer = firstRenderers[i];
                    if (renderer != null && renderer != firstBody)
                    {
                        if (renderer.gameObject.name == "OriginalFrame" || renderer.gameObject.name == "DiscreteFill")
                        {
                            healthBarLayeringValid =
                                renderer.sortingOrder > MutinyWeapon.WeaponSortingOrder &&
                                renderer.sortingOrder > MutinyLevelBuilder.TerrainSortingOrder &&
                                renderer.sortingOrder < MutinyLevelBuilder.TidalWaveSortingOrder &&
                                renderer.sortingOrder < MutinyLevelBuilder.SeagullSortingOrder;
                        }
                        else
                        {
                            firstOverlayStaysInsideItsHolderSlot &= renderer.sortingOrder > firstBody.sortingOrder &&
                                                                renderer.sortingOrder < secondBody.sortingOrder;
                        }

                    }
                }

                int maxInitialCharacterOrder =
                    MutinyLevelBuilder.GetCharacterSortingOrder(17) +
                    MutinyLevelBuilder.CharacterOverlaySortingOffset + 5;
                bool laterCharacterLayerContentStaysAboveInitialCharacters =
                    MutinyWeapon.WeaponSortingOrder > maxInitialCharacterOrder &&
                    MutinyExplosion.ExplosionSortingOrder > MutinyWeapon.WeaponSortingOrder &&
                    MutinyLevelBuilder.WaterSortingOrder > MutinyExplosion.ExplosionSortingOrder;
                bool tidalWaveAndSeagullLayeringValid =
                    MutinyLevelBuilder.TidalWaveSortingOrder > MutinyWeapon.WeaponSortingOrder &&
                    MutinyLevelBuilder.TidalWaveSortingOrder > MutinyLevelBuilder.TerrainSortingOrder &&
                    MutinyLevelBuilder.SeagullSortingOrder > MutinyWeapon.WeaponSortingOrder &&
                    MutinyLevelBuilder.SeagullSortingOrder > MutinyLevelBuilder.TerrainSortingOrder &&
                    MutinyLevelBuilder.WaterSortingOrder > MutinyLevelBuilder.TidalWaveSortingOrder &&
                    MutinyLevelBuilder.WaterSortingOrder > MutinyLevelBuilder.SeagullSortingOrder;

                result.Assert(firstSlotMatchesOriginalCreationOrder && secondSlotIsStableAndAboveFirst &&
                              firstOverlayStaysInsideItsHolderSlot && healthBarLayeringValid &&
                              tidalWaveAndSeagullLayeringValid && laterCharacterLayerContentStaysAboveInitialCharacters,
                    "CHAR-LAYER-01/02/03 production level assigns unique XML-order character holder slots, keeps overlay inside each slot, and layers dynamic content/water above every initial character");
            }
            finally
            {
                DestroyNow(levelObject);
            }
        }

        private static void VerifyCharacterOverlay(MutinyLevel1VerificationResult result)
        {
            GameObject characterObject = null;
            GameObject teamObject = null;
            GameObject turnManagerObject = null;
            GameObject playerInputObject = null;
            try
            {
                bool resourcesPresent =
                    Resources.Load<Texture2D>("UI/CharacterOverlay/p1_indicator") != null &&
                    Resources.Load<Texture2D>("UI/CharacterOverlay/p2_indicator") != null &&
                    Resources.Load<Texture2D>("UI/CharacterOverlay/cpu_indicator") != null &&
                    Resources.Load<Texture2D>("UI/CharacterOverlay/health_background") != null;
                result.Assert(resourcesPresent,
                    "CHAR-OVR-01 original CPU/P1/P2 and health-frame textures load through Resources");

                characterObject = new GameObject("CharacterOverlayVerification");
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                MutinyCharacterOverlay overlay = characterObject.AddComponent<MutinyCharacterOverlay>();
                Transform overlayRoot = characterObject.transform.Find("OriginalCharacterOverlay");
                Transform indicator = overlayRoot != null ? overlayRoot.Find("TurnIndicator") : null;
                Transform healthBar = overlayRoot != null ? overlayRoot.Find("HealthBar") : null;
                SpriteRenderer indicatorRenderer = indicator != null ? indicator.GetComponent<SpriteRenderer>() : null;
                result.Assert(overlayRoot != null && overlayRoot.localPosition == Vector3.zero,
                    "CHAR-OVR-POS-01 runtime overlay is attached to the character registration point without the SWF preview offset");
                result.Assert(indicator != null &&
                              Mathf.Approximately(indicator.localPosition.y,
                                  -MutinyCharacterOverlay.OriginalIndicatorTopY / MutinyPhysics.PixelsPerUnit) &&
                              indicatorRenderer != null && indicatorRenderer.sprite != null &&
                              Mathf.Approximately(indicatorRenderer.sprite.pivot.y,
                                  indicatorRenderer.sprite.rect.height),
                    "CHAR-OVR-POS-02 turn marker uses the original -38.05px top registration");
                result.Assert(healthBar != null &&
                              Mathf.Approximately(healthBar.localPosition.y,
                                  -MutinyCharacterOverlay.OriginalHealthCenterY / MutinyPhysics.PixelsPerUnit),
                    "CHAR-OVR-POS-03 character health bar uses the original +18px source offset");
                characterObject.transform.position = new Vector3(3f, -2f, 0f);
                characterObject.transform.rotation = Quaternion.Euler(0f, 0f, 73f);
                overlay.RefreshVisualStateForVerification();
                result.Assert(overlayRoot != null &&
                              Quaternion.Angle(overlayRoot.rotation, Quaternion.identity) < 0.001f &&
                              Vector3.Distance(overlayRoot.position, characterObject.transform.position) < 0.001f,
                    "CHAR-OVR-POS-04 overlay follows character position while remaining upright during character rotation");
                characterObject.transform.rotation = Quaternion.identity;
                teamObject = new GameObject("CharacterOverlayVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                team.TeamNumber = 1;
                team.RegisterCharacter(character);
                turnManagerObject = new GameObject("CharacterOverlayVerification_TurnManager");
                MutinyTurnManager turnManager = turnManagerObject.AddComponent<MutinyTurnManager>();
                turnManager.CurrentTeam = team;
                turnManager.CurrentPhase = TurnPhase.TurnActive;
                PhysicsBodyState state = PhysicsBodyState.CreateDefault(64f, 64f);
                state.Weight = 0f;
                character.PhysicsBody.State = state;
                character.ShownHealth = 100f;
                character.MaxHealth = 100f;
                overlay.RefreshVisualStateForVerification();
                result.Assert(overlay.IsHealthBarVisible && overlay.CurrentHealthFrame == 28,
                    "CHAR-OVR-04 production overlay maps 100 shown health to original health frame 28");
                result.Assert(overlay.IsTurnIndicatorVisible,
                    "CHAR-OVR-02 production overlay displays the current team's static turn marker");

                turnManager.CurrentTeam = null;
                overlay.RefreshVisualStateForVerification();
                result.Assert(!overlay.IsTurnIndicatorVisible && overlay.IsHealthBarVisible,
                    "CHAR-OVR-02/03 production overlay hides only the marker for a non-current static team");
                turnManager.CurrentTeam = team;

                character.ShownHealth = 50f;
                overlay.RefreshVisualStateForVerification();
                result.Assert(overlay.CurrentHealthFrame == 15,
                    "CHAR-OVR-04 production overlay maps shown health through ceil(27 * health / maxHealth)");

                state = character.PhysicsBody.State;
                state.VelocityX = 1f;
                character.PhysicsBody.State = state;
                overlay.RefreshVisualStateForVerification();
                result.Assert(overlay.IsHealthBarVisible && overlay.IsTurnIndicatorVisible,
                    "CHAR-OVR-02/03 production overlay keeps both elements visible during passive physics motion");

                state.VelocityX = 0f;
                character.PhysicsBody.State = state;
                team.SelectCharacter(character);
                playerInputObject = new GameObject("CharacterOverlayVerification_PlayerInput");
                MutinyPlayerInput playerInput = playerInputObject.AddComponent<MutinyPlayerInput>();
                playerInput.TurnManager = turnManager;
                bool selfThrowCommitted = playerInput.TryCommitCharacterThrow(
                    character, new Vector2(64f, 64f), new Vector2(84f, 64f));
                overlay.RefreshVisualStateForVerification();
                result.Assert(selfThrowCommitted && character.IsSelfThrown &&
                              !overlay.IsHealthBarVisible && !overlay.IsTurnIndicatorVisible,
                    "CHAR-OVR-02/03 production self-throw entry hides both elements without treating passive motion as a throw");

                team.ContinueSelectedCharacterAfterAction();
                overlay.RefreshVisualStateForVerification();
                result.Assert(!character.IsSelfThrown && overlay.IsHealthBarVisible && overlay.IsTurnIndicatorVisible,
                    "CHAR-OVR-02/03 production continuation clears self-throw overlay suppression");

                character.TakeDamage(100f);
                result.Assert(!overlay.IsHealthBarVisible && !overlay.IsTurnIndicatorVisible,
                    "CHAR-OVR-03 production death event hides the complete character overlay");
            }
            finally
            {
                DestroyNow(characterObject);
                DestroyNow(teamObject);
                DestroyNow(turnManagerObject);
                DestroyNow(playerInputObject);
            }
        }

        private static void VerifyAiDecisionFlow(MutinyLevel1VerificationResult result)
        {
            GameObject aiTeamObject = null;
            GameObject enemyTeamObject = null;
            GameObject aiCharacterObject = null;
            GameObject enemyCharacterObject = null;

            try
            {
                aiTeamObject = new GameObject("AiParityVerification_Team");
                MutinyTeam aiTeam = aiTeamObject.AddComponent<MutinyTeam>();
                aiTeam.TeamNumber = 2;
                aiTeam.IsAiControlled = true;
                MutinyAIController ai = aiTeamObject.AddComponent<MutinyAIController>();
                ai.UseFixedDecisionSeed = true;
                ai.FixedDecisionSeed = 24680;
                ai.SaveDecisionTrace = false;

                enemyTeamObject = new GameObject("AiParityVerification_EnemyTeam");
                MutinyTeam enemyTeam = enemyTeamObject.AddComponent<MutinyTeam>();
                enemyTeam.TeamNumber = 1;

                aiCharacterObject = new GameObject("AiParityVerification_Pirate");
                MutinyCharacter pirate = aiCharacterObject.AddComponent<MutinyCharacter>();
                pirate.TeamIndex = 2;
                pirate.CanThrow = true;
                pirate.CanShoot = true;
                PhysicsBodyState pirateState = PhysicsBodyState.CreateDefault(64f, 100f);
                pirate.PhysicsBody.State = pirateState;
                pirate.PhysicsBody.WaterPixelY = 0f;
                pirate.PhysicsBody.SetTerrain(null, 0, 0);
                aiTeam.RegisterCharacter(pirate);

                enemyCharacterObject = new GameObject("AiParityVerification_Enemy");
                MutinyCharacter enemy = enemyCharacterObject.AddComponent<MutinyCharacter>();
                enemy.TeamIndex = 1;
                PhysicsBodyState enemyState = PhysicsBodyState.CreateDefault(400f, 100f);
                enemy.PhysicsBody.State = enemyState;
                enemy.PhysicsBody.WaterPixelY = 0f;
                enemy.PhysicsBody.SetTerrain(null, 0, 0);
                enemyTeam.RegisterCharacter(enemy);

                // The water penalty makes every sampled jump unattractive.  The real
                // production evaluator must nevertheless choose the best of its 50
                // samples during phase one (AI-SEL-02 / AI-MOVE-01).
                AIMove firstAction = ai.EvaluateBestMove();
                result.Assert(firstAction.MoveType == AIMoveType.SelfThrow &&
                              firstAction.Character == pirate && firstAction.Score <= 0f,
                    "AI-SEL-02 production evaluator keeps and selects a non-positive first-phase jump candidate");

                MutinyAIDecisionTrace recorded = ai.LastDecisionTrace;
                AIMove steppedAction = default;
                int decisionSteps = 0;
                var decisionIterator = ai.EvaluateBestMoveStepsForVerification(move => steppedAction = move);
                while (decisionIterator.MoveNext())
                    decisionSteps++;
                result.Assert(decisionSteps >= 51 && recorded != null &&
                              recorded.Candidates.Count == 50 &&
                              steppedAction.MoveType == firstAction.MoveType &&
                              Mathf.Approximately(steppedAction.Score, firstAction.Score) &&
                              Vector2.Distance(steppedAction.LaunchVelocity, firstAction.LaunchVelocity) < 0.001f,
                    "AI-SLICE-01 production decision iterator preserves the fixed-seed result across more than 50 resumable steps");
                result.Assert(!MutinyAIController.ShouldYieldDecisionFrame(false, 29) &&
                              MutinyAIController.ShouldYieldDecisionFrame(false, 30) &&
                              MutinyAIController.ShouldYieldDecisionFrame(true, 0),
                    "AI-SLICE-01 production frame gate uses the original 30 ms threshold and a character boundary");

                ai.SetReplayTraceForVerification(recorded);
                AIMove replayedAction = ai.EvaluateBestMove();
                result.Assert(replayedAction.MoveType == firstAction.MoveType &&
                              Mathf.Approximately(replayedAction.Score, firstAction.Score) &&
                              Vector2.Distance(replayedAction.LaunchVelocity, firstAction.LaunchVelocity) < 0.001f &&
                              ai.LastDecisionTrace.Draws.Count == recorded.Draws.Count,
                    "AI-RNG-01 production decision replays every recorded draw and candidate on the same board");

                MutinyAIDecisionTrace altered = JsonUtility.FromJson<MutinyAIDecisionTrace>(
                    JsonUtility.ToJson(recorded));
                altered.Draws[0].Maximum += 1f;
                bool rejectedAlteredTrace = false;
                try
                {
                    ai.SetReplayTraceForVerification(altered);
                    ai.EvaluateBestMove();
                }
                catch (System.IO.InvalidDataException)
                {
                    rejectedAlteredTrace = true;
                }
                result.Assert(rejectedAlteredTrace,
                    "AI-RNG-01 replay rejects a draw whose recorded bound differs from the production call");

                pirate.CanThrow = false;
                pirate.CanShoot = true;
                aiTeam.SelectCharacter(pirate);
                AIMove continuation = ai.EvaluateBestMove();
                result.Assert(continuation.MoveType == AIMoveType.Pass,
                    "AI-SEL-03 production evaluator bails out only in the post-jump weapon continuation phase");

                Vector2 boulderVelocity = new Vector2(12f, -8f);
                PhysicsBodyState boulderPrediction = MutinyAIController.CreateWeaponSimulationForVerification(
                    new Vector2(64f, 100f), "boulder", boulderVelocity);
                pirate.AddWeapon("boulder");
                pirate.CanThrow = true;
                pirate.CanShoot = true;
                ai.ExecuteMoveForVerification(new AIMove
                {
                    MoveType = AIMoveType.ShootWeapon,
                    Character = pirate,
                    WeaponType = "boulder",
                    LaunchVelocity = boulderVelocity
                });
                MutinyBoulder aiBoulder = null;
                foreach (MutinyBoulder candidate in Object.FindObjectsByType<MutinyBoulder>(FindObjectsSortMode.None))
                {
                    if (candidate != null && candidate.Owner == pirate)
                    {
                        aiBoulder = candidate;
                        break;
                    }
                }
                result.Assert(Mathf.Approximately(boulderPrediction.VelocityX, 12f) &&
                              Mathf.Approximately(boulderPrediction.VelocityY, -8f) &&
                              aiBoulder != null && aiBoulder.IsFired &&
                              Mathf.Approximately(aiBoulder.PhysicsBody.State.VelocityX, 12f) &&
                              Mathf.Approximately(aiBoulder.PhysicsBody.State.VelocityY, -8f),
                    "WPN-04-INT-02 production AI prediction and aiPerform launch retain Boulder candidate velocity without a .5 multiplier");
            }
            finally
            {
                foreach (MutinyBoulder boulder in Object.FindObjectsByType<MutinyBoulder>(FindObjectsSortMode.None))
                    DestroyNow(boulder != null ? boulder.gameObject : null);
                DestroyNow(enemyCharacterObject);
                DestroyNow(aiCharacterObject);
                DestroyNow(enemyTeamObject);
                DestroyNow(aiTeamObject);
            }
        }

        private static void VerifyAiSpecialWeaponCandidates(MutinyLevel1VerificationResult result)
        {
            GameObject aiTeamObject = null;
            GameObject enemyTeamObject = null;
            GameObject shooterObject = null;
            GameObject enemyObject = null;

            try
            {
                aiTeamObject = new GameObject("AiAllWeaponsVerification_Team");
                MutinyTeam aiTeam = aiTeamObject.AddComponent<MutinyTeam>();
                aiTeam.TeamNumber = 2;
                aiTeam.IsAiControlled = true;
                MutinyAIController ai = aiTeamObject.AddComponent<MutinyAIController>();

                enemyTeamObject = new GameObject("AiAllWeaponsVerification_EnemyTeam");
                MutinyTeam enemyTeam = enemyTeamObject.AddComponent<MutinyTeam>();
                enemyTeam.TeamNumber = 1;

                shooterObject = new GameObject("AiAllWeaponsVerification_Shooter");
                MutinyCharacter shooter = shooterObject.AddComponent<MutinyCharacter>();
                shooter.TeamIndex = 2;
                shooter.Luck = 30f;
                PhysicsBodyState shooterState = PhysicsBodyState.CreateDefault(48f, 16f);
                shooterState.Weight = 0f;
                shooter.PhysicsBody.State = shooterState;
                shooter.PhysicsBody.WaterPixelY = 192f;
                aiTeam.RegisterCharacter(shooter);

                enemyObject = new GameObject("AiAllWeaponsVerification_Enemy");
                MutinyCharacter enemy = enemyObject.AddComponent<MutinyCharacter>();
                enemy.TeamIndex = 1;
                PhysicsBodyState enemyState = PhysicsBodyState.CreateDefault(48f, 128f);
                enemyState.Weight = 0f;
                enemy.PhysicsBody.State = enemyState;
                enemyTeam.RegisterCharacter(enemy);

                var enemies = new List<MutinyCharacter> { enemy };
                var allies = new List<MutinyCharacter> { shooter };

                string[,] anchorTerrain = new string[6, 3];
                for (int row = 0; row < 6; row++)
                    for (int column = 0; column < 3; column++)
                        anchorTerrain[row, column] = "-";
                for (int column = 0; column < 3; column++)
                    anchorTerrain[4, column] = "ground";

                shooter.AddWeapon("anchor");
                UnityEngine.Random.InitState(15001);
                AIMove anchorMove = ai.EvaluateCharacterWeaponsForVerification(
                    shooter, enemies, allies, anchorTerrain, 3, 6, 192f, out int anchorCandidates);
                bool anchorSelected = anchorMove.MoveType == AIMoveType.ShootWeapon &&
                                      anchorMove.WeaponType == "anchor" &&
                                      anchorCandidates == 30 &&
                                      anchorMove.TargetPosition.x >= 0f &&
                                      anchorMove.TargetPosition.x < 96f;
                shooter.CanThrow = true;
                shooter.CanShoot = true;
                ai.ExecuteMoveForVerification(anchorMove);
                MutinyAnchor executedAnchor = null;
                foreach (MutinyAnchor candidate in Object.FindObjectsByType<MutinyAnchor>(FindObjectsSortMode.None))
                {
                    if (candidate != null && candidate.Owner == shooter && candidate.IsFired)
                    {
                        executedAnchor = candidate;
                        break;
                    }
                }
                result.Assert(anchorSelected && executedAnchor != null &&
                              Mathf.Approximately(executedAnchor.PhysicsBody.State.X, anchorMove.TargetPosition.x) &&
                              !shooter.HasWeapon("anchor"),
                    "AI-WPN-04 production Anchor routing samples full-level vertical drops, selects a floor candidate, and executes DropForAi");

                shooter.WeaponInventory.Clear();
                shooter.InfiniteWeapons.Clear();
                shooter.AddWeapon("woodenCrate");
                shooterState = shooter.PhysicsBody.State;
                shooterState.X = 9800f;
                shooterState.Y = 64f;
                shooter.PhysicsBody.State = shooterState;
                enemyState = enemy.PhysicsBody.State;
                enemyState.X = 10000f;
                enemyState.Y = 64f;
                enemy.PhysicsBody.State = enemyState;

                string[,] crateTerrain = new string[6, 320];
                for (int row = 0; row < 6; row++)
                    for (int column = 0; column < 320; column++)
                        crateTerrain[row, column] = "-";
                for (int column = 0; column < 320; column++)
                    crateTerrain[4, column] = "ground";

                UnityEngine.Random.InitState(14001);
                AIMove crateMove = ai.EvaluateCharacterWeaponsForVerification(
                    shooter, enemies, allies, crateTerrain, 320, 6, 192f, out int crateCandidates);
                bool crateSelected = crateMove.MoveType == AIMoveType.ShootWeapon &&
                                     crateMove.WeaponType == "woodenCrate" &&
                                     crateCandidates == 1 &&
                                     crateMove.BoxPossibilities != null &&
                                     crateMove.BoxPossibilities.Length >= 3;

                MutinyWoodenCrate crate = MutinyWeaponFactory.SpawnWeapon("woodenCrate", shooter) as MutinyWoodenCrate;
                crate?.PhysicsBody.SetTerrain(crateTerrain, 320, 6);
                bool crateArmed = crate != null && crate.BeginAiPlacement(crateMove.BoxPossibilities);
                for (int tick = 0; tick < MutinyWoodenCrate.AiPlaceDelayTicks; tick++)
                    crate?.AdvanceAiPlacementTickForVerification();
                result.Assert(crateSelected && crateArmed && crate.IsAiPlacementActive && crate.PlacedCount == 1,
                    "AI-WPN-05 production Wooden Crate routing admits BoxWeapon candidates and begins the original delayed three-crate AI sequence");
            }
            finally
            {
                foreach (MutinyAnchor anchor in Object.FindObjectsByType<MutinyAnchor>(FindObjectsSortMode.None))
                    if (anchor != null && anchor.Owner != null && anchor.Owner.name == "AiAllWeaponsVerification_Shooter")
                        DestroyNow(anchor.gameObject);
                foreach (MutinyWoodenCrate crate in Object.FindObjectsByType<MutinyWoodenCrate>(FindObjectsSortMode.None))
                    if (crate != null && crate.Owner != null && crate.Owner.name == "AiAllWeaponsVerification_Shooter")
                        DestroyNow(crate.gameObject);
                DestroyNow(enemyObject);
                DestroyNow(shooterObject);
                DestroyNow(enemyTeamObject);
                DestroyNow(aiTeamObject);
            }
        }

        private static void VerifyAiOriginalPredictionParity(MutinyLevel1VerificationResult result)
        {
            GameObject obstacleObject = null;
            GameObject cameraObject = null;
            GameObject cameraCharacterObject = null;
            GameObject formalOwnerObject = null;
            MutinyWeapon formalMine = null;
            MutinyParachuteBomb formalParachute = null;
            try
            {
                string[,] terrain = new string[10, 12];
                for (int row = 0; row < 10; row++)
                    for (int column = 0; column < 12; column++)
                        terrain[row, column] = "-";
                for (int column = 0; column < 12; column++)
                    terrain[5, column] = "ground";

                PhysicsBodyState rum = MutinyAIController.CreateWeaponSimulationForVerification(
                    new Vector2(160f, 64f), "rumBottle", new Vector2(0f, 20f));
                Vector2 rumImpact = MutinyAIController.SimulateWeaponImpactForVerification(
                    rum, "rumBottle", terrain, 12, 10, 320f, out int rumSteps);
                result.Assert(rumSteps < 101 && rumImpact.y < 160f,
                    "AI-PHY-01 Rum Bottle prediction uses its original first-contact simulationFinished condition");

                PhysicsBodyState boulder = MutinyAIController.CreateWeaponSimulationForVerification(
                    Vector2.zero, "boulder", new Vector2(1f, 0f));
                Vector2 boulderImpact = MutinyAIController.SimulateWeaponImpactForVerification(
                    boulder, "boulder", null, 12, 10, 40f, out int boulderSteps);
                result.Assert(boulderSteps == 101 && boulderImpact.y > 40f,
                    "AI-PHY-01 generic Weapon.randomThrows prediction preserves the original 101-step guard instead of stopping at water or 50 ticks");

                PhysicsBodyState longThrow = PhysicsBodyState.CreateDefault(32f, 0f);
                longThrow.VelocityY = -30f;
                longThrow.Friction = 2f;
                Vector2 longLanding = MutinyAIController.SimulateCharacterLandingForVerification(
                    longThrow, null, 12, 10, 4000f, out int longThrowSteps);
                result.Assert(longThrowSteps > 70 && longLanding.y >= 4000f,
                    "AI-PHY-02 Character.randomThrows prediction continues beyond the old 70-step cap until water");

                MutinyBoxRegistry.ResetForLevel();
                obstacleObject = new GameObject("AiPredictionParity_Box");
                MutinyPhysicsBody obstacle = obstacleObject.AddComponent<MutinyPhysicsBody>();
                PhysicsBodyState obstacleState = PhysicsBodyState.CreateDefault(160f, 100f);
                obstacleState.LeftExtent = obstacleState.RightExtent = 16f;
                obstacleState.TopExtent = obstacleState.BottomExtent = 16f;
                obstacle.State = obstacleState;
                MutinyBoxRegistry.Register(obstacle);

                PhysicsBodyState cherry = MutinyAIController.CreateWeaponSimulationForVerification(
                    new Vector2(64f, 100f), "cherryBomb", new Vector2(20f, 0f));
                Vector2 boxImpact = MutinyAIController.SimulateWeaponImpactForVerification(
                    cherry, "cherryBomb", null, 12, 10, float.PositiveInfinity, out int boxSteps);
                result.Assert(boxSteps < 101 && boxImpact.x < obstacleState.X - obstacleState.LeftExtent,
                    "AI-PHY-02 hitsBoxes weapon prediction collides with the shared production box registry");

                MutinyBoxRegistry.ResetForLevel();
                formalOwnerObject = new GameObject("AiPredictionParity_FormalOwner");
                MutinyCharacter formalOwner = formalOwnerObject.AddComponent<MutinyCharacter>();
                PhysicsBodyState ownerState = PhysicsBodyState.CreateDefault(80f, 0f);
                formalOwner.PhysicsBody.State = ownerState;
                formalOwner.PhysicsBody.SetTerrain(null, 0, 0);
                formalOwner.AddWeapon("mine");
                formalOwner.CanShoot = true;
                float healthBeforePrediction = formalOwner.Health;
                formalMine = MutinyWeaponFactory.SpawnWeapon("mine", formalOwner);
                formalMine.PhysicsBody.SetTerrain(null, 0, 0);
                PhysicsBodyState template = MutinyAIController.CreateFormalWeaponPredictionTemplateForVerification(
                    formalOwner, "mine");
                PhysicsBodyState actualTemplate = formalMine.PhysicsBody.State;
                bool sameInitialPhysics = template.Weight == actualTemplate.Weight &&
                                          template.Bounce == actualTemplate.Bounce &&
                                          template.Friction == actualTemplate.Friction &&
                                          template.LeftExtent == actualTemplate.LeftExtent &&
                                          template.BottomExtent == actualTemplate.BottomExtent &&
                                          template.HitsBoxes == actualTemplate.HitsBoxes &&
                                          formalOwner.HasWeapon("mine") && formalOwner.CanShoot &&
                                          Mathf.Approximately(formalOwner.Health, healthBeforePrediction);
                formalMine.Fire(new Vector2(10f, -5f));
                PhysicsBodyState predictedMine = formalMine.PhysicsBody.State;
                for (int tick = 0; tick < 3; tick++)
                {
                    MutinyPhysics.Step(ref predictedMine, null, 0, 0, null);
                    formalMine.PhysicsBody.AdvanceSimulationTick();
                }
                PhysicsBodyState actualMine = formalMine.PhysicsBody.State;
                result.Assert(sameInitialPhysics &&
                              Mathf.Abs(predictedMine.X - actualMine.X) < 0.001f &&
                              Mathf.Abs(predictedMine.Y - actualMine.Y) < 0.001f &&
                              Mathf.Abs(predictedMine.VelocityX - actualMine.VelocityX) < 0.001f &&
                              Mathf.Abs(predictedMine.VelocityY - actualMine.VelocityY) < 0.001f,
                    "AI-PHY-03 candidate template uses formal Mine initialization and matches three actual weapon ticks without collision");

                formalParachute = MutinyWeaponFactory.SpawnWeapon("parachuteBomb", formalOwner)
                    as MutinyParachuteBomb;
                formalParachute.PhysicsBody.SetTerrain(null, 0, 0);
                formalParachute.Fire(new Vector2(10f, -5f));
                PhysicsBodyState predictedParachute = formalParachute.PhysicsBody.State;
                for (int tick = 0; tick < 3; tick++)
                {
                    MutinyParachuteBomb.ApplyOriginalAirMotion(ref predictedParachute);
                    MutinyPhysics.Step(ref predictedParachute, null, 0, 0, null);
                    MutinyParachuteBomb.ApplyOriginalCeilingClamp(ref predictedParachute);
                    formalParachute.AdvanceOriginalTickForVerification(false, 0f);
                }
                PhysicsBodyState actualParachute = formalParachute.PhysicsBody.State;
                result.Assert(Mathf.Abs(predictedParachute.X - actualParachute.X) < 0.001f &&
                              Mathf.Abs(predictedParachute.Y - actualParachute.Y) < 0.001f &&
                              Mathf.Abs(predictedParachute.VelocityX - actualParachute.VelocityX) < 0.001f &&
                              Mathf.Abs(predictedParachute.VelocityY - actualParachute.VelocityY) < 0.001f,
                    "AI-PHY-03 Parachute prediction air motion and ceiling clamp match three production weapon ticks");

                result.Assert(
                    Mathf.Approximately(MutinyAIController.ScoreSeagullDistanceForVerification(20f, false), 0.5f) &&
                    Mathf.Approximately(MutinyAIController.ScoreSeagullDistanceForVerification(20f, true), -1f) &&
                    Mathf.Approximately(MutinyAIController.ScoreSeagullDistanceForVerification(40f, true), 0f),
                    "AI-WPN-06 Seagull uses distinct original enemy reward and ally penalty formulas");

                cameraObject = new GameObject("AiPredictionParity_Camera");
                cameraObject.AddComponent<Camera>();
                MutinyCameraController camera = cameraObject.AddComponent<MutinyCameraController>();
                cameraCharacterObject = new GameObject("AiPredictionParity_Winner");
                MutinyCharacter winner = cameraCharacterObject.AddComponent<MutinyCharacter>();
                result.Assert(MutinyAIController.BeginWinnerCameraPanForVerification(camera, winner) &&
                              camera.IsPanningToTurnTarget,
                    "AI-CAM-01 production AI winner path installs the selected character as the camera completion gate");
                result.Assert(
                    MutinyCameraController.ShouldPauseForAiThinking(true, true) &&
                    !MutinyCameraController.ShouldPauseForAiThinking(true, false) &&
                    !MutinyCameraController.ShouldPauseForAiThinking(false, true),
                    "AI-CAM-02 production camera gate pauses automatic branches only while the active AI is still evaluating candidates");
            }
            finally
            {
                MutinyBoxRegistry.ResetForLevel();
                DestroyNow(cameraCharacterObject);
                DestroyNow(cameraObject);
                DestroyNow(obstacleObject);
                DestroyNow(formalMine != null ? formalMine.gameObject : null);
                DestroyNow(formalParachute != null ? formalParachute.gameObject : null);
                DestroyNow(formalOwnerObject);
            }
        }

        private static void VerifyFrontendFlow(MutinyLevel1VerificationResult result)
        {
            result.Assert(Mathf.Approximately(MutinyBitmapFont.GetPirateGlyphTopOffset('A'), 0f) &&
                          Mathf.Approximately(MutinyBitmapFont.GetPirateGlyphTopOffset('K'), 0f) &&
                          Mathf.Approximately(MutinyBitmapFont.GetPirateGlyphTopOffset('R'), -1f) &&
                          Mathf.Approximately(MutinyBitmapFont.GetPirateGlyphTopOffset('P'), -1f) &&
                          Mathf.Approximately(MutinyBitmapFont.GetPirateGlyphTopOffset('N'), -2f),
                "FRONT-08 production PirateFont layout preserves original K/R/P/N symbol registration points");

            var flow = new MutinyFrontendFlow();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.Title,
                "FRONT-01 production front-end route starts at the title page");

            flow.PressPlay();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.GameSelect,
                "FRONT-01 Play uses the production route to open game select");

            flow.PressGameSelectBack();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.Title,
                "FRONT-02 game-select Back returns to the title page");

            flow.PressPlay();
            flow.PressOnePlayer();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.LevelSelect,
                "FRONT-02 1 Player uses the production route to open level select");

            bool lockedAccepted = flow.TrySelectLevel(2, level => level == 1);
            result.Assert(!lockedAccepted && flow.CurrentPage == MutinyFrontendPage.LevelSelect,
                "FRONT-06 production route rejects a locked level");

            bool firstAccepted = flow.TrySelectLevel(1, level => level == 1);
            result.Assert(firstAccepted && flow.CurrentPage == MutinyFrontendPage.Gameplay,
                "FRONT-05 production route accepts unlocked level 01 and enters gameplay");

            result.Assert(flow.ReturnToSinglePlayerLevelSelect() &&
                          flow.CurrentPage == MutinyFrontendPage.LevelSelect,
                "HUD-CORNER-04 production Quit path returns a one-player game to level select");

            flow = new MutinyFrontendFlow();
            flow.PressHelp();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.Help,
                "FRONT-HELP-01 PressHelp opens the help page");

            flow.PressHelpBack();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.Title,
                "FRONT-HELP-02 PressHelpBack returns to the title page");

            flow.PressCreditsBack();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.Title,
                "FRONT-CRED-03 Credits Back cannot change a non-Credits page");
            flow.PressCredits();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.Credits,
                "FRONT-CRED-01 production Credits route opens from title");
            flow.PressPlay();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.Credits,
                "FRONT-CRED-01 Play cannot bypass the Credits page");
            flow.PressCreditsBack();
            result.Assert(flow.CurrentPage == MutinyFrontendPage.Title,
                "FRONT-CRED-03 production Back route returns from Credits to title");

            Texture2D creditsPanel = Resources.Load<Texture2D>("UI/Frontend/credits_panel");
            Texture2D creditsLogo = Resources.Load<Texture2D>("UI/Frontend/credits_nitrome_logo");
            Texture2D creditsHit = Resources.Load<Texture2D>("UI/Frontend/credits_nitrome_hit");
            Texture2D creditsCopyright = Resources.Load<Texture2D>("UI/Frontend/credits_copyright");
            Texture2D creditsCopyrightHit = Resources.Load<Texture2D>("UI/Frontend/credits_copyright_hit");
            result.Assert(creditsPanel != null && creditsPanel.width == 462 && creditsPanel.height == 352 &&
                          creditsLogo != null && creditsLogo.width == 99 && creditsLogo.height == 74 &&
                          creditsHit != null && creditsHit.isReadable && creditsHit.width == 99 && creditsHit.height == 74 &&
                          creditsCopyright != null && creditsCopyright.width == 141 && creditsCopyright.height == 11 &&
                          creditsCopyrightHit != null && creditsCopyrightHit.isReadable &&
                          creditsCopyrightHit.width == 141 && creditsCopyrightHit.height == 11,
                "FRONT-CRED-02 original Credits panel, Nitrome and copyright hit masks load at SWF dimensions");

            bool allResourcesPresent =
                Resources.Load<Texture2D>("UI/Frontend/background") != null &&
                Resources.Load<Texture2D>("UI/Frontend/title_logo") != null &&
                Resources.Load<Texture2D>("UI/Frontend/game_select_panel") != null &&
                Resources.Load<Texture2D>("UI/Frontend/help_panel") != null &&
                Resources.Load<Texture2D>("UI/Frontend/level_select_panel") != null &&
                Resources.Load<Texture2D>("UI/Frontend/game_type_pirates") != null &&
                Resources.Load<Texture2D>("UI/Frontend/button_small") != null &&
                Resources.Load<Texture2D>("UI/Frontend/button_wide") != null &&
                Resources.Load<Texture2D>("UI/Frontend/button_back") != null &&
                Resources.Load<Texture2D>("UI/Frontend/level_slot") != null;
            for (int level = 1; level <= MutinyFrontendController.SinglePlayerLevelCount; level++)
                allResourcesPresent &= Resources.Load<Texture2D>($"UI/Frontend/LevelPreviews/{level:D2}") != null;
            result.Assert(allResourcesPresent,
                "FRONT-01/03 all original front-end panels, buttons and 15 pirate previews load through Resources");

            bool allHelpResourcesPresent = true;
            for (int i = 1; i <= 160; i++)
                allHelpResourcesPresent &= Resources.Load<Texture2D>($"UI/Help/tutorial_{i:D3}") != null;
            result.Assert(allHelpResourcesPresent,
                "FRONT-HELP-03 all 160 help tutorial animation frames exist in Resources/UI/Help");
        }

        private static void VerifyCharacterTimeline(MutinyLevel1VerificationResult result)
        {
            GameObject characterObject = null;

            try
            {
                characterObject = new GameObject("CharacterTimelineVerification");
                SpriteRenderer renderer = characterObject.AddComponent<SpriteRenderer>();
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                character.CharacterType = "redPirate";
                MutinyCharacterAnimator animator = characterObject.AddComponent<MutinyCharacterAnimator>();

                result.Assert(!animator.IsInitialized && animator.EnsureInitialized(),
                    "CHAR-TL-04 baked-scene production initialization loads frames from serialized CharacterType");

                result.Assert(animator.CurrentFrame == 1,
                    "CHAR-TL-01 production animator starts the original static label at frame 1");

                Sprite sprite1 = renderer.sprite;
                result.Assert(sprite1 != null,
                    "CHAR-IDLE-04 frame 1 sprite is assigned to SpriteRenderer");

                for (int tick = 0; tick < 3; tick++)
                    animator.AdvanceOriginalTick();
                result.Assert(animator.CurrentFrame == 4,
                    "CHAR-IDLE-01 static pose advances at the original three-tick boundary 1-3 to 4");
                Sprite sprite4 = renderer.sprite;
                result.Assert(sprite4 != null && sprite4 != sprite1,
                    "CHAR-IDLE-04 frame 4 sprite switches to squish/down bounce pose");
                result.Assert(animator.EnsureInitialized() && animator.CurrentFrame == 4,
                    "CHAR-IDLE-03 repeated lifecycle initialization does not reset a runtime-built animator");

                for (int tick = 0; tick < 3; tick++)
                    animator.AdvanceOriginalTick();
                result.Assert(animator.CurrentFrame == 7,
                    "CHAR-IDLE-01 static pose advances at the original three-tick boundary 4-6 to 7");

                for (int tick = 0; tick < 3; tick++)
                    animator.AdvanceOriginalTick();
                result.Assert(animator.CurrentFrame == 10,
                    "CHAR-IDLE-01 static pose advances at the original three-tick boundary 7-9 to 10");
                Sprite sprite10 = renderer.sprite;
                result.Assert(sprite10 != null && sprite10 != sprite4 && sprite10 != sprite1,
                    "CHAR-IDLE-04 frame 10 sprite switches to stretch/up bounce pose");

                for (int tick = 0; tick < 2; tick++)
                    animator.AdvanceOriginalTick();
                result.Assert(animator.CurrentFrame == 12,
                    "CHAR-TL-01 production animator reaches the last visible static frame 12");

                animator.AdvanceOriginalTick();
                result.Assert(animator.CurrentFrame == 1 && renderer.sprite == sprite1,
                    "CHAR-TL-01 frame 13 gotoAndPlay loops directly to static without displaying transparent frames 13-14");

                animator.PlayHit();
                result.Assert(animator.CurrentFrame == 15,
                    "CHAR-TL-02 production hit entry starts at the original hit label frame 15");

                for (int tick = 0; tick < 18; tick++)
                    animator.AdvanceOriginalTick();
                result.Assert(animator.CurrentFrame == 34,
                    "CHAR-TL-03 stopped character reaches the last visible hit frame 34");

                animator.AdvanceOriginalTick();
                result.Assert(animator.CurrentFrame == 1,
                    "CHAR-TL-03 frame 35 gotoAndPlay returns directly to static frame 1");

                string[] allCharacterTypes = new string[]
                {
                    "redPirate", "bluePirate", "cabinBoy", "skeletonPirate", "rainbowBeard",
                    "femalePirate", "blindPirate", "soldier", "bossGuy", "bossGuyZombie",
                    "soldierCaptain", "blindPirateCaptain", "femalePirateCaptain", "rainbowBeardCaptain",
                    "oldPirateCaptain", "cabinBoyCaptain", "tribeChief", "skeletonPirateCaptain",
                    "squid", "bluePirateCaptain", "redPirateCaptain", "oldPirate", "tribe",
                    "monkey", "crab", "shark", "parrot"
                };
                bool allCharactersLoaded = true;
                for (int i = 0; i < allCharacterTypes.Length; i++)
                {
                    Sprite[] frames = MutinyCharacterAnimator.LoadFrames(allCharacterTypes[i]);
                    if (frames == null || frames.Length != 35)
                    {
                        allCharactersLoaded = false;
                        break;
                    }
                }
                result.Assert(allCharactersLoaded,
                    "CHAR-IDLE-05 all 27 character types successfully load 35 animation frames deterministically");
            }
            finally
            {
                DestroyNow(characterObject);
            }
        }

        private static void VerifyCharacterCollisionAudio(MutinyLevel1VerificationResult result)
        {
            GameObject characterObject = null;
            try
            {
                characterObject = new GameObject("CharacterCollisionAudioVerification");
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                MutinyPhysicsBody body = character.PhysicsBody;
                string[,] terrain = new string[3, 3];
                for (int row = 0; row < 3; row++)
                    for (int column = 0; column < 3; column++)
                        terrain[row, column] = "-";
                terrain[1, 0] = terrain[1, 1] = terrain[1, 2] = "ground";
                body.SetTerrain(terrain, 3, 3);

                PhysicsBodyState state = PhysicsBodyState.CreateDefault(16f, 20f);
                state.Weight = 0f;
                state.Friction = 2f;
                state.LeftExtent = state.RightExtent = 6f;
                state.TopExtent = state.BottomExtent = 8f;
                state.VelocityY = 0f;
                body.State = state;

                // Character.advance increments contactTime after advanceMotion.
                // Six quiet production physics ticks make the next contact audible.
                for (int tick = 0; tick < 6; tick++)
                    body.AdvanceSimulationTick();
                state = body.State;
                state.VelocityY = 5f;
                body.State = state;
                body.AdvanceSimulationTick();
                bool firstPhysicalContactIsAudible = character.ContactSoundCount == 1;

                character.TakeDamage(1f);
                bool damageDoesNotPlayCollisionAudio = character.ContactSoundCount == 1;

                for (int tick = 0; tick < 5; tick++)
                    body.AdvanceSimulationTick();
                state = body.State;
                state.VelocityY = 5f;
                body.State = state;
                body.AdvanceSimulationTick();
                bool laterPhysicalContactUsesOriginalCooldown = character.ContactSoundCount == 2;

                result.Assert(Resources.Load<AudioClip>("Audio/SFX/hitwall") != null &&
                              firstPhysicalContactIsAudible && damageDoesNotPlayCollisionAudio &&
                              laterPhysicalContactUsesOriginalCooldown,
                    "CHAR-AUD-01 production character physics plays hitwall only on contacts after the original >5-tick cooldown, never on damage");
            }
            finally
            {
                DestroyNow(characterObject);
            }
        }

        private static void VerifyCharacterThrowAudio(MutinyLevel1VerificationResult result)
        {
            GameObject playerManagerObject = null;
            GameObject playerTeamObject = null;
            GameObject playerCharacterObject = null;
            GameObject playerInputObject = null;
            GameObject aiManagerObject = null;
            GameObject aiTeamObject = null;
            GameObject aiCharacterObject = null;
            MutinyAudioManager audio = MutinyAudioManager.Instance;
            bool previousSfxEnabled = audio.SfxEnabled;
            var playedSounds = new List<string>();
            System.Action<string> recordSfx = soundName => playedSounds.Add(soundName);

            try
            {
                audio.SfxEnabled = true;
                audio.SfxPlayed += recordSfx;

                playerManagerObject = new GameObject("CharacterThrowAudio_PlayerManager");
                playerTeamObject = new GameObject("CharacterThrowAudio_PlayerTeam");
                playerCharacterObject = new GameObject("CharacterThrowAudio_PlayerCharacter");
                playerInputObject = new GameObject("CharacterThrowAudio_PlayerInput");
                MutinyTurnManager playerManager = playerManagerObject.AddComponent<MutinyTurnManager>();
                MutinyTeam playerTeam = playerTeamObject.AddComponent<MutinyTeam>();
                MutinyCharacter playerCharacter = playerCharacterObject.AddComponent<MutinyCharacter>();
                MutinyPlayerInput playerInput = playerInputObject.AddComponent<MutinyPlayerInput>();
                playerTeam.TeamNumber = 1;
                playerTeam.RegisterCharacter(playerCharacter);
                playerManager.CurrentTeam = playerTeam;
                playerManager.CurrentPhase = TurnPhase.TurnActive;
                playerInput.TurnManager = playerManager;
                bool selected = playerInput.TrySelectCharacterForVerification(
                    playerTeam, MutinyPhysics.UnityToPixel(playerCharacter.transform.position));
                playerInput.SelectCharacterThrow();
                playedSounds.Clear();
                bool playerCommitted = playerInput.TryCommitCharacterThrow(
                    playerCharacter, new Vector2(100f, 100f), new Vector2(140f, 120f));
                bool playerThrowHasNoDirectSfx = playedSounds.Count == 0;

                aiManagerObject = new GameObject("CharacterThrowAudio_AiManager");
                aiTeamObject = new GameObject("CharacterThrowAudio_AiTeam");
                aiCharacterObject = new GameObject("CharacterThrowAudio_AiCharacter");
                MutinyTurnManager aiManager = aiManagerObject.AddComponent<MutinyTurnManager>();
                MutinyTeam aiTeam = aiTeamObject.AddComponent<MutinyTeam>();
                MutinyAIController aiController = aiTeamObject.AddComponent<MutinyAIController>();
                MutinyCharacter aiCharacter = aiCharacterObject.AddComponent<MutinyCharacter>();
                aiTeam.IsAiControlled = true;
                aiTeam.TeamNumber = 2;
                aiTeam.RegisterCharacter(aiCharacter);
                aiManager.CurrentTeam = aiTeam;
                aiManager.CurrentPhase = TurnPhase.TurnActive;
                playedSounds.Clear();
                aiController.ExecuteMoveForVerification(new AIMove
                {
                    MoveType = AIMoveType.SelfThrow,
                    Character = aiCharacter,
                    LaunchVelocity = new Vector2(12f, -8f)
                });
                bool aiThrowHasNoDirectSfx = playedSounds.Count == 0;

                result.Assert(Resources.Load<AudioClip>("Audio/SFX/click") != null &&
                              Resources.Load<AudioClip>("Audio/SFX/icon_collect") != null &&
                              selected && playerCommitted && playerThrowHasNoDirectSfx &&
                              aiCharacter.IsSelfThrown && !aiCharacter.CanThrow && aiCharacter.CanShoot &&
                              aiThrowHasNoDirectSfx,
                    "CHAR-THROW-AUD-01/02 production player and AI Throw Self submit motion without requesting chest click or icon_collect audio");
            }
            finally
            {
                audio.SfxPlayed -= recordSfx;
                audio.SfxEnabled = previousSfxEnabled;
                DestroyNow(aiCharacterObject);
                DestroyNow(aiTeamObject);
                DestroyNow(aiManagerObject);
                DestroyNow(playerInputObject);
                DestroyNow(playerCharacterObject);
                DestroyNow(playerTeamObject);
                DestroyNow(playerManagerObject);
            }
        }

        private static void VerifyLandDeath(MutinyLevel1VerificationResult result)
        {
            GameObject characterObject = null;
            GameObject corpseObject = null;

            try
            {
                Texture2D[] textures = Resources.LoadAll<Texture2D>("Art/Characters/DeadCharacter");
                result.Assert(textures.Length == MutinyDeadCharacterEffect.OriginalFrameCount,
                    "DEATH-03 all 24 original deadCharacter frames load from Resources");
                bool dimensionsMatch = textures.Length == MutinyDeadCharacterEffect.OriginalFrameCount;
                for (int i = 0; i < textures.Length; i++)
                    dimensionsMatch &= textures[i] != null && textures[i].width == 26 && textures[i].height == 22;
                result.Assert(dimensionsMatch,
                    "DEATH-03 every original deadCharacter frame is 26x22 pixels");
                result.Assert(Resources.Load<AudioClip>("Audio/SFX/die") != null,
                    "DEATH-03 original die audio loads from Resources");

                characterObject = new GameObject("LandDeathVerification_Character");
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                MutinyPhysicsBody body = character.PhysicsBody;
                PhysicsBodyState state = PhysicsBodyState.CreateDefault(64f, 96f);
                state.Weight = 0f;
                body.State = state;
                body.WaterPixelY = float.PositiveInfinity;
                body.SetTerrain(null, 0, 0);

                SpriteRenderer characterRenderer = character.GetComponent<SpriteRenderer>();
                character.TakeDamage(100f);
                result.Assert(!character.IsAlive && characterRenderer.enabled &&
                              !character.HasLandDeathPresentation,
                    "DEATH-01 lethal damage removes action eligibility but keeps the body visible before rest resolution");

                for (int tick = 0; tick < 99; tick++)
                    body.AdvanceSimulationTick();
                result.Assert(Mathf.Approximately(character.ShownHealth, 1f) &&
                              !character.HasLandDeathPresentation,
                    "DEATH-02 production ticks lower shown health by exactly one before the terminal tick");

                body.AdvanceSimulationTick();
                corpseObject = GameObject.Find($"DeadCharacter_{character.name}");
                MutinyDeadCharacterEffect effect =
                    corpseObject != null ? corpseObject.GetComponent<MutinyDeadCharacterEffect>() : null;
                Vector3 expectedPosition = MutinyPhysics.PixelToUnity(64f, 104f);
                result.Assert(character.HasLandDeathPresentation && !characterRenderer.enabled &&
                              effect != null && effect.CurrentFrame == 1 &&
                              Vector3.Distance(corpseObject.transform.position, expectedPosition) < 0.001f,
                    "DEATH-03 terminal shown-health tick hides the character and spawns frame 1 eight pixels below its origin");

                if (effect != null)
                {
                    for (int tick = 0; tick < 23; tick++)
                        effect.AdvanceOriginalTick();
                    effect.AdvanceOriginalTick();
                    result.Assert(effect.CurrentFrame == 24 && effect.IsComplete,
                        "DEATH-04 deadCharacter plays once and holds its stopped frame 24");
                }
            }
            finally
            {
                DestroyNow(corpseObject);
                DestroyNow(characterObject);
            }
        }

        private static void VerifyAirDropPresentation(MutinyLevel1VerificationResult result)
        {
            GameObject rootObject = null;
            GameObject managerObject = null;
            try
            {
                rootObject = new GameObject("AirDropPresentation_Root");
                MutinyLevelRoot root = rootObject.AddComponent<MutinyLevelRoot>();
                root.Width = 100;
                root.Height = 100;
                root.WaterLevelY = -80f;
                GameObject holder = new GameObject("Objects");
                holder.transform.SetParent(rootObject.transform, false);
                root.ObjectsHolder = holder.transform;

                var level = new MutinyLevelData
                {
                    Name = "AirDropPresentation",
                    Width = 1,
                    Height = 2,
                    Terrain = new string[2, 1],
                    Background = new string[2, 1]
                };
                level.Terrain[1, 0] = "solid";
                var pool = new MutinyLevelObject { Type = "potentialWeapons" };
                pool.Properties["banana"] = "1";
                level.Objects.Add(pool);
                managerObject = new GameObject("AirDropPresentation_Manager");
                MutinyTreasureChestManager manager =
                    managerObject.AddComponent<MutinyTreasureChestManager>();
                manager.Initialize(level, root);
                bool spawned = manager.TryDropNew();
                MutinyTreasureChest chest = spawned ? manager.Chests[0] : null;
                result.Assert(spawned && chest != null &&
                              Mathf.Approximately(chest.PixelY, -300f) &&
                              chest.CurrentVisualFrame == 10,
                    "AIR-PRES-01 production drop starts at -300 px on falling frame 10");
                if (chest == null)
                    return;

                manager.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                chest.ApplyPresentationPoseForVerification();
                result.Assert(chest.TimeTaken == 1 &&
                              Mathf.Approximately(chest.PixelY, -297f) &&
                              chest.CurrentVisualFrame == 11 &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(chest.transform.position).y + 300f) < 0.001f,
                    "AIR-PRES-01 first completed 25 Hz tick moves authority 3 px while display starts at previous pose");

                manager.AdvanceSimulationFrameForVerification(1f / 120f);
                chest.ApplyPresentationPoseForVerification();
                float firstRenderY = MutinyPhysics.UnityToPixel(chest.transform.position).y;
                manager.AdvanceSimulationFrameForVerification(1f / 120f);
                chest.ApplyPresentationPoseForVerification();
                float secondRenderY = MutinyPhysics.UnityToPixel(chest.transform.position).y;
                result.Assert(chest.TimeTaken == 1 &&
                              Mathf.Approximately(chest.PixelY, -297f) &&
                              firstRenderY > -300f && secondRenderY > firstRenderY &&
                              secondRenderY < chest.PixelY &&
                              Vector3.Distance(chest.transform.position, chest.PresentationPosition) < 0.00001f,
                    "AIR-PRES-02 120 Hz render samples advance between 25 Hz ticks without a second logical or sprite-frame advance");

                manager.AdvanceSimulationFrameForVerification(1f / 60f);
                chest.ApplyPresentationPoseForVerification();
                float thirdRenderY = MutinyPhysics.UnityToPixel(chest.transform.position).y;
                result.Assert(chest.TimeTaken == 1 && thirdRenderY > secondRenderY &&
                              thirdRenderY < chest.PixelY && chest.CurrentVisualFrame == 11,
                    "AIR-PRES-02 60 Hz render interval continues the same descent phase and leaves animation timing at 25 Hz");

                int guard = 0;
                while (chest.IsFalling && guard++ < 200)
                    manager.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                chest.ApplyPresentationPoseForVerification();
                result.Assert(!chest.IsFalling && chest.TimeTaken == 106 &&
                              Mathf.Approximately(chest.PixelY, 17f) &&
                              chest.CurrentVisualFrame == 20 &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(chest.transform.position).y - 17f) < 0.001f,
                    "AIR-PRES-01/02 landing tick clamps to floorY-15, starts touchdown, and clears the old fall interpolation");
            }
            finally
            {
                DestroyNow(managerObject);
                DestroyNow(rootObject);
            }
        }

        private static void VerifyAirDrops(MutinyLevel1VerificationResult result)
        {
            GameObject managerObject = null;
            GameObject chestObject = null;
            GameObject rootObject = null;
            GameObject objectsObject = null;
            GameObject characterObject = null;
            GameObject turnManagerObject = null;
            GameObject cameraObject = null;
            GameObject playerInputObject = null;

            try
            {
                rootObject = new GameObject("AirDropVerification_Root");
                MutinyLevelRoot root = rootObject.AddComponent<MutinyLevelRoot>();
                objectsObject = new GameObject("Objects");
                objectsObject.transform.SetParent(rootObject.transform, false);
                root.ObjectsHolder = objectsObject.transform;

                var level = new MutinyLevelData
                {
                    Name = "AirDropVerification",
                    Width = 2,
                    Height = 2,
                    Terrain = new string[2, 2],
                    Background = new string[2, 2]
                };
                level.Terrain[1, 0] = "solid";
                level.Terrain[1, 1] = "solid";
                level.Background[0, 1] = "antichest";
                var potentialWeapons = new MutinyLevelObject { Type = "potentialWeapons" };
                potentialWeapons.Properties["banana"] = "2";
                potentialWeapons.Properties["dynamite"] = "1";
                potentialWeapons.Properties["luck"] = "99";
                potentialWeapons.Properties["maxChests"] = "9";
                level.Objects.Add(potentialWeapons);

                managerObject = new GameObject("AirDropVerification_Manager");
                MutinyTreasureChestManager manager =
                    managerObject.AddComponent<MutinyTreasureChestManager>();
                manager.Initialize(level, root);
                bool dropped = manager.TryDropNew();
                result.Assert(MutinyTreasureChestManager.SystemEnabled &&
                              manager.ValidDropColumns.Count == 1 && manager.ValidDropColumns[0] == 0 &&
                              manager.PotentialWeapons.Count == 3 && dropped && manager.Chests.Count == 1,
                    "AIR-01 production drop entry uses antichest columns and the XML weighted pool; luck/maxChests do not add a second probability or override original max=3");

                turnManagerObject = new GameObject("AirDropVerification_TurnManager");
                MutinyTurnManager turnManager = turnManagerObject.AddComponent<MutinyTurnManager>();
                MutinyIngameTextArea ingameText = managerObject.AddComponent<MutinyIngameTextArea>();
                ingameText.Initialize(turnManager, null);
                cameraObject = new GameObject("AirDropVerification_Camera");
                cameraObject.AddComponent<Camera>();
                MutinyCameraController camCtrl = cameraObject.AddComponent<MutinyCameraController>();
                camCtrl.TurnManager = turnManager;

                playerInputObject = new GameObject("AirDropVerification_PlayerInput");
                MutinyPlayerInput playerInput = playerInputObject.AddComponent<MutinyPlayerInput>();
                playerInput.TurnManager = turnManager;

                chestObject = new GameObject("AirDropVerification_Chest");
                MutinyTreasureChest chest = chestObject.AddComponent<MutinyTreasureChest>();
                chest.Initialize(manager, 100000f, 96f, new List<string> { "banana" });
                chest.AdvanceOriginalTick();
                bool fallingRestBlocked = !turnManager.CheckAllBodiesAtRest();
                bool cameraTracksFallingChest = camCtrl.IsTrackingAirDrop;
                turnManager.CurrentPhase = TurnPhase.TurnActive;
                turnManager.AdvanceSimulationTick();
                bool preActionPhaseRemainsActive = turnManager.CurrentPhase == TurnPhase.TurnActive;
                bool playerCanActWhileFalling = playerInput.CanProcessCurrentTurnInputForVerification();
                result.Assert(chest.IsFalling && chest.CurrentVisualFrame == 11 && chest.TimeTaken == 1 &&
                              fallingRestBlocked && cameraTracksFallingChest &&
                              preActionPhaseRemainsActive && playerCanActWhileFalling,
                    "AIR-02 production tick moves through 10..19 loop, tracks camera at 50px/tick, and keeps TurnActive open so player/AI can act before touchdown");

                turnManager.NotifyActionStarted();
                turnManager.AdvanceSimulationTick();
                bool postActionPhaseIsExecuting = turnManager.CurrentPhase == TurnPhase.ActionExecuting;
                result.Assert(postActionPhaseIsExecuting,
                    "AIR-02B committing an action while chest is falling enters ActionExecuting and blocks turn settling until touchdown");

                int guard = 0;
                while (chest.IsFalling && guard++ < 200)
                    chest.AdvanceOriginalTick();
                bool landedRestClean = turnManager.CheckAllBodiesAtRest();
                bool cameraReleasedAfterLanding = !camCtrl.IsTrackingAirDrop;
                result.Assert(!chest.IsFalling && Mathf.Approximately(chest.PixelY, 81f) &&
                              chest.CurrentVisualFrame == 20 && landedRestClean && cameraReleasedAfterLanding,
                    "AIR-03 chest stops at floorY-15, releases camera tracking, and starts touchdown frame 20 with the Flash registration pivot");

                characterObject = new GameObject("AirDropVerification_PassiveCharacter");
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                character.TeamIndex = 1;
                PhysicsBodyState characterState = PhysicsBodyState.CreateDefault(100000f, 81f);
                characterState.VelocityX = 6f;
                characterState.VelocityY = -2f;
                character.PhysicsBody.State = characterState;

                chest.AdvanceOriginalTick();
                bool collectingRestBlocked = !turnManager.CheckAllBodiesAtRest();
                result.Assert(chest.CollectingCharacter == character && chest.CurrentVisualFrame == 35 && collectingRestBlocked,
                    "AIR-04 a moving character overlapping the landed chest opens it, and active collection keeps turn settling active");

                for (int tick = 0; tick < 10; tick++)
                    chest.AdvanceOriginalTick();
                result.Assert(character.HasWeapon("banana") && chest.RemainingContents == 0 &&
                              chest.CurrentVisualFrame == 44,
                    "AIR-05 first weapon is granted after 10 ticks and starts weapon_out frame 44");
                result.Assert(ingameText.PendingLineCount == 1 &&
                              ingameText.VisibleText == null,
                    "TXT-CHEST-01 production chest collection queues one text line on the exact weapon grant tick");
                ingameText.AdvanceOriginalTick();
                result.Assert(ingameText.VisibleText == "collected banana",
                    "TXT-CHEST-02 queued notice uses the original weapon-select display name");

                for (int tick = 0; tick < 3; tick++)
                    chest.AdvanceOriginalTick();
                Transform releasedIcon = chest.transform.Find("ReleasedWeaponIcon");
                result.Assert(chest.CurrentVisualFrame == 47 && releasedIcon != null &&
                              releasedIcon.GetComponent<SpriteRenderer>().enabled,
                    "AIR-06 weapon_out advances frame-by-frame and presents the actual released weapon icon");

                for (int tick = 0; tick < 37; tick++)
                    chest.AdvanceOriginalTick();
                result.Assert(chest.IsFinished && chest.CurrentVisualFrame == 81,
                    "AIR-07 empty chest waits the original 40 ticks, then starts fade_out frame 81");

                MutinyTreasureChest activeChest = manager.Chests.Count > 0 ? manager.Chests[0] : null;
                result.Assert(activeChest != null &&
                              Mathf.Approximately(MutinyAIController.ScoreChestMoveLanding(
                                  new Vector2(activeChest.PixelX + 39.99f, activeChest.FloorPixelY), activeChest), 0.5f) &&
                              Mathf.Approximately(MutinyAIController.ScoreChestMoveLanding(
                                  new Vector2(activeChest.PixelX + 40f, activeChest.FloorPixelY), activeChest), 0f),
                    "AIR-08 AI adds +0.5 only inside the original strict 40 px chest radius");
            }
            finally
            {
                DestroyNow(playerInputObject);
                DestroyNow(characterObject);
                DestroyNow(cameraObject);
                DestroyNow(turnManagerObject);
                DestroyNow(chestObject);
                DestroyNow(managerObject);
                DestroyNow(objectsObject);
                DestroyNow(rootObject);
            }
        }

        private static void VerifyTurnNotice(MutinyLevel1VerificationResult result)
        {
            GameObject managerObject = null;
            GameObject team1Object = null;
            GameObject team2Object = null;
            GameObject character1Object = null;
            GameObject character2Object = null;
            try
            {
                team1Object = new GameObject("TextVerification_PlayerTeam");
                MutinyTeam team1 = team1Object.AddComponent<MutinyTeam>();
                team1.TeamNumber = 1;
                team2Object = new GameObject("TextVerification_ComputerTeam");
                MutinyTeam team2 = team2Object.AddComponent<MutinyTeam>();
                team2.TeamNumber = 2;
                team2.IsAiControlled = true;
                character1Object = new GameObject("TextVerification_Player");
                team1.RegisterCharacter(character1Object.AddComponent<MutinyCharacter>());
                character2Object = new GameObject("TextVerification_Computer");
                team2.RegisterCharacter(character2Object.AddComponent<MutinyCharacter>());

                managerObject = new GameObject("TextVerification_Manager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.Team1 = team1;
                manager.Team2 = team2;
                MutinySpeechController speech = managerObject.AddComponent<MutinySpeechController>();
                speech.Initialize(manager);
                MutinyIngameTextArea textArea = managerObject.AddComponent<MutinyIngameTextArea>();
                textArea.Initialize(manager, speech);
                manager.Initialize(team1, team2);

                textArea.AdvanceOriginalTick();
                result.Assert(textArea.PendingLineCount == 1 && textArea.VisibleText == null,
                    "TXT-TURN-01 starting the player turn queues the original line while the captain introduction is pending");

                speech.Initialize(null);
                textArea.AdvanceOriginalTick();
                result.Assert(textArea.VisibleText == "Player 1, take your turn" &&
                              textArea.ClipY < 400f &&
                              Resources.Load<Texture2D>("UI/Fonts/dangle_font") != null,
                    "TXT-TURN-02 after speech the stage-centered DangleFont notice rises into view");

                textArea.SayCollected("tidalWave");
                for (int tick = 0; tick < 8; tick++)
                    textArea.AdvanceOriginalTick();
                result.Assert(textArea.ClipY == 373f,
                    "TXT-PRES-01 original frame 9 rises to y=373");
                textArea.AdvanceOriginalTick();
                result.Assert(textArea.ClipY == 370f,
                    "TXT-PRES-01 original frame 10 reaches the y=370 hold");
                for (int tick = 0; tick < 49; tick++)
                    textArea.AdvanceOriginalTick();
                result.Assert(textArea.ClipY == 370f,
                    "TXT-PRES-01 original frame 59 is the last held frame");
                textArea.AdvanceOriginalTick();
                result.Assert(textArea.ClipY == 370f,
                    "TXT-PRES-01 original frame 60 starts the descent at y=370");
                for (int tick = 0; tick < 10; tick++)
                    textArea.AdvanceOriginalTick();
                result.Assert(textArea.ClipY == 400f &&
                              textArea.VisibleText == "Player 1, take your turn" &&
                              textArea.PendingLineCount == 1,
                    "TXT-PRES-01 original frame 70 ends at y=400 without changing the queued message");
                textArea.AdvanceOriginalTick();
                result.Assert(textArea.VisibleText == "collected tidal wave" &&
                              textArea.ClipY == 400f && textArea.PendingLineCount == 0,
                    "TXT-PRES-01 frame 71 atomically replaces the first notice at the hidden origin");

                foreach (int fps in new[] { 25, 60, 120 })
                {
                    textArea.Initialize(manager, null);
                    textArea.SayCollected("tidalWave");
                    textArea.AdvancePresentationFrame(0.04f);
                    float firstTickY = textArea.ClipY;
                    float firstRenderY = textArea.RenderClipY;
                    textArea.AdvancePresentationFrame(1f / fps);
                    float expectedY = fps == 25 ? 397f : 400f - 3f * (1f / fps) / 0.04f;
                    result.Assert(textArea.VisibleText == "collected tidal wave" &&
                                  textArea.PendingLineCount == 0 &&
                                  firstTickY == 397f && firstRenderY == 400f &&
                                  Mathf.Abs(textArea.RenderClipY - expectedY) < 0.001f,
                        $"TXT-PRES-02 {fps} FPS draws between original tick poses without advancing the FIFO or text state");
                }
            }
            finally
            {
                DestroyNow(managerObject);
                DestroyNow(character1Object);
                DestroyNow(character2Object);
                DestroyNow(team1Object);
                DestroyNow(team2Object);
            }
        }

        private static void VerifyOriginalRotation(MutinyLevel1VerificationResult result)
        {
            GameObject characterObject = null;
            GameObject dynamiteObject = null;
            GameObject cherryBombObject = null;
            GameObject splashObject = null;

            try
            {
                characterObject = new GameObject("RotationVerification_Character");
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                MutinyPhysicsBody characterBody = character.PhysicsBody;
                characterBody.WaterPixelY = float.PositiveInfinity;

                PhysicsBodyState state = PhysicsBodyState.CreateDefault(48f, 16f);
                state.Weight = 0f;
                state.VelocityX = 4f;
                characterBody.State = state;
                characterBody.SetTerrain(null, 0, 0);
                characterBody.AdvanceSimulationTick();
                result.Assert(Mathf.Approximately(character.LogicalRotationDegrees, -12f),
                    "ROT-01 production physics tick rotates character by -velocityX * 3 once");
                result.Assert(Mathf.Approximately(character.SampleOriginalRotation(0f), 0f) &&
                              Mathf.Approximately(character.SampleOriginalRotation(0.5f), -6f) &&
                              Mathf.Approximately(character.SampleOriginalRotation(1f), -12f) &&
                              Mathf.Approximately(character.LogicalRotationDegrees, -12f),
                    "ROT-07 render sampling smoothly presents one exact 25 Hz logical step without integrating another rotation");

                character.ResetOriginalRotation(270f);
                string[,] floor = new string[3, 3];
                floor[1, 1] = "solid";
                state = PhysicsBodyState.CreateDefault(48f, 20f);
                state.Weight = 0f;
                state.VelocityY = 4f;
                characterBody.State = state;
                characterBody.SetTerrain(floor, 3, 3);
                StepResult floorResult = characterBody.AdvanceSimulationTick();
                result.Assert(floorResult.HitFloor &&
                              Mathf.Approximately(character.LogicalRotationDegrees, -45f),
                    "ROT-02 production floor contact normalizes 270 degrees and halves it to -45");

                result.Assert(
                    Mathf.Approximately(MutinyRotationRules.SettleCharacterFloorAngle(2f), 1f) &&
                    Mathf.Approximately(MutinyRotationRules.SettleCharacterFloorAngle(1f), 0f),
                    "ROT-02 floor correction uses the original strict one-degree snap threshold");

                character.ResetOriginalRotation(0f);
                state = PhysicsBodyState.CreateDefault(48f, 20f);
                state.Weight = 0f;
                state.Friction = 2f;
                state.VelocityX = 6f;
                state.VelocityY = 4f;
                characterBody.State = state;
                characterBody.SetTerrain(floor, 3, 3);
                characterBody.AdvanceSimulationTick();
                float characterVx1 = characterBody.State.VelocityX;

                state = characterBody.State;
                state.Y = 20f;
                state.VelocityY = 4f;
                characterBody.State = state;
                characterBody.AdvanceSimulationTick();
                float characterVx2 = characterBody.State.VelocityX;

                state = characterBody.State;
                state.Y = 20f;
                state.VelocityY = 4f;
                characterBody.State = state;
                characterBody.AdvanceSimulationTick();
                float characterVx3 = characterBody.State.VelocityX;
                result.Assert(Mathf.Approximately(characterVx1, 4f) &&
                              Mathf.Approximately(characterVx2, 2f) &&
                              Mathf.Approximately(characterVx3, 0f) &&
                              Mathf.Approximately(MutinyRotationRules.CharacterMotionDelta(characterVx1), -12f) &&
                              Mathf.Approximately(MutinyRotationRules.CharacterMotionDelta(characterVx2), -6f) &&
                              Mathf.Approximately(MutinyRotationRules.CharacterMotionDelta(characterVx3), 0f),
                    "ROT-08 production Character floor contacts reduce vx by 2 and therefore its source angular step from 12 to 6 to 0 degrees");

                character.ResetOriginalRotation(0f);
                characterBody.WaterPixelY = 0f;
                state = PhysicsBodyState.CreateDefault(48f, 1f);
                state.Weight = 0f;
                state.VelocityX = 5f;
                state.VelocityY = 6f;
                characterBody.State = state;
                characterBody.SetTerrain(null, 0, 0);
                characterBody.AdvanceSimulationTick();
                result.Assert(characterBody.IsInWater &&
                              Mathf.Approximately(character.LogicalRotationDegrees, -37f),
                    "ROT-03 character applies normal spin, water drag, Y clamp and character-only water spin in order");
                splashObject = GameObject.Find("WaterSplash");

                result.Assert(
                    Mathf.Approximately(MutinyRotationRules.WeaponRotationMultiplier("banana"), 2f) &&
                    Mathf.Approximately(MutinyRotationRules.WeaponRotationMultiplier("dynamite"), 2f) &&
                    Mathf.Approximately(MutinyRotationRules.WeaponRotationMultiplier("rumBottle"), 2f) &&
                    Mathf.Approximately(MutinyRotationRules.WeaponRotationMultiplier("voodooDoll"), 2f) &&
                    Mathf.Approximately(MutinyRotationRules.WeaponRotationMultiplier("boulder"), 2.5f) &&
                    Mathf.Approximately(MutinyRotationRules.WeaponRotationMultiplier("cherryBomb"), 0f),
                    "ROT-04 production weapon rotation table matches every original AS2 override");

                dynamiteObject = new GameObject("RotationVerification_Dynamite");
                MutinyDynamite dynamite = dynamiteObject.AddComponent<MutinyDynamite>();
                dynamite.Initialize(null);
                dynamite.PhysicsBody.WaterPixelY = float.PositiveInfinity;
                state = dynamite.PhysicsBody.State;
                state.Weight = 0f;
                dynamite.PhysicsBody.State = state;
                dynamite.Fire(new Vector2(4f, 0f));
                dynamite.PhysicsBody.AdvanceSimulationTick();
                result.Assert(Mathf.Approximately(dynamite.LogicalRotationDegrees, -8f),
                    "ROT-04 fired Dynamite rotates once at 25 Hz by its original multiplier");
                result.Assert(Mathf.Approximately(dynamite.SampleOriginalRotation(0.5f), -4f),
                    "ROT-07 Dynamite render sampling interpolates the production logical angle instead of adding per-render-frame spin");

                dynamite.ResetOriginalRotation(0f);
                dynamite.PhysicsBody.SetTerrain(floor, 3, 3);
                state = PhysicsBodyState.CreateDefault(48f, 20f);
                state.Weight = 0f;
                state.Friction = 1.7f;
                state.LeftExtent = state.RightExtent = state.TopExtent = state.BottomExtent = 11f;
                state.VelocityX = 6f;
                state.VelocityY = 4f;
                dynamite.PhysicsBody.State = state;
                StepResult dynamiteFloor1 = dynamite.PhysicsBody.AdvanceSimulationTick();
                float firstDynamiteAngle = dynamite.LogicalRotationDegrees;
                float firstDynamiteVelocity = dynamite.PhysicsBody.State.VelocityX;

                state = dynamite.PhysicsBody.State;
                state.Y = 20f;
                state.VelocityY = 4f;
                dynamite.PhysicsBody.State = state;
                StepResult dynamiteFloor2 = dynamite.PhysicsBody.AdvanceSimulationTick();
                float secondDynamiteDelta = Mathf.DeltaAngle(
                    firstDynamiteAngle, dynamite.LogicalRotationDegrees);
                result.Assert(dynamiteFloor1.HitFloor && dynamiteFloor2.HitFloor &&
                              Mathf.Approximately(firstDynamiteAngle, -12f) &&
                              Mathf.Approximately(firstDynamiteVelocity, 4.3f) &&
                              Mathf.Approximately(secondDynamiteDelta, -8.6f) &&
                              Mathf.Approximately(dynamite.PhysicsBody.State.VelocityX, 2.6f),
                    "ROT-08 production Dynamite rotation decays from 12 to 8.6 degrees per tick as original floor friction reduces velocityX");

                cherryBombObject = new GameObject("RotationVerification_CherryBomb");
                MutinyCherryBomb cherryBomb = cherryBombObject.AddComponent<MutinyCherryBomb>();
                cherryBomb.Initialize(null);
                cherryBomb.PhysicsBody.WaterPixelY = float.PositiveInfinity;
                state = cherryBomb.PhysicsBody.State;
                state.Weight = 0f;
                cherryBomb.PhysicsBody.State = state;
                cherryBomb.Fire(new Vector2(4f, 0f));
                cherryBomb.PhysicsBody.AdvanceSimulationTick();
                result.Assert(Mathf.Approximately(cherryBomb.LogicalRotationDegrees, 0f),
                    "ROT-05 CherryBomb keeps its timeline animation without invented transform spin");
            }
            finally
            {
                DestroyNow(splashObject);
                DestroyNow(cherryBombObject);
                DestroyNow(dynamiteObject);
                DestroyNow(characterObject);
            }
        }

        private static void VerifyPanelTiming(MutinyLevel1VerificationResult result)
        {
            float alpha = 0f;
            bool contentsActive = false;
            for (int tick = 1; tick <= 3; tick++)
                MutinyGameHUD.AdvanceActionPanelState(true, ref alpha, ref contentsActive);
            result.Assert(Mathf.Approximately(alpha, 0.75f) && !contentsActive,
                "TURN-02 panel is visible but rejects clicks after three opening ticks");

            MutinyGameHUD.AdvanceActionPanelState(true, ref alpha, ref contentsActive);
            result.Assert(Mathf.Approximately(alpha, 1f) && contentsActive,
                "TURN-02 panel accepts clicks on the fourth opening tick");

            MutinyGameHUD.AdvanceActionPanelState(false, ref alpha, ref contentsActive);
            result.Assert(Mathf.Approximately(alpha, 0.75f) && !contentsActive,
                "TURN-03 panel rejects clicks immediately when closing starts");

            for (int tick = 0; tick < 3; tick++)
                MutinyGameHUD.AdvanceActionPanelState(false, ref alpha, ref contentsActive);
            result.Assert(Mathf.Approximately(alpha, -0.25f) && !contentsActive,
                "TURN-03 closed panel reaches the original hidden alpha state");

            for (int tick = 1; tick <= 4; tick++)
                MutinyGameHUD.AdvanceActionPanelState(true, ref alpha, ref contentsActive);
            result.Assert(Mathf.Approximately(alpha, 0.75f) && !contentsActive,
                "TURN-05 reopened panel still rejects clicks for four ticks from hidden state");
            MutinyGameHUD.AdvanceActionPanelState(true, ref alpha, ref contentsActive);
            result.Assert(Mathf.Approximately(alpha, 1f) && contentsActive,
                "TURN-05 reopened panel accepts clicks on its fifth tick from hidden state");
        }

        private static void VerifyVisualStateMapping(MutinyLevel1VerificationResult result)
        {
            result.Assert(
                MutinyGameHUD.ResolveThrowButtonVisualState(1, true, false) == MutinyThrowButtonVisualState.RedUp,
                "TURN-02 red unused throw action resolves to red_up");
            result.Assert(
                MutinyGameHUD.ResolveThrowButtonVisualState(1, true, true) == MutinyThrowButtonVisualState.RedOver,
                "TURN-02 red unused throw action resolves to red_over while hovered");
            result.Assert(
                MutinyGameHUD.ResolveThrowButtonVisualState(2, true, false) == MutinyThrowButtonVisualState.BlueUp,
                "TURN-02 blue unused throw action resolves to blue_up");
            result.Assert(
                MutinyGameHUD.ResolveThrowButtonVisualState(2, true, true) == MutinyThrowButtonVisualState.BlueOver,
                "TURN-02 blue unused throw action resolves to blue_over while hovered");
            result.Assert(
                MutinyGameHUD.ResolveThrowButtonVisualState(1, false, true) == MutinyThrowButtonVisualState.Disabled,
                "TURN-05 consumed throw action stays disabled while hovered");
        }

        private static void VerifyRuntimeResources(MutinyLevel1VerificationResult result)
        {
            for (int i = 0; i < ThrowButtonResources.Length; i++)
            {
                string path = ThrowButtonResources[i];
                Texture2D texture = Resources.Load<Texture2D>(path);
                result.Assert(texture != null, $"Original throw button texture loads from Resources: {path}");
                if (texture != null)
                    result.Assert(texture.width == 86 && texture.height == 57,
                        $"Original throw button texture is 86x57: {path}");
            }

            AssertTexture(result, "UI/weapon_select_red", 271, 247);
            AssertTexture(result, "UI/weapon_select_blue", 271, 247);
            for (int i = 0; i < WeaponSlotResources.Length; i++)
                AssertTexture(result, WeaponSlotResources[i], 24, 36);
            for (int i = 0; i < EndTurnButtonResources.Length; i++)
                AssertTexture(result, EndTurnButtonResources[i], 86, 57);
            for (int i = 0; i < WeaponIconResources.Length; i++)
                AssertTexture(result, $"UI/WeaponIcons/{WeaponIconResources[i]}", 18, 17);
            AssertTexture(result, "UI/weapon_ammo_infinite", 18, 9);
            AssertTexture(result, "UI/Frontend/button_small_over", 163, 24);
            AssertTexture(result, "UI/Frontend/button_wide_over", 200, 24);
            AssertTexture(result, "UI/Frontend/button_back_over", 140, 24);
            AssertTexture(result, "UI/Frontend/level_slot_over", 51, 77);
            AssertTexture(result, "Art/Effects/Water/1", 1024, 384);
            AssertTexture(result, "Art/Effects/Splash/1", 48, 44);
            result.Assert(Resources.Load<AudioClip>("Audio/SFX/splash") != null,
                "WATER-03 original splash audio loads from Resources");
        }

        private static void VerifyOriginalWaterSurface(MutinyLevel1VerificationResult result)
        {
            int[] levelIndices = { 1, 6, 11 };
            var level = new MutinyLevelData
            {
                Name = "WaterSurfaceVerification",
                Width = 64, // Wider than the source's 1024 px water sprite.
                Height = 16,
                Players = 2,
                Terrain = new string[16, 64],
                Background = new string[16, 64]
            };

            for (int sky = 1; sky <= 3; sky++)
            {
                GameObject levelObject = null;
                try
                {
                    levelObject = MutinyLevelBuilder.BuildLevel(level, levelIndex: levelIndices[sky - 1]);
                    MutinyLevelRoot root = levelObject.GetComponent<MutinyLevelRoot>();
                    MutinyWaterSurface surface = root.WaterHolder.GetComponent<MutinyWaterSurface>();
                    SpriteRenderer backdrop = surface.BackdropRenderer;
                    Texture2D expectedWater = Resources.Load<Texture2D>($"Art/Effects/Water/{(sky - 1) * 10 + 1}");
                    Texture2D expectedBackdrop = Resources.Load<Texture2D>($"Art/Background/waterBackground{sky}");
                    float stride = MutinyWaterSurface.VisibleWidthPixels / MutinyPhysics.PixelsPerUnit;
                    bool continuous = true;
                    float lastX = float.NaN;
                    float rightEdge = float.NegativeInfinity;
                    int segments = 0;
                    foreach (SpriteRenderer renderer in root.WaterHolder.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (!renderer.name.StartsWith("WaterSurface_"))
                            continue;
                        segments++;
                        float x = renderer.transform.localPosition.x;
                        continuous &= renderer.sprite != null && renderer.sprite.texture == expectedWater &&
                                      Mathf.Approximately(renderer.sprite.rect.width, MutinyWaterSurface.VisibleWidthPixels) &&
                                      renderer.sortingOrder == MutinyLevelBuilder.WaterSortingOrder;
                        if (!float.IsNaN(lastX))
                            continuous &= Mathf.Abs(x - lastX - stride) < 0.001f;
                        lastX = x;
                        rightEdge = x + stride;
                    }

                    result.Assert(root.SkyColour == sky && surface.CurrentSkyColour == sky &&
                                  surface.LoadedFrameCount == 10 && segments >= 3 && continuous &&
                                  rightEdge >= level.Width * MutinyLevelBuilder.CellSize &&
                                  backdrop != null && backdrop.sprite != null &&
                                  backdrop.sprite.texture == expectedBackdrop &&
                                  backdrop.sortingOrder < MutinyLevelBuilder.BackgroundSortingOrder &&
                                  backdrop.transform.localPosition.x < 0f &&
                                  backdrop.transform.localPosition.x +
                                  backdrop.sprite.rect.width / MutinyPhysics.PixelsPerUnit *
                                  backdrop.transform.localScale.x > level.Width * MutinyLevelBuilder.CellSize,
                        $"VIS-WATER-01 level {levelIndices[sky - 1]} uses sky {sky}, 980 px seamless water and full-width original-colour backdrop");
                }
                finally
                {
                    DestroyNow(levelObject);
                }
            }

            GameObject bakedRootObject = new GameObject("WaterSurfaceVerification_BakedRoot");
            TextAsset bakedXml = null;
            try
            {
                MutinyLevelRoot bakedRoot = bakedRootObject.AddComponent<MutinyLevelRoot>();
                MutinyLevelController controller = bakedRootObject.AddComponent<MutinyLevelController>();
                bakedXml = new TextAsset("<level/>");
                controller.LevelXml = bakedXml;
                controller.CurrentLevelIndex = 6;
                bakedRoot.Width = level.Width;
                bakedRoot.WaterLevelY = -14f;
                GameObject waterObject = new GameObject("Water");
                waterObject.transform.SetParent(bakedRootObject.transform, false);
                bakedRoot.WaterHolder = waterObject.transform;
                bakedRoot.EnsureRuntimeWater();
                result.Assert(bakedRoot.SkyColour == 2 &&
                              waterObject.GetComponent<MutinyWaterSurface>()?.CurrentSkyColour == 2,
                    "VIS-WATER-01 baked level derives its missing sky colour from the original level index");
            }
            finally
            {
                DestroyNow(bakedRootObject);
                if (bakedXml != null)
                    Object.DestroyImmediate(bakedXml);
            }
        }

        private static void VerifyCornerLevelControls(MutinyLevel1VerificationResult result)
        {
            float alpha = 0f;
            for (int tick = 0; tick < 4; tick++)
                MutinyGameHUD.AdvanceQuitPromptState(true, ref alpha);
            result.Assert(Mathf.Approximately(alpha, 1f),
                "HUD-CORNER-T01 production quit prompt fades in at 25 Hz in four 25% ticks");

            for (int tick = 0; tick < 4; tick++)
                MutinyGameHUD.AdvanceQuitPromptState(false, ref alpha);
            result.Assert(Mathf.Approximately(alpha, 0f),
                "HUD-CORNER-T01 production Continue fade-out reaches hidden after four 25% ticks");

            result.Assert(
                MutinyGameHUD.ResolveCornerToggleVisualState(true, false) == MutinyCornerToggleVisualState.OnUp &&
                MutinyGameHUD.ResolveCornerToggleVisualState(true, true) == MutinyCornerToggleVisualState.OnOver &&
                MutinyGameHUD.ResolveCornerToggleVisualState(false, false) == MutinyCornerToggleVisualState.OffUp &&
                MutinyGameHUD.ResolveCornerToggleVisualState(false, true) == MutinyCornerToggleVisualState.OffOver,
                "HUD-CORNER-T03 production Music/SFX state mapping retains all original on/off and hover frames");

            result.Assert(
                RectApproximately(MutinyGameHUD.ResolveOriginalCornerVisualRect(MutinyCornerControl.Quit),
                    new Rect(479.9f, 11f, 23f, 34f)) &&
                RectApproximately(MutinyGameHUD.ResolveOriginalCornerVisualRect(MutinyCornerControl.Music),
                    new Rect(496.9f, 11f, 31f, 34f)) &&
                RectApproximately(MutinyGameHUD.ResolveOriginalCornerVisualRect(MutinyCornerControl.Sfx),
                    new Rect(501.9f, 11f, 47f, 34f)) &&
                RectApproximately(MutinyGameHUD.ResolveOriginalCornerHitRect(MutinyCornerControl.Quit),
                    new Rect(483.95f, 11f, 13.95f, 13.9f)) &&
                RectApproximately(MutinyGameHUD.ResolveOriginalCornerHitRect(MutinyCornerControl.Music),
                    new Rect(502f, 11f, 19.95f, 13.9f)) &&
                RectApproximately(MutinyGameHUD.ResolveOriginalCornerHitRect(MutinyCornerControl.Sfx),
                    new Rect(525.95f, 11f, 13.95f, 13.9f)),
                "HUD-CORNER-T04 production icon visual and hit rectangles retain root-timeline twip placements and source bounds");

            result.Assert(
                MutinyGameHUD.ResolveOriginalCornerTooltip(MutinyCornerControl.Quit) == "quit" &&
                MutinyGameHUD.ResolveOriginalCornerTooltip(MutinyCornerControl.Music) == "music" &&
                MutinyGameHUD.ResolveOriginalCornerTooltip(MutinyCornerControl.Sfx) == "sound fx",
                "HUD-CORNER-T05 production hover state maps to the original quit, music, and sound fx bubble labels");

            result.Assert(
                RectApproximately(MutinyGameHUD.ResolveOriginalCornerBubbleRect(MutinyCornerControl.Quit),
                    new Rect(479.9f, 28f, 23f, 17f)) &&
                RectApproximately(MutinyGameHUD.ResolveOriginalCornerBubbleRect(MutinyCornerControl.Music),
                    new Rect(496.9f, 28f, 31f, 17f)) &&
                RectApproximately(MutinyGameHUD.ResolveOriginalCornerBubbleRect(MutinyCornerControl.Sfx),
                    new Rect(501.9f, 28f, 47f, 17f)),
                "HUD-CORNER-T06 production corner bubble rectangles are located at y=28..45 under the icons");

            // Verify Sound FX hover transitions:
            bool sfxIconHover = MutinyGameHUD.IsCornerHovered(MutinyCornerControl.Sfx, new Vector2(530f, 15f), false);
            bool sfxBubbleHover = MutinyGameHUD.IsCornerHovered(MutinyCornerControl.Sfx, new Vector2(530f, 35f), true);
            bool sfxMovingToMusic = MutinyGameHUD.IsCornerHovered(MutinyCornerControl.Sfx, new Vector2(510f, 15f), true);
            bool musicHover = MutinyGameHUD.IsCornerHovered(MutinyCornerControl.Music, new Vector2(510f, 15f), false);
            bool sfxMovingDown = MutinyGameHUD.IsCornerHovered(MutinyCornerControl.Sfx, new Vector2(530f, 60f), true);
            result.Assert(
                sfxIconHover && sfxBubbleHover && !sfxMovingToMusic && musicHover && !sfxMovingDown,
                "HUD-CORNER-T07 Sound FX hover releases cleanly when moving to Music or downwards, preventing sticky bubble");

            GameObject hudObject = null;
            try
            {
                hudObject = new GameObject("CornerControlsVerification_Hud");
                MutinyGameHUD hud = hudObject.AddComponent<MutinyGameHUD>();
                bool opened = hud.OpenQuitPrompt();
                bool rejectedDuplicate = !hud.OpenQuitPrompt();
                bool continued = hud.ContinueQuitPrompt();
                result.Assert(opened && rejectedDuplicate && continued && !hud.IsQuitPromptShowRequested,
                    "HUD-CORNER-T02 production Quit rejects duplicate open and Continue only dismisses the popup");
            }
            finally
            {
                DestroyNow(hudObject);
            }

            MutinyAudioManager audio = MutinyAudioManager.Instance;
            bool previousSfx = audio.SfxEnabled;
            bool previousMusic = audio.MusicEnabled;
            try
            {
                audio.SfxEnabled = true;
                Mutiny.Persistence.MutinySaveSystem.SfxEnabled = true;
                audio.ToggleSFX();
                bool sfxOffSaved = !audio.SfxEnabled && !Mutiny.Persistence.MutinySaveSystem.SfxEnabled;
                audio.ToggleSFX();

                audio.MusicEnabled = true;
                Mutiny.Persistence.MutinySaveSystem.MusicEnabled = true;
                audio.ToggleMusic();
                bool musicOffSaved = !audio.MusicEnabled && !Mutiny.Persistence.MutinySaveSystem.MusicEnabled;
                audio.ToggleMusic();
                result.Assert(sfxOffSaved && audio.SfxEnabled && musicOffSaved && audio.MusicEnabled,
                    "HUD-CORNER-T03 production Music/SFX actions toggle and persist their corresponding saved settings");
            }
            finally
            {
                audio.SfxEnabled = previousSfx;
                audio.MusicEnabled = previousMusic;
                Mutiny.Persistence.MutinySaveSystem.SfxEnabled = previousSfx;
                Mutiny.Persistence.MutinySaveSystem.MusicEnabled = previousMusic;
            }

            AssertTexture(result, "UI/Frontend/button_small", 163, 24);
            AssertTexture(result, "UI/Frontend/button_back", 140, 24);
            AssertTexture(result, "UI/CornerControls/quit_up", 23, 34);
            AssertTexture(result, "UI/CornerControls/quit_over", 23, 34);
            AssertTexture(result, "UI/CornerControls/music_on_up", 31, 34);
            AssertTexture(result, "UI/CornerControls/music_on_over", 31, 34);
            AssertTexture(result, "UI/CornerControls/music_off_up", 31, 34);
            AssertTexture(result, "UI/CornerControls/music_off_over", 31, 34);
            AssertTexture(result, "UI/CornerControls/sfx_on_up", 47, 34);
            AssertTexture(result, "UI/CornerControls/sfx_on_over", 47, 34);
            AssertTexture(result, "UI/CornerControls/sfx_off_up", 47, 34);
            AssertTexture(result, "UI/CornerControls/sfx_off_over", 47, 34);
        }

        private static void VerifyMutedMusicToggleStartsRequestedTrack(MutinyLevel1VerificationResult result)
        {
            MutinyAudioManager audio = MutinyAudioManager.Instance;
            AudioSource musicSource = audio != null ? audio.MusicSource : null;
            AudioClip previousClip = musicSource != null ? musicSource.clip : null;
            bool previousLoop = musicSource != null && musicSource.loop;
            float previousVolume = musicSource != null ? musicSource.volume : 0f;
            bool previousWasPlaying = musicSource != null && musicSource.isPlaying;
            bool previousMusicEnabled = audio != null && audio.MusicEnabled;
            bool previousSavedMusicEnabled = Mutiny.Persistence.MutinySaveSystem.MusicEnabled;
            GameObject hudObject = null;

            try
            {
                AudioClip gameMusic = Resources.Load<AudioClip>("Audio/Music/game_music");
                AudioClip menuMusic = Resources.Load<AudioClip>("Audio/Music/menu_music");
                if (audio == null || musicSource == null || gameMusic == null || menuMusic == null)
                {
                    result.Assert(false,
                        "AUDIO-MUSIC-T02 production music toggle requires both original music clips and an AudioSource");
                    return;
                }

                audio.MusicEnabled = true;
                audio.PlayMusic("game_music");
                audio.ToggleMusic();
                audio.PlayMusic("menu_music");

                bool menuTrackRememberedWhileMuted =
                    !audio.MusicEnabled && musicSource.clip == menuMusic && !musicSource.isPlaying;

                hudObject = new GameObject("MutedMusicToggleVerification_Hud");
                MutinyGameHUD hud = hudObject.AddComponent<MutinyGameHUD>();
                hud.ToggleCornerMusic();

                bool playbackStartedImmediately =
                    audio.MusicEnabled && musicSource.clip == menuMusic &&
                    (!Application.isPlaying || musicSource.isPlaying);
                result.Assert(menuTrackRememberedWhileMuted && playbackStartedImmediately,
                    "AUDIO-MUSIC-T02 production flow remembers menu_music while muted and the Music button starts it immediately");
            }
            finally
            {
                DestroyNow(hudObject);
                if (musicSource != null)
                {
                    musicSource.Stop();
                    musicSource.clip = previousClip;
                    musicSource.loop = previousLoop;
                    musicSource.volume = previousVolume;
                }

                if (audio != null)
                    audio.MusicEnabled = previousMusicEnabled;
                Mutiny.Persistence.MutinySaveSystem.MusicEnabled = previousSavedMusicEnabled;

                if (musicSource != null && previousWasPlaying && previousMusicEnabled && previousClip != null)
                    musicSource.Play();
            }
        }

        private static void VerifyGameEndPopup(MutinyLevel1VerificationResult result)
        {
            float alpha = 0f;
            for (int tick = 0; tick < 4; tick++)
                MutinyGameHUD.AdvanceGameEndPopupState(true, ref alpha);
            result.Assert(Mathf.Approximately(alpha, 1f),
                "END-POP-T03 production game-end popup fades in at 25 Hz in four 25% ticks");

            result.Assert(
                MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Team1Wins, 1) == MutinyGameEndPopupKind.LevelComplete &&
                MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Team1Wins, MutinyFrontendController.SinglePlayerLevelCount) == MutinyGameEndPopupKind.GameComplete &&
                MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Team2Wins, 1) == MutinyGameEndPopupKind.LevelFailed &&
                MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Draw, 1) == MutinyGameEndPopupKind.LevelFailed,
                "END-POP-T01/T02 production result mapping selects original complete, final-complete, and failed popup frames");

            result.Assert(
                MutinyGameHUD.AdvanceDisplayedScore(0, 600, 287) == 287 &&
                MutinyGameHUD.AdvanceDisplayedScore(574, 600, 287) == 600 &&
                MutinyGameHUD.AdvanceDisplayedScore(0, 600, 347) == 347,
                "END-POP-T04 production score counters use original 287/347 tick increments and clamp at the target");

            result.Assert(
                RectApproximately(MutinyGameHUD.ResolveOriginalPopupPanelRect(), new Rect(100f, 70f, 350f, 260f)) &&
                RectApproximately(MutinyGameHUD.ResolveOriginalPopupPrimaryButtonRect(), new Rect(135f, 245f, 280f, 24f)) &&
                RectApproximately(MutinyGameHUD.ResolveOriginalPopupSecondaryButtonRect(), new Rect(135f, 280f, 280f, 24f)),
                "END-POP-T07 popup panel and active button bounds match DefineShape_326/328 and the 900/1600-twip timeline placements");

            IReadOnlyList<MutinyGameHUD.FailedPopupButton> failedButtons = MutinyGameHUD.FailedPopupButtons;
            Rect formerSubmitArea = new Rect(135f, 209f, 280f, 24f);
            bool failureHasOnlyRestartAndBack = failedButtons.Count == 2 &&
                failedButtons[0].Action == MutinyFailedPopupAction.RestartLevel &&
                failedButtons[0].Label == "restart level" &&
                RectApproximately(failedButtons[0].Rect, MutinyGameHUD.ResolveOriginalPopupPrimaryButtonRect()) &&
                failedButtons[1].Action == MutinyFailedPopupAction.BackToMenu &&
                failedButtons[1].Label == "back to menu" &&
                RectApproximately(failedButtons[1].Rect, MutinyGameHUD.ResolveOriginalPopupSecondaryButtonRect());
            for (int i = 0; i < failedButtons.Count; i++)
                failureHasOnlyRestartAndBack &= !failedButtons[i].Rect.Overlaps(formerSubmitArea);
            result.Assert(failureHasOnlyRestartAndBack,
                "SCORE-EXT-04 production failure popup registers only Restart and Back, leaving the former Submit Score area without a visible or clickable control");

            GameObject controllerObject = null;
            GameObject managerObject = null;
            GameObject team1Object = null;
            GameObject team2Object = null;
            GameObject playerObject = null;
            GameObject enemyObject = null;
            GameObject hudObject = null;
            GameObject failureHudObject = null;
            GameObject victoryCrateObject = null;
            GameObject victoryBarrelObject = null;
            GameObject victoryMineObject = null;
            GameObject defeatCrateObject = null;
            GameObject defeatBarrelObject = null;
            GameObject defeatMineObject = null;
            GameObject unloadCrateObject = null;
            GameObject unloadMineObject = null;
            try
            {
                controllerObject = new GameObject("GameEndVerification_LevelController");
                MutinyLevelController controller = controllerObject.AddComponent<MutinyLevelController>();
                controller.CurrentLevelIndex = 2;
                team1Object = new GameObject("GameEndVerification_PlayerTeam");
                team2Object = new GameObject("GameEndVerification_EnemyTeam");
                MutinyTeam playerTeam = team1Object.AddComponent<MutinyTeam>();
                MutinyTeam enemyTeam = team2Object.AddComponent<MutinyTeam>();
                playerTeam.TeamNumber = 1;
                enemyTeam.TeamNumber = 2;
                enemyTeam.IsAiControlled = true;
                playerObject = new GameObject("GameEndVerification_Player");
                enemyObject = new GameObject("GameEndVerification_Enemy");
                MutinyCharacter player = playerObject.AddComponent<MutinyCharacter>();
                MutinyCharacter enemy = enemyObject.AddComponent<MutinyCharacter>();
                playerTeam.RegisterCharacter(player);
                enemyTeam.RegisterCharacter(enemy);
                managerObject = new GameObject("GameEndVerification_TurnManager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.Initialize(playerTeam, enemyTeam);

                MutinyWoodenCrate victoryCrate =
                    MutinyWeaponFactory.SpawnWeapon("woodenCrate", player) as MutinyWoodenCrate;
                MutinyGunpowderBarrel victoryBarrel =
                    MutinyWeaponFactory.SpawnWeapon("gunpowderBarrel", player) as MutinyGunpowderBarrel;
                MutinyMine victoryMine =
                    MutinyWeaponFactory.SpawnWeapon("mine", player) as MutinyMine;
                victoryCrateObject = victoryCrate != null ? victoryCrate.gameObject : null;
                victoryBarrelObject = victoryBarrel != null ? victoryBarrel.gameObject : null;
                victoryMineObject = victoryMine != null ? victoryMine.gameObject : null;
                string[,] boxTerrain = new string[6, 12];
                for (int row = 0; row < 6; row++)
                    for (int column = 0; column < 12; column++)
                        boxTerrain[row, column] = row == 4 ? "ground" : "-";
                if (victoryCrate != null)
                    victoryCrate.PhysicsBody.SetTerrain(boxTerrain, 12, 6);
                bool victoryCratePlaced = victoryCrate != null &&
                    victoryCrate.TryPlaceAt(new Vector2(64f, 96f)) &&
                    victoryCrate.TryPlaceAt(new Vector2(128f, 96f)) &&
                    victoryCrate.TryPlaceAt(new Vector2(192f, 96f));
                // BoxWeapon parents complete on their next 25 Hz advance after
                // the final child finishes; do not leave a staged chain blocking
                // the genuine GameOver path in this synchronous verification.
                victoryCrate?.NextBox?.AdvanceAiPlacementTickForVerification();
                victoryCrate?.AdvanceAiPlacementTickForVerification();
                if (victoryMine != null)
                {
                    victoryMine.Fire(Vector2.zero);
                    victoryMine.AdvanceOriginalTickForVerification();
                }

                // Damage uses the real death path.  Make the display immediately
                // settled so the production turn manager reaches its normal
                // inactivity boundary instead of waiting for a presentation tween.
                enemy.TakeDamage(100f);
                enemy.ShownHealth = enemy.Health;
                for (int tick = 0; tick <= MutinyTurnManager.InactivitySettlingThreshold; tick++)
                    manager.AdvanceSimulationTick();

                hudObject = new GameObject("GameEndVerification_Hud");
                MutinyGameHUD hud = hudObject.AddComponent<MutinyGameHUD>();
                hud.TurnManager = manager;
                hud.LevelController = controller;
                bool synchronized = hud.SynchronizeGameEndPopup();

                int expectedLevelScore = MutinyLevelController.CalculateOriginalSinglePlayerLevelScore(playerTeam, 2);
                bool victoryObjectsPersist = victoryCratePlaced && victoryMine != null && victoryMine.IsStored &&
                    MutinyBoxRegistry.Count == 3 &&
                    MutinyBoxRegistry.GetObstacles().Count == 3 &&
                    victoryCrateObject != null && victoryCrateObject.activeSelf &&
                    victoryBarrelObject != null && victoryBarrelObject.activeSelf &&
                    victoryMineObject != null && victoryMineObject.activeSelf;
                result.Assert(manager.CurrentPhase == TurnPhase.GameOver &&
                              manager.GameResult == GameOverResult.Team1Wins &&
                              controller.LastCompletedLevelScore == expectedLevelScore &&
                              controller.SinglePlayerScore == expectedLevelScore &&
                              synchronized && hud.GameEndPopupKind == MutinyGameEndPopupKind.LevelComplete &&
                              victoryObjectsPersist,
                    $"END-POP-T01/LVL-PERSIST-01 victory speech and popup retain placed crate collision, pending barrel, and mine (placed={victoryCratePlaced}, stored={victoryMine?.IsStored}, boxes={MutinyBoxRegistry.Count}, phase={manager.CurrentPhase})");

                // Re-enter the same production state machine with the opposite
                // team defeated so LVL-PERSIST-01 covers both victory and failure.
                player.Health = player.MaxHealth;
                player.ShownHealth = player.Health;
                player.IsAlive = true;
                player.IsDrowned = false;
                enemy.Health = enemy.MaxHealth;
                enemy.ShownHealth = enemy.Health;
                enemy.IsAlive = true;
                enemy.IsDrowned = false;
                controller.CurrentLevelIndex = MutinyFrontendController.SinglePlayerLevelCount;
                MutinySaveSystem.CompletedScoreEntry[] scoresBeforeFailure =
                    MutinySaveSystem.GetTopCompletedScoreEntries();
                manager.StartGame();
                MutinyWoodenCrate defeatCrate =
                    MutinyWeaponFactory.SpawnWeapon("woodenCrate", player) as MutinyWoodenCrate;
                MutinyGunpowderBarrel defeatBarrel =
                    MutinyWeaponFactory.SpawnWeapon("gunpowderBarrel", player) as MutinyGunpowderBarrel;
                MutinyMine defeatMine =
                    MutinyWeaponFactory.SpawnWeapon("mine", player) as MutinyMine;
                defeatCrateObject = defeatCrate != null ? defeatCrate.gameObject : null;
                defeatBarrelObject = defeatBarrel != null ? defeatBarrel.gameObject : null;
                defeatMineObject = defeatMine != null ? defeatMine.gameObject : null;
                if (defeatBarrel != null)
                    defeatBarrel.PhysicsBody.SetTerrain(boxTerrain, 12, 6);
                bool defeatBarrelPlaced = defeatBarrel != null &&
                    defeatBarrel.TryPlaceAt(new Vector2(256f, 96f)) &&
                    defeatBarrel.TryPlaceAt(new Vector2(320f, 96f));
                defeatBarrel?.AdvancePlacementTickForVerification();
                if (defeatMine != null)
                {
                    defeatMine.Fire(Vector2.zero);
                    defeatMine.AdvanceOriginalTickForVerification();
                }
                player.TakeDamage(player.MaxHealth);
                player.ShownHealth = player.Health;
                for (int tick = 0; tick <= MutinyTurnManager.InactivitySettlingThreshold; tick++)
                    manager.AdvanceSimulationTick();
                failureHudObject = new GameObject("GameEndVerification_FailureHud");
                MutinyGameHUD failureHud = failureHudObject.AddComponent<MutinyGameHUD>();
                failureHud.TurnManager = manager;
                failureHud.LevelController = controller;
                bool failurePopupOpened = failureHud.SynchronizeGameEndPopup();
                MutinySaveSystem.CompletedScoreEntry[] scoresAfterFailure =
                    MutinySaveSystem.GetTopCompletedScoreEntries();
                bool historyUnchanged = scoresBeforeFailure.Length == scoresAfterFailure.Length;
                for (int scoreIndex = 0; scoreIndex < scoresBeforeFailure.Length && historyUnchanged; scoreIndex++)
                    historyUnchanged = scoresBeforeFailure[scoreIndex].Score == scoresAfterFailure[scoreIndex].Score &&
                        scoresBeforeFailure[scoreIndex].Timestamp == scoresAfterFailure[scoreIndex].Timestamp;
                result.Assert(failurePopupOpened &&
                              failureHud.GameEndPopupKind == MutinyGameEndPopupKind.LevelFailed &&
                              historyUnchanged,
                    "SCORE-EXT-04 production final-level defeat opens failure popup and leaves completed-score history unchanged");
                bool defeatObjectsPersist = defeatBarrelPlaced && defeatMine != null && defeatMine.IsStored &&
                    MutinyBoxRegistry.Count == 5 &&
                    MutinyBoxRegistry.GetObstacles().Count == 5 &&
                    defeatCrateObject != null && defeatCrateObject.activeSelf &&
                    defeatBarrelObject != null && defeatBarrelObject.activeSelf &&
                    defeatMineObject != null && defeatMineObject.activeSelf;
                result.Assert(manager.CurrentPhase == TurnPhase.GameOver &&
                              manager.GameResult == GameOverResult.Team2Wins &&
                              defeatObjectsPersist,
                    $"LVL-PERSIST-01 production failure path retains both box types, mine, and placed collision state (placed={defeatBarrelPlaced}, stored={defeatMine?.IsStored}, boxes={MutinyBoxRegistry.Count}, phase={manager.CurrentPhase})");

                bool nextLevelBuilt = controller.TryLoadLevel(1);
                bool oldObjectsCleared = MutinyBoxRegistry.Count == 0 &&
                    (victoryCrateObject == null || !victoryCrateObject.activeSelf) &&
                    (victoryBarrelObject == null || !victoryBarrelObject.activeSelf) &&
                    (victoryMineObject == null || !victoryMineObject.activeSelf) &&
                    (defeatCrateObject == null || !defeatCrateObject.activeSelf) &&
                    (defeatBarrelObject == null || !defeatBarrelObject.activeSelf) &&
                    (defeatMineObject == null || !defeatMineObject.activeSelf);
                result.Assert(nextLevelBuilt && oldObjectsCleared,
                    "LVL-RESET-01 production next-level initialization clears prior boxes and mines, not the GameOver speech");

                MutinyWoodenCrate unloadCrate =
                    MutinyWeaponFactory.SpawnWeapon("woodenCrate", player) as MutinyWoodenCrate;
                MutinyMine unloadMine =
                    MutinyWeaponFactory.SpawnWeapon("mine", player) as MutinyMine;
                unloadCrateObject = unloadCrate != null ? unloadCrate.gameObject : null;
                unloadMineObject = unloadMine != null ? unloadMine.gameObject : null;
                if (unloadCrate != null)
                    MutinyBoxRegistry.Register(unloadCrate.PhysicsBody);
                controller.ClearLevel();
                result.Assert(controller.CurrentLevel == null && MutinyBoxRegistry.Count == 0 &&
                              (unloadCrateObject == null || !unloadCrateObject.activeSelf) &&
                              (unloadMineObject == null || !unloadMineObject.activeSelf),
                    "LVL-RESET-01 production explicit level unload removes prior box collision and mine");

            }
            finally
            {
                DestroyNow(failureHudObject);
                DestroyNow(unloadMineObject);
                DestroyNow(unloadCrateObject);
                DestroyNow(defeatMineObject);
                DestroyNow(victoryMineObject);
                DestroyNow(defeatBarrelObject);
                DestroyNow(defeatCrateObject);
                DestroyNow(victoryBarrelObject);
                DestroyNow(victoryCrateObject);
                DestroyNow(hudObject);
                DestroyNow(enemyObject);
                DestroyNow(playerObject);
                DestroyNow(managerObject);
                DestroyNow(team2Object);
                DestroyNow(team1Object);
                DestroyNow(controllerObject);
            }
        }

        private static void VerifyEndingSequence(MutinyLevel1VerificationResult result)
        {
            AssertTexture(result, "UI/Ending/ship", 228, 287);
            AssertTexture(result, "UI/Ending/bubble", 244, 132);
            AssertTexture(result, "UI/Ending/bubble_kraken", 244, 132);
            AssertTexture(result, "UI/Ending/score_panel", 360, 190);
            AssertTexture(result, "UI/Ending/back_button", 280, 24);

            int[] shipWidths = { 21, 228, 34, 32, 32, 33, 33, 25, 28 };
            int[] shipHeights = { 26, 255, 32, 32, 32, 32, 32, 25, 32 };
            bool shipResources = MutinyEndingSequence.ShipSymbols.Length == shipWidths.Length &&
                MutinyEndingSequence.ShipLayers.Length == 28;
            for (int symbolIndex = 0; symbolIndex < MutinyEndingSequence.ShipSymbols.Length; symbolIndex++)
            {
                MutinyEndingSequence.ShipSymbol symbol = MutinyEndingSequence.ShipSymbols[symbolIndex];
                for (int frame = 1; frame <= symbol.FrameCount; frame++)
                {
                    Texture2D texture = Resources.Load<Texture2D>(
                        $"UI/Ending/ShipParts/{symbol.Id}_{frame:D2}");
                    shipResources &= texture != null && texture.width == shipWidths[symbolIndex] &&
                        texture.height == shipHeights[symbolIndex];
                }
            }
            result.Assert(shipResources,
                "END-SEQ-05 all 91 original ship parts load with their source dimensions and depth layout");

            bool nestedFrames = MutinyEndingSequence.ShipFrameAt(0, 7) == 1 &&
                MutinyEndingSequence.ShipFrameAt(3, 7) == 4 &&
                MutinyEndingSequence.ShipFrameAt(6, 7) == 7 &&
                MutinyEndingSequence.ShipFrameAt(9, 7) == 10 &&
                MutinyEndingSequence.ShipFrameAt(12, 7) == 1 &&
                MutinyEndingSequence.ShipFrameAt(11, 8) == 12 &&
                MutinyEndingSequence.ShipFrameAt(12, 8) == 1 &&
                MutinyEndingSequence.ShipFrameAt(15, 3) == 16 &&
                MutinyEndingSequence.ShipFrameAt(16, 3) == 1 &&
                MutinyEndingSequence.ShipLayers[12].SymbolIndex == 7 &&
                MutinyEndingSequence.ShipLayers[16].SymbolIndex == 8;
            result.Assert(nestedFrames,
                "END-SEQ-05 character 12-frame and ripple 16-frame child timelines loop independently");

            bool timing = MutinyEndingSequence.FrameAtElapsed(0f) == 1 &&
                MutinyEndingSequence.FrameAtElapsed(39f / 25f) == 40 &&
                MutinyEndingSequence.FrameAtElapsed(100f) == 774 &&
                MutinyEndingSequence.DialogueIndexAtFrame(39) == -1 &&
                MutinyEndingSequence.DialogueIndexAtFrame(40) == 0 &&
                MutinyEndingSequence.DialogueIndexAtFrame(301) == -1 &&
                MutinyEndingSequence.DialogueIndexAtFrame(310) == 1 &&
                MutinyEndingSequence.DialogueIndexAtFrame(425) == 2 &&
                MutinyEndingSequence.DialogueIndexAtFrame(535) == 3 &&
                MutinyEndingSequence.DialogueIndexAtFrame(645) == 4 &&
                MutinyEndingSequence.DialogueIndexAtFrame(747) == -1 &&
                MutinyEndingSequence.LineRevealAtFrame(40, 0, 0) > 0f &&
                MutinyEndingSequence.LineRevealAtFrame(40, 0, 1) == 0f &&
                MutinyEndingSequence.LineRevealAtFrame(110, 0, 6) == 1f &&
                !MutinyEndingSequence.IsScoreVisible(753) &&
                MutinyEndingSequence.IsScoreVisible(754) &&
                !MutinyEndingSequence.CanReturnToTitle(753) &&
                MutinyEndingSequence.CanReturnToTitle(754) &&
                MutinyEndingSequence.CanReturnToTitle(770) &&
                Mathf.Approximately(MutinyEndingSequence.ScorePanelTop(770), 91f) &&
                RectApproximately(MutinyEndingSequence.BackButtonRect(770),
                    new Rect(141f, 236f, 280f, 24f));
            result.Assert(timing,
                "END-SEQ-02/03/04 production timeline switches all five lines, reveals text, lands the score panel and stops at frame 774");

            GameObject controllerObject = null;
            GameObject managerObject = null;
            GameObject firstTeamObject = null;
            GameObject secondTeamObject = null;
            GameObject firstObject = null;
            GameObject secondObject = null;
            GameObject hudObject = null;
            GameObject frontendObject = null;
            int savedUnlock = Mutiny.Persistence.MutinySaveSystem.HighestUnlockedLevel;
            try
            {
                controllerObject = new GameObject("EndingVerification_LevelController");
                MutinyLevelController controller = controllerObject.AddComponent<MutinyLevelController>();
                controller.ConfigureSession(MutinyGameMode.SinglePlayer);
                controller.CurrentLevelIndex = 15;
                firstTeamObject = new GameObject("EndingVerification_PlayerTeam");
                secondTeamObject = new GameObject("EndingVerification_EnemyTeam");
                MutinyTeam firstTeam = firstTeamObject.AddComponent<MutinyTeam>();
                MutinyTeam secondTeam = secondTeamObject.AddComponent<MutinyTeam>();
                firstTeam.TeamNumber = 1;
                secondTeam.TeamNumber = 2;
                secondTeam.IsAiControlled = true;
                firstObject = new GameObject("EndingVerification_Player");
                secondObject = new GameObject("EndingVerification_Enemy");
                MutinyCharacter first = firstObject.AddComponent<MutinyCharacter>();
                MutinyCharacter second = secondObject.AddComponent<MutinyCharacter>();
                firstTeam.RegisterCharacter(first);
                secondTeam.RegisterCharacter(second);
                managerObject = new GameObject("EndingVerification_TurnManager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.Initialize(firstTeam, secondTeam);
                frontendObject = new GameObject("EndingVerification_Frontend");
                MutinyFrontendController frontend = frontendObject.AddComponent<MutinyFrontendController>();
                frontend.Initialize(controller);
                var flowField = typeof(MutinyFrontendController).GetField("m_Flow",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                MutinyFrontendFlow flow = flowField != null
                    ? flowField.GetValue(frontend) as MutinyFrontendFlow : null;
                if (flow != null)
                {
                    flow.PressPlay();
                    flow.PressOnePlayer();
                    flow.TrySelectLevel(15, level => level == 15);
                }
                MutinyAudioManager audio = MutinyAudioManager.Instance;
                AudioClip gameMusic = Resources.Load<AudioClip>("Audio/Music/game_music");
                AudioClip menuMusic = Resources.Load<AudioClip>("Audio/Music/menu_music");
                audio?.PlayMusic("game_music");
                hudObject = new GameObject("EndingVerification_Hud");
                MutinyGameHUD hud = hudObject.AddComponent<MutinyGameHUD>();
                hud.TurnManager = manager;
                hud.LevelController = controller;
                hud.FrontendController = frontend;

                second.TakeDamage(second.MaxHealth);
                second.ShownHealth = second.Health;
                for (int tick = 0; tick <= MutinyTurnManager.InactivitySettlingThreshold; tick++)
                    manager.AdvanceSimulationTick();
                bool opened = hud.SynchronizeGameEndPopup();
                int finalScore = controller.SinglePlayerScore;
                bool entered = hud.CompleteCampaignAndShowEnding();
                bool endingKeptGameMusic = entered && audio != null && audio.MusicSource != null &&
                    gameMusic != null && audio.MusicSource.clip == gameMusic;
                bool unloaded = controller.CurrentLevel == null;
                for (int tick = 1; tick < MutinyEndingSequence.FrameCount; tick++)
                    frontend.AdvanceEndingTick();
                int stoppedShipTick = frontend.CurrentEndingShipTick;
                int stoppedCharacterFrame = MutinyEndingSequence.ShipFrameAt(stoppedShipTick, 7);
                frontend.AdvanceEndingTick();
                bool childrenContinueAfterStop = frontend.CurrentEndingFrame == 774 &&
                    frontend.CurrentEndingShipTick == stoppedShipTick + 1 &&
                    MutinyEndingSequence.ShipFrameAt(frontend.CurrentEndingShipTick, 7) !=
                    stoppedCharacterFrame;
                bool endingFinishedWithGameMusic = audio.MusicSource.clip == gameMusic;
                bool returned = frontend.ReturnFromEndingToTitle();
                result.Assert(endingKeptGameMusic && endingFinishedWithGameMusic &&
                    returned && menuMusic != null &&
                    audio.MusicSource.clip == menuMusic,
                    "END-SEQ-07 final victory retains game_music through ending and back to title selects menu_music");
                result.Assert(flow != null && frontend.HasAnimatedEndingShip && opened && entered && returned &&
                    manager.GameResult == GameOverResult.Team1Wins &&
                    hud.GameEndPopupKind == MutinyGameEndPopupKind.GameComplete &&
                    finalScore >= 150 && frontend.CurrentEndingScore == finalScore &&
                    unloaded && frontend.CurrentEndingFrame == 774 &&
                    frontend.CurrentPage == MutinyFrontendPage.Title,
                    "END-SEQ-01/02/04 final-level production victory opens ending, advances to stop frame and returns to title");
                result.Assert(childrenContinueAfterStop,
                    "END-SEQ-05 original nested character clips continue after outer frame 774 stops");
            }
            finally
            {
                Mutiny.Persistence.MutinySaveSystem.HighestUnlockedLevel = savedUnlock;
                DestroyNow(frontendObject);
                DestroyNow(hudObject);
                DestroyNow(secondObject);
                DestroyNow(firstObject);
                DestroyNow(managerObject);
                DestroyNow(secondTeamObject);
                DestroyNow(firstTeamObject);
                DestroyNow(controllerObject);
            }
        }

        private static bool WeaponSmokeFollowsVisibleTrajectory<T>(bool fire) where T : MutinyWeapon
        {
            GameObject weaponObject = new GameObject($"SmokePositionVerification_{typeof(T).Name}");
            try
            {
                DestroySmokeTrails();
                T weapon = weaponObject.AddComponent<T>();
                weapon.Initialize(null);
                weapon.PhysicsBody.SetTerrain(new string[32, 32], 32, 32);
                PhysicsBodyState state = weapon.PhysicsBody.State;
                state.X = 320f;
                state.Y = 192f;
                state.Weight = fire ? 0f : 2f;
                weapon.PhysicsBody.State = state;
                weapon.transform.position = MutinyPhysics.PixelToUnity(state.X, state.Y);
                if (fire)
                    weapon.Fire(new Vector2(12f, -20f));

                weapon.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                weapon.PhysicsBody.ApplyPresentationPoseForVerification();
                MutinyRumBottleSmokeTrail[] trails = Object.FindObjectsByType<MutinyRumBottleSmokeTrail>(FindObjectsSortMode.None);
                if (trails.Length != 1)
                    return false;

                Vector2 smoke = MutinyPhysics.UnityToPixel(trails[0].transform.position);
                Vector2 initialVisible = MutinyPhysics.UnityToPixel(weapon.transform.position);
                Vector2 authoritative = new Vector2(weapon.PhysicsBody.State.X, weapon.PhysicsBody.State.Y);
                bool smokeAtVisibleStart = Vector2.Distance(smoke, initialVisible) < 0.001f &&
                    Vector2.Distance(authoritative, smoke) > 1f;

                weapon.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep * 0.5f);
                weapon.PhysicsBody.ApplyPresentationPoseForVerification();
                Vector2 halfwayVisible = MutinyPhysics.UnityToPixel(weapon.transform.position);
                Vector2 expectedHalfway = Vector2.Lerp(smoke, authoritative, 0.5f);
                return smokeAtVisibleStart &&
                    Vector2.Distance(halfwayVisible, expectedHalfway) < 0.001f &&
                    Vector2.Distance(smoke, MutinyPhysics.UnityToPixel(trails[0].transform.position)) < 0.001f;
            }
            finally
            {
                DestroyNow(weaponObject);
                DestroySmokeTrails();
            }
        }

        private static void VerifyBattleHud(MutinyLevel1VerificationResult result)
        {
            AssertTexture(result, "UI/BattleHUD/team1_panel", 132, 34);
            AssertTexture(result, "UI/BattleHUD/team2_panel", 132, 34);
            AssertTexture(result, "UI/BattleHUD/team1_portrait", 22, 21);
            Texture2D[] portraits = Resources.LoadAll<Texture2D>("UI/BattleHUD/Opponents");
            result.Assert(portraits.Length == 33,
                "HUD-03 all 33 original opponent portrait frames load from Resources");

            GameObject teamObject = null;
            GameObject firstObject = null;
            GameObject secondObject = null;
            try
            {
                teamObject = new GameObject("BattleHudVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                firstObject = new GameObject("BattleHudVerification_First");
                secondObject = new GameObject("BattleHudVerification_Second");
                MutinyCharacter first = firstObject.AddComponent<MutinyCharacter>();
                MutinyCharacter second = secondObject.AddComponent<MutinyCharacter>();
                first.Health = 100f;
                first.MaxHealth = 100f;
                second.Health = 50f;
                second.MaxHealth = 100f;
                team.RegisterCharacter(first);
                team.RegisterCharacter(second);

                result.Assert(MutinyGameHUD.ResolveTeamHealthTargetFrame(team) == 73,
                    "HUD-02 production team-health target uses 1 + floor(96 * totalHealth / totalMaxHealth)");
                result.Assert(MutinyGameHUD.SlideFrame(71, 73) == 72 &&
                              MutinyGameHUD.SlideFrame(73, 71) == 72 &&
                              MutinyGameHUD.SlideFrame(72, 72) == 72,
                    "HUD-02 production team-health frame moves exactly one frame per original tick");
                result.Assert(MutinyGameHUD.ProjectMapCoordinate(64f, -2) == 4,
                    "HUD-01 production minimap projection matches the original 3/32 pixel projection and offset");

                Rect mapBorder = MutinyGameHUD.ResolveOriginalMapBorderRect(20, 10);
                result.Assert(RectApproximately(mapBorder, new Rect(10f, 10f, 80f, 50f)),
                    "HUD-POS-01 minimap frame starts at stage (10,10) around holder (20,20)");
                result.Assert(RectApproximately(MutinyGameHUD.ResolveOriginalTeam1PanelRect(),
                                  new Rect(6f, 363.05f, 132f, 34f)) &&
                              RectApproximately(MutinyGameHUD.ResolveOriginalTeam2PanelRect(),
                                  new Rect(411.9f, 363.05f, 132f, 34f)),
                    "HUD-POS-02 team panels retain their original root timeline registration points");
                result.Assert(RectApproximately(MutinyGameHUD.ResolveOriginalTeam1HealthRect(),
                                  new Rect(37.95f, 381f, 96f, 8f)) &&
                              RectApproximately(MutinyGameHUD.ResolveOriginalTeam2HealthRect(),
                                  new Rect(415.85f, 381f, 96f, 8f)),
                    "HUD-POS-03 team health fills retain the original offsets from their panel origins");
                result.Assert(RectApproximately(MutinyGameHUD.ResolveOriginalTeam1PortraitRect(),
                                  new Rect(12.95f, 370f, 22f, 21f)) &&
                              RectApproximately(MutinyGameHUD.ResolveOriginalTeam2PortraitRect(),
                                  new Rect(505.85f, 337f, 40f, 58f)),
                    "HUD-POS-04 both portraits retain the original offsets from their team panels");
                result.Assert(
                    RectApproximately(MutinyGameHUD.ResolveWeaponSlotAmmoNumberRect(0, 0), new Rect(112f, 50f, 24f, 12f)) &&
                    RectApproximately(MutinyGameHUD.ResolveWeaponSlotInfiniteAmmoRect(0, 0), new Rect(115f, 52f, 18f, 9f)) &&
                    RectApproximately(MutinyGameHUD.ResolveWeaponSlotInfiniteAmmoRect(4, 2), new Rect(239f, 138f, 18f, 9f)),
                    "HUD-POS-05 weapon slot ammo text and infinite ammo symbol retain authentic Flash SWF 1831 twip offsets (540-80=460 twips -> y=23px)");
                result.Assert(
                    RectApproximately(MutinyGameHUD.ResolveOriginalWeaponsPanelRect(), new Rect(137.05f, 74.05f, 271f, 247f)),
                    "HUD-POS-06 weapon select panel retains root timeline placement (2741, 1481) twips -> (137.05, 74.05) px, centered at (275, 200)");
            }
            finally
            {
                DestroyNow(secondObject);
                DestroyNow(firstObject);
                DestroyNow(teamObject);
            }
        }

        private static bool RectApproximately(Rect actual, Rect expected)
        {
            return Mathf.Abs(actual.x - expected.x) < 0.001f &&
                   Mathf.Abs(actual.y - expected.y) < 0.001f &&
                   Mathf.Abs(actual.width - expected.width) < 0.001f &&
                   Mathf.Abs(actual.height - expected.height) < 0.001f;
        }

        private static void VerifyOriginalPanelCopy(MutinyLevel1VerificationResult result)
        {
            MutinyGameHUD.GetOriginalActionCopy(null, out string title, out string description);
            result.Assert(title == "weapons",
                "TURN-UI default title comes from the original WeaponSelectPanel");
            result.Assert(description == "Click one of the options above\nto select it.",
                "TURN-UI default description comes from the original WeaponSelectPanel");

            MutinyGameHUD.GetOriginalActionCopy("cherryBomb", out title, out description);
            result.Assert(title == "cherry bomb" && description.Contains("explodes on impact"),
                "TURN-UI weapon hover copy comes from original WeaponSelectButton.hoverText");
        }

        private static void AssertTexture(
            MutinyLevel1VerificationResult result, string path, int width, int height)
        {
            Texture2D texture = Resources.Load<Texture2D>(path);
            result.Assert(texture != null, $"Original action UI texture loads from Resources: {path}");
            if (texture != null)
                result.Assert(texture.width == width && texture.height == height,
                    $"Original action UI texture is {width}x{height}: {path}");
        }

        private static void VerifyProductionActionMethods(MutinyLevel1VerificationResult result)
        {
            GameObject managerObject = null;
            GameObject team1Object = null;
            GameObject team2Object = null;
            GameObject characterObject = null;
            GameObject inputObject = null;
            GameObject splashObject = null;

            try
            {
                managerObject = new GameObject("TurnActionUiVerification_Manager");
                team1Object = new GameObject("TurnActionUiVerification_Team1");
                team2Object = new GameObject("TurnActionUiVerification_Team2");
                characterObject = new GameObject("TurnActionUiVerification_Character");
                inputObject = new GameObject("TurnActionUiVerification_Input");

                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                MutinyTeam team1 = team1Object.AddComponent<MutinyTeam>();
                MutinyTeam team2 = team2Object.AddComponent<MutinyTeam>();
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();

                team1.TeamNumber = 1;
                team2.TeamNumber = 2;
                team1.RegisterCharacter(character);
                character.AddWeapon("cherryBomb");

                manager.Team1 = team1;
                manager.Team2 = team2;
                manager.CurrentTeam = team1;
                manager.CurrentPhase = TurnPhase.TurnActive;
                input.TurnManager = manager;

                bool initialCharacterSelected = input.TrySelectCharacterForVerification(
                    team1, MutinyPhysics.UnityToPixel(character.transform.position));
                result.Assert(initialCharacterSelected && team1.SelectedCharacter == character &&
                              input.IsActionMenuOpen,
                    "TURN-01 production selection accepts a character when a new turn begins with no selected character");

                team2.IsAiControlled = true;
                manager.CurrentTeam = team2;
                manager.CurrentPhase = TurnPhase.ActionExecuting;
                result.Assert(MutinyAIController.ResolveTurnGate(manager, team2) == MutinyAITurnGate.Wait,
                    "AI turn waits through transient ActionExecuting instead of abandoning its turn");
                manager.CurrentPhase = TurnPhase.TurnActive;
                result.Assert(MutinyAIController.ResolveTurnGate(manager, team2) == MutinyAITurnGate.Execute,
                    "AI turn proceeds when the same team returns to TurnActive");
                manager.CurrentTeam = team1;
                result.Assert(MutinyAIController.ResolveTurnGate(manager, team2) == MutinyAITurnGate.Cancel,
                    "AI turn cancels only after ownership moves to another team");
                team2.IsAiControlled = false;

                result.Assert(input.SelectCharacterThrow(),
                    "TURN-03 production input accepts the unused throw action");
                result.Assert(character.CanThrow && character.CanShoot,
                    "TURN-03 selecting throw does not consume either action");

                bool committed = input.TryCommitCharacterThrow(
                    character, new Vector2(100f, 100f), new Vector2(140f, 120f));
                result.Assert(committed, "TURN-04 production input commits a valid character throw");
                result.Assert(!character.CanThrow && character.CanShoot,
                    "TURN-04 committed character throw consumes only CanThrow");

                manager.CurrentPhase = TurnPhase.TurnActive;
                result.Assert(input.SelectWeapon("cherryBomb"),
                    "TURN-05 production input still accepts an owned weapon after character throw");

                result.Assert(!MutinyTurnManager.HasReachedInactivityThreshold(10),
                    "TURN-09 inactivity 10 does not settle the action");
                result.Assert(MutinyTurnManager.HasReachedInactivityThreshold(11),
                    "TURN-09 inactivity 11 settles the action");

                team1.StartTurn();
                result.Assert(character.CanThrow && character.CanShoot && team1.SelectedCharacter == null,
                    "TURN-07 production StartTurn restores both actions and returns to character selection");

                MutinyPhysicsBody body = character.PhysicsBody;
                body.WaterPixelY = 100f;
                PhysicsBodyState waterState = body.State;
                waterState.X = 64f;
                waterState.Y = 99f;
                waterState.Weight = 0f;
                waterState.VelocityX = 0f;
                waterState.VelocityY = 1f;
                body.State = waterState;
                body.SetTerrain(null, 0, 0);
                body.AdvanceSimulationTick();
                splashObject = GameObject.Find("WaterSplash");
                result.Assert(!body.IsInWater && !character.IsDrowned && splashObject != null &&
                              splashObject.GetComponent<MutinySplashEffect>() != null &&
                              Mathf.Approximately(splashObject.transform.position.x, 2f) &&
                              Mathf.Approximately(splashObject.transform.position.y, -100f / MutinyPhysics.PixelsPerUnit),
                    "VIS-SPLASH-01 character crossing at equality spawns at its current X and the surface Y before drowning");
                body.AdvanceSimulationTick();
                result.Assert(body.IsInWater && character.IsDrowned && !character.IsAlive,
                    "WATER-01 baked character Awake binding drowns after crossing the surface");
                result.Assert(Object.FindObjectsByType<MutinySplashEffect>(FindObjectsSortMode.None).Length == 1,
                    "VIS-SPLASH-01 drowning after the crossing does not spawn a second splash");

                // WATER-03: Weapon falling into water invalidation
                GameObject testDynamiteObject = new GameObject("WaterVerification_Dynamite");
                try
                {
                    MutinyDynamite testDynamite = testDynamiteObject.AddComponent<MutinyDynamite>();
                    testDynamite.Initialize(character);
                    testDynamite.PhysicsBody.WaterPixelY = 100f;
                    testDynamite.Fire(new Vector2(0f, 5f));
                    testDynamite.PhysicsBody.State.Y = 105f;
                    testDynamite.PhysicsBody.EvaluateWaterState();
                    result.Assert(testDynamite.PhysicsBody.IsInWater && !testDynamite.IsLit,
                        "WATER-03 Dynamite entering water extinguishes fuse");

                    // Submerged water depth invalidates and finishes the weapon
                    testDynamite.PhysicsBody.State.Y = 130f;
                    testDynamite.Finish();
                    result.Assert(testDynamite.IsFinished,
                        "WATER-04 Submerged weapon finishes without blocking turn manager");
                }
                finally
                {
                    DestroyNow(testDynamiteObject);
                }
            }
            finally
            {
                DestroyNow(splashObject);
                DestroyNow(inputObject);
                DestroyNow(characterObject);
                DestroyNow(team2Object);
                DestroyNow(team1Object);
                DestroyNow(managerObject);
            }
        }

        private static void VerifyOriginalSplashTimeline(MutinyLevel1VerificationResult result)
        {
            for (int sky = 1; sky <= 3; sky++)
            {
                GameObject splashObject = new GameObject($"SplashVerification_Sky{sky}");
                try
                {
                    MutinySplashEffect splash = splashObject.AddComponent<MutinySplashEffect>();
                    splash.Initialize(sky);
                    SpriteRenderer renderer = splashObject.GetComponent<SpriteRenderer>();
                    int firstFrame = (sky - 1) * 24 + 1;
                    bool firstVisible = renderer != null && renderer.enabled &&
                                        renderer.sprite != null && splash.CurrentSourceFrame == firstFrame &&
                                        renderer.sortingOrder == MutinyLevelBuilder.SplashSortingOrder &&
                                        renderer.sortingOrder < MutinyLevelBuilder.WaterSortingOrder &&
                                        Vector2.Distance(renderer.sprite.pivot, new Vector2(24f, 0f)) < 0.01f;
                    for (int tick = 2; tick <= 18; tick++)
                        splash.AdvanceOriginalTickForVerification();
                    bool lastVisible = renderer != null && renderer.enabled &&
                                       splash.CurrentSourceFrame == firstFrame + 17;
                    splash.AdvanceOriginalTickForVerification();
                    result.Assert(firstVisible && lastVisible && renderer != null && !renderer.enabled,
                        $"VIS-SPLASH-01 sky {sky} stays below water, displays frames {firstFrame}..{firstFrame + 17} and destroys on {firstFrame + 18}");
                }
                finally
                {
                    DestroyNow(splashObject);
                }
            }
        }

        private static void VerifyCherryBombWaterSplash(MutinyLevel1VerificationResult result)
        {
            GameObject bombObject = new GameObject("SplashVerification_CherryBomb");
            try
            {
                MutinyCherryBomb bomb = bombObject.AddComponent<MutinyCherryBomb>();
                bomb.Initialize(null);
                MutinyPhysicsBody body = bomb.PhysicsBody;
                body.WaterPixelY = 100f;
                body.SetTerrain(null, 0, 0);
                PhysicsBodyState state = body.State;
                state.X = 64f;
                state.Y = 99f;
                state.Weight = 0f;
                body.State = state;
                bomb.Fire(new Vector2(0f, 1f));

                int before = Object.FindObjectsByType<MutinySplashEffect>(FindObjectsSortMode.None).Length;
                body.AdvanceSimulationTick();
                int atSurface = Object.FindObjectsByType<MutinySplashEffect>(FindObjectsSortMode.None).Length;
                body.AdvanceSimulationTick();
                int afterEntry = Object.FindObjectsByType<MutinySplashEffect>(FindObjectsSortMode.None).Length;
                result.Assert(atSurface == before + 1 && afterEntry == atSurface && !bomb.IsFinished,
                    "VIS-SPLASH-02 CherryBomb splashes at equality once and entering water does not trigger contact explosion");

                state = body.State;
                state.Y = 101f;
                state.Weight = 0f;
                state.VelocityY = -2f;
                body.State = state;
                body.AdvanceSimulationTick();
                result.Assert(Object.FindObjectsByType<MutinySplashEffect>(FindObjectsSortMode.None).Length == afterEntry + 1,
                    "VIS-SPLASH-02 resurfacing produces a second crossing splash");
            }
            finally
            {
                foreach (MutinySplashEffect splash in Object.FindObjectsByType<MutinySplashEffect>(FindObjectsSortMode.None))
                    DestroyNow(splash.gameObject);
                DestroySmokeTrails();
                DestroyNow(bombObject);
            }
        }

        private static void VerifyBanana(MutinyLevel1VerificationResult result)
        {
            GameObject humanTeamObject = null;
            GameObject aiTeamObject = null;
            GameObject humanOwnerObject = null;
            GameObject aiOwnerObject = null;
            GameObject turnManagerObject = null;
            GameObject inputObject = null;
            GameObject bounceObject = null;
            GameObject aiBananaObject = null;
            try
            {
                result.Assert(Resources.Load<Sprite>("Art/Weapons/Banana/1") != null,
                    "WPN-03-ANI-01 original Banana sprite loads through Resources");
                result.Assert(Resources.Load<AudioClip>("Audio/SFX/banana_bounce") != null,
                    "WPN-03-AUD-01 original banana_bounce audio loads through Resources");

                humanTeamObject = new GameObject("BananaVerification_HumanTeam");
                MutinyTeam humanTeam = humanTeamObject.AddComponent<MutinyTeam>();
                humanTeam.TeamNumber = 1;
                humanOwnerObject = new GameObject("BananaVerification_HumanOwner");
                MutinyCharacter humanOwner = humanOwnerObject.AddComponent<MutinyCharacter>();
                humanOwner.AddWeapon("banana");
                humanTeam.RegisterCharacter(humanOwner);
                turnManagerObject = new GameObject("BananaVerification_TurnManager");
                MutinyTurnManager turnManager = turnManagerObject.AddComponent<MutinyTurnManager>();
                turnManager.CurrentTeam = humanTeam;
                turnManager.CurrentPhase = TurnPhase.TurnActive;
                inputObject = new GameObject("BananaVerification_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = turnManager;

                // TileSystem.mouseUp submits twanging.twang() directly in Flash;
                // Weapon.release's unrelated 20-force cap is not on this path.
                MutinyWeapon launched = MutinyWeaponFactory.SpawnAndLaunch(
                    "banana", humanOwner, Vector2.zero, new Vector2(-400f, 0f));
                MutinyBanana humanBanana = launched as MutinyBanana;
                result.Assert(humanBanana != null &&
                              Mathf.Approximately(humanBanana.PhysicsBody.State.VelocityX, 30f),
                    "BAN-PHY-01 production Banana twang commits the same 30-force limit shown by its preview");

                Vector2 previewPosition = Vector2.zero;
                Vector2 previewVelocity = MutinyPhysics.CalculateTwangVelocity(
                    Vector2.zero, new Vector2(-400f, 0f),
                    MutinyWeaponFactory.GetTwangMaxForce("banana"));
                bool previewMatchesActual = humanBanana != null;
                for (int tick = 0; tick < 5 && previewMatchesActual; tick++)
                {
                    previewVelocity = MutinyTrajectoryRenderer.PredictVelocityTick(
                        "banana", previewVelocity, MutinyPhysics.Gravity);
                    previewPosition += previewVelocity;
                    humanBanana.PhysicsBody.AdvanceSimulationTick();
                    previewMatchesActual =
                        Vector2.Distance(
                            previewPosition,
                            new Vector2(humanBanana.PhysicsBody.State.X, humanBanana.PhysicsBody.State.Y)) < 0.001f;
                }
                result.Assert(previewMatchesActual,
                    "BAN-TRAJ-01 Banana preview and production physics match for the first five unobstructed 25 Hz ticks");

                // The later click reaches the active banana after CanShoot has
                // already been consumed, then the next production simulation tick
                // creates the original 160/80 explosion.
                bool phaseGateAdmitsSecondClick = turnManager.CurrentPhase == TurnPhase.ActionExecuting &&
                                                  input.CanProcessCurrentTurnInputForVerification();
                bool detonationRequested = MutinyBanana.TryRequestPlayerDetonation(humanTeam);
                humanBanana.AdvanceOriginalTickForVerification();
                MutinyExplosion[] playerExplosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                result.Assert(phaseGateAdmitsSecondClick && detonationRequested && humanBanana.IsFinished && playerExplosions.Length == 1 &&
                              Mathf.Approximately(playerExplosions[0].Size, 160f) &&
                              Mathf.Approximately(playerExplosions[0].MaxDamage, 80f) &&
                              !playerExplosions[0].PlayPopOnHit,
                    "WPN-03-INT-01 production ActionExecuting input gate admits the second click and detonates Banana once at 160/80 without delayed duplicate pop");

                for (int i = 0; i < playerExplosions.Length; i++)
                    DestroyNow(playerExplosions[i].gameObject);
                DestroyNow(humanBanana.gameObject);

                // A physics floor contact must preserve the source bounce/friction
                // values and leave the banana armed instead of exploding on contact.
                bounceObject = new GameObject("BananaVerification_Bounce");
                MutinyBanana bouncingBanana = bounceObject.AddComponent<MutinyBanana>();
                bouncingBanana.Initialize(humanOwner);
                string[,] terrain = new string[4, 4];
                terrain[2, 2] = "solid";
                bouncingBanana.PhysicsBody.SetTerrain(terrain, 4, 4);
                PhysicsBodyState bounceState = bouncingBanana.PhysicsBody.State;
                bounceState.X = 64f;
                bounceState.Y = 48f;
                bounceState.Weight = 0f;
                bouncingBanana.PhysicsBody.State = bounceState;
                bouncingBanana.Fire(new Vector2(3f, 2f));
                bouncingBanana.PhysicsBody.AdvanceSimulationTick();
                result.Assert(!bouncingBanana.IsFinished &&
                              Mathf.Approximately(bouncingBanana.PhysicsBody.State.VelocityX, 2.5f) &&
                              Mathf.Approximately(bouncingBanana.PhysicsBody.State.VelocityY, -1.6f),
                    "WPN-03-EFF-02 production floor collision uses Banana bounce 0.8 and friction 0.5 without contact detonation");

                aiTeamObject = new GameObject("BananaVerification_AiTeam");
                MutinyTeam aiTeam = aiTeamObject.AddComponent<MutinyTeam>();
                aiTeam.TeamNumber = 2;
                aiTeam.IsAiControlled = true;
                aiOwnerObject = new GameObject("BananaVerification_AiOwner");
                MutinyCharacter aiOwner = aiOwnerObject.AddComponent<MutinyCharacter>();
                aiTeam.RegisterCharacter(aiOwner);
                PhysicsBodyState aiOwnerState = PhysicsBodyState.CreateDefault(64f, 64f);
                aiOwner.PhysicsBody.State = aiOwnerState;

                aiBananaObject = new GameObject("BananaVerification_AiBanana");
                MutinyBanana aiBanana = aiBananaObject.AddComponent<MutinyBanana>();
                aiBanana.Initialize(aiOwner);
                PhysicsBodyState aiState = aiBanana.PhysicsBody.State;
                aiState.X = 80f;
                aiState.Y = 64f;
                aiState.Weight = 0f;
                aiBanana.PhysicsBody.State = aiState;
                aiBanana.Fire(new Vector2(1f, 0f));
                aiBanana.AdvanceOriginalTickForVerification();
                result.Assert(aiBanana.IsFinished,
                    "WPN-03-EFF-03 production AI Banana detonates when any character is within original 20px range");
            }
            finally
            {
                MutinyExplosion[] explosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                for (int i = 0; i < explosions.Length; i++)
                    DestroyNow(explosions[i].gameObject);

                DestroyNow(aiBananaObject);
                DestroyNow(bounceObject);
                DestroyNow(inputObject);
                DestroyNow(turnManagerObject);
                DestroyNow(aiOwnerObject);
                DestroyNow(humanOwnerObject);
                DestroyNow(aiTeamObject);
                DestroyNow(humanTeamObject);
            }
        }

        private static void VerifyBananaLongFlight(MutinyLevel1VerificationResult result)
        {
            GameObject levelObject = null;
            GameObject humanTeamObject = null;
            GameObject aiTeamObject = null;
            GameObject humanOwnerObject = null;
            GameObject aiOwnerObject = null;
            GameObject managerObject = null;
            GameObject bananaObject = null;
            try
            {
                // A clear, tall level permits a genuine 30 px/tick twang to fly
                // beyond the old 3500 px cutoff and 150-tick turn watchdog.
                levelObject = new GameObject("BananaLongFlight_Level");
                MutinyLevelRoot level = levelObject.AddComponent<MutinyLevelRoot>();
                level.Width = 300;
                level.Height = 700;
                level.WaterLevelY = -1000f;

                humanTeamObject = new GameObject("BananaLongFlight_HumanTeam");
                MutinyTeam humanTeam = humanTeamObject.AddComponent<MutinyTeam>();
                humanTeam.TeamNumber = 1;
                humanOwnerObject = new GameObject("BananaLongFlight_HumanOwner");
                MutinyCharacter humanOwner = humanOwnerObject.AddComponent<MutinyCharacter>();
                humanOwner.AddWeapon("banana");
                humanTeam.RegisterCharacter(humanOwner);

                aiTeamObject = new GameObject("BananaLongFlight_AiTeam");
                MutinyTeam aiTeam = aiTeamObject.AddComponent<MutinyTeam>();
                aiTeam.TeamNumber = 2;
                aiTeam.IsAiControlled = true;
                aiOwnerObject = new GameObject("BananaLongFlight_AiOwner");
                MutinyCharacter aiOwner = aiOwnerObject.AddComponent<MutinyCharacter>();
                aiTeam.RegisterCharacter(aiOwner);

                managerObject = new GameObject("BananaLongFlight_TurnManager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.Initialize(humanTeam, aiTeam);
                humanTeam.SelectCharacter(humanOwner);

                MutinyBanana banana = MutinyWeaponFactory.SpawnAndLaunch(
                    "banana", humanOwner, Vector2.zero, new Vector2(-400f, 0f)) as MutinyBanana;
                bananaObject = banana != null ? banana.gameObject : null;
                result.Assert(banana != null, "BAN-LIFE-01 production factory launches the long-flight Banana");
                if (banana == null)
                    return;
                banana.PhysicsBody.SetTerrain(new string[level.Height, level.Width], level.Width, level.Height);

                for (int tick = 0; tick < 205; tick++)
                {
                    banana.PhysicsBody.AdvanceSimulationTick();
                    manager.AdvanceSimulationTick();
                }

                result.Assert(!banana.IsFinished &&
                              banana.PhysicsBody.State.X > 3500f &&
                              banana.PhysicsBody.State.Y < level.Height * MutinyPhysics.PixelsPerUnit &&
                              manager.CurrentTeam == humanTeam && manager.TurnCount == 0,
                    "BAN-LIFE-01 fired Banana survives 205 production ticks, the former turn watchdog and horizontal cutoff");

                for (int tick = 0; tick < 40 && !banana.IsFinished; tick++)
                {
                    banana.PhysicsBody.AdvanceSimulationTick();
                    manager.AdvanceSimulationTick();
                }
                result.Assert(banana.IsFinished &&
                              banana.PhysicsBody.State.Y > level.Height * MutinyPhysics.PixelsPerUnit &&
                              banana.PhysicsBody.State.VelocityY > 0f,
                    "BAN-LIFE-01 Banana finishes only after descending beyond the original level-bottom boundary");
            }
            finally
            {
                DestroyNow(bananaObject);
                DestroyNow(managerObject);
                DestroyNow(aiOwnerObject);
                DestroyNow(humanOwnerObject);
                DestroyNow(aiTeamObject);
                DestroyNow(humanTeamObject);
                DestroyNow(levelObject);
            }
        }

        private static void VerifySharedWeaponLifecycle(MutinyLevel1VerificationResult result)
        {
            GameObject levelObject = null;
            GameObject ownerObject = null;
            GameObject weaponObject = null;
            try
            {
                levelObject = new GameObject("WeaponLifecycleVerification_Level");
                MutinyLevelRoot level = levelObject.AddComponent<MutinyLevelRoot>();
                level.Width = 300;
                level.Height = 700;
                level.WaterLevelY = -1000f;
                string[,] clearTerrain = new string[level.Height, level.Width];
                ownerObject = new GameObject("WeaponLifecycleVerification_Owner");
                MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();

                string[] inheritedTypes = { "cherryBomb", "dynamite", "rumBottle" };
                for (int weaponIndex = 0; weaponIndex < inheritedTypes.Length; weaponIndex++)
                {
                    string weaponType = inheritedTypes[weaponIndex];
                    owner.AddWeapon(weaponType);
                    MutinyWeapon weapon = MutinyWeaponFactory.SpawnAndLaunch(
                        weaponType, owner, Vector2.zero, new Vector2(-400f, 0f));
                    weaponObject = weapon != null ? weapon.gameObject : null;
                    result.Assert(weapon != null, $"WPN-LIFE-01 production factory launches {weaponType}");
                    if (weapon == null)
                        continue;
                    weapon.PhysicsBody.SetTerrain(clearTerrain, level.Width, level.Height);

                    for (int tick = 0; tick < 205; tick++)
                        weapon.PhysicsBody.AdvanceSimulationTick();
                    result.Assert(!weapon.IsFinished && weapon.PhysicsBody.State.X > 3500f &&
                                  weapon.PhysicsBody.State.Y < level.Height * MutinyPhysics.PixelsPerUnit,
                        $"WPN-LIFE-01 {weaponType} survives 205 flight ticks past the removed world-X and lifetime cutoffs");

                    for (int tick = 0; tick < 40 && !weapon.IsFinished; tick++)
                        weapon.PhysicsBody.AdvanceSimulationTick();
                    result.Assert(weapon.IsFinished &&
                                  weapon.PhysicsBody.State.Y > level.Height * MutinyPhysics.PixelsPerUnit,
                        $"WPN-LIFE-01 {weaponType} ends on the inherited descending level-bottom rule");
                    DestroyNow(weaponObject);
                    weaponObject = null;
                    DestroySmokeTrails();
                }

                owner.AddWeapon("rumBottle");
                MutinyWeapon wetBottle = MutinyWeaponFactory.SpawnAndFire(
                    "rumBottle", owner, new Vector2(3f, 5f));
                weaponObject = wetBottle != null ? wetBottle.gameObject : null;
                if (wetBottle != null)
                {
                    wetBottle.PhysicsBody.SetTerrain(clearTerrain, level.Width, level.Height);
                    wetBottle.PhysicsBody.WaterPixelY = 0f;
                    for (int tick = 0; tick < 20; tick++)
                        wetBottle.PhysicsBody.AdvanceSimulationTick();
                    result.Assert(wetBottle.PhysicsBody.IsInWater &&
                                  !wetBottle.PhysicsBody.ApplyWaterMotion &&
                                  Mathf.Approximately(wetBottle.PhysicsBody.State.VelocityX, 3f) &&
                                  !wetBottle.IsFinished,
                        "WPN-WATER-01 fired bottle crosses water without character-only drag or generic water expiry");
                    DestroyNow(weaponObject);
                    weaponObject = null;
                    DestroySmokeTrails();
                }

                owner.AddWeapon("dynamite");
                MutinyDynamite wetDynamite = MutinyWeaponFactory.SpawnAndFire(
                    "dynamite", owner, new Vector2(0f, 5f)) as MutinyDynamite;
                weaponObject = wetDynamite != null ? wetDynamite.gameObject : null;
                if (wetDynamite != null)
                {
                    string[,] wetTerrain = new string[8, 8];
                    for (int column = 0; column < 8; column++)
                        wetTerrain[3, column] = "solid";
                    wetDynamite.PhysicsBody.SetTerrain(wetTerrain, 8, 8);
                    wetDynamite.PhysicsBody.WaterPixelY = 32f;
                    PhysicsBodyState startState = wetDynamite.PhysicsBody.State;
                    startState.X = 64f;
                    wetDynamite.PhysicsBody.State = startState;
                    bool sawUnlitBeforeFinish = false;
                    for (int tick = 0; tick < 80 && !wetDynamite.IsFinished; tick++)
                    {
                        wetDynamite.PhysicsBody.AdvanceSimulationTick();
                        sawUnlitBeforeFinish |= wetDynamite.PhysicsBody.IsInWater &&
                                                !wetDynamite.IsLit && !wetDynamite.IsFinished;
                    }
                    bool exploded = false;
                    MutinyExplosion[] explosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                    for (int i = 0; i < explosions.Length; i++)
                        exploded |= Mathf.Approximately(explosions[i].Size, 250f) &&
                                    Mathf.Approximately(explosions[i].MaxDamage, 70f);
                    result.Assert(sawUnlitBeforeFinish && wetDynamite.IsFinished && exploded,
                        "DYN-WATER-02 submerged production Dynamite shows unlit then detonates on physical rest, never dud-expires");
                }
            }
            finally
            {
                DestroyNow(weaponObject);
                MutinyExplosion[] explosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                for (int i = 0; i < explosions.Length; i++)
                    DestroyNow(explosions[i].gameObject);
                DestroySmokeTrails();
                DestroyNow(ownerObject);
                DestroyNow(levelObject);
            }
        }

        private static void VerifyParachuteBomb(MutinyLevel1VerificationResult result)
        {
            GameObject teamObject = null;
            GameObject managerObject = null;
            GameObject ownerObject = null;
            GameObject inputObject = null;
            GameObject impactObject = null;
            try
            {
                bool framesPresent = true;
                for (int frame = 1; frame <= 30; frame++)
                    framesPresent &= Resources.Load<Sprite>($"Art/Weapons/ParachuteBomb/{frame}") != null;
                result.Assert(framesPresent,
                    "WPN-08-ANI-01 all 30 original ParachuteBomb timeline frames load through Resources");

                teamObject = new GameObject("ParachuteBombVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                team.TeamNumber = 1;
                team.IsAiControlled = false;

                managerObject = new GameObject("ParachuteBombVerification_TurnManager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.CurrentTeam = team;
                manager.CurrentPhase = TurnPhase.TurnActive;

                ownerObject = CreateBoulderCharacter("ParachuteBombVerification_Owner", 0f, 0f);
                MutinyCharacter owner = ownerObject.GetComponent<MutinyCharacter>();
                owner.TeamIndex = 1;
                owner.AddWeapon("parachuteBomb");
                team.RegisterCharacter(owner);
                team.SelectCharacter(owner);

                inputObject = new GameObject("ParachuteBombVerification_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = manager;
                bool selected = input.SelectWeapon("parachuteBomb");
                bool launched = input.TryLaunchWeaponForVerification(owner, Vector2.zero, new Vector2(-400f, 0f));
                MutinyParachuteBomb bomb = Object.FindAnyObjectByType<MutinyParachuteBomb>();
                result.Assert(selected && launched && bomb != null && bomb.IsFired &&
                              Mathf.Approximately(bomb.PhysicsBody.State.VelocityX, 30f) &&
                              Mathf.Approximately(bomb.PhysicsBody.State.LeftExtent, MutinyParachuteBomb.OriginalExtentPixels) &&
                              Mathf.Approximately(bomb.PhysicsBody.State.TopExtent, MutinyParachuteBomb.OriginalExtentPixels) &&
                              bomb.PhysicsBody.State.HitsBoxes && !owner.HasWeapon("parachuteBomb") &&
                              !owner.CanThrow && !owner.CanShoot && manager.CurrentPhase == TurnPhase.ActionExecuting,
                    "WPN-08-INT-01/EFF-01 production twang preserves the original 30px/tick ParachuteBomb launch cap and Solid parameters");
                Vector2 fanMouse = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                input.UpdateSpecialWeaponCursorForVerification(owner, fanMouse, true);
                MutinySpecialWeaponCursor fanCursor =
                    input.GetComponentInChildren<MutinySpecialWeaponCursor>();
                Rect fanRect = fanCursor.CursorRectForScreenForVerification(1100, 800);
                result.Assert(input.SpecialWeaponCursorModeForVerification == "ParachuteFan" &&
                              Mathf.Approximately(fanRect.width, 62f) &&
                              Mathf.Approximately(fanRect.height, 42f) &&
                              Mathf.Approximately(fanRect.x, fanMouse.x - 30f) &&
                              Mathf.Approximately(fanRect.y, 800f - fanMouse.y - 20f),
                    "CUR-SCALE-01 production Parachute Bomb fan artwork and rotation hotspot use the 550x400 canvas scale");
                bomb.SetMobileFanStateForVerification(false, true);
                bool shortTapFirstTick = bomb.ConsumeMobileFanActiveForVerification();
                bool shortTapSecondTick = bomb.ConsumeMobileFanActiveForVerification();
                result.Assert(
                    MutinyParachuteBomb.ResolveMobileFanActive(true, false) &&
                    MutinyParachuteBomb.ResolveMobileFanActive(false, true) &&
                    !MutinyParachuteBomb.ResolveMobileFanActive(false, false) &&
                    shortTapFirstTick && !shortTapSecondTick,
                    "AND-PCB-FAN-01 Android fan remains active while held and consumes exactly one physics tick for a short tap");

                PhysicsBodyState state = bomb.PhysicsBody.State;
                state.X = 0f;
                state.Y = 0f;
                state.VelocityX = 10f;
                state.VelocityY = -10f;
                state.Weight = 1f;
                bomb.PhysicsBody.State = state;
                bomb.AdvanceOriginalTickForVerification(false, 0f);
                Vector2 previewVelocity = MutinyTrajectoryRenderer.PredictVelocityTick(
                    "parachuteBomb", new Vector2(10f, 2f), 1f);
                result.Assert(Mathf.Approximately(previewVelocity.x, 9.5f) &&
                              Mathf.Approximately(previewVelocity.y, 2f),
                    "WPN-08-EFF-02 production trajectory prediction applies ParachuteBomb's vy reduction, clamp, .95 drag, then gravity");
                result.Assert(!bomb.ChuteOpen && Mathf.Approximately(bomb.PhysicsBody.State.VelocityX, 9.5f),
                    "WPN-08-EFF-02 keeps the chute closed at the original vy=-10 threshold and applies .95 horizontal drag first");

                state = bomb.PhysicsBody.State;
                state.X = 0f;
                state.Y = 0f;
                state.VelocityX = 10f;
                state.VelocityY = -9.9f;
                state.Weight = 1f;
                bomb.PhysicsBody.State = state;
                bomb.AdvanceOriginalTickForVerification(true, -1f);
                result.Assert(bomb.ChuteOpen && bomb.IsFanActive && bomb.CurrentAnimationFrame == 11 &&
                              Mathf.Approximately(bomb.PhysicsBody.State.VelocityX, 9.7f) &&
                              Mathf.Approximately(bomb.PhysicsBody.State.VelocityY, -8.9f),
                    "WPN-08-EFF-02/INT-01 opens above -10 before gravity, begins frame 11 then advances, and mouse-left fans right by .2");

                state = bomb.PhysicsBody.State;
                state.X = 0f;
                state.Y = 0f;
                state.VelocityX = 10f;
                state.VelocityY = 2f;
                state.Weight = 1f;
                bomb.PhysicsBody.State = state;
                bomb.AdvanceOriginalTickForVerification(true, 0f);
                result.Assert(Mathf.Approximately(bomb.PhysicsBody.State.VelocityX, 9.3f),
                    "WPN-08-INT-01 treats a mouse exactly at the bomb as the original right-side fan branch");

                for (int tick = 0; tick < 18; tick++)
                    bomb.AdvanceOriginalTickForVerification(false, 0f);
                bomb.AdvanceOriginalTickForVerification(false, 0f);
                result.Assert(bomb.CurrentAnimationFrame == 26,
                    "WPN-08-ANI-01 loops DefineSprite 939 from frame 30 back to its original open frame 26");

                impactObject = new GameObject("ParachuteBombVerification_Impact");
                MutinyParachuteBomb impactBomb = impactObject.AddComponent<MutinyParachuteBomb>();
                impactBomb.Initialize(owner);
                string[,] terrain = new string[4, 4];
                terrain[2, 1] = "solid";
                impactBomb.PhysicsBody.SetTerrain(terrain, 4, 4);
                state = impactBomb.PhysicsBody.State;
                state.X = 48f;
                state.Y = 52f;
                state.VelocityX = 0f;
                state.VelocityY = 2f;
                state.Weight = 0f;
                impactBomb.PhysicsBody.State = state;
                impactBomb.Fire(new Vector2(0f, 2f));
                impactBomb.AdvanceOriginalTickForVerification(false, 0f);
                MutinyExplosion[] explosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                bool has160x50Explosion = false;
                for (int i = 0; i < explosions.Length; i++)
                {
                    has160x50Explosion |= Mathf.Approximately(explosions[i].Size, 160f) &&
                                           Mathf.Approximately(explosions[i].MaxDamage, 50f);
                }
                result.Assert(impactBomb.IsFinished && has160x50Explosion,
                    "WPN-08-EFF-03 production terrain contact hides ParachuteBomb and creates its 160/50 explosion");
            }
            finally
            {
                MutinyExplosion[] explosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                for (int i = 0; i < explosions.Length; i++)
                    DestroyNow(explosions[i].gameObject);
                foreach (MutinyParachuteBomb bomb in Object.FindObjectsByType<MutinyParachuteBomb>(FindObjectsSortMode.None))
                    DestroyNow(bomb != null ? bomb.gameObject : null);
                DestroyNow(impactObject);
                DestroyNow(inputObject);
                DestroyNow(ownerObject);
                DestroyNow(managerObject);
                DestroyNow(teamObject);
            }
        }
        private static void VerifyRumBottle(MutinyLevel1VerificationResult result)
        {
            GameObject twangObject = null;
            GameObject bottleObject = null;
            GameObject obliqueBottleObject = null;
            GameObject targetObject = null;
            try
            {
                bool bottleFramesPresent = true;
                for (int frame = 1; frame <= 12; frame++)
                    bottleFramesPresent &= Resources.Load<Sprite>($"Art/Weapons/RumBottle/{frame}") != null;
                result.Assert(bottleFramesPresent,
                    "WPN-10-ANI-01 all 12 original RumBottle timeline frames load through Resources");

                bool flameFramesPresent = true;
                for (int frame = 1; frame <= MutinySweepingFlame.OriginalFrameCount; frame++)
                    flameFramesPresent &= Resources.Load<Sprite>($"Art/Effects/SweepingFlame/{frame}") != null;
                result.Assert(flameFramesPresent,
                    "WPN-10-ANI-02 all 11 original sweepingFlame timeline frames load through Resources");

                // TileSystem.mouseUp calls twanging.twang, so the preview and
                // committed RumBottle flight both use Solid.twang's 30-force
                // limit; Weapon.release's 20-force cap only covers placement.
                twangObject = new GameObject("RumBottleVerification_Twang");
                MutinyRumBottle twangBottle = twangObject.AddComponent<MutinyRumBottle>();
                twangBottle.Initialize(null);
                twangBottle.PhysicsBody.SetTerrain(new string[32, 32], 32, 32);
                PhysicsBodyState twangState = twangBottle.PhysicsBody.State;
                twangState.X = 320f;
                twangState.Y = 192f;
                twangBottle.PhysicsBody.State = twangState;
                Vector2 trajectoryStart = new Vector2(320f, 192f);
                Vector2 trajectoryDrag = new Vector2(-80f, 192f);
                Vector2 previewVelocity = MutinyPhysics.CalculateTwangVelocity(
                    trajectoryStart, trajectoryDrag, MutinyWeaponFactory.GetTwangMaxForce("rumBottle"));
                twangBottle.Twang(trajectoryStart, trajectoryDrag);
                Vector2 firedVelocity = new Vector2(twangBottle.PhysicsBody.State.VelocityX,
                    twangBottle.PhysicsBody.State.VelocityY);
                result.Assert(Mathf.Approximately(twangBottle.TwangMaxForce, 30f) &&
                              Mathf.Approximately(previewVelocity.magnitude, 30f) &&
                              Vector2.Distance(firedVelocity, previewVelocity) < 0.001f,
                    "WPN-10-EFF-01 fired RumBottle matches the trajectory preview at its 30 px/tick limit");
                result.Assert(Mathf.Approximately(twangBottle.PhysicsBody.State.VelocityX, 30f) &&
                              Mathf.Approximately(twangBottle.PhysicsBody.State.VelocityY, 0f),
                    "RUM-PHY-01 production RumBottle twang commits the original 30px/tick full pull");

                Vector2 expectedPosition = trajectoryStart;
                Vector2 expectedVelocity = previewVelocity;
                bool trajectoryMatches = true;
                for (int tick = 0; tick < 5; tick++)
                {
                    expectedVelocity = MutinyTrajectoryRenderer.PredictVelocityTick(
                        "rumBottle", expectedVelocity, MutinyWeaponFactory.GetPredictionWeight("rumBottle"));
                    expectedPosition += expectedVelocity;
                    twangBottle.PhysicsBody.AdvanceSimulationTick();
                    trajectoryMatches &= Mathf.Approximately(twangBottle.PhysicsBody.State.X, expectedPosition.x) &&
                                         Mathf.Approximately(twangBottle.PhysicsBody.State.Y, expectedPosition.y);
                }
                result.Assert(trajectoryMatches,
                    "RUM-TRAJ-01 production preview ticks match five collision-free RumBottle physics ticks");

                // The production physics route must contact the floor, create its
                // 80/25 explosion, and seed left/right flames from the exposed top of
                // the impacted tile. Both seed flames hit the character at the origin.
                string[,] terrain = new string[5, 5];
                terrain[2, 1] = "solid";
                terrain[2, 2] = "solid";
                terrain[2, 3] = "solid";

                targetObject = new GameObject("RumBottleVerification_Target");
                MutinyCharacter target = targetObject.AddComponent<MutinyCharacter>();
                PhysicsBodyState targetState = PhysicsBodyState.CreateDefault(64f, 56f);
                targetState.Weight = 0f;
                target.PhysicsBody.State = targetState;
                target.PhysicsBody.SetTerrain(null, 0, 0);
                target.Health = 100f;
                target.MaxHealth = 100f;
                target.ShownHealth = 100f;

                bottleObject = new GameObject("RumBottleVerification_Impact");
                MutinyRumBottle bottle = bottleObject.AddComponent<MutinyRumBottle>();
                bottle.Initialize(null);
                bottle.PhysicsBody.SetTerrain(terrain, 5, 5);
                PhysicsBodyState bottleState = bottle.PhysicsBody.State;
                bottleState.X = 64f;
                bottleState.Y = 48f;
                bottleState.Weight = 0f;
                bottle.PhysicsBody.State = bottleState;
                bottle.Fire(new Vector2(0f, 2f));
                bottle.PhysicsBody.AdvanceSimulationTick();

                MutinySweepingFlame[] initialFlames = Object.FindObjectsByType<MutinySweepingFlame>(FindObjectsSortMode.None);
                result.Assert(bottle.IsFinished && initialFlames.Length == 2 && Mathf.Approximately(target.Health, 40f),
                    "WPN-10-EFF-02/03 production floor contact creates a 80/25 bottle blast and two 30-damage origin flames");

                Sprite initialFrame = Resources.Load<Sprite>("Art/Effects/SweepingFlame/1");
                result.Assert(initialFlames.Length == 2 &&
                              initialFlames[0].GetComponent<SpriteRenderer>().sprite == initialFrame &&
                              initialFlames[1].GetComponent<SpriteRenderer>().sprite == initialFrame,
                    "WPN-10-ANI-02 production floor flames begin on original sweepingFlame frame 1");

                for (int i = 0; i < initialFlames.Length; i++)
                {
                    for (int tick = 0; tick < 3; tick++)
                        initialFlames[i].AdvanceOriginalTickForVerification();
                }
                MutinySweepingFlame[] propagatedFlames = Object.FindObjectsByType<MutinySweepingFlame>(FindObjectsSortMode.None);
                Sprite propagationFrame = Resources.Load<Sprite>("Art/Effects/SweepingFlame/4");
                result.Assert(propagatedFlames.Length >= 4 && initialFlames[0].CreatedNext && initialFlames[1].CreatedNext &&
                              initialFlames[0].GetComponent<SpriteRenderer>().sprite == propagationFrame &&
                              initialFlames[1].GetComponent<SpriteRenderer>().sprite == propagationFrame,
                    "WPN-10-ANI-02 production flame plays original frame 4 while propagating 8px in both directions across exposed ground");

                string[,] obliqueTerrain = new string[5, 16];
                for (int column = 12; column <= 14; column++)
                    obliqueTerrain[2, column] = "solid";
                obliqueBottleObject = new GameObject("RumBottleVerification_ObliqueImpact");
                MutinyRumBottle obliqueBottle = obliqueBottleObject.AddComponent<MutinyRumBottle>();
                obliqueBottle.Initialize(null);
                obliqueBottle.PhysicsBody.SetTerrain(obliqueTerrain, 16, 5);
                PhysicsBodyState obliqueState = obliqueBottle.PhysicsBody.State;
                obliqueState.X = 405f;
                obliqueState.Y = 48f;
                obliqueBottle.PhysicsBody.State = obliqueState;
                obliqueBottle.Fire(new Vector2(20f, 2f));
                obliqueBottle.PhysicsBody.AdvanceSimulationTick();
                bool explosionAtVerticalContact = false;
                MutinyExplosion[] obliqueExplosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                for (int i = 0; i < obliqueExplosions.Length; i++)
                    explosionAtVerticalContact |= Mathf.Approximately(obliqueExplosions[i].PixelX, 405f);
                int flamesAtVerticalContact = 0;
                MutinySweepingFlame[] obliqueFlames = Object.FindObjectsByType<MutinySweepingFlame>(FindObjectsSortMode.None);
                for (int i = 0; i < obliqueFlames.Length; i++)
                    if (Mathf.Approximately(MutinyPhysics.UnityToPixel(obliqueFlames[i].transform.position).x, 384f))
                        flamesAtVerticalContact++;
                result.Assert(obliqueBottle.IsFinished &&
                              Mathf.Approximately(obliqueBottle.PhysicsBody.State.X, 425f) &&
                              explosionAtVerticalContact && flamesAtVerticalContact == 2,
                    "RUM-HIT-02 production floor contact creates blast and twin flames at pre-horizontal X while the bottle completes its motion tick");
            }
            finally
            {
                MutinySweepingFlame[] flames = Object.FindObjectsByType<MutinySweepingFlame>(FindObjectsSortMode.None);
                for (int i = 0; i < flames.Length; i++)
                    DestroyNow(flames[i].gameObject);

                MutinyExplosion[] explosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                for (int i = 0; i < explosions.Length; i++)
                    DestroyNow(explosions[i].gameObject);

                DestroyNow(bottleObject);
                DestroyNow(obliqueBottleObject);
                DestroyNow(targetObject);
                DestroyNow(twangObject);
            }
        }

        private static void VerifyMineCameraAndLevelCleanup(MutinyLevel1VerificationResult result)
        {
            GameObject mineObject = null;
            GameObject cameraObject = null;
            GameObject managerObject = null;
            GameObject controllerObject = null;
            try
            {
                mineObject = new GameObject("MineVerification_OutsideLevelRoot");
                MutinyMine mine = mineObject.AddComponent<MutinyMine>();
                mine.Initialize(null);
                mine.Fire(new Vector2(1f, 0f));

                managerObject = new GameObject("MineVerification_TurnManager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.CurrentPhase = TurnPhase.ActionExecuting;
                cameraObject = new GameObject("MineVerification_Camera");
                cameraObject.AddComponent<Camera>();
                MutinyCameraController camera = cameraObject.AddComponent<MutinyCameraController>();
                camera.TurnManager = manager;

                bool followedFlyingMine = camera.FindActionTargetForVerification() == mine.transform;
                mine.PhysicsBody.SetVelocity(0f, 0f);
                mine.AdvanceOriginalTickForVerification();
                camera.TrackWeapon(mine);
                result.Assert(followedFlyingMine && mine.IsStored &&
                              camera.FindActionTargetForVerification() == null,
                    "MINE-CAM-01 camera follows a flying mine but releases it after placement, including explicit tracking");

                controllerObject = new GameObject("MineVerification_LevelController");
                MutinyLevelController controller = controllerObject.AddComponent<MutinyLevelController>();
                controller.ClearLevel();
                result.Assert(!mineObject.activeSelf &&
                              Object.FindObjectsByType<MutinyMine>(FindObjectsSortMode.None).Length == 0,
                    "MINE-LVL-01 level rebuild immediately removes an unparented stored mine and its physics");
            }
            finally
            {
                DestroyNow(mineObject);
                DestroyNow(cameraObject);
                DestroyNow(managerObject);
                DestroyNow(controllerObject);
            }
        }

        private static bool MatchesSeagullFrame(MutinySeagull seagull, int frame)
        {
            Sprite expected = Resources.Load<Sprite>($"Art/Weapons/Seagull/{frame}");
            Sprite actual = seagull != null && seagull.SpriteRenderer != null
                ? seagull.SpriteRenderer.sprite
                : null;
            return expected != null && actual != null && actual.texture == expected.texture;
        }

        private static void VerifySeagull(MutinyLevel1VerificationResult result)
        {
            GameObject teamObject = null;
            GameObject ownerObject = null;
            GameObject seagullObject = null;
            GameObject turnObject = null;
            GameObject inputObject = null;
            GameObject firstShotObject = null;
            GameObject secondShotObject = null;
            try
            {
                bool framesPresent = Resources.Load<Sprite>("Art/Weapons/SeagullFire/1") != null;
                for (int frame = 1; frame <= 14; frame++)
                    framesPresent &= Resources.Load<Sprite>($"Art/Weapons/Seagull/{frame}") != null;
                result.Assert(framesPresent &&
                              Resources.Load<AudioClip>("Audio/SFX/poop1") != null &&
                              Resources.Load<AudioClip>("Audio/SFX/poop2") != null &&
                              Resources.Load<AudioClip>("Audio/SFX/poop3") != null,
                    "WPN-11-ANI/AUD original 14-frame Seagull, seagullFire and poop1..3 resources load through Resources");

                teamObject = new GameObject("SeagullVerification_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                team.TeamNumber = 1;
                ownerObject = new GameObject("SeagullVerification_Owner");
                MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();
                team.RegisterCharacter(owner);
                owner.CanShoot = true;
                owner.CanThrow = true;

                seagullObject = new GameObject("SeagullVerification_Bird");
                MutinySeagull seagull = seagullObject.AddComponent<MutinySeagull>();
                seagull.Initialize(owner);
                string[,] terrain = new string[10, 100];
                seagull.PhysicsBody.SetTerrain(terrain, 100, 10);
                seagull.PlaceAtFlightHeight(32f);
                result.Assert(seagull.IsFired &&
                              Mathf.Approximately(seagull.PhysicsBody.State.X, MutinySeagull.OriginalFlightStartX) &&
                              Mathf.Approximately(seagull.PhysicsBody.State.VelocityX, MutinySeagull.OriginalFlightSpeed) &&
                              !owner.CanShoot && !owner.CanThrow,
                    "WPN-11-INT production first click starts the -300px no-gravity flight and consumes both actions");

                // The real input gate must remain open after NotifyActionStarted
                // changes the turn to ActionExecuting; otherwise Update never
                // reaches its later TryRequestPlayerShot production branch.
                turnObject = new GameObject("SeagullVerification_TurnManager");
                MutinyTurnManager turnManager = turnObject.AddComponent<MutinyTurnManager>();
                turnManager.Team1 = team;
                turnManager.CurrentTeam = team;
                turnManager.CurrentPhase = TurnPhase.ActionExecuting;
                inputObject = new GameObject("SeagullVerification_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = turnManager;
                result.Assert(MutinySeagull.HasPlayerActiveFlight(team) && input.CanProcessCurrentTurnInputForVerification(),
                    "WPN-11-INT-02 production ActionExecuting input gate stays open while the player's Seagull flight awaits repeat clicks");

                for (int tick = 0; tick <= 150; tick++)
                    turnManager.AdvanceSimulationTick();
                result.Assert(!seagull.IsFinished,
                    "SEA-END-02 production turn watchdog does not expire a normally flying Seagull after 150 ticks");

                seagull.SpriteRenderer.sortingOrder = MutinyLevelBuilder.SeagullSortingOrder;
                bool acceptedShot = MutinySeagull.TryRequestPlayerShot(team);
                MutinySeagullFire[] shots = Object.FindObjectsByType<MutinySeagullFire>(FindObjectsSortMode.None);
                result.Assert(acceptedShot && shots.Length == 0 && seagull.ActiveShotCount == 0,
                    "SEA-SHOT-02 production click queues input until the Seagull's original simulation tick");

                seagull.PhysicsBody.AdvanceSimulationTick();
                shots = Object.FindObjectsByType<MutinySeagullFire>(FindObjectsSortMode.None);
                result.Assert(shots.Length == 1 && seagull.ActiveShotCount == 1 &&
                              Mathf.Approximately(seagull.PhysicsBody.State.X, -290f) &&
                              Mathf.Approximately(shots[0].PhysicsBody.State.X, -290f) &&
                              Mathf.Approximately(shots[0].PhysicsBody.State.Y, 33f) &&
                              Mathf.Approximately(shots[0].PhysicsBody.State.VelocityX, 10f) &&
                              Mathf.Approximately(shots[0].PhysicsBody.State.Weight, 1f) &&
                              shots[0].PhysicsBody.SimulationTickCount == 1 &&
                              !shots[0].PhysicsBody.IsActive,
                    "SEA-SHOT-02 production tick moves bird first, creates at x-10, then advances the parent-owned shot exactly once");

                if (shots.Length > 0)
                {
                    SpriteRenderer shotRenderer = shots[0].GetComponent<SpriteRenderer>();
                    result.Assert(shotRenderer != null &&
                                  shotRenderer.sortingLayerID == seagull.SpriteRenderer.sortingLayerID &&
                                  shotRenderer.sortingOrder < seagull.SpriteRenderer.sortingOrder,
                        "SEA-VIS-01 production shot is rendered below its parent Seagull in the same sorting layer");
                }
                else
                {
                    result.Assert(false, "SEA-VIS-01 production shot exists for rendering-order verification");
                }

                bool seagullFramesMatch = MatchesSeagullFrame(seagull, 11);
                for (int frame = 12; frame <= 14; frame++)
                {
                    seagull.PhysicsBody.AdvanceSimulationTick();
                    seagullFramesMatch &= MatchesSeagullFrame(seagull, frame);
                }
                seagull.PhysicsBody.AdvanceSimulationTick();
                seagullFramesMatch &= MatchesSeagullFrame(seagull, 1);
                for (int frame = 2; frame <= 8; frame++)
                {
                    seagull.PhysicsBody.AdvanceSimulationTick();
                    seagullFramesMatch &= MatchesSeagullFrame(seagull, frame);
                }
                seagull.PhysicsBody.AdvanceSimulationTick();
                seagullFramesMatch &= MatchesSeagullFrame(seagull, 1);
                result.Assert(seagullFramesMatch && seagull.SpriteRenderer.enabled,
                    "SEA-ANI-01 production shot renders 11-14 then flight 1-8 without displaying transparent frames 9/10");

                firstShotObject = shots.Length > 0 ? shots[0].gameObject : null;
                if (shots.Length > 0)
                {
                    string[,] impactTerrain = new string[5, 8];
                    impactTerrain[2, 1] = "solid";
                    impactTerrain[2, 2] = "solid";
                    shots[0].PhysicsBody.SetTerrain(impactTerrain, 8, 5);
                    PhysicsBodyState impactState = shots[0].PhysicsBody.State;
                    impactState.X = 48f;
                    impactState.Y = 53f;
                    impactState.VelocityY = 2f;
                    shots[0].PhysicsBody.State = impactState;
                    seagull.PhysicsBody.AdvanceSimulationTick();
                }
                MutinyExplosion[] explosions = Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None);
                bool hasSeagullExplosion = false;
                for (int i = 0; i < explosions.Length; i++)
                    hasSeagullExplosion |= Mathf.Approximately(explosions[i].Size, 50f) &&
                                          Mathf.Approximately(explosions[i].MaxDamage, 50f);
                result.Assert(hasSeagullExplosion && seagull.ActiveShotCount == 0,
                    "WPN-11-EFF seagullFire terrain contact creates the original 50/50 explosion and ends that child shot");

                DestroyNow(firstShotObject);
                firstShotObject = null;

                MutinySeagull.TryRequestPlayerShot(team);
                seagull.PhysicsBody.AdvanceSimulationTick();
                shots = Object.FindObjectsByType<MutinySeagullFire>(FindObjectsSortMode.None);
                if (shots.Length > 0)
                {
                    secondShotObject = shots[0].gameObject;
                    shots[0].PhysicsBody.WaterPixelY = 40f;
                    PhysicsBodyState waterState = shots[0].PhysicsBody.State;
                    waterState.Y = 41f;
                    shots[0].PhysicsBody.State = waterState;
                    seagull.PhysicsBody.AdvanceSimulationTick();
                }
                result.Assert(seagull.ActiveShotCount == 0,
                    "WPN-11-EFF seagullFire water entry ends the child without creating another explosion");
            }
            finally
            {
                DestroyNow(secondShotObject);
                DestroyNow(firstShotObject);
                MutinySeagullFire[] shots = Object.FindObjectsByType<MutinySeagullFire>(FindObjectsSortMode.None);
                for (int i = 0; i < shots.Length; i++)
                    DestroyNow(shots[i].gameObject);
                DestroyNow(seagullObject);
                DestroyNow(ownerObject);
                DestroyNow(teamObject);
                DestroyNow(inputObject);
                DestroyNow(turnObject);
            }
        }

        private static void VerifySeagullPresentation(MutinyLevel1VerificationResult result)
        {
            long referenceImpactTick = -1;
            long referenceWaterTick = -1;
            var existingShots = new HashSet<MutinySeagullFire>(Object.FindObjectsByType<MutinySeagullFire>(FindObjectsSortMode.None));
            var existingExplosions = new HashSet<MutinyExplosion>(Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None));
            foreach (int fps in new[] { 25, 60, 120 })
            {
                GameObject birdObject = null;
                try
                {
                    birdObject = new GameObject($"SeagullPresentation_{fps}FPS");
                    MutinySeagull bird = birdObject.AddComponent<MutinySeagull>();
                    bird.Initialize(null);
                    bird.PhysicsBody.SetTerrain(new string[100, 100], 100, 100);
                    bird.PhysicsBody.WaterPixelY = 2000f;
                    bird.CallAirstrike(400f, 100f, 0f);
                    bool accepted = bird.RequestShotForVerification();
                    bird.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                    MutinySeagullFire first = null;
                    foreach (MutinySeagullFire shot in Object.FindObjectsByType<MutinySeagullFire>(FindObjectsSortMode.None))
                    {
                        if (!existingShots.Contains(shot))
                            first = shot;
                    }
                    result.Assert(accepted && first != null && first.PhysicsBody.SimulationTickCount == 1 &&
                                  !first.PhysicsBody.IsActive,
                        $"SEA-PRES-01 {fps}FPS birth tick advances the parent-owned child exactly once");
                    if (first == null)
                        continue;

                    first.PhysicsBody.ApplyPresentationPoseForVerification();
                    bird.PhysicsBody.ApplyPresentationPoseForVerification();
                    Vector2 birthPose = MutinyPhysics.UnityToPixel(first.transform.position);
                    result.Assert(Vector2.Distance(birthPose, new Vector2(400f, 100f)) < 0.001f &&
                                  Vector2.Distance(new Vector2(first.PhysicsBody.State.X, first.PhysicsBody.State.Y),
                                      new Vector2(410f, 101f)) < 0.001f,
                        $"SEA-PRES-01 {fps}FPS first visible child starts at the birth tick origin, not ahead of the displayed bird");
                    typeof(MutinyPhysicsBody).GetMethod("Start",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                        .Invoke(first.PhysicsBody, null);
                    result.Assert(first.PhysicsBody.State.X == 410f && first.PhysicsBody.State.Y == 101f &&
                                  first.PhysicsBody.SimulationTickCount == 1,
                        $"SEA-PRES-02 {fps}FPS delayed Start cannot write the displayed birth pose back into authority");
                    first.PhysicsBody.SetTerrain(new string[100, 100], 100, 100);
                    first.PhysicsBody.WaterPixelY = 2000f;

                    bird.PhysicsBody.AdvanceSimulationFrameForVerification(0.02f);
                    first.PhysicsBody.AdvanceSimulationFrameForVerification(0.02f);
                    first.PhysicsBody.ApplyPresentationPoseForVerification();
                    bird.PhysicsBody.ApplyPresentationPoseForVerification();
                    Vector2 halfPose = MutinyPhysics.UnityToPixel(first.transform.position);
                    result.Assert(Vector2.Distance(halfPose, new Vector2(405f, 100.5f)) < 0.001f &&
                                  Mathf.Abs(first.PhysicsBody.SimulationInterpolationAlpha -
                                      bird.PhysicsBody.SimulationInterpolationAlpha) < 0.0001f &&
                                  Mathf.Abs(first.transform.position.x - bird.transform.position.x) < 0.0001f &&
                                  first.PhysicsBody.SimulationTickCount == 1 && first.PhysicsBody.State.Y == 101f,
                        $"SEA-PRES-01 {fps}FPS mid-tick child Transform interpolates gravity and shares bird phase without stepping State");

                    bool frameChecksPassed = true;
                    bool interTickMoved = fps == 25;
                    MutinySeagullFire second = null;
                    bool secondRequested = false;
                    long secondBirthParentTick = -1;
                    for (int frame = 0; frame < fps && bird.PhysicsBody.SimulationTickCount < 15; frame++)
                    {
                        if (!secondRequested && bird.PhysicsBody.SimulationTickCount >= 5)
                            secondRequested = bird.RequestShotForVerification();
                        long oldCount = first.PhysicsBody.SimulationTickCount;
                        Vector3 oldPose = first.transform.position;
                        // This is the child's real autonomous-frame entry: it must
                        // remain inert even though presentation is enabled.
                        first.PhysicsBody.AdvanceSimulationFrameForVerification(1f / fps);
                        if (second != null)
                            second.PhysicsBody.AdvanceSimulationFrameForVerification(1f / fps);
                        frameChecksPassed &= first.PhysicsBody.SimulationTickCount == oldCount;
                        bird.PhysicsBody.AdvanceSimulationFrameForVerification(1f / fps);
                        if (secondRequested && second == null)
                        {
                            foreach (MutinySeagullFire shot in Object.FindObjectsByType<MutinySeagullFire>(FindObjectsSortMode.None))
                            {
                                if (shot != first && !existingShots.Contains(shot))
                                    second = shot;
                            }
                            if (second != null)
                                secondBirthParentTick = bird.PhysicsBody.SimulationTickCount;
                        }
                        first.PhysicsBody.ApplyPresentationPoseForVerification();
                        bird.PhysicsBody.ApplyPresentationPoseForVerification();
                        frameChecksPassed &= first.PhysicsBody.SimulationTickCount == bird.PhysicsBody.SimulationTickCount &&
                            Mathf.Abs(first.transform.position.x - bird.transform.position.x) < 0.0001f &&
                            Mathf.Abs(first.PhysicsBody.State.X - bird.PhysicsBody.State.X) < 0.001f;
                        if (first.PhysicsBody.SimulationTickCount == oldCount && first.transform.position != oldPose)
                            interTickMoved = true;
                        if (second != null)
                        {
                            second.PhysicsBody.ApplyPresentationPoseForVerification();
                            frameChecksPassed &= second.PhysicsBody.SimulationTickCount ==
                                bird.PhysicsBody.SimulationTickCount - secondBirthParentTick + 1 &&
                                Mathf.Abs(second.transform.position.x - bird.transform.position.x) < 0.0001f;
                        }
                    }
                    result.Assert(frameChecksPassed && interTickMoved && second != null && bird.ActiveShotCount == 2,
                        $"SEA-PRES-01 {fps}FPS successive rendered frames and repeated drops share phase, never double-step, and retain sub-tick motion");

                    // Use real contact and water-entry paths, not a test-only finish.
                    if (second != null)
                    {
                        second.PhysicsBody.WaterPixelY = second.PhysicsBody.State.Y + 20f;
                        long waterStart = second.PhysicsBody.SimulationTickCount;
                        for (int frame = 0; frame < fps * 2 && bird.ActiveShotCount == 2; frame++)
                            bird.PhysicsBody.AdvanceSimulationFrameForVerification(1f / fps);
                        long waterTicks = second.PhysicsBody.SimulationTickCount - waterStart;
                        if (referenceWaterTick < 0)
                            referenceWaterTick = waterTicks;
                        int waterExplosionCount = 0;
                        foreach (MutinyExplosion explosion in Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None))
                            if (!existingExplosions.Contains(explosion))
                                waterExplosionCount++;
                        result.Assert(bird.ActiveShotCount == 1 && waterTicks == referenceWaterTick && waterExplosionCount == 0,
                            $"SEA-PRES-02 {fps}FPS real water entry ends on the same child tick without explosion");
                        DestroyNow(second.gameObject);
                    }

                    string[,] impactTerrain = new string[100, 100];
                    for (int x = 0; x < 100; x++)
                        impactTerrain[12, x] = "solid";
                    first.PhysicsBody.SetTerrain(impactTerrain, 100, 100);
                    for (int frame = 0; frame < fps * 2 && bird.ActiveShotCount > 0; frame++)
                        bird.PhysicsBody.AdvanceSimulationFrameForVerification(1f / fps);
                    long impactTick = first.PhysicsBody.SimulationTickCount;
                    if (referenceImpactTick < 0)
                        referenceImpactTick = impactTick;
                    int explosionCount = 0;
                    bool explosionMatches = false;
                    foreach (MutinyExplosion explosion in Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None))
                    {
                        if (!existingExplosions.Contains(explosion))
                        {
                            explosionCount++;
                            explosionMatches = explosion.Size == 50f && explosion.MaxDamage == 50f;
                        }
                    }
                    result.Assert(bird.ActiveShotCount == 0 && impactTick == referenceImpactTick &&
                                  explosionCount == 1 && explosionMatches,
                        $"SEA-PRES-02 {fps}FPS real terrain contact ends on the same physics tick with exactly one 50/50 explosion");
                }
                finally
                {
                    foreach (MutinySeagullFire shot in Object.FindObjectsByType<MutinySeagullFire>(FindObjectsSortMode.None))
                        if (!existingShots.Contains(shot))
                            DestroyNow(shot.gameObject);
                    foreach (MutinyExplosion explosion in Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None))
                        if (!existingExplosions.Contains(explosion))
                            DestroyNow(explosion.gameObject);
                    DestroyNow(birdObject);
                }
            }
        }

        private static void VerifyTidalWave(MutinyLevel1VerificationResult result)
        {
            GameObject waveObject = null;
            GameObject targetObject = null;
            GameObject outsideObject = null;
            MutinyAudioManager audioManager = null;
            string playedSfx = null;
            bool previousSfxEnabled = false;
            System.Action<string> audioListener = null;
            try
            {
                bool framesPresent = true;
                for (int frame = 1; frame <= 27; frame++)
                    framesPresent &= Resources.Load<Sprite>($"Art/Weapons/TidalWave/{frame}") != null;
                result.Assert(framesPresent &&
                              MutinyTidalWave.ResolveOriginalSkyColourForLevel(1) == 1 &&
                              MutinyTidalWave.ResolveOriginalSkyColourForLevel(6) == 2 &&
                              MutinyTidalWave.ResolveOriginalSkyColourForLevel(11) == 3 &&
                              Resources.Load<Sprite>("Art/Weapons/TidalWave/1") != Resources.Load<Sprite>("Art/Weapons/TidalWave/10") &&
                              Resources.Load<Sprite>("Art/Weapons/TidalWave/10") != Resources.Load<Sprite>("Art/Weapons/TidalWave/19"),
                    "WPN-12-ANI original 27-frame timeline loads and level 1/6/11 select distinct anim1/anim2/anim3 colour groups");

                targetObject = new GameObject("TidalWaveVerification_Target");
                MutinyCharacter target = targetObject.AddComponent<MutinyCharacter>();
                PhysicsBodyState targetState = PhysicsBodyState.CreateDefault(-530f, 100f);
                targetState.Weight = 0f;
                target.PhysicsBody.State = targetState;
                target.Health = 100f;
                target.MaxHealth = 100f;
                target.ShownHealth = 100f;

                outsideObject = new GameObject("TidalWaveVerification_Outside");
                MutinyCharacter outside = outsideObject.AddComponent<MutinyCharacter>();
                PhysicsBodyState outsideState = PhysicsBodyState.CreateDefault(-379f, 100f);
                outsideState.Weight = 0f;
                outside.PhysicsBody.State = outsideState;
                outside.Health = 100f;
                outside.MaxHealth = 100f;
                outside.ShownHealth = 100f;

                waveObject = new GameObject("TidalWaveVerification_Wave");
                MutinyTidalWave wave = waveObject.AddComponent<MutinyTidalWave>();
                wave.Initialize(null);
                bool hiddenBeforeStart = wave.SpriteRenderer != null && !wave.SpriteRenderer.enabled;
                wave.PhysicsBody.SetTerrain(new string[4, 8], 8, 4);
                wave.StartWave(200f, 400f, -1f);
                bool visibleAfterStart = wave.SpriteRenderer != null && wave.SpriteRenderer.enabled;
                result.Assert(hiddenBeforeStart && visibleAfterStart && wave.IsFired &&
                              wave.SpriteRenderer != null && wave.SpriteRenderer.sortingOrder == MutinyLevelBuilder.TidalWaveSortingOrder &&
                              Mathf.Approximately(wave.PhysicsBody.State.X, -550f) &&
                              Mathf.Approximately(wave.PhysicsBody.State.Y, 400f) &&
                              Mathf.Approximately(wave.PhysicsBody.State.VelocityX, 20f),
                    "WPN-12-INT production startWave ignores click direction and starts the original rightward -550px wave (hidden during preview, visible on wave start)");

                audioManager = MutinyAudioManager.Instance;
                previousSfxEnabled = audioManager.SfxEnabled;
                audioManager.SfxEnabled = true;
                audioListener = soundName => playedSfx = soundName;
                audioManager.SfxPlayed += audioListener;
                wave.PhysicsBody.AdvanceSimulationTick();
                result.Assert(Mathf.Approximately(target.Health, 95f) &&
                              Mathf.Approximately(outside.Health, 100f) &&
                              wave.CurrentVisibleFrame == 2 && wave.CurrentSourceFrame == 2 &&
                              Resources.Load<AudioClip>("Audio/SFX/splash") != null && playedSfx == "splash",
                    "WPN-12-EFF/ANI/AUD production tick damages only the inclusive ±150px window for 5 HP, advances sky-colour frame 2, and emits inherited splash once");

                PhysicsBodyState waveState = wave.PhysicsBody.State;
                waveState.X = 807f; // terrain width 8 * 32 + original +550 exit padding
                wave.PhysicsBody.State = waveState;
                wave.AdvanceOriginalTickForVerification();
                result.Assert(wave.IsFinished,
                    "WPN-12-EFF production wave finishes only after passing levelWidth + 550px");
            }
            finally
            {
                if (audioManager != null)
                {
                    if (audioListener != null)
                        audioManager.SfxPlayed -= audioListener;
                    audioManager.SfxEnabled = previousSfxEnabled;
                }

                MutinySplashEffect[] splashes = Object.FindObjectsByType<MutinySplashEffect>(FindObjectsSortMode.None);
                for (int i = 0; i < splashes.Length; i++)
                    DestroyNow(splashes[i].gameObject);
                DestroyNow(waveObject);
                DestroyNow(outsideObject);
                DestroyNow(targetObject);
            }
        }

        private static void VerifyLongRunningWaveTurn(MutinyLevel1VerificationResult result)
        {
            GameObject levelObject = null;
            GameObject humanTeamObject = null;
            GameObject aiTeamObject = null;
            GameObject ownerObject = null;
            GameObject opponentObject = null;
            GameObject managerObject = null;
            GameObject waveObject = null;
            try
            {
                levelObject = new GameObject("LongWaveTurnVerification_Level");
                MutinyLevelRoot level = levelObject.AddComponent<MutinyLevelRoot>();
                level.Width = 200;
                level.Height = 20;
                level.WaterLevelY = -12.5f;
                humanTeamObject = new GameObject("LongWaveTurnVerification_HumanTeam");
                MutinyTeam humanTeam = humanTeamObject.AddComponent<MutinyTeam>();
                humanTeam.TeamNumber = 1;
                ownerObject = new GameObject("LongWaveTurnVerification_Owner");
                MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();
                humanTeam.RegisterCharacter(owner);
                aiTeamObject = new GameObject("LongWaveTurnVerification_AiTeam");
                MutinyTeam aiTeam = aiTeamObject.AddComponent<MutinyTeam>();
                aiTeam.TeamNumber = 2;
                aiTeam.IsAiControlled = true;
                opponentObject = new GameObject("LongWaveTurnVerification_Opponent");
                MutinyCharacter opponent = opponentObject.AddComponent<MutinyCharacter>();
                aiTeam.RegisterCharacter(opponent);
                managerObject = new GameObject("LongWaveTurnVerification_Manager");
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.Initialize(humanTeam, aiTeam);
                humanTeam.SelectCharacter(owner);

                MutinyTidalWave wave = MutinyWeaponFactory.SpawnWeapon("tidalWave", owner) as MutinyTidalWave;
                waveObject = wave != null ? wave.gameObject : null;
                if (wave == null)
                {
                    result.Assert(false, "WPN-LIFE-02 production factory creates long-running Tidal Wave");
                    return;
                }
                wave.PhysicsBody.SetTerrain(new string[level.Height, level.Width], level.Width, level.Height);
                wave.StartWave(0f, 400f);
                manager.NotifyActionStarted();
                for (int tick = 0; tick < 205; tick++)
                {
                    wave.PhysicsBody.AdvanceSimulationTick();
                    manager.AdvanceSimulationTick();
                }
                result.Assert(!wave.IsFinished && manager.CurrentTeam == humanTeam && manager.TurnCount == 0 &&
                              Mathf.Approximately(wave.PhysicsBody.State.X, -550f + 205f * MutinyTidalWave.OriginalSpeed),
                    "WPN-LIFE-02 Tidal Wave remains active after 205 production turn ticks without a 150-tick forced finish");

                for (int tick = 0; tick < 200 && !wave.IsFinished; tick++)
                {
                    wave.PhysicsBody.AdvanceSimulationTick();
                    manager.AdvanceSimulationTick();
                }
                for (int tick = 0; tick < 11; tick++)
                    manager.AdvanceSimulationTick();
                result.Assert(wave.IsFinished && manager.CurrentTeam == aiTeam && manager.TurnCount == 1,
                    "WPN-LIFE-02 wave exits by its own level-width rule before the normal 11-tick turn transition");
            }
            finally
            {
                DestroyNow(waveObject);
                DestroyNow(managerObject);
                DestroyNow(opponentObject);
                DestroyNow(ownerObject);
                DestroyNow(aiTeamObject);
                DestroyNow(humanTeamObject);
                DestroyNow(levelObject);
            }
        }

        private static void VerifyVoodooDoll(MutinyLevel1VerificationResult result)
        {
            GameObject inputObject = null;
            GameObject turnObject = null;
            GameObject playerTeamObject = null;
            GameObject enemyTeamObject = null;
            GameObject ownerObject = null;
            GameObject targetObject = null;
            GameObject dollObject = null;
            GameObject cameraObject = null;
            GameObject levelRootObject = null;
            try
            {
                result.Assert(Resources.Load<Sprite>("Art/Weapons/VoodooDoll/1") != null &&
                              Resources.Load<Sprite>("UI/CharacterOverlay/voodoo_target") != null &&
                              Resources.Load<AudioClip>("Audio/SFX/voodoo") != null,
                    "WPN-13-ANI original doll, target-crosshair and voodoo audio resources load through Resources");

                playerTeamObject = new GameObject("VoodooVerification_PlayerTeam");
                MutinyTeam playerTeam = playerTeamObject.AddComponent<MutinyTeam>();
                playerTeam.TeamNumber = 1;
                enemyTeamObject = new GameObject("VoodooVerification_EnemyTeam");
                MutinyTeam enemyTeam = enemyTeamObject.AddComponent<MutinyTeam>();
                enemyTeam.TeamNumber = 2;
                enemyTeam.IsAiControlled = true;
                turnObject = new GameObject("VoodooVerification_TurnManager");
                MutinyTurnManager turnManager = turnObject.AddComponent<MutinyTurnManager>();
                turnManager.Team1 = playerTeam;
                turnManager.Team2 = enemyTeam;
                turnManager.CurrentTeam = playerTeam;
                turnManager.CurrentPhase = TurnPhase.TurnActive;

                levelRootObject = new GameObject("VoodooVerification_LevelRoot");
                MutinyLevelRoot levelRoot = levelRootObject.AddComponent<MutinyLevelRoot>();
                levelRoot.Width = 100;
                levelRoot.Height = 20;
                levelRoot.WaterLevelY = -20f;
                cameraObject = new GameObject("VoodooVerification_Camera");
                Camera unityCamera = cameraObject.AddComponent<Camera>();
                unityCamera.orthographic = true;
                unityCamera.orthographicSize = 200f / MutinyPhysics.PixelsPerUnit;
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                MutinyCameraController cameraController = cameraObject.AddComponent<MutinyCameraController>();
                cameraController.TurnManager = turnManager;

                ownerObject = new GameObject("VoodooVerification_Owner");
                MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();
                owner.transform.position = MutinyPhysics.PixelToUnity(0f, 0f);
                PhysicsBodyState ownerState = PhysicsBodyState.CreateDefault(100f, 100f);
                ownerState.Weight = 0f;
                owner.PhysicsBody.State = ownerState;
                owner.WeaponInventory["voodooDoll"] = 3;
                owner.WeaponInventory["anchor"] = 1;
                playerTeam.RegisterCharacter(owner);
                playerTeam.SelectCharacter(owner);

                targetObject = new GameObject("VoodooVerification_Target");
                MutinyCharacter target = targetObject.AddComponent<MutinyCharacter>();
                target.transform.position = MutinyPhysics.PixelToUnity(30f, 0f);
                PhysicsBodyState targetState = PhysicsBodyState.CreateDefault(220f, 100f);
                targetState.Weight = 0f;
                targetState.VelocityX = -3f;
                targetState.VelocityY = 4f;
                target.PhysicsBody.State = targetState;
                enemyTeam.RegisterCharacter(target);

                inputObject = new GameObject("VoodooVerification_Input");
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = turnManager;
                input.GameCamera = unityCamera;
                Vector2 cursorScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                bool anchorSelected = input.SelectWeapon("anchor");
                input.UpdateSpecialWeaponCursorForVerification(owner, cursorScreen, false);
                Texture2D anchorCursor = input.SpecialWeaponCursorTextureForVerification;
                result.Assert(anchorSelected && input.SpecialWeaponCursorModeForVerification == "Anchor" &&
                              anchorCursor != null && anchorCursor.name == "OriginalCursor_Anchor" &&
                              anchorCursor.width == 31 && anchorCursor.height == 22,
                    "ANC-CUR-01 production Anchor selection shows the original 31x22 cursor asset");
                MutinySpecialWeaponCursor specialCursor =
                    input.GetComponentInChildren<MutinySpecialWeaponCursor>();
                Rect anchorRect = specialCursor.CursorRectForScreenForVerification(1100, 800);
                result.Assert(Mathf.Approximately(anchorRect.width, 62f) &&
                              Mathf.Approximately(anchorRect.height, 44f) &&
                              Mathf.Approximately(anchorRect.x, cursorScreen.x - 30f) &&
                              Mathf.Approximately(anchorRect.y, 800f - cursorScreen.y - 22f),
                    "CUR-SCALE-01 production Anchor cursor artwork and hotspot double with a 1100x800 canvas");
                input.CancelWeaponSelection();
                input.UpdateSpecialWeaponCursorForVerification(owner, cursorScreen, false);
                result.Assert(input.SpecialWeaponCursorModeForVerification == "None",
                    "ANC-CUR-01 cancelling Anchor selection restores the ordinary cursor");

                bool voodooArmed = input.SelectWeapon("voodooDoll");
                Vector3 dollCenter = input.ArmedVoodooDoll != null
                    ? input.ArmedVoodooDoll.SpriteRenderer.bounds.center
                    : Vector3.zero;
                Vector2 overDollScreen = unityCamera.WorldToScreenPoint(dollCenter);
                Vector2 awayFromDollScreen = unityCamera.WorldToScreenPoint(dollCenter + Vector3.right * 2f);
                input.UpdateSpecialWeaponCursorForVerification(owner, awayFromDollScreen, false);
                Texture2D voodooCursor = input.SpecialWeaponCursorTextureForVerification;
                result.Assert(voodooArmed && input.SpecialWeaponCursorModeForVerification == "VoodooDoll" &&
                              voodooCursor != null && voodooCursor.name == "OriginalCursor_VoodooDoll" &&
                              voodooCursor.width == 31 && voodooCursor.height == 22,
                    "VOO-CUR-01 production untargeted Voodoo Doll shows the original 31x22 cursor away from the doll");
                input.UpdateSpecialWeaponCursorForVerification(owner, overDollScreen, false);
                result.Assert(input.SpecialWeaponCursorModeForVerification == "None",
                    "VOO-CUR-01 hovering the Voodoo Doll body restores the ordinary cursor");
                bool selectedAt30 = input.SelectWeapon("voodooDoll") &&
                                      input.TrySelectVoodooTargetForVerification(
                                          playerTeam, owner, MutinyPhysics.PixelToUnity(0f, 0f));
                result.Assert(!selectedAt30 && input.ArmedVoodooDoll != null && !input.ArmedVoodooDoll.HasTarget,
                    "WPN-13-INT-02 production target selection rejects the original exact 30px boundary");
                input.CancelWeaponSelection();

                target.transform.position = MutinyPhysics.PixelToUnity(29f, 0f);
                bool selectedAt29 = input.SelectWeapon("voodooDoll") &&
                                      input.TrySelectVoodooTargetForVerification(
                                          playerTeam, owner, MutinyPhysics.PixelToUnity(0f, 0f));
                result.Assert(selectedAt29 && input.ArmedVoodooDoll != null &&
                              input.ArmedVoodooDoll.TargetCharacter == target,
                    "WPN-13-INT-02 production target selection accepts an enemy at 29px and exposes its target marker state");
                input.UpdateSpecialWeaponCursorForVerification(owner, awayFromDollScreen, false);
                result.Assert(input.SpecialWeaponCursorModeForVerification == "None",
                    "VOO-CUR-01 binding a target restores the ordinary cursor before twang aiming");
                input.CancelWeaponSelection();
                result.Assert(owner.GetAmmunition("voodooDoll") == 3,
                    "WPN-13-INT-04 production cancellation destroys an unfired doll without consuming inventory");

                dollObject = new GameObject("VoodooVerification_Doll");
                MutinyVoodooDoll doll = dollObject.AddComponent<MutinyVoodooDoll>();
                doll.Initialize(owner);
                doll.Fire(new Vector2(8f, -6f));
                result.Assert(!doll.IsFired && !doll.IsTwangable,
                    "WPN-13-INT-01 production doll rejects a throw until a live target is selected");

                result.Assert(doll.BindTarget(target) && doll.IsTwangable,
                    "WPN-13-INT-02 production doll binds the selected target and only then enables twang");
                result.Assert(cameraController.IsPanningToCharacter(owner),
                    "VOO-CAM-01 production target binding sets panToCharacter back to the doll owner");
                doll.Fire(new Vector2(8f, -6f));
                result.Assert(cameraController.TrackedWeaponForVerification == doll &&
                              !cameraController.IsPanningToCharacter(owner),
                    "VOO-CAM-01 production throw clears the owner pan and explicitly tracks the flying doll");

                for (int tick = 0; tick < MutinyVoodooDoll.OriginalOwnerFlightTicks; tick++)
                    doll.AdvanceOriginalTickForVerification();
                result.Assert(doll.IsTargetFocusRequested && !doll.HasTransferredTargetVelocity &&
                              cameraController.TrackedWeaponForVerification == null &&
                              cameraController.IsPanningToCharacter(target) &&
                              cameraController.FindActionTargetForVerification() != doll.transform &&
                              Mathf.Approximately(target.PhysicsBody.State.VelocityX, -3f) &&
                              Mathf.Approximately(target.PhysicsBody.State.VelocityY, 4f),
                    "VOO-CAM-02 production tenth tick releases doll tracking and assigns the one-time target-character pan");

                int waitBeforeCameraArrival = doll.FramesOnTarget;
                doll.AdvanceOriginalTickForVerification(false);
                result.Assert(doll.FramesOnTarget == waitBeforeCameraArrival &&
                              !doll.HasTransferredTargetVelocity,
                    "VOO-CAM-02 target wait does not advance while panToCharacter still names the victim");

                for (int tick = 0; tick <= 150; tick++)
                    turnManager.AdvanceSimulationTick();
                result.Assert(!doll.IsFinished,
                    "VOO-END-02 production turn watchdog cannot expire a doll waiting for its target-camera pan");

                Vector3 targetCameraPosition = target.transform.position;
                targetCameraPosition.y = 0f;
                targetCameraPosition.z = -10f;
                cameraObject.transform.position = targetCameraPosition;
                cameraController.AdvanceCameraForVerification();
                result.Assert(!cameraController.IsPanningToCharacter(target) &&
                              cameraController.CanAcceptManualScrollingForVerification(),
                    "VOO-CAM-02/03 production camera clears panToCharacter at the victim and restores ordinary manual scrolling");

                for (int tick = 0; tick < MutinyVoodooDoll.OriginalTargetWaitTicks; tick++)
                    doll.AdvanceOriginalTickForVerification(false);
                result.Assert(!doll.HasTransferredTargetVelocity && doll.FramesOnTarget == 0,
                    "WPN-13-EFF-01 production doll waits the original 10 target-camera ticks after focus");

                doll.AdvanceOriginalTickForVerification();
                result.Assert(doll.HasTransferredTargetVelocity &&
                              cameraController.TrackedWeaponForVerification == null &&
                              cameraController.FindActionTargetForVerification() != doll.transform &&
                              Mathf.Approximately(target.PhysicsBody.State.VelocityX, 8f) &&
                              Mathf.Approximately(target.PhysicsBody.State.VelocityY, -6f),
                    "VOO-CAM-03/VOO-XFER-01 target receives the saved velocity once without the fading doll reclaiming camera tracking");

                doll.PhysicsBody.SetVelocity(-12f, 7f);
                doll.AdvanceOriginalTickForVerification();
                result.Assert(Mathf.Approximately(target.PhysicsBody.State.VelocityX, 8f) &&
                              Mathf.Approximately(target.PhysicsBody.State.VelocityY, -6f),
                    "WPN-13-EFF-02 target velocity is not continuously mirrored from later doll motion");

                for (int tick = 1; tick < 10; tick++)
                    doll.AdvanceOriginalTickForVerification();
                result.Assert(doll.IsFinished,
                    "WPN-13-ANI production doll fades by 10% per tick and finishes after the tenth fade step");
            }
            finally
            {
                DestroyNow(dollObject);
                DestroyNow(inputObject);
                DestroyNow(targetObject);
                DestroyNow(ownerObject);
                DestroyNow(turnObject);
                DestroyNow(enemyTeamObject);
                DestroyNow(playerTeamObject);
                DestroyNow(cameraObject);
                DestroyNow(levelRootObject);
            }
        }

        private static void VerifyGMUnlockAllLevels(MutinyGMManager gm, MutinyLevel1VerificationResult result)
        {
            int savedUnlock = Mutiny.Persistence.MutinySaveSystem.HighestUnlockedLevel;
            try
            {
                Mutiny.Persistence.MutinySaveSystem.HighestUnlockedLevel = 1;
                bool succeeded = gm.ExecuteCommand("unlockalllevels");
                bool allUnlocked = true;
                for (int level = 1; level <= Mutiny.Persistence.MutinySaveSystem.MaxLevel; level++)
                    allUnlocked &= Mutiny.Persistence.MutinySaveSystem.IsLevelUnlocked(level);
                result.Assert(succeeded && allUnlocked &&
                              Mutiny.Persistence.MutinySaveSystem.HighestUnlockedLevel ==
                                  Mutiny.Persistence.MutinySaveSystem.MaxLevel &&
                              PlayerPrefs.GetInt("mutiny_highest_unlocked_level", 1) ==
                                  Mutiny.Persistence.MutinySaveSystem.MaxLevel &&
                              gm.RecentSuccessfulCommands.Count > 0 &&
                              gm.RecentSuccessfulCommands[0] == "unlockalllevels",
                    "GM-03 lowercase unlockalllevels unlocks every level, persists progress and enters recent history");

                Mutiny.Persistence.MutinySaveSystem.HighestUnlockedLevel = 1;
                bool replaySucceeded = gm.RunRecentCommand(0);
                result.Assert(replaySucceeded &&
                              Mutiny.Persistence.MutinySaveSystem.HighestUnlockedLevel ==
                                  Mutiny.Persistence.MutinySaveSystem.MaxLevel &&
                              gm.RecentSuccessfulCommands.Count > 0 &&
                              gm.RecentSuccessfulCommands[0] == "unlockalllevels",
                    "GM-03 recent command button repeats the production all-level unlock");
            }
            finally
            {
                Mutiny.Persistence.MutinySaveSystem.HighestUnlockedLevel = savedUnlock;
            }
        }

        private static void VerifyGMManager(MutinyLevel1VerificationResult result, bool includeLegacyChecks = true)
        {
            GameObject gmObject = new GameObject("GM_Verification_Host");
            GameObject characterObject = new GameObject("GM_Verification_Character");
            GameObject aiTeamObject = null;
            GameObject aiCharacterObject = null;
            GameObject enemyTeamObject = null;
            GameObject enemyCharacterObject = null;
            try
            {
                MutinyGMManager gm = gmObject.AddComponent<MutinyGMManager>();
                VerifyGMUnlockAllLevels(gm, result);
                VerifyExplosionCameraCommands(gm, result);
                bool logEnabled = gm.ExecuteCommand("  AiLoG 1  ") && MutinyAIController.ActionLogEnabled;
                result.Assert(logEnabled, "GM-10 production GM parser enables AI action logging with case and outer whitespace tolerance");
                int logHistoryCount = gm.RecentSuccessfulCommands.Count;
                bool invalidLogCommandsRejected = !gm.ExecuteCommand("ailog") &&
                    !gm.ExecuteCommand("ailog 2") && !gm.ExecuteCommand("ailog -1") &&
                    !gm.ExecuteCommand("ailog 1 extra") && !gm.ExecuteCommand("ailog nope") &&
                    MutinyAIController.ActionLogEnabled && gm.RecentSuccessfulCommands.Count == logHistoryCount;
                result.Assert(invalidLogCommandsRejected,
                    "GM-10 invalid AI log commands preserve the production toggle and successful command history");
                bool logDisabled = gm.ExecuteCommand("ailog 0") && !MutinyAIController.ActionLogEnabled;
                result.Assert(logDisabled, "GM-10 production GM parser disables AI action logging");
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                character.TeamIndex = 1;
                character.IsSelected = true;
                character.PhysicsBody.State = PhysicsBodyState.CreateDefault(10000f, 100f);

                if (includeLegacyChecks)
                {
                    gm.ExecuteCommand("UnlockWeapons");

                    result.Assert(character.HasWeapon("cherryBomb") && character.IsInfinite("cherryBomb"),
                        "GM-01 UnlockWeapons unlocks cherryBomb with infinite ammo");
                    result.Assert(character.HasWeapon("cannon") && character.HasWeapon("cannonball") && character.IsInfinite("cannon"),
                        "GM-02 UnlockWeapons unlocks cannon and cannonball aliases");
                    result.Assert(character.GetAmmunition("banana") == -1 && character.GetAmmunition("tidalWave") == -1,
                        "GM-03 UnlockWeapons sets ammunition to -1 (infinite) for all weapons");
                    result.Assert(character.CanShoot,
                        "GM-04 UnlockWeapons enables CanShoot on target character");
                    result.Assert(Mathf.Approximately(MutinyGMManager.BaseButtonSize, 30f) &&
                                  RectApproximately(MutinyGMManager.ResolveButtonRect(400f), new Rect(4f, 185f, 30f, 30f)),
                        "GM-05 GM button size is 30px on 550x400 reference canvas and scales relative to canvas size");
                    result.Assert(RectApproximately(MutinyGMManager.ResolveButtonRect(1100f, 800f), new Rect(8f, 370f, 60f, 60f)) &&
                                  RectApproximately(MutinyGMManager.ResolveButtonRect(1920f, 1080f), new Rect(228.3f, 499.5f, 81f, 81f)) &&
                                  RectApproximately(MutinyGMManager.ResolveButtonRect(3840f, 2160f), new Rect(456.6f, 999f, 162f, 162f)),
                        "GM-06 GM button anchors to the left edge of the visible letterboxed game canvas and scales dynamically with resolution");
                }

                aiTeamObject = new GameObject("GM_ForceAiTeam");
                MutinyTeam aiTeam = aiTeamObject.AddComponent<MutinyTeam>();
                aiTeam.TeamNumber = 2;
                aiTeam.IsAiControlled = true;
                MutinyAIController ai = aiTeamObject.AddComponent<MutinyAIController>();
                ai.UseFixedDecisionSeed = true;
                ai.FixedDecisionSeed = 41901;
                ai.SaveDecisionTrace = false;

                aiCharacterObject = new GameObject("GM_ForceAiCharacter");
                MutinyCharacter aiCharacter = aiCharacterObject.AddComponent<MutinyCharacter>();
                aiCharacter.TeamIndex = 2;
                aiCharacter.Luck = 3f;
                aiCharacter.PhysicsBody.State = PhysicsBodyState.CreateDefault(100f, 100f);
                aiCharacter.PhysicsBody.WaterPixelY = 10000f;
                aiCharacter.AddWeapon("banana", 2);
                aiTeam.RegisterCharacter(aiCharacter);

                enemyTeamObject = new GameObject("GM_ForceEnemyTeam");
                MutinyTeam enemyTeam = enemyTeamObject.AddComponent<MutinyTeam>();
                enemyTeam.TeamNumber = 3;
                enemyCharacterObject = new GameObject("GM_ForceEnemyCharacter");
                MutinyCharacter enemy = enemyCharacterObject.AddComponent<MutinyCharacter>();
                enemy.TeamIndex = 3;
                enemy.PhysicsBody.State = PhysicsBodyState.CreateDefault(10000f, 100f);
                enemyTeam.RegisterCharacter(enemy);
                var enemies = new List<MutinyCharacter> { enemy };
                var allies = new List<MutinyCharacter> { aiCharacter };

                gm.ExecuteCommand("aiweapon 1");
                result.Assert(MutinyAIController.ForcedWeaponId == 1, "GM-07 aiweapon alias sets forced weapon");
                gm.ExecuteCommand("aiforceusewaepon 1");
                AIMove forcedCherry = ai.EvaluateCharacterWeaponsForVerification(
                    aiCharacter, enemies, allies, null, 0, 0, 10000f, out int cherryCandidates);
                result.Assert(MutinyAIController.ForcedWeaponId == 1 && !aiCharacter.HasWeapon("cherryBomb") &&
                              forcedCherry.WeaponType == "cherryBomb" && forcedCherry.UsesForcedWeaponSupply &&
                              cherryCandidates == 3 && aiCharacter.GetAmmunition("banana") == 2,
                    "GM-07 forced Cherry Bomb enters the production AI dispatcher without changing real inventory");

                aiCharacter.CanThrow = true;
                aiCharacter.CanShoot = true;
                ai.EvaluateBestMove();
                MutinyAIDecisionTrace firstTrace = ai.LastDecisionTrace;
                bool hasJump = false;
                bool onlyCherryWeapons = true;
                foreach (MutinyAICandidateRecord candidate in firstTrace.Candidates)
                {
                    hasJump |= candidate.MoveType == nameof(AIMoveType.SelfThrow);
                    if (candidate.MoveType == nameof(AIMoveType.ShootWeapon) && candidate.Weapon != "cherryBomb")
                        onlyCherryWeapons = false;
                }
                result.Assert(hasJump && onlyCherryWeapons && firstTrace.Candidates.Count >= 53,
                    "GM-07 forced weapon preserves the production first-action jump candidates and excludes other weapons");

                aiCharacter.CanThrow = false;
                aiCharacter.CanShoot = true;
                aiCharacter.Luck = 0f;
                aiTeam.SelectCharacter(aiCharacter);
                AIMove continuation = ai.EvaluateBestMove();
                result.Assert(continuation.MoveType == AIMoveType.Pass &&
                              ai.LastDecisionTrace.Candidates.Count == 0,
                    "GM-07 AI can still pass after a jump when no forced Cherry Bomb shot qualifies");

                aiCharacter.Luck = 3f;
                aiCharacter.AddWeapon("cherryBomb", 2);
                aiCharacter.CanThrow = true;
                aiCharacter.CanShoot = true;
                ai.ExecuteMoveForVerification(forcedCherry);
                result.Assert(aiCharacter.GetAmmunition("cherryBomb") == 2 &&
                              aiCharacter.GetAmmunition("banana") == 2,
                    "GM-07 production forced shot does not consume the character's real ammunition");

                gm.ExecuteCommand("aiforceusewaepon 0");
                aiCharacter.WeaponInventory.Remove("cherryBomb");
                AIMove restored = ai.EvaluateCharacterWeaponsForVerification(
                    aiCharacter, enemies, allies, null, 0, 0, 10000f, out int restoredCandidates);
                result.Assert(MutinyAIController.ForcedWeaponId == 0 && restored.WeaponType == "banana" &&
                              !restored.UsesForcedWeaponSupply && restoredCandidates == 3 &&
                              aiCharacter.GetAmmunition("banana") == 2,
                    "GM-07 zero restores normal production inventory-based candidate selection");

                gm.ExecuteCommand("aiforceusewaepon 6");
                gm.ExecuteCommand("aiforceusewaepon 16");
                gm.ExecuteCommand("aiforceusewaepon nope");
                gm.ExecuteCommand("aiforceusewaepon");
                result.Assert(MutinyAIController.ForcedWeaponId == 6 &&
                              MutinyAIController.ForcedWeaponType == "banana",
                    "GM-07 invalid IDs do not change the active AI override");

                string[] expectedWeapons =
                {
                    "cherryBomb", "boulder", "dynamite", "piecesOfEight", "rumBottle",
                    "banana", "parachuteBomb", "woodenCrate", "gunpowderBarrel", "seagull",
                    "mine", "cannon", "anchor", "voodooDoll", "tidalWave"
                };
                bool allIdsMapToMenuWeapons = true;
                for (int weaponId = 1; weaponId <= expectedWeapons.Length; weaponId++)
                {
                    gm.ExecuteCommand($"aiforceusewaepon {weaponId}");
                    allIdsMapToMenuWeapons &= MutinyAIController.ForcedWeaponId == weaponId &&
                                              MutinyAIController.ForcedWeaponType == expectedWeapons[weaponId - 1];
                }
                result.Assert(allIdsMapToMenuWeapons,
                    "GM-07 IDs 1..15 map to the 15 selectable weapons in menu order");

                bool sixSucceeded = gm.ExecuteCommand("Help") &&
                                    gm.ExecuteCommand("aiforceusewaepon 1") &&
                                    gm.ExecuteCommand("aiforceusewaepon 2") &&
                                    gm.ExecuteCommand("Help") &&
                                    gm.ExecuteCommand("aiforceusewaepon 3") &&
                                    gm.ExecuteCommand("  hElP  ");
                result.Assert(sixSucceeded && gm.RecentSuccessfulCommands.Count == 5 &&
                              gm.RecentSuccessfulCommands[0] == "hElP" &&
                              gm.RecentSuccessfulCommands[1] == "aiforceusewaepon 3" &&
                              gm.RecentSuccessfulCommands[2] == "Help" &&
                              gm.RecentSuccessfulCommands[3] == "aiforceusewaepon 2" &&
                              gm.RecentSuccessfulCommands[4] == "aiforceusewaepon 1",
                    "GM-UI-02 keeps the five most recent successful executions, including repeats");

                bool invalidSucceeded = gm.ExecuteCommand("aiforceusewaepon 16") ||
                                        gm.ExecuteCommand("UnknownGM");
                result.Assert(!invalidSucceeded && gm.RecentSuccessfulCommands.Count == 5 &&
                              gm.RecentSuccessfulCommands[0] == "hElP" &&
                              gm.RecentSuccessfulCommands[4] == "aiforceusewaepon 1",
                    "GM-UI-02 excludes failed commands without changing the history");

                bool replaySucceeded = gm.RunRecentCommand(3);
                result.Assert(replaySucceeded && MutinyAIController.ForcedWeaponId == 2 &&
                              gm.RecentSuccessfulCommands.Count == 5 &&
                              gm.RecentSuccessfulCommands[0] == "aiforceusewaepon 2" &&
                              gm.RecentSuccessfulCommands[1] == "hElP" &&
                              gm.RecentSuccessfulCommands[4] == "aiforceusewaepon 2" &&
                              !gm.RunRecentCommand(-1) && !gm.RunRecentCommand(5),
                    "GM-UI-02 recent button reruns the production command and moves that execution to the front");
                result.Assert(gm.LockOpen && gm.IsPinned, "GM-UI-03 LockOpen defaults to true (keep open enabled by default)");
                gm.LockOpen = false;
                result.Assert(!gm.LockOpen && !gm.IsPinned, "GM-UI-03 LockOpen toggle works");
                gm.LockOpen = true;

                // The forced-shot check intentionally leaves a live projectile.
                // Remove only that fixture before exercising a separate real board.
                foreach (MutinyCherryBomb bomb in Object.FindObjectsByType<MutinyCherryBomb>(FindObjectsSortMode.None))
                    if (bomb.Owner == aiCharacter) DestroyNow(bomb.gameObject);
                VerifyGMAiTakeover(gm, result);
                VerifyGMAiLevelLuck(gm, result);
                VerifyGMSetLanguage(gm, result);
            }
            finally
            {
                MutinyAIController.SetActionLogEnabled(false);
                MutinyAIController.TrySetForcedWeaponId(0);
                foreach (MutinyCherryBomb bomb in Object.FindObjectsByType<MutinyCherryBomb>(FindObjectsSortMode.None))
                    if (bomb != null && bomb.Owner != null && bomb.Owner.name == "GM_ForceAiCharacter")
                        DestroyNow(bomb.gameObject);
                DestroyNow(enemyCharacterObject);
                DestroyNow(enemyTeamObject);
                DestroyNow(aiCharacterObject);
                DestroyNow(aiTeamObject);
                DestroyNow(characterObject);
                DestroyNow(gmObject);
            }
        }

        private static void VerifyGMAiLevelLuck(MutinyGMManager gm, MutinyLevel1VerificationResult result)
        {
            var host = new GameObject("GM_LevelLuck_Controller");
            int savedForce = MutinyAIController.ForcedWeaponId;
            MutinyLevelRoot oldRoot = null;
            try
            {
                var controller = host.AddComponent<MutinyLevelController>();
                int history = gm.RecentSuccessfulCommands.Count;
                result.Assert(!gm.ExecuteCommand("aisetluck 5") && !gm.ExecuteCommand("airesetluck") &&
                              gm.RecentSuccessfulCommands.Count == history,
                    "GM-12/13 reject missing current level without recording success");
                controller.ConfigureSession(MutinyGameMode.SinglePlayer);
                controller.LevelXml = Resources.Load<TextAsset>("Data/Levels/level_05");
                controller.BuildLevel();
                MutinyLevelRoot root = controller.CurrentLevel;
                MutinyAIController ai = root.Team2.GetComponent<MutinyAIController>();
                var defaults = new Dictionary<MutinyCharacter, float>();
                foreach (MutinyCharacter character in root.Team2.Characters)
                    defaults[character] = character.Luck;
                result.Assert(new HashSet<float>(defaults.Values).Count > 1,
                    "GM-13 real level fixture contains distinct authored enemy Luck values");
                MutinyCharacter shooter = root.Team2.Characters[0];
                float playerLuck = root.Team1.Characters[0].Luck;
                result.Assert(gm.ExecuteCommand("  AiSetLuck 101.5  ") && ai.LevelLuckOverride == 101.5f &&
                              ai.GetEffectiveLuck(shooter) == 101.5f && shooter.Luck == defaults[shooter] &&
                              root.Team1.Characters[0].Luck == playerLuck,
                    "GM-12 changes effective native AI Luck above 100 without altering authored/player Luck");
                gm.ExecuteCommand("aiweapon 1");
                ai.SaveDecisionTrace = false;
                ai.EvaluateCharacterWeaponsForVerification(shooter,
                    new List<MutinyCharacter>(root.Team1.Characters), new List<MutinyCharacter>(root.Team2.Characters),
                    null, 0, 0, 1000f, out int candidates);
                result.Assert(candidates == 101,
                    "GM-12 production weapon dispatcher uses level override for actual candidate count");
                history = gm.RecentSuccessfulCommands.Count;
                string[] invalid = { "aisetluck", "aisetluck -1", "aisetluck 99999.1", "aisetluck NaN",
                    "aisetluck Infinity", "aisetluck x", "aisetluck 1,5", "aisetluck 1 extra", "airesetluck 1" };
                bool rejected = true;
                foreach (string command in invalid) rejected &= !gm.ExecuteCommand(command);
                result.Assert(rejected && ai.LevelLuckOverride == 101.5f && gm.RecentSuccessfulCommands.Count == history,
                    "GM-12/13 invalid commands retain override and success history");
                result.Assert(gm.ExecuteCommand("aisetluck 0") && ai.LevelLuckOverride == 0f,
                    "GM-12 zero is a valid override, not a reset");
                ai.EvaluateCharacterWeaponsForVerification(shooter,
                    new List<MutinyCharacter>(root.Team1.Characters), new List<MutinyCharacter>(root.Team2.Characters),
                    null, 0, 0, 1000f, out int zeroCandidates);
                result.Assert(zeroCandidates == 0, "GM-12 zero Luck reaches the production weapon sampler");
                result.Assert(gm.ExecuteCommand("aisetluck 99999") && ai.LevelLuckOverride == 99999f,
                    "GM-12 upper boundary Luck 99999 is accepted");
                controller.ConfigureSession(MutinyGameMode.LocalTwoPlayer);
                history = gm.RecentSuccessfulCommands.Count;
                result.Assert(!gm.ExecuteCommand("aisetluck 3") && !gm.ExecuteCommand("airesetluck") &&
                              ai.LevelLuckOverride == 99999f && gm.RecentSuccessfulCommands.Count == history,
                    "GM-12/13 reject two-player session even if the XML was authored for single-player");
                controller.ConfigureSession(MutinyGameMode.SinglePlayer);
                bool reset = gm.ExecuteCommand("  AiResetLuck  ") && ai.LevelLuckOverride == null;
                foreach (var entry in defaults)
                    reset &= entry.Key.Luck == entry.Value && ai.GetEffectiveLuck(entry.Key) == entry.Value;
                result.Assert(reset, "GM-13 restores every native character's level-authored default Luck");
                ai.EvaluateCharacterWeaponsForVerification(shooter,
                    new List<MutinyCharacter>(root.Team1.Characters), new List<MutinyCharacter>(root.Team2.Characters),
                    null, 0, 0, 1000f, out int defaultCandidates);
                result.Assert(defaultCandidates == Mathf.FloorToInt(defaults[shooter]),
                    "GM-13 restores production candidate sampling after reset");
                root.Team2.StartTurn();
                gm.ExecuteCommand("aisetluck 2");
                var steps = ai.EvaluateBestMoveStepsForVerification(move => { });
                bool started = steps.MoveNext();
                gm.ExecuteCommand("aisetluck 5");
                while (steps.MoveNext()) { }
                int frozenCount = ai.LastDecisionTrace.Candidates.FindAll(c => c.Weapon == "cherryBomb").Count;
                result.Assert(started && frozenCount == 2 * root.Team2.Characters.Count,
                    "GM-12 in-flight production decision keeps its starting Luck snapshot");
                ai.EvaluateBestMove();
                int nextCount = ai.LastDecisionTrace.Candidates.FindAll(c => c.Weapon == "cherryBomb").Count;
                result.Assert(nextCount == 5 * root.Team2.Characters.Count,
                    "GM-12 next production decision uses the updated level Luck");
                gm.ExecuteCommand("aisetluck 7");
                oldRoot = root;
                controller.BuildLevel();
                MutinyAIController rebuiltAi = controller.CurrentLevel.Team2.GetComponent<MutinyAIController>();
                result.Assert(rebuiltAi != ai && rebuiltAi.LevelLuckOverride == null &&
                              rebuiltAi.GetEffectiveLuck(controller.CurrentLevel.Team2.Characters[0]) == defaults[shooter],
                    "GM-12 override is scoped to the current level and does not survive production rebuild");
                DestroyNow(oldRoot.gameObject);
                oldRoot = null;
            }
            finally
            {
                if (oldRoot != null) DestroyNow(oldRoot.gameObject);
                DestroyNow(host);
                MutinyAIController.TrySetForcedWeaponId(savedForce);
            }
        }

        private static void VerifyGMSetLanguage(MutinyGMManager gm, MutinyLevel1VerificationResult result)
        {
            result.Assert(gm != null && MutinyLocalization.IsReady,
                "GM-09 localization tables must be loaded before checking visible translations");
            if (gm == null || !MutinyLocalization.IsReady)
                return;

            const string languageKey = "mutiny_language_v1";
            bool hadLanguage = PlayerPrefs.HasKey(languageKey);
            string previousSaved = MutinySaveSystem.LanguageCode;
            string previousCode = MutinyLocalization.Code;
            try
            {
                result.Assert(gm.ExecuteCommand("setlanguage zh-cn") &&
                              MutinyLocalization.Code == MutinyLocalization.SimplifiedChinese &&
                              MutinySaveSystem.LanguageCode == MutinyLocalization.SimplifiedChinese &&
                              !MutinyLocalization.UseOriginalFont &&
                              MutinyLocalization.Text("frontend.play", "play") == "开始游戏",
                    "GM-09 zh-cn changes production text, dynamic font route and saved preference");
                Rect compactButton = new Rect(193f, 187f, 163f, 24f);
                Rect wideButton = new Rect(135f, 245f, 280f, 24f);
                result.Assert(
                    RectApproximately(MutinyLocalizedText.ResolvePirateButtonTextRect(compactButton),
                        new Rect(193f, 185f, 163f, 24f)) &&
                    RectApproximately(MutinyLocalizedText.ResolvePirateButtonTextRect(wideButton),
                        new Rect(135f, 243f, 280f, 24f)) &&
                    RectApproximately(MutinyGameHUD.ResolveActionPanelTitleRect(),
                        new Rect(10.35f, -1f, 200f, 16f)) &&
                    RectApproximately(MutinyGameHUD.ResolveActionPanelDescriptionRect(),
                        new Rect(20f, 164f, 238f, 66f)),
                    "LOC-BTN-01 production Chinese button text and action panel text move up by target offsets without changing button width or height");
                Vector2 revisedTextPosition = MutinyLocalizedText.ResolvePirateButtonTextRect(compactButton).position;
                Matrix4x4 doubledCanvas = Matrix4x4.Scale(new Vector3(2f, 2f, 1f));
                result.Assert(Mathf.Approximately(revisedTextPosition.y - 184f, 1f) &&
                              Mathf.Approximately(doubledCanvas.MultiplyPoint3x4(revisedTextPosition).y - 368f, 2f),
                    "LOC-BTN-01 one original pixel adjustment scales to one screen pixel at 1x and two screen pixels at 2x");
                result.Assert(gm.ExecuteCommand("setlanguage en") &&
                              MutinyLocalization.Code == MutinyLocalization.English &&
                              MutinySaveSystem.LanguageCode == MutinyLocalization.English &&
                              MutinyLocalization.UseOriginalFont &&
                              MutinyLocalization.Text("frontend.play", "play") == "play",
                    "GM-09 en restores English text, original font route and saved preference");
                result.Assert(
                    RectApproximately(MutinyLocalizedText.ResolvePirateButtonTextRect(compactButton), compactButton) &&
                    RectApproximately(MutinyLocalizedText.ResolvePirateButtonTextRect(wideButton), wideButton) &&
                    RectApproximately(MutinyGameHUD.ResolveActionPanelTitleRect(),
                        new Rect(10.35f, 1f, 200f, 16f)) &&
                    RectApproximately(MutinyGameHUD.ResolveActionPanelDescriptionRect(),
                        new Rect(20f, 166f, 238f, 66f)),
                    "LOC-BTN-01 production English PirateFont button and action panel text positions remain unchanged");
                result.Assert(gm.ExecuteCommand("  SeTLaNgUaGe\tZH-CN  ") &&
                              MutinyLocalization.Code == MutinyLocalization.SimplifiedChinese &&
                              MutinyLocalization.Text("frontend.play", "play") == "开始游戏" &&
                              !MutinyLocalization.UseOriginalFont,
                    "GM-09 parsing accepts case changes and whitespace separators");
                result.Assert(gm.ExecuteCommand("setlanguage zh-cn") &&
                              gm.RecentSuccessfulCommands[0] == "setlanguage zh-cn" &&
                              MutinySaveSystem.LanguageCode == MutinyLocalization.SimplifiedChinese,
                    "GM-09 reselecting the current language succeeds and joins recent history");

                var history = new List<string>(gm.RecentSuccessfulCommands);
                bool rejected = !gm.ExecuteCommand("setlanguage") && !gm.ExecuteCommand("setlanguage jp") &&
                                !gm.ExecuteCommand("setlanguage en extra") && !gm.ExecuteCommand("setlanguagecn") &&
                                !gm.ExecuteCommand("setlanguage cn");
                bool unchangedHistory = history.Count == gm.RecentSuccessfulCommands.Count;
                for (int i = 0; unchangedHistory && i < history.Count; i++)
                    unchangedHistory = history[i] == gm.RecentSuccessfulCommands[i];
                result.Assert(rejected && unchangedHistory &&
                              MutinyLocalization.Code == MutinyLocalization.SimplifiedChinese &&
                              MutinySaveSystem.LanguageCode == MutinyLocalization.SimplifiedChinese,
                    "GM-09 invalid parameters preserve runtime language, saved preference and successful history");
                result.Assert(gm.ExecuteCommand("setlanguage en") && gm.RunRecentCommand(1) &&
                              MutinyLocalization.Code == MutinyLocalization.SimplifiedChinese &&
                              gm.RecentSuccessfulCommands[0] == "setlanguage zh-cn" &&
                              MutinyLocalization.Text("frontend.play", "play") == "开始游戏",
                    "GM-09 a recent command replays through the production parser and changes language again");

                bool repeatedSwitchesWork = true;
                foreach (string command in new[]
                         { "setlanguage en", "setlanguage zh-cn", "setlanguage en", "setlanguage zh-cn", "setlanguage en" })
                {
                    bool english = command == "setlanguage en";
                    repeatedSwitchesWork &= gm.ExecuteCommand(command) &&
                        MutinyLocalization.Code == (english ? MutinyLocalization.English : MutinyLocalization.SimplifiedChinese) &&
                        MutinySaveSystem.LanguageCode == MutinyLocalization.Code &&
                        MutinyLocalization.UseOriginalFont == english &&
                        MutinyLocalization.Text("frontend.play", "play") == (english ? "play" : "开始游戏") &&
                        MutinyLocalization.Text("frontend.scores", "scores") == (english ? "scores" : "成绩");
                }
                result.Assert(repeatedSwitchesWork,
                    "LOC-ANDROID-01 repeated production GM switches keep title translations, font route and saved choice usable");
            }
            finally
            {
                MutinyLocalization.Select(previousCode);
                if (hadLanguage)
                    MutinySaveSystem.LanguageCode = previousSaved;
                else
                    PlayerPrefs.DeleteKey(languageKey);
                PlayerPrefs.Save();
            }
        }

        private static void VerifyGMAiTakeover(MutinyGMManager gm, MutinyLevel1VerificationResult result)
        {
            GameObject rootObject = new GameObject("GM_Takeover_Level");
            GameObject humanObject = new GameObject("GM_Takeover_HumanTeam");
            GameObject opponentObject = new GameObject("GM_Takeover_OpponentTeam");
            GameObject ownerObject = new GameObject("GM_Takeover_Owner");
            GameObject targetObject = new GameObject("GM_Takeover_Target");
            GameObject managerObject = new GameObject("GM_Takeover_Manager");
            GameObject inputObject = new GameObject("GM_Takeover_Input");
            MutinyWeapon wave = null;
            int savedForce = MutinyAIController.ForcedWeaponId;
            try
            {
                MutinyLevelRoot root = rootObject.AddComponent<MutinyLevelRoot>();
                root.Width = 40;
                root.Height = 40;
                root.WaterLevelY = -1000f / MutinyPhysics.PixelsPerUnit;
                MutinyTeam human = humanObject.AddComponent<MutinyTeam>();
                human.TeamNumber = 1;
                MutinyTeam opponent = opponentObject.AddComponent<MutinyTeam>();
                opponent.TeamNumber = 2;
                opponent.IsAiControlled = true;
                MutinyCharacter owner = ownerObject.AddComponent<MutinyCharacter>();
                owner.TeamIndex = 1;
                owner.Luck = 3f;
                owner.PhysicsBody.State = PhysicsBodyState.CreateDefault(64f, 96f);
                owner.PhysicsBody.WaterPixelY = 1000f;
                owner.AddWeapon("banana", 2);
                human.RegisterCharacter(owner);
                MutinyCharacter target = targetObject.AddComponent<MutinyCharacter>();
                target.TeamIndex = 2;
                target.PhysicsBody.State = PhysicsBodyState.CreateDefault(10000f, 800f);
                opponent.RegisterCharacter(target);
                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                manager.Initialize(human, opponent);
                MutinyPlayerInput input = inputObject.AddComponent<MutinyPlayerInput>();
                input.TurnManager = manager;
                human.SelectCharacter(owner);
                input.SelectWeapon("banana");
                MutinyWeapon readyWeapon = input.EquippedWeapon;
                int historyCount = gm.RecentSuccessfulCommands.Count;
                MutinyAIController.TrySetForcedWeaponId(0);
                result.Assert(!gm.ExecuteCommand("aitakeover") &&
                              !gm.ExecuteCommand("aitakeoverwithluck") &&
                              !gm.ExecuteCommand("aitakeoverwithluck -1") &&
                              !gm.ExecuteCommand("aitakeoverwithluck 99999.1") &&
                              !gm.ExecuteCommand("aitakeoverwithluck NaN") &&
                              !gm.ExecuteCommand("aitakeoverwithluck Infinity") &&
                              !gm.ExecuteCommand("aitakeoverwithluck x") &&
                              !gm.ExecuteCommand("aitakeoverwithluck 1,5") &&
                              !gm.ExecuteCommand("aitakeoverwithluck 1 extra") && !human.IsAiControlled &&
                              !manager.IsAiTakeoverActive && gm.RecentSuccessfulCommands.Count == historyCount,
                    "GM-08 rejects invalid Luck without changing ownership or success history");

                int turns = human.TotalTurnsTaken;
                result.Assert(gm.ExecuteCommand("  AiTakeOverWithLuck 7.5  ") && manager.IsAiTakeoverActive &&
                              human.IsAiControlled && owner.CanThrow && owner.CanShoot &&
                              human.TotalTurnsTaken == turns && input.EquippedWeapon == null &&
                              input.ActiveWeapon == null && !input.SelectWeapon("banana") &&
                              owner.GetAmmunition("banana") == 2 && owner.Luck == 3f,
                    "GM-08 new command takes over only this turn and clears unfired input without changing character Luck/actions/inventory");
                // Destroy is deferred in Play Mode; do not let the cancelled probe
                // survive into subsequent independent checks in this synchronous suite.
                if (readyWeapon != null) DestroyNow(readyWeapon.gameObject);
                result.Assert(!gm.ExecuteCommand("aitakeoverwithluck 20") && manager.IsAiTakeoverActive,
                    "GM-08 repeated takeover cannot extend its lifetime or replace its Luck");
                MutinyAIController ai = human.GetComponent<MutinyAIController>();
                ai.UseFixedDecisionSeed = true;
                ai.FixedDecisionSeed = 731;
                ai.SaveDecisionTrace = false;
                AIMove luckMove = ai.EvaluateCharacterWeaponsForVerification(
                    owner, new List<MutinyCharacter> { target }, new List<MutinyCharacter> { owner },
                    null, 0, 0, 1000f, out int luckCandidates);
                result.Assert(ai.TakeoverLuckOverride == 7.5f && luckCandidates == 7 &&
                              luckMove.WeaponType == "banana" && owner.Luck == 3f,
                    "GM-08 fractional Luck controls production weapon sample count without mutating authored Luck");
                MutinyAIController.TrySetForcedWeaponId(15);
                ai.EvaluateBestMove();
                bool hasJump = ai.LastDecisionTrace.Candidates.Exists(c => c.MoveType == nameof(AIMoveType.SelfThrow));
                result.Assert(ai.LastDecisionTrace.Phase == "first-action" && hasJump &&
                              MutinyAIController.ResolveTurnGate(manager, human) == MutinyAITurnGate.Execute,
                    "GM-08 first-action takeover uses the normal AI candidate pool including jump");
                manager.PassTurn();
                for (int tick = 0; tick < 11; tick++) manager.AdvanceSimulationTick();
                result.Assert(manager.CurrentTeam == opponent && !human.IsAiControlled &&
                              !manager.IsAiTakeoverActive && opponent.IsAiControlled &&
                              ai.TakeoverLuckOverride == null && owner.Luck == 3f &&
                              !gm.ExecuteCommand("aitakeoverwithluck 4"),
                    "GM-08 production pass/settlement clears Luck override and rejects native AI turns");
                manager.PassTurn();
                for (int tick = 0; tick < 11; tick++) manager.AdvanceSimulationTick();
                result.Assert(manager.CurrentTeam == human && !human.IsAiControlled &&
                              MutinyAIController.ResolveTurnGate(manager, human) == MutinyAITurnGate.Cancel,
                    "GM-08 takeover does not persist when the player receives the next turn");
                MutinyAIController.TrySetForcedWeaponId(0);
                ai.EvaluateCharacterWeaponsForVerification(
                    owner, new List<MutinyCharacter> { target }, new List<MutinyCharacter> { owner },
                    null, 0, 0, 1000f, out int restoredLuckCandidates);
                result.Assert(restoredLuckCandidates == 3 && ai.TakeoverLuckOverride == null && owner.Luck == 3f,
                    "GM-08 next player turn reverts weapon sampling to the character's original Luck");

                string[,] terrain = new string[12, 12];
                for (int y = 0; y < 12; y++)
                    for (int x = 0; x < 12; x++) terrain[y, x] = y == 4 ? "ground" : "-";
                owner.PhysicsBody.SetTerrain(terrain, 12, 12);
                human.SelectCharacter(owner);
                bool jumped = input.TryCommitCharacterThrow(owner, new Vector2(64f, 96f), new Vector2(64f, 146f));
                Vector2 jumpVelocity = new Vector2(owner.PhysicsBody.State.VelocityX, owner.PhysicsBody.State.VelocityY);
                result.Assert(jumped && gm.ExecuteCommand("aitakeoverwithluck 9.25") && !owner.CanThrow && owner.CanShoot &&
                              owner.IsSelfThrown && human.SelectedCharacter == owner &&
                              jumpVelocity == new Vector2(owner.PhysicsBody.State.VelocityX, owner.PhysicsBody.State.VelocityY) &&
                              MutinyAIController.ResolveTurnGate(manager, human) == MutinyAITurnGate.Wait &&
                              ai.TakeoverLuckOverride == 9.25f && owner.Luck == 3f,
                    "GM-08 accepts a real player jump in flight with explicit Luck without resetting the jump");
                ai.EvaluateCharacterWeaponsForVerification(
                    owner, new List<MutinyCharacter> { target }, new List<MutinyCharacter> { owner },
                    null, 0, 0, 1000f, out int continuationLuckCandidates);
                result.Assert(continuationLuckCandidates == 9 && owner.Luck == 3f,
                    "GM-08 post-jump weapon sampling uses the same requested Luck");
                MutinyAIController.TrySetForcedWeaponId(15);
                for (int tick = 0; tick < 400 && manager.CurrentPhase != TurnPhase.TurnActive; tick++)
                {
                    owner.PhysicsBody.AdvanceSimulationTick();
                    manager.AdvanceSimulationTick();
                }
                AIMove shot = ai.EvaluateBestMove();
                bool onlyOwnerShots = ai.LastDecisionTrace.Candidates.TrueForAll(c =>
                    c.Character == owner.name && c.MoveType == nameof(AIMoveType.ShootWeapon));
                result.Assert(manager.CurrentPhase == TurnPhase.TurnActive && manager.IsAiTakeoverActive &&
                              !owner.CanThrow && owner.CanShoot && ai.LastDecisionTrace.Phase == "continuation" &&
                              onlyOwnerShots && shot.WeaponType == "tidalWave" && shot.Score > 0f,
                    "GM-08 real jump settlement keeps takeover and enters selected-character-only weapon continuation");
                ai.ExecuteMoveForVerification(shot, manager);
                foreach (MutinyTidalWave candidate in Object.FindObjectsByType<MutinyTidalWave>(FindObjectsSortMode.None))
                    if (candidate.Owner == owner) wave = candidate;
                result.Assert(wave != null && wave.IsFired && !owner.CanThrow && !owner.CanShoot &&
                              manager.IsAiTakeoverActive && human.IsAiControlled,
                    "GM-08 takeover persists while the normal AI weapon executes");
                if (wave != null)
                {
                    wave.PhysicsBody.SetTerrain(new string[40, 40], 40, 40);
                    for (int tick = 0; tick < 500 && !wave.IsFinished; tick++)
                    {
                        wave.PhysicsBody.AdvanceSimulationTick();
                        manager.AdvanceSimulationTick();
                    }
                }
                for (int tick = 0; tick < 11; tick++) manager.AdvanceSimulationTick();
                result.Assert(manager.CurrentTeam == opponent && !manager.IsAiTakeoverActive && !human.IsAiControlled,
                    "GM-08 full AI weapon lifecycle restores human control only when the turn finishes");
                if (wave != null) DestroyNow(wave.gameObject);
                manager.StartGame();
                bool takenAgain = gm.ExecuteCommand("aitakeoverwithluck 0");
                manager.StartGame();
                result.Assert(takenAgain && !manager.IsAiTakeoverActive && !human.IsAiControlled &&
                              ai.TakeoverLuckOverride == null && owner.Luck == 3f,
                    "GM-08 accepts zero Luck and restarting the production game clears its override");
                result.Assert(gm.ExecuteCommand("aitakeoverwithluck 10000") && ai.TakeoverLuckOverride == 10000f,
                    "GM-08 legacy alias accepts Luck above the former 100 limit");
                manager.StartGame();
                bool highLuckTaken = gm.ExecuteCommand("aitakeover 99999");
                result.Assert(highLuckTaken && ai.TakeoverLuckOverride == 99999f && owner.Luck == 3f,
                    "GM-08 aitakeover accepts upper boundary Luck 99999 without mutating character Luck");
                target.TakeDamage(1000f);
                target.ShownHealth = target.Health;
                for (int tick = 0; tick < 11; tick++) manager.AdvanceSimulationTick();
                result.Assert(manager.CurrentPhase == TurnPhase.GameOver && !human.IsAiControlled &&
                              !manager.IsAiTakeoverActive && ai.TakeoverLuckOverride == null &&
                              owner.Luck == 3f && !gm.ExecuteCommand("aitakeoverwithluck 5"),
                    "GM-08 production GameOver restores ownership/Luck and rejects further takeover");
            }
            finally
            {
                if (wave != null) DestroyNow(wave.gameObject);
                DestroyNow(inputObject);
                DestroyNow(managerObject);
                DestroyNow(targetObject);
                DestroyNow(ownerObject);
                DestroyNow(opponentObject);
                DestroyNow(humanObject);
                DestroyNow(rootObject);
                MutinyAIController.TrySetForcedWeaponId(savedForce);
            }
        }

        private static void VerifySpritePivots(MutinyLevel1VerificationResult result)
        {
            // Character pivots
            Vector2 redPiratePivot = MutinyCharacterAnimator.GetCharacterPivot("redPirate");
            result.Assert(Mathf.Approximately(redPiratePivot.x, 12f / 28f) && Mathf.Approximately(redPiratePivot.y, 15f / 30f),
                "PIVOT-01 redPirate pivot matches Flash registration point (12/28, 15/30)");

            Vector2 soldierPivot = MutinyCharacterAnimator.GetCharacterPivot("soldier");
            result.Assert(Mathf.Approximately(soldierPivot.x, 12f / 24f) && Mathf.Approximately(soldierPivot.y, 15f / 46f),
                "PIVOT-02 soldier pivot matches Flash registration point (12/24, 15/46) preventing floor sinking");

            Vector2 squidPivot = MutinyCharacterAnimator.GetCharacterPivot("squid");
            result.Assert(Mathf.Approximately(squidPivot.x, 12f / 24f) && Mathf.Approximately(squidPivot.y, 15f / 39f),
                "PIVOT-03 squid pivot matches Flash registration point (12/24, 15/39) preventing floor sinking");

            Vector2 bossGuyPivot = MutinyCharacterAnimator.GetCharacterPivot("bossGuy");
            result.Assert(Mathf.Approximately(bossGuyPivot.x, 21f / 40f) && Mathf.Approximately(bossGuyPivot.y, 15f / 62f),
                "PIVOT-04 bossGuy pivot matches Flash registration point (21/40, 15/62)");

            // Effects & Props pivots
            result.Assert(Mathf.Approximately(MutinySweepingFlame.FlamePivot.x, 9f / 19f) &&
                          Mathf.Approximately(MutinySweepingFlame.FlamePivot.y, 1f / 31f),
                "PIVOT-05 SweepingFlame pivot matches Flash bottom registration (9/19, 1/31)");

            result.Assert(Mathf.Approximately(MutinyAnchor.OriginalAnchorPivot.x, 52f / 104f) &&
                          Mathf.Approximately(MutinyAnchor.OriginalAnchorPivot.y, 2f / 100f),
                "PIVOT-06 Anchor pivot matches Flash anchor tip registration (52/104, 2/100)");

            result.Assert(Mathf.Approximately(MutinyTreasureChest.OriginalPivot.x, 29f / 62f) &&
                          Mathf.Approximately(MutinyTreasureChest.OriginalPivot.y, 27.95f / 82f),
                "PIVOT-07 TreasureChest pivot matches Flash registration point (29/62, 27.95/82)");

            // Weapon pivots
            result.Assert(Mathf.Approximately(MutinyCherryBomb.OriginalPivot.x, 10f / 20f) &&
                          Mathf.Approximately(MutinyCherryBomb.OriginalPivot.y, 10f / 32f),
                "PIVOT-08 CherryBomb pivot matches Flash registration (10/20, 10/32)");

            result.Assert(Mathf.Approximately(MutinyRumBottle.OriginalPivot.x, 9f / 18f) &&
                          Mathf.Approximately(MutinyRumBottle.OriginalPivot.y, 15f / 48f),
                "PIVOT-09 RumBottle pivot matches Flash registration (9/18, 15/48)");

            result.Assert(Mathf.Approximately(MutinyParachuteBomb.OriginalPivot.x, 18f / 36f) &&
                          Mathf.Approximately(MutinyParachuteBomb.OriginalPivot.y, 14f / 60f),
                "PIVOT-10 ParachuteBomb pivot matches Flash registration (18/36, 14/60)");

            result.Assert(Mathf.Approximately(MutinyBanana.OriginalPivot.x, 13f / 27f) &&
                          Mathf.Approximately(MutinyBanana.OriginalPivot.y, 8f / 15f),
                "PIVOT-11 Banana pivot matches Flash registration (13/27, 8/15)");

            result.Assert(Mathf.Approximately(MutinyPiecesOfEight.OriginalPivot.x, 7f / 15f) &&
                          Mathf.Approximately(MutinyPiecesOfEight.OriginalPivot.y, 8f / 15f),
                "PIVOT-12 PiecesOfEight pivot matches Flash registration (7/15, 8/15)");

            result.Assert(Mathf.Approximately(MutinyMine.OriginalPivot.x, 18f / 38f) &&
                          Mathf.Approximately(MutinyMine.OriginalPivot.y, 17f / 35f),
                "PIVOT-13 Mine pivot matches Flash registration (18/38, 17/35)");

            result.Assert(Mathf.Approximately(MutinyVoodooDoll.OriginalPivot.x, 9f / 19f) &&
                          Mathf.Approximately(MutinyVoodooDoll.OriginalPivot.y, 12f / 26f),
                "PIVOT-14 VoodooDoll pivot matches Flash registration (9/19, 12/26)");

            result.Assert(Mathf.Approximately(MutinySeagullFire.OriginalPivot.x, 7f / 13f) &&
                          Mathf.Approximately(MutinySeagullFire.OriginalPivot.y, 8f / 20f),
                "PIVOT-15 SeagullFire pivot matches Flash registration (7/13, 8/20)");

            result.Assert(Mathf.Approximately(MutinyTidalWave.OriginalTidalWavePivot.x, 310f / 499f) &&
                          Mathf.Approximately(MutinyTidalWave.OriginalTidalWavePivot.y, 2f / 352f),
                "PIVOT-16 TidalWave pivot matches Flash bottom registration (310/499, 2/352)");

            result.Assert(Mathf.Approximately(MutinySeagull.OriginalSeagullPivot.x, 13f / 26f) &&
                          Mathf.Approximately(MutinySeagull.OriginalSeagullPivot.y, 10f / 20f),
                "PIVOT-17 Seagull pivot matches Flash registration (13/26, 10/20)");

            result.Assert(Mathf.Approximately(MutinyDynamite.OriginalPivot.x, 5f / 22f) &&
                          Mathf.Approximately(MutinyDynamite.OriginalPivot.y, 12f / 27f),
                "PIVOT-18 Dynamite pivot matches Flash registration (5/22, 12/27)");

            result.Assert(Mathf.Approximately(MutinyCannon.OriginalPivot.x, 26f / 53f) &&
                          Mathf.Approximately(MutinyCannon.OriginalPivot.y, 19f / 38f),
                "PIVOT-19 Cannon pivot matches Flash registration (26/53, 19/38)");

            result.Assert(Mathf.Approximately(MutinyGunpowderBarrel.OriginalPivot.x, 16f / 33f) &&
                          Mathf.Approximately(MutinyGunpowderBarrel.OriginalPivot.y, 16f / 32f),
                "PIVOT-20 GunpowderBarrel pivot matches Flash registration (16/33, 16/32)");

            result.Assert(Mathf.Approximately(MutinyWoodenCrate.OriginalPivot.x, 30.65f / 62f) &&
                          Mathf.Approximately(MutinyWoodenCrate.OriginalPivot.y, 29.35f / 64f),
                "PIVOT-21 WoodenCrate pivot matches Flash registration (30.65/62, 29.35/64)");
        }

        private static Vector2 GetExpectedWeaponPivot(string weaponType)
        {
            switch (weaponType)
            {
                case "cherryBomb": return MutinyCherryBomb.OriginalPivot;
                case "dynamite": return MutinyDynamite.OriginalPivot;
                case "banana": return MutinyBanana.OriginalPivot;
                case "boulder": return new Vector2(0.5f, 0.5f);
                case "cannon": return MutinyCannon.OriginalPivot;
                case "gunpowderBarrel": return MutinyGunpowderBarrel.OriginalPivot;
                case "mine": return MutinyMine.OriginalPivot;
                case "parachuteBomb": return MutinyParachuteBomb.OriginalPivot;
                case "piecesOfEight": return MutinyPiecesOfEight.OriginalPivot;
                case "rumBottle": return MutinyRumBottle.OriginalPivot;
                case "voodooDoll": return MutinyVoodooDoll.OriginalPivot;
                case "anchor": return MutinyAnchor.OriginalAnchorPivot;
                case "seagull": return MutinySeagull.OriginalSeagullPivot;
                case "tidalWave": return MutinyTidalWave.OriginalTidalWavePivot;
                case "woodenCrate": return MutinyWoodenCrate.OriginalPivot;
                default: return new Vector2(0.5f, 0.5f);
            }
        }

        private static void VerifyScreenTransitions(MutinyLevel1VerificationResult result)
        {
            result.Assert(Mathf.Approximately(MutinyTransitionManager.DefaultFadeDuration, 8f / 25f),
                "TRANS-01 default fade duration matches Flash 8 frames at 25 fps (0.32s)");

            GameObject transitionObject = new GameObject("TransitionVerification");
            try
            {
                MutinyTransitionManager manager = transitionObject.AddComponent<MutinyTransitionManager>();
                result.Assert(manager.Phase == TransitionPhase.Idle && !manager.IsTransitioning,
                    "TRANS-02 transition starts in Idle phase");

                bool peakCalled = false;
                manager.TransitionTo(() => { peakCalled = true; }, showLoading: false);

                result.Assert(manager.Phase == TransitionPhase.FadeIn && manager.IsTransitioning,
                    "TRANS-03 TransitionTo enters FadeIn phase");

                manager.StepForVerification(0.16f);
                result.Assert(manager.Phase == TransitionPhase.FadeIn && Mathf.Approximately(manager.CurrentAlpha, 0.5f) && !peakCalled,
                    "TRANS-04 FadeIn advances alpha proportionally at midpoint (0.16s = alpha 0.5)");

                manager.StepForVerification(0.16f);
                result.Assert(manager.Phase == TransitionPhase.Peak && Mathf.Approximately(manager.CurrentAlpha, 1f) && peakCalled,
                    "TRANS-05 FadeIn completes at 0.32s, reaching Peak and invoking onPeak action");

                manager.StepForVerification(0.04f);
                result.Assert(manager.Phase == TransitionPhase.FadeOut,
                    "TRANS-06 Peak transitions to FadeOut after hold duration");

                manager.StepForVerification(0.32f);
                result.Assert(manager.Phase == TransitionPhase.Idle && Mathf.Approximately(manager.CurrentAlpha, 0f) && !manager.IsTransitioning,
                    "TRANS-07 FadeOut completes at 0.32s, returning to Idle with alpha 0");

                // Test with showLoading = true
                peakCalled = false;
                manager.TransitionTo(() => { peakCalled = true; }, showLoading: true);
                manager.StepForVerification(0.32f);
                result.Assert(manager.Phase == TransitionPhase.Peak && manager.IsLoadingVisible,
                    "TRANS-08 showLoading=true displays authentic loading text during Peak phase");

                manager.StepForVerification(MutinyTransitionManager.DefaultLoadingHoldDuration);
                result.Assert(manager.Phase == TransitionPhase.FadeOut && !manager.IsLoadingVisible,
                    "TRANS-09 loading text hides when exiting Peak into FadeOut");

                manager.StepForVerification(0.32f);
                result.Assert(manager.Phase == TransitionPhase.Idle && !manager.IsTransitioning,
                    "TRANS-10 transition cleanly finishes back to Idle");
            }
            finally
            {
                Object.DestroyImmediate(transitionObject);
            }
        }

        private static void VerifyCameraInitialization(MutinyLevel1VerificationResult result)
        {
            GameObject host = null;
            GameObject cameraObject = null;
            GameObject oldWeaponObject = null;
            MutinyFrontendController frontend = Object.FindAnyObjectByType<MutinyFrontendController>();
            bool frontendWasActive = frontend != null && frontend.gameObject.activeSelf;
            try
            {
                // Keep the title-page gate out of this gameplay-entry test without
                // modifying speech flags or replacing its state machine.
                if (frontendWasActive)
                    frontend.gameObject.SetActive(false);
                cameraObject = new GameObject("CameraInitialization_Camera");
                Camera unityCamera = cameraObject.AddComponent<Camera>();
                unityCamera.orthographic = true;
                unityCamera.orthographicSize = 6.25f;
                cameraObject.transform.position = new Vector3(25f, -20f, -10f);
                MutinyCameraController camera = cameraObject.AddComponent<MutinyCameraController>();
                host = new GameObject("CameraInitialization_LevelHost");
                MutinyLevelController loader = host.AddComponent<MutinyLevelController>();
                loader.ConfigureSession(MutinyGameMode.SinglePlayer);

                int[] indices = { 1, 7, 13, 30 };
                float[] centreYPixels = { 200f, 168f, 8f, 168f };
                for (int i = 0; i < indices.Length; i++)
                {
                    MutinyLevelRoot oldLevel = loader.CurrentLevel;
                    if (oldLevel != null)
                    {
                        oldWeaponObject = new GameObject("CameraInitialization_OldWeapon");
                        MutinyCherryBomb weapon = oldWeaponObject.AddComponent<MutinyCherryBomb>();
                        weapon.Initialize(oldLevel.Characters[0]);
                        camera.TrackWeapon(weapon);
                        camera.PanToTarget(oldLevel.Characters[0].transform);
                    }
                    cameraObject.transform.position = new Vector3(25f, -20f, -10f);
                    bool loaded = loader.TryLoadLevel(indices[i]);
                    MutinyLevelRoot level = loader.CurrentLevel;
                    Vector2 centre = MutinyPhysics.UnityToPixel(cameraObject.transform.position);
                    result.Assert(loaded && Mathf.Abs(centre.x - 275f) < 0.001f &&
                                  Mathf.Abs(centre.y - centreYPixels[i]) < 0.001f &&
                                  cameraObject.transform.position.z == -10f,
                        $"CAM-INIT-01/02 production load {indices[i]} resets arbitrary old position with original water clamp");
                    result.Assert(level != null && camera.TurnManager == level.GetComponent<MutinyTurnManager>() &&
                                  camera.PlayerInput == level.GetComponent<MutinyPlayerInput>() &&
                                  camera.SpeechForVerification == level.GetComponent<MutinySpeechController>() &&
                                  !camera.IsPanningToTurnTarget && camera.TrackedWeaponForVerification == null,
                        $"CAM-INIT-01 load {indices[i]} binds the new level and clears old turn/weapon targets before deferred destruction");
                    // Verify before destroying the old root: Unity Destroy really
                    // does leave it alive for the remainder of this frame.
                    DestroyNow(oldWeaponObject);
                    oldWeaponObject = null;
                    if (oldLevel != null)
                        DestroyNow(oldLevel.gameObject);
                }

                MutinyLevelRoot previous = loader.CurrentLevel;
                cameraObject.transform.position = new Vector3(20f, -15f, -10f);
                loader.RestartCurrentLevel();
                result.Assert(loader.CurrentLevel != previous &&
                              Vector2.Distance(MutinyPhysics.UnityToPixel(cameraObject.transform.position),
                                  new Vector2(275f, 168f)) < 0.001f,
                    "CAM-INIT-01 RestartCurrentLevel resets the level-30 water-limited starting camera");
                DestroyNow(previous.gameObject);

                previous = loader.CurrentLevel;
                cameraObject.transform.position = new Vector3(20f, -15f, -10f);
                loader.LoadNextLevel();
                result.Assert(loader.CurrentLevel != previous && loader.CurrentLevelIndex == 31 &&
                              Vector2.Distance(MutinyPhysics.UnityToPixel(cameraObject.transform.position),
                                  new Vector2(275f, 200f)) < 0.001f,
                    "CAM-INIT-01 LoadNextLevel does not carry the previous camera into level 31");
                DestroyNow(previous.gameObject);

                previous = loader.CurrentLevel;
                loader.ClearLevel();
                DestroyNow(previous.gameObject);
                cameraObject.transform.position = new Vector3(20f, -15f, -10f);
                bool reentered = loader.TryLoadLevel(1);
                result.Assert(reentered && Vector2.Distance(
                                  MutinyPhysics.UnityToPixel(cameraObject.transform.position),
                                  new Vector2(275f, 200f)) < 0.001f,
                    "CAM-INIT-01 explicit unload followed by re-entry restores the canonical camera origin");

                MutinyTurnManager turn = loader.CurrentLevel.GetComponent<MutinyTurnManager>();
                MutinySpeechController speech = loader.CurrentLevel.GetComponent<MutinySpeechController>();
                turn.StartGame();
                speech.AdvanceSpeechForVerification(1f / 120f);
                Vector3 start = cameraObject.transform.position;
                Vector3 target = speech.BubbleWorldPosition;
                camera.AdvanceCameraForVerification(1f / 120f);
                Vector2 displacement = cameraObject.transform.position - start;
                result.Assert(speech.HasActiveBubble && !speech.IsBubbleVisible &&
                              displacement.magnitude > 0f &&
                              displacement.magnitude <= 50f / 32f / 0.04f / 120f + 0.0001f &&
                              Vector2.Dot(displacement, (Vector2)(target - start)) > 0f &&
                              Vector2.Distance(cameraObject.transform.position, target) < Vector2.Distance(start, target),
                    $"CAM-INIT-03 production opening speech moves from reset origin at the 50px/tick render-frame limit before reveal (active={speech.HasActiveBubble}, visible={speech.IsBubbleVisible}, start={start}, target={target}, delta={displacement}, transition={MutinyTransitionManager.IsTransitionActive})");

                previous = loader.CurrentLevel;
                loader.ClearLevel();
                DestroyNow(previous.gameObject);
                TextAsset bakedXml = Resources.Load<TextAsset>("Data/Levels/level_07");
                GameObject baked = MutinyLevelBuilder.BuildLevel(
                    MutinyLevelXmlParser.Parse(bakedXml.text), host.transform, 7, MutinyGameMode.SinglePlayer);
                // Adding the controller drives its real Awake adoption branch;
                // Start must reset a baked level that did not call BuildLevel.
                MutinyLevelController bakedLoader = baked.AddComponent<MutinyLevelController>();
                cameraObject.transform.position = new Vector3(20f, -15f, -10f);
                typeof(MutinyLevelController).GetMethod("Start",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(bakedLoader, null);
                result.Assert(bakedLoader.CurrentLevel == baked.GetComponent<MutinyLevelRoot>() &&
                              camera.TurnManager == baked.GetComponent<MutinyTurnManager>() &&
                              Vector2.Distance(MutinyPhysics.UnityToPixel(cameraObject.transform.position),
                                  new Vector2(275f, 168f)) < 0.001f,
                    "CAM-INIT-01 baked-level Awake adoption and Start reset the canonical water-limited origin");
            }
            finally
            {
                DestroyNow(oldWeaponObject);
                DestroyNow(host);
                DestroyNow(cameraObject);
                if (frontendWasActive && frontend != null)
                    frontend.gameObject.SetActive(true);
            }
        }

        private static void VerifyExplosionCameraCommands(MutinyGMManager gm, MutinyLevel1VerificationResult result)
        {
            if (gm == null)
                return;
            bool savedEnabled = MutinyCameraController.ExplosionCameraEnabled;
            try
            {
                result.Assert(gm.ExecuteCommand("excamera") && !MutinyCameraController.ExplosionCameraEnabled,
                    "GM-11 bare excamera disables the extension through production parsing");
                result.Assert(gm.ExecuteCommand("  ExCamera   1  ") && MutinyCameraController.ExplosionCameraEnabled &&
                              gm.RecentSuccessfulCommands[0] == "ExCamera   1",
                    "GM-11 valid case/whitespace command enables the extension and enters successful history");
                var history = new List<string>(gm.RecentSuccessfulCommands);
                bool rejected = true;
                foreach (string command in new[] { "excamera 2", "excamera -1", "excamera true", "excamera 1 0", "excamerax 1" })
                    rejected &= !gm.ExecuteCommand(command);
                bool unchanged = history.Count == gm.RecentSuccessfulCommands.Count;
                for (int i = 0; unchanged && i < history.Count; i++)
                    unchanged &= history[i] == gm.RecentSuccessfulCommands[i];
                result.Assert(rejected && unchanged && MutinyCameraController.ExplosionCameraEnabled,
                    "GM-11 invalid parameters preserve the switch and successful history");
                result.Assert(gm.ExecuteCommand("excamera 0") && !MutinyCameraController.ExplosionCameraEnabled,
                    "GM-11 explicit zero is a valid disable alias");
                result.Assert(gm.RunRecentCommand(1) && MutinyCameraController.ExplosionCameraEnabled,
                    "GM-11 replay uses the same production parser to re-enable");
                MutinyCameraController.ResetExplosionCameraSetting();
                result.Assert(!MutinyCameraController.ExplosionCameraEnabled,
                    "GM-11 the production SubsystemRegistration initializer resets the session default to disabled");
            }
            finally
            {
                MutinyCameraController.SetExplosionCameraEnabled(savedEnabled);
            }
        }

        private static void VerifyExplosionCamera(MutinyLevel1VerificationResult result)
        {
            var objects = new List<GameObject>();
            bool savedEnabled = MutinyCameraController.ExplosionCameraEnabled;
            MutinyFrontendController frontend = Object.FindAnyObjectByType<MutinyFrontendController>();
            bool frontendWasActive = frontend != null && frontend.gameObject.activeSelf;
            var existingExplosions = new HashSet<MutinyExplosion>(Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None));
            var existingSmoke = new HashSet<MutinyRumBottleSmokeTrail>(Object.FindObjectsByType<MutinyRumBottleSmokeTrail>(FindObjectsSortMode.None));
            GameObject MakeObject(string name)
            {
                var go = new GameObject("ExplosionCamera_" + name);
                objects.Add(go);
                return go;
            }
            try
            {
                if (frontendWasActive)
                    frontend.gameObject.SetActive(false);
                MutinyGMManager gm = MutinyGMManager.Instance;
                if (gm == null)
                    gm = MakeObject("GM").AddComponent<MutinyGMManager>();
                VerifyExplosionCameraCommands(gm, result);
                gm.ExecuteCommand("excamera");

                var level = MakeObject("Level").AddComponent<MutinyLevelRoot>();
                level.Width = 100;
                level.Height = 100;
                level.WaterLevelY = -95f;
                var turn = level.gameObject.AddComponent<MutinyTurnManager>();
                var red = MakeObject("Red").AddComponent<MutinyTeam>();
                red.TeamNumber = 1;
                var blue = MakeObject("Blue").AddComponent<MutinyTeam>();
                blue.TeamNumber = 2;
                blue.IsAiControlled = true;
                MutinyCharacter MakeCharacter(string name, MutinyTeam team, float x, float y)
                {
                    var go = MakeObject(name);
                    go.transform.position = MutinyPhysics.PixelToUnity(x, y);
                    var ch = go.AddComponent<MutinyCharacter>();
                    ch.TeamIndex = team.TeamNumber;
                    ch.PhysicsBody.ApplyWaterPhysics = false;
                    ch.PhysicsBody.State.HitsTiles = false;
                    ch.PhysicsBody.State.HitsBoxes = false;
                    team.RegisterCharacter(ch);
                    return ch;
                }
                var owner = MakeCharacter("Owner", red, 500f, 1000f);
                var near = MakeCharacter("Near", blue, 1280f, 1000f);
                var far = MakeCharacter("Far", blue, 1440f, 1000f);
                turn.Initialize(red, blue);
                turn.NotifyActionStarted();
                var cameraObject = MakeObject("Camera");
                cameraObject.AddComponent<Camera>();
                var camera = cameraObject.AddComponent<MutinyCameraController>();
                camera.TurnManager = turn;
                camera.ResetForLevel(level);
                cameraObject.transform.position = MutinyPhysics.PixelToUnity(1000f, 950f) + new Vector3(0f, 0f, -10f);

                void Hit(float x, float y, float size = 200f, float damage = 20f)
                {
                    MutinyExplosion explosion = MutinyExplosion.Spawn(new Vector2(x, y), size, damage, owner, false);
                    objects.Add(explosion.gameObject);
                    explosion.ApplyHit();
                }
                void Settle(MutinyCharacter character)
                {
                    var terrain = new string[100, 100];
                    for (int x = 0; x < 100; x++)
                        terrain[60, x] = "solid";
                    var body = character.PhysicsBody;
                    body.State.HitsTiles = true;
                    body.State.Friction = 2f;
                    body.SetTerrain(terrain, 100, 100);
                    for (int tick = 0; tick < 500 && !body.IsAtRest; tick++)
                        body.AdvanceSimulationTick();
                    result.Assert(body.IsAtRest,
                        "EXT-EXCAM-03 fixture settles by real gravity, terrain collision and friction");
                }

                Hit(1360f, 1000f);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == null && near.Health < 100f &&
                              near.PhysicsBody.State.VelocityY < 0f,
                    "EXT-EXCAM-01 disabled camera leaves actual explosion damage/impulse intact");
                gm.ExecuteCommand("excamera 1");
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == null,
                    "EXT-EXCAM-02 enabling does not retroactively infer an explosion from moving characters");

                Hit(1360f, 1000f);
                float beforeX = MutinyPhysics.UnityToPixel(cameraObject.transform.position).x;
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == near &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(cameraObject.transform.position).x - beforeX - 6.25f) < 0.001f &&
                              !camera.CanAcceptManualScrollingForVerification(),
                    "EXT-EXCAM-02 nearest knocked character immediately wins and pans at 120 FPS without a physics tick");
                beforeX = MutinyPhysics.UnityToPixel(cameraObject.transform.position).x;
                camera.AdvanceCameraForVerification(1f / 60f);
                result.Assert(camera.ExplosionFocusForVerification == near &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(cameraObject.transform.position).x - beforeX - 12.5f) < 0.001f,
                    "EXT-EXCAM-02 the locked camera advances at the 60 FPS equivalent speed");

                near.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                near.PhysicsBody.AdvanceSimulationFrameForVerification(0.02f);
                near.PhysicsBody.ApplyPresentationPoseForVerification();
                cameraObject.transform.position = near.PhysicsBody.PresentationPosition +
                    new Vector3(0f, 50f / MutinyPhysics.PixelsPerUnit, -10f);
                camera.AdvanceCameraForVerification(0.02f);
                result.Assert(Vector2.Distance(cameraObject.transform.position,
                                  near.PhysicsBody.PresentationPosition + new Vector3(0f, 50f / MutinyPhysics.PixelsPerUnit)) < 0.001f,
                    "EXT-EXCAM-02 follow samples the character's actual half-tick displayed pose");

                var chest = MakeObject("Chest").AddComponent<MutinyTreasureChest>();
                chest.Initialize(null, 1800f, 800f, new List<string> { "cherryBomb" });
                var bird = MakeObject("Bird").AddComponent<MutinySeagull>();
                bird.Initialize(owner);
                bird.CallAirstrike(1800f, 1000f, 0f);
                camera.TrackWeapon(bird);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == near &&
                              Vector2.Distance(cameraObject.transform.position,
                                  near.PhysicsBody.PresentationPosition + new Vector3(0f, 50f / MutinyPhysics.PixelsPerUnit)) < 0.001f,
                    "EXT-EXCAM-04 blast focus overrides active weapon and falling chest");
                DestroyNow(chest.gameObject);
                DestroyNow(bird.gameObject);

                cameraObject.transform.position = far.PhysicsBody.PresentationPosition + new Vector3(0f, 0f, -10f);
                Hit(far.PhysicsBody.State.X, far.PhysicsBody.State.Y, 80f);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == near,
                    "EXT-EXCAM-03 another blast and a now-closer victim cannot replace the lock");
                red.RegisterCharacter(far);
                red.SelectCharacter(far);
                Settle(near);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == null && !far.PhysicsBody.IsAtRest &&
                              camera.FindActionTargetForVerification() != far.transform,
                    "EXT-EXCAM-03 stopped focus does not relay to a still-moving victim, including ordinary selected-character fallback");
                camera.PanToCharacter(far);
                Vector3 cameraBeforeReturn = cameraObject.transform.position;
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.IsPanningToTurnTarget && camera.ExplosionFocusForVerification == null &&
                              cameraObject.transform.position == cameraBeforeReturn,
                    "EXT-EXCAM-03 explicit queued return is deferred without cancelling its arrival handshake or bypassing the no-relay lock");
                Hit(far.PhysicsBody.State.X, far.PhysicsBody.State.Y, 80f);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == null,
                    "EXT-EXCAM-03 chain blast after focus stops cannot reacquire another victim");
                Settle(far);
                camera.AdvanceCameraForVerification(1f / 120f);
                Hit(far.PhysicsBody.State.X, far.PhysicsBody.State.Y, 80f);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == far,
                    "EXT-EXCAM-03 a genuinely new blast after the whole batch settles can choose a new focus");
                far.Drown();
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == null,
                    "EXT-EXCAM-03 production drowning releases the focus");

                var otherNear = MakeCharacter("BatchNear", blue, 1250f, 1000f);
                var otherFar = MakeCharacter("BatchFar", blue, 1700f, 1000f);
                cameraObject.transform.position = MutinyPhysics.PixelToUnity(1000f, 950f) + new Vector3(0f, 0f, -10f);
                Hit(1700f, 1000f, 80f);
                Hit(1250f, 1000f, 80f);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == otherNear,
                    "EXT-EXCAM-02 same-frame separate explosions aggregate before selecting the closest, not the first event");
                gm.ExecuteCommand("excamera");
                result.Assert(camera.ExplosionFocusForVerification == null && !MutinyCameraController.ExplosionCameraEnabled,
                    "GM-11 disabling immediately clears an active lock, without waiting for LateUpdate");
                gm.ExecuteCommand("excamera 1");
                Hit(otherNear.PhysicsBody.State.X, otherNear.PhysicsBody.State.Y, 80f, 200f);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(!otherNear.IsAlive && camera.ExplosionFocusForVerification == otherNear,
                    "EXT-EXCAM-03 lethal damage does not discard a still-visible flying body");
                otherNear.PhysicsBody.IsActive = false;
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == null,
                    "EXT-EXCAM-03 deactivated physics releases the focus");

                gm.ExecuteCommand("excamera");
                gm.ExecuteCommand("excamera 1");
                var edge = MakeCharacter("ZeroForce", blue, 2100f, 1000f);
                Hit(2000f, 1000f, 160f);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == null && edge.PhysicsBody.IsAtRest,
                    "EXT-EXCAM-02 exact radius boundary with zero force is not a knockback candidate");
                edge.PhysicsBody.Twang(new Vector2(2100f, 1000f), new Vector2(2100f, 1100f));
                edge.PhysicsBody.AdvanceSimulationTick();
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == null,
                    "EXT-EXCAM-02 real ordinary twang/jump does not trigger blast focus");

                Hit(otherFar.PhysicsBody.State.X, otherFar.PhysicsBody.State.Y, 80f);
                camera.AdvanceCameraForVerification(1f / 120f);
                camera.ResetForLevel(level);
                result.Assert(camera.ExplosionFocusForVerification == null && MutinyCameraController.ExplosionCameraEnabled,
                    "EXT-EXCAM-03 level reset clears the batch but preserves the session switch");

                // Exercise the actual weapon -> explosion -> camera chain too.
                var weapon = MakeObject("CherryBomb").AddComponent<MutinyCherryBomb>();
                weapon.Initialize(owner);
                weapon.PhysicsBody.State.X = otherFar.PhysicsBody.State.X;
                weapon.PhysicsBody.State.Y = otherFar.PhysicsBody.State.Y;
                weapon.Fire(Vector2.zero);
                var prior = new HashSet<MutinyExplosion>(Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None));
                weapon.Explode();
                foreach (MutinyExplosion explosion in Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None))
                    if (!prior.Contains(explosion))
                        explosion.ApplyHit();
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(weapon.IsFinished && camera.ExplosionFocusForVerification == otherFar,
                    "EXT-EXCAM-02 actual Cherry Bomb explosion production chain supplies the camera event");
                DestroyNow(otherFar.gameObject);
                camera.AdvanceCameraForVerification(1f / 120f);
                result.Assert(camera.ExplosionFocusForVerification == null,
                    "EXT-EXCAM-03 destroyed target releases without reacquiring");
            }
            finally
            {
                MutinyCameraController.SetExplosionCameraEnabled(savedEnabled);
                for (int i = objects.Count - 1; i >= 0; i--)
                    DestroyNow(objects[i]);
                foreach (MutinyExplosion explosion in Object.FindObjectsByType<MutinyExplosion>(FindObjectsSortMode.None))
                    if (explosion != null && !existingExplosions.Contains(explosion))
                        DestroyNow(explosion.gameObject);
                foreach (MutinyRumBottleSmokeTrail smoke in Object.FindObjectsByType<MutinyRumBottleSmokeTrail>(FindObjectsSortMode.None))
                    if (smoke != null && !existingSmoke.Contains(smoke))
                        DestroyNow(smoke.gameObject);
                if (frontendWasActive && frontend != null)
                    frontend.gameObject.SetActive(true);
            }
        }

        private static void VerifyCameraMovement(MutinyLevel1VerificationResult result)
        {
            GameObject levelObject = null;
            GameObject turnObject = null;
            GameObject cameraObject = null;
            GameObject teamObject = null;
            GameObject characterObject = null;
            GameObject seagullObject = null;
            GameObject waveObject = null;
            GameObject ballObject = null;
            GameObject chestObject = null;
            var existingSmoke = new HashSet<MutinyRumBottleSmokeTrail>(
                Object.FindObjectsByType<MutinyRumBottleSmokeTrail>(FindObjectsSortMode.None));
            try
            {
                levelObject = new GameObject("CameraMovement_Level");
                MutinyLevelRoot level = levelObject.AddComponent<MutinyLevelRoot>();
                level.Width = 100;
                level.Height = 100;
                level.WaterLevelY = -80f;

                turnObject = new GameObject("CameraMovement_Turn");
                MutinyTurnManager turn = turnObject.AddComponent<MutinyTurnManager>();
                turn.CurrentPhase = TurnPhase.ActionExecuting;

                cameraObject = new GameObject("CameraMovement_Camera");
                cameraObject.AddComponent<Camera>();
                MutinyCameraController camera = cameraObject.AddComponent<MutinyCameraController>();
                camera.TurnManager = turn;
                camera.SetLevelRootForVerification(level);

                teamObject = new GameObject("CameraMovement_Team");
                MutinyTeam team = teamObject.AddComponent<MutinyTeam>();
                characterObject = new GameObject("CameraMovement_JumpingCharacter");
                characterObject.transform.position = MutinyPhysics.PixelToUnity(1000f, 1000f);
                MutinyCharacter character = characterObject.AddComponent<MutinyCharacter>();
                team.RegisterCharacter(character);
                team.SelectedCharacter = character;
                turn.CurrentTeam = team;
                MutinyPhysicsBody characterBody = character.PhysicsBody;
                characterBody.ApplyWaterPhysics = false;
                characterBody.State.HitsTiles = false;
                characterBody.State.HitsBoxes = false;
                characterBody.SetVelocity(0f, -10f);
                float jumpStartY = characterBody.State.Y;
                characterBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                float jumpCurrentY = characterBody.State.Y;
                characterBody.ApplyPresentationPoseForVerification();
                result.Assert(Mathf.Abs(MutinyPhysics.UnityToPixel(characterObject.transform.position).y - jumpStartY) < 0.001f &&
                              Mathf.Abs(jumpCurrentY - jumpStartY) > 0.001f,
                    "CAM-PRES-01 production character tick preserves the 25 Hz authority and starts visible interpolation at the prior pose");

                characterBody.AdvanceSimulationFrameForVerification(0.02f);
                characterBody.ApplyPresentationPoseForVerification();
                Vector3 jumpRenderPose = characterObject.transform.position;
                cameraObject.transform.position = jumpRenderPose +
                    new Vector3(0f, 50f / MutinyPhysics.PixelsPerUnit, -10f);
                camera.AdvanceCameraForVerification(0.02f);
                bool jumpTargetMatches = camera.FindActionTargetForVerification() == character.transform;
                float jumpCameraError = Vector2.Distance(cameraObject.transform.position,
                    jumpRenderPose + new Vector3(0f, 50f / MutinyPhysics.PixelsPerUnit));
                float jumpInterpolationError = Mathf.Abs(MutinyPhysics.UnityToPixel(jumpRenderPose).y -
                    (jumpStartY + jumpCurrentY) * 0.5f);
                result.Assert(jumpTargetMatches && jumpCameraError < 0.001f &&
                              jumpInterpolationError < 0.001f &&
                              Mathf.Abs(characterBody.State.Y - jumpCurrentY) < 0.001f,
                    "CAM-PRES-01 jumping character and camera consume the same half-tick render pose without changing physics");
                if (!jumpTargetMatches || jumpCameraError >= 0.001f || jumpInterpolationError >= 0.001f)
                    Debug.LogWarning($"[CameraMovement] jump target={jumpTargetMatches} cameraError={jumpCameraError:F5} interpolationError={jumpInterpolationError:F5} startY={jumpStartY:F3} currentY={jumpCurrentY:F3} alpha={characterBody.SimulationInterpolationAlpha:F3}");

                characterBody.AdvanceSimulationFrameForVerification(1f / 60f);
                characterBody.ApplyPresentationPoseForVerification();
                camera.AdvanceCameraForVerification(1f / 60f);
                float laterJumpY = MutinyPhysics.UnityToPixel(characterObject.transform.position).y;
                result.Assert(laterJumpY > jumpCurrentY &&
                              laterJumpY < (jumpStartY + jumpCurrentY) * 0.5f &&
                              Mathf.Abs(characterBody.State.Y - jumpCurrentY) < 0.001f &&
                              Mathf.Abs(cameraObject.transform.position.y -
                                  (characterObject.transform.position.y + 50f / MutinyPhysics.PixelsPerUnit)) < 0.001f,
                    "CAM-PRES-02 production jumping character and camera both advance on a 60 FPS render frame without a physics tick");

                seagullObject = new GameObject("CameraMovement_Seagull");
                MutinySeagull seagull = seagullObject.AddComponent<MutinySeagull>();
                seagull.Initialize(null);
                seagull.PhysicsBody.SetTerrain(new string[100, 100], 100, 100);
                seagull.CallAirstrike(1000f, 1000f, 0f);
                seagull.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                seagull.PhysicsBody.AdvanceSimulationFrameForVerification(0.02f);
                seagull.PhysicsBody.ApplyPresentationPoseForVerification();
                camera.TrackWeapon(seagull);
                Vector3 birdPose = seagull.transform.position;
                cameraObject.transform.position = birdPose +
                    new Vector3(0f, -50f / MutinyPhysics.PixelsPerUnit, -10f);
                camera.AdvanceCameraForVerification(0.02f);
                result.Assert(camera.FindActionTargetForVerification() == seagull.transform &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(birdPose).x - 1005f) < 0.001f &&
                              Mathf.Abs(seagull.PhysicsBody.State.X - 1010f) < 0.001f &&
                              Vector2.Distance(cameraObject.transform.position,
                                  birdPose + new Vector3(0f, -50f / MutinyPhysics.PixelsPerUnit)) < 0.001f,
                    "CAM-PRES-02/CAM-TRACK-SEA-01 Seagull visible pose and camera track at half-tick with original trackY offset");

                seagull.PhysicsBody.AdvanceSimulationFrameForVerification(1f / 120f);
                seagull.PhysicsBody.ApplyPresentationPoseForVerification();
                camera.AdvanceCameraForVerification(1f / 120f);
                float laterBirdX = MutinyPhysics.UnityToPixel(seagull.transform.position).x;
                result.Assert(laterBirdX > 1005f && laterBirdX < 1010f &&
                              Mathf.Abs(seagull.PhysicsBody.State.X - 1010f) < 0.001f &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(cameraObject.transform.position).x - laterBirdX) < 0.001f,
                    "CAM-PRES-02 production Seagull and camera both advance on a 120 FPS render frame without a physics tick");
                seagull.PhysicsBody.RestoreAuthoritativePoseForVerification();
                result.Assert(Mathf.Abs(MutinyPhysics.UnityToPixel(seagull.transform.position).x -
                                        seagull.PhysicsBody.State.X) < 0.001f,
                    "CAM-PRES-01 visible interpolation is restored to authoritative Transform before the next simulation frame");

                waveObject = new GameObject("CameraMovement_TidalWave");
                MutinyTidalWave wave = waveObject.AddComponent<MutinyTidalWave>();
                wave.Initialize(null);
                wave.PhysicsBody.SetTerrain(new string[100, 100], 100, 100);
                wave.StartWave(0f, 2560f);
                for (int tick = 0; tick < 80; tick++)
                    wave.PhysicsBody.AdvanceSimulationTick();
                float wavePreviousX = wave.PhysicsBody.State.X;
                wave.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                wave.PhysicsBody.AdvanceSimulationFrameForVerification(0.02f);
                wave.PhysicsBody.ApplyPresentationPoseForVerification();
                camera.TrackWeapon(wave);
                cameraObject.transform.position = new Vector3(
                    wavePreviousX / MutinyPhysics.PixelsPerUnit, -76.25f, -10f);
                camera.AdvanceCameraForVerification(0.02f);
                float waveRenderX = MutinyPhysics.UnityToPixel(wave.transform.position).x;
                result.Assert(camera.FindActionTargetForVerification() == wave.transform &&
                              Mathf.Abs(waveRenderX - (wavePreviousX + 10f)) < 0.001f &&
                              Mathf.Abs(wave.PhysicsBody.State.X - (wavePreviousX + 20f)) < 0.001f &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(cameraObject.transform.position).x - waveRenderX) < 0.001f,
                    "CAM-PRES-02 Tidal Wave and camera each advance 10 visible pixels in a half-tick while authority advances 20");

                ballObject = new GameObject("CameraMovement_Cannonball");
                MutinyCannonball ball = ballObject.AddComponent<MutinyCannonball>();
                ball.Initialize(null);
                ball.PhysicsBody.SetTerrain(new string[100, 100], 100, 100);
                ball.PhysicsBody.State.HitsTiles = false;
                ball.SetLaunchPosition(new Vector2(1200f, 1000f));
                ball.Fire(new Vector2(30f, 0f));
                ball.PhysicsBody.AdvanceSimulationFrameForVerification(MutinyPhysics.TimeStep);
                ball.PhysicsBody.AdvanceSimulationFrameForVerification(0.02f);
                ball.PhysicsBody.ApplyPresentationPoseForVerification();
                camera.TrackWeapon(ball);
                cameraObject.transform.position = new Vector3(1200f / MutinyPhysics.PixelsPerUnit,
                    -1000f / MutinyPhysics.PixelsPerUnit + 50f / MutinyPhysics.PixelsPerUnit, -10f);
                camera.AdvanceCameraForVerification(0.02f);
                result.Assert(camera.FindActionTargetForVerification() == ball.transform &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(ball.transform.position).x - 1215f) < 0.001f &&
                              Mathf.Abs(ball.PhysicsBody.State.X - 1230f) < 0.001f &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(cameraObject.transform.position).x - 1215f) < 0.001f,
                    "CAM-PRES-02 Cannonball and camera share the half-tick pose within the original 30 px/tick cap");

                ball.PhysicsBody.State.X = 2000f;
                result.Assert(Mathf.Abs(MutinyPhysics.UnityToPixel(
                                  ball.PhysicsBody.SamplePresentationPosition(0.5f)).x - 2000f) < 0.001f,
                    "CAM-PRES-01 an external position change does not interpolate from the old cannonball path");

                cameraObject.transform.position = new Vector3(0f,
                    -1000f / MutinyPhysics.PixelsPerUnit + 50f / MutinyPhysics.PixelsPerUnit, -10f);
                camera.AdvanceCameraForVerification(MutinyPhysics.TimeStep);
                result.Assert(Mathf.Abs(MutinyPhysics.UnityToPixel(cameraObject.transform.position).x - 30f) < 0.001f,
                    "CAM-PRES-02 distant weapon follow remains capped at the original 30 pixels per 25 Hz tick");

                chestObject = new GameObject("CameraMovement_AirDrop");
                MutinyTreasureChest chest = chestObject.AddComponent<MutinyTreasureChest>();
                chest.Initialize(null, 1400f, 500f, new List<string> { "cherryBomb" });
                chest.AdvanceOriginalTick();
                cameraObject.transform.position = new Vector3(1000f / MutinyPhysics.PixelsPerUnit, 0f, -10f);
                camera.AdvanceCameraForVerification(MutinyPhysics.TimeStep);
                result.Assert(camera.IsTrackingAirDrop && chest.IsFalling &&
                              Mathf.Abs(chest.PixelY + 297f) < 0.001f &&
                              Mathf.Abs(MutinyPhysics.UnityToPixel(cameraObject.transform.position).x - 1050f) < 0.001f,
                    "CAM-PRES-02 falling chest keeps priority over the weapon and its existing 50 px/tick camera pan");
            }
            finally
            {
                DestroyNow(chestObject);
                DestroyNow(ballObject);
                DestroyNow(waveObject);
                DestroyNow(seagullObject);
                DestroyNow(characterObject);
                DestroyNow(teamObject);
                DestroyNow(cameraObject);
                DestroyNow(turnObject);
                DestroyNow(levelObject);
                foreach (MutinyRumBottleSmokeTrail smoke in Object.FindObjectsByType<MutinyRumBottleSmokeTrail>(FindObjectsSortMode.None))
                {
                    if (smoke != null && !existingSmoke.Contains(smoke))
                        DestroyNow(smoke.gameObject);
                }
            }
        }

        private static void DestroyNow(GameObject gameObject)
        {
            if (gameObject != null)
                Object.DestroyImmediate(gameObject);
        }
    }
}
