using System;
using Newtonsoft.Json;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;

namespace SecondDimension.Gameplay.State
{
    [Serializable]
    public sealed class CampaignState
    {
        public CampaignState(
            string campaignGuid,
            long campaignSeed,
            string contentAuthorityVersion,
            ModeRuleSnapshot rules,
            GuildState guild)
            : this(
                campaignGuid,
                campaignSeed,
                contentAuthorityVersion,
                rules,
                guild,
                profile: null,
                openingFlow: null,
                battle: null,
                sssV4090: null)
        {
        }

        public CampaignState(
            string campaignGuid,
            long campaignSeed,
            string contentAuthorityVersion,
            ModeRuleSnapshot rules,
            GuildState guild,
            NewGuildProfileState profile,
            OpeningFlowState openingFlow)
            : this(campaignGuid, campaignSeed, contentAuthorityVersion, rules, guild, profile, openingFlow, battle: null, sssV4090: null)
        {
        }

        [JsonConstructor]
        public CampaignState(
            string campaignGuid,
            long campaignSeed,
            string contentAuthorityVersion,
            ModeRuleSnapshot rules,
            GuildState guild,
            NewGuildProfileState profile,
            OpeningFlowState openingFlow,
            BattleState battle,
            SssTenV4State090 sssV4090 = null,
            EquipmentUndoState112 equipmentUndo112 = null,
            UnionBattlePlan132 nextBattleUnions132 = null,
            SecondDimension.Gameplay.Campaign020.CampaignRecovery150 recovery150 = null,
            SecondDimension.Gameplay.GuildCity017D.CatchUpState153 catchUp153 = null,
            SecondDimension.Gameplay.TitanTrials160.TitanTrialState160 titanTrials160 = null,
            SecondDimension.Gameplay.Navigation164.LoopCheckpointState164 loops164 = null)
        {
            CampaignGuid = string.IsNullOrWhiteSpace(campaignGuid)
                ? throw new ArgumentException("Campaign GUID is required.", nameof(campaignGuid))
                : campaignGuid;
            CampaignSeed = campaignSeed;
            ContentAuthorityVersion = string.IsNullOrWhiteSpace(contentAuthorityVersion)
                ? throw new ArgumentException("Content authority version is required.", nameof(contentAuthorityVersion))
                : contentAuthorityVersion;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Guild = guild ?? throw new ArgumentNullException(nameof(guild));
            Profile = profile;
            OpeningFlow = openingFlow;
            Battle = battle;
            // Optional for pre-V4 saves. Runtime callers use
            // SssTenV4CampaignAccessor090.Read to obtain the valid default.
            SssV4090 = sssV4090;
            EquipmentUndo112 = equipmentUndo112;
            NextBattleUnions132 = nextBattleUnions132;
            Recovery150 = recovery150;
            CatchUp153 = catchUp153;
            TitanTrials160 = titanTrials160;
            Loops164 = loops164;
            if(Loops164!=null&&Loops164.ProfileId!=CampaignGuid)throw new ArgumentException("Loop profile mismatch.");
            if(TitanTrials160!=null&&TitanTrials160.ProfileId!=CampaignGuid)throw new ArgumentException("Titan profile mismatch.");
            if(CatchUp153!=null&&CatchUp153.ProfileId!=CampaignGuid)throw new ArgumentException("Training profile mismatch.");
            NextBattleUnions132?.Validate(Guild);
            if ((profile == null) != (openingFlow == null))
            {
                throw new ArgumentException("M1 profile and opening flow must either both be present or both be absent.");
            }
        }

        public string CampaignGuid { get; }
        public long CampaignSeed { get; }
        public string ContentAuthorityVersion { get; }
        public ModeRuleSnapshot Rules { get; }
        public GuildState Guild { get; }
        public NewGuildProfileState Profile { get; }
        public OpeningFlowState OpeningFlow { get; }
        public BattleState Battle { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SssTenV4State090 SssV4090 { get; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public EquipmentUndoState112 EquipmentUndo112 { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public UnionBattlePlan132 NextBattleUnions132 { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SecondDimension.Gameplay.Campaign020.CampaignRecovery150 Recovery150 { get; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SecondDimension.Gameplay.GuildCity017D.CatchUpState153 CatchUp153 {get;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SecondDimension.Gameplay.TitanTrials160.TitanTrialState160 TitanTrials160 {get;}
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SecondDimension.Gameplay.Navigation164.LoopCheckpointState164 Loops164 {get;}
        public CampaignState WithLoops164(SecondDimension.Gameplay.Navigation164.LoopCheckpointState164 loops) =>
            new CampaignState(CampaignGuid,CampaignSeed,ContentAuthorityVersion,Rules,Guild,Profile,OpeningFlow,Battle,SssV4090,EquipmentUndo112,NextBattleUnions132,Recovery150,CatchUp153,TitanTrials160,loops);

        public CampaignState WithTitanTrials160(SecondDimension.Gameplay.TitanTrials160.TitanTrialState160 trials) =>
            new CampaignState(CampaignGuid,CampaignSeed,ContentAuthorityVersion,Rules,Guild,Profile,OpeningFlow,Battle,SssV4090,EquipmentUndo112,NextBattleUnions132,Recovery150,CatchUp153,trials,Loops164);

        public CampaignState WithCatchUp153(SecondDimension.Gameplay.GuildCity017D.CatchUpState153 training) =>
            new CampaignState(CampaignGuid,CampaignSeed,ContentAuthorityVersion,Rules,Guild,Profile,OpeningFlow,Battle,SssV4090,EquipmentUndo112,NextBattleUnions132,Recovery150,training,TitanTrials160,Loops164);

        public CampaignState WithRecovery150(SecondDimension.Gameplay.Campaign020.CampaignRecovery150 recovery) =>
            new CampaignState(CampaignGuid, CampaignSeed, ContentAuthorityVersion,
                Rules, Guild, Profile, OpeningFlow, Battle, SssV4090, EquipmentUndo112, NextBattleUnions132, recovery, CatchUp153, TitanTrials160, Loops164);

        public CampaignState WithNextBattleUnions132(UnionBattlePlan132 plan) =>
            new CampaignState(CampaignGuid, CampaignSeed, ContentAuthorityVersion,
                Rules, Guild, Profile, OpeningFlow, Battle, SssV4090, EquipmentUndo112, plan, Recovery150, CatchUp153, TitanTrials160, Loops164);

        public CampaignState WithEquipmentUndo112(EquipmentUndoState112 undo) =>
            new CampaignState(CampaignGuid, CampaignSeed, ContentAuthorityVersion,
                Rules, Guild, Profile, OpeningFlow, Battle, SssV4090, undo, NextBattleUnions132, Recovery150, CatchUp153, TitanTrials160, Loops164);

        public CampaignState With(GuildState guild, OpeningFlowState openingFlow) =>
            new CampaignState(
                CampaignGuid,
                CampaignSeed,
                ContentAuthorityVersion,
                Rules,
                guild,
                Profile,
                openingFlow,
                Battle,
                SssV4090,
                EquipmentUndo112,
                NextBattleUnions132, Recovery150, CatchUp153, TitanTrials160, Loops164);

        public CampaignState WithBattle(BattleState battle) =>
            new CampaignState(
                CampaignGuid,
                CampaignSeed,
                ContentAuthorityVersion,
                Rules,
                Guild,
                Profile,
                OpeningFlow,
                battle,
                SssV4090,
                EquipmentUndo112,
                NextBattleUnions132, Recovery150, CatchUp153, TitanTrials160, Loops164);

        public CampaignState WithSssV4090(SssTenV4State090 sssV4090) =>
            new CampaignState(
                CampaignGuid,
                CampaignSeed,
                ContentAuthorityVersion,
                Rules,
                Guild,
                Profile,
                OpeningFlow,
                Battle,
                sssV4090 ?? throw new ArgumentNullException(nameof(sssV4090)),
                EquipmentUndo112,
                NextBattleUnions132, Recovery150, CatchUp153, TitanTrials160, Loops164);
    }
}
