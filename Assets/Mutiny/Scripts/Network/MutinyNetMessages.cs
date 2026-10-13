using Mirror;

namespace Mutiny.Network
{
    public enum MutinyNetCommandType : byte
    {
        SelectCharacter,
        EquipWeapon,
        CommitThrow,
        LaunchWeapon,
        VoodooTarget,
        CannonCommit,
        PassTurn,
        SeagullShot,
        BananaDetonate
    }

    /// <summary>
    /// One deterministic simulation input. Pixel-space coordinates travel as raw
    /// float bits (TCP keeps order), so both peers replay the exact same values.
    /// Character references are indices inside MutinyTeam.Characters, which the
    /// level XML builds in the same order on every machine.
    /// </summary>
    public struct MutinyNetCommand
    {
        public MutinyNetCommandType Type;
        public int TeamNumber;
        public int CharacterIndex;
        public string WeaponType;
        public float OriginX;
        public float OriginY;
        public float ReleaseX;
        public float ReleaseY;
        public int TargetIndex;
    }

    public struct MutinyNetCommandMessage : NetworkMessage
    {
        public MutinyNetCommand Command;
    }

    /// <summary>
    /// Host -> client: begin the match. The seed drives MutinyMatchRandom so both
    /// peers resolve the opening coin flip, chest drops and flame scatter alike.
    /// QuickBattle carries the host's lobby rule with it: the drops have to use
    /// the same fall speed on both machines or the deterministic boards diverge.
    /// FirstPlayer carries the host's opening rule too: a forced side must skip
    /// the match RNG so neither peer advances the shared sequence on its own.
    /// AllowCheats carries the room's GM-menu rule: real-player matches keep the
    /// cheat menu closed unless the host opted in, so both peers gate it alike.
    /// </summary>
    public struct MatchStartMessage : NetworkMessage
    {
        public int LevelIndex;
        public int MatchSeed;
        public bool QuickBattle;
        public MutinyFirstPlayerRule FirstPlayer;
        public bool AllowCheats;
    }

    /// <summary>Lobby rule for which side opens the match (host's choice).</summary>
    public enum MutinyFirstPlayerRule : byte
    {
        Random = 0,
        Player1 = 1,
        Player2 = 2
    }

    /// <summary>End-of-match popup button pressed on the client.</summary>
    public enum MutinyMatchControlAction : byte
    {
        /// <summary>Re-seed and reload the current level on both peers.</summary>
        RestartLevel,
        /// <summary>Host re-opens the versus picker; client waits in the lobby.</summary>
        BackToLevelSelect
    }

    /// <summary>Client -> host: the remote player pressed an end-of-match button.</summary>
    public struct MatchControlRequestMessage : NetworkMessage
    {
        public MutinyMatchControlAction Action;
    }

    /// <summary>Host -> client: the host left the finished match for the level picker.</summary>
    public struct LobbyReturnMessage : NetworkMessage
    {
    }
}
