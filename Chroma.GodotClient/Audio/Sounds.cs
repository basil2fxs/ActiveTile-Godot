namespace MysticClue.Chroma.GodotClient.Audio;

/// <summary>
/// Enum of sounds so that games don't need to know the names/paths of specific audio files.
///
/// At the moment all audio files are from pixabay.com, and all have the very permissive
/// Pixabay Content License.
///
/// We need to add an attribution file (including publishing it in the release software)
/// in order to use any files that require attribution.
/// </summary>
public enum Sounds
{
    // Naming convention is Directory_Path_Name.
    // Pascal case for each component separated by underscores.
    // The name should be based on the file name.
    //
    // Disable the "no underscores" naming rule.
#pragma warning disable CA1707
    Collision_CardboardboxA,
    Collision_CardboardboxD3,
    Collision_CardboardboxF,
    Collision_T8DrumLoopE,
    Countdown_CountdownSoundEffect8Bit,
    Flourish_8BitBlastF,
    Flourish_8BitBlastG,
    Flourish_8BitBlastH,
    Flourish_8BitBlastI,
    Flourish_8BitBlastK,
    Flourish_8BitBlastM,
    Flourish_8BitBlastN,
    Flourish_90sGameUi4,
    Flourish_CuteLevelUp1,
    Flourish_CuteLevelUp2,
    Flourish_CuteLevelUp3,
    GameLose_90sGameUi15,
    GameLose_GameOverArcade,
    GameStart_8BitArcadeVdeoGameStartSoundEffectGunReloadAndJump,
    GameStart_8BitBlastE,
    GameWin_GoodResult,
    GameWin_PowerUpSparkle1,
    GameWin_Yipee,
    GameWin_YouWinSequence1,
    Music_Challenge_8Bit,
    Music_Challenge_ArcadeParty,
    Music_Competitive_CreepyDevilDance,
    Music_Competitive_CruisingDown8BitLane,
    Music_Competitive_PixelFight8Bit,
    Music_Competitive_Ragtime,
    Music_Competitive_SparkGrooveElectroSwingDancyFunny,
    Music_Competitive_StealthBattle,
    Negative_8BitGame1,
    Negative_ClassicGameActionNegative8,
    Negative_ClassicGameActionNegative9,
    Negative_ClassicGameActionNegative18,
    Negative_HurtC08,
    Negative_RetroHurt1,
    Negative_RetroHurt2,
    Negative_StabF01,
    Neutral_8BitBlastA,
    Neutral_Button,
    Positive_90sGameUi2,
    Positive_90sGameUi6,
    Positive_ClassicGameActionPositive30,
    Positive_ClassicGameActionPositive5,
    Positive_GameBonus
#pragma warning restore CA1707
}
