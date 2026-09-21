using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ExpeditionBlindCardChoice091Tests
    {
        [Test]
        public void WideSlotsKeepAllMagicBacksPortraitWithMatchingHitRect091()
        {
            using (var draft = new Draft091(false))
            {
                foreach (var image in draft.Root.GetComponentsInChildren<Image>())
                {
                    if (!image.name.StartsWith("Blind Quest Card Back ")) continue;
                    var slot = (RectTransform)image.transform.parent;
                    slot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 900f);
                    slot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 510f);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(slot);
                    var back = image.rectTransform;
                    Assert.That(image.preserveAspect, Is.True);
                    Assert.That(back.rect.width / back.rect.height,
                        Is.EqualTo(2f / 3f).Within(0.001f));
                    Assert.That(back.rect.width, Is.LessThan(slot.rect.width));
                    Assert.That(image.GetComponent<Button>().targetGraphic.rectTransform,
                        Is.SameAs(back));
                }
            }
        }

        [Test]
        public void DealingNeverRevealsAnOfferOrCommitsWithoutPlayerInput091()
        {
            using (var draft = new Draft091(true))
            {
                Assert.That(draft.Choice.Choose091(0), Is.False);
                draft.Choice.Tick091(30f);
                Assert.That(draft.Choice.Phase091,
                    Is.EqualTo(ExpeditionCardChoice091.ChoicePhase091.AwaitingChoice));
                Assert.That(draft.Faces.All(value => !value.activeInHierarchy), Is.True);
                Assert.That(draft.Root.GetComponentsInChildren<Text>()
                    .Any(value => value.text.Contains("SECRET")), Is.False);
                Assert.That(draft.Root.GetComponentsInChildren<Button>()
                    .Count(value => value.interactable), Is.EqualTo(3));
                draft.Choice.Tick091(300f);
                Assert.That(draft.Commits, Is.Zero);
                Assert.That(draft.Choice.SelectedIndex091, Is.EqualTo(-1));
            }
        }

        [Test]
        public void BlindPickLocksOtherCardsThenRevealsOnlyChosenCardBeforeExactAction091()
        {
            using (var draft = new Draft091(true))
            {
                draft.Choice.Tick091(2f);
                Assert.That(draft.Choice.Choose091(1), Is.True);
                Assert.That(draft.Choice.Choose091(2), Is.False);
                Assert.That(draft.Choice.Confirm091(1), Is.False);
                draft.Choice.Tick091(ExpeditionCardChoice091.RevealDuration091 * 0.3f);
                Assert.That(draft.Faces.All(value => !value.activeInHierarchy), Is.True);
                draft.Choice.Tick091(ExpeditionCardChoice091.RevealDuration091);
                Assert.That(draft.Faces[1].activeInHierarchy, Is.True);
                Assert.That(draft.Faces[0].activeInHierarchy, Is.False);
                Assert.That(draft.Faces[2].activeInHierarchy, Is.False);
                Assert.That(draft.Commits, Is.Zero,
                    "A mystery reveal is not permission to buy an undisclosed offer.");
                Assert.That(draft.Choice.Confirm091(0), Is.False);
                Assert.That(draft.Choice.Confirm091(1), Is.True);
                Assert.That(draft.Choice.Confirm091(1), Is.False);
                Assert.That(draft.Commits, Is.EqualTo(1));
                Assert.That(draft.CommittedIndex, Is.EqualTo(1));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void OptionalOrLockedOfferCanReturnWithoutSpendingAndStaysKnown091(bool canCommit)
        {
            using (var draft = new Draft091(false, canCommit))
            {
                Assert.That(draft.Choice.Choose091(1), Is.True);
                Assert.That(draft.Commits, Is.Zero);
                if (!canCommit) Assert.That(draft.Choice.Confirm091(1), Is.False);
                draft.Choice.ReturnToCards091();
                Assert.That(draft.Commits, Is.Zero);
                Assert.That(draft.Faces[1].activeInHierarchy, Is.True,
                    "Declining must not pretend the already revealed card is unknown.");
                Assert.That(draft.Faces[0].activeInHierarchy, Is.False);
                Assert.That(draft.Faces[2].activeInHierarchy, Is.False);
                Assert.That(draft.Choice.Choose091(2), Is.True);
                Assert.That(draft.Choice.Confirm091(2), Is.True);
                Assert.That(draft.Commits, Is.EqualTo(1));
                Assert.That(draft.CommittedIndex, Is.EqualTo(2));
            }
        }

        [Test]
        public void ReducedMotionStillRequiresBothDeliberatePickAndAction091()
        {
            using (var draft = new Draft091(false))
            {
                draft.Choice.Tick091(100f);
                Assert.That(draft.Commits, Is.Zero);
                Assert.That(draft.Choice.Confirm091(0), Is.False);
                draft.Choice.Choose091(0);
                Assert.That(draft.Faces[0].activeInHierarchy, Is.True);
                Assert.That(draft.Commits, Is.Zero);
                draft.Choice.Confirm091(0);
                Assert.That(draft.Commits, Is.EqualTo(1));
            }
        }

        sealed class Draft091 : IDisposable
        {
            public readonly GameObject Root;
            public readonly ExpeditionCardChoice091 Choice;
            public readonly GameObject[] Faces = new GameObject[3];
            public int Commits;
            public int CommittedIndex = -1;

            public Draft091(bool animate, bool merchantCanCommit = true)
            {
                Root = new GameObject("Blind Card Draft Test 091", typeof(RectTransform));
                Choice = Root.AddComponent<ExpeditionCardChoice091>();
                Choice.Configure091(animate, null, false);
                for (var index = 0; index < 3; index++)
                {
                    var captured = index;
                    var wrapper = new GameObject("Slot " + index, typeof(RectTransform));
                    wrapper.transform.SetParent(Root.transform, false);
                    var face = new GameObject("Face " + index, typeof(RectTransform), typeof(Image));
                    face.transform.SetParent(wrapper.transform, false);
                    Faces[index] = face;
                    var text = new GameObject("Hidden Offer", typeof(RectTransform), typeof(Text));
                    text.transform.SetParent(face.transform, false);
                    text.GetComponent<Text>().text = "SECRET RECRUIT / CHEST / 45 XP";
                    var actionObject = new GameObject("Resolve", typeof(RectTransform), typeof(Image), typeof(Button));
                    actionObject.transform.SetParent(face.transform, false);
                    Choice.Register091((RectTransform)wrapper.transform,
                        (RectTransform)face.transform, actionObject.GetComponent<Button>(),
                        index != 1 || merchantCanCommit,
                        () => { Commits++; CommittedIndex = captured; }, null, index == 1);
                }
            }

            public void Dispose() => UnityEngine.Object.DestroyImmediate(Root);
        }
    }
}
