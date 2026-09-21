using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EnemyReferencedSupplementalArts094Tests
    {
        M2CombatContent _content;
        JObject _authored;
        JObject _enemies;

        [OneTimeSetUp]
        public void Load094()
        {
            var root = HeroRosterAudit093.ContentRoot093;
            _content = M2CombatContent.LoadFromDirectory(root);
            _authored = JObject.Parse(File.ReadAllText(Path.Combine(root, "PASS_02", "ART_DEFINITIONS.json")));
            _enemies = JObject.Parse(File.ReadAllText(Path.Combine(root, "PASS_03", "ENEMY_DEFINITIONS.json")));
        }

        [Test]
        public void EveryCurrentEnemyLearnedArtResolvesAndOnlyTwoSupplementalDefinitionsAreAdded094()
        {
            foreach (var enemy in _enemies["enemies"].Cast<JObject>())
            foreach (var id in enemy["artIds"].Values<string>())
                Assert.That(_content.Arts.ContainsKey(id), Is.True, enemy.Value<string>("id") + " -> " + id);
            var extras = _authored["culturalArts"].Concat(_authored["hybridArts"])
                .Select(row => row.Value<string>("id")).ToArray();
            Assert.That(extras.Where(_content.Arts.ContainsKey), Is.EquivalentTo(new[] {
                EnemyReferencedSupplementalArts094.Guard094, EnemyReferencedSupplementalArts094.Commander094 }));
        }

        [TestCase(EnemyReferencedSupplementalArts094.Guard094, 3, 0, "Guard")]
        [TestCase(EnemyReferencedSupplementalArts094.Commander094, 5, 1, "Tactical")]
        public void AdaptedSupportKeepsAuthoredIdentityCostsAndEffectEvidence094(string id, int ap, int mp, string sourceDiscipline)
        {
            var raw = _authored["culturalArts"].Concat(_authored["hybridArts"])
                .Single(row => row.Value<string>("id") == id);
            var art = _content.Art(id);
            Assert.That(art.Name, Is.EqualTo(raw.Value<string>("displayName")));
            Assert.That(art.Family, Is.EqualTo(raw.Value<string>("family")));
            Assert.That(art.AnimationTag, Is.EqualTo(raw.Value<string>("animationTag")));
            Assert.That(art.SharedApCost, Is.EqualTo(ap).And.EqualTo(raw.Value<int>("sharedApCost")));
            Assert.That(art.PersonalMpCost, Is.EqualTo(mp).And.EqualTo(raw.Value<int>("personalMpCost")));
            Assert.That(art.RequiredEquipmentTags, Is.EquivalentTo(raw["requiredEquipmentTags"].Values<string>()));
            Assert.That(raw["effectFormula"].Values<string>().All(art.EffectTags.Contains), Is.True);
            Assert.That(art.EffectTags, Does.Contain("SOURCE_DISCIPLINE_" + sourceDiscipline.ToUpperInvariant()));
            Assert.That(art.Discipline, Is.EqualTo("Support"));
            Assert.That(art.TargetRule, Is.EqualTo("ALLY_UNION"));
            Assert.That(M2BattleCommandService.SupportsFriendlyUnionTargetForVerification080(art), Is.True);
            Assert.That(art.PlayerDirectlySelectableInStandard, Is.False);
            if (id == EnemyReferencedSupplementalArts094.Guard094)
                Assert.That(art.EffectTags, Does.Contain("BARRIER"));
        }

        [TestCase(EnemyReferencedSupplementalArts094.Guard094, "ENEMY_BRASSJAW_PACKLORD_01", "ENCOUNTER_BRASSJAW_PACKLORD")]
        [TestCase(EnemyReferencedSupplementalArts094.Commander094, "ENEMY_CAPTAIN_RAVEL_01", "ENCOUNTER_CAPTAIN_RAVEL")]
        public void ActualEnemySelectionAndSupportResolutionChargeOnceAndNeverHit094(string id, string definitionId, string encounter)
        {
            var root = HeroRosterAudit093.ContentRoot093;
            var resolver = EncounterRosterResolver070.LoadFromContentRoot(root);
            var roster = resolver.Resolve(9402, "SUPPLEMENTAL_094", "SUPPLEMENTAL_BOARD_094",
                encounter, 10, "EXPLICIT_ISOLATED_SUPPORT_FIXTURE_094");
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.RosterId == 171);
            var campaign = HeroRosterAudit093.SignAndPlace093(HeroRosterAudit093.CreateFixture093(hero),
                hero, new HeroRosterAuditRow093());
            campaign = HeroRosterAudit093.Require093(new M2BattleCommandService().StartEncounterBattleWithRoster070(
                campaign, _content, "SUPPLEMENTAL_BATTLE_094", "Isolated real enemy support verification.", roster));
            var unions = campaign.Battle.EnemyUnions.ToList();
            var sourceIndex = unions.FindIndex(union => union.Members.Any(member => member.ClassId == definitionId));
            Assert.That(sourceIndex, Is.GreaterThanOrEqualTo(0));
            var actor = unions[sourceIndex].Members.Single(member => member.ClassId == definitionId);
            Assert.That(actor.LearnedArtIds, Does.Contain(id), "Use the real source enemy's existing learned Art.");
            var targetIndex = sourceIndex == 0 ? 1 : 0;
            unions[targetIndex] = unions[targetIndex].With(cohesion: 1, formationConditionBasisPoints: 1000,
                guarding: false, engagement: EngagementState.Broken);
            var target = unions[targetIndex];
            var beforeHp = unions.SelectMany(union => union.Members).ToDictionary(member => member.MemberId, member => member.CurrentHp);
            var beforeAp = unions[sourceIndex].CurrentAp;
            var events = new List<BattleEventState>();
            // Test the existing runtime selector/handler, not a copied support model.
            var method = typeof(M2BattleCommandService).GetMethod("TryResolveEnemySupportArt086",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var applied = (bool)method.Invoke(null, new object[] {
                1, _content, unions, sourceIndex, actor.MemberId, target, events });
            Assert.That(applied, Is.True);
            var art = _content.Art(id);
            Assert.That(unions[sourceIndex].CurrentAp, Is.EqualTo(beforeAp - art.SharedApCost));
            Assert.That(unions[sourceIndex].Members.Single(member => member.MemberId == actor.MemberId).CurrentMp,
                Is.EqualTo(actor.CurrentMp - art.PersonalMpCost));
            Assert.That(events.Count(value => value.EventType == "ENEMY_SUPPORT_FORECAST" && value.ArtId == id), Is.EqualTo(1));
            Assert.That(events.Count(value => value.EventType == "ALLY_SUPPORT" && value.ArtId == id), Is.EqualTo(1));
            Assert.That(events.Where(value => value.ArtId == id).All(value => !value.EventType.Contains("HIT")), Is.True);
            Assert.That(events.Where(value => value.ArtId == id).All(value =>
                value.ActorUnionId == unions[sourceIndex].UnionId && value.TargetUnionId == target.UnionId), Is.True);
            Assert.That(unions[targetIndex].Cohesion, Is.GreaterThan(target.Cohesion));
            Assert.That(unions[targetIndex].FormationConditionBasisPoints, Is.GreaterThan(target.FormationConditionBasisPoints));
            Assert.That(unions.SelectMany(union => union.Members).All(member => member.CurrentHp == beforeHp[member.MemberId]), Is.True);
            if (id == EnemyReferencedSupplementalArts094.Guard094)
            {
                Assert.That(unions[targetIndex].Guarding, Is.True);
                Assert.That(events.Any(value => value.ArtId == id && value.EventType == "ALLY_PROTECTED"), Is.True);
            }
            var offensive = typeof(M2BattleCommandService).GetMethod("SelectEnemyArt070",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(offensive, Is.Not.Null);
            for (var round = 1; round <= 8; round++)
                Assert.That((string)offensive.Invoke(null, new object[] { actor, _content, 100, round, 0 }),
                    Is.Not.EqualTo(id), "A supportive Art must never be relabelled as a damaging enemy hit.");
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrDuplicateReferencedSupplementalSourceFailsClosed094(bool duplicate)
        {
            var copy = (JObject)_authored.DeepClone();
            var array = (JArray)copy["culturalArts"];
            var row = array.Single(value => value.Value<string>("id") == EnemyReferencedSupplementalArts094.Guard094);
            if (duplicate) array.Add(row.DeepClone()); else row.Remove();
            Assert.Throws<InvalidDataException>(() => EnemyReferencedSupplementalArts094.AddToRuntime094(
                new Dictionary<string, M2ArtDefinition>(), copy, _enemies));
        }
    }
}
