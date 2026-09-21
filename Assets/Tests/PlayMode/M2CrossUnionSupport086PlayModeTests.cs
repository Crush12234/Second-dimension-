using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M2CrossUnionSupport086PlayModeTests
    {
        [UnityTest]
        public IEnumerator RevivedActorLeavesDownedStateAndShowsRecoveredHp086()
        {
            var hostObject = new GameObject(
                "Cross Union Revival Host 086", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>();
            var union = new M2BattleUnionView
            {
                UnionId = "PLAYER_RESCUED_UNION_086",
                DisplayName = "Rescued Union"
            };
            var member = new M2BattleMemberView
            {
                MemberId = "SIGI_MAREN_REVIVE_086",
                PortraitAuthorityId = "SIGREC_MAREN_HOLT",
                DisplayName = "Maren Holt",
                CurrentHp = 0,
                MaximumHp = 100,
                Downed = true
            };
            var actor = new M2BattleActorRig072(host, union, member, false);

            Assert.That(actor.Downed, Is.True);
            Assert.That(actor.NameLabel076.text, Does.Contain("DOWN"));
            Assert.That(actor.HealthLabel076.text, Is.EqualTo("HP 0 / 100"));

            actor.PresentHp076(31, 100);
            Assert.That(actor.SetPoseImmediate(BattleArtPoseDirector011.Idle),
                Is.True);

            Assert.That(actor.Downed, Is.False,
                "Visible HP recovery must override the immutable pre-round Downed snapshot.");
            Assert.That(actor.PresentedCurrentHp076, Is.EqualTo(31));
            Assert.That(actor.PresentedHpFill076, Is.EqualTo(0.31f).Within(0.001f));
            Assert.That(actor.HealthLabel076.text, Is.EqualTo("HP 31 / 100"));
            Assert.That(actor.NameLabel076.text, Does.Not.Contain("DOWN"));

            actor.Dispose();
            Object.Destroy(hostObject);
            yield return null;
        }
    }
}
