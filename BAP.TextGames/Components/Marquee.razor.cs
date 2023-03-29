using Microsoft.AspNetCore.Components;
using BAP.Web.Games;
using MudBlazor.Charts;

namespace BAP.TextGames.Components
{
    [GamePage("Marquee", "Put a text Marquee on the buttons", UniqueId = "ac9d1bfe-f46d-4337-9d4b-7f7faee860c3")]
    public partial class Marquee : ComponentBase, IDisposable
    {
        [Inject]
        IGameProvider GameProvider { get; set; } = default!;
        [Inject]
        ILayoutProvider LayoutProvider { get; set; } = default!;
        [Inject]
        ISubscriber<LayoutChangeMessage> LayoutChangedPipe { get; set; } = default!;
        TextMarqueGame Game = default!;
        IDisposable Subscriptions { get; set; } = default!;
        List<string> TextToDisplay { get; set; } = new();
        HashSet<MarqueType> SelectedTypes { get; set; } = new();




        protected override void OnInitialized()
        {
            var bag = DisposableBag.CreateBuilder();
            LayoutChangedPipe.Subscribe(async (x) => await LayoutChanged()).AddTo(bag);
            Subscriptions = bag.Build();

            Game = (TextMarqueGame)GameProvider.UpdateToNewGameType(typeof(TextMarqueGame));
            SetNumberOfRows();
        }
        public async Task LayoutChanged()
        {
            bool changesNeeded = SetNumberOfRows();
            if (changesNeeded)
            {
                await InvokeAsync(StateHasChanged);
            }

        }

        public bool SetNumberOfRows()
        {
            if (LayoutProvider.CurrentButtonLayout != null)
            {
                if (LayoutProvider.CurrentButtonLayout.RowCount != TextToDisplay.Count)
                {
                    int variance = LayoutProvider.CurrentButtonLayout.RowCount - TextToDisplay.Count;
                    if (variance > 0)
                    {
                        for (int i = 0; i < variance; i++)
                        {
                            TextToDisplay.Add("");
                        }
                    }
                    else
                    {
                        TextToDisplay.RemoveRange(TextToDisplay.Count - Math.Abs(variance), Math.Abs(variance));
                    }
                    return true;
                }

            }
            return false;
        }

        public async Task<bool> StartGame()
        {
            Game.SetText(TextToDisplay);
            Game.MarqueTypes = SelectedTypes.ToList();
            return await Game.Start();

        }

        public Task<bool> EndGame()
        {
            Game.End("Game ended by User");
            return Task.FromResult(true);

        }

        public void Dispose()
        {
            Subscriptions.Dispose();
        }
    }
}
