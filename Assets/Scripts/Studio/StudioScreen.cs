using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using Match3Engine.Core;
using Match3Engine.Designer;
using Match3Engine.Players;

namespace Match3Engine.Studio
{
    // The studio's home screen: money, the levels in the game, and how last month went.
    // From here the player designs a new level or lets a month of new players through the game.
    public class StudioScreen : MonoBehaviour
    {
        [Header("Rules")]
        [SerializeField] private StudioSettings rules;
        [SerializeField] private PlayerModelSettings playerModel;

        [Header("Screens")]
        [SerializeField] private GameObject studioScreen;
        [SerializeField] private LevelDesigner designer;

        [Header("Display")]
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text monthText;
        [SerializeField] private TMP_Text playersText;
        [SerializeField] private TMP_Text levelsText;
        [SerializeField] private TMP_Text summaryText;

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button designButton;
        [SerializeField] private UnityEngine.UI.Button runButton;
        [SerializeField] private UnityEngine.UI.Button resetButton;

        public StudioSettings Rules => rules;
        public float Money => state.money;

        private StudioState state;
        private Coroutine monthRoutine;

        private static readonly CultureInfo culture = CultureInfo.InvariantCulture;

        private string SavePath => Path.Combine(Application.persistentDataPath, "studio.json");

        private void Awake()
        {
            // Loaded here so the designer can ask about money from its own Start
            Load();
        }

        private void Start()
        {
            designButton.onClick.AddListener(OpenDesigner);
            runButton.onClick.AddListener(RunMonth);
            resetButton.onClick.AddListener(ResetStudio);

            Show();
        }

        // ---------- Saving ----------

        private void Load()
        {
            state = null;
            if (File.Exists(SavePath)) state = JsonUtility.FromJson<StudioState>(File.ReadAllText(SavePath));
            if (state == null) state = StudioState.CreateNew(rules);
        }

        private void Save()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(state));
        }

        // ---------- What the designer asks for ----------

        public bool TrySpend(float cost)
        {
            if (!state.TrySpend(cost)) return false;

            Save();
            return true;
        }

        // Why a level cannot be published right now, or null if it can
        public string PublishBlocker()
        {
            return state.PublishBlocker(rules);
        }

        // Adds a copy of the level to the end of the game
        public bool Publish(LevelData level)
        {
            if (!state.Publish(JsonUtility.ToJson(level), rules)) return false;

            Save();
            return true;
        }

        // ---------- Screens ----------

        public void Show()
        {
            designer.Hide();
            studioScreen.SetActive(true);
            Refresh();
        }

        private void OpenDesigner()
        {
            if (monthRoutine != null) return;

            studioScreen.SetActive(false);
            designer.Show();
        }

        private void ResetStudio()
        {
            if (monthRoutine != null) return;

            state = StudioState.CreateNew(rules);
            Save();
            Refresh();
        }

        // ---------- Running a month ----------

        private void RunMonth()
        {
            if (monthRoutine != null || state.levels.Count == 0) return;

            monthRoutine = StartCoroutine(MonthRoutine());
        }

        // A month of new players goes through every level in order. It is spread over frames
        // because a long game with a big crowd takes a while.
        private IEnumerator MonthRoutine()
        {
            SetButtons(false);

            List<LevelData> levels = new List<LevelData>();
            foreach (string json in state.levels)
            {
                LevelData level = ScriptableObject.CreateInstance<LevelData>();
                JsonUtility.FromJsonOverwrite(json, level);
                levels.Add(level);
            }

            // Seeded by the month, so a given month of a given game always plays out the same
            CohortSimulation cohort = new CohortSimulation(levels, playerModel, state.cohortSize, state.month * 7919);
            System.Diagnostics.Stopwatch frameTimer = System.Diagnostics.Stopwatch.StartNew();

            while (!cohort.IsDone)
            {
                cohort.SimulateNextPlayer();

                if (frameTimer.ElapsedMilliseconds > 12)
                {
                    summaryText.text = "Month " + state.month + " in progress... " + (cohort.PlayersDone * 100 / cohort.PlayerCount) + "%";
                    yield return null;
                    frameTimer.Restart();
                }
            }

            foreach (LevelData level in levels) Destroy(level);

            state.ApplyMonth(cohort.Report, rules);
            Save();

            monthRoutine = null;
            Refresh();
        }

        // ---------- Display ----------

        private void SetButtons(bool on)
        {
            designButton.interactable = on;
            runButton.interactable = on && state.levels.Count > 0;
            resetButton.interactable = on;
        }

        private void Refresh()
        {
            moneyText.text = "Money\n" + Dollars(state.money);
            monthText.text = "Month\n" + state.month;
            playersText.text = "New players\n" + state.cohortSize;

            levelsText.text = DescribeLevels();
            summaryText.text = DescribeLastMonth();

            SetButtons(monthRoutine == null);
        }

        private string DescribeLevels()
        {
            if (state.levels.Count == 0) return "Your game has no levels yet.\nDesign one and publish it.";

            // Columns are placed with <pos> so the numbers line up whatever their width
            StringBuilder text = new StringBuilder();
            text.Append("<b>Level<pos=22%>Players<pos=44%>Passed<pos=64%>Quit<pos=80%>Sales</b>\n");

            for (int i = 0; i < state.levels.Count; i++)
            {
                text.Append(i + 1);

                if (i < state.lastStats.Count)
                {
                    LevelStats stats = state.lastStats[i];
                    text.Append("<pos=22%>").Append(stats.players);
                    text.Append("<pos=44%>").Append(stats.passed);
                    text.Append("<pos=64%>").Append(stats.quit);
                    text.Append("<pos=80%>").Append(Dollars(stats.revenue));
                }
                else
                {
                    text.Append("<pos=22%>new");
                }

                text.Append('\n');
            }

            return text.ToString();
        }

        private string DescribeLastMonth()
        {
            if (!state.hasRun)
            {
                return state.levels.Count == 0
                    ? "Start by designing your first level."
                    : "Run a month to see how players do.";
            }

            return "Last month: " + state.lastStarted + " players started, " + state.lastFinished + " finished the game.\n"
                + "Earned " + Dollars(state.lastSales) + " from sales and " + Dollars(state.lastAdIncome) + " from ads.";
        }

        private static string Dollars(float amount)
        {
            return "$" + amount.ToString("0.#", culture);
        }
    }
}
