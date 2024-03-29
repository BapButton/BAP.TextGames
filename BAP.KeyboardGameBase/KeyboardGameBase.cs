﻿using MessagePipe;
using Microsoft.Extensions.Logging;
using BAP.Types;
using static System.Formats.Asn1.AsnWriter;
using BAP.Helpers;

namespace BAP.KeyBoardGameBase
{
    public abstract class KeyboardGameBase : IBapGame, IDisposable
    {
        public abstract ILogger _logger { get; set; }
        public bool IsGameRunning { get; internal set; }
        public abstract IGameDataSaver DbSaver { get; set; }
        ISubscriber<KeyboardKeyPressedMessage> KeyPressed { get; set; } = default!;
        IDisposable subscriptions = default!;
        internal BapColor numberColor = new BapColor(0, 255, 0);
        internal string lastNodeId = "";
        public int correctScore = 0;
        public int wrongScore = 0;
        public List<string> nodeIdsToUseForDisplay { get; set; } = new();

        public int SecondsToRun { get; set; }
        public IBapMessageSender MsgSender { get; set; }
        public IKeyboardProvider KeyboardProvider { get; set; }
        public ILayoutProvider LayoutProvider { get; set; }

        public PausableTimer gameTimer = new PausableTimer();
        public char[] Answer { get; set; } = Array.Empty<char>();
        DateTime gameEndTime;
        List<string> _correctSounds = new();
        List<string> _wrongSounds = new();
        public int CurrentSpotInAnswerString = 0;
        public int ButtonCount { get; set; } = 0;
        internal char CurrentDigit => Answer[CurrentSpotInAnswerString];
        IKeyboardProvider keyboard { get; set; }

        public KeyboardGameBase(IKeyboardProvider keyboardProvider, ILayoutProvider layoutProvider, IBapMessageSender msgSender, ISubscriber<KeyboardKeyPressedMessage> keyPressed)
        {
            KeyboardProvider = keyboardProvider;
            MsgSender = msgSender;
            KeyPressed = keyPressed;
            LayoutProvider = layoutProvider;

            if (layoutProvider == null)
            {
                throw new Exception("No keyboard is setup");
            }
            keyboard = keyboardProvider;
            var bag = DisposableBag.CreateBuilder();
            KeyPressed.Subscribe(async (x) => await OnCharacterPressed(x)).AddTo(bag);
            subscriptions = bag.Build();
        }

        public MathGameStatus GetStatus()
        {
            return new MathGameStatus()
            {
                CorrectScore = correctScore,
                WrongScore = wrongScore,
                TimeRemaining = gameEndTime - DateTime.Now
            };

        }

        public virtual async void EndGameEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            keyboard.Disable();
            IsGameRunning = true;
            Score score = GenerateScoreWithCurrentData();
            List<Score> scores = await DbSaver.GetScoresWithNewScoreIfWarranted(score);
            await EndGame("Time Expired - Game Ended", scores.Where(t => t.ScoreId == 0).Any(), false);
        }

        public abstract Score GenerateScoreWithCurrentData();

        public virtual async Task<bool> EndGame(string message, bool highScoreAchieved = false, bool forceClosed = true)
        {
            IsGameRunning = false;
            _logger.LogInformation(message);
            gameTimer.Dispose();
            keyboard.Disable(!highScoreAchieved);
            if (forceClosed)
            {
                MsgSender.SendImageToAllButtons(new ButtonImage());
            }
            else if (forceClosed == false && !highScoreAchieved)
            {

                MsgSender.SendImageToAllButtons(new ButtonImage(PatternHelper.GetBytesForPattern(Patterns.AllOneColor), new(255, 0, 0)));
                MsgSender.PlayAudio("GameFailure.mp3");
                await Task.Delay(5000);
                MsgSender.SendImageToAllButtons(new ButtonImage());
            }
            else
            {
                MsgSender.SendImageToAllButtons(new ButtonImage(PatternHelper.GetBytesForPattern(Patterns.AllOneColor), new(0, 0, 255)));
                MsgSender.PlayAudio("GameSuccess.mp3");
                await Task.Delay(5000);
                MsgSender.SendImageToAllButtons(new ButtonImage());
            }
            MsgSender.SendUpdate("Game Ended", true, highScoreAchieved);
            return true;
        }


        public virtual string GetNextSound(List<string> files)
        {
            return BapBasicGameHelper.GetRandomItemFromList(files);
        }

        public virtual void PlayNextWrongSound()
        {
            MsgSender.PlayAudio(GetNextSound(_wrongSounds), true);
        }
        public virtual Task<bool> WrongButtonPressed(bool setupNextMathProblem)
        {
            wrongScore++;
            PlayNextWrongSound();
            if (setupNextMathProblem)
            {
                SetupNextMathProblem(false);
            }
            return Task.FromResult(true);
        }

        public virtual bool RightButtonPressed()
        {
            MsgSender.SendUpdate("Score Updated");
            return true;
        }

        public virtual async Task CorrectMathResultAchievedAsync(bool setupNextMathProblem)
        {
            MsgSender.PlayAudio(GetNextSound(_correctSounds));
            correctScore++;
            if (setupNextMathProblem)
            {
                var nextProblemCreated = await SetupNextMathProblem(true);
                if (!nextProblemCreated)
                {
                    await EndGame("Game Completed");
                }
            }

        }

        public abstract Task<bool> SetupNextMathProblem(bool wasLastPressCorrect);

        public async virtual Task OnCharacterPressed(KeyboardKeyPressedMessage e)
        {
            await NumberPressed(e.KeyValue);
        }

        public virtual async Task<bool> NumberPressed(char digit)
        {
            if (IsGameRunning)
            {
                if (char.ToUpperInvariant(CurrentDigit) == char.ToUpperInvariant(digit))
                {
                    RightButtonPressed();
                    keyboard.OverrideButtonWithImage(digit, new(PatternHelper.GetBytesForPattern(Patterns.CheckMark), new(0, 255, 0)), 500);
                    if (CurrentSpotInAnswerString + 1 == Answer.Length)
                    {
                        await CorrectMathResultAchievedAsync(true);
                    }
                    else
                    {
                        CurrentSpotInAnswerString++;
                    }

                    return true;
                }
                else
                {
                    await WrongButtonPressed(true);
                    keyboard.OverrideButtonWithImage(digit, new(PatternHelper.GetBytesForPattern(Patterns.XOut), new(255, 0, 0)), 500);
                    return false;
                }

            }
            return true;

        }

        public virtual void SetNewAnswer(int answer, bool wasLastPressCorrect)
        {
            SetNewAnswer(answer.ToString().ToCharArray(), wasLastPressCorrect);
        }

        public virtual void SetNewAnswer(char[] answer, bool wasLastPressCorrect)
        {
            Answer = answer;
            CurrentSpotInAnswerString = 0;
        }


        public virtual void Initialize(List<string>? correctSounds = null, List<string>? wrongSounds = null)
        {
            if (correctSounds == null)
            {
                _correctSounds.Add("open_001.wav");
            }
            else
            {
                _correctSounds = correctSounds;
            }
            if (wrongSounds == null)
            {
                _wrongSounds.Add("error_004.wav");
            }
            else
            {
                _wrongSounds = wrongSounds;
            }
        }
        public async virtual Task<bool> Start()
        {
            return await StartGame(120, false);
        }

        public virtual bool SetButtonCountAndButtonList(bool useTopRowForDisplay)
        {
            if (useTopRowForDisplay)
            {
                ButtonCount = LayoutProvider?.CurrentButtonLayout?.ButtonPositions.Where(t => t.RowId != 1)?.Count() ?? MsgSender.GetConnectedButtons().Count;
                nodeIdsToUseForDisplay = LayoutProvider?.CurrentButtonLayout?.ButtonPositions.Where(t => t.RowId == 1).Select(t => t.ButtonId).ToList() ?? new List<string>(); ;
                if (nodeIdsToUseForDisplay.Count < 3)
                {
                    nodeIdsToUseForDisplay = new List<string>();
                    useTopRowForDisplay = false;
                }
            }
            if (!useTopRowForDisplay)
            {
                ButtonCount = MsgSender.GetConnectedButtons().Count;
                nodeIdsToUseForDisplay = new();
            }

            return true;
        }

        public async virtual Task<bool> StartGame(int secondsToRun, bool useTopRowForDisplay, string keyboardCharacters = BasicKeyboardLetters.Numbers)
        {
            if (IsGameRunning)
            {
                return false;
            }
            IsGameRunning = true;
            SecondsToRun = secondsToRun; ;
            MsgSender.SendUpdate("Game Started");
            gameTimer = new PausableTimer(secondsToRun * 1000);
            gameTimer.Elapsed += EndGameEvent;
            gameTimer.AutoReset = false;
            gameTimer.Start();
            gameEndTime = DateTime.Now.AddSeconds(secondsToRun);
            correctScore = 0;
            wrongScore = 0;
            nodeIdsToUseForDisplay = new List<string>();
            SetButtonCountAndButtonList(useTopRowForDisplay);

            keyboard.SetCharacters(keyboardCharacters);
            keyboard.AddNodesToAvoid(nodeIdsToUseForDisplay);
            keyboard.SetPlayDefaultSoundOnPress(true);
            keyboard.Reset();
            if (ButtonCount < 5)
            {
                MsgSender.SendUpdate($"Not enough Buttons. You only have {ButtonCount} buttons but you need at least 5", true);
                await EndGame($"Not enough Buttons.");
                return false;
            }
            if (LayoutProvider?.CurrentButtonLayout == null)
            {
                MsgSender.SendUpdate($"No layout setup. Setup a layout for your buttons", true);
                await EndGame($"No layout setup.");
                return false;
            }
            MsgSender.SendImageToAllButtons(new ButtonImage(PatternHelper.GetBytesForPattern(Patterns.NoPattern), new BapColor(0, 0, 0)));
            MsgSender.SendUpdate("Starting Game", false);
            KeyboardProvider.ShowKeyboard();
            await SetupNextMathProblem(false);
            return true;
        }

        public async Task<bool> ForceEndGame()
        {
            await EndGame("Game Force Ended");
            return true;
        }

        public void Dispose()
        {
            if (subscriptions != null)
            {
                subscriptions.Dispose();
            }

        }
    }
    public class MathGameStatus
    {
        public int CorrectScore { get; set; }
        public int WrongScore { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public int QuestionsRemaining { get; set; }
        public TimeSpan TimeSinceStartOfGame { get; set; }
    }
}

