using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Match3Engine.AI;
using Match3Engine.Core;
using Match3Engine.Players;
using Match3Engine.Simulation;

namespace Match3Engine.Tests
{
    public class PlayerModelTests
    {
        private List<Object> created = new List<Object>();
        private PlayerModelSettings settings;

        [SetUp]
        public void CreateSettings()
        {
            settings = ScriptableObject.CreateInstance<PlayerModelSettings>();
            settings.populationSize = 150;
            created.Add(settings);
        }

        [TearDown]
        public void DestroyCreated()
        {
            foreach (Object item in created) Object.DestroyImmediate(item);
            created.Clear();
        }

        // 8x8, five colours, collect the same amount of red and of blue
        private LevelData MakeLevel(int moveLimit, int goalAmount)
        {
            LevelData level = ScriptableObject.CreateInstance<LevelData>();
            level.width = 8;
            level.height = 8;
            level.availableTileTypes = new[] { TileType.Red, TileType.Blue, TileType.Green, TileType.Yellow, TileType.Pink };
            level.moveLimit = moveLimit;
            level.goals = new[]
            {
                new LevelGoal { type = TileType.Red, amount = goalAmount },
                new LevelGoal { type = TileType.Blue, amount = goalAmount }
            };

            created.Add(level);
            return level;
        }

        private LevelData EasyLevel() { return MakeLevel(25, 20); }
        private LevelData MediumLevel() { return MakeLevel(11, 20); }
        private LevelData HardLevel() { return MakeLevel(7, 30); }

        [Test]
        public void ExtraMoves_BringALostGameBack()
        {
            GameSimulation game = new GameSimulation(HardLevel(), 3);
            BatchRunner.PlayToEnd(game, BotFactory.Create(BotStrategy.Random, 3));
            Assert.That(game.Progress.State, Is.EqualTo(LevelState.Lost), "Test setup: this game should be lost.");

            Assert.That(game.AddMoves(5), Is.True);

            Assert.That(game.Progress.State, Is.EqualTo(LevelState.Playing));
            Assert.That(game.Progress.MovesLeft, Is.EqualTo(5));
            Assert.That(game.GetValidMoves(), Is.Not.Empty);
        }

        [Test]
        public void ExtraMoves_AreRefusedOnceTheLevelIsWon()
        {
            GameSimulation game = new GameSimulation(EasyLevel(), 3);
            BatchRunner.PlayToEnd(game, BotFactory.Create(BotStrategy.GoalAware, 3));
            Assert.That(game.Progress.State, Is.EqualTo(LevelState.Won), "Test setup: this game should be won.");

            int movesLeft = game.Progress.MovesLeft;

            Assert.That(game.AddMoves(5), Is.False);
            Assert.That(game.Progress.MovesLeft, Is.EqualTo(movesLeft));
        }

        [Test]
        public void Population_FollowsTheSettings()
        {
            settings.populationSize = 2000;
            List<PlayerProfile> players = PlayerPopulation.Generate(settings, 5);

            float skillTotal = 0f;
            int sometimes = 0, often = 0;
            foreach (PlayerProfile player in players)
            {
                Assert.That(player.skill, Is.InRange(0f, 1f));
                Assert.That(player.patience, Is.InRange(settings.patienceMin, settings.patienceMax));

                skillTotal += player.skill;
                if (player.spender == SpenderType.Sometimes) sometimes++;
                if (player.spender == SpenderType.Often) often++;
            }

            Assert.That(players, Has.Count.EqualTo(2000));
            Assert.That(skillTotal / players.Count, Is.EqualTo(settings.skillAverage).Within(0.03f));
            Assert.That((float)sometimes / players.Count, Is.EqualTo(settings.shareSometimes).Within(0.04f));
            Assert.That((float)often / players.Count, Is.EqualTo(settings.shareOften).Within(0.04f));
        }

        [Test]
        public void SameSeed_GivesTheSameReport()
        {
            LevelData level = MediumLevel();

            LevelReport first = LevelAnalyzer.Run(level, settings, 9);
            LevelReport second = LevelAnalyzer.Run(level, settings, 9);

            Assert.That(second.passed, Is.EqualTo(first.passed));
            Assert.That(second.quit, Is.EqualTo(first.quit));
            Assert.That(second.revenue, Is.EqualTo(first.revenue));
            Assert.That(second.progressOnFail, Is.EqualTo(first.progressOnFail));
        }

        [Test]
        public void EveryPlayer_EitherPassesOrQuits()
        {
            foreach (LevelData level in new[] { EasyLevel(), MediumLevel(), HardLevel() })
            {
                LevelReport report = LevelAnalyzer.Run(level, settings);

                Assert.That(report.players, Is.EqualTo(settings.populationSize));
                Assert.That(report.passed + report.quit, Is.EqualTo(report.players));
                Assert.That(report.revenue, Is.EqualTo(report.purchases * settings.extraMovesPrice));
            }
        }

        [Test]
        public void PlayersWhoNeverPay_BringNoRevenue()
        {
            settings.shareSometimes = 0f;
            settings.shareOften = 0f;

            LevelReport report = LevelAnalyzer.Run(MediumLevel(), settings);

            Assert.That(report.purchases, Is.Zero);
            Assert.That(report.revenue, Is.Zero);
        }

        [Test]
        public void MorePatientPlayers_QuitLess()
        {
            LevelData level = MakeLevel(9, 20);

            settings.patienceMin = settings.patienceMax = 1.5f;
            float impatient = LevelAnalyzer.Run(level, settings).QuitRate;

            settings.patienceMin = settings.patienceMax = 8f;
            float patient = LevelAnalyzer.Run(level, settings).QuitRate;

            Assert.That(patient, Is.LessThan(impatient - 0.1f), "Patient " + patient + " vs impatient " + impatient);
        }

        [Test]
        public void EasyMediumHard_BehaveTheWayTheGameNeeds()
        {
            // The heart of the game: too easy earns nothing, too hard drives players away,
            // and the level in between is the one that makes money.
            LevelReport easy = LevelAnalyzer.Run(EasyLevel(), settings);
            LevelReport medium = LevelAnalyzer.Run(MediumLevel(), settings);
            LevelReport hard = LevelAnalyzer.Run(HardLevel(), settings);

            string numbers = "revenue per 100: easy " + easy.RevenuePer100Players + ", medium " + medium.RevenuePer100Players + ", hard " + hard.RevenuePer100Players
                + "; quit: easy " + easy.QuitRate + ", medium " + medium.QuitRate + ", hard " + hard.QuitRate;

            Assert.That(easy.FirstAttemptPassRate, Is.GreaterThan(0.9f), numbers);
            Assert.That(easy.QuitRate, Is.LessThan(0.02f), numbers);

            Assert.That(hard.QuitRate, Is.GreaterThan(0.8f), numbers);

            Assert.That(medium.RevenuePer100Players, Is.GreaterThan(easy.RevenuePer100Players * 3f), numbers);
            Assert.That(medium.RevenuePer100Players, Is.GreaterThan(hard.RevenuePer100Players * 3f), numbers);
            Assert.That(medium.QuitRate, Is.LessThan(0.3f), numbers);
        }
    }
}
