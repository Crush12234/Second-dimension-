using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class AreaForecastPresentation095Tests
    {
        [Test]
        public void SingleAndSupportForecastsKeepTheirExistingTargetLabels()
        {
            Assert.That(M2BattleCommandHud072.AreaTargetSummaryForVerification095(null), Is.Empty);
            Assert.That(M2BattleCommandHud072.AreaTargetSummaryForVerification095(new M2ForecastView {
                MemberActions = new[] { new M2PredictedActionView { ArtName = "Field Remedy" } }
            }), Is.Empty);
        }
        [TestCase(1, "1 TARGET IN ONE UNION")]
        [TestCase(5, "5 TARGETS IN ONE UNION")]
        public void SelectedUnionAreaShowsTargetCount(int count, string expected)
        {
            Assert.That(M2BattleCommandHud072.AreaTargetSummaryForVerification095(new M2ForecastView {
                MemberActions = new[] { new M2PredictedActionView {
                    AreaTargetCount095 = count, AreaUnionIds095 = new[] { "ENEMY_01" }
                } }
            }), Is.EqualTo(expected));
        }
        [Test]
        public void CrossUnionAreaCountsDistinctUnionsNotRepeatedHits()
        {
            Assert.That(M2BattleCommandHud072.AreaTargetSummaryForVerification095(new M2ForecastView {
                MemberActions = new[] {
                    new M2PredictedActionView { AreaTargetCount095 = 5,
                        AreaUnionIds095 = new[] { "A", "B", "A" } },
                    new M2PredictedActionView { AreaTargetCount095 = 2,
                        AreaUnionIds095 = new[] { "A", "C" } }
                }
            }), Is.EqualTo("3 ENEMY UNIONS"));
        }
    }
}
