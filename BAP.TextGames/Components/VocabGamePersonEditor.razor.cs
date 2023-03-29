using Microsoft.AspNetCore.Components;
using BAP.Web.Games;
using BAP.Types;
using BAP.Helpers;

namespace BAP.TextGames.Components
{
    public partial class VocabGamePersonEditor : ComponentBase, IDisposable
    {
        [Parameter]
        public string PersonId { get; set; } = default!;
        string PersonName { get; set; } = default!;
        [Inject]
        private IDialogService DialogService { get; set; } = default!;
        [Inject]
        private IBapMessageSender _messageSender { get; set; } = default!;
        [Parameter]
        public VocabGame VocabGame { get; set; } = default!;
        [CascadingParameter]
        MudDialogInstance MudDialog { get; set; } = default!;


        protected override void OnInitialized()
        {
            PersonName = VocabGame.VocabPeople.FirstOrDefault(t => t.Id == PersonId)?.PersonName ?? "";
        }


        public async Task CloseWithoutSaving()
        {

            bool? result = await DialogService.ShowMessageBox(
            "Warning",
            "Exit without saving changes",
            yesText: "Don't Save Changes", cancelText: "Go back to editing");
            if (result.HasValue && result == true)
            {
                MudDialog.Close();
            }
            StateHasChanged();

        }


        public async Task Save()
        {
            if (string.IsNullOrEmpty(PersonId))
            {
                await VocabGame.AddPerson(PersonName);
            }
            else
            {
                await VocabGame.RenamePerson(PersonId, PersonName);
            }
            _messageSender.SendUpdate("Modified People", pageRefreshRecommended: true);
            MudDialog.Close();
        }

        public void Dispose()
        {


        }
    }
}
