#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

// Reflection keeps the existing Assembly-CSharp/Inspector layout intact.
// Run through Tools/Run-BattleSnapshotTests.ps1: the production database initializer
// overwrites its runtime database in Editor, so this suite requires an isolated product.
public class BattleSnapshotTests
{
    private Component manager;
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string PlanError =
        "EnemyActionPlan tidak tersedia atau tidak valid. Restart Run diperlukan.";

    private T Get<T>(string name) => (T)manager.GetType().GetField(name, Fields).GetValue(manager);
    private void Set(string name, object value)
    {
        manager.GetType().GetField(name, Fields).SetValue(manager, value);

        if (name != "playerHP" && name != "playerMana")
        {
            return;
        }

        object activeRun = Get<object>("runManager");

        if (activeRun == null)
        {
            return;
        }

        int currentHP = name == "playerHP"
            ? (int)value
            : Property<int>(activeRun, "CurrentHP");
        int currentMana = name == "playerMana"
            ? (int)value
            : Property<int>(activeRun, "CurrentMana");

        activeRun.GetType().GetMethod("SetResources").Invoke(
            activeRun,
            new object[] { currentHP, currentMana, int.MaxValue, int.MaxValue }
        );
    }
    private object Call(string name, params object[] args) => manager.GetType().GetMethod(name, Fields).Invoke(manager, args);
    private T Property<T>(object target, string name) => (T)target.GetType().GetProperty(name).GetValue(target);
    private Type RuntimeType(string name) => Type.GetType($"{name}, Assembly-CSharp", true);
    private object NewRuntimeObject(string name, params object[] args) =>
        Activator.CreateInstance(RuntimeType(name), args);
    private object InvokeRuntime(object target, string name, params object[] args)
    {
        try
        {
            return target.GetType().GetMethod(name).Invoke(target, args);
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException ?? exception;
        }
    }
    private object InvokeRuntimeStatic(
        string typeName,
        string name,
        params object[] args
    )
    {
        try
        {
            return RuntimeType(typeName).GetMethod(name).Invoke(null, args);
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException ?? exception;
        }
    }
    private T RuntimeProperty<T>(object target, string name) =>
        (T)target.GetType().GetProperty(name).GetValue(target);
    private T RuntimeField<T>(object target, string name) =>
        (T)target.GetType().GetField(name).GetValue(target);
    private void SetRuntimeField(object target, string name, object value) =>
        target.GetType().GetField(name).SetValue(target, value);
    private object Plan => Get<object>("enemyActionPlan");
    private object Orbit => Get<object>("orbitState");
    private int OrbitCount => Property<int>(Orbit, "Count");
    private GameObject TooltipRoot => Get<GameObject>("convergenceTooltipRoot");
    private Array RewardChoices => Get<Array>("currentRewardChoices");
    private Component SettingsUI => UnityEngine.Object.FindAnyObjectByType(
        Type.GetType("BattleSettingsUI, Assembly-CSharp", true)
    ) as Component;
    private Component RunMapUI => UnityEngine.Object.FindAnyObjectByType(
        Type.GetType("StandardRunMapUI, Assembly-CSharp", true)
    ) as Component;

    private bool IsRunMapOpen
    {
        get
        {
            Component mapUI = RunMapUI;
            return mapUI != null && Property<bool>(mapUI, "IsOpen");
        }
    }

    private object CurrentRunNode =>
        Property<object>(Get<object>("runManager"), "CurrentNode");

    private void EnterOrResolveCurrentMapNode()
    {
        Assert.That(IsRunMapOpen, Is.True, "Run Map harus terbuka.");
        object node = CurrentRunNode;
        Assert.That(node, Is.Not.Null);
        Call("HandleRunMapNodeSelected", RuntimeField<string>(node, "Code"));
    }

    private void AdvanceMapUntilBattleStarts()
    {
        int safety = 0;

        while (IsRunMapOpen)
        {
            Assert.That(safety++, Is.LessThan(12), "Route tidak mencapai battle.");
            EnterOrResolveCurrentMapNode();
        }

        Assert.That(Get<bool>("isPlayerTurn"), Is.True);
        Assert.That(Get<bool>("battleEnded"), Is.False);
        Assert.That(Plan, Is.Not.Null);
    }

    private object DatabaseEnemy(int battleIndex)
    {
        Type databaseType = Type.GetType("GameDatabase, Assembly-CSharp", true);
        object database = databaseType.GetProperty("Instance").GetValue(null);

        return databaseType.GetMethod("GetEnemyByBattleIndex").Invoke(
            database,
            new object[] { battleIndex }
        );
    }

    private void Mana(int value)
    {
        Set("playerMana", value);
        Call("UpdateUI");
        Call("UpdateButtons");
    }

    private void Click(string field) => Get<Button>(field).onClick.Invoke();

    private Button RewardButton(int index)
    {
        return Get<Button>($"rewardButton{index + 1}");
    }

    private int FindRewardChoice(string effectType)
    {
        Array choices = RewardChoices;

        for (int index = 0; index < choices.Length; index++)
        {
            object choice = choices.GetValue(index);

            if (Property<object>(choice, "EffectType").ToString() == effectType)
            {
                return index;
            }
        }

        Assert.Fail($"Reward effect {effectType} tidak ditemukan.");
        return -1;
    }

    private int MaximumTier(Array values)
    {
        int maximum = 0;

        for (int index = 0; index < values.Length; index++)
        {
            maximum = Mathf.Max(
                maximum,
                Property<int>(values.GetValue(index), "Tier")
            );
        }

        return maximum;
    }

    private void PointerEnter(Button button)
    {
        Assert.That(EventSystem.current, Is.Not.Null);
        ExecuteEvents.Execute(
            button.gameObject,
            new PointerEventData(EventSystem.current),
            ExecuteEvents.pointerEnterHandler
        );
    }

    private void PointerExit(Button button)
    {
        ExecuteEvents.Execute(
            button.gameObject,
            new PointerEventData(EventSystem.current),
            ExecuteEvents.pointerExitHandler
        );
    }

    private void Select(Button button)
    {
        ExecuteEvents.Execute(
            button.gameObject,
            new BaseEventData(EventSystem.current),
            ExecuteEvents.selectHandler
        );
    }

    private void Deselect(Button button)
    {
        ExecuteEvents.Execute(
            button.gameObject,
            new BaseEventData(EventSystem.current),
            ExecuteEvents.deselectHandler
        );
    }

    private void AddOrbit(string action)
    {
        Type actionType = Type.GetType("PlayerActionType, Assembly-CSharp", true);
        Orbit.GetType().GetMethod("AddAction").Invoke(Orbit, new[] { Enum.Parse(actionType, action) });
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        if (Application.productName != "SYNGRAVA_BattleTests")
        {
            Assert.Ignore("Use Tools/Run-BattleSnapshotTests.ps1 to protect the user's runtime database.");
        }

        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/Battle.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        manager = UnityEngine.Object.FindAnyObjectByType(
            Type.GetType("BattleManager, Assembly-CSharp", true)) as Component;
        Assert.That(manager, Is.Not.Null);
        Assert.That(IsRunMapOpen, Is.True);
        AdvanceMapUntilBattleStarts();
        Assert.That(Get<bool>("isPlayerTurn"), Is.True);
        Assert.That(Plan, Is.Not.Null);
        Time.timeScale = 10f;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;
        Type databaseType = Type.GetType("GameDatabase, Assembly-CSharp", true);
        var database = databaseType.GetProperty("Instance").GetValue(null) as Component;
        if (manager != null)
        {
            ((MonoBehaviour)manager).StopAllCoroutines();
        }
        if (database != null)
        {
            UnityEngine.Object.Destroy(database.gameObject);
        }
        yield return null;
        LogAssert.NoUnexpectedReceived();
    }

    private IEnumerator AwaitEnemy()
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while (!Get<bool>("isPlayerTurn") && !Get<bool>("battleEnded"))
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Enemy turn soft lock");
            yield return null;
        }
    }

    private IEnumerator AwaitRewardSelection(bool waitForCards = true)
    {
        float deadline = Time.realtimeSinceStartup + 10f;

        while (!Get<bool>("rewardSelectionOpen"))
        {
            Assert.That(
                Time.realtimeSinceStartup,
                Is.LessThan(deadline),
                "Reward selection did not open automatically"
            );
            yield return null;
        }

        if (!waitForCards)
        {
            yield break;
        }

        Button[] buttons =
        {
            Get<Button>("rewardButton1"),
            Get<Button>("rewardButton2"),
            Get<Button>("rewardButton3")
        };

        while (Array.Exists(buttons, button => !button.interactable))
        {
            Assert.That(
                Time.realtimeSinceStartup,
                Is.LessThan(deadline),
                "Reward card entrance did not finish"
            );
            yield return null;
        }
    }

    [Test]
    public void TC_ANL_001_EightManaRevealsPreparedPlan()
    {
        Mana(8);
        object plan = Plan;
        Assert.That(Get<TMP_Text>("enemyIntentText").gameObject.activeSelf, Is.False);
        Click("analyzeButton");
        Assert.That(Get<int>("playerMana"), Is.Zero);
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.True);
        Assert.That(Get<Button>("analyzeButton").interactable, Is.False);
        Assert.That(Plan, Is.SameAs(plan));
        TMP_Text result = Get<TMP_Text>("enemyIntentText");
        Assert.That(result.gameObject.activeSelf, Is.True);
        StringAssert.Contains("Action: Normal Attack", result.text);
        StringAssert.Contains("Target: Warden", result.text);
        StringAssert.Contains("Hits: 1", result.text);
        StringAssert.Contains($"Estimated Damage: {Property<int>(plan, "TotalEstimatedDamage")}", result.text);
        Assert.That(result.rectTransform.rect.height, Is.GreaterThan(55f));
        Assert.That(Get<bool>("isPlayerTurn"), Is.True);
        Assert.That(OrbitCount, Is.Zero);
    }

    [Test]
    public void TC_ANL_002_InsufficientManaLeavesStateUntouched()
    {
        Mana(7);
        object plan = Plan;
        AddOrbit("Guard");
        Call("UseAnalyze"); // Exercise validation even though the UI button is disabled.
        Assert.That(Get<int>("playerMana"), Is.EqualTo(7));
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.False);
        Assert.That(Get<bool>("isPlayerTurn"), Is.True);
        Assert.That(OrbitCount, Is.EqualTo(1));
        Assert.That(Plan, Is.SameAs(plan));
        Assert.That(Get<TMP_Text>("battleLogText").text, Is.EqualTo("Mana tidak cukup untuk Analyze"));
        Mana(8);
        Assert.That(Get<Button>("analyzeButton").interactable, Is.True);
        Click("analyzeButton");
        Assert.That(Get<int>("playerMana"), Is.Zero);
    }

    [Test]
    public void TC_ANL_003_RepeatCannotChargeOrReplacePlan()
    {
        Mana(10);
        object plan = Plan;
        Click("analyzeButton");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(2));
        Mana(10);
        Click("analyzeButton");
        Call("UseAnalyze");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(10));
        Assert.That(Plan, Is.SameAs(plan));
        Assert.That(Get<Button>("analyzeButton").interactable, Is.False);
    }

    [UnityTest]
    public IEnumerator TC_ANL_004_ExecutionUsesRevealedSnapshotEvenIfSourceChanges()
    {
        Mana(8);
        object plan = Plan;
        int damage = Property<int>(plan, "TotalEstimatedDamage");
        int hp = Get<int>("playerHP");
        Click("analyzeButton");
        object enemy = Get<object>("currentEnemy");
        enemy.GetType().GetProperty("AttackDamage").SetValue(enemy, damage + 100);
        Set("enemyHP", 1000);
        Click("basicAttackButton");
        yield return AwaitEnemy();
        Assert.That(Get<int>("playerHP"), Is.EqualTo(hp - damage));
        Assert.That(Plan, Is.SameAs(plan));
        Assert.That(Property<int>(plan, "DamagePerHit"), Is.EqualTo(damage));
        Assert.That(Get<TMP_Text>("enemyIntentText").gameObject.activeSelf, Is.False);
        Assert.That(Get<TMP_Text>("enemyIntentText").text, Is.Empty);
        // A second turn also repeats the same plan; no late reroll or node reset.
        Click("basicAttackButton");
        yield return AwaitEnemy();
        Assert.That(Get<int>("playerHP"), Is.EqualTo(hp - 2 * damage));
        Assert.That(Plan, Is.SameAs(plan));
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.True);
    }

    [UnityTest]
    public IEnumerator TC_ANL_005_RewardAdvancesOneNodeAndResetsAnalyze()
    {
        Mana(8);
        Click("analyzeButton");
        object oldPlan = Plan;
        Set("enemyHP", 1);
        Click("basicAttackButton");
        Assert.That(Get<bool>("battleEnded"), Is.True);
        int embers = Get<int>("totalEmbers");
        Call("EndBattle", true);
        Assert.That(Get<int>("totalEmbers"), Is.EqualTo(embers));
        Assert.That(Get<Button>("nextBattleButton").gameObject.activeSelf, Is.False);
        yield return AwaitRewardSelection();
        int attackChoice = FindRewardChoice("AttackPercent");
        RewardButton(attackChoice).onClick.Invoke();
        RewardButton(attackChoice).onClick.Invoke();
        Assert.That(Get<int>("battleNumber"), Is.EqualTo(2));
        Assert.That(Get<float>("runAttackBonusPercent"), Is.EqualTo(0.1f));
        Assert.That(IsRunMapOpen, Is.True);
        AdvanceMapUntilBattleStarts();
        Assert.That(Plan, Is.Not.SameAs(oldPlan));
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.False);
        Assert.That(Get<TMP_Text>("enemyIntentText").text, Is.Empty);
        Assert.That(Get<TMP_Text>("enemyIntentText").gameObject.activeSelf, Is.False);
        Assert.That(OrbitCount, Is.Zero);
        // The use limit resets on the next node, but spent Mana stays spent.
        Assert.That(Get<int>("playerMana"), Is.Zero);
        Assert.That(Get<Button>("analyzeButton").interactable, Is.False);
        Mana(8);
        Assert.That(Get<Button>("analyzeButton").interactable, Is.True);
        Click("analyzeButton");
        Assert.That(Get<int>("playerMana"), Is.Zero);
    }

    [Test]
    public void TC_ANL_006_TwoOrbitActionsAreUnchanged()
    {
        AddOrbit("Attack");
        AddOrbit("Attack");
        object actions = Property<object>(Orbit, "Actions");
        Mana(8);
        Click("analyzeButton");
        Assert.That(OrbitCount, Is.EqualTo(2));
        Assert.That(Property<object>(Orbit, "Actions"), Is.SameAs(actions));
        var list = (IList)actions;
        Assert.That(list[0].ToString(), Is.EqualTo("Attack"));
        Assert.That(list[1].ToString(), Is.EqualTo("Attack"));
        Assert.That(Get<string>("convergenceNotice"), Is.Empty);
        Assert.That(Get<bool>("reversalCounterReady"), Is.False);
    }

    [UnityTest]
    public IEnumerator TC_ANL_007_AnalyzeDoesNotScheduleEnemyAndAllowsValidCommands()
    {
        Mana(10);
        int hp = Get<int>("playerHP");
        Click("analyzeButton");
        yield return new WaitForSeconds(3f);
        Assert.That(Get<bool>("isPlayerTurn"), Is.True);
        Assert.That(Get<int>("playerHP"), Is.EqualTo(hp));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(2));
        foreach (string field in new[] { "basicAttackButton", "skillButton", "guardButton" })
        {
            Assert.That(Get<Button>(field).interactable, Is.True, field);
        }
    }

    [Test]
    public void TC_SET_001_SettingsButtonOpensAndRestoresBattlePause()
    {
        Assert.That(SettingsUI, Is.Not.Null);

        Button settingsButton = (Button)SettingsUI.GetType()
            .GetProperty("SettingsButton")
            .GetValue(SettingsUI);
        GameObject settingsPanel = (GameObject)SettingsUI.GetType()
            .GetProperty("SettingsPanel")
            .GetValue(SettingsUI);
        Type gearType = Type.GetType("GearIconGraphic, Assembly-CSharp", true);

        Assert.That(settingsButton, Is.Not.Null);
        Assert.That(settingsPanel, Is.Not.Null);
        Assert.That(settingsPanel.activeSelf, Is.False);
        Component gearIcon = settingsButton.GetComponentInChildren(
            gearType,
            true
        );
        Assert.That(gearIcon, Is.Not.Null);
        Assert.That(gearIcon.GetComponent<CanvasRenderer>(), Is.Not.Null);
        Assert.That(
            gearIcon.GetComponent<Graphic>().color.a,
            Is.GreaterThan(0.99f)
        );

        RectTransform buttonRect = settingsButton.GetComponent<RectTransform>();
        Assert.That(buttonRect.anchorMin, Is.EqualTo(Vector2.zero));
        Assert.That(buttonRect.anchorMax, Is.EqualTo(Vector2.zero));
        Assert.That(buttonRect.anchoredPosition.x, Is.GreaterThan(20f));
        Assert.That(buttonRect.anchoredPosition.y, Is.GreaterThan(20f));
        Assert.That(buttonRect.sizeDelta.x, Is.InRange(62f, 76f));
        Assert.That(buttonRect.sizeDelta.y, Is.InRange(62f, 76f));

        float originalTimeScale = Time.timeScale;

        try
        {
            settingsButton.onClick.Invoke();
            Assert.That(settingsPanel.activeSelf, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(
                settingsButton.transform.GetSiblingIndex(),
                Is.EqualTo(settingsButton.transform.parent.childCount - 1)
            );

            settingsButton.onClick.Invoke();
            Assert.That(settingsPanel.activeSelf, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(originalTimeScale));
        }
        finally
        {
            settingsPanel.SetActive(false);
            Time.timeScale = originalTimeScale;
        }
    }

    [Test]
    public void TC_SET_002_SettingsPanelShowsCategoryListAndOptionRows()
    {
        Assert.That(SettingsUI, Is.Not.Null);

        GameObject settingsPanel = (GameObject)SettingsUI.GetType()
            .GetProperty("SettingsPanel")
            .GetValue(SettingsUI);
        Transform settingsCard = settingsPanel.transform.Find("SettingsCard");
        Assert.That(settingsCard, Is.Not.Null);

        Transform categoryPanel = settingsCard.Find("SettingsCategoryPanel");
        Transform contentPanel = settingsCard.Find("SettingsContentPanel");

        Assert.That(categoryPanel, Is.Not.Null);
        Assert.That(contentPanel, Is.Not.Null);
        Assert.That(
            categoryPanel.GetComponentsInChildren<Button>(true),
            Has.Length.EqualTo(4)
        );
        Assert.That(
            contentPanel.GetComponentsInChildren<TMP_Text>(true).Length,
            Is.GreaterThanOrEqualTo(7)
        );

        Button accessibilityCategory =
            categoryPanel.Find("SettingsCategory_1").GetComponent<Button>();

        Assert.That(accessibilityCategory, Is.Not.Null);
        accessibilityCategory.onClick.Invoke();

        TMP_Text[] labels = contentPanel.GetComponentsInChildren<TMP_Text>(true);
        Assert.That(
            labels.Any(label => label.text == "ACCESSIBILITY"),
            Is.True
        );
        Assert.That(
            labels.Any(label => label.text == "High Contrast"),
            Is.True
        );
    }

    [Test]
    public void TC_BAL_001_EnemyMaxHPUsesGentleEarlyCurve()
    {
        int[] expectedMaxHP = { 40, 50, 60 };
        int[] expectedAttackDamage = { 12, 14, 17 };
        int[] expectedRewards = { 10, 18, 30 };
        bool[] expectedBossFlags = { false, false, true };

        for (int index = 0; index < expectedMaxHP.Length; index++)
        {
            object enemy = DatabaseEnemy(index + 1);
            Assert.That(enemy, Is.Not.Null, $"Enemy battle index {index + 1}");
            Assert.That(Property<int>(enemy, "MaxHP"), Is.EqualTo(expectedMaxHP[index]));
            Assert.That(
                Property<int>(enemy, "AttackDamage"),
                Is.EqualTo(expectedAttackDamage[index])
            );
            Assert.That(
                Property<int>(enemy, "RewardEmbers"),
                Is.EqualTo(expectedRewards[index])
            );
            Assert.That(
                Property<bool>(enemy, "IsBoss"),
                Is.EqualTo(expectedBossFlags[index])
            );
        }

        Assert.That(expectedMaxHP[1], Is.LessThanOrEqualTo(expectedMaxHP[0] * 1.25f));
        Assert.That(expectedMaxHP[2], Is.LessThanOrEqualTo(expectedMaxHP[1] * 1.25f));
    }

    [UnityTest]
    public IEnumerator TC_HVR_001_PreviewIsHiddenUntilTwoOrbitActions()
    {
        yield return new WaitForSecondsRealtime(0.6f);
        Button attack = Get<Button>("basicAttackButton");

        Assert.That(TooltipRoot, Is.Not.Null);
        Assert.That(TooltipRoot.activeSelf, Is.False);
        Assert.That(Get<TMP_Text>("convergencePreviewText").text, Is.Empty);

        PointerEnter(attack);
        Assert.That(TooltipRoot.activeSelf, Is.False);
        PointerExit(attack);

        AddOrbit("Attack");
        PointerEnter(attack);
        Assert.That(TooltipRoot.activeSelf, Is.False);
        Assert.That(Get<TMP_Text>("convergencePreviewText").text, Is.Empty);
        PointerExit(attack);
    }

    [UnityTest]
    public IEnumerator TC_HVR_002_EachValidCommandShowsOnlyItsOwnPreviewAboveButton()
    {
        yield return new WaitForSecondsRealtime(0.6f);
        Mana(10);
        AddOrbit("Attack");
        AddOrbit("Attack");

        Button[] buttons =
        {
            Get<Button>("basicAttackButton"),
            Get<Button>("skillButton"),
            Get<Button>("guardButton")
        };
        string[] labels = { "ATTACK", "SKILL", "GUARD" };
        string[] expectedResults =
        {
            "UNSTABLE PULSE",
            "EVENT HORIZON",
            "UNSTABLE PULSE"
        };

        for (int index = 0; index < buttons.Length; index++)
        {
            PointerEnter(buttons[index]);
            Assert.That(TooltipRoot.activeSelf, Is.True, labels[index]);

            string preview = Get<TMP_Text>("convergencePreviewText").text;
            StringAssert.Contains(labels[index], preview);
            StringAssert.Contains(expectedResults[index], preview);

            for (int otherIndex = 0; otherIndex < labels.Length; otherIndex++)
            {
                if (otherIndex != index)
                {
                    StringAssert.DoesNotContain(labels[otherIndex] + " →", preview);
                }
            }

            RectTransform tooltipRect = TooltipRoot.GetComponent<RectTransform>();
            RectTransform buttonRect = buttons[index].GetComponent<RectTransform>();
            RectTransform canvasRect = TooltipRoot.transform.parent as RectTransform;
            Vector3[] tooltipCorners = new Vector3[4];
            Vector3[] buttonCorners = new Vector3[4];
            Vector3[] canvasCorners = new Vector3[4];
            tooltipRect.GetWorldCorners(tooltipCorners);
            buttonRect.GetWorldCorners(buttonCorners);
            canvasRect.GetWorldCorners(canvasCorners);

            Assert.That(
                tooltipCorners[0].y,
                Is.GreaterThan(buttonCorners[1].y),
                labels[index] + " tooltip must be above its button"
            );
            Assert.That(tooltipCorners[0].x, Is.GreaterThanOrEqualTo(canvasCorners[0].x - 0.5f));
            Assert.That(tooltipCorners[2].x, Is.LessThanOrEqualTo(canvasCorners[2].x + 0.5f));
            Assert.That(tooltipCorners[2].y, Is.LessThanOrEqualTo(canvasCorners[2].y + 0.5f));

            PointerExit(buttons[index]);
            Assert.That(TooltipRoot.activeSelf, Is.False, labels[index]);
        }
    }

    [UnityTest]
    public IEnumerator TC_HVR_003_FocusPointerOverlapAndExecutionStayConsistent()
    {
        yield return new WaitForSecondsRealtime(0.6f);
        Mana(10);
        AddOrbit("Attack");
        AddOrbit("Attack");
        Button attack = Get<Button>("basicAttackButton");
        Button skill = Get<Button>("skillButton");

        Select(attack);
        StringAssert.Contains("ATTACK", Get<TMP_Text>("convergencePreviewText").text);

        PointerEnter(skill);
        StringAssert.Contains("SKILL", Get<TMP_Text>("convergencePreviewText").text);
        StringAssert.Contains("EVENT HORIZON", Get<TMP_Text>("convergencePreviewText").text);

        PointerExit(skill);
        Assert.That(TooltipRoot.activeSelf, Is.True);
        StringAssert.Contains("ATTACK", Get<TMP_Text>("convergencePreviewText").text);

        Deselect(attack);
        Assert.That(TooltipRoot.activeSelf, Is.False);

        Select(skill);
        StringAssert.Contains("EVENT HORIZON", Get<TMP_Text>("convergencePreviewText").text);
        Set("enemyHP", 1000);
        Click("skillButton");
        Assert.That(TooltipRoot.activeSelf, Is.False);
        StringAssert.Contains("EVENT HORIZON", Get<string>("convergenceNotice"));
        Assert.That(OrbitCount, Is.Zero);
    }

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(3, 3)]
    [TestCase(4, 3)]
    [TestCase(10, 8)]
    [TestCase(11, 9)]
    [TestCase(12, 9)]
    [TestCase(int.MaxValue, 1610612736)]
    public void AnalyzeCostRoundsUp(int maxMana, int expected)
    {
        Set("playerMaxMana", maxMana);
        Call("InitializeRunStatController");
        Assert.That((int)Call("GetAnalyzeManaCost"), Is.EqualTo(expected));
    }

    [UnityTest]
    public IEnumerator Regression_AttackSkillAndInputLock()
    {
        Set("enemyHP", 1000);
        int hp = Get<int>("playerHP");
        int damage = Property<int>(Plan, "TotalEstimatedDamage");
        object plan = Plan;
        Click("basicAttackButton");
        Click("basicAttackButton"); // A second listener/input cannot apply damage again.
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(990));
        Assert.That(OrbitCount, Is.EqualTo(1));
        Assert.That(Get<bool>("isPlayerTurn"), Is.False);
        Mana(10);
        Call("UseAnalyze");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(10));
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.False);
        yield return AwaitEnemy();
        Assert.That(Get<int>("playerHP"), Is.EqualTo(hp - damage));
        Mana(10);
        object skill = Get<object>("shadowStrike");
        Click("skillButton");
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(990 - Property<int>(skill, "Power")));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(10 - Property<int>(skill, "ManaCost")));
        Assert.That(OrbitCount, Is.EqualTo(2));
        Assert.That(Plan, Is.SameAs(plan));
        yield return AwaitEnemy();
        Mana(0);
        int before = Get<int>("enemyHP");
        Call("UseSkill");
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(before));
        Assert.That(OrbitCount, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator Regression_GuardReversalPreserveSnapshot()
    {
        Mana(8);
        Click("analyzeButton");
        object plan = Plan;
        int damage = Property<int>(plan, "TotalEstimatedDamage");
        int hp = Get<int>("playerHP");
        Set("enemyHP", 1000);
        AddOrbit("Guard");
        AddOrbit("Attack");
        Click("guardButton");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(2));
        Assert.That(Get<bool>("reversalCounterReady"), Is.True);
        Assert.That(OrbitCount, Is.Zero);
        yield return AwaitEnemy();
        Assert.That(Get<int>("playerHP"), Is.EqualTo(hp - Mathf.CeilToInt(damage * 0.5f)));
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(995));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(3));
        Assert.That(Get<bool>("isGuarding"), Is.False);
        Assert.That(Get<bool>("reversalCounterReady"), Is.False);
        Assert.That(Plan, Is.SameAs(plan));
        Assert.That(Property<int>(plan, "TotalEstimatedDamage"), Is.EqualTo(damage));
    }

    [Test]
    public void Regression_EventHorizonMatchesPreviewAndResolvesOnce()
    {
        Mana(10);
        AddOrbit("Attack");
        AddOrbit("Attack");
        Type actionType = Type.GetType("PlayerActionType, Assembly-CSharp", true);
        object resolver = Get<object>("orbitResolver");
        object preview = resolver.GetType().GetMethod("Preview").Invoke(resolver,
            new[] { Property<object>(Orbit, "Actions"), Enum.Parse(actionType, "Skill") });
        Assert.That(Property<string>(preview, "DisplayName"), Is.EqualTo("EVENT HORIZON"));
        Set("enemyHP", 1000);
        int power = Property<int>(Get<object>("shadowStrike"), "Power");
        Click("skillButton");
        Click("skillButton");
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(1000 - power - Mathf.RoundToInt(power * 0.3f)));
        Assert.That(OrbitCount, Is.Zero);
        StringAssert.Contains("EVENT HORIZON", Get<string>("convergenceNotice"));
    }

    [Test]
    public void Regression_UnstablePulseRestoresOneMana()
    {
        Mana(4);
        AddOrbit("Attack");
        AddOrbit("Attack");
        Set("enemyHP", 1000);
        Click("basicAttackButton");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(5));
        Assert.That(OrbitCount, Is.Zero);
        StringAssert.Contains("UNSTABLE PULSE", Get<string>("convergenceNotice"));
    }

    [UnityTest]
    public IEnumerator TC_RWD_001_NonFinalVictoryOpensModalAutomatically()
    {
        Set("enemyHP", 1);
        Click("basicAttackButton");

        Assert.That(Get<bool>("battleEnded"), Is.True);
        Assert.That(Get<GameObject>("resultPanel").activeSelf, Is.True);
        Assert.That(Get<GameObject>("rewardPanel").activeSelf, Is.False);
        Assert.That(Get<Button>("nextBattleButton").gameObject.activeSelf, Is.False);
        Assert.That(Get<bool>("rewardSelectionOpen"), Is.False);

        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(Get<GameObject>("resultPanel").activeSelf, Is.True);
        Assert.That(Get<bool>("rewardSelectionOpen"), Is.False);

        yield return AwaitRewardSelection(false);

        Assert.That(Get<GameObject>("rewardPanel").activeSelf, Is.True);
        Assert.That(Get<GameObject>("resultPanel").activeSelf, Is.False);
        Assert.That(Get<GameObject>("commandPanelObject").activeSelf, Is.False);
        Assert.That(Get<GameObject>("orbitPanelObject").activeSelf, Is.False);
        Assert.That(TooltipRoot.activeSelf, Is.False);

        foreach (string field in new[]
        {
            "basicAttackButton",
            "skillButton",
            "guardButton",
            "analyzeButton"
        })
        {
            Assert.That(Get<Button>(field).interactable, Is.False, field);
        }

        Color dimColour = Get<GameObject>("rewardPanel").GetComponent<Image>().color;
        Assert.That(dimColour.r, Is.EqualTo(0f).Within(0.001f));
        Assert.That(dimColour.g, Is.EqualTo(0f).Within(0.001f));
        Assert.That(dimColour.b, Is.EqualTo(0f).Within(0.001f));
        Assert.That(dimColour.a, Is.EqualTo(0.72f).Within(0.01f));

        foreach (string field in new[]
        {
            "rewardButton1",
            "rewardButton2",
            "rewardButton3"
        })
        {
            Button card = Get<Button>(field);
            Assert.That(card.interactable, Is.False, field + " during entrance");
        }

        Assert.That(
            Get<Button>("rewardButton3")
                .GetComponent<RectTransform>()
                .anchoredPosition.y,
            Is.LessThan(-500f),
            "The final staggered card starts below the Canvas"
        );
    }

    [UnityTest]
    public IEnumerator TC_RWD_002_PortraitCardsEnterAndSupportPointerAndFocus()
    {
        Set("enemyHP", 1);
        Click("basicAttackButton");
        yield return AwaitRewardSelection();

        Button[] cards =
        {
            Get<Button>("rewardButton1"),
            Get<Button>("rewardButton2"),
            Get<Button>("rewardButton3")
        };
        Array choices = RewardChoices;

        Assert.That(
            EventSystem.current.currentSelectedGameObject,
            Is.Null,
            "No reward card may glow before pointer or navigation focus"
        );
        yield return new WaitForSecondsRealtime(0.2f);

        for (int index = 0; index < cards.Length; index++)
        {
            RectTransform cardRect = cards[index].GetComponent<RectTransform>();
            Type viewType = Type.GetType("RewardCardView, Assembly-CSharp", true);
            Component view = cards[index].GetComponent(viewType);
            TMP_Text label = Property<TMP_Text>(view, "CardLabel");
            TMP_Text glyph = cards[index]
                .transform
                .Find("RewardPortrait/RewardGlyph")
                .GetComponent<TMP_Text>();
            object choice = choices.GetValue(index);
            string expectedName = Property<string>(choice, "DisplayName");
            string expectedGlyph = Property<string>(choice, "Glyph");
            string exactEffect = Property<string>(choice, "ExactEffectText");

            Assert.That(cardRect.rect.height, Is.GreaterThan(cardRect.rect.width));
            Assert.That(view, Is.Not.Null);
            Assert.That(Property<bool>(view, "HasPortrait"), Is.True);
            Assert.That(cards[index].transform.Find("RewardPortrait"), Is.Not.Null);
            Assert.That(cards[index].transform.Find("RewardPortrait/SingularityRings"), Is.Not.Null);
            Assert.That(cards[index].transform.Find("RewardPortrait/RewardGlyph"), Is.Not.Null);
            Assert.That(glyph.text, Is.EqualTo(expectedGlyph));
            StringAssert.Contains("TIER I", label.text);
            StringAssert.Contains(expectedName, label.text);
            StringAssert.Contains(exactEffect, label.text);
            StringAssert.Contains("STACK 0 → 1", label.text);
            Assert.That(cards[index].interactable, Is.True);
            Assert.That(Property<bool>(view, "IsHighlighted"), Is.False);
            Assert.That(
                cards[index].GetComponent<Outline>().effectColor.a,
                Is.EqualTo(0.45f).Within(0.01f)
            );
            Assert.That(cardRect.localScale.x, Is.EqualTo(1f).Within(0.01f));
        }

        RectTransform firstRect = cards[0].GetComponent<RectTransform>();
        Vector2 restingPosition = firstRect.anchoredPosition;
        Outline border = cards[0].GetComponent<Outline>();
        float restingAlpha = border.effectColor.a;

        PointerEnter(cards[0]);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(firstRect.anchoredPosition.y, Is.GreaterThan(restingPosition.y));
        Assert.That(firstRect.localScale.x, Is.GreaterThan(1f));
        Assert.That(border.effectColor.a, Is.GreaterThan(restingAlpha));

        RectTransform secondRect = cards[1].GetComponent<RectTransform>();
        Vector2 secondRestingPosition = secondRect.anchoredPosition;
        PointerEnter(cards[1]);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(firstRect.anchoredPosition.y, Is.EqualTo(restingPosition.y).Within(0.1f));
        Assert.That(firstRect.localScale.x, Is.EqualTo(1f).Within(0.01f));
        Assert.That(secondRect.anchoredPosition.y, Is.GreaterThan(secondRestingPosition.y));
        Assert.That(
            cards.Count(card =>
                Property<bool>(
                    card.GetComponent(
                        Type.GetType("RewardCardView, Assembly-CSharp", true)
                    ),
                    "IsHighlighted"
                )),
            Is.EqualTo(1),
            "Pointer and focus must never leave two reward cards glowing"
        );

        PointerExit(cards[1]);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(secondRect.anchoredPosition.y, Is.EqualTo(secondRestingPosition.y).Within(0.1f));

        Select(cards[0]);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(firstRect.anchoredPosition.y, Is.GreaterThan(restingPosition.y));
        Select(cards[1]);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(firstRect.anchoredPosition.y, Is.EqualTo(restingPosition.y).Within(0.1f));
        Assert.That(secondRect.anchoredPosition.y, Is.GreaterThan(secondRestingPosition.y));
        Deselect(cards[1]);
    }

    [UnityTest]
    public IEnumerator TC_RWD_003_RapidSelectionAppliesOnceAndAdvancesOneNode()
    {
        Set("enemyHP", 1);
        Click("basicAttackButton");
        yield return AwaitRewardSelection();

        int startingBattle = Get<int>("battleNumber");
        int startingStackTotal =
            Get<int>("runAttackRewardStacks") +
            Get<int>("runMaxHPRewardStacks") +
            Get<int>("runGuardRewardStacks");
        object selectedReward = RewardChoices.GetValue(0);
        string selectedEffect =
            Property<object>(selectedReward, "EffectType").ToString();
        object oldPlan = Plan;

        Click("rewardButton1");
        Click("rewardButton1");

        Assert.That(Get<int>("battleNumber"), Is.EqualTo(startingBattle + 1));
        Assert.That(IsRunMapOpen, Is.True);
        Assert.That(
            Get<int>("runAttackRewardStacks") +
            Get<int>("runMaxHPRewardStacks") +
            Get<int>("runGuardRewardStacks"),
            Is.EqualTo(startingStackTotal + 1)
        );
        Assert.That(Get<bool>("rewardSelectionOpen"), Is.False);
        Assert.That(Get<GameObject>("rewardPanel").activeSelf, Is.False);
        Assert.That(Plan, Is.Null);
        Assert.That(Get<GameObject>("commandPanelObject").activeSelf, Is.False);
        Assert.That(Get<GameObject>("orbitPanelObject").activeSelf, Is.False);

        AdvanceMapUntilBattleStarts();
        Assert.That(Plan, Is.Not.SameAs(oldPlan));
        Assert.That(Get<GameObject>("commandPanelObject").activeSelf, Is.True);
        Assert.That(Get<GameObject>("orbitPanelObject").activeSelf, Is.True);

        Set("enemyHP", 1);
        Click("basicAttackButton");
        yield return AwaitRewardSelection();
        int matchingChoice = FindRewardChoice(selectedEffect);
        Type viewType = Type.GetType("RewardCardView, Assembly-CSharp", true);
        Component view = RewardButton(matchingChoice).GetComponent(viewType);
        TMP_Text label = Property<TMP_Text>(view, "CardLabel");
        StringAssert.Contains("STACK 1 → 2", label.text);
    }

    [Test]
    public void TC_RWD_005_Tier10EligibilityStartsAtDepth1001()
    {
        Type managerType = Type.GetType("RewardManager, Assembly-CSharp", true);
        object rewardManager = Activator.CreateInstance(
            managerType,
            new object[] { 15, 0.1f, 0.2f }
        );
        MethodInfo eligibleMethod = managerType.GetMethod("GetEligibleTiers");
        Array at1000 = (Array)eligibleMethod.Invoke(
            rewardManager,
            new object[] { 1000 }
        );
        Array at1001 = (Array)eligibleMethod.Invoke(
            rewardManager,
            new object[] { 1001 }
        );

        Assert.That(MaximumTier(at1000), Is.EqualTo(9));
        Assert.That(MaximumTier(at1001), Is.EqualTo(10));

        MethodInfo generateMethod = managerType.GetMethod("GenerateChoices");
        bool foundTier10 = false;
        bool foundRollWithoutTier10 = false;

        for (int seed = 1; seed <= 2000; seed++)
        {
            Array choices = (Array)generateMethod.Invoke(
                rewardManager,
                new object[] { 1001, seed }
            );
            bool containsTier10 = MaximumTier(choices) == 10;
            foundTier10 |= containsTier10;
            foundRollWithoutTier10 |= !containsTier10;

            if (foundTier10 && foundRollWithoutTier10)
            {
                break;
            }
        }

        Assert.That(foundTier10, Is.True, "Tier 10 harus mungkin muncul pada depth 1001.");
        Assert.That(
            foundRollWithoutTier10,
            Is.True,
            "Tier 10 eligible tetapi tidak boleh dijamin muncul."
        );
    }

    [Test]
    public void RewardGenerationIsSeededDistinctAndDoesNotRerollPresentation()
    {
        Type managerType = Type.GetType("RewardManager, Assembly-CSharp", true);
        object rewardManager = Activator.CreateInstance(
            managerType,
            new object[] { 15, 0.1f, 0.2f }
        );
        MethodInfo generateMethod = managerType.GetMethod("GenerateChoices");
        Array first = (Array)generateMethod.Invoke(
            rewardManager,
            new object[] { 1001, 8675309 }
        );
        Array second = (Array)generateMethod.Invoke(
            rewardManager,
            new object[] { 1001, 8675309 }
        );

        Assert.That(first.Length, Is.EqualTo(3));

        for (int index = 0; index < first.Length; index++)
        {
            Assert.That(
                Property<string>(first.GetValue(index), "Code"),
                Is.EqualTo(Property<string>(second.GetValue(index), "Code"))
            );
            Assert.That(
                Property<int>(first.GetValue(index), "Tier"),
                Is.EqualTo(Property<int>(second.GetValue(index), "Tier"))
            );

            for (int other = index + 1; other < first.Length; other++)
            {
                Assert.That(
                    Property<string>(first.GetValue(index), "Code"),
                    Is.Not.EqualTo(Property<string>(first.GetValue(other), "Code"))
                );
            }
        }

        Set("enemyHP", 1);
        Click("basicAttackButton");
        Call("OpenRewardSelection");
        Array snapshot = RewardChoices;
        Call("ConfigureRewardButtonText");
        Assert.That(RewardChoices, Is.SameAs(snapshot));
    }

    [UnityTest]
    public IEnumerator Regression_DefeatRetryAndFinalVictory()
    {
        Mana(8);
        Click("analyzeButton");
        Set("playerHP", 1);
        Set("enemyHP", 1000);
        Click("basicAttackButton");
        yield return AwaitEnemy();
        Assert.That(Get<TMP_Text>("resultTitleText").text, Is.EqualTo("DEFEAT"));
        Assert.That(Get<TMP_Text>("enemyIntentText").gameObject.activeSelf, Is.False);
        Set("runMaxHPBonus", 15);
        Click("retryButton");
        Assert.That(Get<int>("playerHP"), Is.EqualTo(100));
        Assert.That(Get<int>("runMaxHPBonus"), Is.Zero);
        Assert.That(Get<int>("playerMana"), Is.EqualTo(5));
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.False);
        Assert.That(OrbitCount, Is.Zero);
        Assert.That(IsRunMapOpen, Is.True);
        AdvanceMapUntilBattleStarts();

        int safety = 0;

        while (!Get<bool>("runCompleted"))
        {
            Assert.That(safety++, Is.LessThan(20), "Standard Run tidak selesai.");
            Set("enemyHP", 1);
            Click("basicAttackButton");

            if (Get<bool>("runCompleted"))
            {
                break;
            }

            yield return AwaitRewardSelection();
            Click("rewardButton2");
            AdvanceMapUntilBattleStarts();
        }

        yield return new WaitForSecondsRealtime(1f);
        Assert.That(Get<TMP_Text>("resultTitleText").text, Is.EqualTo("RUN COMPLETE"));
        Assert.That(Get<bool>("runCompleted"), Is.True);
        Assert.That(Get<bool>("rewardSelectionOpen"), Is.False);
        Assert.That(Get<GameObject>("rewardPanel").activeSelf, Is.False);
        Assert.That(Get<Button>("nextBattleButton").gameObject.activeSelf, Is.False);
        int mana = Get<int>("playerMana");
        Call("UseAnalyze");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(mana));
        Click("retryButton");
        Assert.That(Get<int>("battleNumber"), Is.EqualTo(1));
        Assert.That(Get<int>("runMaxHPBonus"), Is.Zero);
        Assert.That(Get<int>("playerHP"), Is.EqualTo(100));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(5));
        Assert.That(Get<int>("runAttackRewardStacks"), Is.Zero);
        Assert.That(Get<int>("runMaxHPRewardStacks"), Is.Zero);
        Assert.That(Get<int>("runGuardRewardStacks"), Is.Zero);
        Assert.That(IsRunMapOpen, Is.True);
    }

    [Test]
    public void TC_RES_001_OnlyNewRunInitializesResources()
    {
        Assert.That(Get<int>("playerHP"), Is.EqualTo(100));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(5));

        Set("playerHP", 43);
        Mana(8);
        Call("StartBattle");
        Assert.That(Get<int>("playerHP"), Is.EqualTo(43));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(8));
        StringAssert.Contains("43/100", Get<TMP_Text>("playerHPText").text);
        StringAssert.Contains("8/10", Get<TMP_Text>("playerManaText").text);
    }

    [UnityTest]
    public IEnumerator TC_RES_002_AllRewardsCarryCurrentHPAndMana()
    {
        string[] effects = { "AttackPercent", "MaxHPFlat", "GuardStrengthPercent" };
        int[] manaValues = { 0, 7, 10 };

        for (int scenario = 0; scenario < effects.Length; scenario++)
        {
            Call("RestartRun");
            AdvanceMapUntilBattleStarts();
            Set("playerHP", 43);
            Mana(manaValues[scenario]);
            object oldPlan = Plan;
            Set("enemyHP", 1);
            Click("basicAttackButton");
            yield return AwaitRewardSelection();
            Assert.That(Get<int>("playerHP"), Is.EqualTo(43));
            Assert.That(Get<int>("playerMana"), Is.EqualTo(manaValues[scenario]));

            int choiceIndex = FindRewardChoice(effects[scenario]);
            object reward = RewardChoices.GetValue(choiceIndex);
            int expectedMaxHP = 100 + Property<int>(reward, "FlatValue");
            int expectedPlayerHP = effects[scenario] == "MaxHPFlat"
                ? 43 + Property<int>(reward, "FlatValue")
                : 43;
            RewardButton(choiceIndex).onClick.Invoke();
            RewardButton(choiceIndex).onClick.Invoke();

            Assert.That(Get<int>("battleNumber"), Is.EqualTo(2));
            Assert.That(Get<int>("playerHP"), Is.EqualTo(expectedPlayerHP), effects[scenario]);
            Assert.That(Get<int>("playerMana"), Is.EqualTo(manaValues[scenario]));
            Assert.That((int)Call("GetCurrentPlayerMaxHP"), Is.EqualTo(expectedMaxHP));
            Assert.That(IsRunMapOpen, Is.True);
            AdvanceMapUntilBattleStarts();
            Assert.That(Plan, Is.Not.SameAs(oldPlan));
            Assert.That(OrbitCount, Is.Zero);
            Assert.That(Get<bool>("analyzeUsedThisNode"), Is.False);
            Assert.That(Get<TMP_Text>("enemyIntentText").gameObject.activeSelf, Is.False);
            Assert.That(Get<Button>("analyzeButton").interactable,
                Is.EqualTo(manaValues[scenario] >= 8));
            Assert.That(Get<Button>("skillButton").interactable,
                Is.EqualTo(manaValues[scenario] >= 2));
            StringAssert.Contains($"{expectedPlayerHP}/{expectedMaxHP}", Get<TMP_Text>("playerHPText").text);
            StringAssert.Contains($"{manaValues[scenario]}/10", Get<TMP_Text>("playerManaText").text);

            // There is no delayed heal, Mana refill, or old enemy turn after the modal.
            yield return new WaitForSeconds(3f);
            Assert.That(Get<int>("playerHP"), Is.EqualTo(expectedPlayerHP));
            Assert.That(Get<int>("playerMana"), Is.EqualTo(manaValues[scenario]));
            Assert.That(Get<bool>("isPlayerTurn"), Is.True);
        }
    }

    [UnityTest]
    public IEnumerator TC_RES_003_VitalCoreAddsMaxAndCurrentHPAcrossTwoNodes()
    {
        int expectedMaxHP = 100;
        Mana(9);

        for (int battle = 1; battle <= 2; battle++)
        {
            Set("enemyHP", 1);
            Click("basicAttackButton");
            yield return AwaitRewardSelection();
            int choiceIndex = FindRewardChoice("MaxHPFlat");
            expectedMaxHP += Property<int>(RewardChoices.GetValue(choiceIndex), "FlatValue");
            RewardButton(choiceIndex).onClick.Invoke();
            Assert.That(
                Get<int>("playerHP"),
                Is.EqualTo(expectedMaxHP),
                "Vital Core adds the same snapshot value to Max HP and current HP"
            );
            Assert.That(Get<int>("playerMana"), Is.EqualTo(9));
            Assert.That((int)Call("GetCurrentPlayerMaxHP"), Is.EqualTo(expectedMaxHP));
            Assert.That(Get<int>("runMaxHPRewardStacks"), Is.EqualTo(battle));
            Assert.That(Get<int>("playerMaxHP"), Is.EqualTo(100), "Locked Base Stats stay unchanged");
            AdvanceMapUntilBattleStarts();
        }

        Call("RestartRun");
        Assert.That(Get<int>("playerHP"), Is.EqualTo(100));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(5));
        Assert.That(Get<int>("runMaxHPBonus"), Is.Zero);
        Assert.That(Get<int>("runMaxHPRewardStacks"), Is.Zero);
    }

    [Test]
    public void TC_RUN_001_RunModifierCollectionAggregatesAndResets()
    {
        Assert.That((bool)Call("PrepareRewardChoices"), Is.True);
        object collection = Get<object>("runModifiers");
        Type collectionType = collection.GetType();
        Array choices = RewardChoices;
        int expectedFlat = 0;
        float expectedAttackPercent = 0f;
        float expectedGuardPercent = 0f;
        object maxHPReward = null;

        for (int index = 0; index < choices.Length; index++)
        {
            object reward = choices.GetValue(index);
            string effect = Property<object>(reward, "EffectType").ToString();
            Call("ApplyReward", reward);

            if (effect == "MaxHPFlat")
            {
                expectedFlat += Property<int>(reward, "FlatValue");
                maxHPReward = reward;
            }
            else if (effect == "AttackPercent")
            {
                expectedAttackPercent += Property<float>(reward, "PercentValue");
            }
            else if (effect == "GuardStrengthPercent")
            {
                expectedGuardPercent += Property<float>(reward, "PercentValue");
            }
        }

        Assert.That(Property<int>(collection, "Count"), Is.EqualTo(3));
        Array snapshot = (Array)collectionType
            .GetMethod("CreateSnapshot")
            .Invoke(collection, null);
        Assert.That(snapshot.Length, Is.EqualTo(3));

        for (int index = 0; index < snapshot.Length; index++)
        {
            object modifier = snapshot.GetValue(index);
            Assert.That(Property<object>(modifier, "Scope").ToString(), Is.EqualTo("ActiveRun"));
            Assert.That(Property<int>(modifier, "StackCount"), Is.EqualTo(1));
        }

        Assert.That(Get<int>("runMaxHPBonus"), Is.EqualTo(expectedFlat));
        Assert.That(Get<float>("runAttackBonusPercent"), Is.EqualTo(expectedAttackPercent));
        Assert.That(Get<float>("runGuardStrengthBonusPercent"), Is.EqualTo(expectedGuardPercent));
        Assert.That(Get<int>("playerHP"), Is.EqualTo(100 + expectedFlat));

        Call("ApplyReward", maxHPReward);
        Assert.That(Property<int>(collection, "Count"), Is.EqualTo(3));
        Assert.That(Get<int>("runMaxHPBonus"), Is.EqualTo(expectedFlat * 2));
        Assert.That(Get<int>("runMaxHPRewardStacks"), Is.EqualTo(2));
        Assert.That(Get<int>("playerHP"), Is.EqualTo(100 + expectedFlat * 2));

        Call("ResetTemporaryRunStats");
        Assert.That(Property<int>(collection, "Count"), Is.Zero);
        Assert.That(
            ((Array)collectionType.GetMethod("CreateSnapshot").Invoke(collection, null)).Length,
            Is.Zero
        );
        Assert.That(Get<int>("runMaxHPBonus"), Is.Zero);
        Assert.That(Get<int>("runAttackRewardStacks"), Is.Zero);
        Assert.That(Get<int>("runMaxHPRewardStacks"), Is.Zero);
        Assert.That(Get<int>("runGuardRewardStacks"), Is.Zero);
    }

    [Test]
    public void TC_RUN_002_RunStatControllerCalculatesEffectiveStats()
    {
        Assert.That((bool)Call("PrepareRewardChoices"), Is.True);
        object stats = Get<object>("runStatController");
        Array choices = RewardChoices;
        int expectedMaxHPBonus = 0;
        float expectedAttackBonus = 0f;
        float expectedGuardBonus = 0f;

        Assert.That(Property<int>(stats, "BaseMaxHP"), Is.EqualTo(100));
        Assert.That(Property<int>(stats, "BaseMaxMana"), Is.EqualTo(10));
        Assert.That(Property<int>(stats, "BaseBasicAttackDamage"), Is.EqualTo(10));
        Assert.That(Property<int>(stats, "EffectiveMaxHP"), Is.EqualTo(100));
        Assert.That(Property<int>(stats, "EffectiveMaxMana"), Is.EqualTo(10));
        Assert.That(Property<int>(stats, "EffectiveBasicAttackDamage"), Is.EqualTo(10));
        Assert.That(Property<int>(stats, "AnalyzeManaCost"), Is.EqualTo(8));

        for (int index = 0; index < choices.Length; index++)
        {
            object reward = choices.GetValue(index);
            string effect = Property<object>(reward, "EffectType").ToString();
            Call("ApplyReward", reward);

            if (effect == "MaxHPFlat")
            {
                expectedMaxHPBonus += Property<int>(reward, "FlatValue");
            }
            else if (effect == "AttackPercent")
            {
                expectedAttackBonus += Property<float>(reward, "PercentValue");
            }
            else if (effect == "GuardStrengthPercent")
            {
                expectedGuardBonus += Property<float>(reward, "PercentValue");
            }
        }

        Assert.That(
            Property<int>(stats, "EffectiveMaxHP"),
            Is.EqualTo(100 + expectedMaxHPBonus)
        );
        Assert.That(
            Property<int>(stats, "EffectiveMaxMana"),
            Is.EqualTo(10)
        );
        Assert.That(
            Property<int>(stats, "EffectiveBasicAttackDamage"),
            Is.EqualTo(Mathf.RoundToInt(10f * (1f + expectedAttackBonus)))
        );
        Assert.That(
            Property<float>(stats, "EffectiveGuardDamageReduction"),
            Is.EqualTo(Mathf.Clamp(0.5f * (1f + expectedGuardBonus), 0f, 0.9f))
                .Within(0.0001f)
        );

        Assert.That(Get<int>("playerMaxHP"), Is.EqualTo(100));
        Assert.That(Get<int>("playerMaxMana"), Is.EqualTo(10));
        Assert.That(Get<int>("basicAttackDamage"), Is.EqualTo(10));

        Call("ResetTemporaryRunStats");
        Assert.That(Property<int>(stats, "EffectiveMaxHP"), Is.EqualTo(100));
        Assert.That(Property<int>(stats, "EffectiveBasicAttackDamage"), Is.EqualTo(10));
        Assert.That(
            Property<float>(stats, "EffectiveGuardDamageReduction"),
            Is.EqualTo(0.5f).Within(0.0001f)
        );
    }

    [Test]
    public void TC_RUN_003_RunStatControllerCapsGuardAtNinetyPercent()
    {
        Assert.That((bool)Call("PrepareRewardChoices"), Is.True);
        object stats = Get<object>("runStatController");
        int guardChoice = FindRewardChoice("GuardStrengthPercent");
        object guardReward = RewardChoices.GetValue(guardChoice);

        for (int stack = 0; stack < 10; stack++)
        {
            Call("ApplyReward", guardReward);
        }

        Assert.That(
            Property<float>(stats, "EffectiveGuardDamageReduction"),
            Is.EqualTo(0.9f).Within(0.0001f)
        );
        Assert.That(
            (float)Call("GetCurrentGuardDamageReduction"),
            Is.EqualTo(0.9f).Within(0.0001f)
        );
    }

    [Test]
    public void TC_RUN_004_RunManagerOwnsLifecycleResourcesAndModifiers()
    {
        object activeRun = Get<object>("runManager");
        Type runType = activeRun.GetType();
        object modifierCollection = Get<object>("runModifiers");

        Assert.That(activeRun, Is.Not.Null);
        Assert.That(
            Property<object>(activeRun, "Modifiers"),
            Is.SameAs(modifierCollection)
        );
        Assert.That(Property<int>(activeRun, "Depth"), Is.EqualTo(1));
        Assert.That(Property<int>(activeRun, "RunSeed"), Is.Not.Zero);
        Assert.That(Property<int>(activeRun, "CurrentHP"), Is.EqualTo(100));
        Assert.That(Property<int>(activeRun, "CurrentMana"), Is.EqualTo(5));
        Assert.That(Property<int>(activeRun, "TotalEmbers"), Is.Zero);
        Assert.That(Property<bool>(activeRun, "IsActive"), Is.True);
        Assert.That(Property<bool>(activeRun, "IsCompleted"), Is.False);

        Assert.That(
            (bool)runType.GetMethod("TrySpendMana").Invoke(
                activeRun,
                new object[] { 5 }
            ),
            Is.True
        );
        Assert.That(Property<int>(activeRun, "CurrentMana"), Is.Zero);
        Assert.That(
            (int)runType.GetMethod("RestoreMana").Invoke(
                activeRun,
                new object[] { 99, 10 }
            ),
            Is.EqualTo(10)
        );
        Assert.That(Property<int>(activeRun, "CurrentMana"), Is.EqualTo(10));
        Assert.That(
            (int)runType.GetMethod("ApplyDamage").Invoke(
                activeRun,
                new object[] { 30 }
            ),
            Is.EqualTo(30)
        );
        Assert.That(Property<int>(activeRun, "CurrentHP"), Is.EqualTo(70));
        Assert.That(
            (int)runType.GetMethod("RestoreHP").Invoke(
                activeRun,
                new object[] { 99, 100 }
            ),
            Is.EqualTo(30)
        );
        Assert.That(Property<int>(activeRun, "CurrentHP"), Is.EqualTo(100));

        runType.GetMethod("AddEmbers").Invoke(activeRun, new object[] { 12 });
        Assert.That(Property<int>(activeRun, "TotalEmbers"), Is.EqualTo(12));
        Assert.That(
            (bool)runType.GetMethod("TryAdvanceDepth").Invoke(activeRun, null),
            Is.True
        );
        Assert.That(Property<int>(activeRun, "Depth"), Is.EqualTo(2));

        runType.GetMethod("CompleteRun").Invoke(activeRun, null);
        Assert.That(Property<bool>(activeRun, "IsActive"), Is.False);
        Assert.That(Property<bool>(activeRun, "IsCompleted"), Is.True);
        Assert.That(
            (bool)runType.GetMethod("TryAdvanceDepth").Invoke(activeRun, null),
            Is.False
        );

        Call("SyncLegacyRunStateFields");
        Assert.That(Get<int>("playerHP"), Is.EqualTo(100));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(10));
        Assert.That(Get<int>("battleNumber"), Is.EqualTo(2));
        Assert.That(Get<int>("totalEmbers"), Is.EqualTo(12));
        Assert.That(Get<bool>("runCompleted"), Is.True);
    }

    [Test]
    public void TC_RUN_005_RestartCreatesOneCleanActiveRun()
    {
        Assert.That((bool)Call("PrepareRewardChoices"), Is.True);
        object reward = RewardChoices.GetValue(0);
        Call("ApplyReward", reward);

        object oldRun = Get<object>("runManager");
        Type runType = oldRun.GetType();
        runType.GetMethod("AddEmbers").Invoke(oldRun, new object[] { 25 });
        runType.GetMethod("TryAdvanceDepth").Invoke(oldRun, null);
        runType.GetMethod("CompleteRun").Invoke(oldRun, null);
        Call("SyncLegacyRunStateFields");

        Call("RestartRun");

        object restartedRun = Get<object>("runManager");
        object modifiers = Property<object>(restartedRun, "Modifiers");
        Assert.That(restartedRun, Is.SameAs(oldRun));
        Assert.That(modifiers, Is.SameAs(Get<object>("runModifiers")));
        Assert.That(Property<int>(modifiers, "Count"), Is.Zero);
        Assert.That(Property<int>(restartedRun, "Depth"), Is.EqualTo(1));
        Assert.That(Property<int>(restartedRun, "RunSeed"), Is.Not.Zero);
        Assert.That(Property<int>(restartedRun, "CurrentHP"), Is.EqualTo(100));
        Assert.That(Property<int>(restartedRun, "CurrentMana"), Is.EqualTo(5));
        Assert.That(Property<int>(restartedRun, "TotalEmbers"), Is.Zero);
        Assert.That(Property<bool>(restartedRun, "IsActive"), Is.True);
        Assert.That(Property<bool>(restartedRun, "IsCompleted"), Is.False);
        Assert.That(Get<int>("battleNumber"), Is.EqualTo(1));
        Assert.That(Get<int>("totalEmbers"), Is.Zero);
        Assert.That(Get<bool>("runCompleted"), Is.False);
        Assert.That(Get<int>("runAttackRewardStacks"), Is.Zero);
        Assert.That(Get<int>("runMaxHPRewardStacks"), Is.Zero);
        Assert.That(Get<int>("runGuardRewardStacks"), Is.Zero);
    }

    [Test]
    public void TC_RUN_006_SeededRunStateRoundTripsDeterministically()
    {
        object firstModifiers = NewRuntimeObject("RunModifierCollection");
        object firstRun = NewRuntimeObject("RunManager", firstModifiers);
        InvokeRuntime(firstRun, "StartNewRun", 730041, 100, 5, 100, 10);

        object tier = NewRuntimeObject(
            "RewardTierConfiguration",
            1,
            1,
            1,
            1f,
            "Seeded state test"
        );
        object maxHPReward = NewRuntimeObject(
            "RewardDefinition",
            "TEST_MAX_HP",
            "Test Vital Core",
            "",
            "",
            Enum.Parse(RuntimeType("RewardEffectType"), "MaxHPFlat"),
            tier,
            15,
            0f,
            "Max HP +15"
        );
        object attackReward = NewRuntimeObject(
            "RewardDefinition",
            "TEST_ATTACK",
            "Test Attack Surge",
            "",
            "",
            Enum.Parse(RuntimeType("RewardEffectType"), "AttackPercent"),
            tier,
            0,
            0.10f,
            "Basic Attack +10%"
        );

        InvokeRuntime(firstModifiers, "AddOrStack", maxHPReward);
        InvokeRuntime(firstModifiers, "AddOrStack", attackReward);
        InvokeRuntime(firstRun, "SetResources", 113, 7, 115, 10);
        Assert.That(
            (bool)InvokeRuntime(firstRun, "TryAdvanceDepth"),
            Is.True
        );
        InvokeRuntime(firstRun, "AddEmbers", 18);

        object captured = InvokeRuntime(firstRun, "CaptureState");
        string serialized = JsonUtility.ToJson(captured);
        object decoded = JsonUtility.FromJson(
            serialized,
            RuntimeType("SeededRunState")
        );

        object restoredModifiers = NewRuntimeObject("RunModifierCollection");
        object restoredRun = NewRuntimeObject("RunManager", restoredModifiers);
        InvokeRuntime(restoredRun, "RestoreState", decoded, 115, 10);

        Assert.That(
            JsonUtility.ToJson(InvokeRuntime(restoredRun, "CaptureState")),
            Is.EqualTo(serialized),
            "A serialized state must restore to the same deterministic state"
        );

        object reversedModifiers = NewRuntimeObject("RunModifierCollection");
        object reversedRun = NewRuntimeObject("RunManager", reversedModifiers);
        InvokeRuntime(reversedRun, "StartNewRun", 730041, 100, 5, 100, 10);
        InvokeRuntime(reversedModifiers, "AddOrStack", attackReward);
        InvokeRuntime(reversedModifiers, "AddOrStack", maxHPReward);
        InvokeRuntime(reversedRun, "SetResources", 113, 7, 115, 10);
        Assert.That(
            (bool)InvokeRuntime(reversedRun, "TryAdvanceDepth"),
            Is.True
        );
        InvokeRuntime(reversedRun, "AddEmbers", 18);

        Assert.That(
            JsonUtility.ToJson(InvokeRuntime(reversedRun, "CaptureState")),
            Is.EqualTo(serialized),
            "Modifier insertion order must not change a seeded state"
        );

        Assert.That(RuntimeProperty<int>(restoredRun, "RunSeed"), Is.EqualTo(730041));
        Assert.That(RuntimeProperty<int>(restoredRun, "Depth"), Is.EqualTo(2));
        Assert.That(RuntimeProperty<int>(restoredRun, "CurrentHP"), Is.EqualTo(113));
        Assert.That(RuntimeProperty<int>(restoredRun, "CurrentMana"), Is.EqualTo(7));
        Assert.That(RuntimeProperty<int>(restoredRun, "TotalEmbers"), Is.EqualTo(18));
        Assert.That(RuntimeProperty<bool>(restoredRun, "IsActive"), Is.True);
        Assert.That(RuntimeProperty<bool>(restoredRun, "IsCompleted"), Is.False);
        object restoredModifierCollection = RuntimeProperty<object>(
            restoredRun,
            "Modifiers"
        );
        Assert.That(
            RuntimeProperty<int>(restoredModifierCollection, "Count"),
            Is.EqualTo(2)
        );

        // The captured DTO owns copies, so later mutation of serialized data
        // cannot silently alter the live run or its modifier collection.
        Array capturedModifiers = RuntimeField<Array>(captured, "Modifiers");
        SetRuntimeField(capturedModifiers.GetValue(0), "StackCount", 99);
        Array liveSnapshot = (Array)InvokeRuntime(
            firstModifiers,
            "CreateStateSnapshot"
        );
        Assert.That(
            RuntimeField<int>(liveSnapshot.GetValue(0), "StackCount"),
            Is.Not.EqualTo(99)
        );

        object differentSeed = InvokeRuntime(captured, "Clone");
        Array differentModifiers = RuntimeField<Array>(differentSeed, "Modifiers");
        SetRuntimeField(differentModifiers.GetValue(0), "StackCount", 1);
        SetRuntimeField(
            differentSeed,
            "RunSeed",
            RuntimeField<int>(differentSeed, "RunSeed") + 1
        );
        Assert.That(
            JsonUtility.ToJson(differentSeed),
            Is.Not.EqualTo(serialized),
            "Different seeds must produce different state identities"
        );
    }

    [Test]
    public void TC_RUN_007_SeededRunStateRejectsInvalidDataWithoutMutation()
    {
        object run = NewRuntimeObject(
            "RunManager",
            NewRuntimeObject("RunModifierCollection")
        );
        InvokeRuntime(run, "StartNewRun", 730042, 100, 5, 100, 10);
        object before = InvokeRuntime(run, "CaptureState");

        object invalid = InvokeRuntime(before, "Clone");
        SetRuntimeField(invalid, "RunSeed", 0);

        Assert.Throws<InvalidOperationException>(
            () => InvokeRuntime(run, "RestoreState", invalid, 100, 10)
        );
        Assert.That(
            JsonUtility.ToJson(InvokeRuntime(run, "CaptureState")),
            Is.EqualTo(JsonUtility.ToJson(before)),
            "Rejected checkpoint data must not mutate the current run"
        );
    }

    [Test]
    public void TC_NODE_001_StandardRunNodeMapFollowsGddShape()
    {
        object map = InvokeRuntimeStatic(
            "StandardRunNodeMap",
            "Generate",
            123,
            10
        );

        InvokeRuntime(map, "Validate");

        Assert.That(RuntimeProperty<int>(map, "Count"), Is.EqualTo(10));

        Array nodes = RuntimeField<Array>(map, "Nodes");
        Assert.That(nodes.Length, Is.EqualTo(10));

        int normalBattles = 0;
        int randomEvents = 0;
        int eliteBattles = 0;
        int rests = 0;
        int merchants = 0;
        int bosses = 0;

        for (int index = 0; index < nodes.Length; index++)
        {
            object node = nodes.GetValue(index);
            Assert.That(node, Is.Not.Null);
            Assert.That(
                RuntimeField<int>(node, "Depth"),
                Is.EqualTo(index + 1)
            );
            Assert.That(
                RuntimeField<string>(node, "Code"),
                Is.Not.Empty
            );
            Assert.That(
                RuntimeField<bool>(node, "IsFinal"),
                Is.EqualTo(index == nodes.Length - 1)
            );

            switch (RuntimeField<object>(node, "Type").ToString())
            {
                case "NormalBattle":
                    normalBattles++;
                    break;
                case "RandomEvent":
                    randomEvents++;
                    break;
                case "EliteBattle":
                    eliteBattles++;
                    break;
                case "Rest":
                    rests++;
                    break;
                case "Merchant":
                    merchants++;
                    break;
                case "Boss":
                    bosses++;
                    break;
                default:
                    Assert.Fail("Standard Run menghasilkan tipe node asing.");
                    break;
            }
        }

        Assert.That(
            RuntimeField<object>(nodes.GetValue(0), "Type").ToString(),
            Is.EqualTo("NormalBattle")
        );
        Assert.That(
            RuntimeField<object>(nodes.GetValue(nodes.Length - 1), "Type")
                .ToString(),
            Is.EqualTo("Boss")
        );
        Assert.That(normalBattles, Is.InRange(3, 5));
        Assert.That(randomEvents, Is.InRange(2, 3));
        Assert.That(eliteBattles, Is.EqualTo(1));
        Assert.That(rests, Is.GreaterThanOrEqualTo(1));
        Assert.That(merchants, Is.InRange(0, 1));
        Assert.That(bosses, Is.EqualTo(1));
    }

    [Test]
    public void TC_NODE_002_StandardRunNodeMapIsDeterministic()
    {
        object first = InvokeRuntimeStatic(
            "StandardRunNodeMap",
            "Generate",
            123,
            10
        );
        object repeat = InvokeRuntimeStatic(
            "StandardRunNodeMap",
            "Generate",
            123,
            10
        );
        object alternate = InvokeRuntimeStatic(
            "StandardRunNodeMap",
            "Generate",
            321,
            10
        );

        Assert.That(
            NodeRouteKey(first),
            Is.EqualTo(NodeRouteKey(repeat)),
            "Seed yang sama harus menghasilkan rute yang sama"
        );
        Assert.That(
            NodeRouteKey(first),
            Is.Not.EqualTo(NodeRouteKey(alternate)),
            "Seed berbeda harus mengubah rute pada generator ini"
        );
    }

    [Test]
    public void TC_NODE_003_StandardRunNodeMapSupportsEightToTenNodes()
    {
        for (int nodeCount = 8; nodeCount <= 10; nodeCount++)
        {
            object map = InvokeRuntimeStatic(
                "StandardRunNodeMap",
                "Generate",
                700 + nodeCount,
                nodeCount
            );

            InvokeRuntime(map, "Validate");
            Assert.That(
                RuntimeProperty<int>(map, "Count"),
                Is.EqualTo(nodeCount)
            );

            object first = InvokeRuntime(
                map,
                "GetNodeAtDepth",
                1
            );
            object final = InvokeRuntime(
                map,
                "GetNodeAtDepth",
                nodeCount
            );

            Assert.That(
                RuntimeField<object>(first, "Type").ToString(),
                Is.EqualTo("NormalBattle")
            );
            Assert.That(
                RuntimeField<object>(final, "Type").ToString(),
                Is.EqualTo("Boss")
            );

            object missing = null;
            bool found = (bool)InvokeRuntime(
                map,
                "TryGetNodeAtDepth",
                0,
                missing
            );
            Assert.That(found, Is.False);
        }
    }

    [Test]
    public void TC_NODE_004_StandardRunNodeMapRejectsInvalidInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InvokeRuntimeStatic(
                "StandardRunNodeMap",
                "Generate",
                0,
                10
            )
        );
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InvokeRuntimeStatic(
                "StandardRunNodeMap",
                "Generate",
                123,
                7
            )
        );
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InvokeRuntimeStatic(
                "StandardRunNodeMap",
                "Generate",
                123,
                11
            )
        );

        object map = InvokeRuntimeStatic(
            "StandardRunNodeMap",
            "Generate",
            456,
            10
        );

        Assert.Throws<ArgumentOutOfRangeException>(
            () => InvokeRuntime(map, "GetNodeAtDepth", 0)
        );
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InvokeRuntime(map, "GetNodeAtDepth", 11)
        );
    }

    [Test]
    public void TC_NODE_005_StandardRunNodeMapJsonRoundTrips()
    {
        object generated = InvokeRuntimeStatic(
            "StandardRunNodeMap",
            "Generate",
            90210,
            10
        );
        string serialized = JsonUtility.ToJson(generated);
        object decoded = JsonUtility.FromJson(
            serialized,
            RuntimeType("StandardRunNodeMap")
        );

        InvokeRuntime(decoded, "Validate");

        Assert.That(
            JsonUtility.ToJson(decoded),
            Is.EqualTo(serialized),
            "Rute yang disimpan harus dapat dipulihkan tanpa perubahan"
        );
        Assert.That(
            NodeRouteKey(decoded),
            Is.EqualTo(NodeRouteKey(generated))
        );
    }

    [Test]
    public void TC_NODE_006_RunManagerOwnsAndGuardsNodeProgression()
    {
        object run = NewRuntimeObject(
            "RunManager",
            NewRuntimeObject("RunModifierCollection")
        );

        InvokeRuntime(run, "StartNewRun", 246810, 100, 6, 100, 10);

        object map = RuntimeProperty<object>(run, "NodeMap");
        object firstNode = RuntimeProperty<object>(run, "CurrentNode");
        object firstType = RuntimeField<object>(firstNode, "Type");
        string firstCode = RuntimeField<string>(firstNode, "Code");

        Assert.That(map, Is.Not.Null);
        Assert.That(RuntimeField<int>(map, "RunSeed"), Is.EqualTo(246810));
        Assert.That(
            RuntimeProperty<int>(map, "Count"),
            Is.EqualTo(10)
        );
        Assert.That(RuntimeProperty<int>(run, "Depth"), Is.EqualTo(1));
        Assert.That(RuntimeField<int>(firstNode, "Depth"), Is.EqualTo(1));
        Assert.That(RuntimeProperty<bool>(run, "HasNextNode"), Is.True);

        object wrongType = Enum.Parse(
            RuntimeType("StandardRunNodeType"),
            "Rest"
        );

        Assert.That(
            (bool)InvokeRuntime(
                run,
                "TryResolveCurrentNode",
                wrongType,
                firstCode
            ),
            Is.False,
            "Handler untuk tipe node lain tidak boleh memajukan run"
        );
        Assert.That(
            (bool)InvokeRuntime(
                run,
                "TryResolveCurrentNode",
                firstType,
                "STALE_NODE_CODE"
            ),
            Is.False,
            "Callback dengan node code lama atau asing harus ditolak"
        );
        Assert.That(RuntimeProperty<int>(run, "Depth"), Is.EqualTo(1));

        Assert.That(
            (bool)InvokeRuntime(
                run,
                "TryResolveCurrentNode",
                firstType,
                firstCode
            ),
            Is.True
        );
        Assert.That(RuntimeProperty<int>(run, "Depth"), Is.EqualTo(2));
        Assert.That(
            (bool)InvokeRuntime(
                run,
                "TryResolveCurrentNode",
                firstType,
                firstCode
            ),
            Is.False,
            "Callback node pertama tidak boleh menyelesaikan node kedua"
        );

        int nodeCount = RuntimeProperty<int>(map, "Count");

        for (int step = 1; step < nodeCount; step++)
        {
            object current = RuntimeProperty<object>(run, "CurrentNode");
            Assert.That(current, Is.Not.Null);
            Assert.That(
                (bool)InvokeRuntime(
                    run,
                    "TryResolveCurrentNode",
                    RuntimeField<object>(current, "Type"),
                    RuntimeField<string>(current, "Code")
                ),
                Is.True
            );
        }

        Assert.That(RuntimeProperty<int>(run, "Depth"), Is.EqualTo(nodeCount));
        Assert.That(RuntimeProperty<bool>(run, "IsActive"), Is.False);
        Assert.That(RuntimeProperty<bool>(run, "IsCompleted"), Is.True);
        Assert.That(RuntimeProperty<bool>(run, "HasNextNode"), Is.False);
        Assert.That(RuntimeProperty<int>(run, "CurrentHP"), Is.EqualTo(100));
        Assert.That(RuntimeProperty<int>(run, "CurrentMana"), Is.EqualTo(6));

        object finalNode = RuntimeProperty<object>(run, "CurrentNode");
        Assert.That(RuntimeField<bool>(finalNode, "IsFinal"), Is.True);
        Assert.That(
            RuntimeField<object>(finalNode, "Type").ToString(),
            Is.EqualTo("Boss")
        );
        Assert.That(
            (bool)InvokeRuntime(
                run,
                "TryResolveCurrentNode",
                RuntimeField<object>(finalNode, "Type"),
                RuntimeField<string>(finalNode, "Code")
            ),
            Is.False,
            "Boss yang sudah selesai tidak boleh dikomit dua kali"
        );
    }

    [Test]
    public void TC_NODE_007_SeededStateRestoresTheSameNodeMap()
    {
        object source = NewRuntimeObject(
            "RunManager",
            NewRuntimeObject("RunModifierCollection")
        );

        InvokeRuntime(
            source,
            "StartNewRunWithNodeCount",
            86420,
            8,
            100,
            4,
            100,
            10
        );

        for (int step = 0; step < 2; step++)
        {
            object node = RuntimeProperty<object>(source, "CurrentNode");
            Assert.That(
                (bool)InvokeRuntime(
                    source,
                    "TryResolveCurrentNode",
                    RuntimeField<object>(node, "Type"),
                    RuntimeField<string>(node, "Code")
                ),
                Is.True
            );
        }

        object captured = InvokeRuntime(source, "CaptureState");
        Assert.That(RuntimeField<int>(captured, "NodeCount"), Is.EqualTo(8));

        string serialized = JsonUtility.ToJson(captured);
        object decoded = JsonUtility.FromJson(
            serialized,
            RuntimeType("SeededRunState")
        );
        object restored = NewRuntimeObject(
            "RunManager",
            NewRuntimeObject("RunModifierCollection")
        );

        InvokeRuntime(restored, "RestoreState", decoded, 100, 10);

        object sourceMap = RuntimeProperty<object>(source, "NodeMap");
        object restoredMap = RuntimeProperty<object>(restored, "NodeMap");
        object sourceNode = RuntimeProperty<object>(source, "CurrentNode");
        object restoredNode = RuntimeProperty<object>(restored, "CurrentNode");

        Assert.That(NodeRouteKey(restoredMap), Is.EqualTo(NodeRouteKey(sourceMap)));
        Assert.That(RuntimeProperty<int>(restored, "Depth"), Is.EqualTo(3));
        Assert.That(
            RuntimeField<string>(restoredNode, "Code"),
            Is.EqualTo(RuntimeField<string>(sourceNode, "Code"))
        );
        Assert.That(RuntimeProperty<int>(restored, "CurrentHP"), Is.EqualTo(100));
        Assert.That(RuntimeProperty<int>(restored, "CurrentMana"), Is.EqualTo(4));

        object target = NewRuntimeObject(
            "RunManager",
            NewRuntimeObject("RunModifierCollection")
        );
        InvokeRuntime(target, "StartNewRun", 97531, 90, 3, 100, 10);
        string targetBefore = JsonUtility.ToJson(
            InvokeRuntime(target, "CaptureState")
        );

        object invalid = InvokeRuntime(captured, "Clone");
        SetRuntimeField(invalid, "Depth", 9);

        Assert.Throws<InvalidOperationException>(
            () => InvokeRuntime(target, "RestoreState", invalid, 100, 10)
        );
        Assert.That(
            JsonUtility.ToJson(InvokeRuntime(target, "CaptureState")),
            Is.EqualTo(targetBefore),
            "Checkpoint di luar map tidak boleh memutasi run yang aktif"
        );
    }

    [Test]
    public void TC_MAP_001_NewRunShowsOnlyCurrentRouteNodeAsReachable()
    {
        Call("RestartRun");

        Component mapUI = RunMapUI;
        object activeRun = Get<object>("runManager");
        object nodeMap = Property<object>(activeRun, "NodeMap");
        object currentNode = Property<object>(activeRun, "CurrentNode");
        Button[] buttons = ((IEnumerable)Property<object>(mapUI, "NodeButtons"))
            .Cast<Button>()
            .ToArray();

        Assert.That(mapUI, Is.Not.Null);
        Assert.That(IsRunMapOpen, Is.True);
        Assert.That(buttons.Length, Is.EqualTo(Property<int>(nodeMap, "Count")));
        Assert.That(buttons.Count(button => button.interactable), Is.EqualTo(1));
        Assert.That(buttons[0].interactable, Is.True);
        Assert.That(Property<int>(mapUI, "CurrentDepth"), Is.EqualTo(1));
        Assert.That(
            Property<string>(mapUI, "SelectedNodeCode"),
            Is.EqualTo(RuntimeField<string>(currentNode, "Code"))
        );
        Assert.That(Property<Button>(mapUI, "EnterNodeButton").interactable, Is.True);
        Assert.That(Property<TMP_Text>(mapUI, "DetailTitle").text, Is.EqualTo("NORMAL BATTLE"));
        Assert.That(Get<GameObject>("commandPanelObject").activeSelf, Is.False);
        Assert.That(Get<GameObject>("orbitPanelObject").activeSelf, Is.False);
        Assert.That(Plan, Is.Null);
    }

    [Test]
    public void TC_MAP_002_SelectionDispatchesOnceAndRejectsStaleCallback()
    {
        Call("RestartRun");
        object node = CurrentRunNode;
        string nodeCode = RuntimeField<string>(node, "Code");
        int depth = Get<int>("battleNumber");

        LogAssert.Expect(
            LogType.Error,
            "Run Map menolak pilihan node yang stale atau tidak aktif."
        );
        Call("HandleRunMapNodeSelected", "FOREIGN_NODE");
        Assert.That(Get<int>("battleNumber"), Is.EqualTo(depth));
        Assert.That(IsRunMapOpen, Is.True);

        Property<Button>(RunMapUI, "EnterNodeButton").onClick.Invoke();
        Assert.That(IsRunMapOpen, Is.False);
        Assert.That(Get<bool>("isPlayerTurn"), Is.True);
        Assert.That(Plan, Is.Not.Null);
        object plan = Plan;

        LogAssert.Expect(
            LogType.Error,
            "Run Map menolak callback karena peta tidak sedang terbuka."
        );
        Call("HandleRunMapNodeSelected", nodeCode);
        Assert.That(Plan, Is.SameAs(plan));
        Assert.That(Get<int>("battleNumber"), Is.EqualTo(depth));
        Assert.That(IsRunMapOpen, Is.False);
    }

    [Test]
    public void TC_MAP_003_PlaceholderPreservesResourcesAndAdvancesOnce()
    {
        Call("RestartRun");
        Set("playerHP", 63);
        Mana(7);

        object activeRun = Get<object>("runManager");
        object node = Property<object>(activeRun, "CurrentNode");
        int safety = 0;

        while ((bool)Property<object>(node, "IsBattle"))
        {
            Assert.That(safety++, Is.LessThan(9));
            Assert.That(
                (bool)InvokeRuntime(
                    activeRun,
                    "TryResolveCurrentNode",
                    RuntimeField<object>(node, "Type"),
                    RuntimeField<string>(node, "Code")
                ),
                Is.True
            );
            node = Property<object>(activeRun, "CurrentNode");
        }

        Call("SyncLegacyRunStateFields");
        Call("OpenRunMap");

        int startingDepth = Property<int>(activeRun, "Depth");
        string nodeCode = RuntimeField<string>(node, "Code");
        Call("HandleRunMapNodeSelected", nodeCode);

        Assert.That(Property<int>(activeRun, "Depth"), Is.EqualTo(startingDepth + 1));
        Assert.That(Property<int>(activeRun, "CurrentHP"), Is.EqualTo(63));
        Assert.That(Property<int>(activeRun, "CurrentMana"), Is.EqualTo(7));
        Assert.That(Property<int>(activeRun, "TotalEmbers"), Is.Zero);
        Assert.That(Property<object>(activeRun, "Modifiers"), Is.SameAs(Get<object>("runModifiers")));
        Assert.That(IsRunMapOpen, Is.True);

        LogAssert.Expect(
            LogType.Error,
            "Run Map menolak pilihan node yang stale atau tidak aktif."
        );
        Call("HandleRunMapNodeSelected", nodeCode);
        Assert.That(Property<int>(activeRun, "Depth"), Is.EqualTo(startingDepth + 1));
    }

    private string NodeRouteKey(object map)
    {
        Array nodes = RuntimeField<Array>(map, "Nodes");
        var parts = new string[nodes.Length];

        for (int index = 0; index < nodes.Length; index++)
        {
            object node = nodes.GetValue(index);
            parts[index] =
                RuntimeField<int>(node, "Depth") + ":" +
                RuntimeField<object>(node, "Type") + ":" +
                RuntimeField<string>(node, "Code");
        }

        return string.Join("|", parts);
    }

    [Test]
    public void TC_ORB_001_OrbitSlotsFormDropdownLeftOfPlayer()
    {
        RectTransform panel = Get<GameObject>("orbitPanelObject")
            .GetComponent<RectTransform>();
        RectTransform player = GameObject.Find("PlayerBox")
            .GetComponent<RectTransform>();
        RectTransform first = Get<TMP_Text>("orbitSlot1Text").rectTransform;
        RectTransform second = Get<TMP_Text>("orbitSlot2Text").rectTransform;
        RectTransform third = Get<TMP_Text>("orbitSlot3Text").rectTransform;

        Assert.That(panel.anchorMin.x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(panel.anchorMax.x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(
            panel.anchoredPosition.x + panel.rect.width * (1f - panel.pivot.x),
            Is.LessThan(player.anchoredPosition.x),
            "Orbit panel must stay entirely left of PlayerBox"
        );
        Assert.That(first.anchoredPosition.x, Is.EqualTo(second.anchoredPosition.x).Within(0.1f));
        Assert.That(second.anchoredPosition.x, Is.EqualTo(third.anchoredPosition.x).Within(0.1f));
        Assert.That(first.anchoredPosition.y, Is.GreaterThan(second.anchoredPosition.y));
        Assert.That(second.anchoredPosition.y, Is.GreaterThan(third.anchoredPosition.y));
        Assert.That(first.gameObject.activeSelf, Is.True);
        Assert.That(second.gameObject.activeSelf, Is.True);
        Assert.That(third.gameObject.activeSelf, Is.True);
    }

    [Test]
    public void TC_ORB_002_AuthoredFrameAndActionIconsAreConfigured()
    {
        GameObject panel = Get<GameObject>("orbitPanelObject");
        GameObject frameObject = GameObject.Find("OrbitFrameArtwork");
        GameObject ringsObject = GameObject.Find("OrbitRings");
        TMP_Text first = Get<TMP_Text>("orbitSlot1Text");
        TMP_Text second = Get<TMP_Text>("orbitSlot2Text");
        TMP_Text third = Get<TMP_Text>("orbitSlot3Text");
        Image firstIcon = GameObject.Find("OrbitSlot1Icon").GetComponent<Image>();
        Image secondIcon = GameObject.Find("OrbitSlot2Icon").GetComponent<Image>();
        Image thirdIcon = GameObject.Find("OrbitSlot3Icon").GetComponent<Image>();

        Assert.That(frameObject, Is.Not.Null);
        Assert.That(frameObject.transform.parent, Is.EqualTo(panel.transform));
        Assert.That(frameObject.transform.GetSiblingIndex(), Is.Zero);

        Image frameImage = frameObject.GetComponent<Image>();
        Assert.That(frameImage, Is.Not.Null);
        Assert.That(frameImage.sprite, Is.Not.Null);
        Assert.That(frameImage.preserveAspect, Is.True);
        Assert.That(frameImage.raycastTarget, Is.False);

        Assert.That(ringsObject, Is.Not.Null);
        Type ringsType = Type.GetType(
            "SingularityOrbitRings, Assembly-CSharp",
            true
        );
        Behaviour rings = ringsObject.GetComponent(ringsType) as Behaviour;
        Assert.That(
            rings.enabled,
            Is.False,
            "The procedural ring placeholder must not overlap the authored frame"
        );

        Assert.That(first.gameObject.activeSelf, Is.True);
        Assert.That(second.gameObject.activeSelf, Is.True);
        Assert.That(third.gameObject.activeSelf, Is.True);
        Assert.That(first.enabled, Is.False);
        Assert.That(second.enabled, Is.False);
        Assert.That(third.enabled, Is.False);
        Assert.That(first.raycastTarget, Is.False);
        Assert.That(second.raycastTarget, Is.False);
        Assert.That(third.raycastTarget, Is.False);

        Image[] slotIcons = { firstIcon, secondIcon, thirdIcon };
        Vector2[] expectedIconCenters =
        {
            new Vector2(10f, 101f),
            new Vector2(11f, -2f),
            new Vector2(11f, -103f)
        };

        for (int iconIndex = 0; iconIndex < slotIcons.Length; iconIndex++)
        {
            Image slotIcon = slotIcons[iconIndex];
            Assert.That(slotIcon, Is.Not.Null);
            Assert.That(
                slotIcon.transform.parent,
                Is.EqualTo(panel.transform),
                "Icons must be direct overlay children of OrbitPanel"
            );
            Assert.That(
                slotIcon.transform.GetSiblingIndex(),
                Is.GreaterThan(frameObject.transform.GetSiblingIndex()),
                "Icons must render above the authored border"
            );
            Assert.That(slotIcon.rectTransform.localRotation, Is.EqualTo(Quaternion.identity));
            Assert.That(
                slotIcon.rectTransform.rect.width,
                Is.GreaterThan(first.rectTransform.rect.width),
                "Icon artwork should spill slightly beyond the slot border"
            );
            Assert.That(
                slotIcon.rectTransform.anchoredPosition.x,
                Is.EqualTo(expectedIconCenters[iconIndex].x).Within(0.1f),
                $"Orbit icon {iconIndex + 1} must be centered horizontally in its authored square"
            );
            Assert.That(
                slotIcon.rectTransform.anchoredPosition.y,
                Is.EqualTo(expectedIconCenters[iconIndex].y).Within(0.1f),
                $"Orbit icon {iconIndex + 1} must be centered vertically in its authored square"
            );
            Assert.That(slotIcon.preserveAspect, Is.True);
            Assert.That(slotIcon.raycastTarget, Is.False);
            Assert.That(slotIcon.enabled, Is.False);
            Assert.That(slotIcon.sprite, Is.Null);
        }

        const string frameAssetPath =
            "Assets/Project/Art/UI/Orbit/SyngravaOrbitFrame.png";
        string[] iconAssetPaths =
        {
            "Assets/Project/Art/UI/Orbit/Icons/OrbitAttackIcon.png",
            "Assets/Project/Art/UI/Orbit/Icons/OrbitShadowStrikeIcon.png",
            "Assets/Project/Art/UI/Orbit/Icons/OrbitSkillFallbackIcon.png",
            "Assets/Project/Art/UI/Orbit/Icons/OrbitGuardIcon.png"
        };
        TextureImporter importer =
            AssetImporter.GetAtPath(frameAssetPath) as TextureImporter;

        Assert.That(importer, Is.Not.Null);
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(importer.alphaIsTransparency, Is.True);

        foreach (string iconAssetPath in iconAssetPaths)
        {
            TextureImporter iconImporter =
                AssetImporter.GetAtPath(iconAssetPath) as TextureImporter;
            Assert.That(iconImporter, Is.Not.Null, iconAssetPath);
            Assert.That(
                iconImporter.textureType,
                Is.EqualTo(TextureImporterType.Sprite),
                iconAssetPath
            );
            Assert.That(iconImporter.mipmapEnabled, Is.False, iconAssetPath);
            Assert.That(iconImporter.alphaIsTransparency, Is.True, iconAssetPath);
        }

        Type animatorType = Type.GetType(
            "OrbitUIAnimator, Assembly-CSharp",
            true
        );
        Component animator = panel.GetComponent(animatorType);
        MethodInfo setSlotAction = animatorType.GetMethod(
            "SetSlotAction",
            BindingFlags.Instance | BindingFlags.Public
        );
        MethodInfo clearSlotIcons = animatorType.GetMethod(
            "ClearSlotIcons",
            BindingFlags.Instance | BindingFlags.Public
        );
        Type actionType = Type.GetType(
            "PlayerActionType, Assembly-CSharp",
            true
        );
        object attack = Enum.Parse(actionType, "Attack");
        object skill = Enum.Parse(actionType, "Skill");
        object guard = Enum.Parse(actionType, "Guard");

        setSlotAction.Invoke(animator, new[] { (object)0, attack, null });
        Assert.That(
            firstIcon.sprite,
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPaths[0]))
        );

        setSlotAction.Invoke(
            animator,
            new[] { (object)1, skill, "SKL_SHADOW_STRIKE" }
        );
        Assert.That(
            secondIcon.sprite,
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPaths[1]))
        );

        setSlotAction.Invoke(
            animator,
            new[] { (object)1, skill, "SKL_NOT_MAPPED" }
        );
        Assert.That(
            secondIcon.sprite,
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPaths[2]))
        );

        setSlotAction.Invoke(animator, new[] { (object)2, guard, null });
        Assert.That(
            thirdIcon.sprite,
            Is.SameAs(AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPaths[3]))
        );

        clearSlotIcons.Invoke(animator, null);

        for (int iconIndex = 0; iconIndex < slotIcons.Length; iconIndex++)
        {
            Image slotIcon = slotIcons[iconIndex];
            Assert.That(slotIcon.enabled, Is.False);
            Assert.That(slotIcon.sprite, Is.Null);
            Assert.That(
                slotIcon.rectTransform.anchoredPosition.x,
                Is.EqualTo(expectedIconCenters[iconIndex].x).Within(0.1f)
            );
            Assert.That(
                slotIcon.rectTransform.anchoredPosition.y,
                Is.EqualTo(expectedIconCenters[iconIndex].y).Within(0.1f)
            );
        }

        for (int iconIndex = 0; iconIndex < slotIcons.Length; iconIndex++)
        {
            slotIcons[iconIndex].rectTransform.anchoredPosition = Vector2.zero;
        }

        clearSlotIcons.Invoke(animator, null);

        for (int iconIndex = 0; iconIndex < slotIcons.Length; iconIndex++)
        {
            Assert.That(
                slotIcons[iconIndex].rectTransform.anchoredPosition,
                Is.EqualTo(expectedIconCenters[iconIndex]),
                $"Runtime reset must restore Orbit icon {iconIndex + 1} to the authored square center"
            );
        }
    }

    [UnityTest]
    public IEnumerator TC_ORB_003_RuntimeButtonActionsRenderOrbitIcons()
    {
        Image firstIcon = GameObject.Find("OrbitSlot1Icon").GetComponent<Image>();
        Image secondIcon = GameObject.Find("OrbitSlot2Icon").GetComponent<Image>();

        Assert.That(
            Get<object>("orbitUIAnimator"),
            Is.Not.Null,
            "BattleManager must resolve the OrbitUIAnimator used by the scene"
        );

        Click("basicAttackButton");

        Assert.That(OrbitCount, Is.EqualTo(1));
        Assert.That(firstIcon.enabled, Is.True);
        Assert.That(firstIcon.sprite, Is.Not.Null);

        yield return AwaitEnemy();
        yield return new WaitForSecondsRealtime(0.35f);

        Assert.That(firstIcon.enabled, Is.True);
        Assert.That(firstIcon.sprite, Is.Not.Null);
        Assert.That(firstIcon.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));

        firstIcon.enabled = false;
        firstIcon.GetComponent<CanvasGroup>().alpha = 0f;
        Call("UpdateOrbitUI");

        Assert.That(firstIcon.enabled, Is.True, "OrbitState must repair a hidden runtime icon");
        Assert.That(firstIcon.sprite, Is.Not.Null);

        yield return new WaitForSecondsRealtime(0.35f);

        Click("basicAttackButton");

        Assert.That(OrbitCount, Is.EqualTo(2));
        Assert.That(firstIcon.enabled, Is.True);
        Assert.That(secondIcon.enabled, Is.True);
        Assert.That(secondIcon.sprite, Is.Not.Null);

        yield return AwaitEnemy();
        yield return new WaitForSecondsRealtime(0.35f);

        Assert.That(firstIcon.enabled, Is.True);
        Assert.That(secondIcon.enabled, Is.True);
        Assert.That(secondIcon.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f).Within(0.01f));
    }

    [UnityTest]
    public IEnumerator TC_ORB_004_StageEntranceSlidesInFromLeftAndSettles()
    {
        GameObject panelObject = Get<GameObject>("orbitPanelObject");
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        Type animatorType = Type.GetType(
            "OrbitUIAnimator, Assembly-CSharp",
            true
        );
        Component animator = panelObject.GetComponent(animatorType);
        Vector2 restingPosition = (Vector2)animatorType
            .GetField("stageRestingPosition", Fields)
            .GetValue(animator);
        MethodInfo playEntrance = animatorType.GetMethod(
            "PlayStageEntrance",
            BindingFlags.Instance | BindingFlags.Public
        );
        CanvasGroup panelCanvasGroup = panelObject.GetComponent<CanvasGroup>();

        Assert.That(animator, Is.Not.Null);
        Assert.That(playEntrance, Is.Not.Null);
        Assert.That(panelCanvasGroup, Is.Not.Null);

        playEntrance.Invoke(animator, null);
        yield return null;

        Assert.That(panel.anchoredPosition.x, Is.LessThan(restingPosition.x));
        Assert.That(panelCanvasGroup.alpha, Is.LessThan(1f));

        yield return new WaitForSecondsRealtime(1f);

        Assert.That(panel.anchoredPosition.x, Is.EqualTo(restingPosition.x).Within(0.1f));
        Assert.That(panel.anchoredPosition.y, Is.EqualTo(restingPosition.y).Within(0.1f));
        Assert.That(panelCanvasGroup.alpha, Is.EqualTo(1f).Within(0.01f));
    }

    [UnityTest]
    public IEnumerator TC_RES_004_SkillVictoryKeepsCostAndConvergenceMana()
    {
        Set("playerHP", 64);
        Mana(2);
        AddOrbit("Skill");
        AddOrbit("Skill");
        Set("enemyHP", 1);
        Click("skillButton");
        // 2 Mana - 2 Skill cost + 1 Unstable Pulse = 1, without enemy-turn recovery.
        Assert.That(Get<int>("playerMana"), Is.EqualTo(1));
        yield return AwaitRewardSelection();
        RewardButton(FindRewardChoice("AttackPercent")).onClick.Invoke();
        AdvanceMapUntilBattleStarts();
        Assert.That(Get<int>("playerMana"), Is.EqualTo(1));
        Assert.That(Get<int>("playerHP"), Is.EqualTo(64));
        Assert.That(OrbitCount, Is.Zero);
        Assert.That(Get<Button>("skillButton").interactable, Is.False);
        Assert.That(Get<Button>("guardButton").interactable, Is.True);

        int incoming = Property<int>(Plan, "TotalEstimatedDamage");
        Click("guardButton");
        yield return AwaitEnemy();
        // Recovery still comes from a real Guard (+2) and completed enemy turn (+1).
        Assert.That(Get<int>("playerMana"), Is.EqualTo(4));
        Assert.That(Get<int>("playerHP"), Is.EqualTo(64 - Mathf.CeilToInt(incoming * 0.5f)));
        Assert.That(Get<Button>("skillButton").interactable, Is.True);
    }

    [TestCase(160, 15, 100, 10)]
    [TestCase(43, -1, 43, 0)]
    [TestCase(1, 0, 1, 0)]
    public void TC_RES_005_NodeEntryClampsOnlyOutOfRangeResources(
        int hp, int mana, int expectedHP, int expectedMana)
    {
        Set("playerHP", hp);
        Set("playerMana", mana);
        Call("StartBattle");
        Assert.That(Get<int>("playerHP"), Is.EqualTo(expectedHP));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(expectedMana));
    }

    [TestCase(-3, 0)]
    [TestCase(50, 10)]
    public void TC_RES_006_RestartClampsConfiguredStartingMana(int starting, int expected)
    {
        Set("playerStartingMana", starting);
        Call("RestartRun");
        Assert.That(Get<int>("playerHP"), Is.EqualTo(100));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(expected));
        Assert.That(Get<int>("playerMaxMana"), Is.EqualTo(10));
    }

    [Test]
    public void MissingSnapshotOnAnalyzeFailsClearlyWithoutCost()
    {
        Mana(8);
        Set("enemyActionPlan", null);
        LogAssert.Expect(LogType.Error, PlanError);
        Call("UseAnalyze");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(8));
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.False);
        Assert.That(Get<TMP_Text>("resultTitleText").text, Is.EqualTo("BATTLE ERROR"));
        Assert.That(Get<bool>("battleEnded"), Is.True);
    }

    [Test]
    public void MissingSnapshotOnEnemyTurnNeverRebuilds()
    {
        Set("enemyActionPlan", null);
        int hp = Get<int>("playerHP");
        LogAssert.Expect(LogType.Error, PlanError);
        var turn = (IEnumerator)Call("EnemyTurn");
        Assert.That(turn.MoveNext(), Is.False);
        Assert.That(Plan, Is.Null);
        Assert.That(Get<int>("playerHP"), Is.EqualTo(hp));
        Assert.That(Get<bool>("battleEnded"), Is.True);
    }

    [Test]
    public void SnapshotPropertiesAreImmutableAndReferencesRemainAssigned()
    {
        foreach (PropertyInfo property in Plan.GetType().GetProperties())
        {
            Assert.That(property.CanWrite, Is.False, property.Name);
        }
        foreach (FieldInfo field in manager.GetType().GetFields(Fields))
        {
            if (field.IsDefined(typeof(SerializeField), true) &&
                typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                Assert.That(field.GetValue(manager), Is.Not.Null, field.Name);
            }
        }
        Assert.That(Get<int>("basicAttackDamage"), Is.EqualTo(10));
        Call("UpdateUI");
        Assert.That(Get<TMP_Text>("enemyIntentText").gameObject.activeSelf, Is.False);
    }
}
#endif
