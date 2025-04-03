using System.Collections.Generic;

namespace MysticClue.Chroma.GodotClient.Audio;

public static class SoundFiles
{
    public static IReadOnlyDictionary<Sounds, string> Path => _path;

    private static Dictionary<Sounds, string> _path => new()
    {
        [Sounds.Collision_CardboardboxA] = "Collision/cardboardbox-107634-a.wav",
        [Sounds.Collision_CardboardboxD3] = "Collision/cardboardbox-107634-d3.wav",
        [Sounds.Collision_CardboardboxF] = "Collision/cardboardbox-107634-f.wav",
        [Sounds.Collision_T8DrumLoopE] = "Collision/t8-drum-loop-132283-e.wav",
        [Sounds.Countdown_CountdownSoundEffect8Bit] = "Countdown/countdown-sound-effect-8-bit-151797.mp3",
        [Sounds.Flourish_8BitBlastF] = "Flourish/8-bit-blast-63035-f.wav",
        [Sounds.Flourish_8BitBlastG] = "Flourish/8-bit-blast-63035-g.wav",
        [Sounds.Flourish_8BitBlastH] = "Flourish/8-bit-blast-63035-h.wav",
        [Sounds.Flourish_8BitBlastI] = "Flourish/8-bit-blast-63035-i.wav",
        [Sounds.Flourish_8BitBlastK] = "Flourish/8-bit-blast-63035-k.wav",
        [Sounds.Flourish_8BitBlastM] = "Flourish/8-bit-blast-63035-m.wav",
        [Sounds.Flourish_8BitBlastN] = "Flourish/8-bit-blast-63035-n.wav",
        [Sounds.Flourish_90sGameUi4] = "Flourish/90s-game-ui-14-185107.mp3",
        [Sounds.Flourish_CuteLevelUp1] = "Flourish/cute-level-up-1-189852.mp3",
        [Sounds.Flourish_CuteLevelUp2] = "Flourish/cute-level-up-2-189851.mp3",
        [Sounds.Flourish_CuteLevelUp3] = "Flourish/cute-level-up-3-189853.mp3",
        [Sounds.GameLose_90sGameUi15] = "GameLose/90s-game-ui-15-185108.mp3",
        [Sounds.GameLose_GameOverArcade] = "GameLose/game-over-arcade-6435.mp3",
        [Sounds.GameStart_8BitArcadeVdeoGameStartSoundEffectGunReloadAndJump] = "GameStart/086354_8-bit-arcade-video-game-start-sound-effect-gun-reload-and-jump-81124.mp3",
        [Sounds.GameStart_8BitBlastE] = "GameStart/8-bit-blast-63035-e.wav",
        [Sounds.GameWin_GoodResult] = "GameWin/goodresult-82807.mp3",
        [Sounds.GameWin_PowerUpSparkle1] = "GameWin/power-up-sparkle-1-177983.mp3",
        [Sounds.GameWin_YouWinSequence1] = "GameWin/you-win-sequence-1-183948.mp3",
        [Sounds.GameWin_Yipee] = "GameWin/yipee-45360.mp3",
        [Sounds.Music_Challenge_8Bit] = "Music/Challenge/8-bit-219384.mp3",
        [Sounds.Music_Challenge_ArcadeParty] = "Music/Challenge/arcade-party-173553.mp3",
        [Sounds.Music_Competitive_CreepyDevilDance] = "Music/Competitive/creepy-devil-dance-166764.mp3",
        [Sounds.Music_Competitive_CruisingDown8BitLane] = "Music/Competitive/cruising-down-8bit-lane-159615.mp3",
        [Sounds.Music_Competitive_PixelFight8Bit] = "Music/Competitive/pixel-fight-8-bit-arcade-music-background-music-for-video-208775.mp3",
        [Sounds.Music_Competitive_Ragtime] = "Music/Competitive/ragtime-193535.mp3",
        [Sounds.Music_Competitive_SparkGrooveElectroSwingDancyFunny] = "Music/Competitive/spark-groove-electro-swing-dancy-funny-198158.mp3",
        [Sounds.Music_Competitive_StealthBattle] = "Music/Competitive/stealth-battle-205902.mp3",
        [Sounds.Negative_8BitGame1] = "Negative/8-bit-game-1-186975.mp3",
        [Sounds.Negative_ClassicGameActionNegative8] = "Negative/classic-game-action-negative-8-224414.mp3",
        [Sounds.Negative_ClassicGameActionNegative9] = "Negative/classic-game-action-negative-9-224413.mp3",
        [Sounds.Negative_ClassicGameActionNegative18] = "Negative/classic-game-action-negative-18-224576.mp3",
        [Sounds.Negative_HurtC08] = "Negative/hurt_c_08-102842.mp3",
        [Sounds.Negative_RetroHurt1] = "Negative/retro-hurt-1-236672.mp3",
        [Sounds.Negative_RetroHurt2] = "Negative/retro-hurt-2-236675.mp3",
        [Sounds.Negative_StabF01] = "Negative/stab-f-01-brvhrtz-224599.mp3",
        [Sounds.Neutral_8BitBlastA] = "Neutral/8-bit-blast-63035-a.wav",
        [Sounds.Neutral_Button] = "Neutral/button-124476.mp3",
        [Sounds.Positive_90sGameUi2] = "Positive/90s-game-ui-2-185095.mp3",
        [Sounds.Positive_90sGameUi6] = "Positive/90s-game-ui-6-185099.mp3",
        [Sounds.Positive_ClassicGameActionPositive30] = "Positive/classic-game-action-positive-30-224562.mp3",
        [Sounds.Positive_ClassicGameActionPositive5] = "Positive/classic-game-action-positive-5-224402.mp3",
        [Sounds.Positive_GameBonus] = "Positive/game-bonus-144751.mp3",
    };
}
