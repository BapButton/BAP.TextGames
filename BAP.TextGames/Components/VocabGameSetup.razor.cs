﻿using Microsoft.AspNetCore.Components;
using BAP.Web.Games;
using BAP.Types;
using BAP.Helpers;

namespace BAP.TextGames.Components
{
    public partial class VocabGameSetup : ComponentBase, IDisposable
    {

        [Parameter]
        public IGameDataSaver GameDataSaver { get; set; } = default!;
        [Inject]
        private IDialogService DialogService { get; set; } = default!;
        [Inject]
        IGameProvider GameProvider { get; set; } = default!;
        [Inject]
        private IBapMessageSender _messageSender { get; set; } = default!;
        private VocabGame VocabGame { get; set; } = default!;
        internal VocabWords _savedVocab { get; set; } = new();
        internal string SavedWordsConcat { get; set; } = "";
        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; } = default!;


        protected override void OnInitialized()
        {
            var currentGame = (VocabGame?)GameProvider?.CurrentGame;
            if (currentGame != null)
            {
                VocabGame = currentGame;
                _savedVocab = VocabGame.SavedVocab;
            }
            else
            {
                throw new Exception("Vocab game is not currentled loaded");
            }

            SavedWordsConcat = string.Join(@", ", _savedVocab.SavedWords);


            //CurrentLocationInScoreBoards = CurrentScoreBoards.IndexOf()

        }


        public async Task CloseWithoutSaving()
        {

            bool? result = await DialogService.ShowMessageBox(
            "Warning",
            "Exit without saving new Vocabulary List",
            yesText: "Don't Save Changes", cancelText: "Go back to editing");
            if (result.HasValue && result == true)
            {
                MudDialog.Close();
            }
            StateHasChanged();

        }


        public async Task Save()
        {
            var vocabGame = (VocabGame)GameProvider.CurrentGame!;
            await vocabGame.SaveNewVocabWords(SavedWordsConcat, _savedVocab.IsSpanish);
            vocabGame.RefreshSavedVocab();
            MudDialog.Close();
            _messageSender.SendUpdate("Saved new words", pageRefreshRecommended: true);
        }
        public void Dispose()
        {


        }
    }
}
