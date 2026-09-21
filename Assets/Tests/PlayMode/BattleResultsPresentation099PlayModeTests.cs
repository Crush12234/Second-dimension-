#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class BattleResultsPresentation099PlayModeTests
    {
        GameObject _owner;
        Canvas _canvas;
        string _directory;

        [UnityTearDown]
        public IEnumerator Cleanup099()
        {
            if (_owner != null) UnityEngine.Object.Destroy(_owner);
            if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
            yield return null;
            if (!string.IsNullOrEmpty(_directory) && Directory.Exists(_directory))
                Directory.Delete(_directory, true); // Only the exact new GUID fixture directory.
        }

        [UnityTest]
        public IEnumerator ActualEarnedBattleResultsUseNamedArtsWithoutChangingTheRecord099()
        {
            var source = Path.Combine(Application.dataPath, "Tests", "Fixtures", "Tower098", "R100_OldCompletedFloor010.json");
            var original = File.ReadAllBytes(source);
            _directory = Path.Combine(Path.GetTempPath(), "sd_result_names099_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            var save = Path.Combine(_directory, "CopiedEarnedTower.json");
            File.Copy(source, save);
            var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, save);
            var before = File.ReadAllBytes(save);
            var view = coordinator.ReadBattleView098();
            Assert.That(view.IsResolved, Is.True);
            var content = HeroRosterAudit093.LoadCombatContent093();
            var learnedNames = view.Events.Where(item => item.EventType == "BREAKTHROUGH")
                .Select(item => content.Art(item.ArtId).Name).Distinct().ToArray();
            Assert.That(learnedNames.Length, Is.GreaterThanOrEqualTo(4));
            var results = Create099();
            results.Show(view);
            yield return null;
            Assert.That(Field099<Image>(results, "_breakthroughHeroStage076").gameObject.activeSelf, Is.True);
            Assert.That(learnedNames.Select(name => name.ToUpperInvariant()),
                Does.Contain(Field099<Text>(results, "_breakthroughArt076").text));
            var growth = Field099<Text>(results, "_heroArt").text;
            Assert.That(learnedNames.Any(name => growth == "NEW ART  •  " + name), Is.True, growth);
            Assert.That(File.ReadAllBytes(save), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(original));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BothResultArtSlotsPreferTheExactExistingBattleBody099()
        {
            var results = Create099();
            foreach (var identity in new[]
            {
                "SIGREC_ZORIN_BRAMBLECROSS", "SIGREC_YVES_THORNFIELD",
                "SSS_RYLEN_STONEBOND", "HERO_REC_170", "HERO_REC_265"
            })
            {
                // Rendering-only view inputs. No character acquisition, Arts, XP,
                // battle result, or other authority state is created by this test.
                var member = new M2BattleMemberView
                {
                    MemberId = identity, PortraitAuthorityId = identity, VisualSeed = identity,
                    DisplayName = "Exact identity art fixture", ClassName = "Guild Adventurer"
                };
                Assert.That(M1VisualAssets.TryResolveBattleStandee(identity, identity, null, identity,
                    out var expected, out var expectedKey), Is.True, identity);
                Assert.That(expected, Is.Not.Null);
                Assert.That(expectedKey, Does.Not.Contain("/Portraits/"));
                Invoke099(results, "ApplyFeaturedPortrait", member, member.DisplayName);
                Invoke099(results, "ApplyBreakthroughPortrait076", member, member.DisplayName);
                yield return null;
                foreach (var field in new[] { "_heroPortrait", "_breakthroughPortrait076" })
                {
                    var image = Field099<Image>(results, field);
                    Assert.That(image.sprite, Is.SameAs(expected), identity + " " + field);
                    Assert.That(image.sprite.rect, Is.EqualTo(expected.rect), "Use the existing framed body, never an atlas or invented crop.");
                    Assert.That(image.preserveAspect, Is.True);
                    Assert.That(image.color, Is.EqualTo(Color.white));
                    Assert.That(image.raycastTarget, Is.False);
                }
                Assert.That(Field099<Text>(results, "_heroInitials").gameObject.activeSelf, Is.False);
                Assert.That(Field099<Text>(results, "_breakthroughInitials076").gameObject.activeSelf, Is.False);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MissingBodyKeepsTheExistingFallbackWithoutBorrowingAnAuthoredHero099()
        {
            var results = Create099();
            const string identity = "UNAUTHORED_RESULT_IDENTITY_099";
            var member = new M2BattleMemberView { MemberId = identity, VisualSeed = identity, PortraitAuthorityId = identity };
            Assert.That(M1VisualAssets.TryResolveBattleStandee(identity, identity, null, identity, out _, out _), Is.False);
            var hasFallback = M1VisualAssets.TryResolvePortrait(identity, identity, null, identity, out var expected, out _);
            Invoke099(results, "ApplyFeaturedPortrait", member, "Unmapped adventurer");
            Invoke099(results, "ApplyBreakthroughPortrait076", member, "Unmapped adventurer");
            yield return null;
            Assert.That(Field099<Image>(results, "_heroPortrait").sprite, Is.SameAs(expected));
            Assert.That(Field099<Image>(results, "_breakthroughPortrait076").sprite, Is.SameAs(expected));
            Assert.That(Field099<Text>(results, "_heroInitials").gameObject.activeSelf, Is.EqualTo(!hasFallback));
            Assert.That(Field099<Text>(results, "_breakthroughInitials076").gameObject.activeSelf, Is.EqualTo(!hasFallback));
        }

        M2BattleResultsView072 Create099()
        {
            // Use the shipping scaled Canvas construction, not an unscaled test panel.
            var runtimeUi = typeof(M1FlowPresenter).Assembly.GetType("SecondDimension.Presentation.RuntimeUi", true);
            _canvas = (Canvas)runtimeUi.GetMethod("CreateCanvas", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { "Actual results Canvas 099" });
            _owner = new GameObject("Actual results builder 099");
            var results = _owner.AddComponent<M2BattleResultsView072>();
            results.Initialize(_canvas.GetComponent<RectTransform>(), () => { });
            return results;
        }
        static T Field099<T>(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(target);
        }
        static void Invoke099(object target, string name, params object[] args)
        {
            var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, args);
        }
    }
}
#endif
