using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;
using WolfoFixes;

[assembly: AssemblyVersion(RiskOfResources.WarpedEchoBugFixes.version)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace RiskOfResources;

[BepInPlugin(identifier, nameof(WarpedEchoBugFixes), version)]
[BepInDependency(Compatibility.identifier, BepInDependency.DependencyFlags.SoftDependency)]
class WarpedEchoBugFixes : BaseUnityPlugin
{
	public const string version = "1.0.0", identifier = "com.riskofresources.fix.echo";

	protected void Awake()
	{
		Compatibility.Check();
		Harmony.CreateAndPatchAll(typeof(WarpedEchoBugFixes));
	}

	[HarmonyPatch(typeof(HealthComponent), nameof(HealthComponent.TakeDamageProcess))]
	[HarmonyILManipulator]
	static void JustFuckingKillMeAlready(ILContext context)
	{
		ILCursor cursor = new(context);
		VariableDefinition flag = new(context.Import(typeof(bool)));

		context.Method.Body.Variables.Add(flag);
		int input = default, output = default;

		cursor.Emit(OpCodes.Ldc_I4_0);
		cursor.Emit(OpCodes.Stloc, flag);

		Type type = typeof(DamageInfo);
		var rejected = type.GetField(nameof(DamageInfo.rejected));

		ILLabel start = default, end = cursor.DefineLabel();
		cursor.GotoNext(
				( Instruction i ) => i.MatchLdfld(rejected),
				( Instruction i ) => i.MatchBrfalse(out start),
				( Instruction i ) => i.MatchRet()
			);

		var damage = type.GetField(nameof(DamageInfo.damage));

		cursor.GotoNext(( Instruction i ) => i.MatchStfld(damage));
		cursor.GotoNext(( Instruction i ) => i.MatchLdfld(damage));
		cursor.GotoNext(( Instruction i ) => i.MatchStloc(out output));

		cursor.GotoLabel(start);

		cursor.Emit(OpCodes.Ldarg_1);
		cursor.Emit(OpCodes.Ldfld, damage);
		cursor.Emit(OpCodes.Stloc, output);

		var echo = type.GetField(nameof(DamageInfo.delayedDamageSecondHalf));

		cursor.Emit(OpCodes.Ldarg_1);
		cursor.Emit(OpCodes.Ldfld, echo);
		cursor.Emit(OpCodes.Brtrue, end);

		type = typeof(RoR2Content.Buffs);
		var curse = type.GetField(nameof(RoR2Content.Buffs.PermanentCurse));

		cursor.GotoNext(( Instruction i ) => i.MatchLdsfld(curse));
		cursor.GotoPrev(MoveType.After, ( Instruction i ) => i.MatchLdfld(echo));

		cursor.Emit(OpCodes.Pop);
		cursor.Emit(OpCodes.Ldc_I4_0);

		cursor.GotoPrev(
				( Instruction i ) => i.MatchLdloc(output),
				( Instruction i ) => i.MatchStloc(out input)
			);

		ILLabel handle = cursor.DefineLabel(), skip = default;
		cursor.MarkLabel(end);

		++cursor.Index;
		++cursor.Index;

		cursor.Emit(OpCodes.Ldloc, flag);
		cursor.Emit(OpCodes.Brtrue, handle);

		static float deflect(HealthComponent instance, DamageInfo info, float damage)
		{
			int count = instance.body.inventory.GetItemCount(DLC2Content.Items.DelayedDamage);
			if ( count > 0 ) ++count;

			if ( info.damageType.damageType.HasFlag(DamageType.BypassArmor) )
				count = 0;
			else if ( damage < count )
				count = Mathf.FloorToInt(damage);

			float reduction = instance.itemCounts.armorPlate * 5;
			damage -= count * reduction;

			return Mathf.Max(damage, count / 0.8f);
		}

		type = typeof(DLC2Content.Buffs);
		var buff = type.GetField(nameof(DLC2Content.Buffs.DelayedDamageBuff));

		type = typeof(CharacterBody);
		cursor.GotoPrev(
				MoveType.After,
				( Instruction i ) => i.MatchLdsfld(buff),
				( Instruction i ) => i.MatchCallvirt(type, nameof(CharacterBody.HasBuff)),
				( Instruction i ) => i.MatchBrfalse(out skip)
			);

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldarg_1);
		cursor.Emit(OpCodes.Ldloc, output);
		cursor.EmitDelegate(deflect);
		cursor.Emit(OpCodes.Stloc, output);

		cursor.Emit(OpCodes.Ldc_I4_1);
		cursor.Emit(OpCodes.Stloc, flag);

		cursor.Emit(OpCodes.Br, skip);
		cursor.MarkLabel(handle);

		cursor.Emit(OpCodes.Ldc_I4_0);
		cursor.Emit(OpCodes.Stloc, flag);

		var timer = type.GetField(nameof(CharacterBody.outOfDangerStopwatch), AccessTools.all);
		type = typeof(HealthComponent);
		var body = type.GetField(nameof(HealthComponent.body));

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldfld, body);
		cursor.Emit(OpCodes.Ldc_R4, 0F);
		cursor.Emit(OpCodes.Stfld, timer);

		static float delay(HealthComponent instance)
		{
			float delay = instance.ospTimer;
			return delay > 0 ? Time.fixedDeltaTime * 2 + delay : 0;
		}

		type = typeof(CharacterBody);
		cursor.GotoNext(( Instruction i ) => i.MatchCallvirt(
				type, nameof(CharacterBody.SecondHalfOfDelayedDamage)));

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate(delay);
		cursor.Emit(OpCodes.Add);

		cursor.GotoLabel(skip, MoveType.Before);
		cursor.Emit(OpCodes.Br, end);

		var limit = type.GetProperty(nameof(CharacterBody.oneShotProtectionFraction)).GetMethod;

		cursor.GotoNext(( Instruction i ) => i.MatchCallvirt(limit));
		cursor.GotoNext(( Instruction i ) => i.MatchStloc(out _));

		cursor.Emit(OpCodes.Add);

		var penalty = type.GetProperty(nameof(CharacterBody.cursePenalty)).GetMethod;
		type = typeof(HealthComponent);
		var barrier = type.GetField(nameof(HealthComponent.barrier));

		cursor.GotoPrev(( Instruction i ) => i.MatchLdfld(barrier));
		cursor.GotoNext(( Instruction i ) => i.MatchAdd());

		cursor.Emit(OpCodes.Ldc_R4, 0F);

		var health = type.GetProperty(nameof(HealthComponent.fullCombinedHealth)).GetMethod;
		cursor.GotoPrev(MoveType.After, ( Instruction i ) => i.MatchCall(health));

		cursor.Emit(OpCodes.Ldarg_0);
		cursor.Emit(OpCodes.Ldfld, body);
		cursor.Emit(OpCodes.Call, penalty);
		cursor.Emit(OpCodes.Mul);

		cursor.Emit(OpCodes.Ldc_R4, 0.9F);
		cursor.Emit(OpCodes.Mul);

		static float decay(float limit, HealthComponent instance)
		{
			float temporary = instance.barrier, e8 = 0;
			CharacterBody body = instance.body;

			if ( temporary <= 0 )
				return 0;
			else if ( Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse8 )
				e8 = 1 - Mathf.Pow(1 + 0.4f, -1);

			int count = 2 + body.inventory.GetItemCount(DLC2Content.Items.DelayedDamage);
			float decay = 0.75f * body.barrierDecayRate, damage;

			{
				float a = count, b = -1, c = ( temporary - limit ) * decay;
				b *= limit - e8 * temporary - count * decay;

				damage = -b + Mathf.Sqrt(b * b - 4 * a * c);
				damage /= 2 * a;
			}

			count = Mathf.FloorToInt(temporary / damage);
			temporary -= count * damage;

			limit = 1 + Mathf.Max(e8 * damage + temporary - damage, 0);
			e8 *= body.cursePenalty * count * damage;

			return ( count + 0.1f ) * decay + e8 + limit;
		}

		var previous = type.GetField(
				nameof(HealthComponent.serverDamageTakenThisUpdate), AccessTools.all);
		ILLabel conditional = cursor.DefineLabel();

		cursor.GotoNext(
				MoveType.After,
				( Instruction i ) => i.MatchLdfld(previous),
				( Instruction i ) => i.MatchSub()
			);

		cursor.Emit(OpCodes.Ldloc, flag);
		cursor.Emit(OpCodes.Brfalse, conditional);

		cursor.Emit(OpCodes.Dup);
		cursor.Emit(OpCodes.Ldarg_0);
		cursor.EmitDelegate(decay);
		cursor.Emit(OpCodes.Sub);

		cursor.Emit(OpCodes.Ldc_R4, 0.8F);
		cursor.Emit(OpCodes.Div);

		cursor.MarkLabel(conditional);
	}
}

static class Extension
{
	internal static bool MatchRet(this Instruction i)
	{
		if ( i.OpCode == OpCodes.Br )
			switch ( i.Operand )
			{
				case ILLabel label:
					i = label.Target;
					break;
				case Instruction target:
					i = target;
					break;
			}

		return i.OpCode == OpCodes.Ret;
	}
}

static class Compatibility
{
	static internal void Check()
	{
		if ( Chainloader.PluginInfos.ContainsKey(identifier) )
			Harmony.CreateAndPatchAll(typeof(Compatibility));
	}

	internal const string identifier = "Early.Wolfo.WolfFixes";

	[HarmonyPatch(typeof(ItemFixes), nameof(ItemFixes.FixWarpedEchoE8))]
	[HarmonyPatch(typeof(ItemFixes), nameof(ItemFixes.FixWarpedEchoNotUsingArmor))]
	[HarmonyPrefix]
	static bool Apply(ILContext __0)
	{
		return false;
	}
}
