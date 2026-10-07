using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using Match3Engine.AI;
using Match3Engine.Core;
using Match3Engine.Players;
using Match3Engine.Studio;
using Match3Engine.View;

namespace Match3Engine.Designer
{
    // The screen where the level is made: set the board and the rules, send the simulated
    // players at it, read what it earns and loses, and watch a bot play it.
    public class LevelDesigner : MonoBehaviour
    {
        [Header("Game")]
        [SerializeField] private GameController gameController;
        [SerializeField] private BotPlayer botPlayer;
        [SerializeField] private WatchControls watchControls;
        [SerializeField] private PlayerModelSettings playerModel;
        [SerializeField] private StudioScreen studio;

        [Header("Screens")]
        [SerializeField] private GameObject designerScreen;
        [SerializeField] private UnityEngine.UI.Button backButton;
        [SerializeField] private TMP_Text titleText;

        [Header("Board")]
        [SerializeField] private RectTransform gridArea;
        [SerializeField] private UnityEngine.UI.Button cellTemplate;
        [SerializeField] private Color tileColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private Color holeColor = new Color(0f, 0f, 0f, 0.35f);

        [Header("Level")]
        [SerializeField] private Stepper widthStepper;
        [SerializeField] private Stepper heightStepper;
        [SerializeField] private Stepper coloursStepper;
        [SerializeField] private Stepper movesStepper;

        [Tooltip("One stepper, type and icon per goal the designer can set.")]
        [SerializeField] private Stepper[] goalSteppers;
        [SerializeField] private TileType[] goalTypes;
        [SerializeField] private UnityEngine.UI.Image[] goalIcons;

        [Header("Testing")]
        [SerializeField] private UnityEngine.UI.Button testButton;
        [SerializeField] private TMP_Text testButtonLabel;
        [SerializeField] private UnityEngine.UI.Button watchButton;
        [SerializeField] private UnityEngine.UI.Button playerButton;
        [SerializeField] private TMP_Text playerButtonLabel;
        [SerializeField] private TMP_Text resultsText;
        [SerializeField] private TMP_Text verdictText;

        [Header("Studio")]
        [SerializeField] private UnityEngine.UI.Button publishButton;
        [SerializeField] private TMP_Text publishButtonLabel;
        [SerializeField] private UnityEngine.UI.Button studioButton;

        // The colours a level can use, in the order they are switched on
        private static readonly TileType[] palette =
        {
            TileType.Red, TileType.Blue, TileType.Green, TileType.Yellow, TileType.Pink
        };

        // Which kind of player the Watch button shows
        private static readonly string[] playerNames = { "Weak", "Average", "Strong" };
        private static readonly float[] playerSkills = { 0.25f, 0.5f, 0.8f };
        private int playerIndex = 1;

        // Colour goals go up in fives, so the top of their range is a multiple of five
        private const int maxColourGoal = 95;

        // The level being edited. It lives in memory and is saved to disk after every change.
        private LevelData level;
        private Dictionary<Vector2Int, TileType> obstacles = new Dictionary<Vector2Int, TileType>();
        private List<UnityEngine.UI.Button> cells = new List<UnityEngine.UI.Button>();
        private Sprite cellSprite;
        private Coroutine testRoutine;

        private string SavePath => Path.Combine(Application.persistentDataPath, "designer_level.json");

        private void Awake()
        {
            // Nobody plays while the level is being designed
            botPlayer.enabled = false;
        }

        private void Start()
        {
            cellSprite = cellTemplate.image.sprite;
            cellTemplate.gameObject.SetActive(false);

            LoadLevel();

            widthStepper.Changed += value => OnSizeChanged();
            heightStepper.Changed += value => OnSizeChanged();
            coloursStepper.Changed += value => OnLevelEdited();
            movesStepper.Changed += value => OnLevelEdited();
            foreach (Stepper goal in goalSteppers) goal.Changed += value => OnLevelEdited();

            testButton.onClick.AddListener(TestLevel);
            watchButton.onClick.AddListener(WatchLevel);
            playerButton.onClick.AddListener(NextPlayer);
            backButton.onClick.AddListener(Show);
            publishButton.onClick.AddListener(PublishLevel);
            studioButton.onClick.AddListener(BackToStudio);

            testButtonLabel.text = "Test level\n" + Dollars(studio.Rules.testCost);
            publishButtonLabel.text = "Publish\n" + Dollars(studio.Rules.publishCost);

            for (int i = 0; i < goalIcons.Length; i++)
            {
                goalIcons[i].sprite = gameController.GetSpriteForType(goalTypes[i]);
                goalIcons[i].preserveAspect = true;
            }

            // The grid is sized from its area, so the layout has to be worked out before I build it
            Canvas.ForceUpdateCanvases();

            ShowLevel();
            ShowPlayerChoice();
            ShowMessage("Design a level, then test it.");
        }

        // ---------- Loading and saving ----------

        private void LoadLevel()
        {
            level = ScriptableObject.CreateInstance<LevelData>();

            // Starting from the last design if there is one, otherwise from the scene's level
            string json = File.Exists(SavePath) ? File.ReadAllText(SavePath) : JsonUtility.ToJson(gameController.level);
            JsonUtility.FromJsonOverwrite(json, level);

            obstacles.Clear();
            if (level.obstacles != null)
            {
                foreach (LevelObstacle obstacle in level.obstacles)
                {
                    if (obstacle.type == TileType.Crate || obstacle.type == TileType.Hole)
                        obstacles[obstacle.position] = obstacle.type;
                }
            }
        }

        private void SaveLevel()
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(level));
        }

        // ---------- Showing the level on the controls ----------

        private void ShowLevel()
        {
            widthStepper.SetValue(level.width);
            heightStepper.SetValue(level.height);
            movesStepper.SetValue(level.moveLimit);
            coloursStepper.SetValue(level.availableTileTypes != null ? level.availableTileTypes.Length : palette.Length);

            for (int i = 0; i < goalSteppers.Length; i++)
            {
                int amount = 0;
                if (level.goals != null)
                {
                    foreach (LevelGoal goal in level.goals)
                        if (goal.type == goalTypes[i]) amount += goal.amount;
                }

                goalSteppers[i].SetRange(0, maxColourGoal);
                goalSteppers[i].SetValue(amount);
            }

            // Reading everything back makes the level agree with what the controls allow
            ReadLevelFromControls();
            RebuildGrid();
        }

        // ---------- Reacting to edits ----------

        private void OnSizeChanged()
        {
            ReadLevelFromControls();
            RebuildGrid();
            LevelChanged();
        }

        private void OnLevelEdited()
        {
            ReadLevelFromControls();
            LevelChanged();
        }

        private void LevelChanged()
        {
            SaveLevel();

            // Old numbers describe a level that no longer exists
            StopTest();
            ShowMessage("Level changed. Test it again.");
        }

        // Copies the controls into the level, keeping everything inside what a level allows
        private void ReadLevelFromControls()
        {
            level.width = widthStepper.Value;
            level.height = heightStepper.Value;
            level.moveLimit = movesStepper.Value;

            level.availableTileTypes = new TileType[coloursStepper.Value];
            System.Array.Copy(palette, level.availableTileTypes, coloursStepper.Value);

            // Obstacles that fell off a smaller board are dropped
            List<Vector2Int> outside = new List<Vector2Int>();
            foreach (Vector2Int position in obstacles.Keys)
            {
                if (position.x >= level.width || position.y >= level.height) outside.Add(position);
            }
            foreach (Vector2Int position in outside) obstacles.Remove(position);

            int crateCount = 0;
            List<LevelObstacle> obstacleList = new List<LevelObstacle>();
            foreach (KeyValuePair<Vector2Int, TileType> obstacle in obstacles)
            {
                obstacleList.Add(new LevelObstacle { position = obstacle.Key, type = obstacle.Value });
                if (obstacle.Value == TileType.Crate) crateCount++;
            }
            level.obstacles = obstacleList.ToArray();

            // A goal can only ask for something the level actually has
            List<LevelGoal> goals = new List<LevelGoal>();
            for (int i = 0; i < goalSteppers.Length; i++)
            {
                TileType type = goalTypes[i];

                if (type == TileType.Crate) goalSteppers[i].SetRange(0, crateCount);
                else goalSteppers[i].SetRange(0, System.Array.IndexOf(level.availableTileTypes, type) >= 0 ? maxColourGoal : 0);

                if (goalSteppers[i].Value > 0) goals.Add(new LevelGoal { type = type, amount = goalSteppers[i].Value });
            }
            level.goals = goals.ToArray();
        }

        // ---------- The board grid ----------

        private void RebuildGrid()
        {
            foreach (UnityEngine.UI.Button cell in cells) Destroy(cell.gameObject);
            cells.Clear();

            float cellSize = Mathf.Min(gridArea.rect.width / level.width, gridArea.rect.height / level.height);

            for (int x = 0; x < level.width; x++)
            {
                for (int y = 0; y < level.height; y++)
                {
                    Vector2Int position = new Vector2Int(x, y);

                    UnityEngine.UI.Button cell = Instantiate(cellTemplate, gridArea);
                    cell.gameObject.SetActive(true);
                    cell.name = "Cell " + x + "," + y;

                    RectTransform rect = (RectTransform)cell.transform;
                    rect.sizeDelta = new Vector2(cellSize - 6f, cellSize - 6f);
                    rect.anchoredPosition = new Vector2(
                        (x - (level.width - 1) / 2f) * cellSize,
                        (y - (level.height - 1) / 2f) * cellSize);

                    cell.onClick.AddListener(() => CycleCell(position, cell));
                    ShowCell(position, cell);

                    cells.Add(cell);
                }
            }
        }

        // Tapping a slot goes round: tile, crate, hole, and back to tile
        private void CycleCell(Vector2Int position, UnityEngine.UI.Button cell)
        {
            TileType current;
            if (!obstacles.TryGetValue(position, out current)) obstacles[position] = TileType.Crate;
            else if (current == TileType.Crate) obstacles[position] = TileType.Hole;
            else obstacles.Remove(position);

            ShowCell(position, cell);
            OnLevelEdited();
        }

        private void ShowCell(Vector2Int position, UnityEngine.UI.Button cell)
        {
            TileType type;
            obstacles.TryGetValue(position, out type);

            if (type == TileType.Crate)
            {
                cell.image.sprite = gameController.GetSpriteForType(TileType.Crate);
                cell.image.type = UnityEngine.UI.Image.Type.Simple;
                cell.image.color = Color.white;
            }
            else
            {
                cell.image.sprite = cellSprite;
                cell.image.type = UnityEngine.UI.Image.Type.Sliced;
                cell.image.color = type == TileType.Hole ? holeColor : tileColor;
            }
        }

        // ---------- Testing ----------

        private void TestLevel()
        {
            if (!HasGoal()) return;

            // Playtesting is not free, so a level cannot be tuned by endless trial and error
            if (!studio.TrySpend(studio.Rules.testCost))
            {
                ShowMessage("Not enough money to test.");
                return;
            }
            ShowMoney();

            StopTest();
            testRoutine = StartCoroutine(TestRoutine());
        }

        private bool HasGoal()
        {
            if (level.goals.Length > 0) return true;

            ShowMessage("Add at least one goal first.");
            return false;
        }

        // Runs the whole population through the level a few players per frame,
        // so the screen keeps responding while it works
        private IEnumerator TestRoutine()
        {
            const int seed = 1;

            List<PlayerProfile> population = PlayerPopulation.Generate(playerModel, seed);
            LevelReport report = new LevelReport();
            System.Diagnostics.Stopwatch frameTimer = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < population.Count; i++)
            {
                LevelAnalyzer.SimulatePlayer(level, population[i], playerModel, seed + i * 1000, report);

                if (frameTimer.ElapsedMilliseconds > 12)
                {
                    ShowMessage("Testing... " + (i * 100 / population.Count) + "%");
                    yield return null;
                    frameTimer.Restart();
                }
            }

            testRoutine = null;
            ShowReport(report);
        }

        private void StopTest()
        {
            if (testRoutine == null) return;

            StopCoroutine(testRoutine);
            testRoutine = null;
        }

        private void ShowReport(LevelReport report)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;

            resultsText.text =
                "First-try pass: " + (report.FirstAttemptPassRate * 100f).ToString("0", culture) + "%\n" +
                "Attempts to pass: " + report.AverageAttemptsToPass.ToString("0.0", culture) + "\n" +
                "Revenue per 100 players: " + report.RevenuePer100Players.ToString("0.0", culture) + "\n" +
                "Players lost: " + (report.QuitRate * 100f).ToString("0", culture) + "%";

            verdictText.text = LevelVerdict.Describe(LevelVerdict.Rate(report));
        }

        private void ShowMessage(string message)
        {
            resultsText.text = "";
            verdictText.text = message;
        }

        // ---------- Watching ----------

        private void NextPlayer()
        {
            playerIndex = (playerIndex + 1) % playerNames.Length;
            ShowPlayerChoice();
        }

        private void ShowPlayerChoice()
        {
            playerButtonLabel.text = "Player:\n" + playerNames[playerIndex];
        }

        private void WatchLevel()
        {
            if (!HasGoal()) return;

            StopTest();

            gameController.StartLevel(level);
            botPlayer.Configure(playerModel.strategy, playerSkills[playerIndex]);
            botPlayer.enabled = true;
            watchControls.Refresh();

            designerScreen.SetActive(false);
        }

        // ---------- Studio ----------

        private void PublishLevel()
        {
            if (!HasGoal()) return;

            string blocker = studio.PublishBlocker();
            if (blocker != null)
            {
                ShowMessage(blocker);
                return;
            }

            StopTest();
            studio.Publish(level);
            ShowMessage("Design a level, then test it.");

            studio.Show();
        }

        private void BackToStudio()
        {
            StopTest();
            studio.Show();
        }

        private void ShowMoney()
        {
            titleText.text = "Level Designer   " + Dollars(studio.Money);
        }

        private static string Dollars(float amount)
        {
            return "$" + amount.ToString("0.#", CultureInfo.InvariantCulture);
        }

        // ---------- Showing and hiding this screen ----------

        // Opens the designer, from the studio or back from watching
        public void Show()
        {
            botPlayer.enabled = false;
            designerScreen.SetActive(true);

            // The grid is sized from its area, which only has a size once the screen is showing
            Canvas.ForceUpdateCanvases();
            RebuildGrid();
            ShowMoney();
        }

        public void Hide()
        {
            designerScreen.SetActive(false);
        }
    }
}
