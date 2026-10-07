using NUnit.Framework;
using Match3Engine.Players;

namespace Match3Engine.Tests
{
    public class LevelVerdictTests
    {
        // A report for 100 players with the given outcome
        private static LevelReport Report(int passedFirstTry, int quit, float revenue)
        {
            return new LevelReport
            {
                players = 100,
                passedOnFirstAttempt = passedFirstTry,
                passed = 100 - quit,
                quit = quit,
                revenue = revenue
            };
        }

        [Test]
        public void EveryonePassingAndNobodyPaying_IsTooEasy()
        {
            Assert.That(LevelVerdict.Rate(Report(98, 0, 0f)), Is.EqualTo(LevelRating.TooEasy));
        }

        [Test]
        public void GoodRevenueWithFewQuits_IsTheSweetSpot()
        {
            Assert.That(LevelVerdict.Rate(Report(45, 6, 6f)), Is.EqualTo(LevelRating.SweetSpot));
        }

        [Test]
        public void RevenueThatCostsManyPlayers_IsCostly()
        {
            Assert.That(LevelVerdict.Rate(Report(25, 25, 10f)), Is.EqualTo(LevelRating.Costly));
        }

        [Test]
        public void MostPlayersQuitting_IsTooHard_WhateverItEarns()
        {
            Assert.That(LevelVerdict.Rate(Report(0, 99, 1f)), Is.EqualTo(LevelRating.TooHard));
            Assert.That(LevelVerdict.Rate(Report(10, 45, 12f)), Is.EqualTo(LevelRating.TooHard));
        }

        [Test]
        public void LittleRevenueWithoutBeingAFreePass_IsFlat()
        {
            Assert.That(LevelVerdict.Rate(Report(70, 3, 1.5f)), Is.EqualTo(LevelRating.Flat));
        }

        [Test]
        public void EveryRating_HasItsOwnDescription()
        {
            var seen = new System.Collections.Generic.HashSet<string>();

            foreach (LevelRating rating in System.Enum.GetValues(typeof(LevelRating)))
            {
                string description = LevelVerdict.Describe(rating);

                Assert.That(description, Is.Not.Empty);
                Assert.That(seen.Add(description), Is.True, rating + " shares its description with another rating.");
            }
        }
    }
}
