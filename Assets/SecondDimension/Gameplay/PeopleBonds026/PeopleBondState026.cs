using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.PeopleBonds026
{
    [Serializable] public sealed class BondPairState026
    {
        [JsonConstructor] public BondPairState026(string pairId,string firstRecruitId,string secondRecruitId,int trust,int respect,int familiarity,
            int sharedMemoryCount,string tierId,IReadOnlyList<string> appliedMemoryReceiptIds)
        {PairId=Req(pairId,nameof(pairId));FirstRecruitId=Req(firstRecruitId,nameof(firstRecruitId));SecondRecruitId=Req(secondRecruitId,nameof(secondRecruitId));
         Trust=Clamp(trust);Respect=Clamp(respect);Familiarity=Clamp(familiarity);SharedMemoryCount=Math.Max(0,sharedMemoryCount);TierId=tierId??"BOND_TIER026_0";AppliedMemoryReceiptIds=Copy(appliedMemoryReceiptIds);}
        public string PairId{get;} public string FirstRecruitId{get;} public string SecondRecruitId{get;} public int Trust{get;} public int Respect{get;} public int Familiarity{get;}
        public int SharedMemoryCount{get;} public string TierId{get;} public IReadOnlyList<string> AppliedMemoryReceiptIds{get;}
        public BondPairState026 With(int? trust=null,int? respect=null,int? familiarity=null,int? sharedMemoryCount=null,string tierId=null,IReadOnlyList<string> appliedMemoryReceiptIds=null)=>
            new BondPairState026(PairId,FirstRecruitId,SecondRecruitId,trust??Trust,respect??Respect,familiarity??Familiarity,sharedMemoryCount??SharedMemoryCount,tierId??TierId,appliedMemoryReceiptIds??AppliedMemoryReceiptIds);
        public static string CanonicalPairId(string a,string b){a=Req(a,nameof(a));b=Req(b,nameof(b));return StringComparer.Ordinal.Compare(a,b)<=0?"BONDPAIR026_"+a+"__"+b:"BONDPAIR026_"+b+"__"+a;}
        static int Clamp(int value)=>Math.Max(-100,Math.Min(100,value)); static string Req(string v,string p)=>string.IsNullOrWhiteSpace(v)?throw new ArgumentException("Stable ID required.",p):v;
        static IReadOnlyList<string> Copy(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
    }
    [Serializable] public sealed class UnionBondRuntimeState026
    {
        [JsonConstructor] public UnionBondRuntimeState026(string unionId,string doctrineId,IReadOnlyList<string> triggeredRoundKeys,int linkArtTriggerCount)
        {UnionId=Req(unionId,nameof(unionId));DoctrineId=doctrineId??string.Empty;TriggeredRoundKeys=Copy(triggeredRoundKeys);LinkArtTriggerCount=Math.Max(0,linkArtTriggerCount);}
        public string UnionId{get;} public string DoctrineId{get;} public IReadOnlyList<string> TriggeredRoundKeys{get;} public int LinkArtTriggerCount{get;}
        public UnionBondRuntimeState026 With(string doctrineId=null,IReadOnlyList<string> triggeredRoundKeys=null,int? linkArtTriggerCount=null)=>new UnionBondRuntimeState026(UnionId,doctrineId??DoctrineId,triggeredRoundKeys??TriggeredRoundKeys,linkArtTriggerCount??LinkArtTriggerCount);
        static string Req(string v,string p)=>string.IsNullOrWhiteSpace(v)?throw new ArgumentException("Stable ID required.",p):v;
        static IReadOnlyList<string> Copy(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
    }
    [Serializable] public sealed class PeopleBondState026
    {
        public const string ContentVersion="PEOPLE_BONDS_LINK_ARTS_026_1.0";
        [JsonConstructor] public PeopleBondState026(string contentVersion,IReadOnlyList<BondPairState026> pairs,IReadOnlyList<UnionBondRuntimeState026> unions,
            IReadOnlyList<string> appliedReceiptIds,string lastCheckpointId)
        {ContentAuthorityVersion=string.IsNullOrWhiteSpace(contentVersion)?ContentVersion:contentVersion;Pairs=CopyPairs(pairs);Unions=CopyUnions(unions);AppliedReceiptIds=Copy(appliedReceiptIds);LastCheckpointId=lastCheckpointId??string.Empty;}
        public string ContentAuthorityVersion{get;} public IReadOnlyList<BondPairState026> Pairs{get;} public IReadOnlyList<UnionBondRuntimeState026> Unions{get;} public IReadOnlyList<string> AppliedReceiptIds{get;} public string LastCheckpointId{get;}
        public PeopleBondState026 With(IReadOnlyList<BondPairState026> pairs=null,IReadOnlyList<UnionBondRuntimeState026> unions=null,IReadOnlyList<string> appliedReceiptIds=null,string lastCheckpointId=null)=>new PeopleBondState026(ContentAuthorityVersion,pairs??Pairs,unions??Unions,appliedReceiptIds??AppliedReceiptIds,lastCheckpointId??LastCheckpointId);
        public static PeopleBondState026 Default()=>new PeopleBondState026(ContentVersion,Array.Empty<BondPairState026>(),Array.Empty<UnionBondRuntimeState026>(),Array.Empty<string>(),"people_bonds_026_initialized");
        static IReadOnlyList<string> Copy(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
        static IReadOnlyList<BondPairState026> CopyPairs(IReadOnlyList<BondPairState026> values){var r=new List<BondPairState026>();if(values!=null)for(var i=0;i<values.Count;i++)r.Add(values[i]??throw new ArgumentException("Pair cannot be null.",nameof(values)));r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.PairId,b.PairId));return r.AsReadOnly();}
        static IReadOnlyList<UnionBondRuntimeState026> CopyUnions(IReadOnlyList<UnionBondRuntimeState026> values){var r=new List<UnionBondRuntimeState026>();if(values!=null)for(var i=0;i<values.Count;i++)r.Add(values[i]??throw new ArgumentException("Union bond state cannot be null.",nameof(values)));r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.UnionId,b.UnionId));return r.AsReadOnly();}
    }
}
