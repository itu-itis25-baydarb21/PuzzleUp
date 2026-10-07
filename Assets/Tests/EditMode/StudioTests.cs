using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Match3Engine.Core;
using Match3Engine.Players;
using Match3Engine.Studio;

namespace Match3Engine.Tests
{
    public class StudioTests
    {
        private List<Object> created = new List<Object>();
        private PlayerModelSettings model;
        private StudioSettings rules;

        [SetUp]
        public void CreateSettings()
        {
            model = ScriptableObject.CreateInstance<PlayerModelSettings>();
            rules = ScriptableObject.CreateInstance<StudioSettings>();
            created.Add(model);
            created.Add(rules);
        }

        [TearDown]
        public void DestroyCreated()
        {
            foreach (Object item in created) Object.DestroyImmediate(item);
            created.Clear();
        }

        // 8x8, five colours, collect 20 red and 20 blue
        private LevelData MakeLevel(int moveLimit)
        {
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 8;
            level.height = 8;
            level.availableTileTypes = new[] { TileType.Red, TileType.Blue, TileType.Green, TileType.Yellow, TileType.Pink };
            level.moveLimit = moveLimit;
            level.goals = new[]
            {
                new LevelGoal { type = TileType.Red, amount = 20 },
                new LevelGoal { type = TileType.Blue, amount = 20 }
            };

            created.Add(level);
            return level;
        }

        // ---------- A cohort going through a game ----------

        [Test]
        public void EachLevel_OnlySeesThePlayersWhoPassedTheOneBefore()
        {
            List<LevelData> game = new List<LevelData> { MakeLevel(12), MakeLevel(11), MakeLevel(10) };

            CohortReport report = CohortSimulation.Run(game, model, 120, 3);

            Assert.That(report.started, Is.EqualTo(120));
            Assert.That(report.levels, Has.Count.EqualTo(3));
            Assert.That(report.levels[0].players, Is.EqualTo(120));

            for (int i = 0; i < report.levels.Count; i++)
            {
                LevelReport level = report.levels[i];
                Assert.That(level.passed + level.quit, Is.EqualTo(level.players), "Level " + (i + 1));

                if (i > 0) Assert.That(level.players, Is.EqualTo(report.levels[i - 1].passed), "Level " + (i + 1));
            }

            Assert.That(report.finished, Is.EqualTo(report.levels[2].passed));
            Assert.That(report.LevelsPlayed, Is.EqualTo(report.levels[0].players + report.levels[1].players + report.levels[2].players));
        }

        [Test]
        public void SameCohort_PlaysOutTheSame()
        {
            List<LevelData> game = new List<LevelData> { MakeLevel(11), MakeLevel(11) };

            CohortReport first = CohortSimulation.Run(game, model, 80, 5);
            CohortReport second = CohortSimulation.Run(game, model, 80, 5);

            Assert.That(second.finished, Is.EqualTo(first.finished));
            Assert.That(second.Revenue, Is.EqualTo(first.Revenue));
            Assert.That(second.levels[1].progressOnFail, Is.EqualTo(first.levels[1].progressOnFail));
        }

        [Test]
        public void SteppedRun_MatchesTheOneCallRun()
        {
            List<LevelData> game = new List<LevelData> { MakeLevel(11), MakeLevel(11) };

            CohortSimulation stepped = new CohortSimulation(game, model, 60, 8);
            int steps = 0;
            while (!stepped.IsDone)
            {
                stepped.SimulateNextPlayer();
                steps++;
            }

            CohortReport whole = CohortSimulation.Run(game, model, 60, 8);

            Assert.That(steps, Is.EqualTo(60));
            Assert.That(stepped.Report.finished, Is.EqualTo(whole.finished));
            Assert.That(stepped.Report.Revenue, Is.EqualTo(whole.Revenue));
        }

        [Test]
        public void CarriedFrustration_CostsPlayersOverARunOfHardLevels()
        {
            // The same three tough levels, with players who forget everything on a win
            // and players who forget nothing. Remembering must lose more of them.
            List<LevelData> game = new List<LevelData> { MakeLevel(10), MakeLevel(10), MakeLevel(10) };

            model.frustrationKeptAfterWin = 0f;
            int forgetting = CohortSimulation.Run(game, model, 200, 2).finished;

            model.frustrationKeptAfterWin = 1f;
            int remembering = CohortSimulation.Run(game, model, 200, 2).finished;

            Assert.That(remembering, Is.LessThan(forgetting), "Remembering " + remembering + " vs forgetting " + forgetting);
        }

        // ---------- The studio's books ----------

        [Test]
        public void NewStudio_StartsWithTheRulesMoneyAndPlayers()
        {
            StudioState state = StudioState.CreateNew(rules);

            Assert.That(state.money, Is.EqualTo(rules.startingMoney));
            Assert.That(state.cohortSize, Is.EqualTo(rules.startingCohort));
            Assert.That(state.month, Is.EqualTo(1));
            Assert.That(state.levels, Is.Empty);
            Assert.That(state.hasRun, Is.False);
        }

        [Test]
        public void Spending_IsRefusedWithoutEnoughMoney()
        {
            StudioState state = StudioState.CreateNew(rules);
            state.money = 5f;

            Assert.That(state.TrySpend(3f), Is.True);
            Assert.That(state.money, Is.EqualTo(2f));

            Assert.That(state.TrySpend(3f), Is.False);
            Assert.That(state.money, Is.EqualTo(2f), "A refused purchase must not take any money.");
        }

        [Test]
        public void Publishing_CostsMoney_AndAddsTheLevel()
        {
            StudioState state = StudioState.CreateNew(rules);

            Assert.That(state.Publish("{}", rules), Is.True);

            Assert.That(state.levels, Has.Count.EqualTo(1));
            Assert.That(state.money, Is.EqualTo(rules.startingMoney - rules.publishCost));
        }

        [Test]
        public void Publishing_IsBlockedWhenBroke_OrWhenTheGameIsFull()
        {
            StudioState broke = StudioState.CreateNew(rules);
            broke.money = rules.publishCost - 1f;

            Assert.That(broke.PublishBlocker(rules), Is.Not.Null);
            Assert.That(broke.Publish("{}", rules), Is.False);
            Assert.That(broke.levels, Is.Empty);

            StudioState full = StudioState.CreateNew(rules);
            full.money = 100000f;
            for (int i = 0; i < rules.maxLevels; i++) Assert.That(full.Publish("{}", rules), Is.True);

            Assert.That(full.PublishBlocker(rules), Is.Not.Null);
            Assert.That(full.Publish("{}", rules), Is.False);
            Assert.That(full.levels, Has.Count.EqualTo(rules.maxLevels));
        }

        [Test]
        public void AMonth_PaysSalesAndAds_AndMovesTheCalendar()
        {
            StudioState state = StudioState.CreateNew(rules);
            float before = state.money;

            CohortReport report = new CohortReport { started = 200, finished = 150 };
            report.levels.Add(new LevelReport { players = 200, passed = 170, quit = 30, revenue = 12f });
            report.levels.Add(new LevelReport { players = 170, passed = 150, quit = 20, revenue = 5f });

            state.ApplyMonth(report, rules);

            float ads = (200 + 170) * rules.adIncomePerLevelPlayed;

            Assert.That(state.month, Is.EqualTo(2));
            Assert.That(state.hasRun, Is.True);
            Assert.That(state.lastSales, Is.EqualTo(17f));
            Assert.That(state.lastAdIncome, Is.EqualTo(ads).Within(0.001f));
            Assert.That(state.money, Is.EqualTo(before + 17f + ads).Within(0.001f));
            Assert.That(state.lastStats, Has.Count.EqualTo(2));
            Assert.That(state.lastStats[1].players, Is.EqualTo(170));
            Assert.That(state.cohortSize, Is.EqualTo(rules.NextCohort(200, 0.75f)));
        }

        [Test]
        public void KeepingPlayers_BringsMoreNextMonth_AndLosingThemBringsFewer()
        {
            Assert.That(rules.NextCohort(200, 1f), Is.GreaterThan(200));
            Assert.That(rules.NextCohort(200, 0f), Is.LessThan(200));
            Assert.That(rules.NextCohort(200, 0.9f), Is.GreaterThan(rules.NextCohort(200, 0.3f)));

            // And never outside the limits, however long a streak lasts
            Assert.That(rules.NextCohort(rules.maxCohort, 1f), Is.EqualTo(rules.maxCohort));
            Assert.That(rules.NextCohort(rules.minCohort, 0f), Is.EqualTo(rules.minCohort));
        }

        [Test]
        public void StudioState_SurvivesBeingSavedAndLoaded()
        {
            StudioState state = StudioState.CreateNew(rules);
            state.Publish(JsonUtility.ToJson(MakeLevel(11)), rules);
            state.month = 4;

            StudioState loaded = JsonUtility.FromJson<StudioState>(JsonUtility.ToJson(state));

            Assert.That(loaded.money, Is.EqualTo(state.money));
            Assert.That(loaded.month, Is.EqualTo(4));
            Assert.That(loaded.levels, Is.EqualTo(state.levels));

            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            created.Add(level);
            JsonUtility.FromJsonOverwrite(loaded.levels[0], level);

            Assert.That(level.moveLimit, Is.EqualTo(11));
            Assert.That(level.goals, Has.Length.EqualTo(2));
        }
    }
}
