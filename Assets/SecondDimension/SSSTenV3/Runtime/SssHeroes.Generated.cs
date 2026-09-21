// Generated from SSSHero and code data. This is not a live-game registry replacement.
using System;
using System.Linq;
using System.Collections.Generic;
namespace SecondDimension.SSS.V3 {
 public enum SssScaling { FamilyDefeats, TotalDefeats, WorldProgress }
 public static class SssHeroes {
  public const string Rarity = "SSS";
  public static readonly string[] All = new string[] {
   "SSS_RYLEN_STONEBOND",
   "SSS_ELYSIA_NIGHTCALL",
   "SSS_VAELIS_MANYFORM",
   "SSS_NERIS_DAWNWELL",
   "SSS_MYRIEN_STARFALL",
   "SSS_ASTERION_SUNWARD",
   "SSS_SOLENNE_AEGIS",
   "SSS_CAEDRAN_TEMPEST",
   "SSS_ISOLDE_ECLIPSERIFT",
   "SSS_ORINTH_WORLDSONG"
  };
  public static string CanonicalId(string id) {
   if (id==null) return null;
   switch(id) {
    case "HERO_SS_RYLEN_STONEBOND":
    case "SS_RYLEN_STONEBOND": return "SSS_RYLEN_STONEBOND";
    case "HERO_SS_ELYSIA_NIGHTCALL":
    case "SS_ELYSIA_NIGHTCALL": return "SSS_ELYSIA_NIGHTCALL";
    case "HERO_SS_VAELIS_MANYFORM":
    case "SS_VAELIS_MANYFORM": return "SSS_VAELIS_MANYFORM";
    default: return id;
   }
  }
  public static bool IsSss(string id) { return All.Contains(CanonicalId(id),StringComparer.Ordinal); }
  public static SssScaling Scaling(string id) {
   int i=Array.IndexOf(All,CanonicalId(id));
   if(i<0)throw new ArgumentException("Unknown SSS hero.");
   return i<3?SssScaling.FamilyDefeats:(i<5?SssScaling.TotalDefeats:SssScaling.WorldProgress);
  }
  public static string RedemptionHero(string code) {
   if(String.IsNullOrWhiteSpace(code))return null;
   switch(code.Trim().ToUpperInvariant()) {
    case "SDGOW-SSS-BOND": return "SSS_RYLEN_STONEBOND";
    case "SDGOW-SSS-PACT": return "SSS_ELYSIA_NIGHTCALL";
    case "SDGOW-SSS-FORM": return "SSS_VAELIS_MANYFORM";
    case "SDGOW-SSS-MERCY": return "SSS_NERIS_DAWNWELL";
    case "SDGOW-SSS-NOVA": return "SSS_MYRIEN_STARFALL";
    case "SDGOW-SSS-DAWN": return "SSS_ASTERION_SUNWARD";
    case "SDGOW-SSS-AEGIS": return "SSS_SOLENNE_AEGIS";
    case "SDGOW-SSS-STORM": return "SSS_CAEDRAN_TEMPEST";
    case "SDGOW-SSS-ECLIPSE": return "SSS_ISOLDE_ECLIPSERIFT";
    case "SDGOW-SSS-SONG": return "SSS_ORINTH_WORLDSONG";
    default: return null;
   }
  }
  public static string Signature(string id) {
   switch(CanonicalId(id)) {
    case "SSS_RYLEN_STONEBOND": return "Call the Tamed Union";
    case "SSS_ELYSIA_NIGHTCALL": return "Manifest the Pact";
    case "SSS_VAELIS_MANYFORM": return "Union Metamorphosis";
    case "SSS_NERIS_DAWNWELL": return "Mercy Beyond Measure";
    case "SSS_MYRIEN_STARFALL": return "Astral Cataclysm";
    case "SSS_ASTERION_SUNWARD": return "Banner of Ten Thousand Dawns";
    case "SSS_SOLENNE_AEGIS": return "Citadel Without End";
    case "SSS_CAEDRAN_TEMPEST": return "Storm Across Worlds";
    case "SSS_ISOLDE_ECLIPSERIFT": return "Eclipse of Every Front";
    case "SSS_ORINTH_WORLDSONG": return "Concordance of Worlds";
    default: throw new ArgumentException("Unknown SSS hero.");
   }
  }
 }
}
