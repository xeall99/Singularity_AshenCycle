#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

public partial class BattleSnapshotTests
{
    [Test]
    public void TC_SKILL_001_CatalogContainsThirtyStableUniqueOfficialSkills()
    {
        Array definitions = (Array)InvokeRuntimeStatic("SkillCatalog", "get_All");
        Assert.That(definitions.Length, Is.EqualTo(30));
        string[] codes = definitions.Cast<object>()
            .Select(item => RuntimeProperty<string>(item, "Code")).ToArray();
        Assert.That(codes.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(30));
        string[] activeEffects = definitions.Cast<object>()
            .Select(item => RuntimeProperty<object>(item, "ActiveEffect").ToString()).ToArray();
        string[] passiveEffects = definitions.Cast<object>()
            .Select(item => RuntimeProperty<object>(item, "PassiveEffect").ToString()).ToArray();
        string[] passiveDescriptions = definitions.Cast<object>()
            .Select(item => RuntimeProperty<string>(item, "PassiveDescription")).ToArray();
        string[] descriptions = definitions.Cast<object>()
            .Select(item => RuntimeProperty<string>(item, "Description")).ToArray();
        Assert.That(activeEffects.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(30),
            "Each catalog skill must have its own active effect identity.");
        Assert.That(passiveEffects.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(30),
            "Each catalog skill must have its own passive effect identity.");
        Assert.That(passiveEffects, Does.Not.Contain("None"));
        Assert.That(passiveDescriptions.All(text => !string.IsNullOrWhiteSpace(text)), Is.True);
        Assert.That(passiveDescriptions.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(30),
            "Each skill must explain its own distinct passive.");
        Assert.That(passiveDescriptions.Count(text =>
            text.Contains("each battle", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("once per battle", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(29),
            "Every one-shot passive must make its per-battle limit clear in the UI copy.");
        Assert.That(passiveDescriptions.Count(text => text.Contains(
            "repeatable", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1),
            "Echo Script is the only passive that can repeat within a battle.");
        Assert.That(descriptions.Count(text => text.Contains(
            "Passive (once per battle)", StringComparison.Ordinal)), Is.EqualTo(29),
            "Detailed descriptions must disclose the same one-shot limit.");
        Assert.That(descriptions.Count(text => text.Contains(
            "Passive (repeatable)", StringComparison.Ordinal)), Is.EqualTo(1),
            "The detailed Echo Script description must preserve its repeatable rule.");
        Assert.That(InvokeRuntimeStatic("SkillCatalog", "Get", "SKL_SHADOW_STRIKE"), Is.Not.Null);
        Assert.That(InvokeRuntimeStatic("SkillCatalog", "Get", "SKL_CUSTOM_PLAYER_SKILL"), Is.Null);
    }

    [Test]
    public void TC_SKILL_006_StatusDurationExtensionPreservesStacksAndRejectsInvalidInput()
    {
        object statuses = NewRuntimeObject("BattleStatusController");
        InvokeRuntime(statuses, "Apply", Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy"), 2, 2);
        Assert.That((bool)InvokeRuntime(statuses, "ExtendDuration",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy"), 1), Is.True);
        Assert.That((int)InvokeRuntime(statuses, "GetStacks",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy")), Is.EqualTo(2));
        Assert.That((int)InvokeRuntime(statuses, "GetRemainingTurns",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy")), Is.EqualTo(3));
        Assert.That((bool)InvokeRuntime(statuses, "ExtendDuration",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Curse"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy"), 1), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => InvokeRuntime(statuses, "ExtendDuration",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy"), 0));
    }

    [Test]
    public void TC_SKILL_002_StarterCollectionAndLoadoutPersistAcrossStoreRestart()
    {
        string root = CreateSkillTestDirectory();
        string path = Path.Combine(root, "player_progress.db");
        object store = NewRuntimeObject("SkillProgressStore", path);
        try
        {
            Assert.That((bool)InvokeRuntime(store, "Owns", "SKL_SHADOW_STRIKE"), Is.True);
            Assert.That(InvokeRuntime(store, "GetLoadout"),
                Is.EqualTo(new[] { "SKL_SHADOW_STRIKE", null, null }));
            InvokeRuntime(store, "SaveLoadout", (object)new[] { "SKL_SHADOW_STRIKE", null, null });
        }
        finally { InvokeRuntime(store, "Dispose"); }

        object reopened = NewRuntimeObject("SkillProgressStore", path);
        try
        {
            Assert.That((bool)InvokeRuntime(reopened, "Owns", "SKL_SHADOW_STRIKE"), Is.True);
            Assert.That(InvokeRuntime(reopened, "GetLoadout"),
                Is.EqualTo(new[] { "SKL_SHADOW_STRIKE", null, null }));
        }
        finally
        {
            InvokeRuntime(reopened, "Dispose");
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void TC_SKILL_003_OfferIsDeterministicPendingAndClaimableOnlyOnce()
    {
        string root = CreateSkillTestDirectory();
        object store = NewRuntimeObject("SkillProgressStore", Path.Combine(root, "player_progress.db"));
        try
        {
            Assert.That(InvokeRuntime(store, "EnsureStandardRunOffer", "run-ineligible", false, 17), Is.Null);
            object first = InvokeRuntime(store, "EnsureStandardRunOffer", "run-eligible", true, 17);
            object again = InvokeRuntime(store, "EnsureStandardRunOffer", "run-eligible", true, 17);
            string claimId = RuntimeProperty<string>(first, "ClaimId");
            string[] offered = RuntimeProperty<string[]>(first, "OfferedSkills");
            Assert.That(offered.Length, Is.EqualTo(3));
            Assert.That(RuntimeProperty<string[]>(again, "OfferedSkills"), Is.EqualTo(offered));
            Assert.That(offered, Does.Not.Contain("SKL_SHADOW_STRIKE"));
            Assert.That(RuntimeProperty<string>(InvokeRuntime(store, "GetPendingOffer"), "ClaimId"), Is.EqualTo(claimId));
            string choice = offered[0];
            Assert.That((bool)InvokeRuntime(store, "TryClaim", claimId, choice), Is.True);
            Assert.That((bool)InvokeRuntime(store, "TryClaim", claimId, choice), Is.True);
            Assert.That((bool)InvokeRuntime(store, "Owns", choice), Is.True);
            Assert.That(InvokeRuntime(store, "GetPendingOffer"), Is.Null);
        }
        finally
        {
            InvokeRuntime(store, "Dispose");
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void TC_SKILL_004_LoadoutRejectsLockedUnknownAndDuplicateSkills()
    {
        string root = CreateSkillTestDirectory();
        object store = NewRuntimeObject("SkillProgressStore", Path.Combine(root, "player_progress.db"));
        try
        {
            Assert.Throws<InvalidOperationException>(() => InvokeRuntime(store, "SaveLoadout",
                (object)new[] { "SKL_SHADOW_STRIKE", "SKL_ASHWAKE_CANTICLE", null }));
            Assert.Throws<InvalidOperationException>(() => InvokeRuntime(store, "SaveLoadout",
                (object)new[] { "SKL_SHADOW_STRIKE", "SKL_SHADOW_STRIKE", null }));
            Assert.Throws<InvalidOperationException>(() => InvokeRuntime(store, "SaveLoadout",
                (object)new[] { "SKL_CUSTOM_TEST", null, null }));
            Assert.Throws<ArgumentException>(() => InvokeRuntime(store, "SaveLoadout",
                (object)new[] { "SKL_SHADOW_STRIKE" }));
        }
        finally
        {
            InvokeRuntime(store, "Dispose");
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void TC_SKILL_005_RunSnapshotKeepsEquippedSkillCodesAcrossRestore()
    {
        object run = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(run, "StartNewRun", 827310, 100, 5, 100, 10);
        string[] loadout = { "SKL_SHADOW_STRIKE", "SKL_WRAITH_CANDLE", "SKL_UMBRAL_EXECUTION" };
        InvokeRuntime(run, "SetEquippedSkillCodes", (object)loadout);
        object snapshot = InvokeRuntime(run, "CaptureState");
        Assert.That(RuntimeField<int>(snapshot, "SchemaVersion"),
            Is.EqualTo(RuntimeType("SeededRunState").GetField("CurrentSchemaVersion").GetRawConstantValue()));

        object restored = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(restored, "RestoreState", snapshot, 100, 10);
        Assert.That(RuntimeProperty<string[]>(restored, "EquippedSkillCodes"), Is.EqualTo(loadout));
    }

    [UnityTest]
    public IEnumerator TC_SKILL_007_CinderKeyConsumesBurnForEmberInsteadOfWeakness()
    {
        Set("enemyHP", 1000);
        Set("playerMana", 10);
        EquipSkillCodes("SKL_CINDER_KEY");
        object statuses = Get<object>("battleStatuses");
        InvokeRuntime(statuses, "Apply", Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy"), 2, 2);
        int embersBefore = RuntimeProperty<int>(Get<object>("runManager"), "TotalEmbers");

        Call("UseSkillSlot", 0);

        Assert.That((int)InvokeRuntime(statuses, "GetStacks",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy")), Is.EqualTo(1));
        Assert.That((int)InvokeRuntime(statuses, "GetStacks",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Weakness"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy")), Is.Zero);
        Assert.That(RuntimeProperty<int>(Get<object>("runManager"), "TotalEmbers"),
            Is.EqualTo(embersBefore + 1));
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC_SKILL_008_AbyssalVerdictAppliesCurseOnlyAfterConsumingTwoBurn()
    {
        Set("enemyHP", 1000);
        Set("playerMana", 10);
        EquipSkillCodes("SKL_ABYSSAL_VERDICT");
        object statuses = Get<object>("battleStatuses");
        InvokeRuntime(statuses, "Apply", Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy"), 2, 2);

        Call("UseSkillSlot", 0);

        Assert.That((int)InvokeRuntime(statuses, "GetStacks",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Curse"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy")), Is.EqualTo(1));
        Assert.That((int)InvokeRuntime(statuses, "GetStacks",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Weakness"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy")), Is.Zero);
        Assert.That((int)InvokeRuntime(statuses, "GetStacks",
            Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn"),
            Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy")), Is.Zero);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC_SKILL_009_GuardSetsUpEchoScriptDiscountAndEmberManaPassive()
    {
        Set("enemyHP", 1000);
        Set("playerMana", 0);
        EquipSkillCodes("SKL_ECHO_SCRIPT", "SKL_EMBER_REPRISE");
        Call("UseGuard");
        yield return AwaitEnemy();
        Assert.That(Get<int>("playerMana"), Is.EqualTo(3),
            "Guard restores 2 Mana and the enemy turn restores 1.");
        Assert.That(Get<object>("previousPlayerAction").ToString(), Is.EqualTo("Guard"),
            "The Guard action must remain the previous player action after the enemy turn.");
        Assert.That(Property<string[]>(Get<object>("runManager"), "EquippedSkillCodes"),
            Is.EqualTo(new[] { "SKL_ECHO_SCRIPT", "SKL_EMBER_REPRISE", null }));

        Call("UseSkillSlot", 0);

        object usedPassives = Get<object>("usedSkillPassives");
        Assert.That((bool)usedPassives.GetType().GetMethod("Contains").Invoke(
            usedPassives, new object[] { "SKL_EMBER_REPRISE" }), Is.True,
            "The equipped Ember Reprise passive should be consumed by a Skill after Guard.");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(1),
            "Echo Script costs 3 after Guard, then Ember Reprise restores 1 Mana.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TC_SKILL_010_RiftSiphonTriggersWhenBurnKillsCursedEnemy()
    {
        Set("playerHP", 50);
        Set("enemyHP", 1);
        EquipSkillCodes("SKL_RIFT_SIPHON");
        object statuses = Get<object>("battleStatuses");
        object burn = Enum.Parse(RuntimeType("BattleStatusEffectType"), "Burn");
        object curse = Enum.Parse(RuntimeType("BattleStatusEffectType"), "Curse");
        object enemy = Enum.Parse(RuntimeType("BattleStatusTarget"), "Enemy");
        InvokeRuntime(statuses, "Apply", burn, enemy, 1, 2);
        InvokeRuntime(statuses, "Apply", curse, enemy, 1, 2);

        object run = Get<object>("runManager");
        int hpBeforeEnemyTurn = Property<int>(run, "CurrentHP");
        int plannedDamage = Property<int>(Plan, "TotalEstimatedDamage");
        float damageMultiplier = 1f - (float)Call("GetCurrentGuardDamageReduction");
        int expectedIncomingDamage = Math.Max(0, (int)Math.Ceiling(plannedDamage * damageMultiplier));

        Call("UseGuard");
        yield return AwaitEnemy();

        Assert.That((int)InvokeRuntime(statuses, "GetStacks", curse, enemy), Is.GreaterThan(0),
            "The curse must still be active when the Burn tick defeats the enemy.");
        object usedPassives = Get<object>("usedSkillPassives");
        Assert.That((bool)usedPassives.GetType().GetMethod("Contains").Invoke(
            usedPassives, new object[] { "SKL_RIFT_SIPHON" }), Is.True,
            "A cursed-enemy kill from the enemy-turn Burn tick must trigger Rift Siphon.");
        Assert.That(Property<int>(run, "CurrentHP"),
            Is.EqualTo(hpBeforeEnemyTurn - expectedIncomingDamage + 4));
        yield return null;
    }

    private void EquipSkillCodes(params string[] codes)
    {
        int slotCount = (int)RuntimeType("SkillProgressStore")
            .GetField("LoadoutSlotCount").GetRawConstantValue();
        string[] loadout = new string[slotCount];
        Array skills = Array.CreateInstance(RuntimeType("SkillDefinition"), slotCount);
        for (int index = 0; index < codes.Length; index++)
        {
            loadout[index] = codes[index];
            skills.SetValue(InvokeRuntimeStatic("SkillCatalog", "Get", codes[index]), index);
        }
        InvokeRuntime(Get<object>("runManager"), "SetEquippedSkillCodes", (object)loadout);
        Set("equippedSkills", skills);
    }

    private string CreateSkillTestDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "syngrava-skill-progress-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
#endif
