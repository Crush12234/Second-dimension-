using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class BattleOrderReadability092PlayModeTests
    {
        [TestCase("CMD_ALL_OUT", "Attack using combat Arts!", "ARTS")]
        [TestCase("CMD_MYSTIC", "Use Mystic Arts!", "MYSTIC")]
        [TestCase("CMD_GUARD", "Hold the line!", "GUARD")]
        [TestCase("CMD_HEAL", "Heal the wounded Union!", "HEAL")]
        public void CommandCardHasOneTitleAndRetainsCostsAndEffect092(string command, string name, string title)
        {
            var forecast = new M2ForecastView
            {
                CommandId = command, CommandName = name, SharedApCost = 8,
                CombinedMpCost = 6, ExpectedEffect = "~196 HP"
            };
            var method = typeof(M2BattleCommandHud072).GetMethod("BuildForecastButtonLabel",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var label = (string)method.Invoke(null, new object[] { forecast, 0, true });
            var lines = label.Split('\n');
            Assert.That(lines[0], Is.EqualTo("✓ " + title));
            Assert.That(lines.Skip(1).Count(line => StringComparer.Ordinal.Equals(line, title)), Is.Zero);
            Assert.That(label, Does.Contain("AP 8").And.Contain("MP 6"));
            Assert.That(lines.Last(), Is.Not.Empty);
            Assert.That(lines.Length, Is.EqualTo(3), "Short orders should not repeat their own category.");
        }
    }
}
