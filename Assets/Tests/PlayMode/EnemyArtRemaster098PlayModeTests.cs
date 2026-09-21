using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class EnemyArtRemaster098PlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp098()
        {
            EnemyArt700Runtime090.ResetForTests090();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown098()
        {
            EnemyArt700Runtime090.ResetForTests090();
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MyrmidonCrossfadeResetAndReopenRetainOnlyLiveOwnedAtlas098()
        {
            // Presentation/lifetime fixture only. No battle outcome, save or
            // victory injection; the actual shipping actor owns both pose leases.
            var canvasObject = new GameObject("Myrmidon Resource Lifecycle Canvas098",
                typeof(RectTransform), typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var hostObject = new GameObject("Myrmidon Shipping Actor Host098", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>();
            host.SetParent(canvasObject.transform, false);
            host.sizeDelta = new Vector2(440f, 700f);
            M2BattleActorRig072 actor = null;
            try
            {
                var firstResource = Resources.Load<Texture2D>(EnemyArtRemaster098.ResourcePath098);
                Assert.That(firstResource != null, Is.True);
                Assert.That(IsResourceRetired098(firstResource), Is.False,
                    "Negative control: the retirement check must reject a live imported Resource.");
                Assert.That(firstResource.isReadable, Is.True,
                    "A newly loaded Resource must permit its one-time alpha framing.");
                actor = CreateActor098(host);
                yield return null;
                AssertPose098(actor, BattleArtPoseDirector011.Idle, "#IDLE");
                var idle = actor.CurrentArtwork076.sprite;
                Assert.That(idle.texture, Is.SameAs(firstResource));
                Assert.That(firstResource.isReadable, Is.False);
                Assert.That(IsResourceRetired098(firstResource), Is.False,
                    "Discarding CPU pixels is not resource retirement; a live actor still owns the native texture.");
                Assert.That(EnemyArtRemaster098.ResourceLoadCount098, Is.EqualTo(1));
                Assert.That(EnemyArt700Runtime090.TryLoadSprite090(EnemyArtRemaster098.BaseId098,
                    EnemyArtRemaster098.VariantId098, EnemyArt700Pose090.Attack,
                    out var action, out var error), Is.True, error);
                Assert.That(action.texture, Is.SameAs(firstResource));
                Assert.That(action, Is.Not.SameAs(idle));

                // Batch rendering can exceed 2,000 fps: a correct0.12-second
                // transition can legitimately exceed240 frames. Bound wall time
                // independently, keeping a finite pathological-iteration guard.
                var transitionStarted098 = Time.realtimeSinceStartupAsDouble;
                var toAction = actor.CrossfadeToPose(BattleArtPoseDirector011.ActionPrimary,
                    0.12f, () => 1f, () => false);
                Assert.That(toAction.MoveNext(), Is.True,
                    "A positive-duration transition must enter the real crossfade, not skip it.");
                Assert.That(EnemyArtRemaster098.OutstandingLeaseCount098, Is.EqualTo(2));
                Assert.That(EnemyArtRemaster098.PinnedSpriteCount098, Is.EqualTo(2));
                Assert.That(actor.Root.GetComponentsInChildren<Image>(true)
                    .Any(value => ReferenceEquals(value.sprite, action)), Is.True,
                    "The shipping incoming Image must actually bind the action frame.");

                EnemyArt700Runtime090.ClearCache090();
                Assert.That(EnemyArtRemaster098.RetiredSpriteCount098, Is.EqualTo(2));
                Assert.That(EnemyArtRemaster098.OutstandingLeaseCount098, Is.EqualTo(2));
                Assert.That(idle != null && action != null && firstResource != null, Is.True,
                    "Reset cannot unload either pose while the actor crossfade owns them.");
                Assert.That(IsResourceRetired098(firstResource), Is.False,
                    "Reset while pinned must preserve the actual native texture, not merely its Editor wrapper.");
                yield return toAction.Current;
                var transitionFrames = 1;
                while (toAction.MoveNext())
                {
                    Assert.That(++transitionFrames, Is.LessThanOrEqualTo(50000),
                        "Action crossfade exceeded the finite iteration safety guard.");
                    Assert.That(Time.realtimeSinceStartupAsDouble - transitionStarted098, Is.LessThanOrEqualTo(3.0),
                        "The0.12-second action crossfade stalled: frames=" + transitionFrames +
                        "; unscaledDelta=" + Time.unscaledDeltaTime);
                    yield return toAction.Current;
                }
                AssertPose098(actor, BattleArtPoseDirector011.ActionPrimary, "#ACTION");
                Assert.That(actor.CurrentArtwork076.sprite, Is.SameAs(action));
                Assert.That(EnemyArtRemaster098.OutstandingLeaseCount098, Is.EqualTo(1));
                Assert.That(firstResource != null && idle != null, Is.True);
                Assert.That(EnemyArtRemaster098.ResourceLoadCount098, Is.EqualTo(1));

                transitionStarted098 = Time.realtimeSinceStartupAsDouble;
                var toIdle = actor.CrossfadeToPose(BattleArtPoseDirector011.Idle,
                    0.12f, () => 1f, () => false);
                transitionFrames = 0;
                while (toIdle.MoveNext())
                {
                    Assert.That(++transitionFrames, Is.LessThanOrEqualTo(50000),
                        "Idle crossfade exceeded the finite iteration safety guard.");
                    Assert.That(Time.realtimeSinceStartupAsDouble - transitionStarted098, Is.LessThanOrEqualTo(3.0),
                        "The0.12-second idle crossfade stalled: frames=" + transitionFrames +
                        "; unscaledDelta=" + Time.unscaledDeltaTime);
                    yield return toIdle.Current;
                }
                Assert.That(transitionFrames, Is.GreaterThan(0));
                AssertPose098(actor, BattleArtPoseDirector011.Idle, "#IDLE");
                Assert.That(actor.CurrentArtwork076.sprite, Is.SameAs(idle));
                Assert.That(EnemyArtRemaster098.OutstandingLeaseCount098, Is.EqualTo(1));
                Assert.That(EnemyArtRemaster098.ResourceLoadCount098, Is.EqualTo(1));
                Assert.That(actor.MissingPoseCount, Is.Zero);
                LogAssert.NoUnexpectedReceived();

                var releaseFrame098 = Time.frameCount;
                actor.Dispose();
                actor = null;
                // Unity Destroy is deferred in PlayMode; allow the real frame
                // boundary before asserting native-object destruction/reload.
                yield return null;
                Assert.That(Time.frameCount, Is.GreaterThan(releaseFrame098),
                    "The runner must have crossed a real player frame before destruction checks.");
                Assert.That(EnemyArtRemaster098.OutstandingLeaseCount098, Is.Zero);
                Assert.That(EnemyArtRemaster098.ResidentSpriteCount098, Is.Zero);
                Assert.That(EnemyArtRemaster098.RetiredSpriteCount098, Is.Zero);
                var nativeResourceRetired098 = IsResourceRetired098(firstResource);
                Assert.That(idle == null && action == null && nativeResourceRetired098, Is.True,
                    "Final owner release must retire both frames and the Resource. " +
                    "idleDestroyed=" + (idle == null) + "; actionDestroyed=" + (action == null) +
                    "; resourceNativeRetired=" + nativeResourceRetired098 +
                    "; resourceUnityNull=" + (firstResource == null) +
                    "; frameDelta=" + (Time.frameCount - releaseFrame098) +
                    "; playing=" + Application.isPlaying +
                    "; resident=" + EnemyArtRemaster098.ResidentSpriteCount098 +
                    "; owners=" + EnemyArtRemaster098.OutstandingLeaseCount098);

                var reopenedResource = Resources.Load<Texture2D>(EnemyArtRemaster098.ResourcePath098);
                Assert.That(reopenedResource != null, Is.True);
                Assert.That(IsResourceRetired098(reopenedResource), Is.False);
                Assert.That(ReferenceEquals(reopenedResource, firstResource), Is.False,
                    "Reopening must not reuse the destroyed managed Resource wrapper.");
                Assert.That(reopenedResource.isReadable, Is.True,
                    "The reloaded import must restore CPU readability for one-time framing.");
                actor = CreateActor098(host);
                yield return null;
                AssertPose098(actor, BattleArtPoseDirector011.Idle, "#IDLE");
                Assert.That(actor.CurrentArtwork076.sprite.texture, Is.SameAs(reopenedResource));
                Assert.That(reopenedResource.isReadable, Is.False);
                Assert.That(EnemyArtRemaster098.ResourceLoadCount098, Is.EqualTo(2));
                Assert.That(EnemyArt700Runtime090.GetDiagnostics090().PngReadCount, Is.Zero);
                Assert.That(actor.MissingPoseCount, Is.Zero);
                LogAssert.NoUnexpectedReceived();
                EnemyArt700Runtime090.ClearCache090();
                actor.Dispose();
                actor = null;
                yield return null;
                Assert.That(EnemyArtRemaster098.OutstandingLeaseCount098, Is.Zero);
                Assert.That(EnemyArtRemaster098.ResidentSpriteCount098, Is.Zero);
                Assert.That(IsResourceRetired098(reopenedResource), Is.True,
                    "The second lifecycle must also unload the native Resource after final owner release.");
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                actor?.Dispose();
                EnemyArt700Runtime090.ClearCache090();
                Object.Destroy(canvasObject);
            }
        }

#if UNITY_EDITOR
        // Unity6000.3 Object.IsNativeObjectAlive explicitly preserves Editor
        // imported-asset resurrection through the persistent ID even when
        // m_CachedPtr is zero. That overloaded ==null is not a native-retirement
        // check for a disk Resource in Editor. Never use this reflection in player.
        static readonly System.Reflection.FieldInfo NativePointerField098 =
            typeof(UnityEngine.Object).GetField("m_CachedPtr",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
#endif
        static bool IsResourceRetired098(Texture2D resource)
        {
#if UNITY_EDITOR
            Assert.That(ReferenceEquals(resource, null), Is.False,
                "Retain the original managed wrapper to inspect actual native retirement.");
            Assert.That(NativePointerField098, Is.Not.Null,
                "Unity native-pointer field changed; update this explicit Editor verification, never silently pass.");
            return (System.IntPtr)NativePointerField098.GetValue(resource) == System.IntPtr.Zero;
#else
            return resource == null;
#endif
        }

        static M2BattleActorRig072 CreateActor098(RectTransform host) => new M2BattleActorRig072(
            host,
            new M2BattleUnionView { UnionId = "MYRMIDON_LIFECYCLE_UNION098", DisplayName = "Myrmidon lifecycle fixture" },
            new M2BattleMemberView
            {
                MemberId = "MYRMIDON_LIFECYCLE_MEMBER098", DisplayName = "Crystal Myrmidon",
                CurrentHp = 300, MaximumHp = 300,
                EnemyArtBaseId090 = EnemyArtRemaster098.BaseId098,
                EnemyArtVariantId090 = EnemyArtRemaster098.VariantId098
            }, true);

        static void AssertPose098(M2BattleActorRig072 actor, string pose, string suffix)
        {
            Assert.That(actor.CurrentPoseId, Is.EqualTo(pose));
            Assert.That(actor.CurrentResourcePath, Is.EqualTo(EnemyArtRemaster098.ResourcePath098 + suffix));
            Assert.That(actor.CurrentArtwork076.sprite != null, Is.True);
            Assert.That(actor.CurrentArtwork076.sprite.texture != null, Is.True);
            Assert.That(actor.CurrentArtwork076.preserveAspect, Is.True);
            Assert.That(actor.EnemyArtworkShaderName075, Is.EqualTo(M2BattleActorRig072.EnemyCutoutShaderName075));
            Assert.That(EnemyArtRemaster098.TrySourcePath098(actor.CurrentArtwork076.sprite, out var path), Is.True);
            Assert.That(path, Is.EqualTo(actor.CurrentResourcePath));
        }
    }
}
