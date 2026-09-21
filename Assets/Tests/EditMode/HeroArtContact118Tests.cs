using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Presentation.Boot;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroArtContact118Tests
    {
        [Test]
        public void ExplicitFreshOutputValidationDoesNotCreateAnyFile118()
        {
            var parent = Path.GetTempPath();
            var output = Path.Combine(parent, "ArtContact118_" + Guid.NewGuid().ToString("N"));
            var result = HeroArtContactSheet118.ValidateOutput118(new[] {
                HeroArtContactSheet118.Flag118, HeroArtContactSheet118.OutputFlag118 + output },
                @"C:\ArtContactTest118\Personal", @"C:\ArtContactTest118\Build\Game_Data");
            Assert.That(result, Is.EqualTo(output.TrimEnd('\\', '/')));
            Assert.That(Directory.Exists(output), Is.False);
            Assert.That(File.Exists(output), Is.False);
        }

        [Test]
        public void MalformedOrCombinedQaFlagsRemainFailClosed118()
        {
            var output = HeroArtContactSheet118.OutputFlag118 + @"C:\unused_art118";
            foreach (var args in new[] {
                new[] { output },
                new[] { HeroArtContactSheet118.Flag118, HeroArtContactSheet118.Flag118, output },
                new[] { HeroArtContactSheet118.Flag118, output, output },
                new[] { HeroArtContactSheet118.Flag118, output, "--sd-roster-audit-093" },
                new[] { HeroArtContactSheet118.Flag118, output, "--sd-tower-soak110-speed=16" },
                new[] { "--SD-HERO-ART118", output },
                new[] { "--sd-hero-art118-misspelled", output } })
            {
                Assert.That(HeroArtContactSheet118.IsRequested118(args), Is.True);
                Assert.Throws<InvalidOperationException>(() => HeroArtContactSheet118.ValidateOutput118(
                    args, @"C:\Personal118", @"C:\Build118\Game_Data"));
            }
        }

        [Test]
        public void PersonalBuildAndExistingOutputPathsAreRejected118()
        {
            foreach (var output in new[] { @"C:\", @"C:\Personal118", @"C:\Personal118\Child", @"C:\Build118\Evidence", "relative", Path.GetTempPath() })
                Assert.Throws<InvalidOperationException>(() => HeroArtContactSheet118.ValidateOutput118(
                    new[] { HeroArtContactSheet118.Flag118, HeroArtContactSheet118.OutputFlag118 + output },
                    @"C:\Personal118", @"C:\Build118\Game_Data"));
        }

        [Test]
        public void CensusKeepsExactSame300AndDoesNotBindQuarantinedMetadata118()
        {
            var source = Resources.Load<TextAsset>(HeroArtContactSheet118.CatalogResource118);
            Assert.That(source, Is.Not.Null);
            var catalog = HeroMaster300Catalog087.FromJson(source.text);
            var rows = HeroArtContactSheet118.ReadCatalog118(source.text);
            Assert.That(rows.Select(value => value.RosterId), Is.EqualTo(Enumerable.Range(1, 300)));
            Assert.That(rows.Select(value => value.StableId).Distinct().Count(), Is.EqualTo(300));
            Assert.That(rows.Select(value => value.StableId).OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(catalog.AcceptedHeroes.Select(value => value.StableId).Concat(catalog.QuarantinedHeroes.Select(value => value.StableId))
                    .OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(rows.Count(value => value.MetadataStatus == "QUARANTINED"), Is.EqualTo(catalog.QuarantinedHeroes.Count));
            foreach (var row in rows.Where(value => value.MetadataStatus == "QUARANTINED"))
            {
                Assert.That(row.QuarantineReasons, Is.Not.Empty);
                foreach (var pose in new[] { row.Portrait, row.Idle, row.Action })
                {
                    Assert.That(pose.SourceKind, Is.EqualTo("NOT_BOUND_METADATA_QUARANTINED"));
                    Assert.That(pose.BindingAuthority, Is.EqualTo("NOT_CALLED"));
                    Assert.That(pose.Sprite, Is.Null);
                    Assert.That(pose.ResourceKey, Is.Empty);
                }
            }
        }

        [Test]
        public void SamePixelsMissingPixelsAndGeneratedFallbackCannotCountAsDistinctBuiltAction118()
        {
            var idle = Visible118("hashA");
            var action = Visible118("hashA");
            Assert.That(HeroArtContactSheet118.SameVisual118(idle, action), Is.True);
            Assert.That(HeroArtContactSheet118.IsDistinctBuiltAction118(idle, action), Is.False);
            action.PixelSha256 = "hashB";
            Assert.That(HeroArtContactSheet118.IsDistinctBuiltAction118(idle, action), Is.True);
            action.SourceKind = "GENERATED_FALLBACK_NOT_FINISHED_ART";
            Assert.That(HeroArtContactSheet118.IsDistinctBuiltAction118(idle, action), Is.False);
            action = Visible118("hashB"); action.RenderStatus = "BUILT_SPRITE_RENDERED_EMPTY";
            Assert.That(HeroArtContactSheet118.IsDistinctBuiltAction118(idle, action), Is.False);
            action = Visible118("");
            Assert.That(HeroArtContactSheet118.IsDistinctBuiltAction118(idle, action), Is.False);
        }

        [Test]
        public void CsvFailureCannotPublishACompletedJsonReport118()
        {
            // Publication-only fixture: synthetic completed census/sheet metadata
            // exercises the real writer, without binding art or creating a save.
            var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            var output = Path.Combine(temporaryRoot, "ArtContactPublication118_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            var owner = new GameObject("Art contact report publication test118");
            owner.SetActive(false); // Never run the opt-in packaged Start coroutine.
            try
            {
                var helper = owner.AddComponent<HeroArtContactSheet118>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var type = typeof(HeroArtContactSheet118);
                type.GetField("_output", flags).SetValue(helper, output);
                type.GetField("_rows", flags).SetValue(helper, Enumerable.Range(1, 300).Select(id =>
                    new HeroArtContactSheet118.Row118 { RosterId = id, StableId = "SYNTHETIC_REPORT118_" + id,
                        MetadataStatus = "QUARANTINED" }).ToList());
                var sheets = (System.Collections.IList)type.GetField("_sheets", flags).GetValue(helper);
                for (var index = 0; index < 25; index++) sheets.Add(new { File = "synthetic_sheet_" + index });
                var write = type.GetMethod("WriteReport118", flags);
                var csv = Path.Combine(output, "hero_art118.csv");
                var json = Path.Combine(output, "hero_art118.json");
                Directory.CreateDirectory(csv); // Real filesystem failure at the CSV publication boundary.
                var failed = Assert.Throws<System.Reflection.TargetInvocationException>(() => write.Invoke(helper, null));
                Assert.That(failed.InnerException is IOException || failed.InnerException is UnauthorizedAccessException, Is.True);
                Assert.That(File.Exists(json), Is.False,
                    "A failed CSV write must not leave ExportComplete=true JSON behind.");
                Assert.That(Directory.Exists(csv), Is.True);
                Directory.Delete(csv, false);

                write.Invoke(helper, null);
                Assert.That(File.Exists(csv), Is.True);
                Assert.That(File.ReadAllLines(csv), Has.Length.EqualTo(301));
                var report = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(json));
                Assert.That(report.Value<bool>("ExportComplete"), Is.True);
                Assert.That(report.Value<string>("FinishedArtCertification"), Is.EqualTo("NOT_PERFORMED"));
                Assert.That(report.Value<string>("AnimationCertification"), Is.EqualTo("NOT_PERFORMED"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                var resolved = Path.GetFullPath(output);
                Assert.That(resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileName(resolved).StartsWith("ArtContactPublication118_", StringComparison.Ordinal), Is.True);
                Directory.Delete(resolved, true);
            }
        }


        static HeroArtContactSheet118.Pose118 Visible118(string hash) => new HeroArtContactSheet118.Pose118 {
            SourceKind = "BUILT_IDENTITY_BOUND_SOURCE", RenderStatus = "RENDERED_VISIBLE", PixelSha256 = hash
        };
    }
}
