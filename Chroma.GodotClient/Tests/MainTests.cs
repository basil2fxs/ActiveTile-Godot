using GdUnit4;
using Godot;
using MysticClue.Chroma.GodotClient.Debugging;
using MysticClue.Chroma.GodotClient.Games;
using MysticClue.Chroma.GodotClient.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static GdUnit4.Assertions;

namespace MysticClue.Chroma.GodotClient.Tests;

[TestSuite]
public class MainTests
{
    private record struct MainState(
        Button resetButton,
        Button lightButton,
        Button backButton,
        PlayerSelect playerSelect,
        GameSelect gameSelect,
        Dictionary<GameSelection.GameType, Button> gameTypeSelect,
        Container gameList,
        List<Button> difficultySelect,
        Button startGameButton,
        InsideScreen insideScreen,
        Container feedbackSurvey,
        List<Button> feedbackButtons);

    [TestCase]
    public static async Task TestMain()
    {
        // High level test for Main.tscn, but as it becomes more complex, it'll probably better
        // to test components individually.
        ISceneRunner runner = ISceneRunner.Load("res://Main.tscn");
        await runner.AwaitIdleFrame();
        var main = (Main)runner.Scene();
        var ms = new MainState(
            main.GetNode<Button>("%ResetSession"),
            main.GetNode<Button>("%LightButton"),
            main.GetNode<Button>("%BackButton"),
            main.GetNode<PlayerSelect>("%PlayerSelect"),
            main.GetNode<GameSelect>("%GameSelect"),
            new()
            {
                [GameSelection.GameType.Cooperative] = main.GetNode<Button>("%CooperativeGameType"),
                [GameSelection.GameType.Competitive] = main.GetNode<Button>("%CompetitiveGameType"),
                [GameSelection.GameType.Zen] = main.GetNode<Button>("%ZenGameType"),
            },
            main.GetNode<Container>("%GameList"),
            [
                main.GetNode<Button>("%NewbieDifficulty"),
                main.GetNode<Button>("%RegularDifficulty"),
                main.GetNode<Button>("%EliteDifficulty"),
            ],
            main.GetNode<Button>("%StartGame"),
            main.GetNode<InsideScreen>("%MainArea/InsideScreen") ?? main.GetNode<InsideScreen>("InsideScreenWindow/InsideScreen"),
            main.GetNode<Container>("%FeedbackSurvey"),
            [
                main.GetNode<Button>("%SadFace"),
                main.GetNode<Button>("%NeutralFace"),
                main.GetNode<Button>("%HappyFace"),
            ]
        );
        await runner.AwaitIdleFrame();
        CheckPlayerSelectState(ms);

        // Switch lights on and off.
        // Need to wait extra frames because they take a while to fade out.
        for (int i = 0; i < 2; i++)
        {
            ms.lightButton.Press();
            await runner.AwaitIdleFrame();
            CheckLightsOnState(ms);

            ms.backButton.Press();
            await runner.AwaitIdleFrame();
            await runner.AwaitIdleFrame();
            await runner.AwaitIdleFrame();
            await runner.AwaitIdleFrame();
            CheckPlayerSelectState(ms);
        }

        HashSet<GameSelection> allGameVariants = new(ms.gameSelect.AllGameVariants);
        HashSet<GameSelection> sawGameVariants = [];

        // Select every player count.
        var playerSelectButtons = ms.playerSelect.GetNode<HFlowContainer>("HFlowContainer").GetChildrenByType<Button>();
        int surveyButtonChoice = 0;
        foreach (var psb in playerSelectButtons)
        {
            psb.Press();
            await runner.AwaitIdleFrame();
            CheckGameSelectState(ms);

            // Select every game type.
            foreach (var (gameType, gameTypeButton) in ms.gameTypeSelect)
            {
                gameTypeButton.Press();
                await runner.AwaitIdleFrame();
                CheckGameSelectState(ms);

                // Select every available game.
                foreach (var gb in ms.gameList.GetChildrenByType<Button>().Where(b => b.Visible))
                {
                    gb.Press();
                    await runner.AwaitIdleFrame();
                    CheckGameSelectState(ms);

                    // Select every difficulty.
                    foreach (var db in ms.difficultySelect.Where(b => b.Visible))
                    {
                        db.Press();
                        await runner.AwaitIdleFrame();
                        CheckGameSelectState(ms);

                        var gs = ms.gameSelect.CurrentGameSelection!.Value;
                        sawGameVariants.Add(gs);

                        AssertThat(gs.Type).IsEqual(gameType);
                        if (gs.Type == GameSelection.GameType.Competitive)
                            AssertThat(gs.PlayerCount).IsEqual(ms.gameSelect.CurrentPlayerCount);
                        else
                            AssertThat(gs.PlayerCount).IsGreaterEqual(ms.gameSelect.CurrentPlayerCount);

                        ms.startGameButton.Press();
                        await runner.AwaitIdleFrame();
                        CheckInGameState(ms);

                        ms.backButton.Press();
                        await runner.AwaitIdleFrame();
                        CheckSurveyState(ms);

                        // Back goes back into the game.
                        ms.backButton.Press();
                        await runner.AwaitIdleFrame();
                        CheckInGameState(ms);

                        ms.backButton.Press();
                        await runner.AwaitIdleFrame();
                        CheckSurveyState(ms);

                        surveyButtonChoice = (surveyButtonChoice + 1) % ms.feedbackButtons.Count;
                        ms.feedbackButtons[surveyButtonChoice].Press();
                        CheckGameSelectState(ms);
                    }
                }
            }

            ms.backButton.Press();
            await runner.AwaitIdleFrame();
            CheckPlayerSelectState(ms);

            ms.resetButton.Press();
            await runner.AwaitIdleFrame();
            CheckPlayerSelectState(ms);
        }

        // We should have seen every game variant.
        AssertThat(sawGameVariants).Equals(allGameVariants);
    }

    private static void CheckLightsOnState(MainState ms)
    {
        AssertThat(ms.backButton.Visible).IsTrue();
        AssertThat(ms.playerSelect.Visible).IsFalse();
        AssertThat(ms.gameSelect.Visible).IsFalse();
        AssertThat(ms.insideScreen.Visible).IsTrue();
        AssertThat(ms.feedbackSurvey.Visible).IsFalse();
    }

    private static void CheckPlayerSelectState(MainState ms)
    {
        AssertThat(ms.backButton.Visible).IsFalse();
        AssertThat(ms.playerSelect.Visible).IsTrue();
        AssertThat(ms.gameSelect.Visible).IsFalse();
        AssertThat(ms.insideScreen.Visible).IsFalse();
        AssertThat(ms.feedbackSurvey.Visible).IsFalse();
    }

    private static void CheckGameSelectState(MainState ms)
    {
        AssertThat(ms.backButton.Visible).IsTrue();
        AssertThat(ms.playerSelect.Visible).IsFalse();
        AssertThat(ms.gameSelect.Visible).IsTrue();
        AssertThat(ms.insideScreen.Visible).IsFalse();
        AssertThat(ms.feedbackSurvey.Visible).IsFalse();

        // Only one game should be selected.
        var gameButtons = ms.gameList.GetChildrenByType<Button>();
        if (gameButtons.Any(b => b.Visible))
            AssertThat(gameButtons.Count(b => b.Visible && b.ButtonPressed)).IsEqual(1);

        // Only one difficulty should be selected.
        if (ms.difficultySelect.Any(b => b.Visible))
            AssertThat(ms.difficultySelect.Count(b => b.Visible && b.ButtonPressed)).IsEqual(1);
    }

    private static void CheckInGameState(MainState ms)
    {
        AssertThat(ms.backButton.Visible).IsTrue();
        AssertThat(ms.playerSelect.Visible).IsFalse();
        AssertThat(ms.gameSelect.Visible).IsFalse();
        AssertThat(ms.insideScreen.Visible).IsTrue();
        AssertThat(ms.feedbackSurvey.Visible).IsFalse();
    }

    private static void CheckSurveyState(MainState ms)
    {
        AssertThat(ms.backButton.Visible).IsTrue();
        AssertThat(ms.playerSelect.Visible).IsFalse();
        AssertThat(ms.gameSelect.Visible).IsFalse();
        AssertThat(ms.insideScreen.Visible).IsFalse();
        AssertThat(ms.feedbackSurvey.Visible).IsTrue();
    }
}
