using Microsoft.AspNetCore.Components;
using BAP.Web.Games;
using System.Linq;
using Blazored.FluentValidation;
using BAP.UIHelpers.Components;
//using SpotifyAPI.Web;

namespace BAP.TextGames.Components
{
    [GamePage("Vocab Game", "Working on spelling your Vocab words", UniqueId = "4512ba27-1f61-4b72-a795-6ce512f940e7")]
    public partial class Vocab : GamePage
    {
        [Inject]
        IGameProvider GameHandler { get; set; } = default!;
        [Inject]
        ILayoutProvider LayoutProvider { get; set; } = default!;
        //[Inject]
        //NavigationManager NavManager { get; set; } = default!;

        private VocabGame game { get; set; } = default!;

        public bool UseTestingButtons
        {
            get
            {
                return game.useTestingButtons;
            }
            set
            {
                game.useTestingButtons = value;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            game = (VocabGame)GameHandler.UpdateToNewGameType(typeof(VocabGame));
            if (!game.IsGameRunning)
            {
                game.Initialize();
                await game.InitializeAsync();
            }
        }


        private void ShowHighScores(Score? newScore = null)
        {
            if (!game.IsGameRunning)
            {
                game.SetButtonCountAndButtonList(false);
                DialogOptions dialogOptions = new DialogOptions()
                {
                    CloseButton = false,
                    DisableBackdropClick = newScore != null
                };
                DialogParameters dialogParameters = new DialogParameters();
                dialogParameters.Add("NewScore", newScore);
                dialogParameters.Add("GameDataSaver", game?.DbSaver);
                dialogParameters.Add("Description", newScore?.DifficultyDescription ?? "");
                dialogParameters.Add("Difficulty", newScore?.DifficultyId ?? "");
                DialogService.Show<HighScoreTable>("High Scores", dialogParameters, dialogOptions);
            }
        }

        async Task<bool> StartGame()
        {
            _ = KeepTimeUpdated();

            await game.Start();
            return true;
        }
        async Task<bool> EndGame()
        {
            await game.EndGame("Game Ended by User");
            return true;
        }

        private void ShowEditPersonDialog(bool currentPerson)
        {
            if (!game.IsGameRunning)
            {
                DialogOptions dialogOptions = new DialogOptions()
                {
                    CloseButton = false,
                    DisableBackdropClick = true
                };
                DialogParameters dialogParameters = new DialogParameters();
                dialogParameters.Add("PersonId", currentPerson ? game?.SelectedPersonId : "");
                dialogParameters.Add("VocabGame", game);
                DialogService.Show<VocabGamePersonEditor>($"Update Person", dialogParameters, dialogOptions);
            }

        }


        private void ShowVocabGameSetup()
        {
            if (!game.IsGameRunning)
            {
                DialogOptions dialogOptions = new DialogOptions()
                {
                    CloseButton = false,
                    DisableBackdropClick = true
                };
                DialogParameters dialogParameters = new DialogParameters();
                dialogParameters.Add("GameDataSaver", game?.DbSaver);
                DialogService.Show<VocabGameSetup>("Update Vocab Words", dialogParameters, dialogOptions);
            }

        }


        public override async Task<bool> GameUpdateAsync(GameEventMessage gameEventMessage)
        {
            if (gameEventMessage.Message.StartsWith("VocabGameUpdated"))
            {
                await game.RefreshSavedVocab();
            }
            return await base.GameUpdateAsync(gameEventMessage);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

    }
}
