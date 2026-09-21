using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Perspective, presentation-only battlefield for Slice 020. It consumes an
    /// immutable presentation directive and never calls Gameplay or Save services.
    /// The existing illustrated battlefield remains the automatic fallback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class M2Battle3DWorld : MonoBehaviour
    {
        private const int BattleLayer = 29;
        private const float GuildHeight = 2.85f;
        private const float EnemyHeight = 3.00f;

        private sealed class ActorView
        {
            public string MemberId;
            public string UnionId;
            public bool Enemy;
            public Transform Root;
            public Transform Billboard;
            public Transform Pose;
            public Transform SpriteNode;
            public SpriteRenderer Renderer;
            public Sprite IdleSprite;
            public Sprite ActionSprite;
            public LineRenderer Ring;
            public Vector3 LocalHome;
            public float Height;
            public bool Downed;
            public bool Retreated;
            public string EquipmentVisualFamily;
            public string EquipmentAnimatorSet;
            public string ArmorVisualFamily;
            public string ArmorMeshSet;
        }

        private sealed class UnionView
        {
            public string UnionId;
            public bool Enemy;
            public BattleUnionDeploymentSlot020 Slot;
            public Transform FormationRoot;
            public Transform StandardBillboard;
            public TextMesh StandardLabel;
            public LineRenderer StandardRing;
            public List<ActorView> Actors;
            public float OverviewScale;
        }

        private readonly Dictionary<string, ActorView> _actors =
            new Dictionary<string, ActorView>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<ActorView>> _unions =
            new Dictionary<string, List<ActorView>>(StringComparer.Ordinal);
        private readonly Dictionary<string, UnionView> _unionViews =
            new Dictionary<string, UnionView>(StringComparer.Ordinal);
        private readonly List<Material> _ownedMaterials = new List<Material>();
        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private readonly List<ParticleSystem> _particles = new List<ParticleSystem>();
        private readonly List<EnemyArt700SpriteLease090> _enemyArtLeases090 =
            new List<EnemyArt700SpriteLease090>();

        private GameObject _worldRoot;
        private GameObject _forcesRoot;
        private Camera _camera;
        private SpriteRenderer _environmentBackdropRenderer;
        private Material _stone;
        private Material _darkStone;
        private Material _brass;
        private Material _line;
        private Material _particle;
        private Material _guildShadow;
        private Material _enemyShadow;
        private Material _enemyArtCutout090;
        private Material _mystic;
        private Mesh _shadowDisc;
        private Vector3 _widePosition;
        private Quaternion _wideRotation;
        private Vector3 _wideLookPoint = new Vector3(0f, 1.8f, 0f);
        private string _stagedPlayerUnionId = string.Empty;
        private string _stagedEnemyUnionId = string.Empty;
        private float _shakeStrength = 1f;
        private bool _reducedFlash;
        private bool _impactPaused;
        private bool _tacticalLabelsAllowed = true;
        private Func<float> _speedProvider;

        public bool IsReady => _worldRoot != null && _camera != null;
        public string FocusedUnionId { get; private set; } = string.Empty;
        public string ActiveBackdropResourceKey083 { get; private set; } = string.Empty;
        public string ActiveBackdropTextureName083 =>
            _environmentBackdropRenderer?.sprite?.texture?.name ?? string.Empty;

        public bool ShowBattle(M2BattleView battle, bool resolving, bool reducedMotion)
        {
            if (battle == null) return false;
            var deployment = BattleUnionDeploymentPlanning020.Plan(battle);
            if (!CanRenderDeployment(deployment)) return false;
            EnsureWorld();
            if (!IsReady) return false;
            _worldRoot.SetActive(true);
            RefreshCameraLockedBackdrop(battle.BattleId);
            RebuildForces(battle, deployment);
            ConfigureTacticalOverviewCamera(deployment);
            _tacticalLabelsAllowed = !resolving;
            SetTacticalWorldLabelsVisible(true);
            FocusedUnionId = string.Empty;
            SetWideCamera(resolving ? 0.12f : 0f);
            return true;
        }

        public void SetVisible(bool visible)
        {
            if (_worldRoot != null) _worldRoot.SetActive(visible);
        }

        public void HighlightUnion(string unionId)
        {
            ApplyUnionHighlight(unionId, null);
        }

        private void ApplyUnionHighlight(string unionId, string targetUnionId)
        {
            UnionView focus = null;
            var hasFocus = !string.IsNullOrWhiteSpace(unionId) &&
                           _unionViews.TryGetValue(unionId, out focus);
            var hasTarget = !string.IsNullOrWhiteSpace(targetUnionId) &&
                            _unionViews.ContainsKey(targetUnionId);
            var focusIsEnemy = hasFocus && focus.Enemy;
            foreach (var pair in _unionViews)
            {
                var highlighted = !hasFocus ||
                                  StringComparer.Ordinal.Equals(pair.Key, unionId);
                var targeted = hasTarget && StringComparer.Ordinal.Equals(pair.Key, targetUnionId);
                var sameSide = hasFocus && pair.Value.Enemy == focusIsEnemy;
                if (pair.Value.FormationRoot != null)
                {
                    var scale = !hasFocus ? pair.Value.OverviewScale :
                        highlighted ? 1f : targeted ? Mathf.Max(0.88f, pair.Value.OverviewScale) :
                        pair.Value.OverviewScale;
                    pair.Value.FormationRoot.localScale = Vector3.one * scale;
                }
                if (pair.Value.StandardLabel != null)
                {
                    var labelColor = pair.Value.Enemy ? Red : Cyan;
                    pair.Value.StandardLabel.color = new Color(
                        labelColor.r, labelColor.g, labelColor.b,
                        highlighted || targeted ? 1f : 0.72f);
                }
                if (pair.Value.StandardRing != null)
                {
                    var ringColor = pair.Value.Enemy ? Red : Cyan;
                    var ringAlpha = highlighted || targeted ? 0.78f : 0.28f;
                    pair.Value.StandardRing.startColor = new Color(ringColor.r, ringColor.g, ringColor.b, ringAlpha);
                    pair.Value.StandardRing.endColor = new Color(ringColor.r, ringColor.g, ringColor.b, ringAlpha);
                }
                foreach (var actor in pair.Value.Actors)
                {
                    if (actor.Ring != null)
                    {
                        var alpha = !hasFocus ? (actor.Enemy ? 0.64f : 0.68f) :
                            highlighted ? 0.96f : targeted ? 0.72f : sameSide ? 0.12f : 0.30f;
                        if (actor.Downed) alpha = Mathf.Min(alpha, 0.18f);
                        var baseColor = actor.Enemy
                            ? new Color(1f, 0.24f, 0.28f, alpha)
                            : new Color(0.22f, 0.88f, 1f, alpha);
                        actor.Ring.startColor = baseColor;
                        actor.Ring.endColor = baseColor;
                    }

                    if (actor.Renderer == null) continue;
                    var actorColor = actor.Renderer.color;
                    actorColor.a = actor.Retreated ? 0.24f : actor.Downed ? 0.38f :
                        !hasFocus || highlighted ? 1f : targeted ? 0.82f : sameSide ? 0.26f : 0.46f;
                    actor.Renderer.color = actorColor;
                }
            }
        }

        public IEnumerator FocusUnionTurn(
            string unionId,
            Func<float> speed,
            bool reducedMotion,
            Func<bool> stopRequested)
        {
            if (!IsReady || string.IsNullOrWhiteSpace(unionId) ||
                !_unionViews.TryGetValue(unionId, out var union) || union.Actors.Count == 0)
            {
                yield return ReturnToTacticalOverview(speed, reducedMotion, stopRequested);
                yield break;
            }

            RestoreStagedFormations();
            ApplyUnionHighlight(unionId, null);
            SetTacticalWorldLabelsVisible(false);
            var center = Vector3.zero;
            var count = 0;
            for (var index = 0; index < union.Actors.Count; index++)
            {
                var actor = union.Actors[index];
                if (actor?.Root == null) continue;
                center += ActorHome(actor);
                count++;
            }
            if (count == 0)
            {
                yield return ReturnToTacticalOverview(speed, reducedMotion, stopRequested);
                yield break;
            }

            center /= count;
            var enemy = union.Enemy;
            var unionRadius = FormationRadius(union.Actors);
            var focusDistance = Mathf.Max(9.8f, unionRadius * 2.8f + 6.2f);
            var position = center + new Vector3(enemy ? 4.6f : -4.6f, 4.15f, -focusDistance);
            var rotation = Quaternion.LookRotation(center + Vector3.up * 1.75f - position, Vector3.up);
            if (reducedMotion)
            {
                _camera.transform.position = position;
                _camera.transform.rotation = rotation;
                FocusedUnionId = unionId;
                yield break;
            }
            yield return MoveCamera(position, rotation, 0.46f, speed, stopRequested);
            FocusedUnionId = unionId;
        }

        public IEnumerator ReturnToTacticalOverview(
            Func<float> speed,
            bool reducedMotion,
            Func<bool> stopRequested)
        {
            RestoreStagedFormations();
            HighlightUnion(null);
            FocusedUnionId = string.Empty;
            if (!IsReady) yield break;
            if (reducedMotion)
            {
                SetWideCamera(0f);
                SetTacticalWorldLabelsVisible(true);
                yield break;
            }
            yield return MoveCamera(_widePosition, _wideRotation, 0.34f, speed, stopRequested);
            SetTacticalWorldLabelsVisible(true);
        }

        public IEnumerator PlayDirective(
            Battle3DPresentationDirective directive,
            Func<float> speed,
            bool reducedMotion,
            Func<bool> stopRequested,
            float shakeStrength,
            bool reducedFlash)
        {
            if (directive == null || !IsReady || !directive.VisuallyStaged) yield break;
            _speedProvider = speed;
            _shakeStrength = Mathf.Clamp01(shakeStrength);
            _reducedFlash = reducedFlash;
            var actor = ResolveActor(directive.ActorMemberId, directive.ActorUnionId);
            var target = ResolveActor(directive.TargetMemberId, directive.TargetUnionId);

            PrepareDirectiveStaging(directive, actor, target);
            SetTacticalWorldLabelsVisible(false);

            yield return MoveCameraFor(directive, actor, target, reducedMotion, speed, stopRequested);
            if (Stopped(stopRequested)) yield break;

            if (directive.UsesFirstHourArtPerformance071)
            {
                yield return PlayFirstHourArtChoreography071(
                    directive, actor, target, reducedMotion, speed, stopRequested);
                if (Stopped(stopRequested)) yield break;
            }

            if (reducedMotion)
            {
                yield return ReducedMotion(directive, actor, target, speed, stopRequested);
                yield break;
            }

            switch (directive.Action)
            {
                case Battle3DAction.Establish:
                    yield return Establish(speed, stopRequested);
                    break;
                case Battle3DAction.CommitCommand:
                    yield return Pulse(actor, "UNION ORDER", Gold, 0.32f, speed, stopRequested);
                    break;
                case Battle3DAction.WeaponStrike:
                case Battle3DAction.CombatArt:
                case Battle3DAction.TacticalStrike:
                    yield return Strike(actor, target, directive, speed, stopRequested);
                    break;
                case Battle3DAction.MysticArt:
                    yield return Mystic(actor, target, directive, speed, stopRequested);
                    break;
                case Battle3DAction.Restore:
                    yield return Restore(actor, target, directive, speed, stopRequested);
                    break;
                case Battle3DAction.Guard:
                    yield return Guard(actor, "GUARD", speed, stopRequested);
                    break;
                case Battle3DAction.Intercept:
                    yield return Intercept(actor, target, directive, speed, stopRequested);
                    break;
                case Battle3DAction.Recover:
                    yield return Pulse(target ?? actor, "RECOVERY", Gold, 0.34f, speed, stopRequested);
                    break;
                case Battle3DAction.Reform:
                    yield return Pulse(target ?? actor, "FORMATION RESTORED", Cyan, 0.36f, speed, stopRequested);
                    break;
                case Battle3DAction.Reposition:
                    yield return Reposition(actor, target, directive, speed, stopRequested);
                    break;
                case Battle3DAction.Downed:
                    yield return Down(target ?? actor, directive, speed, stopRequested);
                    break;
                case Battle3DAction.Learn:
                    yield return Pulse(target ?? actor,
                        M2BattleReadableText021.ArtXpGain(
                            directive.ArtId,
                            directive.Caption,
                            directive.DisplayAmount),
                        Cyan, 0.52f, speed, stopRequested);
                    break;
                case Battle3DAction.Breakthrough:
                    yield return Pulse(target ?? actor,
                        M2BattleReadableText021.NewArtLearned(directive.ArtId, directive.Caption),
                        Gold, 0.72f, speed, stopRequested);
                    break;
                case Battle3DAction.Retreat:
                    yield return Retreat(actor, speed, stopRequested);
                    break;
                case Battle3DAction.Result:
                    yield return Result(speed, stopRequested);
                    break;
                default:
                    yield return Wait(0.14f, speed, stopRequested);
                    break;
            }
        }

        private static Color Gold => new Color(1f, 0.76f, 0.25f, 1f);
        private static Color Cyan => new Color(0.28f, 0.88f, 1f, 1f);
        private static Color Red => new Color(1f, 0.30f, 0.34f, 1f);

        private void EnsureWorld()
        {
            if (_worldRoot != null) return;
            _worldRoot = NewObject("Second Dimension Twenty Union Cinematic Arena 020", null);
            CreateMaterials();
            CreateCamera();
            CreateLighting();
            CreateEnvironment();
        }

        private void CreateMaterials()
        {
            _stone = MaterialFor("Standard", new Color(0.12f, 0.16f, 0.19f, 1f));
            _darkStone = MaterialFor("Standard", new Color(0.025f, 0.045f, 0.064f, 1f));
            _brass = MaterialFor("Standard", new Color(0.55f, 0.36f, 0.12f, 1f));
            _line = MaterialFor("Sprites/Default", Color.white);
            _particle = MaterialFor("Sprites/Default", new Color(0.80f, 0.62f, 0.36f, 0.78f));
            _guildShadow = MaterialFor("Unlit/Color", new Color(0.002f, 0.018f, 0.030f, 1f));
            _enemyShadow = MaterialFor("Unlit/Color", new Color(0.16f, 0.005f, 0.008f, 1f));
            _enemyArtCutout090 = MaterialFor(
                "SecondDimension/UI/EnemyCutoutClean075", Color.white);
            if (_enemyArtCutout090.HasProperty("_AlphaFloor"))
                _enemyArtCutout090.SetFloat("_AlphaFloor", 0.025f);
            if (_enemyArtCutout090.HasProperty("_AlphaFeather"))
                _enemyArtCutout090.SetFloat("_AlphaFeather", 0.10f);
            _mystic = MaterialFor("Unlit/Color", Cyan);
            _shadowDisc = Disc(1f, 28);
            if (_stone.HasProperty("_Glossiness")) _stone.SetFloat("_Glossiness", 0.14f);
            if (_darkStone.HasProperty("_Glossiness")) _darkStone.SetFloat("_Glossiness", 0.07f);
            if (_brass.HasProperty("_Metallic")) _brass.SetFloat("_Metallic", 0.42f);
        }

        private void CreateCamera()
        {
            var cameraObject = NewObject("Battle 3D Perspective Camera", _worldRoot.transform);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = false;
            _camera.fieldOfView = 46f;
            _camera.nearClipPlane = 0.10f;
            _camera.farClipPlane = 140f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.008f, 0.017f, 0.030f, 1f);
            _camera.cullingMask = 1 << BattleLayer;
            _camera.depth = 4f;
            _camera.allowHDR = true;
            _camera.allowMSAA = true;
            _camera.useOcclusionCulling = false;
            _widePosition = new Vector3(0f, 16.8f, -42f);
            cameraObject.transform.position = _widePosition;
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 1.8f, 0f) - _widePosition, Vector3.up);
            _wideRotation = cameraObject.transform.rotation;
        }

        private void CreateLighting()
        {
            var keyObject = NewObject("Battle 3D Moon Key", _worldRoot.transform);
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.72f, 0.84f, 1f, 1f);
            key.intensity = 1.05f;
            key.cullingMask = 1 << BattleLayer;
            keyObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);

            var rimObject = NewObject("Battle 3D Brass Rim", _worldRoot.transform);
            var rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Point;
            rim.color = new Color(1f, 0.52f, 0.20f, 1f);
            rim.intensity = 4.1f;
            rim.range = 32f;
            rim.cullingMask = 1 << BattleLayer;
            rimObject.transform.position = new Vector3(0f, 5.8f, 9.8f);
        }

        private void CreateEnvironment()
        {
            var environment = NewObject("Tactical Gateworks Perspective Environment 020", _worldRoot.transform);
            CreateCameraLockedBackdrop();

            var floor = NewObject("Twenty Union Gateworks Arena Floor 020", environment.transform);
            floor.AddComponent<MeshFilter>().sharedMesh = Disc(BattleUnionDeploymentPlanning020.ArenaRadius, 88);
            floor.AddComponent<MeshRenderer>().sharedMaterial = _darkStone;
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            GroundRing(environment.transform, 7.2f, new Color(0.20f, 0.55f, 0.65f, 0.38f), 0.055f);
            GroundRing(environment.transform, 12.6f, new Color(0.62f, 0.40f, 0.13f, 0.42f), 0.075f);
            GroundRing(environment.transform, 17.1f, new Color(0.35f, 0.42f, 0.46f, 0.48f), 0.11f);

            Pillar(environment.transform, new Vector3(-13.9f, 2.65f, 9.2f), 5.3f, 0.82f);
            Pillar(environment.transform, new Vector3(13.9f, 2.65f, 9.2f), 5.3f, 0.82f);
            Pillar(environment.transform, new Vector3(-16.1f, 1.9f, 1.4f), 3.8f, 0.58f);
            Pillar(environment.transform, new Vector3(16.1f, 1.9f, 1.4f), 3.8f, 0.58f);
            Arch(environment.transform, new Vector3(0f, 0f, 10.8f), 12.4f, 7.2f,
                new Color(0.48f, 0.57f, 0.63f, 0.72f));
            Arch(environment.transform, new Vector3(0f, 0.03f, -0.8f), 17f, 2.8f,
                new Color(0.45f, 0.31f, 0.13f, 0.38f));
            AmbientDust(environment.transform);
        }

        private void CreateCameraLockedBackdrop()
        {
            if (_camera == null) return;

            // BG_TACTICAL_GATEWORKS_020 is a complete, perspective-composed camera
            // painting: its lower half already depicts the arena floor. Mounting that
            // sprite on the world's XY plane at z=18.2 turned the painted floor into a
            // literal vertical wall whenever the battle camera moved. Keep the painting
            // camera-locked at the far clip instead. The procedural XZ disc, rings,
            // pillars, actors, and VFX remain true world geometry in front of it.
            var item = NewObject("Tactical Gateworks Authored Far Vista 020", _camera.transform);
            _environmentBackdropRenderer = item.AddComponent<SpriteRenderer>();
            _environmentBackdropRenderer.color = new Color(0.56f, 0.62f, 0.70f, 0.76f);
            _environmentBackdropRenderer.sortingOrder = -200;
            RefreshCameraLockedBackdrop(string.Empty);
        }

        private void RefreshCameraLockedBackdrop(string battleId)
        {
            if (_camera == null || _environmentBackdropRenderer == null) return;
            if (!M1VisualAssets.TryResolveBattleBackdrop(
                    battleId,
                    out var sprite,
                    out var resourceKey083) ||
                sprite == null)
            {
                ActiveBackdropResourceKey083 = string.Empty;
                _environmentBackdropRenderer.sprite = null;
                _environmentBackdropRenderer.enabled = false;
                return;
            }

            ActiveBackdropResourceKey083 = resourceKey083;
            _environmentBackdropRenderer.enabled = true;
            _environmentBackdropRenderer.sprite = sprite;

            var distance = Mathf.Min(120f, Mathf.Max(20f, _camera.farClipPlane - 8f));
            var backdrop = _environmentBackdropRenderer.transform;
            backdrop.localPosition = new Vector3(0f, 0f, distance);
            backdrop.localRotation = Quaternion.identity;
            var visibleHeight = 2f * distance *
                                Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var visibleWidth = visibleHeight * Mathf.Max(0.01f, _camera.aspect);
            var scale = Mathf.Max(
                visibleHeight / Mathf.Max(0.01f, sprite.bounds.size.y),
                visibleWidth / Mathf.Max(0.01f, sprite.bounds.size.x));
            backdrop.localScale = Vector3.one * scale;
        }

        private void Pillar(Transform parent, Vector3 position, float height, float radius)
        {
            var pillar = Primitive(PrimitiveType.Cylinder, "Carved Gateworks Pillar", parent, _stone);
            pillar.transform.position = position;
            pillar.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            var crown = Primitive(PrimitiveType.Cylinder, "Pillar Brass Crown", parent, _brass);
            crown.transform.position = position + Vector3.up * height * 0.49f;
            crown.transform.localScale = new Vector3(radius * 1.25f, 0.12f, radius * 1.25f);
        }

        private void Arch(Transform parent, Vector3 position, float radius, float height, Color color)
        {
            var item = NewObject("Layered Stone Gate Arch", parent);
            item.transform.position = position;
            var line = item.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 25;
            line.widthMultiplier = 0.24f;
            line.numCapVertices = 4;
            line.sharedMaterial = _line;
            line.startColor = color;
            line.endColor = color;
            for (var index = 0; index < line.positionCount; index++)
            {
                var angle = Mathf.PI - Mathf.PI * index / (line.positionCount - 1);
                line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius,
                    height + Mathf.Sin(angle) * height, 0f));
            }
        }

        private void AmbientDust(Transform parent)
        {
            var item = NewObject("Tactical Gateworks Ambient Dust 020", parent);
            item.transform.position = new Vector3(0f, 1.1f, 1f);
            var particles = item.AddComponent<ParticleSystem>();
            _particles.Add(particles);
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.76f, 0.62f, 0.42f, 0.18f),
                new Color(0.43f, 0.65f, 0.73f, 0.25f));
            main.maxParticles = 220;
            var emission = particles.emission;
            emission.rateOverTime = 18f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(32f, 4.6f, 20f);
            item.GetComponent<ParticleSystemRenderer>().sharedMaterial = _particle;
            particles.Play();
        }

        private void RebuildForces(M2BattleView battle, BattleUnionDeploymentPlan020 deployment)
        {
            if (_forcesRoot != null) _forcesRoot.SetActive(false);
            ReleaseEnemyArtLeases090();
            _actors.Clear();
            _unions.Clear();
            _unionViews.Clear();
            _stagedPlayerUnionId = string.Empty;
            _stagedEnemyUnionId = string.Empty;
            if (_forcesRoot != null)
            {
                Destroy(_forcesRoot);
            }
            _forcesRoot = NewObject("Authoritative Twenty Union View Formations 020", _worldRoot.transform);
            CreateCapacitySlotMarkers(deployment);
            var overviewScale = OverviewScaleFor(deployment);
            for (var index = 0; index < deployment.Slots.Count; index++)
            {
                var slot = deployment.Slots[index];
                var union = ResolveUnionView(battle, slot);
                if (union != null) AddUnion(union, slot, overviewScale);
            }
        }

        private void AddUnion(
            M2BattleUnionView union,
            BattleUnionDeploymentSlot020 slot,
            float overviewScale)
        {
            if (union == null || slot == null) return;
            var unionId = union.UnionId ?? string.Empty;
            var enemy = slot.Enemy;
            var list = new List<ActorView>();
            _unions[unionId] = list;
            var formationRoot = NewObject(
                (enemy ? "Enemy" : "Player") + " Tactical Formation Root 020 · " +
                slot.SideOrdinal.ToString("D2") + " · " + unionId,
                _forcesRoot.transform).transform;
            formationRoot.position = slot.Anchor;
            formationRoot.localScale = Vector3.one * overviewScale;

            var unionView = CreateTacticalStandard020(union, slot, formationRoot, list, overviewScale);
            _unionViews[unionId] = unionView;
            var members = union.Members ?? Array.Empty<M2BattleMemberView>();
            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                if (member == null) continue;
                var localPosition = MemberFormationPosition020(
                    members.Count,
                    index,
                    enemy,
                    StringComparer.Ordinal.Equals(member.MemberId, union.LeaderMemberId));
                var actor = CreateActor(
                    unionId,
                    member,
                    enemy,
                    formationRoot,
                    localPosition,
                    slot.Anchor + localPosition * overviewScale,
                    index);
                list.Add(actor);
                if (!string.IsNullOrWhiteSpace(actor.MemberId)) _actors[actor.MemberId] = actor;
            }
            FormationRail(formationRoot, list, enemy);
        }

        private ActorView CreateActor(
            string unionId,
            M2BattleMemberView member,
            bool enemy,
            Transform formationRoot,
            Vector3 localPosition,
            Vector3 sortingPosition,
            int ordinal)
        {
            var root = NewObject((enemy ? "Enemy" : "Guild") + " Battle Actor " + member.MemberId,
                formationRoot);
            root.transform.localPosition = localPosition;
            var equipmentProfile = M2EquipmentVisualPolicy018.Resolve(member.EquipmentTags);
            CreateEquipmentSockets(root.transform, equipmentProfile);
            var billboard = NewObject("Perspective Cutout Billboard", root.transform);
            var pose = NewObject("Event Driven Pose", billboard.transform);
            var spriteNode = NewObject("Premium Character Cutout", pose.transform);
            var renderer = spriteNode.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 80 - Mathf.RoundToInt(sortingPosition.z * 3f) + ordinal;
            renderer.flipX = enemy;
            if (enemy && !string.IsNullOrWhiteSpace(member.EnemyArtBaseId090) &&
                _enemyArtCutout090 != null)
                renderer.sharedMaterial = _enemyArtCutout090;

            ResolveSprites(
                member,
                enemy,
                out var idle,
                out var action,
                out var idleEnemyArtLease090,
                out var actionEnemyArtLease090);
            if (idleEnemyArtLease090 != null)
                _enemyArtLeases090.Add(idleEnemyArtLease090);
            if (actionEnemyArtLease090 != null)
                _enemyArtLeases090.Add(actionEnemyArtLease090);
            var height = enemy
                ? EnemyHeight * EnemyArt700Runtime090.RelativeScale090(
                    member.EnemyArtBaseId090)
                : GuildHeight;
            renderer.sprite = idle;
            renderer.color = new Color(1f - (enemy ? ordinal * 0.04f : 0f),
                1f - (enemy ? ordinal * 0.08f : 0f),
                1f - (enemy ? ordinal * 0.08f : 0f), member.Downed ? 0.38f : 1f);
            FitGroundedActorSprite090(spriteNode.transform, idle, height);
            Shadow(root.transform, enemy);
            var ring = ActorRing(root.transform, enemy, member.Downed);

            var view = new ActorView
            {
                MemberId = member.MemberId ?? string.Empty,
                UnionId = unionId,
                Enemy = enemy,
                Root = root.transform,
                Billboard = billboard.transform,
                Pose = pose.transform,
                SpriteNode = spriteNode.transform,
                Renderer = renderer,
                IdleSprite = idle,
                ActionSprite = action ?? idle,
                Ring = ring,
                LocalHome = localPosition,
                Height = height,
                Downed = member.Downed,
                EquipmentVisualFamily = equipmentProfile.WeaponFamilyId,
                EquipmentAnimatorSet = equipmentProfile.AnimatorSetId,
                ArmorVisualFamily = equipmentProfile.ArmorFamilyId,
                ArmorMeshSet = equipmentProfile.ArmorMeshSetId
            };
            if (member.Downed)
            {
                view.Pose.localRotation = Quaternion.Euler(0f, 0f, enemy ? -16f : 16f);
                view.Pose.localPosition = new Vector3(0f, -0.42f, 0f);
            }
            return view;
        }

        private UnionView CreateTacticalStandard020(
            M2BattleUnionView union,
            BattleUnionDeploymentSlot020 slot,
            Transform formationRoot,
            List<ActorView> actors,
            float overviewScale)
        {
            var side = slot.Enemy ? "Enemy" : "Player";
            var root = NewObject(
                side + " Tactical Union Standard 020 · " + slot.SideOrdinal.ToString("D2") + " · " + slot.UnionId,
                _forcesRoot.transform);
            root.transform.position = slot.Anchor;

            var outward = slot.Enemy ? 1.45f : -1.45f;
            var pole = Primitive(PrimitiveType.Cylinder, "Tactical Standard Pole 020", root.transform, _brass);
            pole.transform.localPosition = new Vector3(outward, 1.35f, 0f);
            pole.transform.localScale = new Vector3(0.055f, 1.35f, 0.055f);
            var crown = Primitive(PrimitiveType.Sphere, "Tactical Standard Crown 020", root.transform, _brass);
            crown.transform.localPosition = new Vector3(outward, 2.73f, 0f);
            crown.transform.localScale = Vector3.one * 0.16f;

            var billboard = NewObject("Tactical Standard Billboard 020 · " + slot.UnionId, root.transform).transform;
            billboard.localPosition = new Vector3(outward, 3.25f, 0f);
            var labelObject = NewObject("Tactical Union Label 020 · " + slot.UnionId, billboard);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = (slot.Enemy ? "ENEMY " : "UNION ") + slot.SideOrdinal.ToString("D2") +
                         "\n" + CompactUnionName020(slot.DisplayName);
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 72;
            label.characterSize = 0.032f;
            label.fontStyle = FontStyle.Bold;
            label.color = slot.Enemy ? Red : Cyan;
            ConfigureWorldTextMesh(label, RuntimeUi.Font, 210);

            var ring = CapacitySlotRing020(
                root.transform,
                "Occupied Tactical Union Standard Ring 020 · " + slot.UnionId,
                1.72f,
                slot.Enemy ? new Color(1f, 0.24f, 0.28f, 0.62f) :
                    new Color(0.22f, 0.88f, 1f, 0.62f),
                0.075f);
            return new UnionView
            {
                UnionId = slot.UnionId,
                Enemy = slot.Enemy,
                Slot = slot,
                FormationRoot = formationRoot,
                StandardBillboard = billboard,
                StandardLabel = label,
                StandardRing = ring,
                Actors = actors,
                OverviewScale = overviewScale
            };
        }

        private void CreateCapacitySlotMarkers(BattleUnionDeploymentPlan020 deployment)
        {
            for (var side = 0; side < 2; side++)
            {
                var enemy = side == 1;
                for (var index = 0; index < BattleUnionDeploymentPlanning020.MaxSlotsPerSide; index++)
                {
                    var ordinal = index + 1;
                    var column = index / BattleUnionDeploymentPlanning020.RowsPerColumn;
                    var row = index % BattleUnionDeploymentPlanning020.RowsPerColumn;
                    var distance = column == 0
                        ? BattleUnionDeploymentPlanning020.FrontColumnDistance
                        : BattleUnionDeploymentPlanning020.RearColumnDistance;
                    var anchor = new Vector3(
                        enemy ? distance : -distance,
                        0.018f,
                        (row - 2) * BattleUnionDeploymentPlanning020.RowSpacing);
                    var occupied = IsOccupiedSlot020(deployment, enemy, ordinal);
                    var color = enemy
                        ? new Color(1f, 0.24f, 0.28f, occupied ? 0.22f : 0.045f)
                        : new Color(0.22f, 0.88f, 1f, occupied ? 0.22f : 0.045f);
                    var marker = NewObject(
                        (enemy ? "Enemy" : "Player") + " Tactical Union Slot 020 · " + ordinal.ToString("D2"),
                        _forcesRoot.transform);
                    marker.transform.position = anchor;
                    CapacitySlotRing020(marker.transform, "Twenty Union Capacity Inlay 020", 1.88f, color,
                        occupied ? 0.050f : 0.028f);
                }
            }
        }

        private LineRenderer CapacitySlotRing020(
            Transform parent,
            string name,
            float radius,
            Color color,
            float width)
        {
            var item = NewObject(name, parent);
            item.transform.localPosition = new Vector3(0f, 0.018f, 0f);
            var line = item.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 36;
            line.widthMultiplier = width;
            line.sharedMaterial = _line;
            line.startColor = color;
            line.endColor = color;
            for (var index = 0; index < line.positionCount; index++)
            {
                var angle = Mathf.PI * 2f * index / line.positionCount;
                line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f,
                    Mathf.Sin(angle) * radius * 0.62f));
            }
            return line;
        }

        private static Vector3 MemberFormationPosition020(
            int memberCount,
            int memberIndex,
            bool enemy,
            bool leader)
        {
            var memberSpacing = enemy ? 3.15f : 1.95f;
            var spread = (memberIndex - (Math.Max(1, memberCount) - 1) * 0.5f) * memberSpacing;
            var towardCenter = enemy ? -1f : 1f;
            var depth = leader ? 0.58f : memberIndex == 0 ? 0.30f :
                memberIndex % 2 == 0 ? -0.32f : 0.08f;
            return new Vector3(towardCenter * depth, 0f, spread);
        }

        private static void CreateEquipmentSockets(
            Transform actorRoot,
            M2EquipmentVisualProfile018 profile)
        {
            if (actorRoot == null || profile == null) return;
            var sockets = NewObject("Equipment Attachment Sockets 018", actorRoot).transform;
            NewObject(profile.MainHandSocketId, sockets);
            // Keep every actor rig-compatible even when a legacy loadout lacks
            // enough slot detail to prove whether an off-hand is occupied.
            NewObject(M2EquipmentVisualPolicy018.LeftHandSocket, sockets);
            NewObject(profile.BodyArmorSocketId, sockets);
        }

        private static void ResolveSprites(
            M2BattleMemberView member,
            bool enemy,
            out Sprite idle,
            out Sprite action,
            out EnemyArt700SpriteLease090 idleEnemyArtLease090,
            out EnemyArt700SpriteLease090 actionEnemyArtLease090)
        {
            idle = null;
            action = null;
            idleEnemyArtLease090 = null;
            actionEnemyArtLease090 = null;
            if (enemy && !string.IsNullOrWhiteSpace(member.EnemyArtBaseId090) &&
                !string.IsNullOrWhiteSpace(member.EnemyArtVariantId090))
            {
                EnemyArt700Runtime090.TryAcquireSprite090(
                    member.EnemyArtBaseId090,
                    member.EnemyArtVariantId090,
                    EnemyArt700Pose090.Idle,
                    out idle,
                    out idleEnemyArtLease090,
                    out _);
                EnemyArt700Runtime090.TryAcquireSprite090(
                    member.EnemyArtBaseId090,
                    member.EnemyArtVariantId090,
                    EnemyArt700Pose090.Attack,
                    out action,
                    out actionEnemyArtLease090,
                    out _);
            }
            if (enemy && idle == null)
                M1VisualAssets.TryResolveEnemyBattleStandee(member.MemberId, out idle, out _);
            else
            {
                if (!enemy)
                {
                    M1VisualAssets.TryResolveBattleStandee(member.MemberId, member.VisualSeed, member.RaceId,
                        member.PortraitAuthorityId, out idle, out _);
                    if (idle == null)
                        M1VisualAssets.TryResolvePortrait(member.MemberId, member.VisualSeed, member.RaceId,
                            member.PortraitAuthorityId, out idle, out _);
                }
            }
            if (action == null)
                M1VisualAssets.TryResolveBattleActionPose(
                    member.MemberId,
                    member.VisualSeed,
                    member.RaceId,
                    member.PortraitAuthorityId,
                    out action,
                    out _);
            if (action == null) action = idle;
        }

        private void Shadow(Transform parent, bool enemy)
        {
            var item = NewObject("Perspective Ground Shadow", parent);
            item.AddComponent<MeshFilter>().sharedMesh = _shadowDisc;
            item.AddComponent<MeshRenderer>().sharedMaterial = enemy ? _enemyShadow : _guildShadow;
            item.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            item.transform.localScale = new Vector3(1.15f, 1f, 0.58f);
        }

        private LineRenderer ActorRing(Transform parent, bool enemy, bool downed)
        {
            var item = NewObject("Union Formation Ground Mark", parent);
            item.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            var line = item.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 30;
            line.widthMultiplier = downed ? 0.025f : 0.055f;
            line.sharedMaterial = _line;
            var color = enemy ? new Color(1f, 0.24f, 0.28f, downed ? 0.18f : 0.64f) :
                new Color(0.22f, 0.88f, 1f, downed ? 0.18f : 0.68f);
            line.startColor = color;
            line.endColor = color;
            for (var index = 0; index < line.positionCount; index++)
            {
                var angle = Mathf.PI * 2f * index / line.positionCount;
                line.SetPosition(index, new Vector3(Mathf.Cos(angle) * 0.82f, 0f,
                    Mathf.Sin(angle) * 0.48f));
            }
            return line;
        }

        private void FormationRail(Transform formationRoot, IReadOnlyList<ActorView> actors, bool enemy)
        {
            if (actors == null || actors.Count < 2) return;
            var item = NewObject((enemy ? "Enemy" : "Guild") + " Union Formation Rail 020", formationRoot);
            var line = item.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = actors.Count;
            line.widthMultiplier = 0.035f;
            line.sharedMaterial = _line;
            var color = enemy ? new Color(0.90f, 0.16f, 0.22f, 0.30f) :
                new Color(0.20f, 0.78f, 0.90f, 0.32f);
            line.startColor = color;
            line.endColor = color;
            for (var index = 0; index < actors.Count; index++)
                line.SetPosition(index, actors[index].LocalHome + Vector3.up * 0.035f);
        }

        private IEnumerator MoveCameraFor(
            Battle3DPresentationDirective directive,
            ActorView actor,
            ActorView target,
            bool reduced,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            CameraPose(directive.Camera, actor, target, out var position, out var rotation);
            ApplyFirstHourArtCamera071(directive, actor, target, ref position, ref rotation);
            if (reduced)
            {
                _camera.transform.position = position;
                _camera.transform.rotation = rotation;
                yield break;
            }
            var duration = directive.ArtChoreography071?.CameraMoveSeconds ?? 0.22f;
            yield return MoveCamera(position, rotation, duration, speed, stopRequested);
        }

        private static void ApplyFirstHourArtCamera071(
            Battle3DPresentationDirective directive,
            ActorView actor,
            ActorView target,
            ref Vector3 position,
            ref Quaternion rotation)
        {
            var recipe = directive?.ArtChoreography071;
            if (recipe == null || recipe.CameraRecipe == Battle3DArtCameraRecipe071.None) return;

            var actorPosition = actor?.Root.position ?? new Vector3(-2f, 0f, 0f);
            var targetPosition = target?.Root.position ?? new Vector3(2f, 0f, 0f);
            var midpoint = (actorPosition + targetPosition) * 0.5f;
            var actorSide = actor != null && actor.Enemy ? 1f : -1f;
            var lateral = recipe.CameraLateralOffset;
            Vector3 look;

            switch (recipe.CameraRecipe)
            {
                case Battle3DArtCameraRecipe071.MediumThreeQuarterTrackIn:
                    position += new Vector3(actorSide * (0.42f + Mathf.Abs(lateral) * 0.18f), -0.08f, 0.72f);
                    look = midpoint + Vector3.up * 1.62f;
                    break;
                case Battle3DArtCameraRecipe071.CloseProfileLateralTrack:
                    position += new Vector3(actorSide * (1.05f + Mathf.Abs(lateral)), -0.18f, 0.38f);
                    look = targetPosition + Vector3.up * 1.58f;
                    break;
                case Battle3DArtCameraRecipe071.WideTacticalOrbit:
                    position = midpoint + new Vector3(actorSide * (6.7f + lateral), 5.1f, -12.4f);
                    look = midpoint + Vector3.up * 1.45f;
                    break;
                case Battle3DArtCameraRecipe071.OverShoulderReactionSnap:
                    // This is a visual reaction angle only. It does not set the
                    // authoritative BlindSide flag or create a tactical relationship.
                    position = actorPosition + new Vector3(actorSide * (2.5f + lateral), 3.05f, 2.85f);
                    look = Vector3.Lerp(actorPosition, targetPosition, 0.72f) + Vector3.up * 1.55f;
                    break;
                default:
                    return;
            }

            // The authoritative camera intent still establishes the shot. These
            // small semantic offsets only keep thirty different tree grammars from
            // collapsing back into the same four node-number camera moves.
            position += new Vector3(
                recipe.SemanticLateralBias * actorSide * 0.34f,
                recipe.SemanticLiftBias * 0.72f,
                -recipe.SemanticForwardBias * 0.58f);
            look += Vector3.up * recipe.SemanticLiftBias * 0.28f;

            rotation = Quaternion.LookRotation(look - position, Vector3.up);
        }

        private void CameraPose(
            Battle3DCameraIntent intent,
            ActorView actor,
            ActorView target,
            out Vector3 position,
            out Quaternion rotation)
        {
            var actorPosition = actor?.Root.position ?? new Vector3(-2f, 0f, 0f);
            var targetPosition = target?.Root.position ?? new Vector3(2f, 0f, 0f);
            var midpoint = (actorPosition + targetPosition) * 0.5f;
            var pairDistance = RequiredPairShotDistance020(actorPosition, targetPosition);
            Vector3 look;
            switch (intent)
            {
                case Battle3DCameraIntent.Union:
                    look = actorPosition + Vector3.up * 1.7f;
                    position = actorPosition + new Vector3(actor != null && actor.Enemy ? 3.7f : -3.7f, 3.5f, -7.5f);
                    break;
                case Battle3DCameraIntent.Anticipation:
                    look = midpoint + Vector3.up * 1.6f;
                    position = midpoint + new Vector3(actor != null && actor.Enemy ? 2.8f : -2.8f, 3.2f,
                        -Mathf.Max(8f, pairDistance));
                    break;
                case Battle3DCameraIntent.Tracking:
                case Battle3DCameraIntent.Support:
                    look = midpoint + Vector3.up * 1.7f;
                    position = midpoint + new Vector3(0f, 3.7f, -Mathf.Max(9.1f, pairDistance));
                    break;
                case Battle3DCameraIntent.Impact:
                    look = targetPosition + Vector3.up * 1.55f;
                    position = targetPosition + new Vector3(target != null && target.Enemy ? 2.1f : -2.1f,
                        2.9f, -Mathf.Max(7.8f, pairDistance));
                    break;
                case Battle3DCameraIntent.BlindSide:
                    // A rear-quarter angle is reserved for authoritative CMD_FLANK.
                    look = targetPosition + Vector3.up * 1.45f;
                    position = targetPosition + new Vector3(target != null && target.Enemy ? 4.8f : -4.8f,
                        3.1f, 3.6f);
                    break;
                case Battle3DCameraIntent.Hero:
                    look = targetPosition + Vector3.up * 2f;
                    position = targetPosition + new Vector3(0f, 3.4f, -6.3f);
                    break;
                case Battle3DCameraIntent.Result:
                    position = _widePosition + new Vector3(0f, 1.4f, -2f);
                    look = TacticalOverviewLookPoint020();
                    break;
                default:
                    position = _widePosition;
                    rotation = _wideRotation;
                    return;
            }
            rotation = Quaternion.LookRotation(look - position, Vector3.up);
        }

        private IEnumerator Establish(Func<float> speed, Func<bool> stopRequested)
        {
            var end = _widePosition + new Vector3(0f, -0.25f, 1.1f);
            yield return MoveCamera(end, Quaternion.LookRotation(new Vector3(0f, 1.8f, 0.6f) - end),
                0.48f, speed, stopRequested);
            yield return Wait(0.14f, speed, stopRequested);
            yield return MoveCamera(_widePosition, _wideRotation, 0.20f, speed, stopRequested);
        }

        private IEnumerator PlayFirstHourArtChoreography071(
            Battle3DPresentationDirective directive,
            ActorView actor,
            ActorView target,
            bool reducedMotion,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            var recipe = directive?.ArtChoreography071;
            if (recipe == null) yield break;

            var actorPoint = actor?.Root.position + Vector3.up * 1.72f ??
                             target?.Root.position + Vector3.up * 1.72f ?? Vector3.up * 1.72f;
            var targetPoint = target?.Root.position + Vector3.up * 1.62f ?? actorPoint;
            var focusPoint = actorPoint;
            var color = Color.HSVToRGB(
                recipe.SemanticPaletteHue01,
                recipe.SemanticPaletteSaturation,
                recipe.SemanticPaletteValue);
            color.a = 1f;
            StartCoroutine(WorldText(
                recipe.PerformanceCallout + "\n" + recipe.DisplayName.ToUpperInvariant(),
                focusPoint + Vector3.up * 1.95f,
                color,
                Mathf.Clamp(recipe.DurationSeconds * 0.70f, 0.48f, 0.92f),
                speed,
                stopRequested,
                0.042f));
            StageFirstHourArtVfx071(
                recipe, actorPoint, targetPoint, color, speed, stopRequested);

            if (reducedMotion || actor == null)
            {
                yield return Wait(Mathf.Clamp(recipe.WindupSeconds * 0.72f, 0.08f, 0.18f), speed, stopRequested);
                yield break;
            }

            SetAction(actor, true);
            var rootStart = actor.Root.position;
            var posePosition = actor.Pose.localPosition;
            var poseRotation = actor.Pose.localRotation;
            var poseScale = actor.Pose.localScale;
            var toTarget = (target?.Root.position ??
                            rootStart + (actor.Enemy ? Vector3.left : Vector3.right) * 3f) - rootStart;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) toTarget = actor.Enemy ? Vector3.left : Vector3.right;
            toTarget.Normalize();
            var side = Vector3.Cross(Vector3.up, toTarget).normalized;
            var signatureSign = (recipe.MotionVisualSeed & 1u) == 0u ? -1f : 1f;
            var magnitude = recipe.MotionMagnitude;
            var elapsed = 0f;

            while (elapsed < recipe.WindupSeconds && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                var t = Mathf.Clamp01(elapsed / recipe.WindupSeconds);
                var ease = Mathf.SmoothStep(0f, 1f, t);
                var pulse = Mathf.Sin(t * Mathf.PI);
                switch (recipe.MotionRecipe)
                {
                    case Battle3DArtMotionRecipe071.SetAdvancePrimaryRecover:
                        actor.Root.position = rootStart + toTarget * magnitude * ease;
                        actor.Pose.localRotation = poseRotation * Quaternion.Euler(
                            0f, 0f, -signatureSign * recipe.MotionTiltDegrees * pulse);
                        break;
                    case Battle3DArtMotionRecipe071.FeintChainSecondaryRecover:
                        actor.Root.position = rootStart + side * signatureSign * magnitude * Mathf.Sin(t * Mathf.PI * 2f);
                        actor.Pose.localRotation = poseRotation * Quaternion.Euler(0f, 0f,
                            signatureSign * Mathf.Sin(t * Mathf.PI * 2f) * (recipe.MotionTiltDegrees + 4f));
                        break;
                    case Battle3DArtMotionRecipe071.CommitExpandFullRecover:
                        actor.Root.position = rootStart + toTarget * magnitude * 0.48f * ease;
                        actor.Pose.localScale = poseScale * (1f + pulse * (0.08f + magnitude * 0.08f));
                        actor.Pose.localRotation = poseRotation * Quaternion.Euler(
                            0f, 0f, -signatureSign * (recipe.MotionTiltDegrees + 7f) * ease);
                        break;
                    case Battle3DArtMotionRecipe071.ReadReactCounterRecover:
                        actor.Root.position = rootStart - toTarget * magnitude * pulse + side * signatureSign * magnitude * 0.28f * pulse;
                        actor.Pose.localRotation = poseRotation * Quaternion.Euler(
                            0f, 0f, signatureSign * (recipe.MotionTiltDegrees + 9f) * pulse);
                        break;
                }

                // Tree semantics layer over the node's four-beat grammar. Bow Arts
                // lean and lift into release, heavy weapons drive forward, mystics
                // open around the casting axis, and tactical Arts work laterally.
                // The root and pose are restored below, so this remains visual only.
                var semanticWave = Mathf.Sin(t * Mathf.PI *
                                             (1f + (recipe.TreeOrdinal % 3) * 0.5f));
                actor.Root.position +=
                    toTarget * recipe.SemanticForwardBias * ease +
                    side * signatureSign * recipe.SemanticLateralBias * semanticWave +
                    Vector3.up * recipe.SemanticLiftBias * pulse;
                actor.Pose.localRotation *= Quaternion.Euler(
                    recipe.SemanticLiftBias * 18f * pulse,
                    signatureSign * recipe.SemanticPoseTurnDegrees * semanticWave,
                    0f);
                yield return null;
            }

            if (actor.Root != null) actor.Root.position = rootStart;
            if (actor.Pose != null)
            {
                actor.Pose.localPosition = posePosition;
                actor.Pose.localRotation = poseRotation;
                actor.Pose.localScale = poseScale;
            }
            yield return Wait(recipe.RecoveryAccentSeconds * 0.22f, speed, stopRequested);
        }

        private void StageFirstHourArtVfx071(
            Battle3DArtChoreography071 recipe,
            Vector3 origin,
            Vector3 impact,
            Color color,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            if (recipe == null) return;
            var support = recipe.SemanticFamily == Battle3DArtSemanticFamily071.Restoration ||
                          recipe.SemanticFamily == Battle3DArtSemanticFamily071.Warding ||
                          recipe.SemanticFamily == Battle3DArtSemanticFamily071.GuardSupport ||
                          recipe.SemanticFamily == Battle3DArtSemanticFamily071.TacticalSupport;
            var point = support ? impact : Vector3.Lerp(origin, impact, 0.72f);
            switch (recipe.VfxRecipe)
            {
                case Battle3DArtVfxRecipe071.FocusedPrimary:
                    Burst(point, color, recipe.SemanticBurstCount);
                    break;
                case Battle3DArtVfxRecipe071.SecondaryAccent:
                    Burst(point + Vector3.left * recipe.SemanticTraceRadius * 0.24f, color,
                        Mathf.Max(10, recipe.SemanticBurstCount / 2));
                    Burst(point + Vector3.right * recipe.SemanticTraceRadius * 0.24f,
                        Color.Lerp(color, Color.white, 0.32f),
                        Mathf.Max(10, recipe.SemanticBurstCount / 2));
                    break;
                case Battle3DArtVfxRecipe071.ExpandingPattern:
                    Burst(point, color, recipe.SemanticBurstCount + 12);
                    break;
                case Battle3DArtVfxRecipe071.ReactionBurst:
                    Burst(point, color, recipe.SemanticBurstCount + 7);
                    break;
            }

            StartCoroutine(SpriteEffect(
                recipe.EffectSpriteId,
                point,
                3.35f + recipe.SemanticTraceRadius * 0.85f + recipe.NodeIndex * 0.12f,
                color,
                recipe.RecoveryAccentSeconds + 0.18f + recipe.NodeIndex * 0.025f,
                speed,
                stopRequested));
            StartCoroutine(SemanticArtTrace071(
                recipe, origin, impact, color,
                recipe.RecoveryAccentSeconds + 0.28f + recipe.NodeIndex * 0.035f,
                speed, stopRequested));
        }

        private IEnumerator SemanticArtTrace071(
            Battle3DArtChoreography071 recipe,
            Vector3 origin,
            Vector3 impact,
            Color color,
            float duration,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            if (recipe == null || recipe.TraceRecipe == Battle3DArtTraceRecipe071.None) yield break;
            var item = NewObject(
                "Battle 3D Semantic Art Trace 071 · " + recipe.SemanticPerformanceKey,
                _worldRoot.transform);
            var travelTrace = recipe.TraceRecipe == Battle3DArtTraceRecipe071.ReachLine ||
                              recipe.TraceRecipe == Battle3DArtTraceRecipe071.ProjectileFlight;
            item.transform.position = travelTrace ? origin : impact;
            item.transform.rotation = travelTrace
                ? Quaternion.identity
                : Quaternion.Euler(0f, 0f, recipe.SemanticTraceRotationDegrees);

            var layerCount = Mathf.Max(
                recipe.SemanticTraceLayerCount,
                recipe.SemanticProjectileCount);
            var lines = new List<LineRenderer>(layerCount);
            for (var layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                var lineObject = NewObject(
                    recipe.SemanticStyle + " Trace Layer " + (layerIndex + 1),
                    item.transform);
                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.loop = SemanticTraceLoops071(recipe.TraceRecipe);
                line.sharedMaterial = _line;
                line.numCapVertices = 3;
                line.numCornerVertices = 2;
                var points = BuildSemanticTracePoints071(
                    recipe, layerIndex, origin, impact);
                line.positionCount = points.Length;
                line.SetPositions(points);
                line.widthMultiplier = 0.045f + recipe.NodeIndex * 0.008f + layerIndex * 0.004f;
                line.startColor = color;
                line.endColor = Color.Lerp(color, Color.white, 0.24f);
                lines.Add(line);
            }

            var elapsed = 0f;
            while (elapsed < duration && item != null && !Stopped(stopRequested))
            {
                var delta = Delta(speed);
                elapsed += delta;
                var t = Mathf.Clamp01(elapsed / duration);
                var alpha = Mathf.Sin(t * Mathf.PI) * (_reducedFlash ? 0.52f : 0.92f);
                item.transform.localScale = travelTrace
                    ? Vector3.one
                    : Vector3.one *
                      (0.82f + Mathf.SmoothStep(0f, 1f, t) *
                       (0.16f + recipe.NodeIndex * 0.035f));
                item.transform.Rotate(
                    0f,
                    recipe.TraceRecipe == Battle3DArtTraceRecipe071.WardDome ? delta * 32f : 0f,
                    travelTrace ? 0f : delta * (18f + recipe.TreeOrdinal % 7 * 6f),
                    Space.Self);
                for (var index = 0; index < lines.Count; index++)
                {
                    if (lines[index] == null) continue;
                    var lineColor = new Color(color.r, color.g, color.b,
                        alpha * (1f - index * 0.12f));
                    lines[index].startColor = lineColor;
                    var endRgb = Color.Lerp(color, Color.white, 0.24f);
                    lines[index].endColor = new Color(
                        endRgb.r, endRgb.g, endRgb.b, lineColor.a);
                }
                yield return null;
            }
            if (item != null) Destroy(item);
        }

        private static Vector3[] BuildSemanticTracePoints071(
            Battle3DArtChoreography071 recipe,
            int layerIndex,
            Vector3 origin,
            Vector3 impact)
        {
            var count = recipe.SemanticTracePointCount + layerIndex;
            var points = new Vector3[count];
            var radius = recipe.SemanticTraceRadius * (1f + layerIndex * 0.13f);
            var layerOffset = (layerIndex - (recipe.SemanticTraceLayerCount - 1) * 0.5f) * 0.12f;
            var travel = impact - origin;
            var travelDirection = travel.sqrMagnitude > 0.001f ? travel.normalized : Vector3.right;
            var travelSide = Vector3.Cross(Vector3.up, travelDirection).normalized;

            for (var index = 0; index < count; index++)
            {
                var t = count <= 1 ? 0f : index / (float)(count - 1);
                var angle = t * Mathf.PI * 2f;
                switch (recipe.TraceRecipe)
                {
                    case Battle3DArtTraceRecipe071.EdgeArc:
                        points[index] = new Vector3(
                            Mathf.Cos(Mathf.Lerp(-1.15f, 1.15f, t)) * radius,
                            Mathf.Sin(Mathf.Lerp(-1.15f, 1.15f, t)) * radius + 0.12f,
                            layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.WeightedShock:
                        points[index] = new Vector3(
                            Mathf.Lerp(-radius, radius, t),
                            Mathf.Abs(Mathf.Sin(t * Mathf.PI * (2f + recipe.NodeIndex))) * radius * 0.46f - 0.22f,
                            layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.ReachLine:
                    case Battle3DArtTraceRecipe071.ProjectileFlight:
                        points[index] = travel * t + Vector3.up *
                            Mathf.Sin(t * Mathf.PI) * (0.28f + recipe.NodeIndex * 0.09f) +
                            travelSide * layerOffset;
                        break;
                    case Battle3DArtTraceRecipe071.TwinTrail:
                        points[index] = new Vector3(
                            Mathf.Lerp(-radius, radius, t),
                            Mathf.Sin(t * Mathf.PI * (2f + layerIndex)) * radius * 0.54f,
                            layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.GuardPlate:
                        points[index] = new Vector3(
                            Mathf.Cos(angle) * radius * 0.78f,
                            Mathf.Sin(angle) * radius,
                            Mathf.Abs(Mathf.Cos(angle)) * 0.16f + layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.ImpactRing:
                        points[index] = new Vector3(
                            Mathf.Cos(angle) * radius,
                            Mathf.Sin(angle) * radius,
                            layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.ArcaneOrbit:
                        points[index] = new Vector3(
                            Mathf.Cos(angle * (1f + recipe.NodeIndex * 0.25f)) * radius * t,
                            Mathf.Sin(angle * (1f + recipe.NodeIndex * 0.25f)) * radius * t,
                            layerOffset + (t - 0.5f) * 0.32f);
                        break;
                    case Battle3DArtTraceRecipe071.ToolDiagram:
                        points[index] = new Vector3(
                            Mathf.Lerp(-radius, radius, t),
                            ((index + recipe.NodeIndex) % 2 == 0 ? -1f : 1f) * radius * 0.42f,
                            layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.RelicConvergence:
                        var spoke = (index % 2 == 0 ? 1f : 0.42f) * radius;
                        points[index] = new Vector3(
                            Mathf.Cos(angle * 2.5f) * spoke,
                            Mathf.Sin(angle * 2.5f) * spoke,
                            layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.ElementalSpiral:
                        var spiralRadius = radius * (0.22f + t * 0.78f);
                        points[index] = new Vector3(
                            Mathf.Cos(angle * (1.5f + recipe.NodeIndex * 0.25f)) * spiralRadius,
                            Mathf.Sin(angle * (1.5f + recipe.NodeIndex * 0.25f)) * spiralRadius,
                            layerOffset + Mathf.Sin(angle) * 0.15f);
                        break;
                    case Battle3DArtTraceRecipe071.HealingBloom:
                        var petal = radius * Mathf.Sin(angle * (2f + recipe.NodeIndex));
                        points[index] = new Vector3(
                            Mathf.Cos(angle) * petal,
                            Mathf.Sin(angle) * petal,
                            layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.WardDome:
                        var domeAngle = Mathf.Lerp(0f, Mathf.PI, t);
                        points[index] = new Vector3(
                            Mathf.Cos(domeAngle) * radius,
                            Mathf.Sin(domeAngle) * radius,
                            layerOffset + Mathf.Sin(domeAngle) * radius * 0.24f);
                        break;
                    case Battle3DArtTraceRecipe071.TacticalLine:
                        points[index] = new Vector3(
                            Mathf.Lerp(-radius, radius, t),
                            Mathf.Abs(t - 0.5f) * radius * 0.52f,
                            layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.TacticalMarker:
                        points[index] = new Vector3(
                            Mathf.Cos(angle) * radius * (index % 2 == 0 ? 1f : 0.38f),
                            Mathf.Sin(angle) * radius * (index % 2 == 0 ? 1f : 0.38f),
                            layerOffset);
                        break;
                    case Battle3DArtTraceRecipe071.TacticalWave:
                        points[index] = new Vector3(
                            Mathf.Lerp(-radius, radius, t),
                            Mathf.Sin(t * Mathf.PI * (2f + recipe.NodeIndex)) * radius * 0.38f,
                            layerOffset);
                        break;
                    default:
                        points[index] = Vector3.zero;
                        break;
                }
            }
            return points;
        }

        private static bool SemanticTraceLoops071(Battle3DArtTraceRecipe071 recipe) =>
            recipe == Battle3DArtTraceRecipe071.GuardPlate ||
            recipe == Battle3DArtTraceRecipe071.ImpactRing ||
            recipe == Battle3DArtTraceRecipe071.RelicConvergence ||
            recipe == Battle3DArtTraceRecipe071.HealingBloom ||
            recipe == Battle3DArtTraceRecipe071.TacticalMarker;

        private IEnumerator Strike(
            ActorView actor,
            ActorView target,
            Battle3DPresentationDirective directive,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            if (actor == null && target == null) { yield return Wait(0.22f, speed, stopRequested); yield break; }
            SetAction(actor, true);
            var targetPosition = target?.Root.position ?? actor.Root.position + (actor.Enemy ? Vector3.left : Vector3.right) * 4f;
            var origin = actor?.Root.position ?? targetPosition;
            var direction = targetPosition - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.right;
            direction.Normalize();
            var strong = directive.Action == Battle3DAction.CombatArt ||
                         directive.Action == Battle3DAction.TacticalStrike;
            // Stop outside the visible silhouettes rather than at a fixed center
            // distance. The tutorial Gnawer is much wider than a humanoid cutout;
            // the old 2.05-unit stop put both bodies inside one another.
            var contactClearance = ContactClearance020(actor, target, strong);
            var end = targetPosition - direction * contactClearance;
            if (actor != null)
            {
                // A readable combat sentence: brace away from the target, commit
                // through the approach, contact, then recover to formation.
                var windup = origin - direction * (strong ? 0.58f : 0.38f);
                yield return MoveActor(actor, origin, windup, strong ? 0.17f : 0.12f, speed, stopRequested);
                yield return MoveActor(actor, actor.Root.position, end, strong ? 0.31f : 0.25f, speed, stopRequested);
            }
            var point = (target ?? actor)?.Root.position + Vector3.up * 1.7f ?? Vector3.up * 1.7f;
            StartCoroutine(SpriteEffect("WEAPON_ARC", point, strong ? 4.8f : 3.7f, Gold,
                0.30f, speed, stopRequested));
            Burst(point, strong ? Gold : Cyan, strong ? 34 : 24);
            if (directive.DisplayAmount != 0) StartCoroutine(Amount(target ?? actor, directive.DisplayAmount, false,
                speed, stopRequested));
            if (directive.UseImpactPause)
                yield return ImpactPause(strong ? 0.09f : 0.055f, speed, stopRequested);
            if (directive.UseCameraShake) yield return Shake(strong ? 0.18f : 0.11f, 0.19f, speed, stopRequested);
            if (target != null) yield return Recoil(target, direction, 0.20f, speed, stopRequested);
            if (actor != null) yield return MoveActor(actor, actor.Root.position, ActorHome(actor), 0.29f, speed, stopRequested);
            RestoreActor(actor);
        }

        private IEnumerator Mystic(
            ActorView actor,
            ActorView target,
            Battle3DPresentationDirective directive,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            SetAction(actor, true);
            var start = actor?.Root.position + Vector3.up * 2.3f ?? new Vector3(-3f, 2.3f, 0f);
            var end = target?.Root.position + Vector3.up * 1.7f ?? new Vector3(3f, 1.7f, 0f);
            StartCoroutine(WorldText("MYSTIC ART", start + Vector3.up * 1.7f, Cyan, 0.48f, speed, stopRequested));
            yield return Wait(0.16f, speed, stopRequested);
            var orb = Primitive(PrimitiveType.Sphere, "Battle 3D Mystic Projectile", _worldRoot.transform,
                _mystic);
            orb.transform.localScale = Vector3.one * 0.28f;
            yield return MoveObject(orb.transform, start, end, 0.32f, speed, stopRequested);
            if (orb != null) Destroy(orb);
            StartCoroutine(SpriteEffect("MYSTIC_BURST", end, 5.2f, Cyan, 0.40f, speed, stopRequested));
            Burst(end, Cyan, 36);
            if (directive.DisplayAmount != 0) StartCoroutine(Amount(target ?? actor, directive.DisplayAmount, false,
                speed, stopRequested));
            if (directive.UseImpactPause) yield return ImpactPause(0.075f, speed, stopRequested);
            if (directive.UseCameraShake) yield return Shake(0.14f, 0.20f, speed, stopRequested);
            if (target != null)
            {
                var impactDirection = target.Root.position - (actor?.Root.position ?? start);
                impactDirection.y = 0f;
                if (impactDirection.sqrMagnitude < 0.01f) impactDirection = Vector3.right;
                impactDirection.Normalize();
                yield return Recoil(target, impactDirection, 0.20f, speed, stopRequested);
            }
            RestoreActor(actor);
        }

        private IEnumerator Restore(
            ActorView actor,
            ActorView target,
            Battle3DPresentationDirective directive,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            SetAction(actor, true);
            var focus = target ?? actor;
            var point = focus?.Root.position + Vector3.up * 1.7f ?? Vector3.up * 1.7f;
            var green = new Color(0.42f, 1f, 0.66f, 1f);
            StartCoroutine(SpriteEffect("RESTORATION_BLOOM", point, 3.9f, green, 0.44f, speed, stopRequested));
            Burst(point, green, 20);
            if (directive.DisplayAmount != 0) StartCoroutine(Amount(focus, directive.DisplayAmount, true, speed, stopRequested));
            yield return Pulse(focus, "RESTORE", green, 0.38f, speed, stopRequested);
            RestoreActor(actor);
        }

        private IEnumerator Guard(ActorView actor, string label, Func<float> speed, Func<bool> stopRequested)
        {
            SetAction(actor, true);
            var point = actor?.Root.position + Vector3.up * 1.7f ?? Vector3.up * 1.7f;
            StartCoroutine(SpriteEffect("GUARD_IMPACT", point, 4.3f, Cyan, 0.40f, speed, stopRequested));
            yield return Pulse(actor, label, Cyan, 0.34f, speed, stopRequested);
            RestoreActor(actor);
        }

        private IEnumerator Intercept(
            ActorView actor,
            ActorView target,
            Battle3DPresentationDirective directive,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            if (actor == null) { yield return Guard(target, "INTERCEPT", speed, stopRequested); yield break; }
            SetAction(actor, true);
            var origin = actor.Root.position;
            var destination = target == null ? origin + (actor.Enemy ? Vector3.left : Vector3.right) * 1.5f :
                Vector3.Lerp(origin, target.Root.position, 0.58f);
            yield return MoveActor(actor, origin, destination, 0.18f, speed, stopRequested);
            var point = destination + Vector3.up * 1.7f;
            StartCoroutine(SpriteEffect("GUARD_IMPACT", point, 4.6f, Gold, 0.36f, speed, stopRequested));
            Burst(point, Gold, 24);
            StartCoroutine(WorldText("INTERCEPT", point + Vector3.up * 1.9f, Gold, 0.44f, speed, stopRequested));
            if (directive.UseImpactPause) yield return ImpactPause(0.07f, speed, stopRequested);
            yield return MoveActor(actor, actor.Root.position, ActorHome(actor), 0.22f, speed, stopRequested);
            RestoreActor(actor);
        }

        private IEnumerator Reposition(
            ActorView actor,
            ActorView target,
            Battle3DPresentationDirective directive,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            if (actor == null) { yield return Wait(0.25f, speed, stopRequested); yield break; }
            var origin = actor.Root.position;
            var targetPosition = target?.Root.position ?? new Vector3(actor.Enemy ? -4.5f : 4.5f, 0f, origin.z);
            var side = targetPosition + new Vector3(actor.Enemy ? 1.4f : -1.4f, 0f,
                origin.z <= targetPosition.z ? -2.4f : 2.4f);
            var route = Trail(origin + Vector3.up * 0.18f, side + Vector3.up * 0.18f, Cyan);
            StartCoroutine(FadeLine(route, 0.68f, speed));
            SetAction(actor, true);
            yield return MoveActor(actor, origin, side, 0.36f, speed, stopRequested);
            var label = directive.IsSideStrike ? directive.TacticalCallout : "REPOSITION";
            StartCoroutine(WorldText(label, targetPosition + Vector3.up * 4f,
                directive.IsSideStrike ? Gold : Cyan, 0.72f, speed, stopRequested));
            Burst(targetPosition + Vector3.up * 1.2f, Cyan, directive.IsSideStrike ? 30 : 18);
            yield return Wait(0.20f, speed, stopRequested);
            yield return MoveActor(actor, actor.Root.position, ActorHome(actor), 0.28f, speed, stopRequested);
            RestoreActor(actor);
        }

        private IEnumerator Down(
            ActorView actor,
            Battle3DPresentationDirective directive,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            if (actor == null) { yield return Wait(0.20f, speed, stopRequested); yield break; }
            Burst(actor.Root.position + Vector3.up * 0.5f, Red, 32);
            StartCoroutine(WorldText("DOWNED", actor.Root.position + Vector3.up * 4f, Red, 0.55f, speed, stopRequested));
            if (directive.UseImpactPause) yield return ImpactPause(0.08f, speed, stopRequested);
            var startRotation = actor.Pose.localRotation;
            var endRotation = Quaternion.Euler(0f, 0f, actor.Enemy ? -70f : 70f);
            var startPosition = actor.Pose.localPosition;
            var elapsed = 0f;
            const float duration = 0.34f;
            while (elapsed < duration && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                actor.Pose.localRotation = Quaternion.Slerp(startRotation, endRotation, t);
                actor.Pose.localPosition = Vector3.Lerp(startPosition, new Vector3(0f, -0.72f, 0f), t);
                var color = actor.Renderer.color;
                color.a = Mathf.Lerp(1f, 0.36f, t);
                actor.Renderer.color = color;
                yield return null;
            }
            actor.Downed = true;
        }

        private IEnumerator Retreat(ActorView actor, Func<float> speed, Func<bool> stopRequested)
        {
            if (actor == null) { yield return Wait(0.25f, speed, stopRequested); yield break; }
            yield return MoveActor(actor, actor.Root.position,
                actor.Root.position + (actor.Enemy ? Vector3.right : Vector3.left) * 5.4f,
                0.44f, speed, stopRequested);
            var color = actor.Renderer.color;
            color.a = 0.24f;
            actor.Renderer.color = color;
            actor.Retreated = true;
        }

        private IEnumerator Result(Func<float> speed, Func<bool> stopRequested)
        {
            // The beat contract does not carry the authoritative outcome as a typed
            // field, so presentation must not infer victory or defeat from caption copy.
            StartCoroutine(WorldText("BATTLE RESOLVED", new Vector3(0f, 6.4f, 1.5f),
                Gold, 1.05f, speed, stopRequested));
            Burst(new Vector3(0f, 3.2f, 1.5f), Gold, 60);
            var resultPosition = _widePosition + new Vector3(0f, 1.4f, -2f);
            yield return MoveCamera(resultPosition,
                Quaternion.LookRotation(TacticalOverviewLookPoint020() - resultPosition),
                0.52f, speed, stopRequested);
            yield return Wait(0.40f, speed, stopRequested);
        }

        private IEnumerator ReducedMotion(
            Battle3DPresentationDirective directive,
            ActorView actor,
            ActorView target,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            var focus = target ?? actor;
            var point = focus?.Root.position + Vector3.up * 1.8f ?? Vector3.up * 1.8f;
            var color = directive.Action == Battle3DAction.Restore ? new Color(0.42f, 1f, 0.66f, 1f) : Gold;
            Burst(point, color, 12);
            if (directive.Action == Battle3DAction.Learn)
                StartCoroutine(WorldText(
                    M2BattleReadableText021.ArtXpGain(
                        directive.ArtId,
                        directive.Caption,
                        directive.DisplayAmount),
                    point + Vector3.up * 1.5f, Cyan, 0.50f, speed, stopRequested));
            else if (directive.Action == Battle3DAction.Breakthrough)
                StartCoroutine(WorldText(
                    M2BattleReadableText021.NewArtLearned(directive.ArtId, directive.Caption),
                    point + Vector3.up * 1.5f, Gold, 0.62f, speed, stopRequested));
            else if (directive.DisplayAmount != 0 && UsesHitPointAmount(directive.Action))
                StartCoroutine(Amount(focus, directive.DisplayAmount, directive.Action == Battle3DAction.Restore,
                    speed, stopRequested));
            if (directive.IsSideStrike)
                StartCoroutine(WorldText(directive.TacticalCallout, point + Vector3.up * 2f,
                    Gold, 0.50f, speed, stopRequested));
            yield return Wait(0.22f, speed, stopRequested);
        }

        private static bool UsesHitPointAmount(Battle3DAction action) =>
            action == Battle3DAction.WeaponStrike ||
            action == Battle3DAction.CombatArt ||
            action == Battle3DAction.MysticArt ||
            action == Battle3DAction.TacticalStrike ||
            action == Battle3DAction.Restore ||
            action == Battle3DAction.Intercept;

        private IEnumerator Pulse(
            ActorView actor,
            string label,
            Color color,
            float duration,
            Func<float> speed,
            Func<bool> stopRequested)
        {
            var point = actor?.Root.position + Vector3.up * 3.8f ?? new Vector3(0f, 3.8f, 0f);
            StartCoroutine(WorldText(label, point, color, duration + 0.12f, speed, stopRequested));
            Burst(point - Vector3.up * 1.8f, color, 20);
            if (actor == null) { yield return Wait(duration, speed, stopRequested); yield break; }
            var start = actor.Pose.localScale;
            var elapsed = 0f;
            while (elapsed < duration && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                var pulse = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                actor.Pose.localScale = start * Mathf.Lerp(1f, 1.09f, pulse);
                yield return null;
            }
            if (actor.Pose != null) actor.Pose.localScale = start;
        }

        private IEnumerator Recoil(ActorView actor, Vector3 direction, float duration,
            Func<float> speed, Func<bool> stopRequested)
        {
            var start = actor.Root.position;
            var end = start + direction * 0.52f;
            var elapsed = 0f;
            while (elapsed < duration && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                var t = Mathf.Clamp01(elapsed / duration);
                actor.Root.position = Vector3.Lerp(start, end, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            if (actor.Root != null) actor.Root.position = start;
        }

        private IEnumerator MoveActor(ActorView actor, Vector3 from, Vector3 to, float duration,
            Func<float> speed, Func<bool> stopRequested)
        {
            if (actor == null) yield break;
            var elapsed = 0f;
            while (elapsed < duration && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                actor.Root.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.15f;
                yield return null;
            }
            if (actor.Root != null) actor.Root.position = to;
        }

        private IEnumerator MoveObject(Transform item, Vector3 from, Vector3 to, float duration,
            Func<float> speed, Func<bool> stopRequested)
        {
            if (item == null) yield break;
            item.position = from;
            var elapsed = 0f;
            while (elapsed < duration && item != null && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                var t = Mathf.Clamp01(elapsed / duration);
                item.position = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.55f;
                yield return null;
            }
            if (item != null) item.position = to;
        }

        private IEnumerator MoveCamera(Vector3 position, Quaternion rotation, float duration,
            Func<float> speed, Func<bool> stopRequested)
        {
            if (_camera == null) yield break;
            var startPosition = _camera.transform.position;
            var startRotation = _camera.transform.rotation;
            var elapsed = 0f;
            while (elapsed < duration && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _camera.transform.position = Vector3.Lerp(startPosition, position, t);
                _camera.transform.rotation = Quaternion.Slerp(startRotation, rotation, t);
                yield return null;
            }
            if (_camera != null)
            {
                _camera.transform.position = position;
                _camera.transform.rotation = rotation;
            }
        }

        private IEnumerator Shake(float strength, float duration, Func<float> speed, Func<bool> stopRequested)
        {
            if (_camera == null || _shakeStrength <= 0.001f) yield break;
            strength *= _shakeStrength;
            var origin = _camera.transform.position;
            var elapsed = 0f;
            while (elapsed < duration && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                var fade = 1f - Mathf.Clamp01(elapsed / duration);
                var x = Mathf.Sin(elapsed * 97f) * strength * fade;
                var y = Mathf.Cos(elapsed * 79f) * strength * 0.55f * fade;
                _camera.transform.position = origin + new Vector3(x, y, 0f);
                yield return null;
            }
            if (_camera != null) _camera.transform.position = origin;
        }

        private IEnumerator Wait(float duration, Func<float> speed, Func<bool> stopRequested)
        {
            var elapsed = 0f;
            while (elapsed < duration && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                yield return null;
            }
        }

        private IEnumerator ImpactPause(float duration, Func<float> speed, Func<bool> stopRequested)
        {
            _impactPaused = true;
            var elapsed = 0f;
            try
            {
                while (elapsed < duration && !Stopped(stopRequested))
                {
                    var requested = speed == null ? 1f : speed();
                    if (requested > 0f)
                        elapsed += Time.unscaledDeltaTime * Mathf.Clamp(requested, 0.25f, 8f);
                    yield return null;
                }
            }
            finally
            {
                _impactPaused = false;
            }
        }

        private IEnumerator SpriteEffect(string effectId, Vector3 position, float height, Color color,
            float duration, Func<float> speed, Func<bool> stopRequested)
        {
            if (!M1VisualAssets.TryResolveBattleVfx(effectId, out var sprite, out _) || sprite == null) yield break;
            var item = NewObject("Battle 3D " + effectId, _worldRoot.transform);
            item.transform.position = position;
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(color.r, color.g, color.b, 0f);
            renderer.sortingOrder = 220;
            FitSprite(item.transform, sprite, height);
            var elapsed = 0f;
            while (elapsed < duration && item != null && !Stopped(stopRequested))
            {
                var delta = Delta(speed);
                elapsed += delta;
                var t = Mathf.Clamp01(elapsed / duration);
                FaceCamera(item.transform);
                item.transform.localScale *= 1f + delta * 0.82f;
                item.transform.Rotate(0f, 0f, delta * 110f, Space.Self);
                var flashAlpha = _reducedFlash ? 0.52f : 1f;
                renderer.color = new Color(color.r, color.g, color.b,
                    Mathf.Sin(t * Mathf.PI) * color.a * flashAlpha);
                yield return null;
            }
            if (item != null) Destroy(item);
        }

        private IEnumerator Amount(ActorView actor, int amount, bool healing,
            Func<float> speed, Func<bool> stopRequested)
        {
            var position = actor?.Root.position + Vector3.up * 3.5f ?? new Vector3(0f, 3.5f, 0f);
            yield return WorldText(M2BattleReadableText021.HitPointChange(amount, healing), position,
                healing ? new Color(0.42f, 1f, 0.64f, 1f) : new Color(1f, 0.36f, 0.28f, 1f),
                0.62f, speed, stopRequested, 0.070f);
        }

        private IEnumerator WorldText(string value, Vector3 position, Color color, float duration,
            Func<float> speed, Func<bool> stopRequested, float size = 0.046f)
        {
            var item = NewObject("Battle 3D Callout " + value, _worldRoot.transform);
            item.transform.position = position;
            var text = item.AddComponent<TextMesh>();
            text.text = value ?? string.Empty;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 72;
            text.characterSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            ConfigureWorldTextMesh(text, RuntimeUi.Font, 260);
            var start = position;
            var elapsed = 0f;
            while (elapsed < duration && item != null && !Stopped(stopRequested))
            {
                elapsed += Delta(speed);
                var t = Mathf.Clamp01(elapsed / duration);
                item.transform.position = start + Vector3.up * (0.72f * t);
                FaceCamera(item.transform);
                text.color = new Color(color.r, color.g, color.b, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            if (item != null) Destroy(item);
        }

        private void Burst(Vector3 position, Color color, int count)
        {
            var item = NewObject("Battle 3D Impact Particles", _worldRoot.transform);
            item.transform.position = position;
            var particles = item.AddComponent<ParticleSystem>();
            _particles.Add(particles);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.54f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 4.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.16f);
            var flashAlpha = _reducedFlash ? 0.55f : 1f;
            var visible = new Color(color.r, color.g, color.b, color.a * flashAlpha);
            main.startColor = new ParticleSystem.MinMaxGradient(visible,
                new Color(color.r, color.g, color.b, color.a * 0.35f * flashAlpha));
            main.gravityModifier = 0.08f;
            main.maxParticles = Math.Max(8, count);
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.24f;
            item.GetComponent<ParticleSystemRenderer>().sharedMaterial = _particle;
            particles.Emit(Math.Max(8, count));
            StartCoroutine(DestroyAfterPresentationDelay(item, 1.2f));
        }

        private IEnumerator DestroyAfterPresentationDelay(GameObject item, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration && item != null)
            {
                elapsed += Delta(_speedProvider);
                yield return null;
            }
            if (item != null) Destroy(item);
        }

        private LineRenderer Trail(Vector3 from, Vector3 to, Color color)
        {
            var item = NewObject("Battle 3D Tactical Route", _worldRoot.transform);
            var line = item.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 4;
            line.widthMultiplier = 0.11f;
            line.numCapVertices = 4;
            line.sharedMaterial = _line;
            line.startColor = new Color(color.r, color.g, color.b, 0.08f);
            line.endColor = color;
            var midpoint = Vector3.Lerp(from, to, 0.5f) + Vector3.up * 0.45f;
            line.SetPosition(0, from);
            line.SetPosition(1, Vector3.Lerp(from, midpoint, 0.66f));
            line.SetPosition(2, Vector3.Lerp(midpoint, to, 0.66f));
            line.SetPosition(3, to);
            return line;
        }

        private IEnumerator FadeLine(LineRenderer line, float duration, Func<float> speed)
        {
            if (line == null) yield break;
            var color = line.endColor;
            var elapsed = 0f;
            while (elapsed < duration && line != null)
            {
                elapsed += Delta(speed);
                var alpha = 1f - Mathf.Clamp01(elapsed / duration);
                line.startColor = new Color(color.r, color.g, color.b, alpha * 0.08f);
                line.endColor = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
            if (line != null) Destroy(line.gameObject);
        }

        private void SetAction(ActorView actor, bool action)
        {
            if (actor == null || actor.Renderer == null) return;
            var sprite = action ? actor.ActionSprite : actor.IdleSprite;
            if (sprite == null) return;
            actor.Renderer.sprite = sprite;
            FitGroundedActorSprite090(actor.SpriteNode, sprite, actor.Height);
        }

        private void RestoreActor(ActorView actor)
        {
            if (actor == null) return;
            SetAction(actor, false);
            if (actor.Downed) return;
            actor.Root.position = ActorHome(actor);
            actor.Pose.localPosition = Vector3.zero;
            actor.Pose.localRotation = Quaternion.identity;
            actor.Pose.localScale = Vector3.one;
        }

        private ActorView ResolveActor(string memberId, string unionId)
        {
            if (!string.IsNullOrWhiteSpace(memberId) && _actors.TryGetValue(memberId, out var actor)) return actor;
            if (!string.IsNullOrWhiteSpace(unionId) && _unions.TryGetValue(unionId, out var union))
                return union.FirstOrDefault(value => !value.Downed) ?? union.FirstOrDefault();
            return null;
        }

        private bool CanRenderDeployment(BattleUnionDeploymentPlan020 deployment)
        {
            if (deployment == null || deployment.PlayerOverflowCount > 0 || deployment.EnemyOverflowCount > 0)
            {
                Debug.LogWarning(
                    "The Slice 020 perspective battlefield supports ten Unions per side. " +
                    "An overflow battle will use the complete legacy fallback instead of overlapping or hiding Unions.",
                    this);
                return false;
            }
            if (deployment.Slots.Count > BattleUnionDeploymentPlanning020.TotalSlotCapacity) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < deployment.Slots.Count; index++)
            {
                var slot = deployment.Slots[index];
                if (slot == null || string.IsNullOrWhiteSpace(slot.UnionId) || !ids.Add(slot.UnionId))
                {
                    Debug.LogWarning(
                        "The Slice 020 perspective battlefield requires unique non-empty Union IDs; " +
                        "the complete legacy fallback remains available.",
                        this);
                    return false;
                }
            }
            return true;
        }

        private static M2BattleUnionView ResolveUnionView(
            M2BattleView battle,
            BattleUnionDeploymentSlot020 slot)
        {
            if (battle == null || slot == null) return null;
            var unions = slot.Enemy ? battle.EnemyUnions : battle.PlayerUnions;
            if (unions == null) return null;
            var expected = slot.SideOrdinal - 1;
            if (expected >= 0 && expected < unions.Count &&
                StringComparer.Ordinal.Equals(unions[expected]?.UnionId, slot.UnionId))
                return unions[expected];
            for (var index = 0; index < unions.Count; index++)
                if (StringComparer.Ordinal.Equals(unions[index]?.UnionId, slot.UnionId)) return unions[index];
            return null;
        }

        private static float OverviewScaleFor(BattleUnionDeploymentPlan020 deployment)
        {
            if (deployment == null || deployment.Slots.Count == 0) return 1f;
            var playerCount = 0;
            var enemyCount = 0;
            var policyScale = 1f;
            for (var index = 0; index < deployment.Slots.Count; index++)
            {
                var slot = deployment.Slots[index];
                if (slot.Enemy) enemyCount++;
                else playerCount++;
                policyScale = Mathf.Min(policyScale, slot.OverviewScale);
            }
            var density = Math.Max(playerCount, enemyCount);
            if (density <= 2) return 1f;
            if (density <= 4) return 0.90f;
            if (density <= 6) return 0.82f;
            if (density <= 8) return 0.76f;
            return policyScale;
        }

        private static bool IsOccupiedSlot020(
            BattleUnionDeploymentPlan020 deployment,
            bool enemy,
            int sideOrdinal)
        {
            if (deployment == null) return false;
            for (var index = 0; index < deployment.Slots.Count; index++)
            {
                var slot = deployment.Slots[index];
                if (slot != null && slot.Enemy == enemy && slot.SideOrdinal == sideOrdinal) return true;
            }
            return false;
        }

        private static string CompactUnionName020(string value)
        {
            var name = string.IsNullOrWhiteSpace(value) ? "UNION" : value.Trim().ToUpperInvariant();
            return name.Length <= 22 ? name : name.Substring(0, 19).TrimEnd() + "...";
        }

        private void SetTacticalWorldLabelsVisible(bool requestedVisible)
        {
            var visible = requestedVisible && _tacticalLabelsAllowed;
            foreach (var union in _unionViews.Values)
            {
                if (union?.StandardBillboard != null)
                    union.StandardBillboard.gameObject.SetActive(visible);
            }
        }

        private static Vector3 ActorHome(ActorView actor)
        {
            if (actor?.Root == null) return Vector3.zero;
            var parent = actor.Root.parent;
            return parent == null ? actor.LocalHome : parent.TransformPoint(actor.LocalHome);
        }

        private static float FormationRadius(IReadOnlyList<ActorView> actors)
        {
            var radius = 1f;
            if (actors == null) return radius;
            for (var index = 0; index < actors.Count; index++)
                if (actors[index] != null) radius = Mathf.Max(radius, actors[index].LocalHome.magnitude);
            return radius;
        }

        private void PrepareDirectiveStaging(
            Battle3DPresentationDirective directive,
            ActorView actor,
            ActorView target)
        {
            if (directive == null || actor == null || target == null ||
                !UsesEngagementPocket020(directive.Action) ||
                StringComparer.Ordinal.Equals(actor.UnionId, target.UnionId)) return;
            if (!_unionViews.TryGetValue(actor.UnionId, out var actingUnion) ||
                !_unionViews.TryGetValue(target.UnionId, out var targetUnion)) return;
            if (!BattleUnionDeploymentPlanning020.TryCreateEngagementPocket(
                    actingUnion.Slot,
                    targetUnion.Slot,
                    BattleUnionDeploymentPlanning020.DefaultEngagementHalfSeparation,
                    out var pocket)) return;

            if (StringComparer.Ordinal.Equals(_stagedPlayerUnionId, pocket.PlayerUnionId) &&
                StringComparer.Ordinal.Equals(_stagedEnemyUnionId, pocket.EnemyUnionId))
            {
                ApplyUnionHighlight(actor.UnionId, target.UnionId);
                return;
            }

            RestoreStagedFormations();
            if (!_unionViews.TryGetValue(pocket.PlayerUnionId, out var player) ||
                !_unionViews.TryGetValue(pocket.EnemyUnionId, out var enemy)) return;
            player.FormationRoot.position = pocket.PlayerContactAnchor;
            enemy.FormationRoot.position = pocket.EnemyContactAnchor;
            player.FormationRoot.localScale = Vector3.one;
            enemy.FormationRoot.localScale = Vector3.one;
            _stagedPlayerUnionId = pocket.PlayerUnionId;
            _stagedEnemyUnionId = pocket.EnemyUnionId;
            ApplyUnionHighlight(actor.UnionId, target.UnionId);
        }

        private static bool UsesEngagementPocket020(Battle3DAction action)
        {
            switch (action)
            {
                case Battle3DAction.WeaponStrike:
                case Battle3DAction.CombatArt:
                case Battle3DAction.MysticArt:
                case Battle3DAction.TacticalStrike:
                case Battle3DAction.Intercept:
                case Battle3DAction.Reposition:
                    return true;
                default:
                    return false;
            }
        }

        private void RestoreStagedFormations()
        {
            foreach (var union in _unionViews.Values)
            {
                if (union?.FormationRoot == null || union.Slot == null) continue;
                union.FormationRoot.position = union.Slot.Anchor;
                union.FormationRoot.rotation = Quaternion.identity;
                union.FormationRoot.localScale = Vector3.one * union.OverviewScale;
                for (var index = 0; index < union.Actors.Count; index++)
                {
                    var actor = union.Actors[index];
                    if (actor?.Root != null && !actor.Retreated) actor.Root.localPosition = actor.LocalHome;
                }
            }
            _stagedPlayerUnionId = string.Empty;
            _stagedEnemyUnionId = string.Empty;
        }

        private void ConfigureTacticalOverviewCamera(BattleUnionDeploymentPlan020 deployment)
        {
            if (_camera == null || deployment == null) return;
            var first = new Vector3(
                -BattleUnionDeploymentPlanning020.RearColumnDistance,
                1.8f,
                -2f * BattleUnionDeploymentPlanning020.RowSpacing);
            var bounds = new Bounds(first, new Vector3(5.2f, 7.6f, 5.6f));
            for (var side = 0; side < 2; side++)
            {
                var enemy = side == 1;
                for (var index = 0; index < BattleUnionDeploymentPlanning020.MaxSlotsPerSide; index++)
                {
                    var column = index / BattleUnionDeploymentPlanning020.RowsPerColumn;
                    var row = index % BattleUnionDeploymentPlanning020.RowsPerColumn;
                    var columnDistance = column == 0
                        ? BattleUnionDeploymentPlanning020.FrontColumnDistance
                        : BattleUnionDeploymentPlanning020.RearColumnDistance;
                    var center = new Vector3(
                        enemy ? columnDistance : -columnDistance,
                        1.8f,
                        (row - 2) * BattleUnionDeploymentPlanning020.RowSpacing);
                    bounds.Encapsulate(new Bounds(center, new Vector3(5.2f, 7.6f, 5.6f)));
                }
            }

            _wideLookPoint = new Vector3(bounds.center.x, 1.8f, bounds.center.z);
            var verticalHalf = Mathf.Clamp(_camera.fieldOfView, 30f, 70f) * Mathf.Deg2Rad * 0.5f;
            var aspect = Mathf.Max(0.60f, _camera.aspect);
            var horizontalHalf = Mathf.Atan(Mathf.Tan(verticalHalf) * aspect);
            var limitingHalf = Mathf.Min(verticalHalf, horizontalHalf);
            var radius = Mathf.Max(8f, bounds.extents.magnitude);
            var distance = Mathf.Max(18f, radius / Mathf.Max(0.20f, Mathf.Sin(limitingHalf)) * 1.10f);
            var viewDirection = new Vector3(0f, -0.32f, 0.947f).normalized;
            _widePosition = _wideLookPoint - viewDirection * distance;
            _wideRotation = Quaternion.LookRotation(_wideLookPoint - _widePosition, Vector3.up);
            _camera.farClipPlane = Mathf.Max(140f, distance + radius * 2f + 24f);
        }

        private float RequiredPairShotDistance020(Vector3 actorPosition, Vector3 targetPosition)
        {
            var separation = Vector2.Distance(
                new Vector2(actorPosition.x, actorPosition.z),
                new Vector2(targetPosition.x, targetPosition.z));
            var verticalHalf = (_camera == null ? 46f : _camera.fieldOfView) * Mathf.Deg2Rad * 0.5f;
            var aspect = _camera == null ? 1.6f : Mathf.Max(0.60f, _camera.aspect);
            var horizontalHalf = Mathf.Atan(Mathf.Tan(verticalHalf) * aspect);
            return Mathf.Max(7f, (separation * 0.5f + 2.4f) /
                Mathf.Max(0.30f, Mathf.Tan(horizontalHalf)));
        }

        private static float ContactClearance020(ActorView actor, ActorView target, bool strong)
        {
            var actorRadius = SpriteFootprintRadius020(actor, actor != null && actor.Enemy ? 1.65f : 1.05f);
            var targetRadius = SpriteFootprintRadius020(target, target != null && target.Enemy ? 2.45f : 1.15f);
            var clearance = targetRadius + actorRadius * 0.78f + (strong ? 0.22f : 0.34f);
            return Mathf.Clamp(clearance, strong ? 2.45f : 2.60f, 5.35f);
        }

        private static float SpriteFootprintRadius020(ActorView actor, float fallback)
        {
            if (actor?.Renderer?.sprite == null || actor.SpriteNode == null) return fallback;
            var spriteWidth = actor.Renderer.sprite.bounds.size.x;
            var worldWidth = spriteWidth * Mathf.Abs(actor.SpriteNode.lossyScale.x);
            return Mathf.Max(fallback, worldWidth * 0.5f);
        }

        private Vector3 TacticalOverviewLookPoint020() => _wideLookPoint;

        private void SetWideCamera(float zOffset)
        {
            _camera.transform.position = _widePosition + Vector3.forward * zOffset;
            _camera.transform.rotation = _wideRotation;
        }

        private void LateUpdate()
        {
            if (_camera == null || _worldRoot == null || !_worldRoot.activeInHierarchy) return;
            var requestedSpeed = _speedProvider == null ? 1f : _speedProvider();
            var particleSpeed = requestedSpeed <= 0f || _impactPaused
                ? 0f
                : Mathf.Clamp(requestedSpeed, 0.25f, 8f);
            for (var index = _particles.Count - 1; index >= 0; index--)
            {
                var particles = _particles[index];
                if (particles == null)
                {
                    _particles.RemoveAt(index);
                    continue;
                }
                var main = particles.main;
                main.simulationSpeed = particleSpeed;
            }
            foreach (var actor in _actors.Values)
            {
                if (actor.Billboard == null) continue;
                var direction = actor.Billboard.position - _camera.transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                    actor.Billboard.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
            foreach (var union in _unionViews.Values)
                if (union?.StandardBillboard != null) FaceCamera(union.StandardBillboard);
        }

        private void FaceCamera(Transform item)
        {
            if (item == null || _camera == null) return;
            var direction = item.position - _camera.transform.position;
            if (direction.sqrMagnitude > 0.001f)
                item.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static void ConfigureWorldTextMesh(TextMesh text, Font font, int sortingOrder)
        {
            if (text == null) return;
            if (font != null)
            {
                text.font = font;
                font.RequestCharactersInTexture(text.text ?? string.Empty, text.fontSize, text.fontStyle);
            }

            var renderer = text.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            if (font != null && font.material != null) renderer.sharedMaterial = font.material;
            renderer.sortingOrder = sortingOrder;
        }

        private Mesh Disc(float radius, int segments)
        {
            segments = Mathf.Max(8, segments);
            var vertices = new Vector3[segments + 1];
            var uv = new Vector2[segments + 1];
            var triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (var index = 0; index < segments; index++)
            {
                var angle = Mathf.PI * 2f * index / segments;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                uv[index + 1] = new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f,
                    Mathf.Sin(angle) * 0.5f + 0.5f);
                var triangle = index * 3;
                // Reverse the XZ fan order so normals face upward toward the camera.
                triangles[triangle] = 0;
                triangles[triangle + 1] = (index + 1) % segments + 1;
                triangles[triangle + 2] = index + 1;
            }
            var mesh = new Mesh { name = "Battle 3D Procedural Disc" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _ownedMeshes.Add(mesh);
            return mesh;
        }

        private void GroundRing(Transform parent, float radius, Color color, float width)
        {
            var item = NewObject("Gateworks Floor Inlay", parent);
            item.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            var line = item.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 72;
            line.widthMultiplier = width;
            line.sharedMaterial = _line;
            line.startColor = color;
            line.endColor = color;
            for (var index = 0; index < line.positionCount; index++)
            {
                var angle = Mathf.PI * 2f * index / line.positionCount;
                line.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f,
                    Mathf.Sin(angle) * radius));
            }
        }

        private GameObject Primitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.layer = BattleLayer;
            item.transform.SetParent(parent, false);
            item.GetComponent<MeshRenderer>().sharedMaterial = material;
            var collider = item.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return item;
        }

        private Material MaterialFor(string shaderName, Color color)
        {
            var shader = Shader.Find(shaderName) ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No built-in battle shader is available.");
            var material = new Material(shader) { name = "Battle 3D Runtime " + shaderName };
            material.color = color;
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            _ownedMaterials.Add(material);
            return material;
        }

        private static void FitSprite(Transform item, Sprite sprite, float height)
        {
            if (item == null || sprite == null) return;
            var scale = height / Mathf.Max(0.01f, sprite.bounds.size.y);
            item.localScale = new Vector3(scale, scale, scale);
        }

        private static void FitGroundedActorSprite090(
            Transform item,
            Sprite sprite,
            float height)
        {
            if (item == null) return;
            FitSprite(item, sprite, height);
            item.localPosition = Vector3.up * GroundedActorSpriteLocalY090(
                sprite,
                height);
        }

        private static float GroundedActorSpriteLocalY090(Sprite sprite, float height)
        {
            // Existing centered-pivot fallbacks resolve to height / 2. EnemyArt700
            // battle sprites use their authored low skeletal pivot, so derive the
            // offset from the scaled lower bound instead of assuming a centered pose.
            if (sprite == null) return height * 0.5f;
            var scale = height / Mathf.Max(0.01f, sprite.bounds.size.y);
            return -sprite.bounds.min.y * scale;
        }

        private float Delta(Func<float> speed)
        {
            if (_impactPaused) return 0f;
            var requested = speed == null ? 1f : speed();
            if (requested <= 0f) return 0f;
            var multiplier = Mathf.Clamp(requested, 0.25f, 8f);
            return Time.unscaledDeltaTime * multiplier;
        }

        private static bool Stopped(Func<bool> stopRequested) =>
            stopRequested != null && stopRequested();

        private static GameObject NewObject(string name, Transform parent)
        {
            var item = new GameObject(name);
            item.layer = BattleLayer;
            if (parent != null) item.transform.SetParent(parent, false);
            return item;
        }

        private void OnDestroy()
        {
            ReleaseEnemyArtLeases090();
            if (_worldRoot != null) Destroy(_worldRoot);
            foreach (var material in _ownedMaterials)
                if (material != null) Destroy(material);
            foreach (var mesh in _ownedMeshes)
                if (mesh != null) Destroy(mesh);
            _ownedMaterials.Clear();
            _ownedMeshes.Clear();
            _particles.Clear();
        }

        private void ReleaseEnemyArtLeases090()
        {
            for (var index090 = 0; index090 < _enemyArtLeases090.Count; index090++)
                _enemyArtLeases090[index090]?.Dispose();
            _enemyArtLeases090.Clear();
        }
    }
}
