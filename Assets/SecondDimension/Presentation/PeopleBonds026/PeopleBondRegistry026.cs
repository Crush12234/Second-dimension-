using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SecondDimension.Gameplay.PeopleBonds026;
using UnityEngine;

namespace SecondDimension.Presentation.PeopleBonds026
{
    [Serializable] public sealed class PeopleBondsManifest026 { public string contentVersion; public string title; public int bondTiers; public int linkArts; public int unionDoctrines; public int signatureTechniqueTemplates; public string[] hardLaws=Array.Empty<string>(); }
    [Serializable] sealed class TierEnvelope026 { public BondTierDefinition026[] tiers=Array.Empty<BondTierDefinition026>(); }
    [Serializable] sealed class LinkEnvelope026 { public LinkArtDefinition026[] linkArts=Array.Empty<LinkArtDefinition026>(); }
    [Serializable] sealed class DoctrineEnvelope026 { public UnionBondDoctrineDefinition026[] doctrines=Array.Empty<UnionBondDoctrineDefinition026>(); }
    [Serializable] sealed class TechniqueEnvelope026 { public SignatureTechniqueTemplate026[] techniques=Array.Empty<SignatureTechniqueTemplate026>(); }

    public sealed class PeopleBondCatalog026
    {
        public PeopleBondsManifest026 Manifest{get;internal set;}
        public IReadOnlyDictionary<string,BondTierDefinition026> Tiers{get;internal set;}
        public IReadOnlyDictionary<string,LinkArtDefinition026> LinkArts{get;internal set;}
        public IReadOnlyDictionary<string,UnionBondDoctrineDefinition026> Doctrines{get;internal set;}
        public IReadOnlyDictionary<string,SignatureTechniqueTemplate026> Techniques{get;internal set;}
    }
    public static class PeopleBondRegistry026
    {
        const string Base="SecondDimension/PeopleBonds026/Data/"; static PeopleBondCatalog026 _catalog;
        public static PeopleBondCatalog026 Load()
        {
            if(_catalog!=null)return _catalog;
            var c=new PeopleBondCatalog026{
                Manifest=Load<PeopleBondsManifest026>("PeopleBondsManifest026"),
                Tiers=Map(Load<TierEnvelope026>("BondTiers026").tiers,x=>x.tierId),
                LinkArts=Map(Load<LinkEnvelope026>("LinkArts026").linkArts,x=>x.linkArtId),
                Doctrines=Map(Load<DoctrineEnvelope026>("UnionBondDoctrines026").doctrines,x=>x.doctrineId),
                Techniques=Map(Load<TechniqueEnvelope026>("SignatureTechniqueTemplates026").techniques,x=>x.id)};
            Validate(c);_catalog=c;return c;
        }
        static void Validate(PeopleBondCatalog026 c)
        {
            if(c.Manifest==null||!StringComparer.Ordinal.Equals(c.Manifest.contentVersion,"PEOPLE_BONDS_LINK_ARTS_026_1.0"))throw new InvalidOperationException("People Bonds manifest invalid.");
            Check(c.Tiers.Count,c.Manifest.bondTiers,"tiers");Check(c.LinkArts.Count,c.Manifest.linkArts,"Link Arts");Check(c.Doctrines.Count,c.Manifest.unionDoctrines,"doctrines");Check(c.Techniques.Count,c.Manifest.signatureTechniqueTemplates,"techniques");
            foreach(var x in c.LinkArts.Values)if(!x.forecastOnlyInStandard||x.predictedMemberActionsClickable||!x.preservesUnderlyingArtIds||x.maxPerUnionPerRound!=1)throw new InvalidOperationException("Link Art law failed: "+x.linkArtId);
            foreach(var x in c.Doctrines.Values)if(x.directMemberSelection)throw new InvalidOperationException("Doctrine direct-member selection: "+x.doctrineId);
            foreach(var x in c.Techniques.Values)if(!x.forecastOnlyInStandard||x.directIndividualSelection||!x.preservesUnderlyingArtLegality)throw new InvalidOperationException("Signature Technique law failed: "+x.id);
        }
        static void Check(int a,int e,string l){if(a!=e)throw new InvalidOperationException(l+" mismatch: "+a+" != "+e);}
        static T Load<T>(string n){var a=Resources.Load<TextAsset>(Base+n);if(a==null)throw new InvalidOperationException("Missing People Bonds resource: "+n);var v=JsonConvert.DeserializeObject<T>(a.text);if(v==null)throw new InvalidOperationException("Invalid People Bonds resource: "+n);return v;}
        static IReadOnlyDictionary<string,T> Map<T>(IEnumerable<T> values,Func<T,string> id){var d=new Dictionary<string,T>(StringComparer.Ordinal);foreach(var v in values??Array.Empty<T>()){var k=id(v);if(string.IsNullOrWhiteSpace(k)||d.ContainsKey(k))throw new InvalidOperationException("Invalid or duplicate People Bonds ID: "+k);d.Add(k,v);}return d;}
    }
}
