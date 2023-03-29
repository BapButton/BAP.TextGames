using BAP.Types;
using BAP.Helpers;
using BAP.TextGames;

namespace BAP.Web.Games
{

    public enum MarqueType
    {
        MultiLineScrollStaggered = 1,
        MutliLineScrollEvenSpacing = 2,
        MultiLineNoAdjustment = 3,
        AllButtonsSnakeScroll = 4
    }


    public class TextMarqueGame : MarqueGameBase
    {
        public static List<MarqueType> TypesThatRequireMatchedLines => new() { MarqueType.MultiLineNoAdjustment, MarqueType.MutliLineScrollEvenSpacing, MarqueType.MultiLineScrollStaggered };

        ILayoutProvider LayoutProvider { get; set; }
        public bool Repeat { get; set; } = true;
        public List<MarqueType> MarqueTypes { get; set; } = new();
        private int CurrentMarqueTypeLocation { get; set; } = 0;
        private List<string> TextToDisplay { get; set; } = new List<string>();
        public TextMarqueGame(ILayoutProvider layoutProvider, ILogger<TextMarqueGame> logger, IBapMessageSender messageSender, AnimationController animationController, ISubscriber<AnimationCompleteMessage> animationCompletePipe) : base(logger, messageSender, animationController, animationCompletePipe)
        {
            LayoutProvider = layoutProvider;
        }

        public bool SetText(List<string> textToDisplay)
        {
            if (this.IsGameRunning)
            {
                return false;
            }
            else
            {
                TextToDisplay = textToDisplay;
                return true;
            }
        }
        public async override Task<bool> Start()
        {
            base.AnimationRate = 125;
            if (MarqueTypes.Count == 0)
            {
                MarqueTypes = new List<MarqueType>() { MarqueType.MultiLineScrollStaggered, MarqueType.AllButtonsSnakeScroll, MarqueType.MutliLineScrollEvenSpacing, MarqueType.MultiLineNoAdjustment };
            }

            if (LayoutProvider == null || LayoutProvider.CurrentButtonLayout == null)
            {
                MsgSender.SendUpdate($"Cannot Start - Without a layout this is just a garbled mess.", fatalError: true);
                return false;
            }
            if (TextToDisplay.Count != LayoutProvider.CurrentButtonLayout.RowCount)
            {
                if (MarqueTypes.Any(t => TypesThatRequireMatchedLines.Contains(t)))
                {
                    MarqueTypes = MarqueTypes.Except(TypesThatRequireMatchedLines).ToList();
                    Logger.LogWarning("Only the marque type Snake is allowed when the layout row count and line count mismatches");
                    if (MarqueTypes.Count == 0)
                    {
                        MarqueTypes.Add(MarqueType.AllButtonsSnakeScroll);
                    }
                }
            }
            BapColor bapColor = StandardColorPalettes.Default[2];
            MarqueType marqueType = MarqueTypes[CurrentMarqueTypeLocation];
            if (marqueType == MarqueType.AllButtonsSnakeScroll)
            {

                List<ulong[,]> images = new();

                foreach (char letter in string.Join(' ', TextToDisplay).ToCharArray())
                {
                    if (char.IsWhiteSpace(letter))
                    {
                        images.Add(new ulong[8, 8]);
                    }
                    else
                    {
                        images.Add(AnimationHelper.GetMatrix(PaternEnumHelper.GetEnumFromCharacter(letter), bapColor));
                    }
                }

                Lines.Add(new MarqueLine() { Images = images, NodeIdsOrderedLeftToRight = MsgSender.GetConnectedButtonsInOrder() });

            }
            else
            {
                int maxWordLength = TextToDisplay.Select(t => t.Length).Max();
                switch (marqueType)
                {
                    case MarqueType.MultiLineScrollStaggered:
                        int multiMaxWordLength = TextToDisplay.Select(t => t.Length).Max() + (2 * (TextToDisplay.Count - 1));
                        for (int i = 1; i < TextToDisplay.Count; i++)
                        {
                            TextToDisplay[i] = TextToDisplay[i].PadLeft(maxWordLength + (2 * i));
                        }
                        for (int i = 0; i < TextToDisplay.Count; i++)
                        {
                            TextToDisplay[i] = TextToDisplay[i].PadRight(multiMaxWordLength);
                        }
                        break;
                    case MarqueType.MutliLineScrollEvenSpacing:
                        for (int i = 0; i < TextToDisplay.Count; i++)
                        {
                            TextToDisplay[i] = TextToDisplay[i].PadBoth(maxWordLength);
                        }
                        break;
                    default:
                        break;
                }

                for (int i = 0; i < TextToDisplay.Count; i++)
                {
                    List<ulong[,]> images = new();

                    foreach (char letter in TextToDisplay[i].ToCharArray())
                    {
                        if (char.IsWhiteSpace(letter))
                        {
                            images.Add(new ulong[8, 8]);
                        }
                        else
                        {
                            images.Add(AnimationHelper.GetMatrix(PaternEnumHelper.GetEnumFromCharacter(letter), bapColor));
                        }
                    }

                    Lines.Add(new MarqueLine() { Images = images, NodeIdsOrderedLeftToRight = LayoutProvider.CurrentButtonLayout.ButtonPositions.Where(t => t.RowId == i + 1).OrderBy(t => t.ColumnId).Select(t => t.ButtonId).ToList() });
                }

            }


            ScrollAllTextOffScreen = true;
            return await base.Start();
        }

        public override async Task<bool> AnimationComplete(AnimationCompleteMessage animationCompleteMessage)
        {
            if (Repeat)
            {
                if (CurrentMarqueTypeLocation == MarqueTypes.Count - 1)
                {
                    CurrentMarqueTypeLocation = 0;
                }
                else
                {
                    CurrentMarqueTypeLocation++;
                }
                return await Start();
            }
            else
            {
                End("Marquee Completed");
                return true;
            }
        }
    }
}
