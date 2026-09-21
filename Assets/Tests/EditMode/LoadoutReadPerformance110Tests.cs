using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class LoadoutReadPerformance110Tests
    {
        [Test, Timeout(180000)]
        public void AffectedRosterChoicesRemainLegalIndependentAndFreshAfterClaim110()
        {
            var source = Environment.GetEnvironmentVariable("SD_RESET110_PERF_SOURCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
                Assert.Ignore("Set SD_RESET110_PERF_SOURCE to the preserved affected save.");
            var path = Path.Combine(Path.GetTempPath(), "sd_loadout110_" + Guid.NewGuid().ToString("N") + ".json");
            var original = File.ReadAllBytes(source);
            File.Copy(source, path, false);
            try
            {
                var coordinator = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), path);
                var campaignField = typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic);
                var campaign = (CampaignState)campaignField.GetValue(coordinator);
                var view = coordinator.State;
                Assert.That(view.CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(campaign)));
                var allChoices = new List<M1EquipmentChoiceView>();
                foreach (var heroView in view.Recruits)
                {
                    var hero = campaign.Guild.Recruits.Single(value => value.RecruitId == heroView.RecruitId);
                    foreach (var slot in heroView.Slots)
                    foreach (var choice in slot.Choices)
                    {
                        var item = choice.IsEquipped ? hero.Equipment.Find(slot.SlotId).Item
                            : campaign.Guild.Inventory.Single(value => value.InstanceId == choice.ItemId);
                        var legal = SssTenV4Inventory090.CanEquip(hero, item, slot.SlotId, out var reason);
                        Assert.That(choice.IsLegal, Is.EqualTo(legal), hero.RecruitId + "/" + choice.ItemId);
                        Assert.That(choice.LegalityReason, Is.EqualTo(legal
                            ? choice.IsEquipped ? "Currently equipped" : "Compatible slot" : reason));
                        if (!choice.IsEquipped) Assert.That(legal, Is.True, "Inventory only lists compatible choices.");
                        allChoices.Add(choice);
                    }
                }
                var shared = allChoices.Where(value => !value.IsEquipped)
                    .GroupBy(value => value.ItemId).First(group => group.Count() > 1).Take(2).ToArray();
                Assert.That(shared[0], Is.Not.SameAs(shared[1]));
                var name = shared[1].DisplayName;
                shared[0].DisplayName = "MUTATED UI COPY";
                Assert.That(shared[1].DisplayName, Is.EqualTo(name));
                var second = coordinator.State;
                Assert.That(second.Recruits.SelectMany(hero => hero.Slots).SelectMany(slot => slot.Choices)
                    .Any(choice => choice.DisplayName == "MUTATED UI COPY"), Is.False);
                var claim = coordinator.ClaimBattleRewards();
                Assert.That(claim.Succeeded, Is.True, claim.Message);
                var after = coordinator.State;
                Assert.That(after.CanonicalStateHash, Is.Not.EqualTo(view.CanonicalStateHash));
                Assert.That(after.CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(campaignField.GetValue(coordinator))));
                Assert.That(File.ReadAllBytes(source), Is.EqualTo(original));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
            }
        }
    }
}
