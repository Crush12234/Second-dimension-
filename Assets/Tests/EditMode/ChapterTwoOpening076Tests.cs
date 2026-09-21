using System;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ChapterTwoOpening076Tests
    {
        [Test]
        public void OpeningStageProjectsAResumableStoryHandoff076()
        {
            var state = ActiveChapterTwoState076();
            Assert.That(
                M1FlowPresenter.ChapterTwoOpeningStageForVerification076(state),
                Is.EqualTo(ChapterTwoOpeningStage076.WayglassBriefing));

            state.Expedition = ChapterTwoExpedition076("N01");
            Assert.That(
                M1FlowPresenter.ChapterTwoOpeningStageForVerification076(state),
                Is.EqualTo(ChapterTwoOpeningStage076.RouteDecision),
                "After the apprentice scene, the saved board owns the first visible route junction.");

            state.Expedition = ChapterTwoExpedition076("N02");
            Assert.That(
                M1FlowPresenter.ChapterTwoOpeningStageForVerification076(state),
                Is.EqualTo(ChapterTwoOpeningStage076.RouteCommitted),
                "After the decision is saved, ordinary field-command play owns the operation.");
        }

        [Test]
        public void DecisionLayoutIsFixedReadableAndNonOverlapping076()
        {
            var regions = M1FlowPresenter.ChapterTwoDecisionRegionsForVerification076;
            Assert.That(regions.Count, Is.EqualTo(7));
            for (var index = 0; index < regions.Count; index++)
            {
                Assert.That(regions[index].xMin, Is.GreaterThanOrEqualTo(0f), "region " + index);
                Assert.That(regions[index].yMin, Is.GreaterThanOrEqualTo(0f), "region " + index);
                Assert.That(regions[index].xMax, Is.LessThanOrEqualTo(1f), "region " + index);
                Assert.That(regions[index].yMax, Is.LessThanOrEqualTo(1f), "region " + index);
            }

            for (var left = 0; left < regions.Count; left++)
            for (var right = left + 1; right < regions.Count; right++)
                Assert.That(regions[left].Overlaps(regions[right]), Is.False,
                    "Chapter 2 regions " + left + " and " + right + " overlap.");

            Assert.That(M1FlowPresenter.ChapterTwoNorthRouteAction076,
                Is.Not.EqualTo(M1FlowPresenter.ChapterTwoUnderhallRouteAction076));
            Assert.That(M1FlowPresenter.ChapterTwoThresholdAction078,
                Is.EqualTo("DESCEND WITH KIRI"));
            Assert.That(M1FlowPresenter.ChapterTwoArcTitle076, Is.EqualTo("THE DOOR INSIDE"));
            Assert.That(M1FlowPresenter.ChapterTwoOperationTitle076,
                Is.EqualTo("THE LINES NOT RETURNED"));
            Assert.That(M1FlowPresenter.ChapterTwoFreshMarksRouteTitle076,
                Is.EqualTo("THE FRESH MARKS"));
            Assert.That(M1FlowPresenter.ChapterTwoBrokenBridgeRouteTitle076,
                Is.EqualTo("THE BROKEN BRIDGE"));
            Assert.That(M1FlowPresenter.ChapterTwoArcTitle076,
                Is.Not.EqualTo(M1FlowPresenter.ChapterTwoOperationTitle076),
                "The chapter arc and the active operation need visibly distinct hierarchy levels.");
            Assert.That(
                M1FlowPresenter.ChapterTwoRouteButtonHeightForVerification076(1920f, 1080f),
                Is.GreaterThanOrEqualTo(132f),
                "Chapter 2 decisions must retain the production minimum touch height at 1920x1080.");
            Assert.That(
                M1FlowPresenter.ChapterTwoRouteButtonHeightForVerification076(1280f, 800f),
                Is.GreaterThanOrEqualTo(132f),
                "Chapter 2 decisions must retain the production minimum touch height at 1280x800.");
            Assert.That(Resources.Load<Texture2D>(M1FlowPresenter.ChapterTwoKiriKeyArtResource076),
                Is.Not.Null,
                "The Chapter 2 handoff must ship with its authored Kiri/Wayglass key art.");
            Assert.That(M1FlowPresenter.ChapterTwoKiriVerticalFocus076,
                Is.InRange(0.90f, 0.94f),
                "The cinematic crop must favor Kiri's face instead of centering on her torso.");
        }

        [TestCase(1280f, 800f)]
        [TestCase(1920f, 1080f)]
        public void KiriCropKeepsHerHeadInsideTheProductionFrame076(
            float screenWidth,
            float screenHeight)
        {
            var texture = Resources.Load<Texture2D>(
                M1FlowPresenter.ChapterTwoKiriKeyArtResource076);
            Assert.That(texture, Is.Not.Null);
            var canvas = M1FlowPresenter.ExpeditionCanvasSizeForVerification074(
                screenWidth,
                screenHeight);
            var viewportWidth = canvas.x * 0.565f;
            var viewportHeight = Mathf.Max(1f, canvas.y - 84f);
            var artworkAspect = texture.width / (float)Mathf.Max(1, texture.height);
            Assert.That(viewportWidth / viewportHeight, Is.GreaterThan(artworkAspect),
                "This proof models the production EnvelopeParent width constraint.");

            var artworkHeight = viewportWidth / artworkAspect;
            var topCropFraction = Mathf.Max(
                0f,
                (artworkHeight - viewportHeight) *
                (1f - M1FlowPresenter.ChapterTwoKiriVerticalFocus076) /
                artworkHeight);
            Assert.That(topCropFraction, Is.LessThanOrEqualTo(0.05f),
                "Kiri's head band is cropped by " + topCropFraction.ToString("P1") +
                " at " + screenWidth + "x" + screenHeight + ".");
        }

        private static GuildCityPresentationState017D ActiveChapterTwoState076()
        {
            return new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasActiveContract = true,
                Contracts = new[]
                {
                    new GuildCityContractView017D
                    {
                        ContractId = "CONTRACT_BELL_BENEATH_GATE",
                        IsCompleted = true
                    },
                    new GuildCityContractView017D
                    {
                        ContractId = "CONTRACT_LINES_NOT_RETURNED",
                        IsActive = true
                    }
                }
            };
        }

        private static GuildCityExpeditionView017D ChapterTwoExpedition076(string nodeId)
        {
            return new GuildCityExpeditionView017D
            {
                BoardId = "BOARD_LINES_NOT_RETURNED",
                CurrentNodeId = nodeId,
                Status = "Active",
                VisitedNodeIds = new[] { "N00", "N01", nodeId },
                RevealedNodeIds = new[] { "N00", "N01", "N02", "N04" },
                LinkedNodeIds = StringComparer.Ordinal.Equals(nodeId, "N01")
                    ? new[] { "N02", "N04" }
                    : Array.Empty<string>()
            };
        }
    }
}
