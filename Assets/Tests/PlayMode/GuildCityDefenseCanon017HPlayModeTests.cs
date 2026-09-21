using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class GuildCityDefenseCanon017HPlayModeTests
    {
        [UnityTest]
        public IEnumerator StrategicResourcesLoadInPlayerContext()
        {
            Assert.NotNull(Resources.Load<TextAsset>("SecondDimension/GuildCity017H/BUILDING_COMBAT_XP_CONTRIBUTIONS_017H"));
            Assert.NotNull(Resources.Load<TextAsset>("SecondDimension/GuildCity017H/CITY_DEFENSE_PROFILES_017H"));
            Assert.NotNull(Resources.Load<TextAsset>("SecondDimension/GuildCity017H/CANON_EVENT_CATALOG_017H"));
            Assert.NotNull(Resources.Load<UnityEngine.Object>("SecondDimension/GuildCity017H/Defense/CITY_DEFENSE_MAP_017H"));
            Assert.NotNull(Resources.Load<UnityEngine.Object>("SecondDimension/GuildCity017H/Defense/DEF_LANE_MAIN_GATE"));
            Assert.NotNull(Resources.Load<UnityEngine.Object>("SecondDimension/GuildCity017H/Canon/HISTORICAL_CHRONICLE"));
            yield return null;
        }
    }
}
