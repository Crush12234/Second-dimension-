using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourGoldSmokeTowerReloadCopy090Tests
    {
        [Test]
        public void ReloadedTowerCertificationMatchesBattleOnlyLobbyCopy090()
        {
            var smokeSource090 = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "FirstHour071",
                "FirstHourGoldSmoke071.cs"));
            var towerSource090 = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "Campaign022",
                "GuildCityFlowPresenter022.cs"));

            var certification090 = CompactWhitespace090(Slice090(
                smokeSource090,
                "private IEnumerator CertifyPackagedTowerFloorOne081()",
                "private IEnumerator ResolveVisibleTowerBattle081()"));
            var lobby090 = CompactWhitespace090(Slice090(
                towerSource090,
                "void BuildGuildCityAbyss022(",
                "void BuildTowerFloorHero085("));

            Assert.That(lobby090, Does.Contain(
                    "body,\"Climb next Tower floor 084\", \"FIGHT FLOOR \"+" +
                    "s.TowerFloorNumber+\"\\nYOUR NEXT UNION BATTLE\""),
                "The shipping battle-only Tower lobby must keep one explicit next-floor action.");

            Assert.That(certification090, Does.Contain(
                    "var floorTwoBeginCopy081 = floorTwoBeginLabel081?.text ?? string.Empty;"));
            Assert.That(certification090, Does.Contain(
                    "floorTwoBeginCopy081.IndexOf( \"FIGHT FLOOR \" + " +
                    "reloadedTower081.TowerFloorNumber, " +
                    "StringComparison.OrdinalIgnoreCase) >= 0"),
                "Packaged certification must verify the dynamic next-floor battle label, " +
                "not retired card-Tower copy.");
            Assert.That(certification090, Does.Contain(
                    "floorTwoBeginCopy081.IndexOf( \"YOUR NEXT UNION BATTLE\", " +
                    "StringComparison.OrdinalIgnoreCase) >= 0"),
                "Packaged certification must prove the battle-only Tower promise is visible.");
            Assert.That(certification090, Does.Contain(
                    "floorTwoSummaryCopy084.IndexOf( \"FLOOR 2\", " +
                    "StringComparison.OrdinalIgnoreCase) >= 0"));
            Assert.That(certification090, Does.Contain(
                    "floorTwoSummaryCopy084.IndexOf( \"BEST FLOOR • 1\", " +
                    "StringComparison.OrdinalIgnoreCase) >= 0"));
            Assert.That(certification090, Does.Contain(
                    "floorTwoBegin081 != null && floorTwoBegin081.IsInteractable()"),
                "The copy match cannot replace the live active/interactable button proof.");
            Assert.That(certification090, Does.Not.Contain("\"CLIMB NEXT FLOOR\""),
                "The retired card-Tower label must not return to packaged certification.");
        }

        private static string Slice090(
            string source090,
            string start090,
            string end090)
        {
            var startIndex090 = source090.IndexOf(start090, StringComparison.Ordinal);
            var endIndex090 = source090.IndexOf(
                end090,
                Math.Max(0, startIndex090 + start090.Length),
                StringComparison.Ordinal);
            Assert.That(startIndex090, Is.GreaterThanOrEqualTo(0),
                "Missing source anchor: " + start090);
            Assert.That(endIndex090, Is.GreaterThan(startIndex090),
                "Missing source anchor after " + start090 + ": " + end090);
            return source090.Substring(startIndex090, endIndex090 - startIndex090);
        }

        private static string CompactWhitespace090(string value090) =>
            string.Join(
                " ",
                (value090 ?? string.Empty).Split(
                    (char[])null,
                    StringSplitOptions.RemoveEmptyEntries));
    }
}
