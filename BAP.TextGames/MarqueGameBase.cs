using MessagePipe;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using BAP.Helpers;

namespace BAP.TextGames
{

    public class MarqueLine
    {
        public List<string> NodeIdsOrderedLeftToRight { get; set; } = new();
        public List<ulong[,]> Images { get; set; } = new();
        public bool LeftToRight { get; set; } = true;
    }

    public abstract class MarqueGameBase : IBapGame
    {

        public ILogger Logger { get; set; }
        public bool IsGameRunning { get; internal set; }
        public List<MarqueLine> Lines { get; internal set; } = new List<MarqueLine>();
        internal IBapMessageSender MsgSender { get; set; } = default!;
        public int AnimationRate { get; set; } = 200;
        public int FrameSpacing { get; set; } = 2;
        Helpers.AnimationController Animate { get; set; }
        public bool ScrollAllTextOffScreen { get; set; }
        IDisposable subscriptions = default!;
        ISubscriber<AnimationCompleteMessage> AnimationCompletePipe { get; set; }
        public MarqueGameBase(ILogger logger, IBapMessageSender messageSender, Helpers.AnimationController animationController, ISubscriber<AnimationCompleteMessage> animationCompletePipe)
        {
            Logger = logger;
            MsgSender = messageSender;
            Animate = animationController;
            AnimationCompletePipe = animationCompletePipe;
            var bag = DisposableBag.CreateBuilder();
            AnimationCompletePipe.Subscribe(async (x) => await AnimationComplete(x)).AddTo(bag);
            subscriptions = bag.Build();
        }

        public abstract Task<bool> AnimationComplete(AnimationCompleteMessage animationCompleteMessage);

        public virtual async Task<bool> Start()
        {

            Logger.LogInformation($"Starting Marquee");

            IsGameRunning = true;
            //MsgSender.SendGeneralCommand(sbc);
            Animate.FrameRateInMillis = AnimationRate;
            List<BapAnimation> animations = new List<BapAnimation>();
            foreach (var line in Lines)
            {
                animations.AddRange(GenerateFramesForAllButtons(line));
            }
            //AnimationTickCountAtStartOfAnimation = Animate.CurrentFrameTickCount;
            Animate.AddOrUpdateAnimations(animations);

            return true;
        }

        private List<Helpers.BapAnimation> GenerateFramesForAllButtons(MarqueLine line)
        {

            List<BapAnimation> animations = new List<BapAnimation>();
            ulong[,] bigMatrix = AnimationHelper.BuildBigMatrix(line.Images, line.NodeIdsOrderedLeftToRight.Count, FrameSpacing, ScrollAllTextOffScreen);
            foreach (var nodeId in line.NodeIdsOrderedLeftToRight)
            {
                animations.Add(new BapAnimation(new List<Frame>(), nodeId));
            }
            int currentLeftPixel = 0;
            int currentFrameId = 0;
            int screenWidth = line.NodeIdsOrderedLeftToRight.Count * 8;
            while (currentLeftPixel + screenWidth < bigMatrix.GetLength(1))
            {
                for (int i = 0; i < line.NodeIdsOrderedLeftToRight.Count; i++)
                {
                    animations[i].Frames.Add(new Frame(bigMatrix.ExtractMatrix(0, currentLeftPixel + (i * 8)), currentFrameId));
                }

                currentLeftPixel++;
                currentFrameId++;
            }
            return animations;

        }



        public virtual bool End(string reason)
        {
            MsgSender.SendUpdate(reason, true);
            Animate.Stop();
            IsGameRunning = false;
            return true;
        }

        public virtual async Task<bool> ForceEndGame()
        {
            End("Game was force Closed");
            return true;
        }

        public virtual void Dispose()
        {
            subscriptions.Dispose();
        }
    }

}
