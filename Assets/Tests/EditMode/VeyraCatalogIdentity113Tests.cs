using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class VeyraCatalogIdentity113Tests
    {
        const string StableId = "SIGREC_VEYRA_ASHGLASS";

        [Test]
        public void RaceRepairMatchesBothAuthoredIdentityRecordsAndPreservesExistingCodeIdentity113()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var opening = JObject.Parse(File.ReadAllText(Path.Combine(root, "OPENING_SIGNATURE_RECRUITS.json")))
                .Descendants().OfType<JObject>().Single(value => value.Value<string>("signatureId") == "SIG_W01_05");
            var visual = ((JArray)JObject.Parse(File.ReadAllText(Path.Combine(root, "RECRUIT_AUTOGEN_010", "DATA",
                    "SIGNATURE_VISUAL_RECIPES_300_010.json")))["recipes"])
                .Cast<JObject>().Single(value => value.Value<string>("signatureRecruitId") == "SIG_W01_05");
            Assert.That(opening.Value<string>("stableRecruitId"), Is.EqualTo(StableId));
            Assert.That(visual.Value<string>("stableRecruitId"), Is.EqualTo(StableId));
            Assert.That(opening["name"].Value<string>("display"), Is.EqualTo("Veyra Ashglass"));
            Assert.That(visual.Value<string>("displayName"), Is.EqualTo("Veyra Ashglass"));
            Assert.That(opening.Value<string>("raceId"), Is.EqualTo("DEMON_HERITAGE"));
            Assert.That(visual.Value<string>("raceId"), Is.EqualTo("DEMON_HERITAGE"));

            var asset = Resources.Load<TextAsset>("SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300");
            Assert.That(asset, Is.Not.Null);
            var catalog = HeroMaster300Catalog087.FromJson(asset.text);
            Assert.That(catalog.SourceRecordCount, Is.EqualTo(300));
            Assert.That(catalog.AcceptedHeroes.Count, Is.EqualTo(251));
            Assert.That(catalog.QuarantinedHeroes.Count, Is.EqualTo(49));
            Assert.That(catalog.TryGetAcceptedHero(StableId, out var hero), Is.True);
            Assert.That(hero.RosterId, Is.EqualTo(5));
            Assert.That(hero.Name, Is.EqualTo("Veyra Ashglass"));
            Assert.That(hero.Race, Is.EqualTo(opening.Value<string>("raceId")));
            Assert.That(hero.Role, Is.EqualTo("Arcane Tank / Hybrid Front"));
            Assert.That(hero.Weapon, Is.EqualTo("Sword and ward buckler"));
            Assert.That(hero.GameEntityId, Is.EqualTo("SIG_HERO_005_VEYRA_ASHGLASS"));
            Assert.That(hero.GenerationCode, Is.EqualTo("SDG-SS-005-1C457AC9"));
            Assert.That(hero.SsGenerationCode, Is.EqualTo("SD-SS-01-VEYRA_ASHGLASS"));
            Assert.That(hero.Hp, Is.EqualTo(402));
            Assert.That(hero.Ap, Is.EqualTo(42));
            Assert.That(hero.IsNormalApplicantEligible, Is.False, "The metadata repair must not change SS recruitment rules.");
            Assert.That(catalog.TryFindSsByCode(hero.SsGenerationCode, out var byCode), Is.True);
            Assert.That(byCode, Is.SameAs(hero));

            var record = HeroMaster300CreatorRecruitProjection087.BuildOpeningRecord(hero);
            var grant = HeroMaster300CreatorRecruitProjection087.FromHero(hero);
            Assert.That(record.RaceId, Is.EqualTo("DEMON_HERITAGE"));
            Assert.That(grant.Recruit.RaceId, Is.EqualTo(record.RaceId));
            Assert.That(grant.Recruit.AuthoredStableRecruitId, Is.EqualTo(StableId));
            Assert.That(grant.Recruit.RecruitId, Is.EqualTo(HeroMaster300CreatorRecruitProjection087.RecruitIdFor(hero)));
            Assert.That(grant.Recruit.ClassTendencyId, Is.EqualTo(record.StartingClassId));
            Assert.That(grant.Recruit.MaximumHp, Is.EqualTo(402));
            Assert.That(grant.Recruit.MaximumMp, Is.EqualTo(42));
            // The current SS projection remains intact. The older visual recipe's
            // CLASS_GUARDIAN is evidence, not permission to rewrite the master role.
            var legacy = JObject.FromObject(grant.Recruit);
            legacy["RecruitId"] = "SIG_W01_05";
            legacy["SignatureId"] = "SIG_W01_05";
            Assert.That(HeroMaster300CreatorRecruitProjection087.RosterContainsHero(
                new[] { legacy.ToObject<RecruitState>() }, hero), Is.True,
                "An existing canonical Veyra record must still count as owned, not become a duplicate identity.");
        }
    }
}
