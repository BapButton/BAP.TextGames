using BAP.Types;
using BAP.TextGames.Components;
using BAP.KeyBoardGameBase;
using static MudBlazor.Colors;
using BAP.Helpers;

namespace BAP.Web.Games
{
    public class VocabPerson
    {
        public string PersonName { get; set; } = "";
        public string Id { get; set; } = "";
        public VocabWords VocabWords { get; set; } = new();
    }

    public class VocabWords
    {
        public bool IsSpanish { get; set; } = true;
        public List<string> SavedWords { get; set; } = new();
        public DateTime DateSaved { get; set; }
    }

    public class VocabGame : KeyBoardGameBase.KeyboardGameBase, IBapGame, IDisposable
    {
        public List<VocabPerson> VocabPeople { get; set; } = new();
        private IBapMessageSender MessageSender { get; set; }

        public string SelectedPersonId { get; set; } = "";
        public VocabWords SavedVocab
        {
            get
            {
                return VocabPeople.FirstOrDefault(t => t.Id == SelectedPersonId)?.VocabWords ?? new();
            }
        }

        public Queue<int> WordOrder { get; set; } = new Queue<int>();
        public int CurrentWordNumber { get; set; } = -1;
        public override ILogger _logger { get; set; }
        public override IGameDataSaver DbSaver { get; set; }
        public string VocabListSaveKey => "VocabPeopleAndWords";
        internal bool useTestingButtons { get; set; } = true;
        internal int PlaylistCount { get; set; }
        internal int PlaylistTotalSeconds { get; set; }
        internal string PlaylistLengthDisplay
        {
            get
            {
                return TimeSpan.FromSeconds(PlaylistTotalSeconds).ToString(@"hh\:mm\:ss");
            }

        }

        public override Score GenerateScoreWithCurrentData()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(SecondsToRun);

            decimal normalizedScore = 0;
            if (SecondsToRun > 0 && correctScore + wrongScore != 0)
            {
                normalizedScore = ((decimal)correctScore - ((decimal)wrongScore * 2.0M)) / ((decimal)SecondsToRun / 30.0M);
            }
            Score score = new()
            {
                DifficultyId = "",
                DifficultyDescription = "",
                ScoreData = CurrentWordNumber.ToString(),
                NormalizedScore = normalizedScore,
                ScoreDescription = $"",
            };
            return score;
        }

        internal async Task RefreshSavedVocab()
        {
            VocabPeople = await GetSavedVocabPeople();
            MessageSender.SendUpdate("Vocab people or list updated", pageRefreshRecommended: true);
        }

        public VocabGame(IGameDataSaver<VocabGame> gameDataSaver, IKeyboardProvider keyboardProvider, IGameProvider gameHandler, ILayoutProvider layoutProvider, ILogger<VocabGame> logger, ISubscriber<KeyboardKeyPressedMessage> keyPressed, IBapMessageSender messageSender) : base(keyboardProvider, layoutProvider, messageSender, keyPressed)
        {
            _logger = logger;
            DbSaver = gameDataSaver;
            MessageSender = messageSender;
            SelectedPersonId = VocabPeople.FirstOrDefault()?.Id ?? "";
        }

        public async Task InitializeAsync()
        {
            await RefreshSavedVocab(); ;
            if (SelectedPersonId == "")
            {
                SelectedPersonId = VocabPeople.FirstOrDefault()?.Id ?? "";
            }
        }


        public override async Task<bool> WrongButtonPressed(bool setupNextMathProblem)
        {
            return await base.WrongButtonPressed(setupNextMathProblem);
        }

        public async Task AddPerson(string personName)
        {
            var people = await GetSavedVocabPeople();
            people.Add(new VocabPerson() { Id = Guid.NewGuid().ToString(), PersonName = personName });

            await UpdateAllVocabPeople(people);
        }

        public async Task RenamePerson(string personId, string personName)
        {
            var people = await GetSavedVocabPeople();
            var person = people.FirstOrDefault(t => t.Id == personId);
            if (person != null)
            {
                person.PersonName = personName;
                await UpdateAllVocabPeople(people);
            }

        }

        public async Task DeletePerson(string personId)
        {
            var people = await GetSavedVocabPeople();
            var person = people.FirstOrDefault(t => t.Id == personId);
            if (person != null)
            {
                people.Remove(person);
                await UpdateAllVocabPeople(people);
                if (SelectedPersonId == personId)
                {
                    SelectedPersonId = people.FirstOrDefault()?.Id ?? "";
                }
            }
        }


        public void ReplayCurrentWord()
        {
            if (SavedVocab.IsSpanish)
            {
                MsgSender.PlayTTS($"y {SavedVocab.SavedWords[CurrentWordNumber]}", true, TTSLanguage.Spanish);
            }
            else
            {
                MsgSender.PlayTTS(SavedVocab.SavedWords[CurrentWordNumber], true);
            }
        }


        public async Task<List<VocabPerson>> GetSavedVocabPeople()
        {
            try
            {
                return (await DbSaver.GetGameStorage<List<VocabPerson>>()) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failure deserializing Vocab People - starting Over. Error was: {ex.Message}");
            }
            return new();

        }
        public async Task UpdateAllVocabPeople(List<VocabPerson> vocabPeople)
        {
            await DbSaver.UpdateGameStorage(vocabPeople);
            VocabPeople = await GetSavedVocabPeople();
            if (!VocabPeople.Any(t => t.Id == SelectedPersonId))
            {
                SelectedPersonId = "";
            }
            if (SelectedPersonId == "")
            {
                SelectedPersonId = VocabPeople.FirstOrDefault()?.Id ?? "";
            }

            MessageSender.SendUpdate("People or Vocab List Updated");
        }
        public async Task UpdateCurrentVocab(VocabWords vocabWords)
        {
            var people = await GetSavedVocabPeople();
            var person = people.FirstOrDefault(t => t.Id == SelectedPersonId);
            if (person != null)
            {
                person.VocabWords = vocabWords;
                await UpdateAllVocabPeople(people);
            }
            else
            {
                _logger.LogWarning("Could not find current person in the saved People list");
            }

        }


        public async Task<bool> SaveNewVocabWords(string seperatedWordsAsString, bool isSpanish)
        {
            seperatedWordsAsString = seperatedWordsAsString.ReplaceLineEndings("\n");
            char splitCharacter = ',';
            List<char> possibleSpittingCharacters = new() { ',', ';', '\n' };
            foreach (char character in possibleSpittingCharacters)
            {
                if (seperatedWordsAsString.Split(character).Count() > 3)
                {
                    splitCharacter = character;
                    break;
                }
            }
            VocabWords updatedWords = new()
            {
                IsSpanish = isSpanish,
                DateSaved = DateTime.Now,
                SavedWords = seperatedWordsAsString.Split(splitCharacter).Select(t => t.Trim()).ToList(),
                //SpotifyToken = SavedVocab.SpotifyToken
            };
            for (int i = 0; i < updatedWords.SavedWords.Count; i++)
            {
                updatedWords.SavedWords[i] = new string(updatedWords.SavedWords[i].Where(t => !char.IsPunctuation(t) && !char.IsDigit(t) && !char.IsWhiteSpace(t)).ToArray());
            }
            await UpdateCurrentVocab(updatedWords);
            return true;
        }


        public override async Task<bool> SetupNextMathProblem(bool wasLastPressCorrect)
        {
            CurrentSpotInAnswerString = 0;
            if (!wasLastPressCorrect && CurrentWordNumber >= 0)
            {
                WordOrder.Enqueue(CurrentWordNumber);
            }
            bool moreWords = WordOrder.TryDequeue(out int nextWord);
            if (!moreWords)
            {
                return false;
            }
            CurrentWordNumber = nextWord;
            MsgSender.SendUpdate("Next vocab word selected");
            Answer = SavedVocab.SavedWords[CurrentWordNumber].ToCharArray();
            await Task.Delay(1000);
            if (SavedVocab.IsSpanish)
            {
                MsgSender.PlayTTS(SavedVocab.SavedWords[CurrentWordNumber], true, TTSLanguage.Spanish);
            }
            else
            {
                MsgSender.PlayTTS(SavedVocab.SavedWords[CurrentWordNumber], true);
            }

            return await Task.FromResult(true);
        }





        public override async Task<bool> Start()
        {
            if (IsGameRunning)
            {
                return false;
            }
            WordOrder.Clear();
            List<int> wordNumbers = Enumerable.Range(0, SavedVocab.SavedWords.Count).ToList();
            wordNumbers.Shuffle();
            foreach (var item in wordNumbers)
            {
                WordOrder.Enqueue(item);
            }
            string keyboardCharacters = SavedVocab.IsSpanish ? BasicKeyboardLetters.SpanishLowerCase : BasicKeyboardLetters.EnglishLowerCaseLetters;
            await base.StartGame(1200, false, keyboardCharacters);

            return true;
        }
    }
}
