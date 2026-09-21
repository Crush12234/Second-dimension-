// SSS_TEN_V3: supersedes the unfinished SS trio; retain legacy IDs through migration.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SecondDimension.SSS.V3
{
    public static class CovenantLeadership
    {
        // formationOrder is the authoritative original member order, not render order.
        // Preserve a valid existing SSS leader; otherwise promote the first SSS in order.
        // Never silently split the Union or erase another SS hero's gold command.
        public static string Resolve(IList<string> formationOrder,string existingLeader)
        {
            if (formationOrder==null || formationOrder.Count==0 || formationOrder.Any(String.IsNullOrWhiteSpace))
                throw new ArgumentException("Invalid Union membership.");
            if (formationOrder.Select(SssHeroes.CanonicalId).Distinct(StringComparer.Ordinal).Count()!=formationOrder.Count)
                throw new ArgumentException("Duplicate Union member ID.");
            if (formationOrder.Contains(existingLeader) && SssHeroes.IsSss(existingLeader)) return existingLeader;
            foreach (string member in formationOrder) if (SssHeroes.IsSss(member)) return member;
            return formationOrder.Contains(existingLeader)?existingLeader:formationOrder[0];
        }
    }

    [Serializable] public sealed class GoldCommandContext
    {
        public string encounterInstanceId, sourceUnionId, casterHeroId;
        public List<string> sourceMemberIds=new List<string>();
        public List<string> preparedFamilyIds=new List<string>();
        public bool legalForecastPhase, casterAliveAndCanAct, sourceUnionAlreadyTransformed;
        public bool liveCostAffordable, guestSpaceAvailable;
        public bool alreadyUsedThisBattle, alreadyOwnsActiveGuest;
    }
    [Serializable] public sealed class GoldCommandPlan
    {
        public string commandId, label, casterHeroId, sourceUnionId, encounterInstanceId;
        public string displayStyle="gold";
        public bool consumesWholeUnionCommand=true;
        public bool replacesOwnUnion;
        public int spawnedUnionCount, manifestedMemberCount;
        public List<string> baseFamilyIds=new List<string>();
        public List<string> masteryDefeats=new List<string>();
    }
    public static class CovenantUnionCommands
    {
        public static bool TryPlan(GoldCommandContext c,CovenantProgressSave progress,
            out GoldCommandPlan plan,out string disabledReason)
        {
            plan=null; disabledReason=null;
            if (c==null || progress==null) throw new ArgumentNullException();
            CovenantProgression.Validate(progress);
            CovenantKind kind;
            if (!CovenantHeroes.TryKind(c.casterHeroId,out kind)) { disabledReason="Unknown specialist.";return false; }
            if (c.sourceMemberIds==null || c.sourceMemberIds.Count(x=>x==c.casterHeroId)!=1)
            { disabledReason="Hero must belong to this Union.";return false; }
            if (!c.legalForecastPhase) { disabledReason="Available at the next legal Union command phase.";return false; }
            if (!c.casterAliveAndCanAct) { disabledReason="Hero cannot act.";return false; }
            if (c.sourceUnionAlreadyTransformed) { disabledReason="This Union is transformed for this battle.";return false; }
            if (c.alreadyUsedThisBattle) { disabledReason="Already used in this battle.";return false; }
            if (!c.liveCostAffordable) { disabledReason="Insufficient existing Union AP / hero MP.";return false; }
            if (kind!=CovenantKind.Shapeshifter && (c.alreadyOwnsActiveGuest || !c.guestSpaceAvailable))
            { disabledReason="Guest Union unavailable.";return false; }
            int required=kind==CovenantKind.Tamer?6:1;
            if (c.preparedFamilyIds==null || c.preparedFamilyIds.Count!=required)
            { disabledReason=kind==CovenantKind.Tamer?"Prepare six unlocked monsters; repeated families are allowed.":"Prepare one unlocked family.";return false; }
            foreach (string family in c.preparedFamilyIds)
                if (!CovenantMastery.View(CovenantMastery.Count(progress,c.casterHeroId,family)).unlocked)
                { disabledReason="An equipped family is not unlocked.";return false; }
            ExactNumbers.RequireId(c.encounterInstanceId);ExactNumbers.RequireId(c.sourceUnionId);
            plan=new GoldCommandPlan { casterHeroId=c.casterHeroId,sourceUnionId=c.sourceUnionId,
                encounterInstanceId=c.encounterInstanceId,replacesOwnUnion=kind==CovenantKind.Shapeshifter,
                spawnedUnionCount=kind==CovenantKind.Shapeshifter?0:1,manifestedMemberCount=required,
                baseFamilyIds=new List<string>(c.preparedFamilyIds),
                masteryDefeats=c.preparedFamilyIds.Select(x=>CovenantMastery.Count(progress,c.casterHeroId,x)).ToList() };
            if (kind==CovenantKind.Tamer) { plan.commandId="COVENANT_CALL_TAMED_UNION";plan.label="Call the Tamed Union"; }
            else if (kind==CovenantKind.Summoner) { plan.commandId="COVENANT_MANIFEST_PACT";plan.label="Manifest the Pact"; }
            else { plan.commandId="COVENANT_UNION_METAMORPHOSIS";plan.label="Union Metamorphosis"; }
            return true;
        }
    }

    // Required live integration contract, not a fake implementation of the user's engine.
    public interface ICovenantBattleHost
    {
        // Build a fresh context from the LIVE authoritative battle at execution time.
        GoldCommandContext ReadCurrentContext(string sourceUnionId,string casterHeroId);
        CovenantProgressSave ReadProgress();
        // In ONE host transaction: revalidate, pay existing AP/MP, spawn the entire guest
        // or replace the source Union, mark once-per-battle receipt, persist state. On
        // failure roll everything back. False must not charge or consume the special.
        bool TryCommitGoldCommand(GoldCommandPlan plan,out string failureReason);
    }
    public static class CovenantCommandExecutor
    {
        public static bool Execute(ICovenantBattleHost host,string unionId,string heroId,out string reason)
        {
            if(host==null)throw new ArgumentNullException("host");
            GoldCommandPlan plan;
            if(!CovenantUnionCommands.TryPlan(host.ReadCurrentContext(unionId,heroId),host.ReadProgress(),out plan,out reason))return false;
            return host.TryCommitGoldCommand(plan,out reason);
        }
    }
}
