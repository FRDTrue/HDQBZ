using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Mirror.Discovery;
using UnityEngine;

namespace Mutiny.Network
{
    /// <summary>
    /// LAN room discovery for the lobby, built on Mirror's UDP NetworkDiscovery.
    /// Both roles are created at runtime (the game owns no scene assets for
    /// networking), so the secret handshake is a fixed game constant instead of
    /// the editor-randomized per-asset value. The client broadcasts every three
    /// seconds and evicts rooms that stop replying, so the lobby only ever
    /// lists hosts that are still waiting for a player.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MutinyLanDiscovery : NetworkDiscovery
    {
        // "Mutiny" + room tag — fixed so builds on different machines agree;
        // other Mirror games on the same LAN are filtered out by the handshake.
        private const long kSecretHandshake = 0x4D7574696E79_0001L;
        // Discovery rebroadcasts every 3 s; two missed replies evict a room.
        private const float RoomTimeoutSeconds = 7f;

        public struct Room
        {
            public string Address;
            public int Port;
            public float LastSeen;
        }

        private readonly Dictionary<long, Room> m_Rooms = new Dictionary<long, Room>();
        private List<Room> m_SortedRooms = new List<Room>();
        private long m_RoomId;

        /// <summary>True while this machine answers discovery broadcasts.</summary>
        public bool IsAdvertising => serverUdpClient != null;
        /// <summary>True while this machine is browsing for rooms.</summary>
        public bool IsSearching => clientUdpClient != null;

        private void Awake()
        {
            // OnValidate never runs for runtime-created components, so the
            // fixed handshake is assigned here on both host and client.
            EnsureHandshake();
        }

        /// <summary>Assigns the fixed handshake and room id (Awake route; Edit Mode probes call it directly — the editor's OnValidate randomizes the handshake, so the assignment is unconditional).</summary>
        internal void EnsureHandshake()
        {
            secretHandshake = kSecretHandshake;
            if (m_RoomId == 0)
                m_RoomId = RandomLong();
        }

        /// <summary>Visible rooms, ordered by address; stale rooms are evicted first.</summary>
        public List<Room> GetRooms()
        {
            Prune(Time.realtimeSinceStartup);
            if (m_SortedRooms == null || m_SortedRooms.Count != m_Rooms.Count)
                RebuildSortedRooms();
            return m_SortedRooms;
        }

        /// <summary>Drop rooms whose last reply predates the timeout (now is injectable for verification).</summary>
        internal void Prune(float now)
        {
            List<long> stale = null;
            foreach (KeyValuePair<long, Room> pair in m_Rooms)
            {
                if (now - pair.Value.LastSeen <= RoomTimeoutSeconds)
                    continue;
                stale ??= new List<long>();
                stale.Add(pair.Key);
            }
            if (stale == null)
                return;
            foreach (long serverId in stale)
                m_Rooms.Remove(serverId);
        }

        /// <summary>Book a reply from a host (also the verification route for the offline checks).</summary>
        internal void RecordRoom(long serverId, string address, int port)
        {
            // A host replies once per network interface; the first reply may
            // come from a virtual adapter (VMware/Hyper-V) that other machines
            // cannot reach, so a later reply from a gateway-owned subnet — the
            // network peers are actually on — upgrades the row's address.
            if (m_Rooms.TryGetValue(serverId, out Room room))
            {
                bool upgraded = !OnGatewaySubnet(room.Address) && OnGatewaySubnet(address);
                if (upgraded)
                {
                    room.Address = address;
                    room.Port = port;
                }
                room.LastSeen = Time.realtimeSinceStartup;
                m_Rooms[serverId] = room;
                if (upgraded)
                    RebuildSortedRooms();
                return;
            }
            m_Rooms[serverId] = new Room
            {
                Address = address,
                Port = port,
                LastSeen = Time.realtimeSinceStartup
            };
            RebuildSortedRooms();
        }

        // Cached gateway-adapter subnets; refreshed every few seconds so that
        // plugging into a different network is picked up without a restart.
        private static readonly List<IPAddress[]> s_GatewaySubnets = new List<IPAddress[]>();
        private static float s_GatewaySubnetsTime = -10f;

        internal static bool OnGatewaySubnet(string addressString)
        {
            if (!IPAddress.TryParse(addressString, out IPAddress address) ||
                address.AddressFamily != AddressFamily.InterNetwork)
                return false;
            if (Time.realtimeSinceStartup - s_GatewaySubnetsTime > 5f)
            {
                s_GatewaySubnetsTime = Time.realtimeSinceStartup;
                CollectGatewaySubnets();
            }
            for (int i = 0; i < s_GatewaySubnets.Count; i++)
            {
                IPAddress[] subnet = s_GatewaySubnets[i];
                if (SharesSubnet(address, subnet[0], subnet[1]))
                    return true;
            }
            return false;
        }

        private static void CollectGatewaySubnets()
        {
            s_GatewaySubnets.Clear();
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
                        s_GatewaySubnets.Add(new[] { info.Address, info.IPv4Mask });
                    }
                }
            }
            catch (Exception)
            {
                // Interface enumeration can fail on locked-down machines; the
                // room list then keeps first-seen addresses.
            }
        }

        private static bool SharesSubnet(IPAddress a, IPAddress b, IPAddress mask)
        {
            byte[] x = a.GetAddressBytes();
            byte[] y = b.GetAddressBytes();
            byte[] m = mask.GetAddressBytes();
            return (x[0] & m[0]) == (y[0] & m[0]) &&
                   (x[1] & m[1]) == (y[1] & m[1]) &&
                   (x[2] & m[2]) == (y[2] & m[2]) &&
                   (x[3] & m[3]) == (y[3] & m[3]);
        }

        protected override ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
        {
            // transport.ServerUri() is just "tcp://hostname:port" — no listener
            // involved. The room id comes from this component so even the very
            // first reply (before base Start() assigns ServerId) dedupes.
            return new ServerResponse
            {
                serverId = m_RoomId,
                uri = transport != null
                    ? transport.ServerUri()
                    : new UriBuilder("tcp", "localhost", MutinyNetworkManager.DefaultPort).Uri
            };
        }

        protected override void ProcessResponse(ServerResponse response, IPEndPoint endpoint)
        {
            // The packet's source address is the route that actually reached
            // this machine, so it replaces the advertised hostname.
            if (endpoint == null || endpoint.Address.AddressFamily != AddressFamily.InterNetwork)
                return;
            UriBuilder realUri = new UriBuilder(response.uri) { Host = endpoint.Address.ToString() };
            RecordRoom(response.serverId, realUri.Host, realUri.Port);
        }

        private void RebuildSortedRooms()
        {
            m_SortedRooms ??= new List<Room>();
            m_SortedRooms.Clear();
            m_SortedRooms.AddRange(m_Rooms.Values);
            m_SortedRooms.Sort((a, b) => string.CompareOrdinal(a.Address, b.Address));
        }
    }
}
