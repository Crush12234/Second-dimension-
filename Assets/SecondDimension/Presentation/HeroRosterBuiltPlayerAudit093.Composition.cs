using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    [Serializable]
    public sealed class HeroCompositionActor093
    {
        public string recruitId, displayName, resource, texture;
        public float xMin, xMax, yMin, yMax, height, expectedFoot, headClearance;
        public bool exactSavedIdentity, aspectPreserved;
    }

    [Serializable]
    public sealed class HeroCompositionCapture093
    {
        public int members, screenWidth, screenHeight;
        public string fixtureBoundary = "Copied R54 recruited identities and unchanged stats/equipment; fresh presentation-only battle. No campaign progression or rewards claimed.";
        public string screenshot, failure = "";
        public float minimumScreenHeightFraction, visibleHeightRatio, minimumNeighbourGap;
        public bool sourceUnchanged;
        public List<HeroCompositionActor093> actors = new List<HeroCompositionActor093>();
    }

    public sealed partial class HeroRosterBuiltPlayerAudit093
    {
        readonly List<HeroCompositionCapture093> _composition093 = new List<HeroCompositionCapture093>();
        string _compositionSource093 = "", _compositionSourceHash093 = "";

        IEnumerator RunCompositions093()
        {
            var requested = Argument093(Environment.GetCommandLineArgs(), "--sd-roster-audit-composition-source", "");
            if (string.IsNullOrWhiteSpace(requested)) yield break;
            CampaignState source = null;
            Exception sourceError = null;
            try
            {
                if (!Path.IsPathRooted(requested)) throw new InvalidOperationException("Composition source must be an absolute existing save path.");
                _compositionSource093 = Path.GetFullPath(requested);
                var bytes = File.ReadAllBytes(_compositionSource093);
                _compositionSourceHash093 = CompositionHash093(bytes);
                // ReadWithRecovery may repair a save. Run it only on our evidence
                // copy, never against the read-only source save or its backup.
                var copied = Path.Combine(_root, "FixtureSaves", "composition_source_copy_093.json");
                File.WriteAllBytes(copied, bytes);
                source = HeroRosterAudit093.Read093(copied);
            }
            catch (Exception error) { sourceError = error; }
            if (sourceError != null)
            {
                _composition093.Add(new HeroCompositionCapture093 { failure = sourceError.ToString() });
                yield break;
            }
            foreach (var count in new[] { 1, 2, 3, 6 })
            {
                var row = new HeroCompositionCapture093
                {
                    members = count, screenWidth = Screen.width, screenHeight = Screen.height,
                    minimumScreenHeightFraction = count == 1 ? 0.28f : count == 2 ? 0.24f : count == 3 ? 0.22f : 0.145f
                };
                _composition093.Add(row);
                M1RuntimeCoordinator coordinator = null;
                CampaignState fixture = null;
                CompositionStep093(row, () =>
                {
                    fixture = CreateCompositionFixture093(source, count);
                    var save = Path.Combine(_root, "FixtureSaves", "composition_" + count + "_093.json");
                    HeroRosterAudit093.Write093(save, fixture);
                    coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, save);
                    _presenter.Initialize(coordinator);
                    _presenter.ShowRosterAudit093(M1Screen.Battle);
                });
                if (!string.IsNullOrEmpty(row.failure)) continue;
                yield return new WaitForSecondsRealtime(0.55f);
                yield return new WaitForEndOfFrame();
                CompositionStep093(row, () =>
                {
                    row.screenshot = CaptureRelative093("Screenshots/composition_" + count + "_" + Screen.width + "x" + Screen.height + "_093.png");
                    row.sourceUnchanged = CompositionHash093(File.ReadAllBytes(_compositionSource093)) == _compositionSourceHash093;
                    if (!row.sourceUnchanged) throw new InvalidOperationException("Read-only source save changed during composition verification.");
                    InspectComposition093(coordinator, fixture, row);
                });
                WriteReport093(false);
            }
        }

        public static CampaignState CreateCompositionFixture093(CampaignState source, int count)
        {
            if (source?.Guild == null || source.Profile == null || source.OpeningFlow == null)
                throw new InvalidOperationException("Composition source lacks a real recruited campaign.");
            string[] names;
            switch (count)
            {
                case 1: names = new[] { "Yves Thornfield" }; break;
                case 2: names = new[] { "Odelia Fen", "Vaelis Noct" }; break;
                case 3: names = new[] { "Tazren Warmask", "Odelia Fen", "Vaelis Noct" }; break;
                case 6: names = new[] { "Maren Holt", "Odelia Fen", "Gara Redtail", "Daeven Fellstar", "Tazren Warmask", "Jazzi Wirewick" }; break;
                default: throw new ArgumentOutOfRangeException(nameof(count));
            }
            var saved = names.Select(name => source.Guild.Recruits.Single(value => value.DisplayName == name)).ToArray();
            // No stat, identity, Art, gear or progression changes to these recruits.
            // The synthetic fresh Guild only removes active adventure locks from
            // this explicitly isolated layout fixture, not from the source save.
            var guild = new GuildState("COMPOSITION_FIXTURE_093", source.Guild.TreasuryXp,
                saved, Array.Empty<UnionState>(), source.Guild.Inventory, source.Guild.Development);
            var fixture = new CampaignState("00000000-0000-0000-0000-000000000193", source.CampaignSeed,
                source.ContentAuthorityVersion, source.Rules, guild, source.Profile, source.OpeningFlow);
            var commands = new M1CommandService();
            for (var index = 0; index < saved.Length; index++)
                fixture = HeroRosterAudit093.Require093(commands.AssignRecruitToUnion(fixture, saved[index].RecruitId, 0, index));
            foreach (var recruit in saved)
                if (!ReferenceEquals(recruit, fixture.Guild.Recruits.Single(value => value.RecruitId == recruit.RecruitId)))
                    throw new InvalidOperationException("Union placement unexpectedly replaced a saved recruit in the layout fixture.");
            return HeroRosterAudit093.Require093(new M2BattleCommandService().StartEncounterBattle(fixture,
                M2CombatContent.LoadFromDirectory(HeroRosterAudit093.ContentRoot093),
                "BATTLE_COMPOSITION_ONLY_093_" + count, "Isolated composition-size proof, not story progression.", 1));
        }

        static void InspectComposition093(M1RuntimeCoordinator coordinator, CampaignState fixture,
            HeroCompositionCapture093 row)
        {
            var controller = FindFirstObjectByType<M2BattleExperienceController072>();
            var view = controller?.OwnedDiorama078;
            var stage = GameObject.Find("Battle Diorama Experience 072")?.GetComponent<RectTransform>();
            if (view == null || stage == null) throw new InvalidOperationException("Shipping battle view did not become active.");
            var union = coordinator.State.Battle.PlayerUnions.Single();
            if (union.Members.Count != row.members) throw new InvalidOperationException("Wrong number of live party members.");
            var headerBottom = RectTransformUtility.WorldToScreenPoint(null, stage.TransformPoint(new Vector3(0f,
                stage.rect.yMin + stage.rect.height * M2BattleDioramaView072.FocusedUnionHpRibbonMinY076))).y;
            foreach (var member in union.Members)
            {
                var rig = view.ResolveActor(member.MemberId, union.UnionId);
                var art = rig?.CurrentArtwork076;
                if (art?.sprite == null || art.sprite.texture == null ||
                    M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089(rig.CurrentResourcePath))
                    throw new InvalidOperationException("Composition must show actual complete supplied sprites, not missing/generated fallback shapes.");
                var bounds = CompositionScreenBounds093(art);
                var saved = fixture.Guild.Recruits.Single(value => value.RecruitId == member.MemberId);
                var foot = RectTransformUtility.WorldToScreenPoint(null,
                    rig.Root.TransformPoint(new Vector3(0f, rig.Root.rect.height * 0.07f))).y;
                var actor = new HeroCompositionActor093
                {
                    recruitId = member.MemberId, displayName = member.DisplayName,
                    resource = rig.CurrentResourcePath, texture = art.sprite.texture.name,
                    xMin = bounds.xMin, xMax = bounds.xMax, yMin = bounds.yMin, yMax = bounds.yMax,
                    height = bounds.height, expectedFoot = foot, headClearance = headerBottom - bounds.yMax,
                    exactSavedIdentity = saved.DisplayName == member.DisplayName &&
                        (string.IsNullOrEmpty(saved.AuthoredStableRecruitId) || saved.AuthoredStableRecruitId == member.PortraitAuthorityId),
                    aspectPreserved = art.preserveAspect && Mathf.Abs(rig.Root.localScale.x - rig.Root.localScale.y) < 0.0001f
                };
                row.actors.Add(actor);
                if (!actor.exactSavedIdentity || !actor.aspectPreserved || Math.Abs(bounds.yMin - foot) > 2f ||
                    bounds.height <= Screen.height * row.minimumScreenHeightFraction || actor.headClearance < 8f ||
                    bounds.xMin < 0f || bounds.xMax > Screen.width || bounds.yMin < 0f || bounds.yMax > Screen.height)
                    throw new InvalidOperationException("Actual party silhouette fails screen-height/feet/header/aspect/identity limits: " + member.DisplayName);
            }
            row.visibleHeightRatio = row.actors.Max(value => value.height) / row.actors.Min(value => value.height);
            var ordered = row.actors.OrderBy(value => value.xMin).ToArray();
            row.minimumNeighbourGap = ordered.Length == 1 ? Screen.width :
                Enumerable.Range(1, ordered.Length - 1).Min(index => ordered[index].xMin - ordered[index - 1].xMax);
            if (row.visibleHeightRatio > 1.035f || row.minimumNeighbourGap < 4f)
                throw new InvalidOperationException("Actual party has unequal visible body heights or overlapping resting silhouettes.");
        }

        static Rect CompositionScreenBounds093(Image art)
        {
            var sprite = art.sprite;
            var visible = M1SilhouetteFraming091.VisibleRect091(sprite);
            var rect = art.rectTransform.rect;
            var scale = Mathf.Min(rect.width / sprite.rect.width, rect.height / sprite.rect.height);
            var origin = rect.center - sprite.rect.size * (scale * 0.5f);
            var a = RectTransformUtility.WorldToScreenPoint(null, art.rectTransform.TransformPoint(origin + (visible.min - sprite.rect.min) * scale));
            var b = RectTransformUtility.WorldToScreenPoint(null, art.rectTransform.TransformPoint(origin + (visible.max - sprite.rect.min) * scale));
            return Rect.MinMaxRect(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Max(a.x, b.x), Math.Max(a.y, b.y));
        }

        static string CompositionHash093(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }
        static void CompositionStep093(HeroCompositionCapture093 row, Action action)
        {
            if (!string.IsNullOrEmpty(row.failure)) return;
            try { action(); } catch (Exception error) { row.failure = error.ToString(); }
        }
    }
}
