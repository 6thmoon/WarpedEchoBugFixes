using BepInEx;
using HarmonyLib;
using RoR2;
using System.Reflection;

[assembly: AssemblyVersion(RiskOfResources.WarpedEchoBugFixes.version)]
namespace RiskOfResources;

[BepInPlugin(identifier, nameof(WarpedEchoBugFixes), version)]
class WarpedEchoBugFixes : BaseUnityPlugin
{
	public const string version = "2.0.0", identifier = "com.riskofresources.fix.echo";
	protected void Awake() => Harmony.CreateAndPatchAll(typeof(WarpedEchoBugFixes));

	[HarmonyPatch(typeof(CharacterBody),
			nameof(CharacterBody.hasOneShotProtection), MethodType.Getter)]
	[HarmonyPostfix]
	static void CheckOneShotProtection(CharacterBody __instance, ref bool __result)
	{
		__result &= __instance.oneShotProtectionFraction > 0;
	}

	[HarmonyPatch(typeof(CharacterBody), nameof(CharacterBody.SecondHalfOfDelayedDamage))]
	[HarmonyPrefix]
	static void SetDamageType(CharacterBody __instance, DamageInfo __0)
	{
		__0.damageType |= DamageType.BypassBlock;
		__0.damageType |= DamageTypeExtended.BypassDamageCalculations;
	}
}
