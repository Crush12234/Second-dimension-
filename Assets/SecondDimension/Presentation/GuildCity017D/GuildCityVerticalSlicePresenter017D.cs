using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.FirstHour072;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.GuildCity017D
{
    public sealed class GuildCityVerticalSlicePresenter017D : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _safe;
        private RectTransform _content;
        private GuildCityContent017D _catalog;
        private CampaignState _campaign;
        private GuildCityCommandService017D _city;
        private GuildCityExpeditionService017D _expedition;
        private string _screen = "HALL";
        private string _status = "Living Guild + City 017D foundation loaded.";

        private void Awake()
        {
            RuntimeUi.EnsureEventSystem();
            _catalog = GuildCityContent017D.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT","GUILD_CITY_017D"));
            _city = new GuildCityCommandService017D(); _expedition = new GuildCityExpeditionService017D();
            _campaign = CreateDemoCampaign();
            _canvas = RuntimeUi.CreateCanvas("Guild City 017D Canvas");
            _safe = RuntimeUi.AddSafeArea(_canvas.transform);
            BuildChrome(); Render();
        }

        private void BuildChrome()
        {
            var background=RuntimeUi.AddPanel(_safe,"Guild City Background",RuntimeUi.Background); var rect=background.rectTransform; rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;
            var root=RuntimeUi.AddStretchRect(_safe,"Guild City Root"); RuntimeUi.AddVerticalLayout(root,new RectOffset(28,28,24,24),18f);
            var top=RuntimeUi.AddPanel(root,"Institutional Top Bar",RuntimeUi.PanelRaised); RuntimeUi.SetLayout(top,preferredHeight:120f); RuntimeUi.AddHorizontalLayout(top.transform,new RectOffset(28,28,12,12),18f);
            RuntimeUi.AddText(top.transform,"Title","SECOND DIMENSION — RUINED ANNEX",RuntimeUi.CriticalFontPixels,TextAnchor.MiddleLeft,RuntimeUi.Text,FontStyle.Bold);
            RuntimeUi.AddText(top.transform,"Counters","GUILD • CITY • CONTRACTS • EXPEDITIONS",RuntimeUi.SmallBodyFontPixels,TextAnchor.MiddleRight,RuntimeUi.Accent);
            var nav=RuntimeUi.AddPanel(root,"Navigation",RuntimeUi.Panel);RuntimeUi.SetLayout(nav,preferredHeight:132f);RuntimeUi.AddHorizontalLayout(nav.transform,new RectOffset(12,12,8,8),12f);
            AddNav(nav.transform,"HALL");AddNav(nav.transform,"CITY");AddNav(nav.transform,"CONTRACTS");AddNav(nav.transform,"BOARD");AddNav(nav.transform,"RELATIONSHIPS");
            _content=RuntimeUi.AddPanel(root,"Screen Content",RuntimeUi.PanelOverlay).rectTransform;RuntimeUi.SetLayout(_content,flexibleHeight:1f);
            var status=RuntimeUi.AddPanel(root,"Status",RuntimeUi.PanelRaised);RuntimeUi.SetLayout(status,preferredHeight:96f);var label=RuntimeUi.AddText(status.transform,"Status Text","",RuntimeUi.SmallBodyFontPixels,TextAnchor.MiddleLeft,RuntimeUi.MutedText);label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(24,8);label.rectTransform.offsetMax=new Vector2(-24,-8);label.gameObject.name="Guild City Status Label";
        }

        private void AddNav(Transform parent,string screen)=>RuntimeUi.AddButton(parent,screen+" Button",screen,()=>{_screen=screen;Render();},110f);

        private void Render()
        {
            RuntimeUi.ClearChildren(_content);
            if (_content.GetComponent<VerticalLayoutGroup>() == null)
                RuntimeUi.AddVerticalLayout(_content,new RectOffset(32,32,24,24),18f);
            switch(_screen){case"CITY":RenderCity();break;case"CONTRACTS":RenderContracts();break;case"BOARD":RenderBoard();break;case"RELATIONSHIPS":RenderRelationships();break;default:RenderHall();break;}
            var status=GameObject.Find("Guild City Status Label")?.GetComponent<Text>();if(status!=null)status.text=_status;
        }

        private void RenderHall()
        {
            RuntimeUi.AddText(_content,"Hall Heading","LIVING RUINED ANNEX",RuntimeUi.HeadingFontPixels,TextAnchor.MiddleLeft,RuntimeUi.Accent,FontStyle.Bold);
            RuntimeUi.AddText(_content,"Hall Summary","Members remain permanently owned. Recovery, training, staffing, mentorship, relationships, and construction progress in parallel after meaningful operations — never through an empty Wait Day.",RuntimeUi.BodyFontPixels);
            var row=RuntimeUi.AddPanel(_content,"Hall Actions",RuntimeUi.PanelRaised);RuntimeUi.AddHorizontalLayout(row.transform,new RectOffset(18,18,18,18),18f);
            RuntimeUi.AddButton(row.transform,"Assign Trainer","ASSIGN R1 TO TRAINING",()=>Apply(_city.SetAssignment(_campaign,"R1",GuildMemberAssignmentKind017D.Training)));
            RuntimeUi.AddButton(row.transform,"Recover","ASSIGN R2 TO RECOVERY",()=>Apply(_city.SetAssignment(_campaign,"R2",GuildMemberAssignmentKind017D.Recovering)));
            RuntimeUi.AddButton(row.transform,"Complete Operation","COMPLETE MEANINGFUL OPERATION",()=>Apply(_city.CompleteMeaningfulOperation(_campaign,_catalog)),normalColor:RuntimeUi.Accent);
            RuntimeUi.AddText(_content,"Hall State",HallStateText(),RuntimeUi.BodyFontPixels);
        }

        private void RenderCity()
        {
            RuntimeUi.AddText(_content,"City Heading","CITY BUILD MODE — 12 SNAP PLOTS",RuntimeUi.HeadingFontPixels,TextAnchor.MiddleLeft,RuntimeUi.Accent,FontStyle.Bold);
            RuntimeUi.AddText(_content,"City Summary","Six plots begin usable. Buildings visibly occupy plots, accept staff, provide direct effects, and persist. Small projects are immediate; major projects use operation progress, never real-time timers.",RuntimeUi.BodyFontPixels);
            var row=RuntimeUi.AddPanel(_content,"City Actions",RuntimeUi.PanelRaised);RuntimeUi.AddHorizontalLayout(row.transform,new RectOffset(18,18,18,18),18f);
            RuntimeUi.AddButton(row.transform,"Place Recruitment","PLACE RECRUITMENT OFFICE",()=>Apply(_city.PlaceBuilding(_campaign,_catalog,"GC017D_PLOT_01","GC017D_BUILD_RECRUITMENT_OFFICE")));
            RuntimeUi.AddButton(row.transform,"Staff","STAFF WITH R1",()=>Apply(_city.AssignStaff(_campaign,"GC017D_PLOT_01","R1")));
            RuntimeUi.AddButton(row.transform,"Recall","RECALL R1",()=>Apply(_city.DeployStaffMember(_campaign,"R1")));
            RuntimeUi.AddText(_content,"Plot List",CityText(),RuntimeUi.SmallBodyFontPixels);
        }

        private void RenderContracts()
        {
            RuntimeUi.AddText(_content,"Contracts Heading","CONTRACT BOARD",RuntimeUi.HeadingFontPixels,TextAnchor.MiddleLeft,RuntimeUi.Accent,FontStyle.Bold);
            foreach(var pair in _catalog.Contracts)
            {
                var contract=pair.Value;var panel=RuntimeUi.AddPanel(_content,contract.Id,RuntimeUi.PanelRaised);RuntimeUi.AddVerticalLayout(panel.transform,new RectOffset(20,20,12,12),8f);
                RuntimeUi.AddText(panel.transform,"Name",contract.DisplayName+" — "+contract.Sponsor,RuntimeUi.CriticalFontPixels,TextAnchor.MiddleLeft,RuntimeUi.Text,FontStyle.Bold);
                RuntimeUi.AddText(panel.transform,"Objective",contract.PrimaryObjective+"\nCity consequence: "+contract.CityHook,RuntimeUi.SmallBodyFontPixels);
                RuntimeUi.AddButton(panel.transform,"Accept","ACCEPT CONTRACT",()=>Apply(_expedition.AcceptContract(_campaign,_catalog,contract.Id)),100f,RuntimeUi.Accent);
            }
        }

        private void RenderBoard()
        {
            RuntimeUi.AddText(_content,"Board Heading","EXPEDITION BOARD",RuntimeUi.HeadingFontPixels,TextAnchor.MiddleLeft,RuntimeUi.Accent,FontStyle.Bold);
            if(_campaign.Guild.GuildCity.ActiveContract==null){RuntimeUi.AddText(_content,"No Contract","Accept a contract first.");return;}
            if(_campaign.Guild.GuildCity.Expedition==null)RuntimeUi.AddButton(_content,"Start Expedition","BEGIN QUEST",()=>Apply(_expedition.StartExpedition(_campaign,_catalog)),120f,RuntimeUi.Accent);
            else
            {
                var exp=_campaign.Guild.GuildCity.Expedition;var board=_catalog.Board(exp.BoardId);var node=board.Node(exp.CurrentNodeId);
                var resolved=GuildCityExpeditionService017D.IsCurrentNodeResolved(exp,node);
                var requiresResolution=GuildCityExpeditionService017D.CurrentNodeRequiresResolution(node);
                RuntimeUi.AddText(_content,"Board State","NODE "+node.Id+" • "+node.Kind+"\nSUPPLIES "+exp.Supplies+" • FATIGUE "+exp.Fatigue+" • URGENCY "+exp.Urgency,RuntimeUi.CriticalFontPixels);
                if(requiresResolution&&!resolved)
                    RuntimeUi.AddText(_content,"Resolution Required",GuildCityExpeditionService017D.ResolutionHint(node),RuntimeUi.BodyFontPixels,TextAnchor.MiddleLeft,RuntimeUi.Warning,FontStyle.Bold);
                if(!requiresResolution||resolved)
                {
                    var row=RuntimeUi.AddPanel(_content,"Routes",RuntimeUi.PanelRaised);RuntimeUi.AddHorizontalLayout(row.transform,new RectOffset(18,18,18,18),18f);
                    for(var i=0;i<(node.Links?.Length??0);i++){var destination=node.Links[i];RuntimeUi.AddButton(row.transform,"Move "+destination,"MOVE TO "+destination,()=>Apply(_expedition.CommitMove(_campaign,_catalog,destination)));}
                }
                if(!resolved&&(node.Kind=="SKILL_CHECK"||node.Kind=="EVENT"))RuntimeUi.AddButton(_content,"Resolve Check","ROLL 2D6",()=>Apply(_expedition.ResolveCommittedCheck(_campaign,_catalog,node.EventId.Length>0?node.EventId:"EVENT_COLLAPSED_HANDRAIL","R1","R2",2)),110f);
                if(GuildCityExpeditionService017D.IsEncounterNode(node)&&
                    !GuildCityExpeditionService017D.IsEncounterCleared(exp,node))
                    RuntimeUi.AddButton(_content,"Commit Encounter","FACE THE ENEMY",()=>Apply(_expedition.CommitEncounter(_campaign,_catalog,node.EncounterId)),120f,RuntimeUi.Warning);
            }
        }

        private void RenderRelationships()
        {
            RuntimeUi.AddText(_content, "Relationship Heading", "RELATIONSHIPS GROW THROUGH PLAY",
                RuntimeUi.HeadingFontPixels, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
            RuntimeUi.AddText(_content, "Rule",
                "Normal scenes cost zero operations, never expire, and cannot cause member departure. " +
                "Missions, camps, protection, healing, mentorship, staffing, and recovery create memories.",
                RuntimeUi.BodyFontPixels);

            RelationshipMemoryState017D nextScene = null;
            for (var index = 0; index < _campaign.Guild.GuildCity.RelationshipMemories.Count; index++)
            {
                var memory = _campaign.Guild.GuildCity.RelationshipMemories[index];
                if (!memory.Viewed && !string.IsNullOrWhiteSpace(memory.SceneId))
                {
                    nextScene = memory;
                    break;
                }
            }

            if (nextScene != null)
            {
                var sceneId = nextScene.SceneId;
                RuntimeUi.AddButton(_content, "View Scene", "VIEW NEXT FREE HALL SCENE",
                    () => Apply(_city.ViewRelationshipScene(_campaign, sceneId)));
            }
            else
            {
                RuntimeUi.AddText(_content, "No Scene",
                    "NO SHARED MEMORIES YET — complete meaningful Guild work to create them.",
                    RuntimeUi.BodyFontPixels, TextAnchor.MiddleLeft, RuntimeUi.MutedText);
            }

            RuntimeUi.AddText(_content, "Memories", RelationshipText(), RuntimeUi.SmallBodyFontPixels);
        }

        private void Apply(SecondDimension.Core.Result<CampaignState> result){if(result.IsSuccess){_campaign=result.Value;_status="Progress saved.";}else _status=string.Join(" • ",result.Errors);Render();}
        private string HallStateText(){var s=_campaign.Guild.GuildCity;return "OPERATION "+s.OperationOrdinal+" • CHARTER CREDITS "+s.CharterBuildCredits+" • HALL XP "+_campaign.Guild.Development.HallEnhancementXp+"\nMEMBERS "+s.MemberAssignments.Count+" • RELATIONSHIP MEMORIES "+s.RelationshipMemories.Count;}
        private string CityText(){var lines=new List<string>();foreach(var p in _campaign.Guild.GuildCity.CityPlots)lines.Add(p.PlotId+" • "+p.DistrictId+" • "+(p.Unlocked?(p.BuildingId.Length>0?p.BuildingId+" L"+p.BuildingLevel:"OPEN PLOT"):"LOCKED "+p.ConstructionProgress+"/"+GuildCityCommandService017D.OpeningPlotUnlockProgress)+" • STAFF "+p.StaffRecruitIds.Count);return string.Join("\n",lines);}
        private string RelationshipText(){var lines=new List<string>();foreach(var m in _campaign.Guild.GuildCity.RelationshipMemories)lines.Add(m.Summary+" • "+(m.Viewed?"VIEWED":"AVAILABLE FREE"));return lines.Count==0?"No memories yet.":string.Join("\n",lines);}

        private static CampaignState CreateDemoCampaign()
        {
            var recruits=new[]{new RecruitState("R1",100,100,20,20),new RecruitState("R2",100,100,20,20),new RecruitState("R3",100,100,20,20),new RecruitState("R4",100,100,20,20),new RecruitState("R5",100,100,20,20),new RecruitState("R6",100,100,20,20),new RecruitState("R7",100,100,20,20)};
            var unions=new[]{new UnionState("U1","First Union",UnionKind.Normal,"R1",new[]{"R1","R2","R3"},"FORMATION_SKIRMISH_LINE","DOCTRINE_BALANCED",30,7000),new UnionState("U2","Second Union",UnionKind.Normal,"R4",new[]{"R4","R5","R6"},"FORMATION_SKIRMISH_LINE","DOCTRINE_BALANCED",30,7000)};
            var guild=new GuildState("GUILD_017D_DEMO",0,recruits,unions);var profile=new NewGuildProfileState("Guildmaster",SecondDimension.Core.GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false);
            var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,true,"complete");
            return new CampaignState("00000000-0000-0000-0000-000000017017",17017,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,profile,flow);
        }
    }

    /// <summary>
    /// Presentation-only handoff captured when the walkable field temporarily
    /// yields to a mission brief, battle, or another first-hour overlay. It is
    /// intentionally not authoritative campaign state: node/room identity must
    /// still match before the exact world position can be restored.
    /// </summary>
    public sealed class FirstHourFieldCheckpoint076
    {
        public FirstHourFieldCheckpoint076(
            string expeditionId,
            string roomId,
            string nodeId,
            Vector3 position)
        {
            ExpeditionId = expeditionId ?? string.Empty;
            RoomId = roomId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            Position = position;
        }

        public string ExpeditionId { get; }
        public string RoomId { get; }
        public string NodeId { get; }
        public Vector3 Position { get; }

        public bool Matches076(string expeditionId, string roomId) =>
            IsFinite076(Position) &&
            !string.IsNullOrWhiteSpace(ExpeditionId) &&
            StringComparer.Ordinal.Equals(ExpeditionId, expeditionId ?? string.Empty) &&
            StringComparer.Ordinal.Equals(RoomId, roomId ?? string.Empty);

        private static bool IsFinite076(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }

    /// <summary>
    /// First true walkable quest-space slice. The existing 017D expedition
    /// service still owns movement/check/encounter/finalization consequences;
    /// this component replaces destination cards with direct avatar play.
    /// </summary>
    public sealed class OuterGateworksExploration066 : MonoBehaviour
    {
        private enum ExpeditionRoom072
        {
            HallBreach,
            LanternRoad,
            PatrolRescue,
            Gatehouse,
            ReturnRoad
        }

        private const int WorldLayer066 = 29;
        private const float WalkSpeed066 = 5.4f;
        private const float DodgeSpeed066 = 12.5f;
        private const float DodgeDuration066 = 0.42f;
        private const string FirstPlayableRescueBoard069 = "BOARD_BELL_BENEATH_GATE_069";
        private const string FirstHourThreeBattleBoard071 = "BOARD_BELL_BENEATH_GATE_071";
        public const string HallBreachBackdropResource076 =
            "SecondDimension/Art/FirstHour071/Environments/GUILD_HALL_GAMEPLAY_PLATE_071";
        public const string LanternRoadBackdropResource076 =
            "SecondDimension/Art/FirstHour071/Environments/LANTERN_ROAD_GAMEPLAY_PLATE_071";
        public const string LanternAmbushBackdropResource076 =
            "SecondDimension/Art/Battle/BG_TUTORIAL_GATEWORKS_ARENA";
        public const string GatehouseBackdropResource076 =
            "SecondDimension/Art/FirstHour071/Environments/GATEHOUSE_BOSS_ARENA_071";
        public const string SkyhomeReturnBackdropResource076 =
            "SecondDimension/Art/FirstHour071/Environments/SKYHOME_MARKET_GAMEPLAY_PLATE_071";
        private static readonly Vector3 DefaultEntryPosition066 = new Vector3(0f, 0.2f, -11.0f);
        private static readonly Vector3 ReturnWalkPosition066 = new Vector3(0f, 0.2f, -4.8f);
        private static readonly Vector3 LanternPatrolRescuePosition071 =
            new Vector3(-4.3f, 0.2f, 17.6f);
        private static readonly Dictionary<string, Vector3> FirstBoardNodePositions066 =
            new Dictionary<string, Vector3>(StringComparer.Ordinal)
            {
                { "N00", new Vector3(0.0f, 0.2f, -11.8f) },
                { "N01", new Vector3(0.0f, 0.2f, -8.0f) },
                { "N02", new Vector3(-7.0f, 0.2f, -6.5f) },
                { "N03", new Vector3(-7.0f, 0.2f, -2.2f) },
                { "N04", new Vector3(0.0f, 0.2f, 1.0f) },
                { "N05", new Vector3(8.2f, 0.2f, 6.6f) },
                { "N06", new Vector3(0.0f, 0.2f, 7.5f) },
                { "N07", new Vector3(-6.2f, 0.2f, 10.8f) },
                { "N08", new Vector3(5.2f, 0.2f, 10.8f) },
                { "N09", new Vector3(8.2f, 0.2f, 14.2f) },
                { "N10", new Vector3(0.0f, 0.2f, 14.0f) },
                { "N11", new Vector3(-5.5f, 0.2f, 17.6f) },
                { "N12", new Vector3(5.5f, 0.2f, 17.6f) },
                { "N13", new Vector3(0.0f, 0.2f, 21.2f) },
                { "N14", new Vector3(0.0f, 0.2f, -15.5f) }
            };
        private static readonly string[][] FirstBoardRouteEdges066 =
        {
            new[] { "N00", "N01" },
            new[] { "N01", "N02" }, new[] { "N01", "N04" },
            new[] { "N02", "N03" }, new[] { "N03", "N06" },
            new[] { "N04", "N05" }, new[] { "N05", "N06" },
            new[] { "N06", "N07" }, new[] { "N06", "N08" },
            new[] { "N07", "N10" }, new[] { "N08", "N09" },
            new[] { "N09", "N10" },
            new[] { "N10", "N11" }, new[] { "N10", "N12" },
            new[] { "N11", "N13" }, new[] { "N12", "N13" },
            new[] { "N13", "N14" }
        };
        private static readonly string[] RescuedLanternPatrolRecruitIds071 =
        {
            "SIGREC_ZORIN_BRAMBLECROSS",
            "SIGREC_UNA_QUEENSREST",
            "SIGREC_JUNIA_SKYWARD",
            "SIGREC_DAIN_DEEPWELL",
            "SIGREC_WILLOW_LONGSTRIDE",
            "SIGREC_QUIN_LOWEN",
            "SIGREC_ASTER_MARSHLIGHT",
            "SIGREC_PETRA_RUNEBROOK",
            "SIGREC_QUIN_CROWNHILL",
            "SIGREC_YVES_THORNFIELD"
        };
        private static readonly Vector3[] LanternPatrolFormationOffsets076 =
        {
            // Zorin owns the centre-left hero position. The remaining nine
            // survivors form one countable arc instead of hiding in repeated
            // depth columns behind four front-row silhouettes.
            new Vector3(-0.32f, 0f, 0.00f),
            new Vector3(-2.88f, 0f, 0.28f),
            new Vector3(-2.24f, 0f, 0.18f),
            new Vector3(-1.60f, 0f, 0.10f),
            new Vector3(-0.96f, 0f, 0.04f),
            new Vector3( 0.32f, 0f, 0.00f),
            new Vector3( 0.96f, 0f, 0.04f),
            new Vector3( 1.60f, 0f, 0.10f),
            new Vector3( 2.24f, 0f, 0.18f),
            new Vector3( 2.88f, 0f, 0.28f)
        };

        private sealed class GateworksInteraction066
        {
            public string Id;
            public string Title;
            public string Description;
            public Transform Anchor;
            public float Radius;
            public Action Action;
        }

        private IGuildCityPresentationCoordinator017D _coordinator;
        private Action _returnToQuest;
        private Action _requestBattle;
        private FirstHourFieldCheckpoint076 _resumeCheckpoint076;
        private GameObject _worldRoot;
        private Canvas _hudCanvas;
        private Camera _worldCamera;
        private AudioSource _expeditionAudio071;
        private CharacterController _controller;
        private WorldCharacterMotor070 _worldMotor070;
        private WorldCharacterAnimator070 _worldAnimator070;
        private WorldInput071 _worldInput071;
        private ExpeditionSpace070 _expeditionSpace070;
        private FirstHourWorldStage072 _firstHourStage072;
        private Transform _partyRoot;
        private Transform _partyVisual;
        private Transform _roomActors072;
        private Transform _questBeacon;
        private Transform _questBeaconVisual;
        private Transform _paintedVista068;
        private Transform _patrolCastRoot076;
        private Transform _encounterCastRoot076;
        private Transform _controlledIdentityBadge076;
        private Transform _kaelIdentityBadge076;
        private Transform _zorinIdentityBadge076;
        private Transform _patrolCohortBadge076;
        private Sprite _contactShadowSprite076;
        private GateworksInteraction066 _questInteraction;
        private GateworksInteraction066 _campInteraction066;
        private GateworksInteraction066 _secretInteraction066;
        private readonly List<Transform> _followers = new List<Transform>();
        private readonly List<string> _namedCompanionIds076 = new List<string>();
        private readonly List<string> _namedCompanionNames076 = new List<string>();
        private readonly List<TextMesh> _worldLabels = new List<TextMesh>();
        private readonly List<Transform> _characterBillboards068 = new List<Transform>();
        private readonly List<Sprite> _runtimeCharacterSprites068 = new List<Sprite>();
        private readonly List<Texture2D> _runtimeTextures076 = new List<Texture2D>();
        private readonly List<GateworksInteraction066> _interactions = new List<GateworksInteraction066>();
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        private Text _objectiveText;
        private Text _chapterText073;
        private Text _storyPartyText076;
        private Text _storyContextText073;
        private Text _xpText073;
        private Text _routeText073;
        private Image _routeFill073;
        private Text _promptText;
        private Text _messageText;
        private Text _diceText;
        private Image _dicePanel;
        private Image _messagePanel071;
        private Image _controlsPanel071;
        private Image _routeChoicePanel068;
        private Image _roomFadePanel072;
        private Text _routeChoiceOneText068;
        private Text _routeChoiceTwoText068;
        private Text _controlsText076;
        private Vector3 _lastMoveDirection = Vector3.forward;
        private Vector3 _cameraVelocity;
        private float _dodgeRemaining;
        private float _walkCycle;
        private float _messageHideAt071;
        private bool _routeChoiceActive;
        private bool _optionalEliteChoiceActive;
        private IReadOnlyList<string> _routeChoices = Array.Empty<string>();
        private bool _interactionLocked;
        private bool _campShared;
        private bool _secretFound;
        private bool _shutdown;
        private ExpeditionRoom072 _room072;
        private string _roomBeatKey072 = string.Empty;
        private string _authoredBackdropResource076 = string.Empty;
        private Coroutine _roomFadeCoroutine072;

        public Transform ControlledAvatar066 => _partyRoot;
        public bool UsesTrueWorldMovement066 => _controller != null && _worldCamera != null;
        public bool UsesFullFrameAuthoredVista069 =>
            _firstHourStage072 != null &&
            _firstHourStage072.HasWorldAnchoredBackdrop072;
        public bool UsesVersion70Movement070 =>
            _worldMotor070 != null && _worldAnimator070 != null;
        public bool UsesCrossPlatformWorldInput071 => _worldInput071 != null;
        public bool HasTouchControlsForVerification071 =>
            _worldInput071 != null && _worldInput071.TouchControlsBuilt071;
        public WorldInput071 WorldInputForVerification071 => _worldInput071;
        public Vector3 LastMoveDirectionForVerification070 => _lastMoveDirection;
        public bool UsesDataDrivenExpeditionSpace070 =>
            _expeditionSpace070 != null && _expeditionSpace070.StationCount070 > 0;
        public bool UsesBeatSizedWorldStages072 =>
            _firstHourStage072 != null && _firstHourStage072.HasWorldAnchoredBackdrop072;
        public bool UsesPoseDrivenWorldSprite072 =>
            _worldAnimator070 != null && _worldAnimator070.UsesSpritePoses070;
        public string ActiveExpeditionRoomForVerification072 => _room072.ToString();
        public int VisibleStoryActorCountForVerification072 => _roomActors072 == null
            ? 0
            : _roomActors072.GetComponentsInChildren<SpriteRenderer>(false)
                .Count(renderer => renderer != null && renderer.enabled);
        public Vector3 InitialPartyPosition066 { get; private set; }
        public bool SpawnedForReturnWalk066 { get; private set; }

        public bool IsActiveForVerification076 =>
            !_shutdown && _worldRoot != null && _worldRoot.activeInHierarchy &&
            _hudCanvas != null && _hudCanvas.gameObject.activeInHierarchy;
        public bool InteractionLockedForVerification076 => _interactionLocked;
        public Transform ControlledAvatarForVerification076 => _partyRoot;
        public string CurrentNodeForVerification076 =>
            _coordinator?.GuildCity017D?.Expedition?.CurrentNodeId ?? string.Empty;
        public string CurrentObjectiveForVerification076 =>
            _objectiveText?.text ?? string.Empty;
        /// <summary>
        /// Read-only certification telemetry for the currently rendered gold
        /// objective. Direction uses the X/Z input plane consumed by
        /// ApplyMovementInput066; unavailable objectives report zero direction
        /// and positive-infinity distance instead of mutating world state.
        /// </summary>
        public Vector3 CurrentObjectivePositionForVerification076 =>
            HasCurrentObjectiveForVerification076
                ? _questInteraction.Anchor.position
                : Vector3.zero;
        public float CurrentObjectiveDistanceForVerification076 =>
            HasCurrentObjectiveForVerification076 && _partyRoot != null
                ? HorizontalDistance066(_questInteraction.Anchor.position, _partyRoot.position)
                : float.PositiveInfinity;
        public float CurrentObjectiveInteractionRadiusForVerification076 =>
            HasCurrentObjectiveForVerification076
                ? Mathf.Max(0f, _questInteraction.Radius)
                : 0f;
        public bool IsWithinCurrentObjectiveInteractionRangeForVerification076 =>
            HasCurrentObjectiveForVerification076 &&
            CurrentObjectiveDistanceForVerification076 <=
            CurrentObjectiveInteractionRadiusForVerification076;
        public Vector2 CurrentObjectiveDirectionForVerification076
        {
            get
            {
                if (!HasCurrentObjectiveForVerification076 || _partyRoot == null)
                    return Vector2.zero;
                var delta076 = _questInteraction.Anchor.position - _partyRoot.position;
                var direction076 = new Vector2(delta076.x, delta076.z);
                return direction076.sqrMagnitude > 0.000001f
                    ? direction076.normalized
                    : Vector2.zero;
            }
        }
        public int AuthoritativeActionCountForVerification076 =>
            _interactions.Count(value => value?.Anchor != null &&
                                         value.Anchor.gameObject.activeInHierarchy);
        public string AuthoredBackdropForVerification076 =>
            _authoredBackdropResource076;
        public IReadOnlyList<string> NamedCompanionIdsForVerification076 =>
            _namedCompanionIds076.AsReadOnly();
        public string GuildmasterStandeeResourceKeyForVerification076 { get; private set; }
        public string GuildmasterPoseResourceRootForVerification076 { get; private set; }
        public Text StoryPartyTextForVerification076 => _storyPartyText076;
        public Text StoryContextTextForVerification076 => _storyContextText073;
        public Text ControlsTextForVerification076 => _controlsText076;
        public bool DesktopControlsVisibleForVerification076 =>
            _controlsPanel071 != null &&
            _controlsPanel071.gameObject.activeInHierarchy;
        public bool HasControlledIdentityBadgeForVerification076 =>
            _controlledIdentityBadge076 != null &&
            _controlledIdentityBadge076.gameObject.activeInHierarchy;
        public bool HasKaelIdentityBadgeForVerification076 =>
            _kaelIdentityBadge076 != null &&
            _kaelIdentityBadge076.gameObject.activeInHierarchy;
        public bool HasEncounterCastForVerification076 =>
            _encounterCastRoot076 != null &&
            _encounterCastRoot076.gameObject.activeInHierarchy;
        public bool HasZorinIdentityBadgeForVerification076 =>
            _zorinIdentityBadge076 != null &&
            _zorinIdentityBadge076.gameObject.activeInHierarchy;
        public bool HasPatrolCohortBadgeForVerification076 =>
            _patrolCohortBadge076 != null &&
            _patrolCohortBadge076.gameObject.activeInHierarchy;
        public int PatrolMemberCountForVerification076 =>
            PatrolMembers076().Count();
        public int VisiblePatrolStandeeCountForVerification076 =>
            PatrolMembers076()
                .SelectMany(value => value.GetComponentsInChildren<SpriteRenderer>(false))
                .Count(value => value != null && value.enabled && value.sprite != null &&
                                value.name.StartsWith(
                                    "Gateworks Character Art ",
                                    StringComparison.Ordinal));
        public int DistinctPatrolStandeeCountForVerification076 =>
            PatrolMembers076()
                .SelectMany(value => value.GetComponentsInChildren<SpriteRenderer>(false))
                .Where(value => value != null && value.enabled && value.sprite != null &&
                                value.name.StartsWith(
                                    "Gateworks Character Art ",
                                    StringComparison.Ordinal))
                .Select(value => value.sprite.texture != null
                    ? value.sprite.texture.name
                    : value.sprite.name)
                .Distinct(StringComparer.Ordinal)
                .Count();
        public int PatrolDistinctHorizontalColumnsForVerification076 =>
            PatrolMembers076()
                .Select(value => Mathf.RoundToInt(value.localPosition.x * 100f))
                .Distinct()
                .Count();
        public string PatrolCohortLabelForVerification076 =>
            _patrolCohortBadge076 == null
                ? string.Empty
                : _patrolCohortBadge076.GetComponentInChildren<Text>(true)?.text ?? string.Empty;
        public float ClosestPatrolMemberToObjectiveForVerification076 =>
            _questBeacon == null
                ? float.PositiveInfinity
                : PatrolMembers076()
                    .Select(value => HorizontalDistance066(
                        value.position,
                        _questBeacon.position))
                    .DefaultIfEmpty(float.PositiveInfinity)
                    .Min();
        public string CurrentStoryMessageForVerification076 =>
            _messageText?.text ?? string.Empty;
        public bool HasOptionalCampOrSecretForVerification076 =>
            _campInteraction066 != null || _secretInteraction066 != null;
        public bool PatrolRescueCeremonyRequestedForVerification076 { get; private set; }
        public bool RestoredFieldCheckpointForVerification076 { get; private set; }
        public bool PreservedPositionOnLastBeatTransitionForVerification076 { get; private set; }
        public bool UsesBackdropParallaxForVerification076 =>
            _firstHourStage072 != null && _firstHourStage072.UsesBackdropParallax072;
        public float BackdropParallaxTravelForVerification076 =>
            _firstHourStage072?.BackdropParallaxTravel072 ?? 0f;
        public bool BackdropCoversAspectForVerification076(float aspect) =>
            _firstHourStage072 != null &&
            _firstHourStage072.CoversViewportAtAspect072(aspect);

        public static string BackdropResourceForBeatForVerification076(
            string roomId,
            string nodeId)
        {
            if (StringComparer.Ordinal.Equals(roomId, ExpeditionRoom072.HallBreach.ToString()))
                return HallBreachBackdropResource076;
            if (StringComparer.Ordinal.Equals(roomId, ExpeditionRoom072.PatrolRescue.ToString()))
                return LanternAmbushBackdropResource076;
            if (StringComparer.Ordinal.Equals(roomId, ExpeditionRoom072.Gatehouse.ToString()))
                return GatehouseBackdropResource076;
            if (StringComparer.Ordinal.Equals(roomId, ExpeditionRoom072.ReturnRoad.ToString()))
                return SkyhomeReturnBackdropResource076;
            return StringComparer.Ordinal.Equals(nodeId, "N06")
                ? LanternAmbushBackdropResource076
                : LanternRoadBackdropResource076;
        }

        private IEnumerable<Transform> PatrolMembers076() =>
            _patrolCastRoot076 == null
                ? Enumerable.Empty<Transform>()
                : Enumerable.Range(0, _patrolCastRoot076.childCount)
                    .Select(_patrolCastRoot076.GetChild)
                    .Where(value => value != null &&
                                    value.name.StartsWith(
                                        "Rescued Lantern Patrol ",
                                        StringComparison.Ordinal));

        private bool HasCurrentObjectiveForVerification076 =>
            !_shutdown && _questInteraction?.Anchor != null &&
            _questInteraction.Anchor.gameObject.activeInHierarchy;

        public bool TeleportToCurrentObjectiveForVerification076()
        {
            if (_shutdown || _partyRoot == null || _questInteraction?.Anchor == null)
                return false;
            var controllerWasEnabled076 = _controller != null && _controller.enabled;
            if (controllerWasEnabled076) _controller.enabled = false;
            _partyRoot.position = _questInteraction.Anchor.position +
                                  new Vector3(-0.45f, 0f, 0f);
            if (controllerWasEnabled076) _controller.enabled = true;
            UpdateFollowers066(1f);
            UpdateInteractionPrompt066();
            return true;
        }

        public void InteractForVerification076() => ApplyInteractionInput066();

        public void OpenMissionBriefForVerification076() => _returnToQuest?.Invoke();

        public FirstHourFieldCheckpoint076 CaptureFieldCheckpoint076()
        {
            var expedition076 = _coordinator?.GuildCity017D?.Expedition;
            if (_shutdown || _partyRoot == null || expedition076 == null ||
                string.IsNullOrWhiteSpace(expedition076.ExpeditionId))
                return null;
            var position076 = _firstHourStage072 != null
                ? _firstHourStage072.ClampToLane072(_partyRoot.position)
                : _partyRoot.position;
            return new FirstHourFieldCheckpoint076(
                expedition076.ExpeditionId,
                _room072.ToString(),
                expedition076.CurrentNodeId,
                position076);
        }

        /// <summary>PlayMode seam proving which nearby world interaction receives E.</summary>
        public string NearestInteractionIdForVerification066 =>
            NearestInteraction066()?.Id ?? string.Empty;

        /// <summary>PlayMode seam for projecting an externally changed test state.</summary>
        public void RefreshQuestGuidanceForVerification066() => RefreshQuestGuidance066();

        public void RefreshAuthoritativeState076() => RefreshQuestGuidance066();

        public void RefreshPersistentHudForVerification076() => UpdateTransientHud071();

        public void Begin066(
            IGuildCityPresentationCoordinator017D coordinator,
            Action returnToQuest,
            Action requestBattle,
            FirstHourFieldCheckpoint076 resumeCheckpoint076 = null)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            _coordinator = coordinator;
            _returnToQuest = returnToQuest;
            _requestBattle = requestBattle;
            _resumeCheckpoint076 = resumeCheckpoint076;
            BuildWorld066();
            BuildHud066();
            RefreshQuestGuidance066();
            var migrationFlags069 = coordinator.GuildCity017D?.Expedition?.ObjectiveFlags ??
                Array.Empty<string>();
            var migrated069 = migrationFlags069.Any(value => StringComparer.Ordinal.Equals(
                value, GuildCityExpeditionService017D.LegacyFirstRescueMigratedFlag069));
            var rescuePreserved069 = migrationFlags069.Any(value => StringComparer.Ordinal.Equals(
                value, "PRIMARY_OBJECTIVE_RESCUE_COMPLETE"));
            ShowMessage066(IsFirstHourThreeBattle071()
                ? "FOLLOW THE GOLD MARKER. MOVE with WASD / stick, ROLL with SPACE / B, and use ACT near the destination."
                : migrated069
                ? rescuePreserved069
                    ? "Your recovered Wayglass and claimed battle rewards were preserved. Follow the road back to Skyhome—there is no second battle."
                    : "Your older first-story save was restarted safely on the shorter Lantern Road route. Your Guild, members, equipment, and completed progress are unchanged. Follow the objective arrow."
                : "Lantern Road is open. Follow the objective arrow toward the missing patrol and old Gatehouse; use ACT to interact.");
        }

        private void Update()
        {
            if (_shutdown || _controller == null) return;

            var input071 = _worldInput071 != null
                ? _worldInput071.CaptureFrame071()
                : WorldInput071.CaptureLegacyFrame071();

            if (input071.BackPressed071)
            {
                _returnToQuest?.Invoke();
                return;
            }

            if (_routeChoiceActive && input071.RouteChoiceIndex071 >= 0)
            {
                ApplyRouteChoiceInput066(input071.RouteChoiceIndex071);
            }

            var movement070 = input071.Movement071;
            if (_worldMotor070 != null)
            {
                var movementLocked070 = _routeChoiceActive || _interactionLocked;
                _worldMotor070.SetInput070(
                    movementLocked070 ? Vector2.zero : movement070,
                    !movementLocked070 && input071.RunHeld071,
                    !movementLocked070 && input071.DodgePressed071,
                    false);
                SynchronizeLastMoveDirection070(movementLocked070 ? Vector2.zero : movement070);
            }
            else
            {
                ApplyMovementInput066(
                    movement070,
                    input071.DodgePressed071,
                    Time.unscaledDeltaTime);
            }

            UpdateCamera066(Time.unscaledDeltaTime);
            UpdateFollowers066(Time.unscaledDeltaTime);
            UpdateWorldLabels066();
            UpdateInteractionPrompt066();
            UpdateTransientHud071();

            if (!_routeChoiceActive && !_interactionLocked && input071.InteractPressed071)
                ApplyInteractionInput066();
        }

        /// <summary>Input seam used by Update and PlayMode verification.</summary>
        public void ApplyMovementInput066(Vector2 input, bool beginDodge, float deltaTime)
        {
            if (_controller == null || !_controller.enabled) return;
            if (_routeChoiceActive || _interactionLocked)
            {
                _worldMotor070?.StopImmediately070();
                return;
            }
            if (_worldMotor070 != null)
            {
                _worldMotor070.Simulate070(input, false, beginDodge, false, deltaTime);
                SynchronizeLastMoveDirection070(input);
                ClampPartyToGateworks070();
                return;
            }
            var direction = new Vector3(input.x, 0f, input.y);
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            if (direction.sqrMagnitude > 0.001f)
            {
                _lastMoveDirection = direction.normalized;
                _partyRoot.rotation = Quaternion.Slerp(
                    _partyRoot.rotation,
                    Quaternion.LookRotation(_lastMoveDirection, Vector3.up),
                    Mathf.Clamp01(Mathf.Max(0.01f, deltaTime) * 12f));
            }

            if (beginDodge && _dodgeRemaining <= 0f)
            {
                _dodgeRemaining = DodgeDuration066;
                if (direction.sqrMagnitude < 0.001f) direction = _lastMoveDirection;
            }

            var dodging = _dodgeRemaining > 0f;
            if (dodging)
            {
                direction = _lastMoveDirection;
                _dodgeRemaining = Mathf.Max(0f, _dodgeRemaining - Mathf.Max(0f, deltaTime));
            }

            var speed = dodging ? DodgeSpeed066 : WalkSpeed066;
            _controller.Move(direction * speed * Mathf.Max(0f, deltaTime) + Vector3.down * 0.08f);
            var position = _firstHourStage072 != null
                ? _firstHourStage072.ClampToLane072(_partyRoot.position)
                : _partyRoot.position;
            _partyRoot.position = position;

            if (_partyVisual != null)
            {
                _walkCycle += direction.magnitude * deltaTime * 10f;
                var dodgeProgress = dodging
                    ? 1f - (_dodgeRemaining / DodgeDuration066)
                    : 0f;
                _partyVisual.localPosition = new Vector3(0f,
                    direction.sqrMagnitude > 0.001f ? Mathf.Abs(Mathf.Sin(_walkCycle)) * 0.10f : 0f,
                    0f);
                _partyVisual.localEulerAngles = dodging
                    ? new Vector3(dodgeProgress * 360f, 0f, 0f)
                    : Vector3.zero;
            }
        }

        private void LateUpdate()
        {
            if (_shutdown || _worldMotor070 == null) return;
            ClampPartyToGateworks070();
        }

        private void ClampPartyToGateworks070()
        {
            if (_partyRoot == null || _controller == null || !_controller.enabled) return;
            var current = _partyRoot.position;
            var clamped = _firstHourStage072 != null
                ? _firstHourStage072.ClampToLane072(current)
                : new Vector3(
                    Mathf.Clamp(current.x, -6.8f, 6.8f),
                    current.y,
                    Mathf.Clamp(current.z, -2.1f, 2.6f));
            var correction = clamped - current;
            if (correction.sqrMagnitude > 0.000001f) _controller.Move(correction);
        }

        /// <summary>Proximity-interaction seam used by the E key and PlayMode verification.</summary>
        public void ApplyInteractionInput066()
        {
            if (_shutdown || _routeChoiceActive || _interactionLocked) return;
            var nearest = NearestInteraction066();
            if (nearest != null && _expeditionSpace070 != null &&
                _expeditionSpace070.TryGetNearestStation070(
                    _partyRoot.position,
                    true,
                    out var station070) &&
                StringComparer.Ordinal.Equals(station070.StationId070, nearest.Id) &&
                _expeditionSpace070.TryInteractNearest070(out _)) return;
            nearest?.Action?.Invoke();
        }

        /// <summary>Fork-selection seam used by number keys and PlayMode verification.</summary>
        public void ApplyRouteChoiceInput066(int zeroBasedIndex)
        {
            if (_shutdown || !_routeChoiceActive) return;
            if (_optionalEliteChoiceActive)
            {
                CommitOptionalEliteChoice066(zeroBasedIndex);
                return;
            }
            CommitRouteChoice066(zeroBasedIndex);
        }

        private void BuildWorld066()
        {
            _worldRoot = new GameObject("Walkable Skyhome Outer Gateworks 066");
            _worldRoot.layer = WorldLayer066;
            BuildExpeditionAudio071();
            _room072 = ResolveExpeditionRoom072(_coordinator?.GuildCity017D?.Expedition);
            _roomBeatKey072 = ResolveRoomBeatKey072(_coordinator?.GuildCity017D?.Expedition);
            BuildEnvironment066();
            BuildParty066();
            BuildWorldCamera066();
            BuildStoryInteractions066();
            BuildExpeditionSpace070();
        }

        private void BuildExpeditionAudio071()
        {
            var ambience = Resources.Load<AudioClip>(
                "SecondDimension/GuildCity017F/Audio/AMB_CAMP_REST_017F");
            _expeditionAudio071 = _worldRoot.AddComponent<AudioSource>();
            _expeditionAudio071.playOnAwake = false;
            _expeditionAudio071.spatialBlend = 0f;
            _expeditionAudio071.volume = 0.20f;
            if (ambience == null) return;
            _expeditionAudio071.clip = ambience;
            _expeditionAudio071.loop = true;
            _expeditionAudio071.Play();
        }

        private bool IsStreamlinedFirstRescue069() =>
            StringComparer.Ordinal.Equals(
                _coordinator?.GuildCity017D?.Expedition?.BoardId,
                FirstPlayableRescueBoard069);

        private bool IsFirstHourThreeBattle071() =>
            StringComparer.Ordinal.Equals(
                _coordinator?.GuildCity017D?.Expedition?.BoardId,
                FirstHourThreeBattleBoard071);

        private static bool FirstHourLanternPatrolRescued071(
            GuildCityExpeditionView017D expedition) =>
            GuildCityExpeditionService017D.HasFirstHourLanternPatrolRescued071(
                expedition?.ObjectiveFlags);

        private bool IsCompactFirstRescueRoute071() =>
            IsStreamlinedFirstRescue069() || IsFirstHourThreeBattle071();

        private ExpeditionRoom072 ResolveExpeditionRoom072(GuildCityExpeditionView017D expedition)
        {
            if (expedition == null) return ExpeditionRoom072.HallBreach;
            if (expedition.CanFinalizeOperation) return ExpeditionRoom072.ReturnRoad;

            var nodeId = string.IsNullOrWhiteSpace(expedition.CurrentNodeId)
                ? "N00"
                : expedition.CurrentNodeId;
            if (IsFirstHourThreeBattle071())
            {
                var flags = expedition.ObjectiveFlags ?? Array.Empty<string>();
                var hallBreachCleared = flags.Any(flag => StringComparer.Ordinal.Equals(
                    flag, GuildCityExpeditionService017D.EncounterClearedFlag("N01")));
                if (!hallBreachCleared &&
                    (StringComparer.Ordinal.Equals(nodeId, "N00") ||
                     StringComparer.Ordinal.Equals(nodeId, "N01") ||
                     StringComparer.Ordinal.Equals(
                         expedition.CurrentEncounterId, "ENCOUNTER071_HALL_BREACH")))
                    return ExpeditionRoom072.HallBreach;

                if (StringComparer.Ordinal.Equals(nodeId, "N13") ||
                    StringComparer.Ordinal.Equals(
                        expedition.CurrentEncounterId, "ENCOUNTER071_GATE_EATER"))
                    return FirstHourLanternPatrolRescued071(expedition)
                        ? ExpeditionRoom072.Gatehouse
                        : ExpeditionRoom072.PatrolRescue;
            }

            return ExpeditionRoom072.LanternRoad;
        }

        private string ResolveRoomBeatKey072(GuildCityExpeditionView017D expedition)
        {
            if (expedition == null) return "NO_EXPEDITION";
            return ResolveExpeditionRoom072(expedition) + "|" +
                   (expedition.CurrentNodeId ?? string.Empty) + "|" +
                   (expedition.CurrentNodeKind ?? string.Empty) + "|" +
                   (expedition.CurrentEncounterId ?? string.Empty) + "|" +
                   expedition.ResolutionComplete + "|" +
                   expedition.CanCommitEncounter + "|" +
                   expedition.CanFinalizeOperation + "|" +
                   FirstHourLanternPatrolRescued071(expedition);
        }

        private static Vector3 RoomSpawnPosition072(ExpeditionRoom072 room, bool returning)
        {
            switch (room)
            {
                case ExpeditionRoom072.HallBreach:
                    return new Vector3(-4.75f, 0.20f, -0.85f);
                case ExpeditionRoom072.PatrolRescue:
                    return new Vector3(-4.90f, 0.20f, -0.95f);
                case ExpeditionRoom072.Gatehouse:
                    return new Vector3(-4.65f, 0.20f, -0.80f);
                case ExpeditionRoom072.ReturnRoad:
                    return new Vector3(4.75f, 0.20f, -0.78f);
                default:
                    return new Vector3(returning ? -4.15f : -4.80f, 0.20f, -0.90f);
            }
        }

        private static Vector2 RoomHorizontalBounds072(ExpeditionRoom072 room) =>
            room == ExpeditionRoom072.Gatehouse
                ? new Vector2(-5.8f, 5.8f)
                : new Vector2(-6.4f, 6.4f);

        private static Vector3 ClampRoomPosition072(
            ExpeditionRoom072 room,
            Vector3 position)
        {
            var bounds076 = RoomHorizontalBounds072(room);
            position.x = Mathf.Clamp(position.x, bounds076.x, bounds076.y);
            position.z = Mathf.Clamp(position.z, -2.0f, 2.25f);
            return position;
        }

        private static Vector3 RoomObjectivePosition072(ExpeditionRoom072 room)
        {
            switch (room)
            {
                case ExpeditionRoom072.PatrolRescue:
                    return new Vector3(3.65f, 0.20f, 0.65f);
                case ExpeditionRoom072.Gatehouse:
                    return new Vector3(3.75f, 0.20f, 0.50f);
                case ExpeditionRoom072.ReturnRoad:
                    return new Vector3(-4.60f, 0.20f, -0.68f);
                default:
                    return new Vector3(4.15f, 0.20f, 0.72f);
            }
        }

        private void ConfigureRoomStage072(ExpeditionRoom072 room)
        {
            if (_firstHourStage072 == null || _worldCamera == null || _partyRoot == null) return;
            const string hallFallback =
                "SecondDimension/Art/Backgrounds/BG_GUILD_HALL_STAGE_01";
            const string roadFallback =
                "SecondDimension/Art/Battle/BG_TUTORIAL_GATEWORKS_ARENA";

            var expedition076 = _coordinator?.GuildCity017D?.Expedition;
            var backdrop = BackdropResourceForBeatForVerification076(
                room.ToString(),
                expedition076?.CurrentNodeId);
            var fallback = room == ExpeditionRoom072.HallBreach
                ? hallFallback
                : roadFallback;
            var bounds = RoomHorizontalBounds072(room);

            _authoredBackdropResource076 = backdrop;

            _firstHourStage072.Configure072(
                _worldCamera,
                _partyRoot,
                WorldLayer066,
                backdrop,
                fallback,
                bounds,
                new Vector2(-2.0f, 2.25f),
                new Vector3(0f, 5.65f, -12.6f),
                new Vector3(0f, 2.35f, 0.65f),
                room == ExpeditionRoom072.Gatehouse ? 4.65f : 4.78f,
                1.10f,
                1.35f,
                0.24f);
            _paintedVista068 = _firstHourStage072.Backdrop072;
        }

        private void BuildCurrentRoomCast072(GuildCityExpeditionView017D expedition)
        {
            var target = RoomObjectivePosition072(_room072);
            switch (_room072)
            {
                case ExpeditionRoom072.HallBreach:
                {
                    var kael076 = CreateNpc066(
                        "Hall Breach Kael Support 072",
                        // Kael is the destination, not another member of the
                        // departing party. Stage him just beyond the ACT marker
                        // so his body and badge read as one clean focal point.
                        target + new Vector3(1.35f, 0f, 0.10f),
                        new Color(0.58f, 0.74f, 0.82f),
                        HeroArtRepair163.KaelResourceKey,
                        HeroArtRepair163.KaelStandee());
                    _kaelIdentityBadge076 = CreateWorldIdentityBadge076(
                        kael076.transform,
                        "Kael Bell Warden Identity Badge 076",
                        "KAEL  •  BELL WARDEN",
                        new Vector3(0f, 2.08f, 0f),
                        RuntimeUi.Warning,
                        2.15f);
                    CreateEncounterCast072(
                        "Guild Hall Breach Tutorial Encounter 071",
                        target,
                        false);
                    break;
                }
                case ExpeditionRoom072.LanternRoad:
                {
                    var nodeId = expedition?.CurrentNodeId ?? string.Empty;
                    var encounterReady = expedition != null &&
                                         (expedition.CanCommitEncounter ||
                                          StringComparer.Ordinal.Equals(nodeId, "N06") ||
                                          StringComparer.Ordinal.Equals(
                                              expedition.CurrentNodeKind, "OPTIONAL_ELITE"));
                    if (encounterReady)
                        CreateEncounterCast072(
                            StringComparer.Ordinal.Equals(
                                expedition?.CurrentNodeKind, "OPTIONAL_ELITE")
                                ? "Gateworks Optional Elite Encounter 066"
                                : "Gateworks Road Skirmish Encounter 066",
                            target,
                            false);
                    else
                        CreateNpc066(
                            "Gateworks Refugee NPC 066",
                            new Vector3(-1.55f, 0.20f, 0.28f),
                            new Color(0.58f, 0.74f, 0.82f),
                            "SecondDimension/Art/Battle/STANDEE_SIGREC_VEYRA_ASHGLASS");
                    break;
                }
                case ExpeditionRoom072.PatrolRescue:
                    CreateRefugeeGroup066(target);
                    break;
                case ExpeditionRoom072.Gatehouse:
                    CreateEncounterCast072(
                        IsFirstHourThreeBattle071()
                            ? "Old Gatehouse Gate-Eater Boss Encounter 071"
                            : "Gateworks Enemy Encounter 066",
                        target,
                        true);
                    break;
            }
        }

        private GameObject CreateEncounterCast072(string name, Vector3 position, bool boss)
        {
            var root = new GameObject(name);
            root.layer = WorldLayer066;
            root.transform.SetParent(_roomActors072 != null ? _roomActors072 : _worldRoot.transform, false);
            root.transform.position = position + (boss
                ? new Vector3(-2.65f, 0f, 0.55f)
                : new Vector3(-2.80f, 0f, 0.55f));
            _encounterCastRoot076 = root.transform;
            var count = boss ? 1 : 2;
            for (var index = 0; index < count; index++)
            {
                var enemy = new GameObject(
                    boss ? "Gate-Eater Silhouette 072" : "Lantern Road Raider " + (index + 1) + " 072");
                enemy.layer = WorldLayer066;
                enemy.transform.SetParent(root.transform, false);
                enemy.transform.localPosition = boss
                    ? Vector3.zero
                    : new Vector3((index == 0 ? -1f : 1f) * 0.85f, 0f, index * 0.18f);
                CreateAvatarBody066(
                    enemy.transform,
                    enemy.name,
                    new Color(0.68f, 0.12f, 0.16f),
                    false,
                    boss
                        ? "SecondDimension/Art/Battle/ENEMY_GATE_GNAWER_BULWARK"
                        : index == 0
                            ? "SecondDimension/Art/Battle/ENEMY_GATE_GNAWER_SCOUT"
                            : "SecondDimension/Art/Battle/ENEMY_GATE_GNAWER_A",
                    boss ? 2.25f : 1.55f);
            }
            return root;
        }

        private void RebuildRoomPresentation072(ExpeditionRoom072 room, string beatKey)
        {
            var previousRoom072 = _room072;
            var previousPosition072 = _partyRoot != null
                ? _partyRoot.position
                : RoomSpawnPosition072(room, true);
            var preservePosition072 = _partyRoot != null && previousRoom072 == room;
            _room072 = room;
            _roomBeatKey072 = beatKey ?? string.Empty;
            _worldMotor070?.StopImmediately070();
            if (_roomActors072 != null)
            {
                _roomActors072.gameObject.SetActive(false);
                Destroy(_roomActors072.gameObject);
                _roomActors072 = null;
            }
            _interactions.Clear();
            _questInteraction = null;
            _questBeacon = null;
            _questBeaconVisual = null;
            _patrolCastRoot076 = null;
            _encounterCastRoot076 = null;
            _kaelIdentityBadge076 = null;
            _zorinIdentityBadge076 = null;
            _patrolCohortBadge076 = null;
            _campInteraction066 = null;
            _secretInteraction066 = null;

            var controllerWasEnabled = _controller != null && _controller.enabled;
            if (controllerWasEnabled) _controller.enabled = false;
            if (_partyRoot != null)
                _partyRoot.position = preservePosition072
                    ? ClampRoomPosition072(room, previousPosition072)
                    : RoomSpawnPosition072(room, true);
            PreservedPositionOnLastBeatTransitionForVerification076 = preservePosition072;
            for (var index = 0; index < _followers.Count; index++)
            {
                var follower = _followers[index];
                if (follower != null)
                    follower.position = _partyRoot.position + FollowerOffset076(index);
            }
            ConfigureRoomStage072(room);
            BuildStoryInteractions066();
            BuildExpeditionSpace070();
            if (controllerWasEnabled) _controller.enabled = true;
            BeginRoomFadeIn072();
        }

        private void BeginRoomFadeIn072()
        {
            if (_roomFadePanel072 == null || _shutdown) return;
            if (_roomFadeCoroutine072 != null) StopCoroutine(_roomFadeCoroutine072);
            _roomFadeCoroutine072 = StartCoroutine(FadeRoomIn072());
        }

        private IEnumerator FadeRoomIn072()
        {
            if (_roomFadePanel072 == null) yield break;
            _roomFadePanel072.gameObject.SetActive(true);
            var initialColor = _roomFadePanel072.color;
            initialColor.a = 1f;
            _roomFadePanel072.color = initialColor;
            var elapsed = 0f;
            const float duration = 0.32f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var color = _roomFadePanel072.color;
                color.a = 1f - Mathf.Clamp01(elapsed / duration);
                _roomFadePanel072.color = color;
                yield return null;
            }
            _roomFadePanel072.gameObject.SetActive(false);
            _roomFadeCoroutine072 = null;
        }

        private void BuildEnvironment066()
        {
            // Route anchors remain as invisible compatibility/state reference
            // points. The player no longer traverses this forty-metre blockout;
            // FirstHourWorldStage072 builds the small room that is actually seen.
            var anchors = new GameObject("Gateworks Route State Anchors 072");
            anchors.transform.SetParent(_worldRoot.transform, false);
            anchors.layer = WorldLayer066;
            foreach (var node in FirstBoardNodePositions066)
            {
                var anchor = new GameObject("Gateworks Route Anchor " + node.Key + " 066");
                anchor.transform.SetParent(anchors.transform, false);
                anchor.transform.position = node.Value;
                anchor.layer = WorldLayer066;
            }
        }

        private void BuildParty066()
        {
            var guildmasterName076 =
                (_coordinator as IM1PresentationCoordinator)?.State?.GuildmasterName;
            if (string.IsNullOrWhiteSpace(guildmasterName076))
                guildmasterName076 = "Guildmaster";
            var root = new GameObject(
                "Controlled Avatar " + guildmasterName076 + " 076");
            root.transform.SetParent(_worldRoot.transform, false);
            root.layer = WorldLayer066;
            var expedition076 = _coordinator?.GuildCity017D?.Expedition;
            SpawnedForReturnWalk066 = TryBattleReturnPosition066(
                expedition076,
                out _);
            InitialPartyPosition066 = RoomSpawnPosition072(
                _room072,
                SpawnedForReturnWalk066);
            RestoredFieldCheckpointForVerification076 =
                _resumeCheckpoint076 != null &&
                _resumeCheckpoint076.Matches076(
                    expedition076?.ExpeditionId,
                    _room072.ToString());
            if (RestoredFieldCheckpointForVerification076)
                InitialPartyPosition066 = ClampRoomPosition072(
                    _room072,
                    _resumeCheckpoint076.Position);
            _resumeCheckpoint076 = null;
            root.transform.position = InitialPartyPosition066;
            _partyRoot = root.transform;
            _controller = root.AddComponent<CharacterController>();
            _controller.radius = 0.48f;
            _controller.height = 1.85f;
            _controller.center = new Vector3(0f, 0.92f, 0f);
            _controller.stepOffset = 0.35f;

            _partyVisual = new GameObject("Visible Controlled Adventurer 066").transform;
            _partyVisual.SetParent(_partyRoot, false);
            CreateAvatarBody066(
                _partyVisual,
                "Guildmaster Vanguard 066",
                new Color(0.10f, 0.72f, 0.86f),
                true,
                M1VisualAssets.GuildmasterStandeeResourceKey076,
                2.38f);
            GuildmasterStandeeResourceKeyForVerification076 =
                M1VisualAssets.GuildmasterStandeeResourceKey076;
            CreateContactShadow076(
                _partyRoot,
                "Controlled Guildmaster Contact Shadow 076",
                0.92f,
                0.46f);
            _controlledIdentityBadge076 = CreateWorldIdentityBadge076(
                _partyRoot,
                "Controlled Guildmaster Identity Badge 076",
                "YOU  •  GUILDMASTER",
                new Vector3(0f, 2.69f, 0f),
                RuntimeUi.Accent,
                2.34f);
            _namedCompanionIds076.Clear();
            _namedCompanionNames076.Clear();
            var companions076 = ResolveDeployedCompanions076();
            for (var index = 0; index < companions076.Count; index++)
            {
                var companion076 = companions076[index];
                var follower = new GameObject(
                    "Deployed Companion " + companion076.DisplayName + " 076").transform;
                follower.SetParent(_worldRoot.transform, false);
                follower.position = _partyRoot.position + FollowerOffset076(index);
                var standeeResource076 = index == 0
                    ? "SecondDimension/Art/Battle/STANDEE_PROC_F85A4CAA747BC8C6"
                    : "SecondDimension/Art/Battle/STANDEE_PROC_748DD03A23E1FEB0";
                Sprite companionSprite154 = null;
                if (M1VisualAssets.TryResolveBattleStandee(
                        companion076.RecruitId,
                        companion076.VisualSeed,
                        companion076.RaceId,
                        companion076.PortraitAuthorityId,
                        out companionSprite154,
                        out var resolvedResource076) &&
                    !string.IsNullOrWhiteSpace(resolvedResource076))
                    standeeResource076 = resolvedResource076;
                CreateAvatarBody066(
                    follower,
                    "Companion Art " + companion076.RecruitId + " 076",
                    index == 0 ? new Color(0.88f, 0.58f, 0.20f) : new Color(0.60f, 0.35f, 0.82f), false,
                    standeeResource076,
                    2.22f,
                    companionSprite154);
                CreateContactShadow076(
                    follower,
                    "Companion Contact Shadow " + companion076.RecruitId + " 076",
                    0.78f,
                    0.38f);
                _followers.Add(follower);
                _namedCompanionIds076.Add(companion076.RecruitId);
                _namedCompanionNames076.Add(companion076.DisplayName);
            }

            _worldMotor070 = root.AddComponent<WorldCharacterMotor070>();
            _worldMotor070.Configure070(new WorldCharacterMotorTuning070
            {
                WalkSpeed = WalkSpeed066,
                RunSpeed = 8.4f,
                DodgeSpeed = DodgeSpeed066,
                DodgeDuration = DodgeDuration066,
                Acceleration = 20f,
                Braking = 26f,
                TurnSpeedDegrees = 720f
            }, driveFromLegacyKeyboard: false);
            _worldAnimator070 = root.AddComponent<WorldCharacterAnimator070>();
            _worldAnimator070.Configure070(
                _worldMotor070,
                _partyVisual,
                automaticTick: true,
                proceduralFallback: true);
            GuildmasterPoseResourceRootForVerification076 =
                M1VisualAssets.GuildmasterPoseResourceRoot076;
            _worldAnimator070.ConfigureSpritePoses070(
                GuildmasterPoseResourceRootForVerification076);
        }

        private static Vector3 FollowerOffset076(int index)
        {
            var row076 = index / 2;
            var side076 = index % 2 == 0 ? -1.15f : 1.15f;
            return new Vector3(side076, 0f, -(1.38f + row076 * 0.72f));
        }

        private IReadOnlyList<M1RecruitLoadoutView> ResolveDeployedCompanions076()
        {
            var presentation076 = _coordinator as IM1PresentationCoordinator;
            var recruits076 = presentation076?.State?.Recruits ??
                              Array.Empty<M1RecruitLoadoutView>();
            var deployedIds076 = (_coordinator?.GuildCity017D?.Assignments ??
                                  Array.Empty<GuildCityAssignmentView017D>())
                .Where(value => value != null &&
                                StringComparer.OrdinalIgnoreCase.Equals(
                                    value.Kind,
                                    "Deployed") &&
                                !string.IsNullOrWhiteSpace(value.RecruitId))
                .Select(value => value.RecruitId)
                .Concat((presentation076?.State?.Unions ?? Array.Empty<M1UnionView>())
                    .Where(value => value != null &&
                                    value.MemberRecruitIds != null &&
                                    value.MemberRecruitIds.Count > 0)
                    .Select(value => !string.IsNullOrWhiteSpace(value.LeaderRecruitId)
                        ? value.LeaderRecruitId
                        : value.MemberRecruitIds[0]))
                .Distinct(StringComparer.Ordinal)
                .Take(2)
                .ToArray();
            return deployedIds076
                .Select(recruitId076 => recruits076.FirstOrDefault(value =>
                    value != null &&
                    StringComparer.Ordinal.Equals(value.RecruitId, recruitId076)))
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.DisplayName))
                .ToArray();
        }

        private void BuildExpeditionSpace070()
        {
            if (_worldRoot == null || _partyRoot == null || _interactions.Count == 0) return;
            if (_expeditionSpace070 == null)
                _expeditionSpace070 = _worldRoot.AddComponent<ExpeditionSpace070>();
            var definitions070 = new List<ExpeditionStationDefinition070>();
            for (var index = 0; index < _interactions.Count; index++)
            {
                var interaction = _interactions[index];
                if (interaction?.Anchor == null || string.IsNullOrWhiteSpace(interaction.Id)) continue;
                definitions070.Add(new ExpeditionStationDefinition070(
                    interaction.Id,
                    interaction.Title,
                    interaction.Title,
                    ExpeditionStationKindFor070(interaction.Id),
                    _worldRoot.transform.InverseTransformPoint(interaction.Anchor.position),
                    interaction.Radius));
            }
            _expeditionSpace070.Configure070(definitions070, _partyRoot, buildVisibleMarkers: false);
            _expeditionSpace070.BindMotor070(_worldMotor070);
            for (var index = 0; index < _interactions.Count; index++)
            {
                var interaction = _interactions[index];
                if (interaction == null || string.IsNullOrWhiteSpace(interaction.Id)) continue;
                var captured = interaction;
                _expeditionSpace070.RegisterInteraction070(
                    captured.Id,
                    _ => captured.Action?.Invoke());
            }
            _expeditionSpace070.SetObjective070(
                _questInteraction?.Id ?? "QUEST");
        }

        private static ExpeditionStationKind070 ExpeditionStationKindFor070(string interactionId)
        {
            if (string.IsNullOrWhiteSpace(interactionId)) return ExpeditionStationKind070.Arrival;
            if (interactionId.StartsWith("ENCOUNTER", StringComparison.Ordinal))
                return ExpeditionStationKind070.Encounter;
            switch (interactionId)
            {
                case "QUEST": return ExpeditionStationKind070.Objective;
                case "HAZARD": return ExpeditionStationKind070.Hazard;
                case "SECRET": return ExpeditionStationKind070.Secret;
                case "MERCHANT": return ExpeditionStationKind070.Supply;
                case "CAMP": return ExpeditionStationKind070.Guide;
                case "EXIT": return ExpeditionStationKind070.Return;
                case "CREATOR_ROOM": return ExpeditionStationKind070.CreatorRoom;
                case "CODE_CONSOLE": return ExpeditionStationKind070.CodeConsole;
                default: return ExpeditionStationKind070.Guide;
            }
        }

        private static bool TryBattleReturnPosition066(
            GuildCityExpeditionView017D expedition,
            out Vector3 position)
        {
            position = DefaultEntryPosition066;
            if (expedition == null) return false;
            if (expedition.CanFinalizeOperation)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Failed") &&
                    !StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N13") &&
                    !StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N14") &&
                    FirstBoardNodePositions066.TryGetValue(
                        expedition.CurrentNodeId ?? string.Empty, out var failedNodePosition))
                {
                    position = failedNodePosition + new Vector3(-3.8f, 0f, -0.5f);
                    return true;
                }
                position = ReturnWalkPosition066;
                return true;
            }
            var clearedFlag = "ENCOUNTER_CLEARED_" + (expedition.CurrentNodeId ?? string.Empty);
            var clearedHere = (expedition.ObjectiveFlags ?? Array.Empty<string>()).Any(flag =>
                flag.IndexOf(clearedFlag, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!clearedHere || !FirstBoardNodePositions066.TryGetValue(
                    expedition.CurrentNodeId ?? string.Empty, out var currentNodePosition))
                return false;
            position = StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N13")
                ? ReturnWalkPosition066
                : currentNodePosition + new Vector3(-3.8f, 0f, -0.5f);
            return true;
        }

        private void BuildWorldCamera066()
        {
            var cameraObject = new GameObject("Gateworks Following Isometric Camera 066");
            cameraObject.transform.SetParent(_worldRoot.transform, false);
            cameraObject.layer = WorldLayer066;
            _worldCamera = cameraObject.AddComponent<Camera>();
            _worldCamera.clearFlags = CameraClearFlags.SolidColor;
            _worldCamera.backgroundColor = new Color(0.025f, 0.04f, 0.07f);
            _worldCamera.nearClipPlane = 0.15f;
            _worldCamera.farClipPlane = 120f;
            _worldCamera.cullingMask = 1 << WorldLayer066;
            _worldCamera.depth = 80f;
            _firstHourStage072 = _worldRoot.AddComponent<FirstHourWorldStage072>();
            ConfigureRoomStage072(_room072);
        }

        private void BuildStoryInteractions066()
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            var expeditionFlags = expedition?.ObjectiveFlags ?? Array.Empty<string>();
            _secretFound = expeditionFlags.Any(flag => StringComparer.Ordinal.Equals(
                flag, GuildCityExpeditionService017D.GateworksMaintenancePassageFlag066));
            var roomActors = new GameObject("Current Expedition Beat Cast 072");
            roomActors.layer = WorldLayer066;
            roomActors.transform.SetParent(_worldRoot.transform, false);
            _roomActors072 = roomActors.transform;

            BuildCurrentRoomCast072(expedition);

            var questBeacon = new GameObject("Gateworks Contract Objective Beacon 066");
            questBeacon.transform.SetParent(_roomActors072, false);
            questBeacon.transform.position = RoomObjectivePosition072(_room072);
            questBeacon.layer = WorldLayer066;
            _questBeacon = questBeacon.transform;
            AddWorldLabel066(
                _questBeacon,
                "NEXT OBJECTIVE\nINTERACT HERE",
                new Vector3(0f, 3.35f, 0f),
                RuntimeUi.Warning);
            _questBeaconVisual = CreatePrimitive066(
                PrimitiveType.Cylinder,
                "Gold Objective Beacon 073",
                _questBeacon.position + new Vector3(0f, 1.55f, 0f),
                new Vector3(0.18f, 1.55f, 0.18f),
                new Color(1f, 0.64f, 0.12f, 0.90f),
                false).transform;
            _questBeaconVisual.SetParent(_questBeacon, true);
            var returning076 = _room072 == ExpeditionRoom072.ReturnRoad;
            _questInteraction = AddInteraction066(
                returning076 ? "EXIT" : "QUEST",
                returning076 ? "RETURN TO SKYHOME" : "CONTINUE CONTRACT",
                _questBeacon,
                returning076 ? 2.5f : 3.25f,
                returning076
                    ? "Return to the Guild Hall after the operation ends."
                    : "Follow the objective arrow toward the missing patrol and Wayglass.",
                returning076 ? (Action)InteractExit066 : AdvanceQuest066);
        }

        private void BuildHud066()
        {
            RuntimeUi.EnsureEventSystem();
            _hudCanvas = RuntimeUi.CreateCanvas("Outer Gateworks Exploration HUD 066");
            var safe = RuntimeUi.AddSafeArea(_hudCanvas.transform);
            var root = RuntimeUi.AddStretchRect(safe, "Outer Gateworks HUD Root 066");
            _worldInput071 = _hudCanvas.gameObject.AddComponent<WorldInput071>();

            var chapterPanel = RuntimeUi.AddPanel(root, "Gateworks Chapter Panel 073",
                new Color(0.01f, 0.025f, 0.04f, 0.94f));
            Anchor066(chapterPanel.rectTransform, new Vector2(0.025f, 0.905f), new Vector2(0.225f, 0.975f));
            _chapterText073 = RuntimeUi.AddText(chapterPanel.transform, "Gateworks Chapter Text 073",
                "CHAPTER 1  •  LANTERN ROAD", 22, TextAnchor.MiddleCenter,
                RuntimeUi.Accent, FontStyle.Bold);
            Stretch066(_chapterText073.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -4f));

            var objectivePanel = RuntimeUi.AddPanel(root, "Gateworks Objective Panel 066", new Color(0.01f, 0.025f, 0.04f, 0.94f));
            Anchor066(objectivePanel.rectTransform, new Vector2(0.24f, 0.89f), new Vector2(0.745f, 0.975f));
            _objectiveText = RuntimeUi.AddText(objectivePanel.transform, "Gateworks Plain Objective 066",
                string.Empty, 24, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            Stretch066(_objectiveText.rectTransform, new Vector2(18f, 7f), new Vector2(-18f, -7f));
            _objectiveText.resizeTextForBestFit = true;
            _objectiveText.resizeTextMinSize = 18;
            _objectiveText.resizeTextMaxSize = 24;
            _objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _objectiveText.verticalOverflow = VerticalWrapMode.Truncate;

            var xpPanel073 = RuntimeUi.AddPanel(root, "Gateworks Spendable XP Panel 073",
                new Color(0.01f, 0.025f, 0.04f, 0.94f));
            Anchor066(xpPanel073.rectTransform, new Vector2(0.76f, 0.905f), new Vector2(0.975f, 0.975f));
            _xpText073 = RuntimeUi.AddText(xpPanel073.transform, "Gateworks Spendable XP Text 073",
                string.Empty, 19, TextAnchor.MiddleCenter, RuntimeUi.Positive, FontStyle.Bold);
            Stretch066(_xpText073.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -4f));

            var routePanel073 = RuntimeUi.AddPanel(root, "Gateworks Route Strip 073",
                new Color(0.01f, 0.025f, 0.04f, 0.92f));
            Anchor066(routePanel073.rectTransform, new Vector2(0.24f, 0.832f), new Vector2(0.975f, 0.882f));
            _routeText073 = RuntimeUi.AddText(routePanel073.transform, "Gateworks Route Text 073",
                string.Empty, 21, TextAnchor.MiddleCenter, RuntimeUi.Warning, FontStyle.Bold);
            Stretch066(_routeText073.rectTransform, new Vector2(14f, 3f), new Vector2(-14f, -3f));
            var routeTrack073 = RuntimeUi.AddPanel(routePanel073.transform, "Gateworks Route Track 073",
                new Color(1f, 1f, 1f, 0.12f));
            Anchor066(routeTrack073.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.11f));
            _routeFill073 = RuntimeUi.AddPanel(routeTrack073.transform, "Gateworks Route Fill 073",
                RuntimeUi.Warning);
            _routeFill073.raycastTarget = false;

            var storyPanel073 = RuntimeUi.AddPanel(root, "Gateworks Story So Far Panel 073",
                new Color(0.015f, 0.03f, 0.045f, 0.91f));
            Anchor066(storyPanel073.rectTransform, new Vector2(0.025f, 0.635f), new Vector2(0.58f, 0.815f));
            _storyPartyText076 = RuntimeUi.AddText(
                storyPanel073.transform,
                "Gateworks Deployed Party Strip 076",
                string.Empty,
                20,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            Anchor066(
                _storyPartyText076.rectTransform,
                new Vector2(0.035f, 0.69f),
                new Vector2(0.965f, 0.96f));
            _storyPartyText076.resizeTextForBestFit = true;
            _storyPartyText076.resizeTextMinSize = 18;
            _storyPartyText076.resizeTextMaxSize = 20;
            _storyPartyText076.horizontalOverflow = HorizontalWrapMode.Wrap;
            _storyPartyText076.verticalOverflow = VerticalWrapMode.Truncate;
            _storyContextText073 = RuntimeUi.AddText(storyPanel073.transform, "Gateworks Story So Far Text 073",
                string.Empty, 26, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            Anchor066(
                _storyContextText073.rectTransform,
                new Vector2(0.035f, 0.06f),
                new Vector2(0.965f, 0.68f));
            _storyContextText073.resizeTextForBestFit = true;
            _storyContextText073.resizeTextMinSize = 22;
            _storyContextText073.resizeTextMaxSize = 26;
            _storyContextText073.horizontalOverflow = HorizontalWrapMode.Wrap;
            _storyContextText073.verticalOverflow = VerticalWrapMode.Truncate;

            var controls = RuntimeUi.AddPanel(root, "Gateworks Controls Panel 066", new Color(0.01f, 0.025f, 0.04f, 0.91f));
            _controlsPanel071 = controls;
            Anchor066(controls.rectTransform, new Vector2(0.025f, 0.018f), new Vector2(0.235f, 0.160f));
            _controlsText076 = RuntimeUi.AddText(controls.transform, "Gateworks Controls 066",
                "MOVE  WASD / STICK  •  RUN  SHIFT\nROLL  SPACE / B  •  ACT  E / A",
                22, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            Stretch066(_controlsText076.rectTransform, new Vector2(12f, 5f), new Vector2(-12f, -5f));

            var promptPanel = RuntimeUi.AddPanel(root, "Gateworks Interaction Prompt Panel 066", new Color(0.06f, 0.04f, 0.015f, 0.94f));
            Anchor066(promptPanel.rectTransform, new Vector2(0.255f, 0.035f), new Vector2(0.745f, 0.12f));
            _promptText = RuntimeUi.AddText(promptPanel.transform, "Gateworks Nearby Interaction Prompt 066",
                "Follow the objective.", 29, TextAnchor.MiddleCenter, RuntimeUi.Warning, FontStyle.Bold);
            Stretch066(_promptText.rectTransform, new Vector2(14f, 4f), new Vector2(-14f, -4f));

            _routeChoicePanel068 = RuntimeUi.AddPanel(root, "Gateworks Route Choice Panel 068", new Color(0.015f, 0.03f, 0.045f, 0.97f));
            Anchor066(_routeChoicePanel068.rectTransform, new Vector2(0.20f, 0.18f), new Vector2(0.80f, 0.285f));
            RuntimeUi.AddHorizontalLayout(_routeChoicePanel068.transform, new RectOffset(12, 12, 10, 10), 14f, TextAnchor.MiddleCenter);
            var routeOne068 = RuntimeUi.AddButton(
                _routeChoicePanel068.transform,
                "Gateworks Route Choice One 068",
                "1 • UPPER CAUSEWAY",
                () => ApplyRouteChoiceInput066(0),
                92f,
                RuntimeUi.Accent);
            var routeTwo068 = RuntimeUi.AddButton(
                _routeChoicePanel068.transform,
                "Gateworks Route Choice Two 068",
                "2 • LOWER TUNNEL",
                () => ApplyRouteChoiceInput066(1),
                92f,
                RuntimeUi.ButtonNormal);
            _routeChoiceOneText068 = routeOne068.GetComponentInChildren<Text>();
            _routeChoiceTwoText068 = routeTwo068.GetComponentInChildren<Text>();
            _routeChoicePanel068.gameObject.SetActive(false);

            var messagePanel = RuntimeUi.AddPanel(root, "Gateworks Story Message Panel 066", new Color(0.015f, 0.03f, 0.045f, 0.94f));
            _messagePanel071 = messagePanel;
            Anchor066(messagePanel.rectTransform, new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.225f));
            _messageText = RuntimeUi.AddText(messagePanel.transform, "Gateworks Story Message 066",
                string.Empty, 24, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            _messageText.resizeTextForBestFit = true;
            _messageText.resizeTextMinSize = 22;
            _messageText.resizeTextMaxSize = 24;
            Stretch066(_messageText.rectTransform, new Vector2(22f, 10f), new Vector2(-22f, -10f));
            messagePanel.gameObject.SetActive(false);

            _dicePanel = RuntimeUi.AddPanel(root, "Gateworks Visible 2D6 Panel 066", new Color(0.025f, 0.035f, 0.05f, 0.97f));
            Anchor066(_dicePanel.rectTransform, new Vector2(0.31f, 0.35f), new Vector2(0.69f, 0.62f));
            _diceText = RuntimeUi.AddText(_dicePanel.transform, "Gateworks Visible 2D6 Result 066",
                "2D6 CHECK\n[ ? ] + [ ? ]", 42, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            Stretch066(_diceText.rectTransform, new Vector2(20f, 16f), new Vector2(-20f, -16f));
            _dicePanel.gameObject.SetActive(false);
            _worldInput071.BuildTouchControls071(root, true, false);
            controls.gameObject.SetActive(!_worldInput071.TouchControlsVisible071);

            _roomFadePanel072 = RuntimeUi.AddPanel(
                root,
                "First Hour Room Fade 072",
                Color.black);
            Stretch066(_roomFadePanel072.rectTransform, Vector2.zero, Vector2.zero);
            _roomFadePanel072.raycastTarget = false;
            _roomFadePanel072.transform.SetAsLastSibling();
            BeginRoomFadeIn072();
        }

        private void AdvanceQuest066()
        {
            if (_interactionLocked) return;
            var state = _coordinator?.GuildCity017D;
            var expedition = state?.Expedition;
            if (expedition == null)
            {
                ShowMessage066("No expedition is active. Return to the Guild Hall and accept Kiri's Lantern Road order.");
                return;
            }
            if (state.HasPendingEncounter)
            {
                ShowMessage066(StoryBattleLaunchMessage071(expedition));
                _requestBattle?.Invoke();
                return;
            }
            if (expedition.CanFinalizeOperation)
            {
                InteractExit066();
                return;
            }
            if (IsFirstHourThreeBattle071() &&
                StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N13") &&
                !FirstHourLanternPatrolRescued071(expedition))
            {
                InteractObjective066();
                return;
            }
            if (!expedition.ResolutionComplete &&
                (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "EVENT") ||
                 StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "SKILL_CHECK")))
            {
                StartCoroutine(RollCommittedCheck066(expedition));
                return;
            }
            if (!IsFirstHourThreeBattle071() &&
                StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "OPTIONAL_ELITE") &&
                expedition.CanCommitEncounter && expedition.CanMove &&
                expedition.LinkedNodeIds != null && expedition.LinkedNodeIds.Count > 0)
            {
                _optionalEliteChoiceActive = true;
                _routeChoiceActive = true;
                _worldMotor070?.StopImmediately070();
                _routeChoices = expedition.LinkedNodeIds.Take(1).ToArray();
                ShowMessage066("An elite raider guards the Gatehouse causeway. Press 1 to fight it, or 2 to bypass it and continue toward the patrol.");
                ShowRouteChoices068(true);
                UpdateInteractionPrompt066();
                return;
            }
            if (expedition.CanCommitEncounter)
            {
                var committed = _coordinator.CommitGuildCityEncounter017D(expedition.CurrentEncounterId);
                ShowMessage066(committed?.Succeeded == true
                    ? StoryBattleLaunchMessage071(expedition)
                    : committed?.Message ?? "Something went wrong—try again.");
                RefreshQuestGuidance066();
                if (committed?.Succeeded == true) _requestBattle?.Invoke();
                return;
            }
            if (expedition.CanMove && expedition.LinkedNodeIds != null && expedition.LinkedNodeIds.Count > 0)
            {
                if (expedition.LinkedNodeIds.Count > 1)
                {
                    _routeChoices = expedition.LinkedNodeIds.Take(2).ToArray();
                    _routeChoiceActive = true;
                    _worldMotor070?.StopImmediately070();
                    ShowMessage066("The road forks here. Press 1 for the upper causeway or 2 for the lower service tunnel.");
                    ShowRouteChoices068(false);
                    UpdateInteractionPrompt066();
                }
                else CommitRouteChoice066(0);
                return;
            }
            ShowMessage066("Search this part of Lantern Road and speak with nearby scouts. The objective arrow will update when the route is secure.");
        }

        private void CommitOptionalEliteChoice066(int index)
        {
            if (index < 0 || index > 1) return;
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            if (expedition == null ||
                !StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "OPTIONAL_ELITE"))
            {
                ClearRouteChoice066();
                return;
            }

            if (index == 0)
            {
                ClearRouteChoice066();
                var committed = _coordinator.CommitGuildCityEncounter017D(expedition.CurrentEncounterId);
                ShowMessage066(committed?.Succeeded == true
                    ? "You challenge the elite patrol. Entering the Union battle arena..."
                    : committed?.Message ?? "Something went wrong—try again.");
                RefreshQuestGuidance066();
                if (committed?.Succeeded == true) _requestBattle?.Invoke();
                return;
            }

            if (_routeChoices.Count == 0) return;
            var bypassDestination = _routeChoices[0];
            ClearRouteChoice066();
            MoveToNode066(bypassDestination,
                "Your party slips around the elite raider and continues toward the missing patrol and Wayglass.");
        }

        private void CommitRouteChoice066(int index)
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            var choices = _routeChoiceActive ? _routeChoices : expedition?.LinkedNodeIds;
            if (choices == null || index < 0 || index >= choices.Count) return;
            var destination = choices[index];
            ClearRouteChoice066();
            MoveToNode066(destination, "Your party advances along Lantern Road toward the old Gatehouse. Progress saved.");
        }

        private void MoveToNode066(string destination, string successMessage)
        {
            var moved = _coordinator.MoveGuildCityExpedition017D(destination);
            if (moved?.Succeeded == true)
                PlayExpeditionMoveCue071();
            var arrived = moved?.Succeeded == true ? _coordinator?.GuildCity017D?.Expedition : null;
            if (arrived != null && IsFirstHourThreeBattle071() &&
                StringComparer.Ordinal.Equals(arrived.CurrentNodeId, "N13") &&
                !FirstHourLanternPatrolRescued071(arrived))
            {
                ShowMessage066(
                    "You found Zorin and all ten Lantern Patrol members protecting the Wayglass. Reach the patrol and rally them before facing the Gate-Eater.");
                RefreshQuestGuidance066();
                return;
            }
            if (arrived != null && IsStreamlinedFirstRescue069() &&
                StringComparer.Ordinal.Equals(arrived.CurrentNodeId, destination) &&
                arrived.CanCommitEncounter)
            {
                var committed = _coordinator.CommitGuildCityEncounter017D(arrived.CurrentEncounterId);
                ShowMessage066(committed?.Succeeded == true
                    ? StoryBattleLaunchMessage071(arrived)
                    : committed?.Message ?? "Something went wrong—try the Lantern Road operation again.");
                RefreshQuestGuidance066();
                if (committed?.Succeeded == true) _requestBattle?.Invoke();
                return;
            }
            if (arrived != null &&
                (arrived.CanFinalizeOperation ||
                 StringComparer.Ordinal.Equals(arrived.CurrentNodeId, "N14")))
            {
                ShowMessage066(
                    "The Gate-Eater is defeated. Zorin's patrol and the Wayglass are secure—follow the gold marker back to Skyhome.");
                RefreshQuestGuidance066();
                return;
            }
            ShowMessage066(moved?.Succeeded == true
                ? successMessage
                : moved?.Message ?? "Something went wrong—try again.");
            RefreshQuestGuidance066();
        }

        private void PlayExpeditionMoveCue071()
        {
            if (_expeditionAudio071 == null) return;
            var cue = Resources.Load<AudioClip>(
                "SecondDimension/GuildCity017F/Audio/SFX_EXPEDITION_MOVE_017F");
            if (cue != null)
                _expeditionAudio071.PlayOneShot(cue, 0.58f);
        }

        private void ClearRouteChoice066()
        {
            _routeChoiceActive = false;
            _optionalEliteChoiceActive = false;
            _routeChoices = Array.Empty<string>();
            if (_routeChoicePanel068 != null) _routeChoicePanel068.gameObject.SetActive(false);
        }

        private void ShowRouteChoices068(bool optionalElite)
        {
            if (_routeChoicePanel068 == null) return;
            if (_routeChoiceOneText068 != null)
                _routeChoiceOneText068.text = optionalElite ? "1 • FIGHT ELITE" : "1 • UPPER CAUSEWAY";
            if (_routeChoiceTwoText068 != null)
                _routeChoiceTwoText068.text = optionalElite ? "2 • BYPASS" : "2 • LOWER TUNNEL";
            _routeChoicePanel068.gameObject.SetActive(true);
        }

        private IEnumerator RollCommittedCheck066(GuildCityExpeditionView017D expedition)
        {
            if (_interactionLocked) yield break;
            _interactionLocked = true;
            _worldMotor070?.StopImmediately070();
            var assignments = _coordinator.GuildCity017D.Assignments
                .Where(value => value != null &&
                                StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Deployed") &&
                                !string.IsNullOrWhiteSpace(value.RecruitId))
                .Take(2).ToArray();
            if (assignments.Length == 0)
            {
                if (_dicePanel != null) _dicePanel.gameObject.SetActive(false);
                ShowMessage066("No deployed adventurer can attempt this challenge. Return to the Guild Hall and assign a party member.");
                _interactionLocked = false;
                UpdateInteractionPrompt066();
                yield break;
            }

            var eventId = EventIdForCheck066(expedition);
            var checkLabel = CheckLabel066(eventId);
            var modifier = assignments.Length > 1 ? 2 : 0;
            _dicePanel.gameObject.SetActive(true);
            for (var frame = 0; frame < 10; frame++)
            {
                var one = (frame * 5 + 2) % 6 + 1;
                var two = (frame * 3 + 4) % 6 + 1;
                _diceText.text = checkLabel + " • ROLL 2D6\n[ " + one + " ] + [ " + two + " ] + " + modifier;
                yield return new WaitForSecondsRealtime(0.08f);
            }
            var result = _coordinator.ResolveGuildCityCheck017D(
                eventId,
                assignments[0].RecruitId,
                assignments.Length > 1 ? assignments[1].RecruitId : string.Empty,
                modifier);
            var resolved = _coordinator.GuildCity017D.Expedition;
            if (result != null && result.Succeeded && resolved != null)
            {
                _diceText.text = checkLabel + " • 2D6 RESULT\n[ " + resolved.LastCheckDieOne + " ] + [ " +
                    resolved.LastCheckDieTwo + " ] + " + resolved.LastCheckModifier + " = " +
                    resolved.LastCheckTotal + "\n" + Humanize066(resolved.LastCheckOutcome);
                ShowMessage066("The way ahead is open.");
            }
            else _diceText.text = "SOMETHING WENT WRONG—TRY AGAIN\n" +
                                  (result?.Message ?? "The challenge is still waiting.");
            yield return new WaitForSecondsRealtime(1.5f);
            if (_dicePanel != null) _dicePanel.gameObject.SetActive(false);
            _interactionLocked = false;
            RefreshQuestGuidance066();
        }

        private static string EventIdForCheck066(GuildCityExpeditionView017D expedition)
        {
            if (!string.IsNullOrWhiteSpace(expedition?.CurrentEventId)) return expedition.CurrentEventId;
            switch (expedition?.CurrentNodeId)
            {
                case "N02": return "EVENT_INJURED_COURIER";
                case "N04": return "EVENT_COLLAPSED_HANDRAIL";
                case "N08": return "EVENT_UNSTABLE_BELL_CHAIN";
                case "N10": return "EVENT_TRAPPED_FOREMAN";
                default: return "EVENT_ROUTE_CHALLENGE";
            }
        }

        private static string CheckLabel066(string eventId)
        {
            switch (eventId)
            {
                case "EVENT_INJURED_COURIER": return "PATROL RUNNER UNDER THE ARCH";
                case "EVENT_COLLAPSED_HANDRAIL": return "BROKEN LANTERN WAYMARKER";
                case "EVENT_UNSTABLE_BELL_CHAIN": return "WAYGLASS RESONANCE";
                case "EVENT_TRAPPED_FOREMAN": return "PATROL BEHIND THE SUPPORT";
                default: return "ROUTE CHALLENGE";
            }
        }

        private void InteractHazard066()
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            if (expedition != null &&
                StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N04"))
                AdvanceQuest066();
            else ShowMessage066("The broken Lantern Road causeway is unstable. Follow the objective arrow to the safe crossing.");
        }

        private void InteractEnemy066(string relevantNodeId)
        {
            var state = _coordinator?.GuildCity017D;
            var expedition = state?.Expedition;
            if (expedition == null)
            {
                ShowMessage066("No Lantern Road order is active. Return to the Guild Hall before confronting the Gatehouse threat.");
                return;
            }
            if (!StringComparer.Ordinal.Equals(expedition.CurrentNodeId, relevantNodeId))
            {
                ShowMessage066("The Gatehouse threat is still beyond your current route. Follow the road to the missing patrol and Wayglass first.");
                return;
            }
            if (IsFirstHourThreeBattle071() &&
                StringComparer.Ordinal.Equals(relevantNodeId, "N13") &&
                !FirstHourLanternPatrolRescued071(expedition))
            {
                ShowMessage066(
                    "The Gate-Eater cuts you off from Zorin's pinned patrol. Reach them, secure the Wayglass, and rally all ten members before starting Battle 3.");
                RefreshQuestGuidance066();
                return;
            }
            if (state.HasPendingEncounter)
            {
                ShowMessage066(StoryBattleLaunchMessage071(expedition));
                _requestBattle?.Invoke();
                return;
            }
            if (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "OPTIONAL_ELITE") &&
                expedition.CanCommitEncounter && expedition.CanMove)
            {
                AdvanceQuest066();
                return;
            }
            if (expedition.CanCommitEncounter)
            {
                var committed = _coordinator.CommitGuildCityEncounter017D(expedition.CurrentEncounterId);
                ShowMessage066(committed?.Succeeded == true
                    ? StoryBattleLaunchMessage071(expedition)
                    : committed?.Message ?? "Something went wrong—try again.");
                RefreshQuestGuidance066();
                if (committed?.Succeeded == true) _requestBattle?.Invoke();
                return;
            }
            if (StringComparer.Ordinal.Equals(relevantNodeId, "N13") &&
                expedition.CanMove && expedition.LinkedNodeIds != null &&
                expedition.LinkedNodeIds.Any(value => StringComparer.Ordinal.Equals(value, "N14")))
            {
                MoveToNode066("N14", "The Gatehouse threat is defeated. The patrol and Wayglass are secure; follow the gold markers back to Skyhome.");
                return;
            }
            if (expedition.CanMove)
            {
                AdvanceQuest066();
                return;
            }
            ShowMessage066("The Gatehouse threat is still beyond your current route. Follow the road to the missing patrol and Wayglass first.");
        }

        private void InteractExit066()
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            if (expedition != null && expedition.CanFinalizeOperation)
            {
                var finalized = _coordinator.FinalizeGuildCityOperation017D();
                ShowMessage066(finalized?.Succeeded == true
                    ? "Your party passes through the gate and returns to the Guild Hall."
                    : finalized?.Message ?? "Something went wrong—try again.");
                if (finalized != null && finalized.Succeeded) _returnToQuest?.Invoke();
                return;
            }
            ShowMessage066(expedition == null
                ? "No Lantern Road order is active. The Guild Hall is just beyond this gate."
                : "The patrol and Wayglass are not secure yet. Follow Lantern Road before returning to Skyhome.");
        }

        private void InteractObjective066()
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            if (expedition != null && IsFirstHourThreeBattle071() &&
                StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N13"))
            {
                if (FirstHourLanternPatrolRescued071(expedition))
                {
                    ShowMessage066(
                        "Zorin's patrol is safe and the Wayglass is secured. Their reinforcement Unions are ready—face the Gate-Eater.");
                    RefreshQuestGuidance066();
                    return;
                }
                if (!(_coordinator is IFirstHourPatrolRescueCoordinator071 rescueCoordinator071))
                {
                    ShowMessage066(
                        "The patrol rescue command is unavailable. Return to the Guild Hall and resume this saved operation.");
                    return;
                }

                var rescued = rescueCoordinator071.RescueFirstHourLanternPatrol071();
                var rescuedSuccessfully076 = rescued?.Succeeded == true;
                ShowMessage066(rescuedSuccessfully076
                    ? "Zorin: The Wayglass is safe. All ten of us are with your Guild now. Reinforcement Unions, on me—we face the Gate-Eater together!"
                    : rescued?.Message ?? "The patrol could not be secured. Stay at the rescue marker and try again.");
                RefreshQuestGuidance066();
                if (rescuedSuccessfully076)
                {
                    PatrolRescueCeremonyRequestedForVerification076 = true;
                    _returnToQuest?.Invoke();
                }
                return;
            }
            if (expedition != null && StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N11") &&
                expedition.CanMove && expedition.LinkedNodeIds != null && expedition.LinkedNodeIds.Count > 0)
            {
                ShowMessage066("The Lantern patrol is alive and guarding the Wayglass. The Gatehouse threat blocks their road home.");
                CommitRouteChoice066(0);
                return;
            }
            ShowMessage066("The missing patrol is sheltering with the Wayglass. Follow the road until your route reaches them.");
        }

        private void InteractCamp066()
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            if (expedition == null || !StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N07"))
            {
                ShowMessage066("The watch camp is quiet. Follow the objective arrow before stopping here.");
                return;
            }
            UseCamp066();
            if (_campInteraction066 != null)
                _interactions.Remove(_campInteraction066);
        }

        private void InteractSecret066()
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            if (expedition == null || !StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N05"))
            {
                ShowMessage066("The cracked wall is sealed from this side. Continue deeper along Lantern Road.");
                return;
            }
            var result = _coordinator.DiscoverGateworksMaintenancePassage066();
            _secretFound = result != null && result.Succeeded;
            ShowMessage066(_secretFound
                ? "SECRET FOUND • The maintenance cache reveals a hidden service passage behind the cracked wall. The safer way preserves 1 supply."
                : result?.Message ?? "The cache will not open yet.");
            if (_secretFound && _secretInteraction066 != null)
                _interactions.Remove(_secretInteraction066);
        }

        private void UseCamp066()
        {
            if (_campShared)
            {
                ShowMessage066("Your party has already shared this watch.");
                return;
            }
            var assignments = _coordinator?.GuildCity017D?.Assignments
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.RecruitId))
                .Take(2).ToArray() ?? Array.Empty<GuildCityAssignmentView017D>();
            if (assignments.Length < 2)
            {
                ShowMessage066("Two deployed companions are needed to share the watch camp.");
                return;
            }
            var result = _coordinator.AddGuildCityRelationshipMemory017D(
                assignments[0].RecruitId, assignments[1].RecruitId,
                "CAMP_OUTER_GATEWORKS_066",
                "Shared a quiet watch on Lantern Road beneath the old Gatehouse.", 1,
                "REL_SCENE_OUTER_GATEWORKS_WATCH_066");
            _campShared = result != null && result.Succeeded;
            ShowMessage066(_campShared
                ? "The two companions share a quiet Lantern Road watch. No day passes."
                : result?.Message ?? "They cannot settle here yet.");
        }

        private void RefreshQuestGuidance066()
        {
            var state = _coordinator?.GuildCity017D;
            var expedition = state?.Expedition;
            var requestedRoom072 = ResolveExpeditionRoom072(expedition);
            var requestedBeatKey072 = ResolveRoomBeatKey072(expedition);
            if (_firstHourStage072 != null &&
                (requestedRoom072 != _room072 ||
                 !StringComparer.Ordinal.Equals(requestedBeatKey072, _roomBeatKey072)))
                RebuildRoomPresentation072(requestedRoom072, requestedBeatKey072);
            RefreshPaintedExpeditionVista071();
            if (_objectiveText == null || _questBeacon == null) return;
            if (expedition == null)
            {
                _objectiveText.text = "NO ACTIVE CONTRACT • RETURN TO THE GUILD HALL";
                return;
            }
            _secretFound = _secretFound || (expedition.ObjectiveFlags ?? Array.Empty<string>()).Any(flag =>
                StringComparer.Ordinal.Equals(flag, GuildCityExpeditionService017D.GateworksMaintenancePassageFlag066));
            ClearRouteChoice066();
            var nodeId = string.IsNullOrWhiteSpace(expedition.CurrentNodeId) ? "N00" : expedition.CurrentNodeId;
            RefreshStoryActorVisibility071(expedition, state.HasPendingEncounter);
            var patrolRescuePending071 = IsFirstHourThreeBattle071() &&
                StringComparer.Ordinal.Equals(nodeId, "N13") &&
                !FirstHourLanternPatrolRescued071(expedition);
            var beaconNodeId = ObjectiveDestinationNode066(expedition, state.HasPendingEncounter);
            var beaconPosition = RoomObjectivePosition072(_room072);
            var objective = "FIND THE MISSING PATROL AND RECOVER THE WAYGLASS";
            var title = "CONTINUE ALONG LANTERN ROAD";
            if (state.HasPendingEncounter)
            {
                objective = IsFirstHourThreeBattle071()
                    ? StoryBattleObjective071(expedition)
                    : "THE ENEMY IS READY • APPROACH AND USE ACT";
                title = IsFirstHourThreeBattle071()
                    ? StoryBattleTitle071(expedition)
                    : "ENTER UNION BATTLE";
            }
            else if (expedition.CanFinalizeOperation)
            {
                _questBeacon.position = RoomObjectivePosition072(ExpeditionRoom072.ReturnRoad);
                if (StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Failed"))
                {
                    _objectiveText.text = "OPERATION FAILED • RETREAT TO THE SKYHOME EXIT";
                    _questInteraction.Title = "BRING THE PARTY HOME";
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Extracted"))
                {
                    _objectiveText.text = "PARTY EXTRACTED • WALK BACK TO THE SKYHOME EXIT";
                    _questInteraction.Title = "RETURN AFTER EXTRACTION";
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(expedition.Status, "Completed"))
                {
                    _objectiveText.text = "OBJECTIVE COMPLETE • WALK BACK TO THE SKYHOME EXIT";
                    _questInteraction.Title = "RETURN TO GUILD HALL";
                }
                else
                {
                    _objectiveText.text = "OPERATION ENDED • WALK BACK TO THE SKYHOME EXIT";
                    _questInteraction.Title = "RETURN TO GUILD HALL";
                }
                RefreshStoryActorVisibility071(expedition, state.HasPendingEncounter);
                return;
            }
            else if (patrolRescuePending071)
            {
                objective = "FREE ZORIN'S TEN-MEMBER LANTERN PATROL • SECURE THE WAYGLASS";
                title = "RALLY THE LANTERN PATROL";
            }
            else if (!expedition.ResolutionComplete &&
                     (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "EVENT") ||
                      StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "SKILL_CHECK")))
            {
                objective = "INVESTIGATE THE HIGHLIGHTED HAZARD • USE ACT TO ROLL 2D6";
                title = "ROLL 2D6";
            }
            else if (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "OPTIONAL_ELITE") &&
                     expedition.CanCommitEncounter && expedition.CanMove)
            {
                objective = "ELITE PATROL AHEAD • INTERACT TO CHOOSE FIGHT OR BYPASS";
                title = "CHOOSE YOUR APPROACH";
            }
            else if (expedition.CanCommitEncounter)
            {
                objective = IsFirstHourThreeBattle071()
                    ? StoryBattleObjective071(expedition)
                    : StringComparer.Ordinal.Equals(nodeId, "N06") && IsStreamlinedFirstRescue069()
                        ? "REACH THE MISSING PATROL AND WAYGLASS • USE ACT TO BEGIN THE GATEHOUSE BATTLE"
                        : "APPROACH THE GATEHOUSE THREAT • USE ACT TO STAND AND FIGHT";
                title = IsFirstHourThreeBattle071()
                    ? StoryBattleTitle071(expedition)
                    : StringComparer.Ordinal.Equals(nodeId, "N06") && IsStreamlinedFirstRescue069()
                        ? "DEFEND THE LANTERN PATROL"
                        : "APPROACH GATEHOUSE THREAT";
            }
            else if (RescueIsComplete066(expedition))
            {
                objective = "PATROL AND WAYGLASS SECURE • BRING EVERYONE HOME THROUGH THE SKYHOME EXIT";
                title = "RETURN HOME";
            }
            else if (IsFirstHourThreeBattle071())
            {
                switch (beaconNodeId)
                {
                    case "N01":
                        objective = "THE BELL OPENED A BREACH BELOW THE HALL • REACH KAEL";
                        title = "BATTLE 1 OF 3 • HALL BREACH";
                        break;
                    case "N04":
                        objective = "REPAIR THE BROKEN LANTERN WAYMARKER • ROLL 2D6";
                        title = "OPEN LANTERN ROAD";
                        break;
                    case "N06":
                        objective = "RAIDERS CUT THE ROAD TO THE PATROL • BREAK THEIR LINE";
                        title = "BATTLE 2 OF 3 • ROAD AMBUSH";
                        break;
                    case "N13":
                        objective = "ZORIN'S PATROL AND WAYGLASS ARE SECURE • FACE THE GATE-EATER";
                        title = "BATTLE 3 OF 3 • FIGHT WITH THE PATROL";
                        break;
                }
            }
            else if (StringComparer.Ordinal.Equals(beaconNodeId, "N06") && IsStreamlinedFirstRescue069())
            {
                objective = "CROSS THE RUINED LANTERN ROAD CAUSEWAY • THE MISSING PATROL IS JUST AHEAD";
                title = "REACH THE OLD GATEHOUSE";
            }
            _questBeacon.position = beaconPosition;
            if (_expeditionSpace070 != null &&
                _expeditionSpace070.TryGetStation070("QUEST", out var questStation070))
            {
                questStation070.transform.position = beaconPosition;
                _expeditionSpace070.SetObjective070("QUEST");
            }
            _objectiveText.text = "NEXT  •  " + objective;
            _questInteraction.Title = title;
        }

        private static string ObjectiveDestinationNode066(
            GuildCityExpeditionView017D expedition,
            bool hasPendingEncounter)
        {
            if (expedition == null) return "N00";
            var current = string.IsNullOrWhiteSpace(expedition.CurrentNodeId)
                ? "N00"
                : expedition.CurrentNodeId;
            if (expedition.CanFinalizeOperation) return "N14";
            if (hasPendingEncounter || expedition.CanCommitEncounter ||
                (!expedition.ResolutionComplete &&
                 (StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "EVENT") ||
                  StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "SKILL_CHECK"))) ||
                StringComparer.Ordinal.Equals(expedition.CurrentNodeKind, "OPTIONAL_ELITE"))
                return current;
            return expedition.CanMove && expedition.LinkedNodeIds != null && expedition.LinkedNodeIds.Count == 1
                ? expedition.LinkedNodeIds[0]
                : current;
        }

        private bool RescueIsComplete066(GuildCityExpeditionView017D expedition)
        {
            var flags = expedition?.ObjectiveFlags ?? Array.Empty<string>();
            if (FirstHourLanternPatrolRescued071(expedition) || flags.Any(flag =>
                    StringComparer.Ordinal.Equals(flag, "PRIMARY_OBJECTIVE_RESCUE_COMPLETE") ||
                    StringComparer.Ordinal.Equals(flag,
                        GuildCityExpeditionService017D.EncounterClearedFlag("N13"))))
                return true;
            return !IsFirstHourThreeBattle071() && flags.Any(flag =>
                StringComparer.Ordinal.Equals(flag,
                    GuildCityExpeditionService017D.EncounterClearedFlag("N06")));
        }

        private static string StoryBattleTitle071(GuildCityExpeditionView017D expedition)
        {
            switch (expedition?.CurrentEncounterId)
            {
                case "ENCOUNTER071_HALL_BREACH": return "ENTER BATTLE 1 OF 3 • HALL BREACH";
                case "ENCOUNTER071_LANTERN_ROAD_AMBUSH": return "ENTER BATTLE 2 OF 3 • ROAD AMBUSH";
                case "ENCOUNTER071_GATE_EATER": return "ENTER BATTLE 3 OF 3 • GATE-EATER";
                default: return "ENTER UNION BATTLE";
            }
        }

        private static string StoryBattleObjective071(GuildCityExpeditionView017D expedition)
        {
            switch (expedition?.CurrentEncounterId)
            {
                case "ENCOUNTER071_HALL_BREACH":
                    return "BATTLE 1 OF 3 • PROTECT THE GUILD HALL WHILE KAEL HOLDS THE BREACH";
                case "ENCOUNTER071_LANTERN_ROAD_AMBUSH":
                    return "BATTLE 2 OF 3 • BREAK THE AMBUSH AND REOPEN LANTERN ROAD";
                case "ENCOUNTER071_GATE_EATER":
                    return "BATTLE 3 OF 3 • FIGHT BESIDE ZORIN'S PATROL AND STOP THE GATE-EATER";
                default:
                    return "THE ENEMY IS READY • APPROACH AND USE ACT";
            }
        }

        private static string StoryBattleLaunchMessage071(GuildCityExpeditionView017D expedition)
        {
            switch (expedition?.CurrentEncounterId)
            {
                case "ENCOUNTER071_HALL_BREACH":
                    return "BATTLE 1 OF 3 • HALL BREACH — Kael holds the breach. Your Unions defend the Hall.";
                case "ENCOUNTER071_LANTERN_ROAD_AMBUSH":
                    return "BATTLE 2 OF 3 • LANTERN ROAD AMBUSH — Break their line and reopen the road.";
                case "ENCOUNTER071_GATE_EATER":
                    return "BATTLE 3 OF 3 • GATE-EATER — The patrol and Wayglass are behind you. Hold the road home.";
                default:
                    return "The enemy is ready. Entering the Union battle arena...";
            }
        }

        private void UpdateInteractionPrompt066()
        {
            if (_promptText == null) return;
            if (_routeChoiceActive)
            {
                if (_worldInput071 != null &&
                    _worldInput071.LastSource071 == WorldInputSource071.Touch)
                {
                    _promptText.text = _optionalEliteChoiceActive
                        ? "TAP A CHOICE • FIGHT OR BYPASS"
                        : "TAP A CHOICE • UPPER CAUSEWAY OR LOWER TUNNEL";
                }
                else if (_worldInput071 != null &&
                         _worldInput071.LastSource071 == WorldInputSource071.Controller)
                {
                    _promptText.text = _optionalEliteChoiceActive
                        ? "LB • FIGHT ELITE     OR     RB • BYPASS"
                        : "LB • UPPER CAUSEWAY     OR     RB • LOWER TUNNEL";
                }
                else
                {
                    _promptText.text = _optionalEliteChoiceActive
                        ? "PRESS 1 • FIGHT ELITE     OR     PRESS 2 • BYPASS AND CONTINUE"
                        : "PRESS 1 • UPPER CAUSEWAY     OR     PRESS 2 • LOWER SERVICE TUNNEL";
                }
                return;
            }
            if (_interactionLocked)
            {
                _promptText.text = "ROLLING 2D6...";
                return;
            }
            var nearest = NearestInteraction066();
            if (nearest != null)
            {
                var action = _worldInput071 != null ? _worldInput071.InteractionPrompt071 : "E";
                _promptText.text = action + "  •  " + nearest.Title;
                return;
            }
            if (_questBeacon != null && _partyRoot != null)
            {
                var delta = _questBeacon.position - _partyRoot.position;
                delta.y = 0f;
                _promptText.text = ObjectiveDirectionHint071(delta) + "  •  " +
                                   Mathf.CeilToInt(delta.magnitude) + " m TO TARGET";
                return;
            }
            _promptText.text = "FOLLOW THE OBJECTIVE ARROW";
        }

        private GateworksInteraction066 NearestInteraction066()
        {
            if (_partyRoot == null) return null;
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            if (expedition != null && _questInteraction?.Anchor != null &&
                _questInteraction.Anchor.gameObject.activeInHierarchy &&
                HorizontalDistance066(_questInteraction.Anchor.position, _partyRoot.position) <= _questInteraction.Radius)
                return _questInteraction;
            GateworksInteraction066 nearest = null;
            var best = float.MaxValue;
            foreach (var interaction in _interactions)
            {
                if (interaction?.Anchor == null || !interaction.Anchor.gameObject.activeInHierarchy) continue;
                if (StringComparer.Ordinal.Equals(interaction.Id, "EXIT") &&
                    expedition != null && !expedition.CanFinalizeOperation)
                    continue;
                var distance = HorizontalDistance066(interaction.Anchor.position, _partyRoot.position);
                if (distance <= interaction.Radius && distance < best)
                {
                    best = distance;
                    nearest = interaction;
                }
            }
            return nearest;
        }

        private static float HorizontalDistance066(Vector3 left, Vector3 right)
        {
            var delta = left - right;
            delta.y = 0f;
            return delta.magnitude;
        }

        private void UpdateCamera066(float deltaTime)
        {
            if (_worldCamera == null || _partyRoot == null) return;
            _firstHourStage072?.TickCamera072(deltaTime);
        }

        private void UpdateFollowers066(float deltaTime)
        {
            if (_partyRoot == null) return;
            var side = Vector3.Cross(Vector3.up, _lastMoveDirection).normalized;
            for (var index = 0; index < _followers.Count; index++)
            {
                var follower = _followers[index];
                if (follower == null) continue;
                var row076 = index / 2;
                var offset = -_lastMoveDirection * (1.38f + row076 * 0.72f) +
                             side * (index % 2 == 0 ? -1.15f : 1.15f);
                follower.position = Vector3.Lerp(follower.position,
                    _partyRoot.position + offset, Mathf.Clamp01(deltaTime * 6.5f));
                follower.rotation = Quaternion.Slerp(follower.rotation, _partyRoot.rotation,
                    Mathf.Clamp01(deltaTime * 9f));
            }
        }

        private void SynchronizeLastMoveDirection070(Vector2 requestedInput)
        {
            var direction = new Vector3(requestedInput.x, 0f, requestedInput.y);
            if (direction.sqrMagnitude > 0.001f)
                _lastMoveDirection = direction.normalized;
        }

        private void UpdateWorldLabels066()
        {
            if (_worldCamera == null) return;
            foreach (var label in _worldLabels)
                if (label != null) label.transform.rotation = _worldCamera.transform.rotation;
            foreach (var billboard in _characterBillboards068)
            {
                if (billboard == null) continue;
                billboard.rotation = _worldCamera.transform.rotation;
                var renderer = billboard.GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.sortingOrder = 120 - Mathf.RoundToInt(billboard.position.z * 12f);
            }
        }

        private void ShowMessage066(string message)
        {
            if (_messageText != null) _messageText.text = message ?? string.Empty;
            if (_messagePanel071 != null)
                _messagePanel071.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
            _messageHideAt071 = Time.unscaledTime + 7.5f;
        }

        private void UpdateTransientHud071()
        {
            UpdatePersistentGuidance073();
            if (_messagePanel071 != null && _messagePanel071.gameObject.activeSelf &&
                !_routeChoiceActive && !_interactionLocked &&
                Time.unscaledTime >= _messageHideAt071)
                _messagePanel071.gameObject.SetActive(false);

            if (_controlsPanel071 != null)
            {
                var touchVisible071 = _worldInput071 != null &&
                                      _worldInput071.TouchControlsVisible071;
                _controlsPanel071.gameObject.SetActive(!touchVisible071);
            }
        }

        private static string ObjectiveDirectionHint071(Vector3 delta)
        {
            if (Mathf.Abs(delta.z) >= Mathf.Abs(delta.x))
                return delta.z >= 0f ? "GO NORTH  ↑" : "GO SOUTH  ↓";
            return delta.x >= 0f ? "GO EAST  →" : "GO WEST  ←";
        }

        private void UpdatePersistentGuidance073()
        {
            if (_chapterText073 != null)
                _chapterText073.text = "CHAPTER 1  •  " + RoomName073();
            if (_storyPartyText076 != null)
            {
                var companions076 = _namedCompanionNames076.Count == 0
                    ? "DEPLOYED PARTY SAVED"
                    : "WITH  •  " + string.Join("  •  ", _namedCompanionNames076);
                _storyPartyText076.text = companions076;
            }
            if (_storyContextText073 != null)
                _storyContextText073.text = "STORY SO FAR  •  " + StoryContext073();

            var m1 = _coordinator as IM1PresentationCoordinator;
            var progression = m1?.State;
            if (_xpText073 != null)
            {
                _xpText073.text = progression == null
                    ? "GUILD XP  —\nSPENDABLE XP  —"
                    : "GUILD LV " + Math.Max(1, progression.GuildLevel) + "  •  XP " +
                      progression.GuildXpIntoCurrentLevel.ToString("N0") + " / " +
                      progression.GuildXpRequiredForNextLevel.ToString("N0") +
                      "\nXP TO SPEND  " + progression.TreasuryXp.ToString("N0");
            }

            if (_questBeacon == null || _partyRoot == null) return;
            var delta = _questBeacon.position - _partyRoot.position;
            delta.y = 0f;
            var distance = delta.magnitude;
            var targetName = _questInteraction != null
                ? _questInteraction.Title
                : "NEXT OBJECTIVE";
            if (_routeText073 != null)
                _routeText073.text = "YOU  •  " + ObjectiveDirectionHint071(delta) + "  •  " +
                                     Mathf.CeilToInt(distance) + " m  •  " + targetName;
            if (_routeFill073 != null)
            {
                var routeLength = Mathf.Max(0.1f,
                    HorizontalDistance066(RoomSpawnPosition072(_room072, false),
                        RoomObjectivePosition072(_room072)));
                var completion = 1f - Mathf.Clamp01(distance / routeLength);
                var fill = _routeFill073.rectTransform;
                fill.anchorMin = Vector2.zero;
                fill.anchorMax = new Vector2(completion, 1f);
                fill.offsetMin = Vector2.zero;
                fill.offsetMax = Vector2.zero;
            }
            if (_questBeaconVisual != null)
            {
                var pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.12f;
                _questBeaconVisual.localScale = new Vector3(0.18f * pulse, 1.55f, 0.18f * pulse);
            }
        }

        private string RoomName073()
        {
            switch (_room072)
            {
                case ExpeditionRoom072.HallBreach: return "THE HALL BREACH";
                case ExpeditionRoom072.PatrolRescue: return "THE MISSING PATROL";
                case ExpeditionRoom072.Gatehouse: return "THE OLD GATEHOUSE";
                case ExpeditionRoom072.ReturnRoad: return "THE ROAD HOME";
                default: return "LANTERN ROAD";
            }
        }

        private string StoryContext073()
        {
            switch (_room072)
            {
                case ExpeditionRoom072.HallBreach:
                    return "A bell with no rope opened a breach beneath the Guild Hall. Kael is holding the stair while your new Unions answer their first call.";
                case ExpeditionRoom072.PatrolRescue:
                    return "ZORIN • PATROL CAPTAIN • 10 SURVIVORS. The raiders pinned the entire patrol beside the missing Wayglass.";
                case ExpeditionRoom072.Gatehouse:
                    return "The patrol is safe, but the Gate-Eater still blocks the road home. The recovered Wayglass is behind your line.";
                case ExpeditionRoom072.ReturnRoad:
                    return "The Wayglass and patrol are secure. Bring everyone back to Skyhome and learn what the relic opened.";
                default:
                    return "The Hall breach was only the first strike. Follow Lantern Road, find Zorin's missing patrol, and recover the Wayglass before the old Gatehouse falls.";
            }
        }

        private void RefreshStoryActorVisibility071(
            GuildCityExpeditionView017D expedition,
            bool hasPendingEncounter)
        {
            if (!IsCompactFirstRescueRoute071() || expedition == null) return;
            var nodeId071 = string.IsNullOrWhiteSpace(expedition.CurrentNodeId)
                ? "N00"
                : expedition.CurrentNodeId;
            var destination071 = ObjectiveDestinationNode066(expedition, hasPendingEncounter);
            var patrolRescued071 = FirstHourLanternPatrolRescued071(expedition);

            for (var index071 = 0; index071 < _interactions.Count; index071++)
            {
                var interaction071 = _interactions[index071];
                if (interaction071?.Anchor == null) continue;
                var visible071 = true;
                switch (interaction071.Id)
                {
                    case "REFUGEE":
                    case "MERCHANT":
                        visible071 = StringComparer.Ordinal.Equals(nodeId071, "N00") ||
                                     StringComparer.Ordinal.Equals(nodeId071, "N01");
                        break;
                    case "HAZARD":
                        visible071 = StringComparer.Ordinal.Equals(nodeId071, "N04") ||
                                     StringComparer.Ordinal.Equals(destination071, "N04");
                        break;
                    case "ENCOUNTER_N01":
                        visible071 = StringComparer.Ordinal.Equals(nodeId071, "N01");
                        break;
                    case "ENCOUNTER_N06":
                        visible071 = StringComparer.Ordinal.Equals(nodeId071, "N06");
                        break;
                    case "OBJECTIVE":
                        visible071 = StringComparer.Ordinal.Equals(nodeId071, "N13") &&
                                     !patrolRescued071;
                        break;
                    case "ENCOUNTER_N13":
                        visible071 = StringComparer.Ordinal.Equals(nodeId071, "N13") &&
                                     patrolRescued071;
                        break;
                    case "EXIT":
                        visible071 = expedition.CanFinalizeOperation;
                        break;
                    case "QUEST":
                        visible071 = true;
                        break;
                }
                interaction071.Anchor.gameObject.SetActive(visible071);
            }
        }

        private GateworksInteraction066 AddInteraction066(
            string id, string title, Transform anchor, float radius, string description, Action action)
        {
            var interaction = new GateworksInteraction066
            {
                Id = id,
                Title = title,
                Anchor = anchor,
                Radius = radius,
                Description = description,
                Action = action
            };
            _interactions.Add(interaction);
            return interaction;
        }

        private GameObject CreateNpc066(
            string name,
            Vector3 position,
            Color color,
            string artResource068 =
                "SecondDimension/Art/Battle/STANDEE_SIGREC_VEYRA_ASHGLASS",
            Sprite resolvedSprite163 = null)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_roomActors072 != null ? _roomActors072 : _worldRoot.transform, false);
            root.transform.position = position;
            root.layer = WorldLayer066;
            CreateAvatarBody066(root.transform, name + " Body", color, false,
                artResource068,
                2.05f, resolvedSprite163);
            CreateContactShadow076(
                root.transform,
                name + " Contact Shadow 076",
                0.72f,
                0.35f);
            return root;
        }

        private void CreateAvatarBody066(
            Transform parent,
            string name,
            Color color,
            bool controlled,
            string artResource068 = null,
            float artWorldHeight071 = 0f,
            Sprite resolvedSprite154 = null)
        {
            var resolvedHeight071 = artWorldHeight071 > 0f
                ? artWorldHeight071
                : controlled ? 2.20f : 1.85f;
            if (!AddWorldCharacterArt068(parent, name, artResource068, resolvedHeight071, resolvedSprite154))
                Debug.LogWarning("Missing authored world character art: " + (artResource068 ?? name));
        }

        private bool AddWorldCharacterArt068(
            Transform parent,
            string name,
            string resourceKey,
            float worldHeight,
            Sprite resolvedSprite154 = null)
        {
            if (parent == null) return false;
            // Resolver keys may name a cached atlas cell (#IDLE) or generated sprite,
            // not a Resources texture. Keep the exact already-resolved hero Sprite.
            // Shared resolver sprites must not enter this world's destruction list.
            var sprite = resolvedSprite154;
            if (sprite == null)
            {
                if (string.IsNullOrWhiteSpace(resourceKey)) return false;
                var texture = Resources.Load<Texture2D>(resourceKey);
                if (texture == null) return false;
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.02f),
                    100f,
                    0u,
                    SpriteMeshType.FullRect);
                sprite.name = texture.name + "_GATEWORKS_068";
                _runtimeCharacterSprites068.Add(sprite);
            }

            var art = new GameObject("Gateworks Character Art " + name + " 068");
            art.transform.SetParent(parent, false);
            art.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            art.layer = WorldLayer066;
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 12;
            var spriteHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
            art.transform.localScale = Vector3.one * (worldHeight / spriteHeight);
            _characterBillboards068.Add(art.transform);
            return true;
        }

        private void CreateContactShadow076(
            Transform parent,
            string name,
            float width,
            float depth)
        {
            if (parent == null) return;
            var sprite076 = ContactShadowSprite076();
            if (sprite076 == null) return;
            var shadow076 = new GameObject(name);
            shadow076.transform.SetParent(parent, false);
            shadow076.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            shadow076.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow076.transform.localScale = new Vector3(
                width / Mathf.Max(0.01f, sprite076.bounds.size.x),
                depth / Mathf.Max(0.01f, sprite076.bounds.size.y),
                1f);
            shadow076.layer = WorldLayer066;
            var renderer076 = shadow076.AddComponent<SpriteRenderer>();
            renderer076.sprite = sprite076;
            renderer076.sortingOrder = 4;
        }

        private Sprite ContactShadowSprite076()
        {
            if (_contactShadowSprite076 != null) return _contactShadowSprite076;
            const int width076 = 64;
            const int height076 = 32;
            var texture076 = new Texture2D(
                width076,
                height076,
                TextureFormat.RGBA32,
                false)
            {
                name = "Soft Character Contact Shadow 076",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels076 = new Color32[width076 * height076];
            for (var y076 = 0; y076 < height076; y076++)
            for (var x076 = 0; x076 < width076; x076++)
            {
                var normalizedX076 = (x076 + 0.5f - width076 * 0.5f) /
                                     (width076 * 0.5f);
                var normalizedY076 = (y076 + 0.5f - height076 * 0.5f) /
                                     (height076 * 0.5f);
                var falloff076 = Mathf.Clamp01(
                    1f - Mathf.Sqrt(
                        normalizedX076 * normalizedX076 +
                        normalizedY076 * normalizedY076));
                var alpha076 = (byte)Mathf.RoundToInt(
                    112f * falloff076 * falloff076);
                pixels076[y076 * width076 + x076] =
                    new Color32(2, 8, 12, alpha076);
            }
            texture076.SetPixels32(pixels076);
            texture076.Apply(false, false);
            _runtimeTextures076.Add(texture076);
            _contactShadowSprite076 = Sprite.Create(
                texture076,
                new Rect(0f, 0f, width076, height076),
                new Vector2(0.5f, 0.5f),
                64f,
                0u,
                SpriteMeshType.FullRect);
            _contactShadowSprite076.name = "Soft Character Contact Shadow Sprite 076";
            _runtimeCharacterSprites068.Add(_contactShadowSprite076);
            return _contactShadowSprite076;
        }

        private Transform CreateWorldIdentityBadge076(
            Transform parent,
            string name,
            string label,
            Vector3 localPosition,
            Color accent,
            float worldWidth)
        {
            if (parent == null) return null;
            var badge076 = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));
            badge076.transform.SetParent(parent, false);
            badge076.transform.localPosition = localPosition;
            badge076.layer = WorldLayer066;
            var canvas076 = badge076.GetComponent<Canvas>();
            canvas076.renderMode = RenderMode.WorldSpace;
            canvas076.overrideSorting = true;
            canvas076.sortingOrder = 230;
            var rect076 = badge076.GetComponent<RectTransform>();
            rect076.sizeDelta = new Vector2(360f, 64f);
            var scale076 = worldWidth / 360f;
            rect076.localScale = new Vector3(scale076, scale076, scale076);

            var panel076 = RuntimeUi.AddPanel(
                badge076.transform,
                name + " Plate",
                new Color(0.01f, 0.025f, 0.04f, 0.94f));
            Stretch066(panel076.rectTransform, Vector2.zero, Vector2.zero);
            panel076.raycastTarget = false;
            var outline076 = panel076.gameObject.AddComponent<Outline>();
            outline076.effectColor = new Color(accent.r, accent.g, accent.b, 0.92f);
            outline076.effectDistance = new Vector2(3f, -3f);
            outline076.useGraphicAlpha = true;

            var text076 = RuntimeUi.AddText(
                panel076.transform,
                name + " Label",
                label,
                30,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold);
            Stretch066(text076.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -4f));
            text076.raycastTarget = false;
            text076.resizeTextForBestFit = true;
            text076.resizeTextMinSize = 22;
            text076.resizeTextMaxSize = 30;
            _characterBillboards068.Add(badge076.transform);
            return badge076.transform;
        }

        private GameObject CreateMerchant066(Vector3 position)
        {
            var root = new GameObject("Gateworks Supply Merchant 066");
            root.transform.SetParent(_worldRoot.transform, false);
            root.transform.position = position;
            root.layer = WorldLayer066;
            CreateAvatarBody066(root.transform, "Quartermaster Pell 066", new Color(0.76f, 0.58f, 0.24f), false,
                "SecondDimension/Art/Battle/STANDEE_PROC_F85A4CAA747BC8C6", 1.85f);
            var counter = CreatePrimitive066(PrimitiveType.Cube, "Merchant Supply Counter 066",
                Vector3.zero, new Vector3(2.6f, 0.9f, 0.7f), new Color(0.28f, 0.18f, 0.10f), true);
            counter.transform.SetParent(root.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.45f, 1.05f);
            AddWorldLabel066(root.transform, "SUPPLY MERCHANT • PELL", new Vector3(0f, 2.35f, 0f), RuntimeUi.Warning);
            return root;
        }

        private GameObject CreateCamp066(Vector3 position)
        {
            var root = new GameObject("Gateworks Campfire 066");
            root.transform.SetParent(_worldRoot.transform, false);
            root.transform.position = position;
            root.layer = WorldLayer066;
            for (var index = 0; index < 3; index++)
            {
                var log = CreatePrimitive066(PrimitiveType.Cylinder, "Camp Log " + index + " 066",
                    Vector3.zero, new Vector3(0.20f, 0.75f, 0.20f), new Color(0.25f, 0.12f, 0.05f), false);
                log.transform.SetParent(root.transform, false);
                log.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                log.transform.localRotation = Quaternion.Euler(90f, index * 60f, 0f);
            }
            var flame = CreatePrimitive066(PrimitiveType.Sphere, "Camp Flame 066", Vector3.zero,
                new Vector3(0.42f, 0.75f, 0.42f), new Color(1f, 0.38f, 0.06f), false);
            flame.transform.SetParent(root.transform, false);
            flame.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            AddWorldLabel066(root.transform, "WATCH CAMP", new Vector3(0f, 1.8f, 0f), RuntimeUi.Warning);
            return root;
        }

        private GameObject CreateHazard066(Vector3 position)
        {
            var root = new GameObject("Gateworks Hazard 066");
            root.transform.SetParent(_worldRoot.transform, false);
            root.transform.position = position;
            root.layer = WorldLayer066;
            for (var index = 0; index < 4; index++)
            {
                var beam = CreatePrimitive066(PrimitiveType.Cube, "Hazard Broken Beam " + index + " 066",
                    Vector3.zero, new Vector3(0.45f, 0.32f, 2.3f), new Color(0.38f, 0.20f, 0.10f), true);
                beam.transform.SetParent(root.transform, false);
                beam.transform.localPosition = new Vector3((index - 1.5f) * 0.65f, 0.25f, (index % 2) * 0.45f);
                beam.transform.localRotation = Quaternion.Euler(index * 5f, index * 27f, index % 2 == 0 ? 9f : -8f);
            }
            AddWorldLabel066(root.transform, "COLLAPSED ROUTE • SKILL CHECK", new Vector3(0f, 2.0f, 0f), RuntimeUi.Warning);
            return root;
        }

        private GameObject CreateSecret066(Vector3 position)
        {
            var root = new GameObject("Gateworks Secret Cache 066");
            root.transform.SetParent(_worldRoot.transform, false);
            root.transform.position = position;
            root.layer = WorldLayer066;
            var chest = CreatePrimitive066(PrimitiveType.Cube, "Hidden Recovery Chest 066", Vector3.zero,
                new Vector3(1.3f, 0.75f, 0.85f), new Color(0.08f, 0.34f, 0.44f), true);
            chest.transform.SetParent(root.transform, false);
            chest.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            AddWorldLabel066(root.transform, "?", new Vector3(0f, 1.45f, 0f), RuntimeUi.Accent);
            return root;
        }

        private GameObject CreateRefugeeGroup066(Vector3 position)
        {
            var root = new GameObject("Gateworks Contract Objective Refugees 066");
            root.transform.SetParent(_roomActors072 != null ? _roomActors072 : _worldRoot.transform, false);
            root.transform.position = position + new Vector3(-4.05f, 0f, 1.25f);
            root.layer = WorldLayer066;
            _patrolCastRoot076 = root.transform;
            for (var index = 0; index < RescuedLanternPatrolRecruitIds071.Length; index++)
            {
                var recruitId071 = RescuedLanternPatrolRecruitIds071[index];
                var patrolMember071 = new GameObject(
                    "Rescued Lantern Patrol " + recruitId071 + " 071").transform;
                patrolMember071.SetParent(root.transform, false);
                patrolMember071.localPosition = LanternPatrolFormationOffsets076[index];

                var fallbackColor071 = M1VisualAssets.FallbackPortraitColor(
                    string.Empty, recruitId071, recruitId071);
                M1VisualAssets.TryResolveBattleStandee(
                    recruitId071,
                    recruitId071,
                    string.Empty,
                    recruitId071,
                    out var patrolSprite154,
                    out var standeeResourceKey071);
                if (string.IsNullOrWhiteSpace(standeeResourceKey071))
                    standeeResourceKey071 = M1VisualAssets.BattleRoot + "/STANDEE_" + recruitId071;

                CreateAvatarBody066(
                    patrolMember071,
                    "Lantern Patrol Body " + recruitId071 + " 071",
                    fallbackColor071,
                    false,
                    standeeResourceKey071,
                    1.24f,
                    patrolSprite154);
                CreateContactShadow076(
                    patrolMember071,
                    "Lantern Patrol Contact Shadow " + recruitId071 + " 076",
                    0.46f,
                    0.23f);
                if (index == 0)
                    _zorinIdentityBadge076 = CreateWorldIdentityBadge076(
                        patrolMember071,
                        "Zorin Patrol Captain Identity Badge 076",
                        "ZORIN  •  CAPTAIN",
                        new Vector3(0f, 1.50f, 0f),
                        RuntimeUi.Warning,
                        1.72f);
            }
            _patrolCohortBadge076 = CreateWorldIdentityBadge076(
                root.transform,
                "Lantern Patrol Cohort Badge 076",
                "ALL 10 SAFE  •  WAYGLASS",
                new Vector3(0f, 2.18f, 0.60f),
                RuntimeUi.Positive,
                3.48f);
            return root;
        }

        private GameObject CreateEnemyGroup066(
            string name,
            Vector3 position,
            string label,
            Color color)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_worldRoot.transform, false);
            root.transform.position = position;
            root.layer = WorldLayer066;
            for (var index = 0; index < 3; index++)
            {
                var enemy = new GameObject("Gate Gnawer " + (index + 1) + " 066").transform;
                enemy.SetParent(root.transform, false);
                enemy.localPosition = new Vector3((index - 1) * 1.85f, 0f, index == 1 ? 0.45f : 0f);
                CreateAvatarBody066(enemy, "Gate Gnawer Body " + index + " 066",
                    color + new Color(0f, index * 0.03f, 0f, 0f), false,
                    index == 0
                        ? "SecondDimension/Art/Battle/ENEMY_GATE_GNAWER_SCOUT"
                        : index == 1
                            ? "SecondDimension/Art/Battle/ENEMY_GATE_GNAWER_A"
                            : "SecondDimension/Art/Battle/ENEMY_GATE_GNAWER_BULWARK",
                    1.95f);
            }
            AddWorldLabel066(root.transform, label, new Vector3(0f, 2.9f, 0f), RuntimeUi.Error);
            return root;
        }

        private void CreateGateArch066(Vector3 position, string name, Color color, bool includeCrown)
        {
            CreatePrimitive066(PrimitiveType.Cube, name + " Left", position + new Vector3(-2.6f, 2f, 0f),
                new Vector3(1.1f, 4.2f, 1.0f), color, true);
            CreatePrimitive066(PrimitiveType.Cube, name + " Right", position + new Vector3(2.6f, 2f, 0f),
                new Vector3(1.1f, 4.2f, 1.0f), color, true);
            if (includeCrown)
                CreatePrimitive066(PrimitiveType.Cube, name + " Crown", position + new Vector3(0f, 4.3f, 0f),
                    new Vector3(6.3f, 1.0f, 1.1f), color, true);
        }

        private void CreatePaintedGateworksVista068()
        {
            _paintedVista068 = _firstHourStage072 != null
                ? _firstHourStage072.Backdrop072
                : null;
        }

        private void RefreshPaintedExpeditionVista071()
        {
            CreatePaintedGateworksVista068();
        }

        private Texture2D ResolvePaintedExpeditionTexture071()
        {
            const string guildHallResourceKey071 =
                "SecondDimension/Art/FirstHour071/Environments/GUILD_HALL_GAMEPLAY_PLATE_071";
            const string guildHallFallbackResourceKey071 =
                "SecondDimension/Art/Backgrounds/BG_GUILD_HALL_STAGE_01";
            const string lanternRoadResourceKey071 =
                "SecondDimension/Art/FirstHour071/Environments/LANTERN_ROAD_GAMEPLAY_PLATE_071";
            const string gateworksFallbackResourceKey068 =
                "SecondDimension/Art/Battle/BG_TUTORIAL_GATEWORKS_ARENA";

            if (UsesGuildHallBreachVista071())
            {
                return Resources.Load<Texture2D>(guildHallResourceKey071) ??
                       Resources.Load<Texture2D>(guildHallFallbackResourceKey071) ??
                       Resources.Load<Texture2D>(gateworksFallbackResourceKey068);
            }
            return Resources.Load<Texture2D>(lanternRoadResourceKey071) ??
                   Resources.Load<Texture2D>(gateworksFallbackResourceKey068);
        }

        private bool UsesGuildHallBreachVista071()
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            if (expedition == null ||
                !StringComparer.Ordinal.Equals(expedition.BoardId, FirstHourThreeBattleBoard071))
                return false;

            var objectiveFlags = expedition.ObjectiveFlags ?? Array.Empty<string>();
            var hallBreachCleared = objectiveFlags.Any(flag => StringComparer.Ordinal.Equals(
                flag, GuildCityExpeditionService017D.EncounterClearedFlag("N01")));
            if (hallBreachCleared) return false;

            return StringComparer.Ordinal.Equals(expedition.CurrentEncounterId,
                       "ENCOUNTER071_HALL_BREACH") ||
                   string.IsNullOrWhiteSpace(expedition.CurrentNodeId) ||
                   StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N00") ||
                   StringComparer.Ordinal.Equals(expedition.CurrentNodeId, "N01");
        }

        private void FitPaintedExpeditionVista071(Transform vista, Texture2D texture)
        {
            // FirstHourWorldStage072 sizes and anchors the plate in world space.
        }

        private GameObject CreatePrimitive066(
            PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color, bool collider)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(_worldRoot.transform, false);
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            SetLayer066(primitive.transform);
            var primitiveCollider = primitive.GetComponent<Collider>();
            if (primitiveCollider != null) primitiveCollider.enabled = collider;
            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = Material066(color);
                // The first-hour plates already paint the floor, walls, road,
                // causeway, water and arches.  Keep their colliders for real
                // movement, but do not cover the authored scene with the old
                // blockout meshes seen in the owner-review capture.
                renderer.enabled = !ShouldHideBlockoutEnvironment071(name);
            }
            return primitive;
        }

        private bool ShouldHideBlockoutEnvironment071(string objectName)
        {
            if (!IsCompactFirstRescueRoute071() || string.IsNullOrWhiteSpace(objectName))
                return false;

            return objectName.StartsWith("Gateworks Walkable Floor", StringComparison.Ordinal) ||
                   objectName.StartsWith("Gateworks Left Boundary", StringComparison.Ordinal) ||
                   objectName.StartsWith("Gateworks Right Boundary", StringComparison.Ordinal) ||
                   objectName.StartsWith("Gateworks South Boundary", StringComparison.Ordinal) ||
                   objectName.StartsWith("Gateworks North Wall", StringComparison.Ordinal) ||
                   objectName.StartsWith("Gateworks Ruin", StringComparison.Ordinal) ||
                   objectName.StartsWith("Gateworks Authored Road", StringComparison.Ordinal) ||
                   objectName.StartsWith("Collapsed Causeway", StringComparison.Ordinal) ||
                   objectName.StartsWith("Flood Channel", StringComparison.Ordinal) ||
                   objectName.StartsWith("Skyhome Return Gate", StringComparison.Ordinal) ||
                   objectName.StartsWith("Outer Gate Encounter Arch", StringComparison.Ordinal) ||
                   objectName.StartsWith("Merchant Supply Counter", StringComparison.Ordinal) ||
                   objectName.StartsWith("Hazard Broken Beam", StringComparison.Ordinal) ||
                   objectName.StartsWith("Hidden Recovery Chest", StringComparison.Ordinal);
        }

        private Material Material066(Color color)
        {
            var color32 = (Color32)color;
            var key = color32.r + ":" + color32.g + ":" + color32.b + ":" + color32.a;
            if (_materials.TryGetValue(key, out var existing)) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ??
                         Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            if (shader == null)
                throw new InvalidOperationException("No supported runtime shader is available for the Outer Gateworks.");
            var material = new Material(shader) { color = color };
            _materials[key] = material;
            return material;
        }

        private void AddWorldLabel066(Transform parent, string value, Vector3 localPosition, Color color)
        {
            // Names now live in the proximity HUD. Floating TextMesh labels made
            // the authored rooms read like a debug map and obscured the cast.
        }

        private static void SetLayer066(Transform root)
        {
            root.gameObject.layer = WorldLayer066;
            for (var index = 0; index < root.childCount; index++) SetLayer066(root.GetChild(index));
        }

        private static string Humanize066(string value) =>
            string.IsNullOrWhiteSpace(value) ? "RESULT" : value.Replace('_', ' ').ToUpperInvariant();

        private static void Anchor066(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch066(RectTransform rect, Vector2 minimumOffset, Vector2 maximumOffset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minimumOffset;
            rect.offsetMax = maximumOffset;
        }

        public void Shutdown066()
        {
            if (_shutdown) return;
            _shutdown = true;
            StopAllCoroutines();
            if (_worldRoot != null)
            {
                _worldRoot.SetActive(false);
                Destroy(_worldRoot);
                _worldRoot = null;
            }
            if (_hudCanvas != null)
            {
                _hudCanvas.gameObject.SetActive(false);
                Destroy(_hudCanvas.gameObject);
                _hudCanvas = null;
            }
            foreach (var material in _materials.Values)
                if (material != null) Destroy(material);
            _materials.Clear();
            foreach (var sprite in _runtimeCharacterSprites068)
                if (sprite != null) Destroy(sprite);
            _runtimeCharacterSprites068.Clear();
            foreach (var texture076 in _runtimeTextures076)
                if (texture076 != null) Destroy(texture076);
            _runtimeTextures076.Clear();
            _contactShadowSprite076 = null;
            _characterBillboards068.Clear();
            _controller = null;
            _partyRoot = null;
            _partyVisual = null;
            _worldMotor070 = null;
            _worldAnimator070 = null;
            _worldInput071 = null;
            _expeditionSpace070 = null;
            _firstHourStage072 = null;
            _expeditionAudio071 = null;
            _roomActors072 = null;
            _patrolCastRoot076 = null;
            _controlledIdentityBadge076 = null;
            _zorinIdentityBadge076 = null;
            _patrolCohortBadge076 = null;
            _roomFadePanel072 = null;
            _roomFadeCoroutine072 = null;
        }

        private void OnDestroy()
        {
            Shutdown066();
        }
    }
}
