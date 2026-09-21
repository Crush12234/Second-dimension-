using System;
using SecondDimension.Gameplay.Recruitment;
using UnityEngine;

namespace SecondDimension.Presentation.Creator028
{
    /// <summary>
    /// Unity Resources loader for the data-only Hero Master package. All records
    /// cross the strict HeroMaster300Catalog087 boundary before the Creator UI can
    /// see a code or hero.
    /// </summary>
    public sealed class HeroMaster300CreatorRegistry087
    {
        public const string ResourcePath =
            "SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300";

        private HeroMaster300CreatorRegistry087(
            HeroMaster300Catalog087 source,
            HeroMaster300CreatorCodeCatalog087 codes)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Codes = codes ?? throw new ArgumentNullException(nameof(codes));
        }

        public HeroMaster300Catalog087 Source { get; }
        public HeroMaster300CreatorCodeCatalog087 Codes { get; }

        public static HeroMaster300CreatorRegistry087 Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
                throw new InvalidOperationException(
                    "Hero Master SS code data is not installed in this build.");
            var source = HeroMaster300Catalog087.FromJson(asset.text);
            return new HeroMaster300CreatorRegistry087(
                source,
                new HeroMaster300CreatorCodeCatalog087(source));
        }
    }
}
