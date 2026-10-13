using System;
using System.Collections.Generic;
using Mirror;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Network
{
    /// <summary>
    /// LAN session for Mutiny: the host plays Team 1, the client plays Team 2.
    /// The game never sends simulation state — peers run identical deterministic
    /// simulations and exchange only input commands plus the match seed, so
    /// TelepathyTransport (ordered, reliable TCP) is sufficient for LAN play.
    /// Created lazily at runtime: no scene asset depends on networking.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MutinyNetworkManager : NetworkManager
    {
        public enum SessionState
        {
            Idle,
            Hosting,
            Connecting,
            Connected,
            InMatch
        }

        public const int DefaultPort = 7777;

        private static MutinyNetworkManager s_Instance;

        private readonly Queue<MutinyNetCommand> m_QueuedCommands = new Queue<MutinyNetCommand>();
        /// <summary>
        /// True once the level of the current MatchStart has finished building,
        /// i.e. once a command can actually be replayed into it. Until then
        /// incoming commands wait in the queue: a board that is still building
        /// has no input listener yet, so a command replayed straight into it
        /// would be dropped without a trace and desync the peers.
        /// </summary>
        private bool m_HasActiveMatch;
        private MutinyLanDiscovery m_Discovery;

        public static MutinyNetworkManager Instance => s_Instance;
        public static bool Exists => s_Instance != null;

        /// <summary>UDP room discovery on the same GameObject; null only for managers created outside EnsureInstance.</summary>
        public MutinyLanDiscovery Discovery => m_Discovery;

        /// <summary>True while an online match is live on this machine.</summary>
        public static bool IsOnlineMatch
        {
            get
            {
                MutinyNetworkManager manager = s_Instance;
                return manager != null && manager.State == SessionState.InMatch;
            }
        }

        /// <summary>The host always plays team 1 and a joined peer always plays team 2.</summary>
        public const int HostTeamNumber = 1;
        public const int ClientTeamNumber = 2;

        public SessionState State { get; private set; } = SessionState.Idle;
        public bool IsOnline => State != SessionState.Idle;
        public bool IsHost { get; private set; }
        public string LastError { get; private set; }
        public int LocalTeamNumber => IsHost ? HostTeamNumber : ClientTeamNumber;

        /// <summary>
        /// Quick battle: air-dropped chests fall three times as fast and the
        /// camera keeps the ordinary turn view instead of following them. The
        /// host picks it in the LAN lobby and it travels inside MatchStartMessage,
        /// so both peers simulate the identical drop.
        /// </summary>
        public bool QuickBattle { get; private set; }

        /// <summary>
        /// The live rule. False outside a running LAN match, so single-player and
        /// local two-player drops keep their original pacing.
        /// </summary>
        public static bool QuickBattleActive => IsOnlineMatch && s_Instance.QuickBattle;

        /// <summary>Lobby toggle. Only the host decides; a joined peer keeps the host's rule.</summary>
        public bool SetQuickBattle(bool enabled)
        {
            if (IsOnline && !IsHost)
                return false;
            QuickBattle = enabled;
            return true;
        }

        /// <summary>
        /// Which side opens the match. The host picks it in the LAN lobby and it
        /// travels inside MatchStartMessage; a forced side skips the seeded coin
        /// flip entirely, so neither peer consumes match RNG on its own.
        /// </summary>
        public MutinyFirstPlayerRule FirstPlayer { get; private set; } = MutinyFirstPlayerRule.Random;

        /// <summary>
        /// The live rule as a team number: 0 means "no forced side" outside a
        /// running LAN match or when the host left it on random, which keeps
        /// single-player and local two-player openings unchanged.
        /// </summary>
        public static int ForcedFirstTeamNumber => ResolveForcedFirstTeam(
            IsOnlineMatch, s_Instance != null ? s_Instance.FirstPlayer : MutinyFirstPlayerRule.Random);

        internal static int ResolveForcedFirstTeam(bool isOnlineMatch, MutinyFirstPlayerRule rule)
        {
            if (!isOnlineMatch)
                return 0;
            if (rule == MutinyFirstPlayerRule.Player1)
                return 1;
            if (rule == MutinyFirstPlayerRule.Player2)
                return 2;
            return 0;
        }

        /// <summary>Lobby choice. Only the host decides; a joined peer keeps the host's rule.</summary>
        public bool SetFirstPlayer(MutinyFirstPlayerRule rule)
        {
            if (IsOnline && !IsHost)
                return false;
            FirstPlayer = rule;
            return true;
        }

        /// <summary>
        /// Room cheats rule: whether the GM menu may be used inside this online
        /// match. The host picks it in the LAN lobby and it travels inside
        /// MatchStartMessage, so both peers gate the menu alike. It changes
        /// nothing on its own while offline — single-player and local
        /// two-player always keep the menu available.
        /// </summary>
        public bool AllowCheats { get; private set; }

        /// <summary>
        /// The live rule. True outside a running LAN match, so the GM menu
        /// stays available in single-player and local two-player at all times.
        /// </summary>
        public static bool CheatsAllowedInMatch => ResolveCheatsAllowed(
            IsOnlineMatch, s_Instance != null && s_Instance.AllowCheats);

        internal static bool ResolveCheatsAllowed(bool isOnlineMatch, bool allowCheats)
        {
            return !isOnlineMatch || allowCheats;
        }

        /// <summary>Lobby toggle. Only the host decides; a joined peer keeps the host's rule.</summary>
        public bool SetAllowCheats(bool enabled)
        {
            if (IsOnline && !IsHost)
                return false;
            AllowCheats = enabled;
            return true;
        }

        /// <summary>
        /// A LAN match runs exactly one local team per machine, so every
        /// player-owned menu (weapon menu, quit prompt, result popup) is gated on
        /// this. Outside a match every team on this machine is local, which keeps
        /// single-player and local two-player behaviour unchanged.
        /// </summary>
        public static bool IsLocalTeam(int teamNumber)
        {
            MutinyNetworkManager manager = s_Instance;
            return ResolveIsLocalTeam(
                manager != null && manager.State == SessionState.InMatch,
                manager != null && manager.IsHost,
                teamNumber);
        }

        internal static bool ResolveIsLocalTeam(bool isOnlineMatch, bool isHost, int teamNumber)
        {
            if (!isOnlineMatch)
                return true;
            return teamNumber == (isHost ? HostTeamNumber : ClientTeamNumber);
        }

        /// <summary>
        /// Which team an incoming command may address. The server only ever
        /// hears its joined peer (team 2) and a client only ever hears the host
        /// (team 1), so a command that claims the receiving side's own team is
        /// spoofed or stale and is dropped instead of replayed.
        /// </summary>
        internal static bool IsRemoteCommandTeamAllowed(bool receivingOnServer, int commandTeamNumber)
        {
            int expectedTeam = receivingOnServer ? ClientTeamNumber : HostTeamNumber;
            return commandTeamNumber == expectedTeam;
        }

        /// <summary>Raised on every state transition (lobby UI listens).</summary>
        public event Action<SessionState> OnStateChanged;
        /// <summary>Client-side: the host started a match (levelIndex, matchSeed).</summary>
        public event Action<int, int> OnMatchStartReceived;
        /// <summary>Raised after any disconnect tears the session down.</summary>
        public event Action<string> OnSessionEnded;
        /// <summary>Raised per remote command once the local match is active.</summary>
        public event Action<MutinyNetCommand> OnCommandReceived;
        /// <summary>Host: the remote player pressed an end-of-match popup button.</summary>
        public event Action<MutinyMatchControlAction> OnMatchControlRequested;
        /// <summary>Client: the host left the finished match for the level picker.</summary>
        public event Action OnLobbyReturn;

        public static MutinyNetworkManager EnsureInstance()
        {
            if (s_Instance != null)
                return s_Instance;

            // Transport first: Transport.active is claimed during its Awake.
            GameObject host = new GameObject("MutinyNetworkManager");
            TelepathyTransport transport = host.AddComponent<TelepathyTransport>();
            transport.port = (ushort)DefaultPort;
            MutinyNetworkManager manager = host.AddComponent<MutinyNetworkManager>();
            MutinyLanDiscovery discovery = host.AddComponent<MutinyLanDiscovery>();
            discovery.transport = transport;
            manager.m_Discovery = discovery;
            DontDestroyOnLoad(host);
            return manager;
        }

        public override void Awake()
        {
            base.Awake();
            // This game has no player prefabs; identity/spawn plumbing is unused.
            autoCreatePlayer = false;
            s_Instance = this;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (s_Instance == this)
                s_Instance = null;
        }

        public bool StartLanHost()
        {
            if (IsOnline)
                return false;
            LastError = null;
            EnsureInstance();
            if (NetworkServer.active || NetworkClient.active)
            {
                // Mirror ignores StartHost while a server or client is still
                // running, so reporting success here would strand the lobby.
                LastError = "Previous session is still shutting down.";
                return false;
            }
            IsHost = true;
            StartHost();
            if (!NetworkServer.active)
            {
                IsHost = false;
                LastError = "Failed to start host (port may be in use).";
                return false;
            }
            SetState(SessionState.Hosting);
            return true;
        }

        public bool StartLanClient(string address)
        {
            if (IsOnline)
                return false;
            if (string.IsNullOrWhiteSpace(address))
            {
                LastError = "Enter a host IP address.";
                return false;
            }
            LastError = null;
            EnsureInstance();
            if (NetworkServer.active || NetworkClient.active)
            {
                // Mirror ignores StartClient while a server or client is still
                // running, which would leave the lobby stuck on "connecting".
                LastError = "Previous session is still shutting down.";
                return false;
            }
            IsHost = false;
            networkAddress = address.Trim();
            StartClient();
            SetState(SessionState.Connecting);
            return true;
        }

        /// <summary>Leave the lobby or abandon a match; both peers return to idle.</summary>
        public void LeaveSession()
        {
            // Teardown releases the transport, and an unexpected disconnect may
            // already have set the session to Idle while its socket stayed open,
            // so IsOnline alone must not decide whether there is work to do.
            if (!IsOnline && !NetworkServer.active && !NetworkClient.active)
                return;
            Teardown("left session");
        }

        /// <summary>Lobby UI: browse the LAN for rooms while idle (idempotent).</summary>
        public void StartLanRoomSearch()
        {
            if (m_Discovery == null || State != SessionState.Idle || m_Discovery.IsSearching)
                return;
            m_Discovery.StartDiscovery();
        }

        /// <summary>Lobby UI: stop browsing, e.g. when the lobby page is left.</summary>
        public void StopLanRoomSearch()
        {
            if (m_Discovery != null && m_Discovery.IsSearching)
                m_Discovery.StopDiscovery();
        }

        // Loopback verification: server + real-socket client in one process, so
        // the client connects as a remote peer and exercises the same relay
        // paths as a second machine. StartHost's LocalConnection would be
        // skipped by the remote-only fan-out, so this bypasses it.
        public bool StartLanLoopbackForVerification()
        {
            if (IsOnline)
                return false;
            LastError = null;
            EnsureInstance();
            IsHost = true;
            StartServer();
            if (!NetworkServer.active)
            {
                IsHost = false;
                LastError = "Failed to start server (port may be in use).";
                return false;
            }
            SetState(SessionState.Hosting);
            networkAddress = "127.0.0.1";
            StartClient();
            SetState(SessionState.Connecting);
            return true;
        }

        /// <summary>Host only: pick the level and seed, then start it on both peers.</summary>
        public void HostStartMatch(int levelIndex)
        {
            if (!IsHost || NetworkServer.active == false)
                return;

            int seed = Environment.TickCount;
            var message = new MatchStartMessage
            {
                LevelIndex = levelIndex,
                MatchSeed = seed,
                QuickBattle = QuickBattle,
                FirstPlayer = FirstPlayer,
                AllowCheats = AllowCheats
            };
            foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
            {
                if (conn is LocalConnectionToClient)
                    continue;
                conn.Send(message);
            }
            ApplyMatch(levelIndex, seed, QuickBattle, FirstPlayer, AllowCheats);
        }

        /// <summary>
        /// Local level finished building: from here on commands replay straight
        /// into the live board, so the ones that arrived while it was still
        /// building are flushed in arrival order.
        /// </summary>
        public void NotifyMatchActive()
        {
            m_HasActiveMatch = true;
            while (m_QueuedCommands.Count > 0)
                OnCommandReceived?.Invoke(m_QueuedCommands.Dequeue());
        }

        /// <summary>
        /// The finished match is over but the session continues: both peers go back
        /// to waiting (host: versus picker, client: lobby) until the next MatchStart.
        /// The host notifies the client; a client asks the host to follow.
        /// </summary>
        public void ReturnToLobby()
        {
            if (State != SessionState.InMatch)
                return;
            if (NetworkServer.active)
            {
                var message = new LobbyReturnMessage();
                foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
                {
                    if (conn is LocalConnectionToClient)
                        continue;
                    conn.Send(message);
                }
            }
            else if (NetworkClient.active)
            {
                NetworkClient.Send(new MatchControlRequestMessage
                {
                    Action = MutinyMatchControlAction.BackToLevelSelect
                });
            }
            EndMatchLocally();
        }

        /// <summary>Client: forward an end-of-match popup press to the host.</summary>
        public void SendMatchControlRequest(MutinyMatchControlAction action)
        {
            if (NetworkClient.active && !NetworkServer.active)
                NetworkClient.Send(new MatchControlRequestMessage { Action = action });
        }

        public void SendCommand(in MutinyNetCommand command)
        {
            if (!m_HasActiveMatch)
                return;
            var message = new MutinyNetCommandMessage { Command = command };
            if (NetworkServer.active)
            {
                foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
                {
                    if (conn is LocalConnectionToClient)
                        continue;
                    conn.Send(message);
                }
            }
            else if (NetworkClient.active)
            {
                NetworkClient.Send(message);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.RegisterHandler<MutinyNetCommandMessage>(OnServerCommandMessage);
            NetworkServer.RegisterHandler<MatchControlRequestMessage>(OnServerMatchControlRequest);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.RegisterHandler<MutinyNetCommandMessage>(OnClientCommandMessage);
            NetworkClient.RegisterHandler<MatchStartMessage>(OnMatchStartMessage);
            NetworkClient.RegisterHandler<LobbyReturnMessage>(OnClientLobbyReturn);
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            base.OnServerConnect(conn);
            // StartHost dials its own local connection; it is not a second player.
            if (conn is LocalConnectionToClient)
                return;
            // Strictly two players: refuse further connections.
            if (NetworkServer.connections.Count > 2)
            {
                conn.Disconnect();
                return;
            }
            if (State == SessionState.Hosting)
                SetState(SessionState.Connected);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            // StopHost drops its own local connection first; only a remote peer
            // leaving ends the session.
            if (conn is LocalConnectionToClient)
                return;
            if (State == SessionState.Connected || State == SessionState.InMatch)
                LeaveSessionWithReason("opponent disconnected");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            // StartHost's LocalConnectionToServer is the host talking to its own
            // server, so it is not a peer joining; a real client dials a
            // ConnectionToServer and does mean "connected".
            if (NetworkClient.connection is LocalConnectionToServer)
                return;
            SetState(SessionState.Connected);
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            if (State == SessionState.Connecting)
                LastError = "Could not reach the host.";
            if (IsOnline)
                LeaveSessionWithReason("disconnected");
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (IsOnline)
                LeaveSessionWithReason("session stopped", stopTransport: false);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (IsOnline && !NetworkServer.active)
                LeaveSessionWithReason("session stopped", stopTransport: false);
        }

        private void OnMatchStartMessage(MatchStartMessage message)
        {
            ApplyMatch(message.LevelIndex, message.MatchSeed, message.QuickBattle,
                message.FirstPlayer, message.AllowCheats);
        }

        private void OnServerMatchControlRequest(NetworkConnectionToClient sender,
            MatchControlRequestMessage message)
        {
            // Only a real remote peer drives the end-of-match flow, and only
            // while a match is live: a request outside a match is stale noise.
            if (sender == null || sender is LocalConnectionToClient)
                return;
            if (State != SessionState.InMatch)
                return;
            OnMatchControlRequested?.Invoke(message.Action);
        }

        private void OnClientLobbyReturn(LobbyReturnMessage message)
        {
            EndMatchLocally();
            OnLobbyReturn?.Invoke();
        }

        private void OnServerCommandMessage(NetworkConnectionToClient sender, MutinyNetCommandMessage message)
        {
            // The joined peer owns team 2 and nothing else: a command claiming
            // the host's own team would let a modified client drive the
            // opponent's characters on this machine.
            if (!IsRemoteCommandTeamAllowed(receivingOnServer: true, message.Command.TeamNumber))
            {
                Debug.LogWarning($"[MutinyNetwork] dropped command for team {message.Command.TeamNumber}: the peer owns team {ClientTeamNumber}.");
                return;
            }

            if (sender != null)
            {
                // Relay to other remote peers; the loop skips the origin so a
                // command executes exactly once per machine.
                foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
                {
                    if (conn == sender || conn is LocalConnectionToClient)
                        continue;
                    conn.Send(message);
                }
            }
            HandleCommand(in message.Command);
        }

        private void OnClientCommandMessage(MutinyNetCommandMessage message)
        {
            // The host owns team 1; any other team number is stale or spoofed.
            if (!IsRemoteCommandTeamAllowed(receivingOnServer: false, message.Command.TeamNumber))
            {
                Debug.LogWarning($"[MutinyNetwork] dropped command for team {message.Command.TeamNumber}: the host owns team {HostTeamNumber}.");
                return;
            }
            HandleCommand(in message.Command);
        }

        private void HandleCommand(in MutinyNetCommand command)
        {
            if (m_HasActiveMatch)
                OnCommandReceived?.Invoke(command);
            else
                m_QueuedCommands.Enqueue(command);
        }

        private void ApplyMatch(int levelIndex, int seed, bool quickBattle,
            MutinyFirstPlayerRule firstPlayer, bool allowCheats)
        {
            MutinyMatchRandom.SetSeed(seed);
            // Both rules must be in place before the level builds: the chest
            // simulation reads the quick-battle pacing from its first tick, the
            // turn manager resolves the opening side while it starts, and the GM
            // menu consults the cheats rule as soon as the HUD draws.
            QuickBattle = quickBattle;
            FirstPlayer = firstPlayer;
            AllowCheats = allowCheats;
            // OnMatchStartReceived builds the board, so nothing can be replayed
            // yet: hold incoming commands in the queue and drop whatever the
            // previous match left behind.
            m_HasActiveMatch = false;
            m_QueuedCommands.Clear();
            SetState(SessionState.InMatch);
            OnMatchStartReceived?.Invoke(levelIndex, seed);
        }

        /// <summary>The match is over but the session stays connected for the next one.</summary>
        private void EndMatchLocally()
        {
            m_HasActiveMatch = false;
            m_QueuedCommands.Clear();
            SetState(SessionState.Connected);
        }

        private void LeaveSessionWithReason(string reason, bool stopTransport = true)
        {
            OnSessionEnded?.Invoke(reason);
            Teardown(reason, stopTransport);
        }

        private void Teardown(string reason, bool stopTransport = true)
        {
            m_HasActiveMatch = false;
            m_QueuedCommands.Clear();
            IsHost = false;
            // Idle first: OnStopServer/OnStopClient re-enter the leave path and an
            // already-idle state keeps them from recursing (and keeps one
            // unexpected disconnect from reporting OnSessionEnded twice).
            SetState(SessionState.Idle);
            // An unexpected disconnect tears the session down but leaves Mirror's
            // server or client running: the LAN port would stay bound and the next
            // StartLanHost/StartLanClient would be a silent no-op.
            if (stopTransport)
            {
                if (NetworkServer.active)
                    StopHost();
                else if (NetworkClient.active)
                    StopClient();
            }
            Debug.Log($"[MutinyNetwork] session ended: {reason}");
        }

        private void SetState(SessionState state)
        {
            if (State == state)
                return;
            State = state;
            // Advertise only while the room still accepts a second player: the
            // lobby fills (Connected/InMatch) or the session ends → stop
            // advertising, and a client stops browsing once it is no longer idle.
            if (m_Discovery != null)
            {
                if (state == SessionState.Hosting)
                    m_Discovery.AdvertiseServer();
                else
                    m_Discovery.StopDiscovery();
            }
            OnStateChanged?.Invoke(state);
        }
    }
}
