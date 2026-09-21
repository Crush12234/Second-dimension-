using System;
using System.Collections.Generic;
using System.Linq;
namespace SecondDimension.SSS.V3
{
    public enum SssSupplementKind { HeroBoundAscensionCredit, SignatureWeapon }
    [Serializable] public sealed class SssSupplementCode
    {
        public string code, heroId, itemId;
        public SssSupplementKind kind;
        public int quantity=1;
    }
    public sealed class SssSupplementGrant
    {
        public SssSave next;
        public SssSupplementCode definition;
        public string receipt, reason;
        public bool grant;
        public bool autoEquip=false, autoApply=false;
    }
    public static class SssSupplementalCodes
    {
        // Creator codes for a LOCAL/offline save. These are not cryptographic entitlement
        // security. The host owns item creation, ascension, inventory and atomic receipts.
        static readonly string[] Stems = new[] {"SDGOW-SSS-BOND","SDGOW-SSS-PACT","SDGOW-SSS-FORM","SDGOW-SSS-MERCY","SDGOW-SSS-NOVA","SDGOW-SSS-DAWN","SDGOW-SSS-AEGIS","SDGOW-SSS-STORM","SDGOW-SSS-ECLIPSE","SDGOW-SSS-SONG"};
        public static List<SssSupplementCode> Definitions()
        {
            var list=new List<SssSupplementCode>();
            for(int h=0;h<SssHeroes.All.Length;h++)
            {
                string hero=SssHeroes.All[h];
                for(int i=1;i<=10;i++)list.Add(new SssSupplementCode {
                    code=Stems[h]+"-ASCEND-"+i.ToString("00",System.Globalization.CultureInfo.InvariantCulture),
                    heroId=hero, itemId="SSS_ASCENSION_CREDIT_"+hero.Substring(4),
                    kind=SssSupplementKind.HeroBoundAscensionCredit});
                list.Add(new SssSupplementCode {code=Stems[h]+"-WEAPON",heroId=hero,
                    itemId="SSS_WEAPON_"+hero.Substring(4),kind=SssSupplementKind.SignatureWeapon});
            }
            return list;
        }
        public static SssSupplementGrant Plan(SssSave current,string rawCode,
            IEnumerable<string> ownedHeroIds,IEnumerable<string> ownedUniqueItemIds)
        {
            SssProgression.Validate(current);
            if(ownedHeroIds==null || ownedUniqueItemIds==null)throw new ArgumentNullException();
            string normalized=String.IsNullOrWhiteSpace(rawCode)?null:rawCode.Trim().ToUpperInvariant();
            var def=Definitions().SingleOrDefault(x=>x.code==normalized);
            var result=new SssSupplementGrant {next=current.Copy(),definition=def};
            if(def==null){result.reason="Unknown supplemental code; delegate to existing code registry.";return result;}
            string receipt=ExactNumbers.Key("SssCode",def.code);result.receipt=receipt;
            if(current.codeReceipts.Contains(receipt)){result.reason="Already redeemed in this save.";return result;}
            if(!ownedHeroIds.Select(SssHeroes.CanonicalId).Contains(def.heroId,StringComparer.Ordinal))
            {result.reason="Recruit this hero first; code is not consumed.";return result;}
            string uniqueReceipt=def.kind==SssSupplementKind.SignatureWeapon?SssSignatureWeapons.EntitlementReceipt(def.heroId):null;
            bool alreadyClaimed=uniqueReceipt!=null && current.codeReceipts.Contains(uniqueReceipt);
            result.grant=def.kind!=SssSupplementKind.SignatureWeapon || (!alreadyClaimed && !ownedUniqueItemIds.Contains(def.itemId,StringComparer.Ordinal));
            if(uniqueReceipt!=null && !alreadyClaimed)result.next.codeReceipts.Add(uniqueReceipt);
            result.next.codeReceipts.Add(receipt);
            result.next.revision=ExactNumbers.Write(ExactNumbers.Read(current.revision)+1);
            result.reason=result.grant?"Grant item via the existing inventory transaction; never auto-equip or auto-ascend.":"Signature weapon already owned; no duplicate item.";
            return result;
        }
    }
}
