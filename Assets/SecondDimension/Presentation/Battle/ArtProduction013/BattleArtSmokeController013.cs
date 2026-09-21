using System.Collections;
using SecondDimension.Presentation.Battle.ArtProduction011;
using UnityEngine;

namespace SecondDimension.Presentation.Battle.ArtProduction013
{
    public sealed class BattleArtSmokeController013 : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float secondsPerPose = 0.75f;
        [SerializeField] private bool loop = true;

        private IEnumerator Start()
        {
            do
            {
                BattlePoseAnimator011[] animators = FindObjectsByType<BattlePoseAnimator011>(FindObjectsSortMode.None);
                yield return Show(animators, BattlePoseId011.Anticipation);
                yield return Show(animators, BattlePoseId011.PrimaryAction);
                yield return Show(animators, BattlePoseId011.GuardCastSupport);
                yield return Show(animators, BattlePoseId011.HitReaction);
                yield return Show(animators, BattlePoseId011.Victory);
                yield return Show(animators, BattlePoseId011.IdleReady);
            } while (loop);
        }

        private IEnumerator Show(BattlePoseAnimator011[] animators, BattlePoseId011 pose)
        {
            foreach (BattlePoseAnimator011 animator in animators)
            {
                if (animator == null) continue;
                BattleArtCharacterTag013 tag = animator.GetComponent<BattleArtCharacterTag013>();
                if (tag != null) animator.SetPose(tag.StableId, pose);
            }
            yield return new WaitForSecondsRealtime(secondsPerPose);
        }
    }
}
