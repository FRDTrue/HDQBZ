using System;

namespace Mutiny.Presentation
{
    public enum MutinyFrontendPage
    {
        Title,
        GameSelect,
        LanLobby,
        LevelSelect,
        TwoPlayerLevelSelect,
        Help,
        Gameplay,
        Ending,
        Credits,
        Scores
    }

    /// <summary>
    /// Production state machine for the original front-end routes.
    /// Keeping navigation separate from drawing makes the actual route directly verifiable.
    /// </summary>
    public sealed class MutinyFrontendFlow
    {
        public MutinyFrontendPage CurrentPage { get; private set; } = MutinyFrontendPage.Title;
        public int SelectedTwoPlayerLevel { get; private set; } = 16;
        // The versus picker serves both the local two-player route and the LAN
        // host, which reaches it while still waiting for its own MatchStart.
        private bool m_OnlineLevelSelect;

        public void PressPlay()
        {
            if (CurrentPage == MutinyFrontendPage.Title)
                CurrentPage = MutinyFrontendPage.GameSelect;
        }

        public void PressHelp()
        {
            if (CurrentPage == MutinyFrontendPage.Title)
                CurrentPage = MutinyFrontendPage.Help;
        }

        public void PressHelpBack()
        {
            if (CurrentPage == MutinyFrontendPage.Help)
                CurrentPage = MutinyFrontendPage.Title;
        }

        public void PressCredits()
        {
            if (CurrentPage == MutinyFrontendPage.Title)
                CurrentPage = MutinyFrontendPage.Credits;
        }

        public void PressCreditsBack()
        {
            if (CurrentPage == MutinyFrontendPage.Credits)
                CurrentPage = MutinyFrontendPage.Title;
        }

        public void PressScores()
        {
            if (CurrentPage == MutinyFrontendPage.Title)
                CurrentPage = MutinyFrontendPage.Scores;
        }

        public void PressScoresBack()
        {
            if (CurrentPage == MutinyFrontendPage.Scores)
                CurrentPage = MutinyFrontendPage.Title;
        }

        public void PressGameSelectBack()
        {
            if (CurrentPage == MutinyFrontendPage.GameSelect)
                CurrentPage = MutinyFrontendPage.Title;
        }

        public void PressOnePlayer()
        {
            if (CurrentPage == MutinyFrontendPage.GameSelect)
                CurrentPage = MutinyFrontendPage.LevelSelect;
        }

        public void PressTwoPlayer()
        {
            if (CurrentPage == MutinyFrontendPage.GameSelect)
                CurrentPage = MutinyFrontendPage.TwoPlayerLevelSelect;
        }

        public void PressLanMultiplayer()
        {
            if (CurrentPage == MutinyFrontendPage.GameSelect)
                CurrentPage = MutinyFrontendPage.LanLobby;
        }

        public void PressLanLobbyBack()
        {
            if (CurrentPage == MutinyFrontendPage.LanLobby)
                CurrentPage = MutinyFrontendPage.GameSelect;
        }

        /// <summary>Host moved from the LAN lobby into the versus level picker.</summary>
        public bool EnterOnlineLevelSelect()
        {
            if (CurrentPage != MutinyFrontendPage.LanLobby)
                return false;
            m_OnlineLevelSelect = true;
            CurrentPage = MutinyFrontendPage.TwoPlayerLevelSelect;
            return true;
        }

        /// <summary>A MatchStart message (or host pick) moved the lobby into gameplay.</summary>
        public bool EnterOnlineGameplay()
        {
            // The client waits in the lobby for the host's choice, so its
            // MatchStart lands on LanLobby. The host picks the level itself and
            // is already in the versus picker when its own MatchStart arrives;
            // without that route the host would stay on the menu page while the
            // match runs underneath.
            if (CurrentPage != MutinyFrontendPage.LanLobby &&
                !(m_OnlineLevelSelect && CurrentPage == MutinyFrontendPage.TwoPlayerLevelSelect))
                return false;
            m_OnlineLevelSelect = false;
            CurrentPage = MutinyFrontendPage.Gameplay;
            return true;
        }

        /// <summary>
        /// Online match ended: the host re-opens the versus picker to choose the
        /// next level. The online route stays armed so the following MatchStart
        /// can leave the picker again.
        /// </summary>
        public bool ReturnToOnlineLevelSelect()
        {
            if (CurrentPage != MutinyFrontendPage.Gameplay)
                return false;
            m_OnlineLevelSelect = true;
            CurrentPage = MutinyFrontendPage.TwoPlayerLevelSelect;
            return true;
        }

        /// <summary>Online match ended: the client waits in the lobby for the host's next pick.</summary>
        public bool ReturnToLanLobby()
        {
            if (CurrentPage != MutinyFrontendPage.Gameplay)
                return false;
            CurrentPage = MutinyFrontendPage.LanLobby;
            return true;
        }

        /// <summary>Disconnects and explicit online quits land on the title page.</summary>
        public void ForceReturnToTitle()
        {
            m_OnlineLevelSelect = false;
            CurrentPage = MutinyFrontendPage.Title;
        }

        public void PressTwoPlayerLevelSelectBack()
        {
            if (CurrentPage == MutinyFrontendPage.TwoPlayerLevelSelect)
            {
                m_OnlineLevelSelect = false;
                CurrentPage = MutinyFrontendPage.GameSelect;
            }
        }

        public bool StepTwoPlayerLevel(int delta)
        {
            if (CurrentPage != MutinyFrontendPage.TwoPlayerLevelSelect ||
                (delta != -1 && delta != 1))
                return false;
            int next = SelectedTwoPlayerLevel + delta;
            if (next < 16 || next > 33)
                return false;
            SelectedTwoPlayerLevel = next;
            return true;
        }

        public bool TrySelectTwoPlayerLevel(Func<int, bool> hasLevelData)
        {
            if (CurrentPage != MutinyFrontendPage.TwoPlayerLevelSelect ||
                hasLevelData == null || !hasLevelData(SelectedTwoPlayerLevel))
                return false;
            CurrentPage = MutinyFrontendPage.Gameplay;
            return true;
        }

        public bool ReturnToTwoPlayerLevelSelect()
        {
            if (CurrentPage != MutinyFrontendPage.Gameplay)
                return false;
            CurrentPage = MutinyFrontendPage.TwoPlayerLevelSelect;
            return true;
        }

        public void PressLevelSelectBack()
        {
            if (CurrentPage == MutinyFrontendPage.LevelSelect)
                CurrentPage = MutinyFrontendPage.GameSelect;
        }

        public bool TrySelectLevel(int level, Func<int, bool> isUnlocked)
        {
            if (CurrentPage != MutinyFrontendPage.LevelSelect ||
                level < 1 || level > MutinyFrontendController.SinglePlayerLevelCount ||
                isUnlocked == null || !isUnlocked(level))
            {
                return false;
            }

            CurrentPage = MutinyFrontendPage.Gameplay;
            return true;
        }

        /// <summary>
        /// Original QuitGameButton sends a one-player game to level_select_1p,
        /// rather than the title page.
        /// </summary>
        public bool ReturnToSinglePlayerLevelSelect()
        {
            if (CurrentPage != MutinyFrontendPage.Gameplay)
                return false;

            CurrentPage = MutinyFrontendPage.LevelSelect;
            return true;
        }

        public bool ShowEnding()
        {
            if (CurrentPage != MutinyFrontendPage.Gameplay)
                return false;
            CurrentPage = MutinyFrontendPage.Ending;
            return true;
        }

        public bool ReturnFromEndingToTitle()
        {
            if (CurrentPage != MutinyFrontendPage.Ending)
                return false;
            CurrentPage = MutinyFrontendPage.Title;
            return true;
        }
    }
}
