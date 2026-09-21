using System;
using System.Collections;
using System.Collections.Generic;
using SecondDimension.Presentation.Battle.ArtProduction011;
using PoseSetManifest011 = SecondDimension.Presentation.Battle.ArtProduction011.BattleArtManifest011;
using UnityEngine;

namespace SecondDimension.Presentation.Battle.ArtProduction012
{
    /// <summary>
    /// Development-only visual harness for the clean Art 011 assets.
    /// It never resolves combat and never changes authoritative state.
    /// </summary>
    public sealed class ThursdayArtShowcase012 : MonoBehaviour
    {
        [SerializeField] private bool autoCycle = true;
        [SerializeField, Min(0.25f)] private float poseSeconds = 0.75f;
        [SerializeField] private float allyScale = 1.45f;
        [SerializeField] private float enemyScale = 1.55f;

        private readonly List<ShowcaseActor> actors = new List<ShowcaseActor>();
        private Coroutine cycle;

        private static readonly BattlePoseId011[] PoseCycle =
        {
            BattlePoseId011.IdleReady,
            BattlePoseId011.Anticipation,
            BattlePoseId011.PrimaryAction,
            BattlePoseId011.RoleAction,
            BattlePoseId011.GuardCastSupport,
            BattlePoseId011.HitReaction,
            BattlePoseId011.Downed,
            BattlePoseId011.Victory,
        };

        private void Start()
        {
            BuildStage();
            if (autoCycle)
            {
                cycle = StartCoroutine(CyclePoses());
            }
        }

        private void OnDestroy()
        {
            if (cycle != null)
            {
                StopCoroutine(cycle);
            }
        }

        public void BuildStage()
        {
            if (actors.Count > 0)
            {
                return;
            }

            PoseSetManifest011 manifest = BattleArtManifestLoader011.Load();
            EnsureCamera();
            CreateBackground();

            CharacterPoseSet011[] allies = Array.FindAll(manifest.characters, c => c.side == "ALLY");
            CharacterPoseSet011[] enemies = Array.FindAll(manifest.characters, c => c.side == "ENEMY");

            for (int i = 0; i < allies.Length; i++)
            {
                float x = -7.25f + i * 1.85f;
                float y = i % 2 == 0 ? -2.75f : -1.85f;
                CreateActor(allies[i], new Vector3(x, y, 0f), allyScale, 20 + i);
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                float x = 2.9f + i * 2.1f;
                float y = i == 1 ? -1.75f : -2.55f;
                CreateActor(enemies[i], new Vector3(x, y, 0f), enemyScale, 30 + i);
            }
        }

        public void SetAllPoses(BattlePoseId011 pose)
        {
            foreach (ShowcaseActor actor in actors)
            {
                actor.Animator.SetPose(actor.StableId, pose);
            }
        }

        private IEnumerator CyclePoses()
        {
            int index = 0;
            while (true)
            {
                SetAllPoses(PoseCycle[index]);
                index = (index + 1) % PoseCycle.Length;
                yield return new WaitForSecondsRealtime(poseSeconds);
            }
        }

        private void CreateActor(
            CharacterPoseSet011 definition,
            Vector3 position,
            float scale,
            int sortingOrder)
        {
            GameObject actorObject = new GameObject($"Showcase_{definition.stableId}");
            actorObject.transform.SetParent(transform, false);
            actorObject.transform.localPosition = position;
            actorObject.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = actorObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            BattlePoseAnimator011 animator = actorObject.AddComponent<BattlePoseAnimator011>();
            if (!animator.SetPose(definition.stableId, BattlePoseId011.IdleReady))
            {
                throw new InvalidOperationException($"Could not load idle pose for {definition.stableId}.");
            }

            actors.Add(new ShowcaseActor(definition.stableId, animator));
        }

        private void EnsureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Showcase Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(transform, false);
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.transform.position = new Vector3(0f, -0.25f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.065f, 1f);
        }

        private void CreateBackground()
        {
            const string resourcePath = "SecondDimension/Art/Battle/BG_TUTORIAL_GATEWORKS_ARENA";
            Sprite background = Resources.Load<Sprite>(resourcePath);
            if (background == null)
            {
                return;
            }

            GameObject backgroundObject = new GameObject("Showcase Background");
            backgroundObject.transform.SetParent(transform, false);
            backgroundObject.transform.localPosition = new Vector3(0f, 0f, 4f);
            backgroundObject.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            SpriteRenderer renderer = backgroundObject.AddComponent<SpriteRenderer>();
            renderer.sprite = background;
            renderer.sortingOrder = -100;
            renderer.color = new Color(0.82f, 0.86f, 1f, 1f);
        }

        private readonly struct ShowcaseActor
        {
            public ShowcaseActor(string stableId, BattlePoseAnimator011 animator)
            {
                StableId = stableId;
                Animator = animator;
            }

            public string StableId { get; }
            public BattlePoseAnimator011 Animator { get; }
        }
    }
}
