using System;
using System.Collections.Generic;
using Mutiny.Simulation;
using UnityEngine;

namespace Mutiny.Presentation
{
    // SWF Team.lines and SpeechBubble/Controller state machine. One instance belongs
    // to each level root, so replacing a level also replaces its pending dialogue.
    [DisallowMultipleComponent]
    public sealed class MutinySpeechController : MonoBehaviour
    {
        private const float TickSeconds = 1f / 25f;
        private const int StartDelayTicks = 10;
        private const int CompleteDelayTicks = 160;
        private const int CharactersPerTick = 3;

        private static readonly Dictionary<string, string[]> Lines = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["squid"] = new[] { "Arrrgh what sort o' sea|beastie be this?", "Splund splund... I'm gonna|ink you up... splund splund.", "Yearggh take that you|varmint, we be eating squid|rings tonight.", "Splund splund... I'm so|happy I've just inked|myself... splund." },
            ["crab"] = new[] { "Looks like we've got a|bad case of the crabs|here, mateys!", "Prepare to lose your|beards... snip snip!", "I think we shell-shocked|them, mateys!", "Looks like they fell for|our deadly pincer|manoeuvre... at last their|beards be ours." },
            ["shark"] = new[] { "Ahoy. I think there be|something fishy here.", "We're gonna hammer you!|You're Shark Bait!", "Ya Har! 'Twas too easy!|But thar be plenty more|fish in the sea!", "Stop! ...Hammer-time!" },
            ["parrot"] = new[] { "Arrrgh!... Who be a pretty|boy then? ...Does Polly|want a cracker?", "Squawk!... Who's a pretty|boy then? ...Squawk!...|Polly want a cracker!|...Squawk!", "Arrrgh... 'tiz a shame.|They could have made fine|shoulder enhancers.", "Squawk!... Polly want a|cracker!" },
            ["monkey"] = new[] { "Guard the fruit mateys...|If the monkeys get it we'll|be up for another bout|of scurvy!", "Oooh Oooh Ahh Ahh Bananas!", "Yarrgh! Yer monkey business|ain't foolin' any of us!", "Oooh Oooh Ahh Ahh More|Bananas!" },
            ["soldier"] = new[] { "These boys look too clean|to be in a real fight! And|look... they've still got|teeth to knock out!", "We fight for King and|Country! ...and because we|get a decent benefits|package and pension plan!", "They don't look so smart|now lads... I think We'll|be using his toupee to swab|the decks!", "You pirates will get what|you deserve... a short drop|and a sudden stop!" },
            ["blindPirate"] = new[] { "Yargh... These guys have|one too many eye patches!|This should be easy! Let's|get 'em!", "Get who? Where am I...|how did I get here?", "Yarrgh!|They didn't see us coming!", "Gar... I can't hear them...|Does anyone know if we|won?" },
            ["femalePirate"] = new[] { "Garrrgh... Looks like we've|found ourselves some|mighty fine booty here,|mateys!", "Yarp... The only booty you|will be getting is on the|end of my foot!", "Arrgh... shame... what a|waste of such lovely|sea legs!", "Yarp... Everybody knows|girls are better than boys!" },
            ["oldPirate"] = new[] { "Arrrrggggh... I think we|found some old sea dogs!|Ready for retirement!", "We're not Old, we're|experienced... and now we're|gonna kick your... erm...|erm... what was I saying|again... who are you?", "Garrrrgh! Ok, Grandads...|time for your afternoon|nap!", "That'll show you... in my|day we used to fight a|Kraken with nothin' but a|couple o' stones. Arrrgh...|Those be the days..." },
            ["rainbowBeard"] = new[] { "This guy be looking a little|too jolly to be flying the|ol' jolly roger, eh mateys?", "Ohhhh look at you... don't|you look tough? Anyway,|haven't you heard? Fancy|colours are the new black!", "I'd have beaten you sooner if|your fancy beards didn't|keep catchin' the light!", "Oo-er! Now we're finished|with them, it's time for a|bit of retail therapy!" },
            ["cabinBoy"] = new[] { "Yarrgh! ...ye be wanting to|grow a proper beard before|taking on the likes of us,|boys...", "We're no boys! I had a couple|of hairs only last month on|me chin, just ask me mum!", "Yarrgh! Best be running|along home now boys! I think|it be past yer bedtime?", "That was fun... just like a|Nitrome video game! LOL!" },
            ["tribe"] = new[] { "Garrgh! I've got a bad feelin'|about this lads! They be|lookin' kinda hungry!", "Get the cookin' pot ready...|pirate stew tonight!", "Good work lads! we've|avoided being lunch... looks|like they'll have to stick|with mangos and coconuts|from now on!", "mmmmmmm! can't remember|the last time we had a good|pirate stew!|...has anyone got the salt?" },
            ["skeletonPirate"] = new[] { "This crew looks like they|could do with a good feed|more than a fight...", "you'll have to catch us all|first ...fatty!", "I am rubber, you are glue!", "Sticks and stones may break|my bones but pirates never|hurt me!" },
            ["bossGuy"] = new[] { "Garrrgh! what a big hat...|I think he be trying to|make up for something, lads!", "Argh! ye not be winning with|a hat like that. And once I|win I'll be taking your hats.", "I think he was a little|hot-headed...|maybe it was his hat!", "These hats 'll be a mighty|fine addition to my|collection!" },
            ["bossGuyZombie"] = new[] { "I think he's been playing|with a few too many|voodoo dolls, lads! he's back|from Davey Jones locker.", "aaaaaaaaahhhhhhhh...|hhhhhaaaaattttttss...|errrr... I mean...|bbbbrrraaaiiinnsss...", "Yo-Ho-Ho... we've done it|lads! that finished him off...|eerrrrm didn't we?", "Yar hats be mine after all!|hhhhhaaaaattttttss!!!" },
        };

        private MutinyTurnManager m_TurnManager;
        private bool m_IntroPending;
        private bool m_Active;
        private bool m_EndingLine;
        private int m_LineIndex;
        private int m_StartTicks;
        private int m_CompleteTicks;
        private int m_RevealedCharacters;
        private string m_Line = string.Empty;
        private string m_LineKey;
        private string m_EnglishLine;
        private float m_TickAccumulator;
        private Vector3 m_BubbleWorldPosition;

        public bool HasActiveBubble => m_Active;
        public bool HasPendingOrActiveSpeech => m_IntroPending || m_Active;
        public bool IsBubbleVisible => m_Active && m_StartTicks == 0;
        public bool IsPlayingEndingLine => m_Active && m_EndingLine;
        public Vector3 BubbleWorldPosition => m_BubbleWorldPosition;
        public string VisibleText => m_Active ? m_Line.Substring(0, m_RevealedCharacters) : string.Empty;
        public MutinyCharacter Speaker { get; private set; }

        public void Initialize(MutinyTurnManager turnManager)
        {
            MutinyLocalization.Changed -= RefreshLanguage;
            MutinyLocalization.Changed += RefreshLanguage;
            if (m_TurnManager != null)
                m_TurnManager.OnGameOver -= HandleGameOver;
            m_TurnManager = turnManager;
            m_IntroPending = turnManager != null && turnManager.Team2 != null && turnManager.Team2.IsAiControlled;
            m_Active = false;
            m_EndingLine = false;
            if (turnManager != null)
                turnManager.OnGameOver += HandleGameOver;
        }

        private void OnDestroy()
        {
            MutinyLocalization.Changed -= RefreshLanguage;
            if (m_TurnManager != null)
                m_TurnManager.OnGameOver -= HandleGameOver;
        }

        private void Update()
        {
            AdvanceSpeech(Time.unscaledDeltaTime);
        }

        internal void AdvanceSpeechForVerification(float deltaTime) => AdvanceSpeech(deltaTime);

        private void AdvanceSpeech(float deltaTime)
        {
            if (m_IntroPending && m_TurnManager != null &&
                m_TurnManager.CurrentPhase != TurnPhase.NotStarted)
            {
                MutinyFrontendController frontend = FindAnyObjectByType<MutinyFrontendController>();
                if (frontend == null || frontend.CurrentPage == MutinyFrontendPage.Gameplay)
                {
                    m_IntroPending = false;
                    StartLine(0, m_TurnManager.Team1);
                }
            }

            if (!m_Active)
                return;
            m_TickAccumulator += deltaTime;
            while (m_Active && m_TickAccumulator >= TickSeconds)
            {
                m_TickAccumulator -= TickSeconds;
                AdvanceTick();
            }
        }

        private void HandleGameOver(GameOverResult result)
        {
            m_IntroPending = false;
            if (m_TurnManager == null || m_TurnManager.Team2 == null ||
                !m_TurnManager.Team2.IsAiControlled)
                return;

            if (result == GameOverResult.Team1Wins)
                StartLine(2, m_TurnManager.Team1);
            else if (result == GameOverResult.Team2Wins)
                StartLine(3, m_TurnManager.Team2);
            // A draw has no surviving speaker and opens the popup directly.
        }

        private void StartLine(int lineIndex, MutinyTeam team)
        {
            Speaker = ChooseSpeaker(team);
            string type = GetTeamType(m_TurnManager.Team2);
            if (Speaker == null || type == null || !Lines.TryGetValue(type, out string[] sequence))
            {
                m_Active = false;
                m_EndingLine = false;
                Speaker = null;
                return;
            }

            m_LineIndex = lineIndex;
            m_LineKey = LineKey(type, lineIndex);
            m_EnglishLine = sequence[lineIndex];
            m_Line = MutinyLocalization.Text(m_LineKey, m_EnglishLine);
            m_Active = true;
            m_EndingLine = lineIndex >= 2;
            m_StartTicks = StartDelayTicks;
            m_CompleteTicks = 0;
            m_RevealedCharacters = 0;
            m_TickAccumulator = 0f;
            m_BubbleWorldPosition = Speaker.transform.position +
                                    Vector3.up * (90f / MutinyPhysics.PixelsPerUnit);
            // SpeechBubble.setTarget plays the first character's team type once,
            // when the line is assigned (before the bubble's ten-frame reveal).
            string voiceType = GetTeamType(team);
            if (voiceType != null)
                MutinyAudioManager.Instance?.PlaySFX(voiceType);
        }

        public static string LineKey(string opponentType, int lineIndex) =>
            "speech." + opponentType + "." + lineIndex;

        private void RefreshLanguage()
        {
            if (!m_Active)
                return;
            bool complete = m_RevealedCharacters == m_Line.Length;
            float revealedFraction = m_Line.Length == 0 ? 0f : (float)m_RevealedCharacters / m_Line.Length;
            m_Line = MutinyLocalization.Text(m_LineKey, m_EnglishLine);
            m_RevealedCharacters = complete ? m_Line.Length : Mathf.FloorToInt(m_Line.Length * revealedFraction);
            // Keep speaker, voice playback, delay, hold timer and tick accumulator.
            // Reassigning via StartLine would restart the conversation on every switch.
        }

        private void AdvanceTick()
        {
            if (m_StartTicks > 0)
            {
                m_StartTicks--;
                return;
            }
            if (m_RevealedCharacters < m_Line.Length)
            {
                m_RevealedCharacters = Mathf.Min(m_Line.Length, m_RevealedCharacters + CharactersPerTick);
                m_CompleteTicks = 0;
                return;
            }
            if (++m_CompleteTicks <= CompleteDelayTicks)
                return;

            if (m_LineIndex == 0)
            {
                StartLine(1, m_TurnManager.Team2);
                return;
            }
            m_Active = false;
            m_EndingLine = false;
            Speaker = null;
        }

        public void Click()
        {
            if (!IsBubbleVisible)
                return;
            if (m_RevealedCharacters < m_Line.Length)
            {
                // Correct the original TileSystem.mouseDown path: it wrote
                // mc.textField while the visible text was mc.textHolder.textField.
                m_RevealedCharacters = m_Line.Length;
                m_CompleteTicks = 0;
            }
            else if (m_CompleteTicks > 1)
            {
                // Same effect as Flash's Infinity: finish on the next tick.
                m_CompleteTicks = CompleteDelayTicks;
            }
        }

        public static MutinyCharacter ChooseSpeaker(MutinyTeam team)
        {
            if (team == null || team.Characters == null)
                return null;
            foreach (MutinyCharacter character in team.Characters)
            {
                if (character != null && character.IsAlive &&
                    !string.IsNullOrEmpty(character.CharacterType) &&
                    (character.CharacterType.Contains("Captain") || character.CharacterType == "tribeChief"))
                    return character;
            }
            foreach (MutinyCharacter character in team.Characters)
            {
                if (character != null && character.IsAlive)
                    return character;
            }
            return null;
        }

        private static string GetTeamType(MutinyTeam team)
        {
            if (team == null || team.Characters == null || team.Characters.Count == 0 ||
                team.Characters[0] == null)
                return null;
            return team.Characters[0].CharacterType?.Replace("Captain", "").Replace("Chief", "");
        }
    }
}
