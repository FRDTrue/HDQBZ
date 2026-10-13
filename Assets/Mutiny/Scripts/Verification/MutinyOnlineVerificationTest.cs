using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Mutiny.Levels;
using Mutiny.Network;
using Mutiny.Presentation;
using Mutiny.Simulation;
using UnityEngine;
using Mirror;

namespace Mutiny.Verification
{
    /// <summary>
    /// Offline checks for the LAN online mode: frontend routes, versus-mode
    /// accounting, command serialization and seeded-match determinism. Lives in
    /// the runtime assembly so it can drive MutinyPlayerInput's internal
    /// verification routes; the Editor menu only forwards to these entry points.
    /// Live transport coverage lives in the Play Mode loopback script.
    /// </summary>
    public static class MutinyOnlineVerificationTest
    {
        private const int TwoPlayerLevel = 16;
        private const int HalfTurns = 4;
        private const int TicksPerHalfTurn = 400;

        public static void ValidateBasics()
        {
            int failures = 0;

            var flow = new MutinyFrontendFlow();
            Require(ref failures, flow.CurrentPage == MutinyFrontendPage.Title, "starts on title");
            flow.PressLanMultiplayer();
            Require(ref failures, flow.CurrentPage == MutinyFrontendPage.Title,
                "LAN entry is gated until GameSelect");
            flow.PressPlay();
            Require(ref failures, flow.CurrentPage == MutinyFrontendPage.GameSelect,
                "title Play reaches GameSelect");
            Require(ref failures, !flow.EnterOnlineLevelSelect() && !flow.EnterOnlineGameplay(),
                "online routes are gated outside the LAN lobby");
            flow.PressLanMultiplayer();
            Require(ref failures, flow.CurrentPage == MutinyFrontendPage.LanLobby,
                "GameSelect opens the LAN lobby");
            flow.PressLanLobbyBack();
            Require(ref failures, flow.CurrentPage == MutinyFrontendPage.GameSelect,
                "LAN lobby back returns to GameSelect");
            flow.PressLanMultiplayer();
            Require(ref failures, flow.EnterOnlineLevelSelect(),
                "host lobby routes into the versus level picker");
            // The host's own MatchStart lands while the versus picker is open,
            // so that page must be able to reach gameplay (the host otherwise
            // keeps drawing the menu over the live match).
            Require(ref failures,
                flow.EnterOnlineGameplay() && flow.CurrentPage == MutinyFrontendPage.Gameplay,
                "host MatchStart moves the online versus picker into gameplay");

            // Client route: the lobby is left only by the host's MatchStart.
            var clientFlow = new MutinyFrontendFlow();
            clientFlow.PressPlay();
            clientFlow.PressLanMultiplayer();
            Require(ref failures,
                clientFlow.EnterOnlineGameplay() && clientFlow.CurrentPage == MutinyFrontendPage.Gameplay,
                "client MatchStart moves the LAN lobby into gameplay");
            Require(ref failures, !clientFlow.EnterOnlineLevelSelect(),
                "online gameplay does not re-enter the versus level picker");

            // The local versus picker is not an online route.
            var localFlow = new MutinyFrontendFlow();
            localFlow.PressPlay();
            localFlow.PressTwoPlayer();
            Require(ref failures, localFlow.CurrentPage == MutinyFrontendPage.TwoPlayerLevelSelect &&
                                 !localFlow.EnterOnlineGameplay(),
                "the local versus picker cannot enter online gameplay");

            // End of match, host side: the finished match re-opens the versus
            // picker and the next MatchStart must leave it for gameplay again.
            var hostLoop = new MutinyFrontendFlow();
            hostLoop.PressPlay();
            hostLoop.PressLanMultiplayer();
            hostLoop.EnterOnlineLevelSelect();
            hostLoop.EnterOnlineGameplay();
            Require(ref failures, hostLoop.ReturnToOnlineLevelSelect() &&
                                 hostLoop.CurrentPage == MutinyFrontendPage.TwoPlayerLevelSelect,
                "online match end returns the host to the versus picker");
            Require(ref failures, !hostLoop.ReturnToOnlineLevelSelect(),
                "the online picker return is gated outside gameplay");
            Require(ref failures,
                hostLoop.EnterOnlineGameplay() && hostLoop.CurrentPage == MutinyFrontendPage.Gameplay,
                "the second MatchStart leaves the re-opened online picker for gameplay");

            // End of match, client side: the finished match waits in the lobby
            // and follows the host's next MatchStart.
            var clientLoop = new MutinyFrontendFlow();
            clientLoop.PressPlay();
            clientLoop.PressLanMultiplayer();
            clientLoop.EnterOnlineGameplay();
            Require(ref failures, clientLoop.ReturnToLanLobby() &&
                                 clientLoop.CurrentPage == MutinyFrontendPage.LanLobby,
                "online match end returns the client to the LAN lobby");
            Require(ref failures, !clientLoop.ReturnToLanLobby(),
                "the lobby return is gated outside gameplay");
            Require(ref failures,
                clientLoop.EnterOnlineGameplay() && clientLoop.CurrentPage == MutinyFrontendPage.Gameplay,
                "the next MatchStart leaves the waiting lobby for gameplay");

            flow = new MutinyFrontendFlow();
            flow.PressPlay();
            flow.PressLanMultiplayer();
            flow.EnterOnlineLevelSelect();
            flow.PressLanLobbyBack();
            Require(ref failures, flow.CurrentPage == MutinyFrontendPage.TwoPlayerLevelSelect,
                "level picker does not consume the lobby back route");
            flow.PressTwoPlayerLevelSelectBack();
            Require(ref failures, flow.CurrentPage == MutinyFrontendPage.GameSelect &&
                                 !flow.EnterOnlineGameplay(),
                "online level picker back returns to GameSelect and drops the online route");

            Require(ref failures, MutinyLevelController.IsVersusGameMode(MutinyGameMode.LocalTwoPlayer) &&
                                 MutinyLevelController.IsVersusGameMode(MutinyGameMode.OnlineTwoPlayer) &&
                                 !MutinyLevelController.IsVersusGameMode(MutinyGameMode.SinglePlayer),
                "versus-mode helper covers local and online two-player only");

            Require(ref failures,
                MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Team1Wins, 3, MutinyGameMode.OnlineTwoPlayer) ==
                    MutinyGameEndPopupKind.VersusPlayer1Wins &&
                MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Team2Wins, 3, MutinyGameMode.OnlineTwoPlayer) ==
                    MutinyGameEndPopupKind.VersusPlayer2Wins &&
                MutinyGameHUD.ResolveGameEndPopupKind(GameOverResult.Draw, 3, MutinyGameMode.OnlineTwoPlayer) ==
                    MutinyGameEndPopupKind.VersusDraw,
                "online results resolve to the versus popup family");

            GameObject controllerObject = new GameObject("OnlineScoresController");
            try
            {
                MutinyLevelController controller = controllerObject.AddComponent<MutinyLevelController>();
                controller.ConfigureSession(MutinyGameMode.OnlineTwoPlayer, resetVersusWins: true);
                controller.RecordGameResult(GameOverResult.Team1Wins);
                controller.RecordGameResult(GameOverResult.Team1Wins);
                Require(ref failures, controller.Player1Wins == 1 && controller.Player2Wins == 0,
                    "a level records exactly one online result per completion");
                UnityEngine.Object.DestroyImmediate(controllerObject);

                controllerObject = new GameObject("OnlineScoresController2");
                controller = controllerObject.AddComponent<MutinyLevelController>();
                controller.ConfigureSession(MutinyGameMode.OnlineTwoPlayer, resetVersusWins: true);
                controller.RecordGameResult(GameOverResult.Team2Wins);
                Require(ref failures, controller.Player1Wins == 0 && controller.Player2Wins == 1,
                    "online match results advance the versus counters");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(controllerObject);
            }

            var command = new MutinyNetCommand
            {
                Type = MutinyNetCommandType.CannonCommit,
                TeamNumber = 2,
                CharacterIndex = 3,
                WeaponType = "cannon",
                OriginX = 12.5f,
                OriginY = -40.25f,
                ReleaseX = 31.75f,
                ReleaseY = 8.125f,
                TargetIndex = 7
            };
            MutinyNetCommand roundTripped = RoundTrip(command);
            Require(ref failures,
                roundTripped.Type == command.Type &&
                roundTripped.TeamNumber == command.TeamNumber &&
                roundTripped.CharacterIndex == command.CharacterIndex &&
                roundTripped.WeaponType == command.WeaponType &&
                Mathf.Approximately(roundTripped.OriginX, command.OriginX) &&
                Mathf.Approximately(roundTripped.OriginY, command.OriginY) &&
                Mathf.Approximately(roundTripped.ReleaseX, command.ReleaseX) &&
                Mathf.Approximately(roundTripped.ReleaseY, command.ReleaseY) &&
                roundTripped.TargetIndex == command.TargetIndex,
                "net command survives a Mirror writer/reader round trip bit-exactly");

            var matchStart = new MatchStartMessage { LevelIndex = 21, MatchSeed = -123456789, QuickBattle = true };
            var writer = new NetworkWriter();
            writer.Write(matchStart);
            var reader = new NetworkReader(writer.ToArraySegment());
            MatchStartMessage startBack = reader.Read<MatchStartMessage>();
            Require(ref failures,
                startBack.LevelIndex == 21 && startBack.MatchSeed == -123456789 && startBack.QuickBattle,
                "match start message carries the quick-battle rule through a Mirror round trip");

            var matchStartForced = new MatchStartMessage
            {
                LevelIndex = 7,
                MatchSeed = 424242,
                QuickBattle = false,
                FirstPlayer = MutinyFirstPlayerRule.Player2
            };
            writer = new NetworkWriter();
            writer.Write(matchStartForced);
            reader = new NetworkReader(writer.ToArraySegment());
            MatchStartMessage forcedBack = reader.Read<MatchStartMessage>();
            Require(ref failures,
                forcedBack.FirstPlayer == MutinyFirstPlayerRule.Player2 &&
                forcedBack.LevelIndex == 7 && forcedBack.MatchSeed == 424242 && !forcedBack.QuickBattle,
                "match start message carries the host's opening rule through a Mirror round trip");

            var matchStartCheats = new MatchStartMessage
            {
                LevelIndex = 5,
                MatchSeed = 777,
                AllowCheats = true
            };
            writer = new NetworkWriter();
            writer.Write(matchStartCheats);
            reader = new NetworkReader(writer.ToArraySegment());
            MatchStartMessage cheatsBack = reader.Read<MatchStartMessage>();
            Require(ref failures,
                cheatsBack.LevelIndex == 5 && cheatsBack.MatchSeed == 777 &&
                cheatsBack.AllowCheats && !cheatsBack.QuickBattle,
                "match start message carries the room cheats rule through a Mirror round trip");

            // Opening-side rule: only a live LAN match with a forced rule yields a
            // team number, and the lobby button cycles through the three states.
            Require(ref failures,
                MutinyNetworkManager.ResolveForcedFirstTeam(false, MutinyFirstPlayerRule.Player1) == 0 &&
                MutinyNetworkManager.ResolveForcedFirstTeam(false, MutinyFirstPlayerRule.Player2) == 0 &&
                MutinyNetworkManager.ResolveForcedFirstTeam(true, MutinyFirstPlayerRule.Random) == 0 &&
                MutinyNetworkManager.ResolveForcedFirstTeam(true, MutinyFirstPlayerRule.Player1) == 1 &&
                MutinyNetworkManager.ResolveForcedFirstTeam(true, MutinyFirstPlayerRule.Player2) == 2,
                "only a live LAN match with a forced rule yields an opening team number");
            Require(ref failures,
                MutinyFrontendController.NextFirstPlayerRule(MutinyFirstPlayerRule.Random) ==
                    MutinyFirstPlayerRule.Player1 &&
                MutinyFrontendController.NextFirstPlayerRule(MutinyFirstPlayerRule.Player1) ==
                    MutinyFirstPlayerRule.Player2 &&
                MutinyFrontendController.NextFirstPlayerRule(MutinyFirstPlayerRule.Player2) ==
                    MutinyFirstPlayerRule.Random,
                "the lobby opening-rule control cycles random -> 1P -> 2P -> random");

            GameObject openingRuleProbe = new GameObject("OnlineOpeningRuleProbe");
            try
            {
                var probeTeam1Object = new GameObject("OpeningRuleTeam1");
                var probeTeam2Object = new GameObject("OpeningRuleTeam2");
                probeTeam1Object.transform.SetParent(openingRuleProbe.transform, false);
                probeTeam2Object.transform.SetParent(openingRuleProbe.transform, false);
                MutinyTeam probeTeam1 = probeTeam1Object.AddComponent<MutinyTeam>();
                MutinyTeam probeTeam2 = probeTeam2Object.AddComponent<MutinyTeam>();
                probeTeam1.TeamNumber = 1;
                probeTeam2.TeamNumber = 2;
                probeTeam2.IsAiControlled = false;
                Require(ref failures,
                    MutinyTurnManager.ResolveOpeningTeam(probeTeam1, probeTeam2, 1, 0f) == probeTeam1 &&
                    MutinyTurnManager.ResolveOpeningTeam(probeTeam1, probeTeam2, 2, 0f) == probeTeam2 &&
                    MutinyTurnManager.ResolveOpeningTeam(probeTeam1, probeTeam2, 0, 0.6f) == probeTeam2 &&
                    MutinyTurnManager.ResolveOpeningTeam(probeTeam1, probeTeam2, 0, 0.4f) == probeTeam1,
                    "a forced opening side wins outright while random keeps the seeded coin flip");

                // Single player: team 2 is the AI, so the red team always opens.
                probeTeam2.IsAiControlled = true;
                Require(ref failures,
                    MutinyTurnManager.ResolveOpeningTeam(probeTeam1, probeTeam2, 0, 0.9f) == probeTeam1,
                    "single-player still opens with the red team regardless of the roll");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(openingRuleProbe);
            }

            // Room cheats rule: a real-player LAN match hides the GM menu unless
            // the host opted in; offline play always keeps the menu available.
            Require(ref failures,
                MutinyNetworkManager.ResolveCheatsAllowed(false, false) &&
                MutinyNetworkManager.ResolveCheatsAllowed(false, true) &&
                !MutinyNetworkManager.ResolveCheatsAllowed(true, false) &&
                MutinyNetworkManager.ResolveCheatsAllowed(true, true),
                "the GM menu stays available offline and in LAN matches only with the host's cheats rule");

            // Per-player menus: a LAN match runs one local team per machine, so the
            // peer never owns (and must never see or use) the acting team's menu.
            Require(ref failures,
                MutinyNetworkManager.ResolveIsLocalTeam(false, true, 1) &&
                MutinyNetworkManager.ResolveIsLocalTeam(false, true, 2) &&
                MutinyNetworkManager.ResolveIsLocalTeam(false, false, 1) &&
                MutinyNetworkManager.ResolveIsLocalTeam(false, false, 2),
                "outside a LAN match every team on this machine is local");
            Require(ref failures,
                MutinyNetworkManager.ResolveIsLocalTeam(true, true, 1) &&
                !MutinyNetworkManager.ResolveIsLocalTeam(true, true, 2) &&
                !MutinyNetworkManager.ResolveIsLocalTeam(true, false, 1) &&
                MutinyNetworkManager.ResolveIsLocalTeam(true, false, 2),
                "the LAN host owns team 1 and the client owns team 2 only");

            // Wire integrity: a command may only address the team its sender
            // owns, so a modified peer cannot drive the opponent's characters.
            Require(ref failures,
                MutinyNetworkManager.IsRemoteCommandTeamAllowed(true, MutinyNetworkManager.ClientTeamNumber) &&
                !MutinyNetworkManager.IsRemoteCommandTeamAllowed(true, MutinyNetworkManager.HostTeamNumber) &&
                MutinyNetworkManager.IsRemoteCommandTeamAllowed(false, MutinyNetworkManager.HostTeamNumber) &&
                !MutinyNetworkManager.IsRemoteCommandTeamAllowed(false, MutinyNetworkManager.ClientTeamNumber),
                "the server accepts only the client's team and a client only the host's team");

            Require(ref failures,
                Mathf.Approximately(MutinyTreasureChest.ResolveFallPixelsPerTick(false), 3f) &&
                Mathf.Approximately(MutinyTreasureChest.ResolveFallPixelsPerTick(true), 9f),
                "quick battle drops chests three times as fast (150 -> 450 px/s)");
            Require(ref failures,
                MutinyCameraController.ShouldTrackAirDrop(false) &&
                !MutinyCameraController.ShouldTrackAirDrop(true),
                "quick battle keeps the camera on the turn view instead of the falling chest");

            var controlRequest = new MatchControlRequestMessage
            {
                Action = MutinyMatchControlAction.BackToLevelSelect
            };
            writer = new NetworkWriter();
            writer.Write(controlRequest);
            reader = new NetworkReader(writer.ToArraySegment());
            Require(ref failures, reader.Read<MatchControlRequestMessage>().Action ==
                                 MutinyMatchControlAction.BackToLevelSelect,
                "match control request survives a Mirror writer/reader round trip");

            writer = new NetworkWriter();
            writer.Write(new LobbyReturnMessage());
            Require(ref failures, writer.Position == 0,
                "lobby return message serializes to an empty payload");
            reader = new NetworkReader(writer.ToArraySegment());
            reader.Read<LobbyReturnMessage>();

            // LAN room discovery bookkeeping. RecordRoom/Prune are the exact
            // routes the UDP reply handler and the lobby list use; live socket
            // coverage runs in the Play Mode loopback pass.
            GameObject discoveryHost = new GameObject("LanDiscoveryHostProbe");
            GameObject discoveryClient = new GameObject("LanDiscoveryClientProbe");
            try
            {
                MutinyLanDiscovery hostSide = discoveryHost.AddComponent<MutinyLanDiscovery>();
                MutinyLanDiscovery clientSide = discoveryClient.AddComponent<MutinyLanDiscovery>();
                // Awake does not run in Edit Mode, so the probes take the same
                // initialization route the runtime component uses.
                hostSide.EnsureHandshake();
                clientSide.EnsureHandshake();
                Require(ref failures,
                    hostSide.secretHandshake != 0 && hostSide.secretHandshake == clientSide.secretHandshake,
                    "LAN discovery instances share the fixed non-zero game handshake");
                Require(ref failures, clientSide.GetRooms().Count == 0,
                    "a fresh discovery client lists no rooms");

                clientSide.RecordRoom(1L, "192.168.1.5", 7777);
                clientSide.RecordRoom(1L, "192.168.1.9", 7777);
                clientSide.RecordRoom(2L, "192.168.1.2", 7777);
                List<MutinyLanDiscovery.Room> rooms = clientSide.GetRooms();
                Require(ref failures, rooms.Count == 2,
                    "one row per host despite multi-adapter replies");
                Require(ref failures,
                    rooms[0].Address == "192.168.1.2" && rooms[1].Address == "192.168.1.5" &&
                    rooms[0].Port == 7777 && rooms[1].Port == 7777,
                    "rooms keep the first-seen address, the reply port, and sort by address");

                // Multi-NIC upgrade rule: a later reply from a gateway-owned
                // subnet replaces a first-seen address that peers cannot reach
                // (virtual adapters such as VMware/Hyper-V reply first).
                if (TryGetGatewaySubnet(out IPAddress gatewayIp))
                {
                    byte[] octets = gatewayIp.GetAddressBytes();
                    octets[3] = (byte)((octets[3] + 1) & 0xFF);
                    string reachablePeer = new IPAddress(octets).ToString();
                    clientSide.RecordRoom(3L, "9.9.9.9", 7777);
                    clientSide.RecordRoom(3L, reachablePeer, 7777);
                    List<MutinyLanDiscovery.Room> upgraded = clientSide.GetRooms();
                    MutinyLanDiscovery.Room upgradedRoom = upgraded.Find(
                        r => r.Address == reachablePeer || r.Address == "9.9.9.9");
                    Require(ref failures,
                        upgradedRoom.Address == reachablePeer && MutinyLanDiscovery.OnGatewaySubnet(reachablePeer),
                        "a gateway-subnet reply upgrades a first-seen unreachable address");
                }

                clientSide.Prune(Time.realtimeSinceStartup + 10f);
                Require(ref failures, clientSide.GetRooms().Count == 0,
                    "rooms that stopped replying are evicted from the list");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(discoveryHost);
                UnityEngine.Object.DestroyImmediate(discoveryClient);
            }

            if (failures == 0)
                Debug.Log("[MutinyOnline] Online basics validation passed (flow routes, versus accounting, serialization).");
            else
                Debug.LogError($"[MutinyOnline] Online basics validation failed: {failures} assert(s).");
        }

        public static void ValidateDeterminism()
        {
            int failures = 0;
            string localHash = RunScriptedMatch(20260930, driveLocal: true);
            string replayHash = RunScriptedMatch(20260930, driveLocal: false);
            Require(ref failures, localHash == replayHash,
                $"same seed produces identical state via local drive and remote replay ({localHash} vs {replayHash})");

            string divergent = FindDivergentSeed(20260930, localHash);
            Require(ref failures, divergent != null,
                "a different match seed eventually changes the outcome (RNG source is live)");

            Require(ref failures, ForcedOpeningConsumesNoMatchRandom(),
                "a forced opening side consumes no match RNG while the random rule still draws");

            if (failures == 0)
                Debug.Log($"[MutinyOnline] Match determinism validation passed. seed 20260930 hash {localHash}; divergent seed {divergent}.");
            else
                Debug.LogError($"[MutinyOnline] Match determinism validation failed: {failures} assert(s).");
        }

        /// <summary>
        /// A forced opening side must not draw from MutinyMatchRandom: if a single
        /// peer advanced the shared sequence, the seed-driven chest drops and flame
        /// scatter would diverge. The random rule must keep drawing as before.
        /// </summary>
        private static bool ForcedOpeningConsumesNoMatchRandom()
        {
            const int seed = 20261007;
            GameObject host = new GameObject("OnlineForcedOpeningProbe");
            try
            {
                var managerObject = new GameObject("ForcedOpeningTurnManager");
                managerObject.transform.SetParent(host.transform, false);
                var team1Object = new GameObject("ForcedOpeningTeam1");
                team1Object.transform.SetParent(host.transform, false);
                var team2Object = new GameObject("ForcedOpeningTeam2");
                team2Object.transform.SetParent(host.transform, false);
                var character1Object = new GameObject("ForcedOpeningCharacter1");
                character1Object.transform.SetParent(team1Object.transform, false);
                var character2Object = new GameObject("ForcedOpeningCharacter2");
                character2Object.transform.SetParent(team2Object.transform, false);

                MutinyTurnManager manager = managerObject.AddComponent<MutinyTurnManager>();
                MutinyTeam team1 = team1Object.AddComponent<MutinyTeam>();
                MutinyTeam team2 = team2Object.AddComponent<MutinyTeam>();
                MutinyCharacter character1 = character1Object.AddComponent<MutinyCharacter>();
                MutinyCharacter character2 = character2Object.AddComponent<MutinyCharacter>();
                team1.TeamNumber = 1;
                team2.TeamNumber = 2;
                team2.IsAiControlled = false;
                team1.RegisterCharacter(character1);
                team2.RegisterCharacter(character2);
                manager.Team1 = team1;
                manager.Team2 = team2;

                MutinyMatchRandom.SetSeed(seed);
                float reference = MutinyMatchRandom.NextValue();

                MutinyMatchRandom.SetSeed(seed);
                manager.StartGameWithForcedFirstForVerification(1);
                bool pickedTeam1 = manager.CurrentTeam == team1;
                float afterForcedOne = MutinyMatchRandom.NextValue();

                MutinyMatchRandom.SetSeed(seed);
                manager.StartGameWithForcedFirstForVerification(2);
                bool pickedTeam2 = manager.CurrentTeam == team2;
                float afterForcedTwo = MutinyMatchRandom.NextValue();

                MutinyMatchRandom.SetSeed(seed);
                manager.StartGameWithForcedFirstForVerification(0);
                float afterRandom = MutinyMatchRandom.NextValue();

                return pickedTeam1 && pickedTeam2 &&
                       Mathf.Approximately(afterForcedOne, reference) &&
                       Mathf.Approximately(afterForcedTwo, reference) &&
                       !Mathf.Approximately(afterRandom, reference);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static string FindDivergentSeed(int baseSeed, string referenceHash)
        {
            for (int offset = 1; offset <= 6; offset++)
            {
                string hash = RunScriptedMatch(baseSeed + offset, driveLocal: true);
                if (hash != referenceHash)
                    return (baseSeed + offset).ToString();
            }
            return null;
        }

        private static string RunScriptedMatch(int seed, bool driveLocal)
        {
            // Each run owns its level exclusively: TurnManager and the chest
            // manager resolve siblings with scene-wide searches, so stale fixture
            // levels would contaminate rest checks and drops.
            GameObject host = new GameObject($"OnlineDeterminism_{seed}_{(driveLocal ? "local" : "replay")}");
            try
            {
                MutinyMatchRandom.SetSeed(seed);
                MutinyLevelController controller = host.AddComponent<MutinyLevelController>();
                controller.ConfigureSession(MutinyGameMode.OnlineTwoPlayer);
                if (!controller.TryLoadLevel(TwoPlayerLevel))
                    throw new InvalidOperationException($"determinism fixture could not load level {TwoPlayerLevel}");

                MutinyLevelRoot root = controller.CurrentLevel;
                MutinyTurnManager turn = root != null ? root.GetComponent<MutinyTurnManager>() : null;
                MutinyPlayerInput input = root != null ? root.GetComponent<MutinyPlayerInput>() : null;
                if (turn == null || input == null)
                    throw new InvalidOperationException("determinism fixture is missing the turn manager or player input");

                // Start() never runs in Edit Mode, so the seeded opening coin flip is
                // driven here exactly like the production Start hook would.
                turn.StartGame();

                for (int halfTurn = 0; halfTurn < HalfTurns; halfTurn++)
                {
                    MutinyTeam acting = turn.CurrentTeam;
                    int actorIndex = 0;
                    if (driveLocal)
                        input.TrySelectCharacterForVerification(acting, TeamSpawnPixel(acting, actorIndex));
                    else
                        input.ExecuteRemoteCommand(SelectCharacterCommand(acting.TeamNumber, actorIndex));

                    MutinyCharacter actor = acting.SelectedCharacter;
                    if (actor == null)
                        throw new InvalidOperationException("selection command did not select a character");

                    Vector2 start = new Vector2(actor.PhysicsBody.State.X, actor.PhysicsBody.State.Y);
                    // Asymmetric drags make the opening coin flip observable: the
                    // same team order must reproduce, a flipped order must differ.
                    Vector2 drag = start + (acting.TeamNumber == 1
                        ? new Vector2(18f, 12f)
                        : new Vector2(-18f, 12f));
                    if (driveLocal)
                        input.TryCommitCharacterThrow(actor, start, drag);
                    else
                        input.ExecuteRemoteCommand(CommitThrowCommand(acting.TeamNumber, actorIndex, start, drag));

                    AdvanceTicks(turn, TicksPerHalfTurn);
                }

                string hash = HashState(turn, root);
                Debug.Log($"[MutinyOnline] run seed={seed} drive={(driveLocal ? "local" : "replay")} hash={hash}");
                return hash;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static Vector2 TeamSpawnPixel(MutinyTeam team, int index)
        {
            MutinyCharacter character = team.Characters[index];
            return new Vector2(character.PhysicsBody.State.X, character.PhysicsBody.State.Y);
        }

        private static MutinyNetCommand SelectCharacterCommand(int teamNumber, int characterIndex)
        {
            return new MutinyNetCommand
            {
                Type = MutinyNetCommandType.SelectCharacter,
                TeamNumber = teamNumber,
                CharacterIndex = characterIndex
            };
        }

        private static MutinyNetCommand CommitThrowCommand(int teamNumber, int characterIndex,
            Vector2 startPixels, Vector2 releasePixels)
        {
            return new MutinyNetCommand
            {
                Type = MutinyNetCommandType.CommitThrow,
                TeamNumber = teamNumber,
                CharacterIndex = characterIndex,
                OriginX = startPixels.x,
                OriginY = startPixels.y,
                ReleaseX = releasePixels.x,
                ReleaseY = releasePixels.y
            };
        }

        private static void AdvanceTicks(MutinyTurnManager turn, int ticks)
        {
            for (int i = 0; i < ticks; i++)
                turn.AdvanceSimulationTick();
        }        private static string HashState(MutinyTurnManager turn, MutinyLevelRoot root)
        {
            return HashTurnState(turn, root);
        }

        /// <summary>Shared by the Play Mode loopback script to compare two peers.</summary>
        public static string HashTurnState(MutinyTurnManager turn, MutinyLevelRoot root)
        {
            var hash = new StringBuilder();
            hash.Append(turn.TurnCount).Append('|').Append(turn.CurrentPhase).Append('|').Append(turn.GameResult)
                .Append('|');
            AppendTeam(hash, turn.Team1);
            AppendTeam(hash, turn.Team2);
            // Chest drops are seed-driven; include their placement so a live RNG
            // source is observable in the hash.
            MutinyTreasureChest[] chests = root != null
                ? root.GetComponentsInChildren<MutinyTreasureChest>(false)
                : Array.Empty<MutinyTreasureChest>();
            hash.Append("chests=").Append(chests.Length);
            for (int i = 0; i < chests.Length; i++)
            {
                MutinyTreasureChest chest = chests[i];
                if (chest == null)
                    continue;
                hash.Append('(').Append(chest.PixelX.ToString("F1")).Append(',')
                    .Append(chest.PixelY.ToString("F1")).Append(',')
                    .Append(chest.RemainingContents).Append(')');
            }
            return hash.ToString();
        }

        private static void AppendTeam(StringBuilder hash, MutinyTeam team)
        {
            if (team == null)
            {
                hash.Append("null;");
                return;
            }
            hash.Append(team.TeamNumber).Append(':');
            for (int i = 0; i < team.Characters.Count; i++)
            {
                MutinyCharacter character = team.Characters[i];
                if (character == null || character.PhysicsBody == null)
                {
                    hash.Append("null;");
                    continue;
                }
                PhysicsBodyState state = character.PhysicsBody.State;
                hash.Append(character.IsAlive ? "1" : "0")
                    .Append(',').Append(character.Health.ToString("F2"))
                    .Append(',').Append(state.X.ToString("F3")).Append(',').Append(state.Y.ToString("F3"))
                    .Append(',').Append(state.VelocityX.ToString("F3")).Append(',').Append(state.VelocityY.ToString("F3"))
                    .Append(';');
            }
            hash.Append('|');
        }

        private static MutinyNetCommand RoundTrip(MutinyNetCommand command)
        {
            var writer = new NetworkWriter();
            writer.Write(command);
            var reader = new NetworkReader(writer.ToArraySegment());
            return reader.Read<MutinyNetCommand>();
        }

        /// <summary>Any local IPv4 on an adapter that owns a default gateway.</summary>
        private static bool TryGetGatewaySubnet(out IPAddress gatewayIp)
        {
            gatewayIp = null;
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up)
                        continue;
                    IPInterfaceProperties properties = nic.GetIPProperties();
                    if (properties.GatewayAddresses == null || properties.GatewayAddresses.Count == 0)
                        continue;
                    foreach (UnicastIPAddressInformation info in properties.UnicastAddresses)
                    {
                        if (info.Address.AddressFamily != AddressFamily.InterNetwork ||
                            info.IPv4Mask == null)
                            continue;
                        gatewayIp = info.Address;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Offline or locked-down machines simply skip the upgrade check.
            }
            return gatewayIp != null;
        }

        private static void Require(ref int failures, bool condition, string message)
        {
            if (!condition)
            {
                failures++;
                Debug.LogError($"[MutinyOnline] FAIL: {message}");
            }
        }
    }
}
