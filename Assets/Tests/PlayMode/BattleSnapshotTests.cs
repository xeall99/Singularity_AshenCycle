#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
public partial class BattleSnapshotTests
{
    private Component manager;
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string PlanError =
        "EnemyActionPlan tidak tersedia atau tidak valid. Restart Run diperlukan.";
    private const string GuidedTutorialCompletedSettingKey =
        "guided_tutorial_completed";

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
    private object CreateFinishedHistorySnapshot(int seed, bool cleared)
    {
        object modifiers = NewRuntimeObject("RunModifierCollection");
        object run = NewRuntimeObject("RunManager", modifiers);
        object stats = NewRuntimeObject("RunStatController", 100, 10, 10, 0.5f,
            modifiers);
        InvokeRuntime(run, "StartNewRun", seed, 100, 5, 100, 10);
        object outcome = Enum.Parse(RuntimeType("RunSummaryOutcome"),
            cleared ? "Cleared" : "Defeated");
        if (cleared)
        {
            object map = Property<object>(run, "NodeMap");
            for (int depth = 1; depth < Property<int>(map, "Count"); depth++)
                Assert.That((bool)InvokeRuntime(run, "TryAdvanceDepth"), Is.True);
            object boss = Property<object>(run, "CurrentNode");
            Assert.That((bool)InvokeRuntime(run, "TryResolveCurrentNode",
                RuntimeField<object>(boss, "Type"), RuntimeField<string>(boss, "Code")),
                Is.True);
        }
        else
        {
            Assert.That((bool)InvokeRuntime(run, "TryAdvanceDepth"), Is.True);
            InvokeRuntime(run, "MarkDefeated");
        }
        return InvokeRuntimeStatic("RunSummarySnapshot", "Create", outcome,
            cleared ? "Boss defeated." : "The path ended.", "Abyss Herald",
            run, stats, null, null, false, false);
    }
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
    private Component SummaryUI => UnityEngine.Object.FindAnyObjectByType(
        Type.GetType("RunSummaryUI, Assembly-CSharp", true)
    ) as Component;
    private Component GuidedTutorial => UnityEngine.Object.FindAnyObjectByType(
        RuntimeType("GuidedTutorialController")) as Component;

    private string TutorialStep(Component tutorial) =>
        Property<string>(tutorial, "CurrentStepName");

    private bool TutorialIsRunning(Component tutorial) =>
        Property<bool>(tutorial, "IsRunning");

    private Button TutorialButton(Component tutorial, string propertyName) =>
        Property<Button>(tutorial, propertyName);

    private void TutorialAction(Component tutorial, string methodName) =>
        InvokeRuntime(tutorial, methodName);

    private void SetGuidedTutorialCompleted(bool completed)
    {
        object settings = InvokeRuntimeStatic("UserSettingsStore", "CreateDefault");
        InvokeRuntime(settings, "Write", GuidedTutorialCompletedSettingKey,
            completed ? "true" : "false");
    }

    private string ReadGuidedTutorialCompleted() => (string)InvokeRuntime(
        InvokeRuntimeStatic("UserSettingsStore", "CreateDefault"),
        "Read", GuidedTutorialCompletedSettingKey);

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
            if (Property<bool>(CurrentRunNode, "IsBattle"))
            {
                EnterOrResolveCurrentMapNode();
            }
            else
            {
                // These combat regression cases isolate battle carryover from
                // optional event/Rest effects. TC_EVENT and TC_REST exercise
                // the real UI choices, costs, persistence and repeat rejection.
                InvokeRuntime(Get<object>("runManager"), "TryAdvanceDepth");
                Call("SyncLegacyRunStateFields");
                Call("OpenRunMap");
            }
        }

        Assert.That(Get<bool>("isPlayerTurn"), Is.True);
        Assert.That(Get<bool>("battleEnded"), Is.False);
        Assert.That(Plan, Is.Not.Null);
        // Combat-only fixtures bypass the cut-in; dedicated encounter tests
        // below exercise its actual input lock, animation and cancellation.
        object banner = Get<object>("encounterBanner");
        if (banner != null) InvokeRuntime(banner, "Cancel");
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

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

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

        SetGuidedTutorialCompleted(true);
        InvokeRuntime(InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault"),
            "DeleteCheckpoint");
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
        if (Application.productName == "SYNGRAVA_BattleTests")
        {
            SetGuidedTutorialCompleted(true);
            InvokeRuntime(InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault"),
                "DeleteCheckpoint");
        }
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

    private IEnumerator AwaitGuidedTutorialStep(string expectedStep, float timeout = 12f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (GuidedTutorial != null && TutorialStep(GuidedTutorial) != expectedStep)
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                $"Guided tutorial did not reach {expectedStep}; current: {TutorialStep(GuidedTutorial)}");
            yield return null;
        }
        Assert.That(GuidedTutorial, Is.Not.Null);
        Assert.That(TutorialStep(GuidedTutorial), Is.EqualTo(expectedStep));
    }

    private IEnumerator EnterGuidedTutorialBattle()
    {
        Button enterNode = Property<Button>(RunMapUI, "EnterNodeButton");
        if (TutorialButton(GuidedTutorial, "HighlightedButton") != enterNode)
        {
            Button node = TutorialButton(GuidedTutorial, "HighlightedButton");
            Assert.That(node, Is.Not.Null);
            Assert.That(((IEnumerable)Property<object>(RunMapUI, "NodeButtons"))
                .Cast<Button>().Contains(node), Is.True);
            node.onClick.Invoke();
            yield return null;
        }
        Assert.That(TutorialButton(GuidedTutorial, "HighlightedButton"), Is.SameAs(enterNode));
        enterNode.onClick.Invoke();
        Assert.That(IsRunMapOpen, Is.False);
        Assert.That(TutorialStep(GuidedTutorial), Is.EqualTo("BattleIntro"));
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

        while (Array.Exists(
            buttons,
            button => button.gameObject.activeInHierarchy &&
                !button.interactable
        ))
        {
            Assert.That(
                Time.realtimeSinceStartup,
                Is.LessThan(deadline),
                "Reward card entrance did not finish"
            );
            yield return null;
        }
    }

    private IEnumerator CaptureRunSummaryLayout()
    {
        Component ui = SummaryUI;
        Assert.That(ui, Is.Not.Null);
        Assert.That(Property<bool>(ui, "IsOpen"), Is.True);
        Canvas canvas = ui.GetComponent<Canvas>();
        RenderMode oldMode = canvas.renderMode;
        Camera oldCamera = canvas.worldCamera;
        float oldDistance = canvas.planeDistance;
        var cameraObject = new GameObject("RunSummaryCapture", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 540f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.transform.position = new Vector3(0f, 0f, -100f);
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            foreach (Vector2Int size in new[] {
                new Vector2Int(1920, 1080),
                new Vector2Int(1280, 1024) })
            {
                target = new RenderTexture(size.x, size.y, 24);
                target.Create();
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                RectTransform card = Property<RectTransform>(ui, "CardRect");
                RectTransform overlay = Property<GameObject>(ui, "Overlay")
                    .GetComponent<RectTransform>();
                AssertRectInside(card, overlay);
                foreach (TMP_Text label in card.GetComponentsInChildren<TMP_Text>())
                {
                    label.ForceMeshUpdate();
                    Assert.That(label.isTextOverflowing, Is.False, label.name);
                    AssertRectInside(label.rectTransform, card);
                }
                foreach (string panelName in new[] {
                    "RunSummaryResources", "RunSummaryCombat", "RunSummaryArchive" })
                {
                    RectTransform panel = card.GetComponentsInChildren<RectTransform>()
                        .Single(rect => rect.name == panelName);
                    foreach (TMP_Text label in panel.GetComponentsInChildren<TMP_Text>())
                    {
                        AssertRectInside(label.rectTransform, panel);
                    }
                }
                RectTransform route = card.GetComponentsInChildren<RectTransform>()
                    .Single(rect => rect.name == "RunSummaryRoute");
                foreach (Image icon in route.GetComponentsInChildren<Image>()
                    .Where(image => image.name.StartsWith("RunSummaryNode") &&
                        !image.name.Contains("Backing")))
                {
                    AssertRectInside(icon.rectTransform, route);
                    Assert.That(icon.color.r, Is.GreaterThan(0.9f), icon.name);
                    Assert.That(icon.color.g, Is.GreaterThan(0.9f), icon.name);
                }
                Assert.That(Property<TMP_Text>(ui, "RouteText").gameObject.activeSelf,
                    Is.False, "Redundant route footer must stay hidden.");
                AssertRectInside(
                    Property<Button>(ui, "ActionButton").GetComponent<RectTransform>(),
                    card
                );
                camera.Render();
                RenderTexture.active = target;
                var pixels = new Texture2D(
                    size.x, size.y, TextureFormat.RGB24, false
                );
                pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(
                    Path.GetFullPath(Path.Combine(
                        Application.dataPath,
                        "../run-summary-" + size.x + "x" + size.y + ".png"
                    )),
                    pixels.EncodeToPNG()
                );
                UnityEngine.Object.Destroy(pixels);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
                target = null;
            }
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.Destroy(target);
            }
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = oldDistance;
            UnityEngine.Object.Destroy(cameraObject);
        }
    }

    [Test]
    public void TC_SUM_001_StatisticsPersistValidateAndReset()
    {
        object run = NewRuntimeObject(
            "RunManager",
            NewRuntimeObject("RunModifierCollection")
        );
        InvokeRuntime(run, "StartNewRunWithNodeCount", 8273, 10, 80, 5, 100, 10);
        InvokeRuntime(run, "AdvanceElapsedTime", 83.75f);
        InvokeRuntime(run, "RecordPlayerAction", 24, true);
        InvokeRuntime(run, "RecordPlayerAction", 0, false);
        InvokeRuntime(run, "RecordConvergence", Enum.Parse(
            RuntimeType("ConvergenceType"), "EventHorizon"
        ));
        InvokeRuntime(run, "ApplyDamage", 11);
        InvokeRuntime(run, "RestoreHP", 6, 100);
        InvokeRuntime(run, "RecordBattleWon");

        object state = InvokeRuntime(run, "CaptureState");
        object decoded = JsonUtility.FromJson(
            JsonUtility.ToJson(state), RuntimeType("SeededRunState")
        );
        object restored = NewRuntimeObject(
            "RunManager",
            NewRuntimeObject("RunModifierCollection")
        );
        InvokeRuntime(restored, "RestoreState", decoded, 100, 10);

        Assert.That(Property<float>(restored, "ElapsedSeconds"), Is.EqualTo(83.75f));
        Assert.That(Property<int>(restored, "BattlesWon"), Is.EqualTo(1));
        Assert.That(Property<int>(restored, "DamageDealt"), Is.EqualTo(24));
        Assert.That(Property<int>(restored, "DamageTaken"), Is.EqualTo(11));
        Assert.That(Property<int>(restored, "HealingReceived"), Is.EqualTo(6));
        Assert.That(Property<int>(restored, "CriticalHits"), Is.EqualTo(1));
        Assert.That(Property<int>(restored, "ActionsTaken"), Is.EqualTo(2));
        Assert.That(Property<int>(restored, "ConvergencesTriggered"), Is.EqualTo(1));
        Assert.That(Property<int>(restored, "EventHorizonCount"), Is.EqualTo(1));

        string validRestoredState = JsonUtility.ToJson(
            InvokeRuntime(restored, "CaptureState")
        );
        object corrupt = InvokeRuntime(state, "Clone");
        SetRuntimeField(corrupt, "EventHorizonCount", 2);
        Assert.Throws<InvalidOperationException>(() =>
            InvokeRuntime(restored, "RestoreState", corrupt, 100, 10)
        );
        Assert.That(
            JsonUtility.ToJson(InvokeRuntime(restored, "CaptureState")),
            Is.EqualTo(validRestoredState),
            "Invalid summary counters must not partially mutate the active run."
        );

        InvokeRuntime(restored, "StartNewRunWithNodeCount", 8274, 10, 100, 5, 100, 10);
        Assert.That(Property<float>(restored, "ElapsedSeconds"), Is.Zero);
        Assert.That(Property<int>(restored, "DamageDealt"), Is.Zero);
        Assert.That(Property<int>(restored, "BattlesWon"), Is.Zero);
        Assert.That(Property<int>(restored, "ConvergencesTriggered"), Is.Zero);
    }

    [Test]
    public void TC_ECHO_001_PermanentWalletPaysOnceAndSurvivesNewRun()
    {
        string directory = Path.Combine(Application.temporaryCachePath,
            "syngrava-echo-" + Guid.NewGuid().ToString("N"));
        try
        {
            object wallet = NewRuntimeObject("EchoFragmentWallet", directory);
            object run = NewRuntimeObject("RunManager",
                NewRuntimeObject("RunModifierCollection"));
            InvokeRuntime(run, "StartNewRun", 91801, 100, 5, 100, 10);
            string firstId = Property<string>(run, "RunId");
            Assert.That((bool)InvokeRuntime(run, "TryAdvanceDepth"), Is.True);
            InvokeRuntime(run, "MarkDefeated");
            object award = InvokeRuntimeStatic("EchoFragmentAward", "FromFinishedRun", run);
            int found = Property<int>(award, "NormalBattleFragments");
            Assert.That(found, Is.InRange(8, 12));
            int retained = found * 60 / 100;
            Assert.That(Property<int>(award, "Total"), Is.EqualTo(retained));

            object first = InvokeRuntime(wallet, "ClaimFinishedRun", run);
            Assert.That(Property<int>(first, "Earned"), Is.EqualTo(retained));
            Assert.That(Property<int>(first, "Balance"), Is.EqualTo(retained));
            object reopened = NewRuntimeObject("EchoFragmentWallet", directory);
            object repeat = InvokeRuntime(reopened, "ClaimFinishedRun", run);
            Assert.That(Property<bool>(repeat, "WasAlreadyClaimed"), Is.True);
            Assert.That(Property<int>(repeat, "Balance"), Is.EqualTo(retained));

            InvokeRuntime(run, "StartNewRun", 91801, 100, 5, 100, 10);
            Assert.That(Property<string>(run, "RunId"), Is.Not.EqualTo(firstId));
            Assert.That((bool)InvokeRuntime(run, "TryAdvanceDepth"), Is.True);
            InvokeRuntime(run, "MarkDefeated");
            object second = InvokeRuntime(reopened, "ClaimFinishedRun", run);
            Assert.That(Property<int>(second, "Balance"), Is.EqualTo(retained * 2));
            Assert.That((int)InvokeRuntime(reopened, "ReadBalance"), Is.EqualTo(retained * 2));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TC_ECHO_002_LegacyCheckpointGetsStableIdentity()
    {
        object run = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(run, "StartNewRun", 91802, 100, 5, 100, 10);
        object oldState = InvokeRuntime(run, "CaptureState");
        SetRuntimeField(oldState, "RunId", null);
        object restored = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(restored, "RestoreState", oldState, 100, 10);
        string legacyId = Property<string>(restored, "RunId");
        StringAssert.StartsWith("legacy-91802-", legacyId);
        object savedAgain = InvokeRuntime(restored, "CaptureState");
        object replay = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(replay, "RestoreState", savedAgain, 100, 10);
        Assert.That(Property<string>(replay, "RunId"), Is.EqualTo(legacyId));
    }

    [Test]
    public void TC_ECHO_003_ClearPaysEliteBossAndClearBonus()
    {
        object run = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(run, "StartNewRun", 91803, 100, 5, 100, 10);
        object map = Property<object>(run, "NodeMap");
        int count = Property<int>(map, "Count");
        for (int depth = 1; depth < count; depth++)
            Assert.That((bool)InvokeRuntime(run, "TryAdvanceDepth"), Is.True);
        object boss = Property<object>(run, "CurrentNode");
        Assert.That((bool)InvokeRuntime(run, "TryResolveCurrentNode",
            RuntimeField<object>(boss, "Type"), RuntimeField<string>(boss, "Code")),
            Is.True);
        Assert.That(Property<bool>(run, "IsCompleted"), Is.True);
        object award = InvokeRuntimeStatic("EchoFragmentAward", "FromFinishedRun", run);
        Assert.That(Property<int>(award, "BossFragments"), Is.EqualTo(50));
        Assert.That(Property<int>(award, "ClearBonus"), Is.EqualTo(20));
        Assert.That(Property<int>(award, "Total"),
            Is.EqualTo(Property<int>(award, "Found") + 20));
        Assert.That(Property<int>(award, "NormalBattleFragments"), Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void TC_UNLOCK_001_PurchaseSpendsOncePersistsAndExpandsOnlyTheRelicPool()
    {
        string directory = Path.Combine(Application.temporaryCachePath,
            "syngrava-unlock-" + Guid.NewGuid().ToString("N"));
        try
        {
            object wallet = NewRuntimeObject("EchoFragmentWallet", directory);
            object run = NewRuntimeObject("RunManager",
                NewRuntimeObject("RunModifierCollection"));
            InvokeRuntime(run, "StartNewRun", 98102, 100, 5, 100, 10);
            object map = Property<object>(run, "NodeMap");
            for (int depth = 1; depth < Property<int>(map, "Count"); depth++)
                Assert.That((bool)InvokeRuntime(run, "TryAdvanceDepth"), Is.True);
            object boss = Property<object>(run, "CurrentNode");
            Assert.That((bool)InvokeRuntime(run, "TryResolveCurrentNode",
                RuntimeField<object>(boss, "Type"), RuntimeField<string>(boss, "Code")), Is.True);
            InvokeRuntime(wallet, "ClaimFinishedRun", run);
            int before = (int)InvokeRuntime(wallet, "ReadBalance");
            Assert.That(before, Is.GreaterThanOrEqualTo(120));
            Assert.That((bool)InvokeRuntime(wallet, "HasUnlock", "RELIC_ECHO_PRISM"), Is.False);

            object purchase = InvokeRuntime(wallet, "TryPurchase", "RELIC_ECHO_PRISM");
            Assert.That(Property<bool>(purchase, "Purchased"), Is.True);
            Assert.That(Property<int>(purchase, "Balance"), Is.EqualTo(before - 120));
            object reopened = NewRuntimeObject("EchoFragmentWallet", directory);
            Assert.That((bool)InvokeRuntime(reopened, "HasUnlock", "RELIC_ECHO_PRISM"), Is.True);
            object repeat = InvokeRuntime(reopened, "TryPurchase", "RELIC_ECHO_PRISM");
            Assert.That(Property<bool>(repeat, "AlreadyOwned"), Is.True);
            Assert.That(Property<int>(repeat, "Balance"), Is.EqualTo(before - 120));

            object owned = NewRuntimeObject("RunModifierCollection");
            Array baseline = (Array)InvokeRuntimeStatic("RunRelicCatalog", "GetDefinitions");
            Assert.That(baseline.Length, Is.EqualTo(4));
            object newRelic = InvokeRuntimeStatic("RunRelicCatalog", "GetUnlockableDefinition");
            Assert.That(Property<string>(newRelic, "Code"), Is.EqualTo("RELIC_ECHO_PRISM"));
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int seed = 1; seed <= 50; seed++)
            {
                Array locked = (Array)InvokeRuntimeStatic("RunRelicCatalog",
                    "CreateChoicesForUnlockState", seed, owned, false);
                Assert.That(locked.Cast<object>().Any(choice =>
                    Property<string>(choice, "Code") == "RELIC_ECHO_PRISM"), Is.False);
                Array unlocked = (Array)InvokeRuntimeStatic("RunRelicCatalog",
                    "CreateChoicesForUnlockState", seed, owned, true);
                foreach (object choice in unlocked)
                    seen.Add(Property<string>(choice, "Code"));
            }
            Assert.That(seen, Does.Contain("RELIC_ECHO_PRISM"));
            InvokeRuntime(owned, "AddOrStack", newRelic);
            Assert.Throws<InvalidOperationException>(() =>
                InvokeRuntime(owned, "AddOrStack", newRelic));
            Assert.That((int)InvokeRuntime(reopened, "ReadBalance"), Is.EqualTo(before - 120));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TC_UNLOCK_002_EchoPrismAddsFourDamageOnlyToFirstBasicAttack()
    {
        object relic = InvokeRuntimeStatic("RunRelicCatalog", "GetUnlockableDefinition");
        object run = Get<object>("runManager");
        InvokeRuntime(Property<object>(run, "Modifiers"), "AddOrStack", relic);
        Set("enemyHP", 200);
        int seed = Property<int>(run, "RunSeed");
        int depth = Property<int>(run, "Depth");
        bool firstCritical = (bool)InvokeRuntimeStatic("BattleTurnRules", "IsCritical",
            seed, depth, 0, 0.05f);
        bool secondCritical = (bool)InvokeRuntimeStatic("BattleTurnRules", "IsCritical",
            seed, depth, 1, 0.05f);
        int before = Get<int>("enemyHP");
        Call("UseBasicAttack");
        int firstExpected = firstCritical
            ? (int)InvokeRuntimeStatic("BattleTurnRules", "ApplyCritical", 14) : 14;
        Assert.That(before - Get<int>("enemyHP"), Is.EqualTo(firstExpected));

        Set("isPlayerTurn", true);
        before = Get<int>("enemyHP");
        Call("UseBasicAttack");
        int secondExpected = secondCritical
            ? (int)InvokeRuntimeStatic("BattleTurnRules", "ApplyCritical", 10) : 10;
        Assert.That(before - Get<int>("enemyHP"), Is.EqualTo(secondExpected));
        Assert.That(Property<int>(Property<object>(run, "Modifiers"), "Count"), Is.EqualTo(1));
    }

    [Test]
    public void TC_UNLOCK_003_EmptyWalletCannotPurchaseOrAcceptUnknownUnlocks()
    {
        string directory = Path.Combine(Application.temporaryCachePath,
            "syngrava-unlock-" + Guid.NewGuid().ToString("N"));
        try
        {
            object wallet = NewRuntimeObject("EchoFragmentWallet", directory);
            object result = InvokeRuntime(wallet, "TryPurchase", "RELIC_ECHO_PRISM");
            Assert.That(Property<bool>(result, "Purchased"), Is.False);
            Assert.That(Property<int>(result, "Balance"), Is.Zero);
            Assert.Throws<ArgumentException>(() =>
                InvokeRuntime(wallet, "TryPurchase", "UNTRUSTED_UNLOCK"));
            Assert.That((int)InvokeRuntime(wallet, "ReadBalance"), Is.Zero);
            Assert.That(Directory.Exists(directory), Is.False,
                "Rejected purchases must not create a wallet file.");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [UnityTest]
    public IEnumerator TC_MENU_001_MainMenuActionsAndIdentityFitBothResolutions()
    {
        InvokeRuntime(InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault"),
            "DeleteCheckpoint");
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        GameObject card = GameObject.Find("MenuCard");
        Assert.That(card, Is.Not.Null);
        Assert.That(GameObject.Find("MenuEye"), Is.Null);
        Assert.That(GameObject.Find("MenuLegibilityVeil"), Is.Null);
        Assert.That(GameObject.Find("NavigationTitle"), Is.Null);
        GameObject portrait = GameObject.Find("WardenPortraitBackdrop");
        Assert.That(portrait, Is.Not.Null);
        Assert.That(portrait.GetComponent<Image>().sprite.name,
            Is.EqualTo("MainMenuWardenPortrait"));
        foreach (string name in new[] { "ContinueButton", "NewRunButton",
            "EchoArchiveButton", "RunHistoryButton", "SettingsButton",
            "QuitButton", "TopRunsButton", "FieldGuideButton" })
        {
            GameObject button = GameObject.Find(name);
            Assert.That(button, Is.Not.Null, name);
            Image image = button.GetComponent<Image>();
            Assert.That(image, Is.Not.Null, name);
            Assert.That(image.sprite, Is.Null,
                name + " should be a quiet text-led row without ornate button artwork.");
            Assert.That(button.GetComponent(
                Type.GetType("MainMenuActionVisual, Assembly-CSharp", true)), Is.Not.Null,
                name + " needs restrained pointer/gamepad focus feedback.");
            AssertRectInside(button.GetComponent<RectTransform>(),
                card.GetComponent<RectTransform>());
        }
        Assert.That(GameObject.Find("ContentManagerButton"), Is.Null,
            "The player menu must not expose freeform skill authoring.");
        Canvas menuCanvas = card.GetComponentInParent<Canvas>();
        Type contentManagerType = Type.GetType("ContentManagerUI, Assembly-CSharp", true);
        Assert.That(menuCanvas.GetComponent(contentManagerType), Is.Null,
            "The player menu must not initialize the skill CRUD panel.");
        Assert.That(GameObject.Find("ContinueButton").GetComponent<Button>().interactable,
            Is.False, "No checkpoint must be shown as unavailable.");
        Assert.That(EventSystem.current.currentSelectedGameObject,
            Is.EqualTo(GameObject.Find("NewRunButton")),
            "New Expedition must receive initial focus when no save exists.");
        foreach (TMP_Text label in card.GetComponentsInChildren<TMP_Text>())
        {
            label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing, Is.False, label.name);
            AssertRectInside(label.rectTransform, card.GetComponent<RectTransform>());
        }

        Canvas canvas = card.GetComponentInParent<Canvas>();
        RenderMode oldMode = canvas.renderMode;
        Camera oldCamera = canvas.worldCamera;
        float oldDistance = canvas.planeDistance;
        var cameraObject = new GameObject("MainMenuCapture", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 540f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.transform.position = new Vector3(0f, 0f, -100f);
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            foreach (Vector2Int size in new[] {
                new Vector2Int(1920, 1080), new Vector2Int(1280, 1024),
                new Vector2Int(1280, 720) })
            {
                target = new RenderTexture(size.x, size.y, 24);
                target.Create();
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertRectInside(card.GetComponent<RectTransform>(),
                    canvas.GetComponent<RectTransform>());
                camera.Render();
                RenderTexture.active = target;
                var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../main-menu-" + size.x + "x" + size.y + ".png")),
                    pixels.EncodeToPNG());
                UnityEngine.Object.Destroy(pixels);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
                target = null;
            }
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = oldDistance;
            UnityEngine.Object.Destroy(cameraObject);
        }
    }

    [UnityTest]
    public IEnumerator TC_CRUD_001_SkillRecordsRoundTripThroughIsolatedDemoAndSQLite()
    {
        InvokeRuntime(InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault"),
            "DeleteCheckpoint");
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;

        GameObject menuCard = GameObject.Find("MenuCard");
        Canvas menuCanvas = menuCard.GetComponentInParent<Canvas>();
        Assert.That(GameObject.Find("ContentManagerButton"), Is.Null,
            "Skill CRUD must not have a player-facing Main Menu entry.");
        Type contentManagerType = Type.GetType("ContentManagerUI, Assembly-CSharp", true);
        Assert.That(menuCanvas.GetComponent(contentManagerType), Is.Null,
            "Skill CRUD must not be initialized with the player Main Menu.");

        object repository = InvokeRuntimeStatic("SkillContentRepository", "CreateDefault");
        const string skillName = "Test Cinder Pulse";
        int createdId = -1;
        try
        {
            Component demo = menuCanvas.gameObject.AddComponent(contentManagerType);
            InvokeRuntime(demo, "Show");
            GameObject overlay = GameObject.Find("ContentManagerOverlay");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(overlay.activeSelf, Is.True);
            GameObject.Find("NewSkill").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("SkillName").GetComponent<TMP_InputField>().text = skillName;
            GameObject.Find("SkillMana").GetComponent<TMP_InputField>().text = "3";
            GameObject.Find("SkillPower").GetComponent<TMP_InputField>().text = "31";
            GameObject.Find("CreateSkill").GetComponent<Button>().onClick.Invoke();
            Array records = (Array)InvokeRuntime(repository, "ReadAll");
            object created = records.Cast<object>().Single(row =>
                RuntimeProperty<string>(row, "Name") == skillName);
            createdId = RuntimeProperty<int>(created, "Id");
            Assert.That(RuntimeProperty<int>(created, "ManaCost"), Is.EqualTo(3));
            Assert.That(RuntimeProperty<int>(created, "Power"), Is.EqualTo(31));

            GameObject.Find("SkillMana").GetComponent<TMP_InputField>().text = "4";
            GameObject.Find("SkillPower").GetComponent<TMP_InputField>().text = "42";
            GameObject.Find("UpdateSkill").GetComponent<Button>().onClick.Invoke();
            Assert.That(GameObject.Find("ContentManagerConfirmation"), Is.Not.Null);
            GameObject.Find("CancelContentChange").GetComponent<Button>().onClick.Invoke();
            records = (Array)InvokeRuntime(repository, "ReadAll");
            created = records.Cast<object>().Single(row =>
                RuntimeProperty<int>(row, "Id") == createdId);
            Assert.That(RuntimeProperty<int>(created, "Power"), Is.EqualTo(31),
                "Cancelled update must leave the database unchanged.");
            GameObject.Find("UpdateSkill").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("ConfirmContentChange").GetComponent<Button>().onClick.Invoke();
            object reopened = InvokeRuntimeStatic("SkillContentRepository", "CreateDefault");
            records = (Array)InvokeRuntime(reopened, "ReadAll");
            created = records.Cast<object>().Single(row =>
                RuntimeProperty<int>(row, "Id") == createdId);
            Assert.That(RuntimeProperty<int>(created, "ManaCost"), Is.EqualTo(4));
            Assert.That(RuntimeProperty<int>(created, "Power"), Is.EqualTo(42));

            GameObject.Find("DeleteSkill").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("CancelContentChange").GetComponent<Button>().onClick.Invoke();
            Assert.That(((Array)InvokeRuntime(reopened, "ReadAll")).Cast<object>()
                .Any(row => RuntimeProperty<int>(row, "Id") == createdId), Is.True);
            GameObject.Find("DeleteSkill").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("ConfirmContentChange").GetComponent<Button>().onClick.Invoke();
            Assert.That(((Array)InvokeRuntime(reopened, "ReadAll")).Cast<object>()
                .Any(row => RuntimeProperty<int>(row, "Id") == createdId), Is.False);
            createdId = -1;
            Assert.That(((Array)InvokeRuntime(reopened, "ReadAll")).Cast<object>()
                .Any(row => RuntimeProperty<string>(row, "Code") ==
                    "SKL_SHADOW_STRIKE"), Is.True,
                "The shipped combat skill must remain intact.");
            GameObject.Find("ContentManagerClose").GetComponent<Button>().onClick.Invoke();
            Assert.That(overlay.activeSelf, Is.False);
        }
        finally
        {
            if (createdId > 0) InvokeRuntime(repository, "Delete", createdId);
        }
    }

    [Test]
    public void TC_CRUD_002_ValidationCoreProtectionAndActiveRunGuard()
    {
        string seed = Path.Combine(Application.streamingAssetsPath,
            "Database", "game_seed.db");
        string copy = Path.Combine(Application.temporaryCachePath,
            "crud-test-" + Guid.NewGuid().ToString("N") + ".db");
        File.Copy(seed, copy);
        try
        {
            object repository = NewRuntimeObject("SkillContentRepository", copy, null);
            Assert.Throws<ArgumentException>(() =>
                InvokeRuntime(repository, "Create", "   ", 2, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                InvokeRuntime(repository, "Create", "Invalid Mana", 11, 10));
            object created = InvokeRuntime(repository, "Create", "Unique Flame", 2, 10);
            int id = RuntimeProperty<int>(created, "Id");
            Assert.Throws<InvalidOperationException>(() =>
                InvokeRuntime(repository, "Create", "unique flame", 2, 10));
            Assert.Throws<InvalidOperationException>(() =>
                InvokeRuntime(repository, "Update", 1, "Core Override", 2, 50));
            Assert.Throws<InvalidOperationException>(() =>
                InvokeRuntime(repository, "Delete", 1));

            object blocked = NewRuntimeObject("SkillContentRepository", copy,
                new Func<bool>(() => true));
            Assert.That(((Array)InvokeRuntime(blocked, "ReadAll")).Length,
                Is.EqualTo(2), "Read remains available during an active run.");
            Assert.Throws<InvalidOperationException>(() =>
                InvokeRuntime(blocked, "Create", "Locked", 2, 10));
            Assert.Throws<InvalidOperationException>(() =>
                InvokeRuntime(blocked, "Update", id, "Locked", 2, 10));
            Assert.Throws<InvalidOperationException>(() =>
                InvokeRuntime(blocked, "Delete", id));
            Assert.That(((Array)InvokeRuntime(repository, "ReadAll")).Length,
                Is.EqualTo(2), "Rejected writes must leave all records intact.");
        }
        finally
        {
            if (File.Exists(copy)) File.Delete(copy);
        }
    }

    [UnityTest]
    public IEnumerator TC_MENU_002_ActiveCheckpointEnablesContinueAndProtectsNewRun()
    {
        object inspection = InvokeRuntime(
            InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault"),
            "Inspect", 100, 10);
        Assert.That(Property<bool>(inspection, "HasUsableState"), Is.True);
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        Button continueButton = GameObject.Find("ContinueButton").GetComponent<Button>();
        Assert.That(continueButton.interactable, Is.True);
        Assert.That(continueButton.GetComponentsInChildren<TMP_Text>()
            .Any(label => label.text.Contains("STAGE")), Is.True);
        GameObject.Find("NewRunButton").GetComponent<Button>().onClick.Invoke();
        GameObject confirmation = GameObject.Find("NewRunConfirmDialog");
        Assert.That(confirmation, Is.Not.Null);
        Assert.That(confirmation.activeSelf, Is.True);
        GameObject.Find("ConfirmNo").GetComponent<Button>().onClick.Invoke();
        Assert.That(confirmation.activeSelf, Is.False);
        Assert.That(continueButton.interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator TC_MENU_003_NewExpeditionShowsLoadingAndEntersBattle()
    {
        InvokeRuntime(InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault"),
            "DeleteCheckpoint");
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;

        Assert.That(Application.CanStreamedLevelBeLoaded("Battle"), Is.True,
            "The Battle scene must be present in Build Settings for async loading.");
        GameObject.Find("NewRunButton").GetComponent<Button>().onClick.Invoke();
        GameObject overlay = GameObject.Find("MainMenuLoadingOverlay");
        Assert.That(overlay, Is.Not.Null);
        Assert.That(overlay.transform.Find("LoadingBackgroundPlate"), Is.Not.Null);
        Assert.That(overlay.transform.Find("LoadingProgressRail/LoadingProgressFill"),
            Is.Not.Null);

        float deadline = Time.realtimeSinceStartup + 20f;
        while (SceneManager.GetActiveScene().name != "Battle")
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                "Main Menu loading did not activate the Battle scene.");
            yield return null;
        }
        yield return null;
        Assert.That(UnityEngine.Object.FindAnyObjectByType(
            Type.GetType("BattleManager, Assembly-CSharp", true)), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator TC_MENU_004_SettingsButtonOpensTheExistingSettingsPanel()
    {
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;

        object settings = UnityEngine.Object.FindAnyObjectByType(
            Type.GetType("BattleSettingsUI, Assembly-CSharp", true));
        Assert.That(settings, Is.Not.Null);
        GameObject panel = Property<GameObject>(settings, "SettingsPanel");
        Assert.That(panel, Is.Not.Null);
        Assert.That(panel.activeSelf, Is.False);
        float menuTimeScale = Time.timeScale;
        GameObject.Find("SettingsButton").GetComponent<Button>().onClick.Invoke();
        Assert.That(panel.activeSelf, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(menuTimeScale));
        Assert.That(Property<Button>(settings, "SettingsButton")
            .gameObject.activeSelf, Is.False);
        Assert.That(panel.transform.Find("SettingsCard/ReturnToMainMenuButton"),
            Is.Null);
        InvokeRuntime(settings, "CloseSettings");
        Assert.That(panel.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator TC_MENU_006_FieldGuideNavigatesAndRestoresMenuFocus()
    {
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;

        GameObject fieldGuideButton = GameObject.Find("FieldGuideButton");
        Assert.That(fieldGuideButton, Is.Not.Null);
        Component guide = UnityEngine.Object.FindAnyObjectByType(
            RuntimeType("TutorialUI")) as Component;
        Assert.That(guide, Is.Not.Null);
        float originalTimeScale = Time.timeScale;

        EventSystem.current.SetSelectedGameObject(fieldGuideButton);
        fieldGuideButton.GetComponent<Button>().onClick.Invoke();
        Assert.That(Property<bool>(guide, "IsOpen"), Is.True);
        Assert.That(Property<int>(guide, "PageIndex"), Is.Zero);
        Assert.That(Property<int>(guide, "PageCount"), Is.EqualTo(6));
        Assert.That(Property<Button>(guide, "PreviousButton").interactable, Is.False);

        InvokeRuntime(guide, "NextPage");
        Assert.That(Property<int>(guide, "PageIndex"), Is.EqualTo(1));
        InvokeRuntime(guide, "PreviousPage");
        Assert.That(Property<int>(guide, "PageIndex"), Is.Zero);
        for (int page = 1; page < Property<int>(guide, "PageCount"); page++)
            InvokeRuntime(guide, "NextPage");
        Assert.That(Property<int>(guide, "PageIndex"), Is.EqualTo(Property<int>(guide, "PageCount") - 1));
        Assert.That(Property<Button>(guide, "NextButton").interactable, Is.False);
        Assert.That(Property<Button>(guide, "PreviousButton").interactable, Is.True);

        TMP_Text body = GameObject.Find("FieldGuideBody").GetComponent<TMP_Text>();
        body.ForceMeshUpdate();
        Assert.That(body.isTextOverflowing, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(originalTimeScale));

        InvokeRuntime(guide, "HandleEscape");
        Assert.That(Property<bool>(guide, "IsOpen"), Is.False);
        Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(fieldGuideButton));
        Assert.That(Time.timeScale, Is.EqualTo(originalTimeScale));

        fieldGuideButton.GetComponent<Button>().onClick.Invoke();
        GameObject.Find("CloseFieldGuideButton").GetComponent<Button>().onClick.Invoke();
        Assert.That(Property<bool>(guide, "IsOpen"), Is.False);
    }

    [UnityTest]
    public IEnumerator TC_MENU_005_ContinueLoadsTheSavedRunWithoutReplacingItsCheckpoint()
    {
        object store = InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault");
        object inspection = InvokeRuntime(store, "Inspect", 100, 10);
        Assert.That(Property<bool>(inspection, "HasUsableState"), Is.True);

        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;

        Button continueButton = GameObject.Find("ContinueButton").GetComponent<Button>();
        Assert.That(continueButton.interactable, Is.True);
        continueButton.onClick.Invoke();
        Assert.That(GameObject.Find("MainMenuLoadingOverlay"), Is.Not.Null);

        float deadline = Time.realtimeSinceStartup + 20f;
        while (SceneManager.GetActiveScene().name != "Battle")
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                "Continue did not activate the Battle scene.");
            yield return null;
        }
        yield return null;
        Assert.That(IsRunMapOpen, Is.True,
            "Continue should restore the saved map boundary.");
        inspection = InvokeRuntime(store, "Inspect", 100, 10);
        Assert.That(Property<bool>(inspection, "HasUsableState"), Is.True,
            "Continue must not erase the active checkpoint.");
    }

    [UnityTest]
    public IEnumerator TC_E2E_001_MenuContinueClearHistoryLeaderboardAndFreshRun()
    {
        // This scenario expects its only finished run to appear in Top Ten.
        // Reusing an isolated project must not let earlier tests displace it.
        Assert.That(Application.productName, Is.EqualTo("SYNGRAVA_BattleTests"));
        object isolatedHistory = InvokeRuntimeStatic("RunHistoryStore", "CreateDefault");
        File.Delete(Property<string>(isolatedHistory, "HistoryPath"));
        File.Delete(Property<string>(isolatedHistory, "BackupPath"));
        object checkpoint = InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault");
        InvokeRuntime(checkpoint, "DeleteCheckpoint");
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        Assert.That(GameObject.Find("ContinueButton").GetComponent<Button>().interactable,
            Is.False);
        GameObject.Find("NewRunButton").GetComponent<Button>().onClick.Invoke();

        float deadline = Time.realtimeSinceStartup + 20f;
        while (SceneManager.GetActiveScene().name != "Battle")
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        yield return null;
        manager = UnityEngine.Object.FindAnyObjectByType(RuntimeType("BattleManager"))
            as Component;
        Assert.That(manager, Is.Not.Null);
        object run = Get<object>("runManager");
        string completedRunId = Property<string>(run, "RunId");
        int nodeCount = Property<int>(Property<object>(run, "NodeMap"), "Count");
        Assert.That(IsRunMapOpen, Is.True);

        int resolved = 0;
        while (!Property<bool>(run, "IsCompleted"))
        {
            Assert.That(resolved, Is.LessThan(nodeCount),
                "Every committed route choice must advance toward the boss.");
            Assert.That(IsRunMapOpen, Is.True);
            object node = CurrentRunNode;
            string code = RuntimeField<string>(node, "Code");
            string type = RuntimeField<object>(node, "Type").ToString();
            int depth = Property<int>(run, "Depth");

            Button nodeButton = GameObject.Find("RunNode_" + code)
                .GetComponent<Button>();
            Assert.That(nodeButton.interactable, Is.True, code);
            nodeButton.onClick.Invoke();
            if (type == "Merchant")
                GameObject.Find("LeaveMerchantButton").GetComponent<Button>()
                    .onClick.Invoke();
            else if (type == "RandomEvent" &&
                !(bool)InvokeRuntimeStatic("RunEventService", "CanChoose", run, code, 0))
                GameObject.Find("RunNodeAlternativeButton").GetComponent<Button>()
                    .onClick.Invoke();
            else
                GameObject.Find("EnterRunNodeButton").GetComponent<Button>()
                    .onClick.Invoke();

            if (Property<bool>(node, "IsBattle"))
            {
                Assert.That(Get<object>("currentEnemy"), Is.Not.Null, code);
                Call("EndBattle", true);
                if (!Property<bool>(run, "IsCompleted"))
                {
                    Call("OpenRewardSelection");
                    Call("ChooseReward", 0);
                }
            }
            else if (type == "Relic")
            {
                Call("ChooseReward", 0);
            }

            resolved++;
            if (!Property<bool>(run, "IsCompleted"))
            {
                Assert.That(Property<int>(run, "Depth"), Is.EqualTo(depth + 1),
                    code + " must commit exactly once.");
                Assert.That(IsRunMapOpen, Is.True);
            }

            if (resolved == 2)
            {
                int savedDepth = Property<int>(run, "Depth");
                int savedRouteLength = Property<string[]>(run, "VisitedNodeCodes").Length;
                object inspection = InvokeRuntime(checkpoint, "Inspect", 100, 10);
                Assert.That(Property<bool>(inspection, "HasUsableState"), Is.True);
                EditorSceneManager.LoadSceneInPlayMode(
                    "Assets/Project/Scenes/MainMenu.unity",
                    new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                yield return null;
                Button continueButton = GameObject.Find("ContinueButton")
                    .GetComponent<Button>();
                Assert.That(continueButton.interactable, Is.True);
                continueButton.onClick.Invoke();
                deadline = Time.realtimeSinceStartup + 20f;
                while (SceneManager.GetActiveScene().name != "Battle")
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
                    yield return null;
                }
                yield return null;
                manager = UnityEngine.Object.FindAnyObjectByType(
                    RuntimeType("BattleManager")) as Component;
                Assert.That(manager, Is.Not.Null);
                run = Get<object>("runManager");
                Assert.That(Property<string>(run, "RunId"), Is.EqualTo(completedRunId));
                Assert.That(Property<int>(run, "Depth"), Is.EqualTo(savedDepth));
                Assert.That(Property<string[]>(run, "VisitedNodeCodes").Length,
                    Is.EqualTo(savedRouteLength));
                Assert.That(IsRunMapOpen, Is.True);
            }
        }

        Assert.That(resolved, Is.EqualTo(nodeCount));
        Assert.That(Property<bool>(SummaryUI, "IsOpen"), Is.True);
        Assert.That(Get<bool>("historyCommitted"), Is.True);
        Assert.That(Get<bool>("echoSaveFailed"), Is.False);
        Array records = (Array)InvokeRuntime(Get<object>("runHistoryStore"),
            "ReadRecent", 10000);
        Assert.That(records.Cast<object>().Count(record =>
            RuntimeField<string>(record, "RunId") == completedRunId), Is.EqualTo(1));
        Assert.That(Property<bool>(InvokeRuntime(checkpoint, "Inspect", 100, 10),
            "HasUsableState"), Is.False);
        object rankings = NewRuntimeObject("LocalLeaderboardService",
            Get<object>("runHistoryStore"));
        Array topTen = (Array)InvokeRuntime(rankings, "ReadTopTen");
        Assert.That(topTen.Cast<object>().Any(entry =>
            Property<string>(entry, "RunId") == completedRunId), Is.True);

        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        Assert.That(GameObject.Find("ContinueButton").GetComponent<Button>().interactable,
            Is.False, "A cleared run must not remain resumable.");
        GameObject.Find("RunHistoryButton").GetComponent<Button>().onClick.Invoke();
        Component history = UnityEngine.Object.FindAnyObjectByType(
            RuntimeType("RunHistoryUI")) as Component;
        Assert.That(Property<bool>(history, "IsOpen"), Is.True);
        InvokeRuntime(history, "Hide");
        GameObject.Find("TopRunsButton").GetComponent<Button>().onClick.Invoke();
        Component leaderboard = UnityEngine.Object.FindAnyObjectByType(
            RuntimeType("LocalLeaderboardUI")) as Component;
        Assert.That(Property<bool>(leaderboard, "IsOpen"), Is.True);
        InvokeRuntime(leaderboard, "Hide");

        GameObject.Find("NewRunButton").GetComponent<Button>().onClick.Invoke();
        deadline = Time.realtimeSinceStartup + 20f;
        while (SceneManager.GetActiveScene().name != "Battle")
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        yield return null;
        manager = UnityEngine.Object.FindAnyObjectByType(RuntimeType("BattleManager"))
            as Component;
        run = Get<object>("runManager");
        Assert.That(Property<string>(run, "RunId"), Is.Not.EqualTo(completedRunId));
        Assert.That(Property<int>(run, "Depth"), Is.EqualTo(1));
        Assert.That(Property<int>(Property<object>(run, "Modifiers"), "Count"),
            Is.Zero, "A fresh expedition must discard temporary run power.");
    }

    [UnityTest]
    public IEnumerator TC_E2E_003_ThreeExpeditionsUseRealCombatAndCommitTerminalResults()
    {
        // Automated gameplay coverage, not the human/standalone release gate.
        // Never force EndBattle, alter resources, skip nodes, or cancel cut-ins.
        for (int expedition = 0; expedition < 3; expedition++)
        {
            Call("RestartRun");
            object run = Get<object>("runManager");
            string runId = Property<string>(run, "RunId");
            int seed = Property<int>(run, "RunSeed");
            Assert.That(Property<int>(Property<object>(run, "Modifiers"), "Count"), Is.Zero);
            Assert.That(Property<int>(run, "Depth"), Is.EqualTo(1));
            int actions = 0;
            float deadline = Time.realtimeSinceStartup + 120f;
            while (Property<bool>(run, "IsActive"))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                    "Natural combat stalled; seed=" + seed);
                if (IsRunMapOpen)
                {
                    object node = CurrentRunNode;
                    string code = RuntimeField<string>(node, "Code");
                    string type = RuntimeField<object>(node, "Type").ToString();
                    Button nodeButton = GameObject.Find("RunNode_" + code).GetComponent<Button>();
                    Assert.That(nodeButton.interactable, Is.True);
                    nodeButton.onClick.Invoke();
                    string action = type == "Merchant" ? "LeaveMerchantButton" :
                        type == "RandomEvent" ? "RunNodeAlternativeButton" : "EnterRunNodeButton";
                    Button enter = GameObject.Find(action).GetComponent<Button>();
                    Assert.That(enter.interactable, Is.True, code);
                    enter.onClick.Invoke();
                }
                else if (Get<bool>("rewardSelectionOpen"))
                {
                    yield return AwaitRewardSelection();
                    int chosen = 0;
                    for (int i = 0; i < RewardChoices.Length; i++)
                        if (Property<object>(RewardChoices.GetValue(i), "EffectType").ToString() == "MaxHPFlat")
                            chosen = i;
                    Assert.That(RewardButton(chosen).interactable, Is.True);
                    RewardButton(chosen).onClick.Invoke();
                }
                else if (!Get<bool>("battleEnded") && Get<Button>("basicAttackButton").interactable)
                {
                    // Spend available Mana on the ordinary skill; otherwise attack.
                    Button skill = Get<Button>("skillButton");
                    (skill.interactable ? skill : Get<Button>("basicAttackButton")).onClick.Invoke();
                    actions++;
                    Assert.That(actions, Is.LessThan(200), "Action loop; seed=" + seed);
                }
                yield return null;
            }

            Assert.That(actions, Is.GreaterThan(0));
            Assert.That(Property<bool>(SummaryUI, "IsOpen"), Is.True);
            Assert.That(Get<bool>("historyCommitted"), Is.True);
            Assert.That(Get<bool>("echoSaveFailed"), Is.False);
            Assert.That(Property<int>(run, "ActionsTaken"), Is.EqualTo(actions));
            Array records = (Array)InvokeRuntime(Get<object>("runHistoryStore"), "ReadRecent", 10000);
            Assert.That(records.Cast<object>().Count(record =>
                RuntimeField<string>(record, "RunId") == runId), Is.EqualTo(1));
            object checkpoint = InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault");
            Assert.That(Property<bool>(InvokeRuntime(checkpoint, "Inspect", 100, 10), "HasUsableState"), Is.False);
            Debug.Log("NATURAL_COMBAT expedition=" + (expedition + 1) + " seed=" + seed +
                " cleared=" + Property<bool>(run, "IsCompleted") + " depth=" + Property<int>(run, "Depth") +
                " actions=" + actions + " hp=" + Property<int>(run, "CurrentHP"));
        }
    }

    [UnityTest]
    public IEnumerator TC_E2E_004_NaturalDefeatArchivesOnceAndNewRunResetsState()
    {
        Call("RestartRun");
        object run = Get<object>("runManager");
        string runId = Property<string>(run, "RunId");
        string code = RuntimeField<string>(CurrentRunNode, "Code");
        GameObject.Find("RunNode_" + code).GetComponent<Button>().onClick.Invoke();
        GameObject.Find("EnterRunNodeButton").GetComponent<Button>().onClick.Invoke();
        int guards = 0;
        float deadline = Time.realtimeSinceStartup + 60f;
        while (Property<bool>(run, "IsActive"))
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Natural defeat stalled.");
            Button guard = Get<Button>("guardButton");
            if (guard.interactable)
            {
                guard.onClick.Invoke();
                guards++;
                Assert.That(guards, Is.LessThan(100));
            }
            yield return null;
        }
        Assert.That(Property<bool>(run, "IsCompleted"), Is.False);
        Assert.That(Property<int>(run, "CurrentHP"), Is.Zero);
        Assert.That(Property<int>(run, "ActionsTaken"), Is.EqualTo(guards));
        Assert.That(Property<bool>(SummaryUI, "IsOpen"), Is.True);
        Assert.That(Get<bool>("historyCommitted"), Is.True);
        Array records = (Array)InvokeRuntime(Get<object>("runHistoryStore"), "ReadRecent", 10000);
        Assert.That(records.Cast<object>().Count(record =>
            RuntimeField<string>(record, "RunId") == runId), Is.EqualTo(1));
        object checkpoint = InvokeRuntimeStatic("ActiveRunCheckpointStore", "CreateDefault");
        Assert.That(Property<bool>(InvokeRuntime(checkpoint, "Inspect", 100, 10), "HasUsableState"), Is.False);
        Call("RestartRun");
        Assert.That(Property<string>(run, "RunId"), Is.Not.EqualTo(runId));
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(100));
        Assert.That(Property<int>(run, "ActionsTaken"), Is.Zero);
        Assert.That(Property<int>(Property<object>(run, "Modifiers"), "Count"), Is.Zero);
        Assert.That(IsRunMapOpen, Is.True);
    }

    [UnityTest]
    public IEnumerator TC_E2E_002_ConfirmAbandonReplacesCheckpointAndTemporaryBuild()
    {
        object oldRun = Get<object>("runManager");
        string oldId = Property<string>(oldRun, "RunId");
        Call("EndBattle", true);
        Call("OpenRewardSelection");
        Call("ChooseReward", 0);
        Assert.That(IsRunMapOpen, Is.True);
        Assert.That(Property<int>(Property<object>(oldRun, "Modifiers"), "Count"),
            Is.GreaterThan(0), "The abandoned run needs a real temporary reward.");
        object checkpoint = Get<object>("checkpointStore");
        Assert.That(Property<bool>(InvokeRuntime(checkpoint, "Inspect", 100, 10),
            "HasUsableState"), Is.True);

        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        Button continueButton = GameObject.Find("ContinueButton")
            .GetComponent<Button>();
        Assert.That(continueButton.interactable, Is.True);
        GameObject.Find("NewRunButton").GetComponent<Button>().onClick.Invoke();
        Assert.That(GameObject.Find("NewRunConfirmDialog").activeSelf, Is.True);
        GameObject.Find("ConfirmYes").GetComponent<Button>().onClick.Invoke();

        float deadline = Time.realtimeSinceStartup + 20f;
        while (SceneManager.GetActiveScene().name != "Battle")
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        yield return null;
        manager = UnityEngine.Object.FindAnyObjectByType(RuntimeType("BattleManager"))
            as Component;
        Assert.That(manager, Is.Not.Null);
        object freshRun = Get<object>("runManager");
        Assert.That(Property<string>(freshRun, "RunId"), Is.Not.EqualTo(oldId));
        Assert.That(Property<int>(freshRun, "Depth"), Is.EqualTo(1));
        Assert.That(Property<int>(Property<object>(freshRun, "Modifiers"), "Count"),
            Is.Zero);
        Assert.That(Property<string[]>(freshRun, "VisitedNodeCodes"), Is.Empty);
        object saved = InvokeRuntime(checkpoint, "LoadActiveRun");
        Assert.That(RuntimeField<string>(saved, "RunId"),
            Is.EqualTo(Property<string>(freshRun, "RunId")));
        Assert.That(IsRunMapOpen, Is.True);
    }

    [UnityTest]
    public IEnumerator TC_UNLOCK_004_MainMenuArchiveKeepsChoicesAndCopyInsidePanels()
    {
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/MainMenu.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        GameObject menuCard = GameObject.Find("MenuCard");
        Assert.That(menuCard, Is.Not.Null);
        Assert.That(EventSystem.current.GetComponents<BaseInputModule>().Any(module =>
            module.enabled && module.GetType().Name == "InputSystemUIInputModule"),
            Is.True, "Main Menu must use the project's Input System UI module.");
        foreach (Button button in menuCard.GetComponentsInChildren<Button>())
            AssertRectInside(button.GetComponent<RectTransform>(),
                menuCard.GetComponent<RectTransform>());
        GameObject.Find("EchoArchiveButton").GetComponent<Button>().onClick.Invoke();
        GameObject archive = GameObject.Find("EchoArchiveCard");
        Assert.That(archive, Is.Not.Null);
        foreach (TMP_Text label in archive.GetComponentsInChildren<TMP_Text>())
        {
            label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing, Is.False, label.name);
            AssertRectInside(label.rectTransform,
                archive.GetComponent<RectTransform>());
        }
        Canvas menuCanvas = menuCard.GetComponentInParent<Canvas>();
        RenderMode oldMode = menuCanvas.renderMode;
        Camera oldCamera = menuCanvas.worldCamera;
        float oldDistance = menuCanvas.planeDistance;
        var cameraObject = new GameObject("EchoArchiveCapture", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 540f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.transform.position = new Vector3(0f, 0f, -100f);
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        try
        {
            menuCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            menuCanvas.worldCamera = camera;
            menuCanvas.planeDistance = 1f;
            foreach (Vector2Int size in new[] {
                new Vector2Int(1920, 1080), new Vector2Int(1280, 1024) })
            {
                target = new RenderTexture(size.x, size.y, 24);
                target.Create();
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                AssertRectInside(menuCard.GetComponent<RectTransform>(),
                    menuCanvas.GetComponent<RectTransform>());
                AssertRectInside(archive.GetComponent<RectTransform>(),
                    menuCanvas.GetComponent<RectTransform>());
                camera.Render();
                RenderTexture.active = target;
                var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../echo-archive-" + size.x + "x" + size.y + ".png")),
                    pixels.EncodeToPNG());
                UnityEngine.Object.Destroy(pixels);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
                target = null;
            }
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
            menuCanvas.renderMode = oldMode;
            menuCanvas.worldCamera = oldCamera;
            menuCanvas.planeDistance = oldDistance;
            UnityEngine.Object.Destroy(cameraObject);
        }
    }

    [Test]
    public void TC_HISTORY_001_FinishedRunsPersistNewestFirstAndNeverDuplicate()
    {
        string directory = Path.Combine(Application.temporaryCachePath,
            "syngrava-history-" + Guid.NewGuid().ToString("N"));
        try
        {
            object store = NewRuntimeObject("RunHistoryStore", directory);
            object defeat = CreateFinishedHistorySnapshot(99101, false);
            object clear = CreateFinishedHistorySnapshot(99102, true);
            Assert.That((bool)InvokeRuntime(store, "RecordFinishedRun", defeat), Is.True);
            Assert.That((bool)InvokeRuntime(store, "RecordFinishedRun", defeat), Is.False);
            Assert.That((bool)InvokeRuntime(store, "RecordFinishedRun", clear), Is.True);

            object reopened = NewRuntimeObject("RunHistoryStore", directory);
            Array recent = (Array)InvokeRuntime(reopened, "ReadRecent", 10);
            Assert.That(recent.Length, Is.EqualTo(2));
            Assert.That(RuntimeField<string>(recent.GetValue(0), "RunId"),
                Is.EqualTo(Property<string>(clear, "RunId")));
            Assert.That(RuntimeField<string>(recent.GetValue(1), "RunId"),
                Is.EqualTo(Property<string>(defeat, "RunId")));
            Assert.That(RuntimeField<string>(recent.GetValue(0), "RouteText"),
                Does.Not.Contain("BATTLE_"));
            Assert.That(RuntimeField<int>(recent.GetValue(0), "ScoreModelVersion"),
                Is.EqualTo((int)RuntimeType("ScoreCalculator")
                    .GetField("ModelVersion").GetRawConstantValue()));
            Assert.That(RuntimeField<int>(recent.GetValue(0), "Score"),
                Is.GreaterThan(0));
            SetRuntimeField(recent.GetValue(0), "RunSeed", -4);
            Array afterMutation = (Array)InvokeRuntime(reopened, "ReadRecent", 10);
            Assert.That(RuntimeField<int>(afterMutation.GetValue(0), "RunSeed"),
                Is.EqualTo(99102), "UI must not mutate saved history.");
            Assert.That((bool)InvokeRuntime(reopened, "RecordFinishedRun", clear), Is.False);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TC_HISTORY_002_CorruptPrimaryUsesBackupAndUnwritablePathKeepsSource()
    {
        string directory = Path.Combine(Application.temporaryCachePath,
            "syngrava-history-" + Guid.NewGuid().ToString("N"));
        string blocked = Path.Combine(Application.temporaryCachePath,
            "syngrava-history-blocked-" + Guid.NewGuid().ToString("N"));
        try
        {
            object store = NewRuntimeObject("RunHistoryStore", directory);
            object first = CreateFinishedHistorySnapshot(99201, false);
            object second = CreateFinishedHistorySnapshot(99202, false);
            object third = CreateFinishedHistorySnapshot(99203, true);
            InvokeRuntime(store, "RecordFinishedRun", first);
            InvokeRuntime(store, "RecordFinishedRun", second);
            string primary = Property<string>(store, "HistoryPath");
            string backup = Property<string>(store, "BackupPath");
            Assert.That(File.Exists(backup), Is.True);
            File.WriteAllText(primary, "corrupt");
            Array recovered = (Array)InvokeRuntime(store, "ReadRecent", 10);
            Assert.That(recovered.Length, Is.EqualTo(1));
            Assert.That(RuntimeField<string>(recovered.GetValue(0), "RunId"),
                Is.EqualTo(Property<string>(first, "RunId")));
            InvokeRuntime(store, "RecordFinishedRun", third);
            Assert.That(((Array)InvokeRuntime(store, "ReadRecent", 10)).Length,
                Is.EqualTo(2));

            File.WriteAllText(blocked, "leave this file alone");
            object unwritable = NewRuntimeObject("RunHistoryStore", blocked);
            Assert.Throws<IOException>(() =>
                InvokeRuntime(unwritable, "RecordFinishedRun", first));
            Assert.That(File.ReadAllText(blocked), Is.EqualTo("leave this file alone"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            if (File.Exists(blocked)) File.Delete(blocked);
        }
    }

    [Test]
    public void TC_HISTORY_003_TerminalBattleCommitsBeforeCheckpointRemoval()
    {
        string runId = Property<string>(Get<object>("runManager"), "RunId");
        Call("EndBattle", false);
        Assert.That(Get<bool>("historyCommitted"), Is.True);
        Assert.That(Get<bool>("historySaveFailed"), Is.False);
        Array records = (Array)InvokeRuntime(Get<object>("runHistoryStore"),
            "ReadRecent", 10000);
        Assert.That(records.Cast<object>().Any(record =>
            RuntimeField<string>(record, "RunId") == runId), Is.True);
        object inspection = InvokeRuntime(Get<object>("checkpointStore"),
            "Inspect", 100, 10);
        Assert.That(Property<bool>(inspection, "HasUsableState"), Is.False);
    }

    [UnityTest]
    public IEnumerator TC_HISTORY_004_MenuPaginatesAndDisplaysFinishedRuns()
    {
        string directory = Path.Combine(Application.temporaryCachePath,
            "syngrava-history-" + Guid.NewGuid().ToString("N"));
        try
        {
            object store = NewRuntimeObject("RunHistoryStore", directory);
            for (int seed = 99301; seed <= 99307; seed++)
                InvokeRuntime(store, "RecordFinishedRun",
                    CreateFinishedHistorySnapshot(seed, seed == 99307));
            EditorSceneManager.LoadSceneInPlayMode(
                "Assets/Project/Scenes/MainMenu.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            Component historyUI = UnityEngine.Object.FindAnyObjectByType(
                RuntimeType("RunHistoryUI")) as Component;
            Assert.That(historyUI, Is.Not.Null);
            InvokeRuntime(historyUI, "Initialize", store);
            GameObject.Find("RunHistoryButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(Property<bool>(historyUI, "IsOpen"), Is.True);
            GameObject card = GameObject.Find("RunHistoryCard");
            Assert.That(card, Is.Not.Null);
            Assert.That(GameObject.Find("RunHistoryPage").GetComponent<TMP_Text>().text,
                Is.EqualTo("1 / 2"));
            Assert.That(GameObject.Find("RunHistoryCount").GetComponent<TMP_Text>().text,
                Does.Contain("7 FINISHED RUNS"));
            Assert.That(GameObject.Find("RunHistoryDetailBody").GetComponent<TMP_Text>().text,
                Does.Contain("99307"));
            Assert.That(GameObject.Find("RunHistoryDetailBody").GetComponent<TMP_Text>().text,
                Does.Contain("CONVERGENCE +"));
            foreach (TMP_Text label in card.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False, label.name);
                AssertRectInside(label.rectTransform,
                    card.GetComponent<RectTransform>());
            }

            Canvas canvas = card.GetComponentInParent<Canvas>();
            RenderMode oldMode = canvas.renderMode;
            Camera oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            var cameraObject = new GameObject("RunHistoryCapture", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 540f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -100f);
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                foreach (Vector2Int size in new[] {
                    new Vector2Int(1920, 1080), new Vector2Int(1280, 1024) })
                {
                    target = new RenderTexture(size.x, size.y, 24);
                    target.Create();
                    camera.targetTexture = target;
                    Canvas.ForceUpdateCanvases();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    AssertRectInside(card.GetComponent<RectTransform>(),
                        canvas.GetComponent<RectTransform>());
                    camera.Render();
                    RenderTexture.active = target;
                    var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,
                        "../run-history-" + size.x + "x" + size.y + ".png")),
                        pixels.EncodeToPNG());
                    UnityEngine.Object.Destroy(pixels);
                    RenderTexture.active = previous;
                    camera.targetTexture = null;
                    target.Release();
                    UnityEngine.Object.Destroy(target);
                    target = null;
                }
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                UnityEngine.Object.Destroy(cameraObject);
            }
            GameObject.Find("RunHistoryNext").GetComponent<Button>().onClick.Invoke();
            Assert.That(GameObject.Find("RunHistoryPage").GetComponent<TMP_Text>().text,
                Is.EqualTo("2 / 2"));
            Assert.That(GameObject.Find("RunHistoryDetailBody").GetComponent<TMP_Text>().text,
                Does.Contain("99301"));
            Assert.That(GameObject.Find("RunHistoryRow1"), Is.Null);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TC_SCORE_001_NodeBreakdownIsDeterministicAndCannotUseFarmableStats()
    {
        object clear = CreateFinishedHistorySnapshot(99401, true);
        object first = RuntimeProperty<object>(clear, "Score");
        object second = RuntimeType("ScoreCalculator")
            .GetMethod("Calculate", new[] { RuntimeType("RunSummarySnapshot") })
            .Invoke(null, new[] { clear });
        Assert.That(RuntimeProperty<int>(first, "Total"),
            Is.EqualTo(RuntimeProperty<int>(second, "Total")));
        Assert.That(RuntimeProperty<int>(first, "BossPoints"), Is.EqualTo(1500));
        Assert.That(RuntimeProperty<int>(first, "ConvergencePoints"), Is.Zero);

        Array nodes = RuntimeProperty<Array>(clear, "VisitedNodeTypes");
        Type nodeType = RuntimeType("StandardRunNodeType");
        var calculate = RuntimeType("ScoreCalculator").GetMethod("Calculate",
            new[] { nodeType.MakeArrayType(), typeof(int) });
        object withBonus = calculate.Invoke(null, new object[] { nodes, 3 });
        Assert.That(RuntimeProperty<int>(withBonus, "Total"),
            Is.EqualTo(RuntimeProperty<int>(first, "Total") + 150));
        Assert.Throws<TargetInvocationException>(() =>
            calculate.Invoke(null, new object[] { nodes, 999 }));

        object defeat = CreateFinishedHistorySnapshot(99402, false);
        Assert.That(RuntimeProperty<int>(RuntimeProperty<object>(defeat, "Score"),
            "BossPoints"), Is.Zero);
    }

    [Test]
    public void TC_SCORE_002_ConvergenceCapSurvivesRestoreAndFailedBattleEarnsNothing()
    {
        object run = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(run, "StartNewRun", 99403, 100, 5, 100, 10);
        object pattern = Enum.Parse(RuntimeType("ConvergenceType"), "EventHorizon");
        for (int index = 0; index < 5; index++)
            InvokeRuntime(run, "RecordConvergence", pattern);
        Assert.That(RuntimeProperty<int>(run, "CurrentBattleConvergences"),
            Is.EqualTo(3));
        object state = InvokeRuntime(run, "CaptureState");
        object restored = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(restored, "RestoreState", state, 100, 10);
        Assert.That(RuntimeProperty<int>(restored, "CurrentBattleConvergences"),
            Is.EqualTo(3));
        Assert.That((bool)InvokeRuntime(restored, "TryAdvanceDepth"), Is.True);
        Assert.That(RuntimeProperty<int>(restored, "ScoreableConvergences"),
            Is.EqualTo(3));
        Assert.That(RuntimeProperty<int>(restored, "CurrentBattleConvergences"),
            Is.Zero);

        object invalid = InvokeRuntime(restored, "CaptureState");
        SetRuntimeField(invalid, "CurrentBattleConvergences", 4);
        Assert.Throws<InvalidOperationException>(() =>
            InvokeRuntime(restored, "RestoreState", invalid, 100, 10));

        object next = RuntimeProperty<object>(restored, "CurrentNode");
        while (!RuntimeProperty<bool>(next, "IsBattle"))
        {
            Assert.That((bool)InvokeRuntime(restored, "TryAdvanceDepth"), Is.True);
            next = RuntimeProperty<object>(restored, "CurrentNode");
        }
        InvokeRuntime(restored, "RecordConvergence", pattern);
        InvokeRuntime(restored, "RecordConvergence", pattern);
        Assert.That((bool)InvokeRuntime(restored, "TryResolveCurrentNode",
            RuntimeField<object>(next, "Type"), RuntimeField<string>(next, "Code")),
            Is.True);
        Assert.That(RuntimeProperty<int>(restored, "ScoreableConvergences"),
            Is.EqualTo(5), "Each resolved battle must receive its own cap.");

        object defeated = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(defeated, "StartNewRun", 99404, 100, 5, 100, 10);
        for (int index = 0; index < 8; index++)
            InvokeRuntime(defeated, "RecordConvergence", pattern);
        InvokeRuntime(defeated, "MarkDefeated");
        object stats = NewRuntimeObject("RunStatController", 100, 10, 10, 0.5f,
            RuntimeProperty<object>(defeated, "Modifiers"));
        object outcome = Enum.Parse(RuntimeType("RunSummaryOutcome"), "Defeated");
        object summary = InvokeRuntimeStatic("RunSummarySnapshot", "Create",
            outcome, "Defeated", "Enemy", defeated, stats, null, null, false, false);
        Assert.That(RuntimeProperty<int>(RuntimeProperty<object>(summary, "Score"),
            "Total"), Is.Zero);
    }

    [Test]
    public void TC_SCORE_003_PreScoreHistoryRemainsReadableWithoutInventingPoints()
    {
        object snapshot = CreateFinishedHistorySnapshot(99405, true);
        Type recordType = RuntimeType("RunHistoryRecord");
        object record = recordType.GetMethod("FromSnapshot",
            BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new[] { snapshot });
        string json = JsonUtility.ToJson(record);
        json = System.Text.RegularExpressions.Regex.Replace(json,
            ",?\"(ScoreModelVersion|Score|BattlePoints|EventPoints|ElitePoints|BossPoints|ConvergencePoints)\":[0-9]+", "");
        object legacy = JsonUtility.FromJson(json, recordType);
        Assert.That(RuntimeField<int>(legacy, "ScoreModelVersion"), Is.Zero);
        Assert.That(RuntimeField<int>(legacy, "Score"), Is.Zero);
        var validate = RuntimeType("RunHistoryStore").GetMethod("ValidateRecord",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.DoesNotThrow(() => validate.Invoke(null, new[] { legacy }));
    }

    [Test]
    public void TC_SCORE_004_FallbackDoesNotScoreOrConsumeNamedPatternCap()
    {
        object run = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(run, "StartNewRun", 99406, 100, 5, 100, 10);
        object fallback = Enum.Parse(RuntimeType("ConvergenceType"), "UnstablePulse");
        object horizon = Enum.Parse(RuntimeType("ConvergenceType"), "EventHorizon");
        object reversal = Enum.Parse(RuntimeType("ConvergenceType"), "ReversalOrbit");
        for (int index = 0; index < 4; index++)
            InvokeRuntime(run, "RecordConvergence", fallback);
        Assert.That(RuntimeProperty<int>(run, "CurrentBattleConvergences"), Is.Zero);
        Assert.That(RuntimeProperty<int>(run, "UnstablePulseCount"), Is.EqualTo(4));
        InvokeRuntime(run, "RecordConvergence", horizon);
        InvokeRuntime(run, "RecordConvergence", reversal);
        InvokeRuntime(run, "RecordConvergence", fallback);
        InvokeRuntime(run, "RecordConvergence", horizon);
        InvokeRuntime(run, "RecordConvergence", reversal);
        Assert.That(RuntimeProperty<int>(run, "CurrentBattleConvergences"), Is.EqualTo(3));
        Assert.That(RuntimeProperty<int>(run, "ConvergencesTriggered"), Is.EqualTo(9));
        Assert.That((bool)InvokeRuntime(run, "TryAdvanceDepth"), Is.True);
        Assert.That(RuntimeProperty<int>(run, "ScoreableConvergences"), Is.EqualTo(3));
        object state = InvokeRuntime(run, "CaptureState");
        object restored = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(restored, "RestoreState", state, 100, 10);
        Assert.That(RuntimeProperty<int>(restored, "ScoreableConvergences"), Is.EqualTo(3));
        Assert.That(RuntimeProperty<int>(restored, "UnstablePulseCount"), Is.EqualTo(5));

        object fallbackOnly = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(fallbackOnly, "StartNewRun", 99407, 100, 5, 100, 10);
        InvokeRuntime(fallbackOnly, "RecordConvergence", fallback);
        object corrupt = InvokeRuntime(fallbackOnly, "CaptureState");
        SetRuntimeField(corrupt, "CurrentBattleConvergences", 1);
        Assert.Throws<InvalidOperationException>(() =>
            InvokeRuntime(restored, "RestoreState", corrupt, 100, 10));
    }

    [Test]
    public void TC_SCORE_005_LegacyCheckpointKeepsProgressWithoutGuessingBonuses()
    {
        object run = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(run, "StartNewRun", 99408, 100, 5, 100, 10);
        object horizon = Enum.Parse(RuntimeType("ConvergenceType"), "EventHorizon");
        object fallback = Enum.Parse(RuntimeType("ConvergenceType"), "UnstablePulse");
        InvokeRuntime(run, "RecordConvergence", horizon);
        InvokeRuntime(run, "RecordConvergence", fallback);
        Assert.That((bool)InvokeRuntime(run, "TryAdvanceDepth"), Is.True);
        object oldState = InvokeRuntime(run, "CaptureState");
        SetRuntimeField(oldState, "ScoreableConvergences", 2);
        string oldJson = JsonUtility.ToJson(oldState);
        oldJson = System.Text.RegularExpressions.Regex.Replace(oldJson,
            ",?\"ScoreModelVersion\":[0-9]+", "");
        object legacy = JsonUtility.FromJson(oldJson, RuntimeType("SeededRunState"));
        Assert.That(RuntimeField<int>(legacy, "ScoreModelVersion"), Is.Zero);
        object restored = NewRuntimeObject("RunManager",
            NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(restored, "RestoreState", legacy, 100, 10);
        Assert.That(RuntimeProperty<string>(restored, "RunId"),
            Is.EqualTo(RuntimeProperty<string>(run, "RunId")));
        Assert.That(RuntimeProperty<int>(restored, "Depth"), Is.EqualTo(2));
        Assert.That(RuntimeProperty<int>(restored, "CurrentHP"), Is.EqualTo(100));
        Assert.That(RuntimeProperty<int>(restored, "ConvergencesTriggered"), Is.EqualTo(2));
        Assert.That(RuntimeProperty<int>(restored, "ScoreableConvergences"), Is.Zero);
        Assert.That(RuntimeProperty<int>(restored, "CurrentBattleConvergences"), Is.Zero);
        object migrated = InvokeRuntime(restored, "CaptureState");
        Assert.That(RuntimeField<int>(migrated, "ScoreModelVersion"),
            Is.EqualTo((int)RuntimeType("ScoreCalculator").GetField("ModelVersion")
                .GetRawConstantValue()));
        SetRuntimeField(migrated, "ScoreModelVersion", 999);
        Assert.Throws<InvalidOperationException>(() =>
            InvokeRuntime(restored, "RestoreState", migrated, 100, 10));
        Assert.That(RuntimeProperty<string>(restored, "RunId"),
            Is.EqualTo(RuntimeProperty<string>(run, "RunId")));
    }

    [Test]
    public void TC_SCORE_006_PreviousScoreModelHistoryIsReadableButNotRanked()
    {
        object snapshot = CreateFinishedHistorySnapshot(99409, true);
        Type recordType = RuntimeType("RunHistoryRecord");
        object record = recordType.GetMethod("FromSnapshot",
            BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new[] { snapshot });
        SetRuntimeField(record, "ScoreModelVersion", 1);
        var validate = RuntimeType("RunHistoryStore").GetMethod("ValidateRecord",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.DoesNotThrow(() => validate.Invoke(null, new[] { record }));
        Array records = Array.CreateInstance(recordType, 1);
        records.SetValue(record, 0);
        Array ranked = (Array)RuntimeType("LocalLeaderboardService")
            .GetMethod("RankStandardRuns").Invoke(null, new object[] { records, 10 });
        Assert.That(ranked.Length, Is.Zero);
        Assert.That(RuntimeField<int>(record, "ScoreModelVersion"), Is.EqualTo(1));
    }

    [Test]
    public void TC_LDB_001_TopTenUsesScoreDepthBossDurationAndStableTies()
    {
        Type type = RuntimeType("RunHistoryRecord");
        Array records = Array.CreateInstance(type, 13);
        object outcome = Enum.Parse(RuntimeType("RunSummaryOutcome"), "Cleared");
        for (int index = 0; index < records.Length; index++)
        {
            object record = Activator.CreateInstance(type);
            SetRuntimeField(record, "RunId", Guid.NewGuid().ToString("N"));
            SetRuntimeField(record, "Outcome", outcome);
            SetRuntimeField(record, "ScoreModelVersion",
                (int)RuntimeType("ScoreCalculator").GetField("ModelVersion").GetRawConstantValue());
            SetRuntimeField(record, "Score", index < 5 ? 1000 : 200 - index);
            SetRuntimeField(record, "NodeCount", 10);
            SetRuntimeField(record, "NodesResolved", index == 0 ? 6 : 7);
            SetRuntimeField(record, "BossPoints", index == 2 || index == 3 ? 1500 : 0);
            SetRuntimeField(record, "ElapsedSeconds", index == 3 ? 50f : 200f);
            SetRuntimeField(record, "FinishedUtcTicks", DateTime.UtcNow.Ticks + index);
            records.SetValue(record, index);
        }
        // Equal score/depth/boss/duration resolves by earlier finish time.
        SetRuntimeField(records.GetValue(4), "FinishedUtcTicks",
            RuntimeField<long>(records.GetValue(1), "FinishedUtcTicks") + 1);
        // A pre-score archive entry is never ranked as a zero-point run.
        SetRuntimeField(records.GetValue(12), "ScoreModelVersion", 0);

        var rank = RuntimeType("LocalLeaderboardService")
            .GetMethod("RankStandardRuns");
        Array top = (Array)rank.Invoke(null, new object[] { records, 10 });
        Assert.That(top.Length, Is.EqualTo(10));
        Assert.That(RuntimeProperty<string>(top.GetValue(0), "RunId"),
            Is.EqualTo(RuntimeField<string>(records.GetValue(3), "RunId")));
        Assert.That(RuntimeProperty<string>(top.GetValue(1), "RunId"),
            Is.EqualTo(RuntimeField<string>(records.GetValue(2), "RunId")));
        Assert.That(RuntimeProperty<string>(top.GetValue(2), "RunId"),
            Is.EqualTo(RuntimeField<string>(records.GetValue(1), "RunId")));
        Assert.That(RuntimeProperty<string>(top.GetValue(3), "RunId"),
            Is.EqualTo(RuntimeField<string>(records.GetValue(4), "RunId")));
        Assert.That(RuntimeProperty<string>(top.GetValue(4), "RunId"),
            Is.EqualTo(RuntimeField<string>(records.GetValue(0), "RunId")));
        Assert.That(RuntimeProperty<int>(top.GetValue(0), "Rank"), Is.EqualTo(1));
        Assert.That(RuntimeProperty<int>(top.GetValue(9), "Rank"), Is.EqualTo(10));

        // A returned entry is detached from the mutable archive DTO.
        SetRuntimeField(records.GetValue(3), "Score", 1);
        Assert.That(RuntimeProperty<int>(top.GetValue(0), "Score"), Is.EqualTo(1000));

        // Exact gameplay/date ties remain stable regardless of enumeration order.
        object first = records.GetValue(1);
        object second = records.GetValue(4);
        SetRuntimeField(first, "RunId", "00000000000000000000000000000001");
        SetRuntimeField(second, "RunId", "00000000000000000000000000000002");
        SetRuntimeField(second, "FinishedUtcTicks",
            RuntimeField<long>(first, "FinishedUtcTicks"));
        object otherMode = records.GetValue(5);
        SetRuntimeField(otherMode, "Mode", "INFINITE");
        SetRuntimeField(otherMode, "Score", 99999);
        Array tied = Array.CreateInstance(type, 5);
        tied.SetValue(second, 0);
        tied.SetValue(first, 1);
        tied.SetValue(second, 2); // A duplicate identity may not claim another rank.
        tied.SetValue(otherMode, 3);
        tied.SetValue(records.GetValue(12), 4);
        Array stable = (Array)rank.Invoke(null, new object[] { tied, 10 });
        Assert.That(stable.Length, Is.EqualTo(2));
        Assert.That(RuntimeProperty<string>(stable.GetValue(0), "RunId"),
            Is.EqualTo(RuntimeField<string>(first, "RunId")));
        Assert.That(RuntimeProperty<string>(stable.GetValue(1), "RunId"),
            Is.EqualTo(RuntimeField<string>(second, "RunId")));
    }

    [UnityTest]
    public IEnumerator TC_LDB_002_MainMenuShowsReadOnlyTopTenAtBothResolutions()
    {
        string directory = Path.Combine(Application.temporaryCachePath,
            "syngrava-leaderboard-" + Guid.NewGuid().ToString("N"));
        try
        {
            object store = NewRuntimeObject("RunHistoryStore", directory);
            for (int seed = 99501; seed <= 99512; seed++)
                InvokeRuntime(store, "RecordFinishedRun",
                    CreateFinishedHistorySnapshot(seed, seed % 3 == 0));
            string historyPath = RuntimeProperty<string>(store, "HistoryPath");
            string backupPath = RuntimeProperty<string>(store, "BackupPath");
            byte[] originalHistory = File.ReadAllBytes(historyPath);
            byte[] originalBackup = File.ReadAllBytes(backupPath);
            EditorSceneManager.LoadSceneInPlayMode(
                "Assets/Project/Scenes/MainMenu.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            Component board = UnityEngine.Object.FindAnyObjectByType(
                RuntimeType("LocalLeaderboardUI")) as Component;
            Assert.That(board, Is.Not.Null);
            InvokeRuntime(board, "Initialize", store);
            GameObject opener = GameObject.Find("TopRunsButton");
            EventSystem.current.SetSelectedGameObject(opener);
            opener.GetComponent<Button>().onClick.Invoke();
            Assert.That(Property<bool>(board, "IsOpen"), Is.True);
            Assert.That(GameObject.Find("LeaderboardCount").GetComponent<TMP_Text>().text,
                Is.EqualTo("10 RANKED RUNS"));
            Assert.That(GameObject.Find("LeaderboardRow9"), Is.Not.Null);
            Assert.That(GameObject.Find("LeaderboardDetailBreakdown")
                .GetComponent<TMP_Text>().text, Does.Contain("BOSS +"));

            GameObject card = GameObject.Find("LocalLeaderboardCard");
            foreach (TMP_Text label in card.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False, label.name);
                AssertRectInside(label.rectTransform,
                    card.GetComponent<RectTransform>());
            }

            Canvas canvas = card.GetComponentInParent<Canvas>();
            RenderMode oldMode = canvas.renderMode;
            Camera oldCamera = canvas.worldCamera;
            float oldDistance = canvas.planeDistance;
            var cameraObject = new GameObject("LeaderboardCapture", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 540f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -100f);
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                foreach (Vector2Int size in new[] {
                    new Vector2Int(1920, 1080), new Vector2Int(1280, 1024) })
                {
                    target = new RenderTexture(size.x, size.y, 24);
                    target.Create();
                    camera.targetTexture = target;
                    Canvas.ForceUpdateCanvases();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    AssertRectInside(card.GetComponent<RectTransform>(),
                        canvas.GetComponent<RectTransform>());
                    foreach (TMP_Text label in card.GetComponentsInChildren<TMP_Text>())
                    {
                        label.ForceMeshUpdate();
                        Assert.That(label.isTextOverflowing, Is.False, label.name);
                        AssertRectInside(label.rectTransform,
                            card.GetComponent<RectTransform>());
                    }
                    camera.Render();
                    RenderTexture.active = target;
                    var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,
                        "../leaderboard-" + size.x + "x" + size.y + ".png")),
                        pixels.EncodeToPNG());
                    UnityEngine.Object.Destroy(pixels);
                    RenderTexture.active = previous;
                    camera.targetTexture = null;
                    target.Release();
                    UnityEngine.Object.Destroy(target);
                    target = null;
                }
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                UnityEngine.Object.Destroy(cameraObject);
            }
            GameObject secondRow = GameObject.Find("LeaderboardRow1");
            EventSystem.current.SetSelectedGameObject(secondRow);
            Assert.That(GameObject.Find("LeaderboardDetailTitle").GetComponent<TMP_Text>().text,
                Does.Contain("RANK #2"));
            Button lastRow = GameObject.Find("LeaderboardRow9").GetComponent<Button>();
            Assert.That(lastRow.navigation.selectOnDown.gameObject.name,
                Is.EqualTo("LeaderboardClose"));
            Assert.That(secondRow.GetComponent<Button>().colors.selectedColor.r,
                Is.LessThan(0.4f), "Keyboard focus must retain a dark background.");
            ExecuteEvents.Execute(secondRow, new BaseEventData(EventSystem.current),
                ExecuteEvents.cancelHandler);
            Assert.That(Property<bool>(board, "IsOpen"), Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(opener));
            CollectionAssert.AreEqual(originalHistory, File.ReadAllBytes(historyPath));
            CollectionAssert.AreEqual(originalBackup, File.ReadAllBytes(backupPath));

            object emptyStore = NewRuntimeObject("RunHistoryStore",
                Path.Combine(directory, "empty"));
            InvokeRuntime(board, "Initialize", emptyStore);
            InvokeRuntime(board, "Show");
            Assert.That(GameObject.Find("LeaderboardEmpty").GetComponent<TMP_Text>().text,
                Is.EqualTo("NO SCORED RUNS YET"));
            Assert.That(EventSystem.current.currentSelectedGameObject.name,
                Is.EqualTo("LeaderboardClose"));
            GameObject.Find("LeaderboardClose").GetComponent<Button>().onClick.Invoke();
            Assert.That(Property<bool>(board, "IsOpen"), Is.False);

            string corruptPath = RuntimeProperty<string>(emptyStore, "HistoryPath");
            Directory.CreateDirectory(Path.GetDirectoryName(corruptPath));
            File.WriteAllText(corruptPath, "invalid archive");
            InvokeRuntime(board, "Show");
            Assert.That(GameObject.Find("LeaderboardEmpty").GetComponent<TMP_Text>().text,
                Is.EqualTo("ARCHIVE UNAVAILABLE"));
            Assert.That(File.ReadAllText(corruptPath), Is.EqualTo("invalid archive"));
            InvokeRuntime(board, "Hide");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TC_LDB_003_LegacyModeSurvivesNewCommitsAndReopening()
    {
        string directory = Path.Combine(Application.temporaryCachePath,
            "syngrava-leaderboard-legacy-" + Guid.NewGuid().ToString("N"));
        try
        {
            object store = NewRuntimeObject("RunHistoryStore", directory);
            object first = CreateFinishedHistorySnapshot(99521, true);
            InvokeRuntime(store, "RecordFinishedRun", first);
            string path = RuntimeProperty<string>(store, "HistoryPath");
            Type envelopeType = RuntimeType("RunHistoryStore").GetNestedType(
                "Envelope", BindingFlags.NonPublic);
            object envelope = JsonUtility.FromJson(File.ReadAllText(path), envelopeType);
            string payload = RuntimeField<string>(envelope, "Payload");
            payload = System.Text.RegularExpressions.Regex.Replace(payload,
                ",\"(RulesetVersion|Mode)\":\"[^\"]*\"|,\"Cycle\":[0-9]+", "");
            SetRuntimeField(envelope, "Payload", payload);
            using (var sha = System.Security.Cryptography.SHA256.Create())
                SetRuntimeField(envelope, "Sha256", BitConverter.ToString(
                    sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)))
                    .Replace("-", "").ToLowerInvariant());
            File.WriteAllText(path, JsonUtility.ToJson(envelope));

            InvokeRuntime(store, "RecordFinishedRun",
                CreateFinishedHistorySnapshot(99522, false));
            object reopened = NewRuntimeObject("RunHistoryStore", directory);
            Array records = (Array)InvokeRuntime(reopened, "ReadRecent", 10);
            Assert.That(records.Length, Is.EqualTo(2),
                "Appending to legacy history must not silently fall back to its backup.");
            Assert.That(RuntimeField<string>(records.GetValue(1), "RunId"),
                Is.EqualTo(RuntimeProperty<string>(first, "RunId")));
            Assert.That(RuntimeField<int>(records.GetValue(1), "Score"),
                Is.EqualTo(RuntimeProperty<int>(RuntimeProperty<object>(first, "Score"), "Total")));

            InvokeRuntime(reopened, "RecordFinishedRun",
                CreateFinishedHistorySnapshot(99523, false));
            Array ranked = (Array)InvokeRuntime(NewRuntimeObject(
                "LocalLeaderboardService", NewRuntimeObject("RunHistoryStore", directory)),
                "ReadTopTen");
            Assert.That(ranked.Length, Is.EqualTo(3));
            Assert.That(RuntimeProperty<string>(ranked.GetValue(0), "RunId"),
                Is.EqualTo(RuntimeProperty<string>(first, "RunId")));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
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

    [UnityTest]
    public IEnumerator TC_SET_001A_DestroyOpenSettingsRestoresTimeScale()
    {
        Component settings = SettingsUI;
        Assert.That(settings, Is.Not.Null);
        float originalTimeScale = Time.timeScale;
        Button settingsButton = Property<Button>(settings, "SettingsButton");
        settingsButton.onClick.Invoke();
        Assert.That(Time.timeScale, Is.Zero);

        UnityEngine.Object.Destroy(settings);
        yield return null;

        Assert.That(Time.timeScale, Is.EqualTo(originalTimeScale));
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
            Is.GreaterThanOrEqualTo(3)
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
            labels.Any(label => label.text == "Reduce Motion"),
            Is.True
        );
    }

    [UnityTest]
    public IEnumerator TC_SET_003_ReturnToMenuRequiresSafeSaveAndContinueRestoresIt()
    {
        Component settings = SettingsUI;
        GameObject panel = Property<GameObject>(settings, "SettingsPanel");
        Button returnButton = panel.transform.Find(
            "SettingsCard/ReturnToMainMenuButton").GetComponent<Button>();
        object store = Get<object>("checkpointStore");
        string path = RuntimeProperty<string>(store, "CheckpointPath");

        InvokeRuntime(settings, "OpenSettings");
        returnButton.onClick.Invoke();
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Battle"));
        Assert.That(panel.activeSelf, Is.True);
        Assert.That(panel.transform.Find("SettingsCard/SettingsSaveStatus")
            .GetComponent<TMP_Text>().text, Does.Contain("FINISH THIS NODE"));
        InvokeRuntime(settings, "CloseSettings");

        Call("RestartRun");
        Assert.That(IsRunMapOpen, Is.True);
        string earlierSave = File.ReadAllText(path);
        object run = Get<object>("runManager");
        InvokeRuntime(run, "SetResources", 73, 4, 100, 10);
        Call("SyncLegacyRunStateFields");
        string runId = Property<string>(run, "RunId");

        InvokeRuntime(settings, "OpenSettings");
        LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex(
                "^Active Run checkpoint gagal disimpan \\(return to Main Menu\\)"));
        using (var blocked = new FileStream(path + ".tmp",
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            returnButton.onClick.Invoke();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Battle"));
            Assert.That(panel.activeSelf, Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo(earlierSave));
            Assert.That(panel.transform.Find("SettingsCard/SettingsSaveStatus")
                .GetComponent<TMP_Text>().text, Does.Contain("SAVE FAILED"));
        }

        returnButton.onClick.Invoke();
        yield return null;
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        object saved = InvokeRuntime(store, "LoadActiveRun");
        Assert.That(RuntimeField<string>(saved, "RunId"), Is.EqualTo(runId));
        Assert.That(RuntimeField<int>(saved, "CurrentHP"), Is.EqualTo(73));
        Assert.That(RuntimeField<int>(saved, "CurrentMana"), Is.EqualTo(4));
        Assert.That(File.ReadAllText(path), Is.Not.EqualTo(earlierSave));
        Assert.That(Time.timeScale, Is.EqualTo(10f));

        Button continueButton = GameObject.Find("ContinueButton").GetComponent<Button>();
        Assert.That(continueButton.interactable, Is.True);
        continueButton.onClick.Invoke();
        float deadline = Time.realtimeSinceStartup + 20f;
        while (SceneManager.GetActiveScene().name != "Battle")
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        yield return null;
        manager = UnityEngine.Object.FindAnyObjectByType(
            RuntimeType("BattleManager")) as Component;
        Assert.That(manager, Is.Not.Null);
        object restored = Get<object>("runManager");
        Assert.That(Property<string>(restored, "RunId"), Is.EqualTo(runId));
        Assert.That(Property<int>(restored, "CurrentHP"), Is.EqualTo(73));
        Assert.That(Property<int>(restored, "CurrentMana"), Is.EqualTo(4));
        Assert.That(IsRunMapOpen, Is.True);
    }

    [Test]
    public void TC_SET_004_SettingsRepositoryPersistsIndependentValues()
    {
        string databasePath = Path.Combine(Application.temporaryCachePath,
            "syngrava-settings-" + Guid.NewGuid().ToString("N") + ".db");
        object first = NewRuntimeObject("UserSettingsStore", databasePath);
        try
        {
            Assert.That(InvokeRuntime(first, "Read", "reduce_motion"), Is.Null);
            InvokeRuntime(first, "Write", "reduce_motion", "1");
            InvokeRuntime(first, "Write", "master_volume", "0.50");
            object reopened = NewRuntimeObject("UserSettingsStore", databasePath);
            Assert.That(InvokeRuntime(reopened, "Read", "reduce_motion"), Is.EqualTo("1"));
            Assert.That(InvokeRuntime(reopened, "Read", "master_volume"), Is.EqualTo("0.50"));
            InvokeRuntime(reopened, "Write", "reduce_motion", "0");
            Assert.That(InvokeRuntime(first, "Read", "reduce_motion"), Is.EqualTo("0"));
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Test]
    public void TC_SET_005_ReduceMotionChangesCombatPresentationAndPersists()
    {
        Component settings = SettingsUI;
        var states = (System.Collections.Generic.Dictionary<string, bool>)
            settings.GetType().GetField("toggleStates", Fields).GetValue(settings);
        bool original = states["1:0"];
        object store = InvokeRuntimeStatic("UserSettingsStore", "CreateDefault");
        GameObject panel = Property<GameObject>(settings, "SettingsPanel");

        try
        {
            InvokeRuntime(settings, "OpenSettings");
            panel.transform.Find("SettingsCard/SettingsCategoryPanel/SettingsCategory_1")
                .GetComponent<Button>().onClick.Invoke();
            Button toggle = panel.transform.Find(
                "SettingsCard/SettingsContentPanel/SettingsRow_1_0/SettingsToggle_1_0")
                .GetComponent<Button>();
            if (!states["1:0"]) toggle.onClick.Invoke();
            Assert.That(Property<bool>(settings, "MotionEffectsEnabled"), Is.False);
            Assert.That(InvokeRuntime(store, "Read", "reduce_motion"), Is.EqualTo("1"));

            Component visuals = manager.GetComponent(RuntimeType("BattleVisuals"));
            RectTransform player = (RectTransform)visuals.GetType()
                .GetField("playerVisual", Fields).GetValue(visuals);
            Vector2 before = player.anchoredPosition;
            InvokeRuntime(visuals, "PlayPlayerAttack");
            Assert.That(player.anchoredPosition, Is.EqualTo(before));

            Component orbit = UnityEngine.Object.FindAnyObjectByType(
                RuntimeType("OrbitUIAnimator")) as Component;
            Assert.That(orbit, Is.Not.Null);
            RectTransform orbitPanel = (RectTransform)orbit.GetType()
                .GetField("orbitPanel", Fields).GetValue(orbit);
            Vector2 resting = (Vector2)orbit.GetType()
                .GetField("stageRestingPosition", Fields).GetValue(orbit);
            InvokeRuntime(orbit, "PlayStageEntrance");
            Assert.That(orbitPanel.anchoredPosition, Is.EqualTo(resting));

            toggle.onClick.Invoke();
            Assert.That(Property<bool>(settings, "MotionEffectsEnabled"), Is.True);
            Assert.That(InvokeRuntime(store, "Read", "reduce_motion"), Is.EqualTo("0"));
        }
        finally
        {
            InvokeRuntime(store, "Write", "reduce_motion", original ? "1" : "0");
            states["1:0"] = original;
            InvokeRuntime(settings, "CloseSettings");
        }
    }

    [Test]
    public void TC_SET_006_MasterVolumeControlChangesListenerAndSQLite()
    {
        Component settings = SettingsUI;
        GameObject panel = Property<GameObject>(settings, "SettingsPanel");
        object store = InvokeRuntimeStatic("UserSettingsStore", "CreateDefault");
        string previousSetting = (string)InvokeRuntime(store, "Read", "master_volume");
        float previousVolume = AudioListener.volume;
        try
        {
            InvokeRuntime(settings, "OpenSettings");
            panel.transform.Find("SettingsCard/SettingsCategoryPanel/SettingsCategory_3")
                .GetComponent<Button>().onClick.Invoke();
            panel.transform.Find(
                "SettingsCard/SettingsContentPanel/SettingsRow_3_0/SettingsValue_3_0")
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(AudioListener.volume, Is.Not.EqualTo(previousVolume));
            string saved = (string)InvokeRuntime(store, "Read", "master_volume");
            Assert.That(float.Parse(saved,
                System.Globalization.CultureInfo.InvariantCulture),
                Is.EqualTo(AudioListener.volume).Within(0.001f));
        }
        finally
        {
            InvokeRuntime(store, "Write", "master_volume",
                previousSetting ?? previousVolume.ToString("0.00",
                    System.Globalization.CultureInfo.InvariantCulture));
            AudioListener.volume = previousVolume;
            InvokeRuntime(settings, "CloseSettings");
        }
    }

    [UnityTest]
    public IEnumerator TC_SET_007_SettingsGearIsClickableAboveTheOpenRunMap()
    {
        Call("RestartRun");
        yield return null;

        Component settings = SettingsUI;
        Button gear = Property<Button>(settings, "SettingsButton");
        GameObject mapOverlay = Property<GameObject>(RunMapUI, "Overlay");
        Assert.That(mapOverlay.activeInHierarchy, Is.True);
        Assert.That(gear.gameObject.activeInHierarchy, Is.True);
        Assert.That(gear.transform.GetSiblingIndex(),
            Is.GreaterThan(mapOverlay.transform.GetSiblingIndex()));

        Canvas canvas = gear.GetComponentInParent<Canvas>();
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas.worldCamera;
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(
                eventCamera, gear.transform.position)
        };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(pointer, hits);
        Assert.That(hits.Count, Is.GreaterThan(0));
        Assert.That(hits[0].gameObject, Is.EqualTo(gear.gameObject));

        gear.onClick.Invoke();
        Assert.That(Property<bool>(settings, "IsOpen"), Is.True);
        Assert.That(Time.timeScale, Is.Zero);
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
        int criticalHitsBefore = Property<int>(Get<object>("runManager"), "CriticalHits");
        Click("skillButton");
        bool critical = Property<int>(Get<object>("runManager"), "CriticalHits") >
            criticalHitsBefore;
        int expectedSkillDamage = Property<int>(skill, "Power");
        if (critical)
            expectedSkillDamage = (int)InvokeRuntimeStatic("BattleTurnRules",
                "ApplyCritical", expectedSkillDamage);
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(990 - expectedSkillDamage));
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
        int criticalBefore = Property<int>(Get<object>("runManager"), "CriticalHits");
        Click("skillButton");
        Click("skillButton");
        bool critical = Property<int>(Get<object>("runManager"), "CriticalHits") >
            criticalBefore;
        int actualSkillDamage = critical
            ? (int)InvokeRuntimeStatic("BattleTurnRules", "ApplyCritical", power)
            : power;
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(
            1000 - actualSkillDamage - Mathf.RoundToInt(actualSkillDamage * 0.3f)));
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
        Assert.That(SummaryUI, Is.Not.Null);
        Assert.That(Property<bool>(SummaryUI, "IsOpen"), Is.True);
        Assert.That(Property<TMP_Text>(SummaryUI, "TitleText").text,
            Is.EqualTo("THREAD SEVERED"));
        Assert.That(Property<TMP_Text>(SummaryUI, "StatusText").text,
            Is.EqualTo("RUN DEFEATED"));
        Set("runMaxHPBonus", 15);
        Click("retryButton");
        Assert.That(Property<bool>(SummaryUI, "IsOpen"), Is.False);
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
            int rewardIndex = 0;
            for (int index = 0; index < RewardChoices.Length; index++)
            {
                if (Property<object>(RewardChoices.GetValue(index), "EffectType").ToString() !=
                    "AnomalyFrozenOrbit")
                {
                    rewardIndex = index;
                    break;
                }
            }
            RewardButton(rewardIndex).onClick.Invoke();
            AdvanceMapUntilBattleStarts();
        }

        yield return new WaitForSecondsRealtime(1f);
        Assert.That(Get<TMP_Text>("resultTitleText").text, Is.EqualTo("RUN COMPLETE"));
        Assert.That(Get<bool>("runCompleted"), Is.True);
        Assert.That(Get<bool>("rewardSelectionOpen"), Is.False);
        Assert.That(Get<GameObject>("rewardPanel").activeSelf, Is.False);
        Assert.That(Get<Button>("nextBattleButton").gameObject.activeSelf, Is.False);
        Assert.That(SummaryUI, Is.Not.Null);
        Assert.That(Property<bool>(SummaryUI, "IsOpen"), Is.True);
        Assert.That(Property<TMP_Text>(SummaryUI, "TitleText").text,
            Is.EqualTo("THREAD OF FATE SECURED"));
        Assert.That(Property<TMP_Text>(SummaryUI, "StatusText").text,
            Is.EqualTo("RUN CLEARED"));
        Assert.That(Property<TMP_Text>(SummaryUI, "EchoValueText").text,
            Does.StartWith("+"));
        Assert.That(GameObject.Find("EchoFragmentIcon").GetComponent<Image>().sprite,
            Is.Not.Null, "Echo Fragment should render as an imported sprite.");
        StringAssert.DoesNotContain("BATTLE_", Property<TMP_Text>(SummaryUI, "RouteText").text);
        StringAssert.DoesNotContain("EVENT_", Property<TMP_Text>(SummaryUI, "RouteText").text);
        TMP_Text runIdentity = SummaryUI.transform.Find(
            "RunSummaryOverlay/RunSummaryFit/RunSummaryCard/RunSummarySeed"
        ).GetComponent<TMP_Text>();
        object completedRun = Get<object>("runManager");
        Assert.That(runIdentity.gameObject.activeInHierarchy, Is.True);
        StringAssert.Contains(
            $"SEED  {Property<int>(completedRun, "RunSeed")}",
            runIdentity.text
        );
        StringAssert.Contains(
            $"RUN ID  {Property<string>(completedRun, "RunId")}",
            runIdentity.text
        );
        yield return CaptureRunSummaryLayout();
        int mana = Get<int>("playerMana");
        Call("UseAnalyze");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(mana));
        Property<Button>(SummaryUI, "ActionButton").onClick.Invoke();
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

        object reversedState = InvokeRuntime(reversedRun, "CaptureState");
        Assert.That(RuntimeField<string>(reversedState, "RunId"),
            Is.Not.EqualTo(RuntimeField<string>(captured, "RunId")),
            "Separate runs with the same seed need distinct reward identities.");
        SetRuntimeField(reversedState, "RunId", RuntimeField<string>(captured, "RunId"));

        Assert.That(
            JsonUtility.ToJson(reversedState),
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
    public void TC_SAVE_001_ActiveCheckpointRoundTripsCommittedRun()
    {
        string directory = Path.Combine(
            Application.temporaryCachePath,
            "syngrava-checkpoint-" + Guid.NewGuid().ToString("N")
        );
        object store = NewRuntimeObject("ActiveRunCheckpointStore", directory);

        try
        {
            object run = NewRuntimeObject(
                "RunManager",
                NewRuntimeObject("RunModifierCollection")
            );
            InvokeRuntime(run, "StartNewRun", 81001, 91, 6, 100, 10);
            InvokeRuntime(run, "AddEmbers", 17);
            Assert.That(
                (bool)InvokeRuntime(run, "TryAdvanceDepth"),
                Is.True
            );

            string expected = JsonUtility.ToJson(
                InvokeRuntime(run, "CaptureState")
            );
            InvokeRuntime(store, "SaveActiveRun", run);

            Assert.That(
                RuntimeProperty<bool>(store, "HasCheckpoint"),
                Is.True
            );

            object restored = NewRuntimeObject(
                "RunManager",
                NewRuntimeObject("RunModifierCollection")
            );
            InvokeRuntime(store, "RestoreActiveRun", restored, 100, 10);

            Assert.That(
                JsonUtility.ToJson(InvokeRuntime(restored, "CaptureState")),
                Is.EqualTo(expected)
            );
        }
        finally
        {
            InvokeRuntime(store, "DeleteCheckpoint");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TC_SAVE_002_CheckpointRejectsUnsafeRunBoundaries()
    {
        string directory = Path.Combine(
            Application.temporaryCachePath,
            "syngrava-checkpoint-" + Guid.NewGuid().ToString("N")
        );
        object store = NewRuntimeObject("ActiveRunCheckpointStore", directory);

        try
        {
            object run = NewRuntimeObject(
                "RunManager",
                NewRuntimeObject("RunModifierCollection")
            );
            InvokeRuntime(run, "StartNewRun", 81002, 100, 5, 100, 10);
            object node = RuntimeProperty<object>(run, "CurrentNode");
            Assert.That(
                (bool)InvokeRuntime(
                    run,
                    "TryEnterNode",
                    RuntimeField<string>(node, "Code")
                ),
                Is.True
            );

            Assert.Throws<InvalidOperationException>(
                () => InvokeRuntime(store, "SaveActiveRun", run)
            );
            Assert.That(
                RuntimeProperty<bool>(store, "HasCheckpoint"),
                Is.False
            );

            Assert.That(
                (bool)InvokeRuntime(
                    run,
                    "TryResolveCurrentNode",
                    RuntimeField<object>(node, "Type"),
                    RuntimeField<string>(node, "Code")
                ),
                Is.True
            );
            InvokeRuntime(store, "SaveActiveRun", run);
            string safeJson = File.ReadAllText(
                RuntimeProperty<string>(store, "CheckpointPath")
            );

            InvokeRuntime(run, "MarkDefeated");
            Assert.Throws<InvalidOperationException>(
                () => InvokeRuntime(store, "SaveActiveRun", run)
            );
            Assert.That(
                File.ReadAllText(
                    RuntimeProperty<string>(store, "CheckpointPath")
                ),
                Is.EqualTo(safeJson),
                "Rejected saves must not overwrite the last safe checkpoint."
            );
        }
        finally
        {
            InvokeRuntime(store, "DeleteCheckpoint");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TC_SAVE_003_CorruptCheckpointCannotMutateRuntimeState()
    {
        string directory = Path.Combine(
            Application.temporaryCachePath,
            "syngrava-checkpoint-" + Guid.NewGuid().ToString("N")
        );
        object store = NewRuntimeObject("ActiveRunCheckpointStore", directory);

        try
        {
            object source = NewRuntimeObject(
                "RunManager",
                NewRuntimeObject("RunModifierCollection")
            );
            InvokeRuntime(source, "StartNewRun", 81003, 82, 4, 100, 10);
            InvokeRuntime(store, "SaveActiveRun", source);

            string path = RuntimeProperty<string>(store, "CheckpointPath");
            File.WriteAllText(path, "{\"FileVersion\":1,\"StateJson\":\"tampered\"}");

            object target = NewRuntimeObject(
                "RunManager",
                NewRuntimeObject("RunModifierCollection")
            );
            InvokeRuntime(target, "StartNewRun", 91003, 55, 2, 100, 10);
            string before = JsonUtility.ToJson(
                InvokeRuntime(target, "CaptureState")
            );

            Assert.Throws<InvalidDataException>(
                () => InvokeRuntime(
                    store,
                    "RestoreActiveRun",
                    target,
                    100,
                    10
                )
            );
            Assert.That(
                JsonUtility.ToJson(InvokeRuntime(target, "CaptureState")),
                Is.EqualTo(before)
            );
        }
        finally
        {
            InvokeRuntime(store, "DeleteCheckpoint");
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [UnityTest]
    public IEnumerator TC_SAVE_004_BattleLifecycleCommitsAndClearsCheckpoint()
    {
        Call("RestartRun");
        object activeRun = Get<object>("runManager");
        object store = Get<object>("checkpointStore");
        Assert.That(
            RuntimeProperty<bool>(store, "HasCheckpoint"),
            Is.True
        );

        object initial = InvokeRuntime(store, "LoadActiveRun");
        Assert.That(RuntimeField<int>(initial, "Depth"), Is.EqualTo(1));
        Assert.That(RuntimeField<bool>(initial, "NodeEntered"), Is.False);
        Assert.That(
            JsonUtility.ToJson(initial),
            Is.EqualTo(JsonUtility.ToJson(InvokeRuntime(activeRun, "CaptureState"))),
            "Restart must replace any previous checkpoint with the new run."
        );

        EnterOrResolveCurrentMapNode();
        Assert.That(Property<bool>(activeRun, "NodeEntered"), Is.True);
        Call("EndBattle", true);
        Call("OpenRewardSelection");
        Call("ChooseReward", 0);
        yield return null;

        object committed = InvokeRuntime(store, "LoadActiveRun");
        object liveState = InvokeRuntime(activeRun, "CaptureState");
        float committedElapsed = RuntimeField<float>(committed, "ElapsedSeconds");
        float liveElapsed = RuntimeField<float>(liveState, "ElapsedSeconds");
        Assert.That(committedElapsed, Is.LessThanOrEqualTo(liveElapsed));
        Assert.That(liveElapsed - committedElapsed, Is.LessThan(1f));
        SetRuntimeField(committed, "ElapsedSeconds", liveElapsed);
        Assert.That(
            JsonUtility.ToJson(committed),
            Is.EqualTo(JsonUtility.ToJson(liveState))
        );
        Assert.That(RuntimeField<int>(committed, "Depth"), Is.EqualTo(2));
        Assert.That(RuntimeField<bool>(committed, "NodeEntered"), Is.False);

        AdvanceMapUntilBattleStarts();
        Call("EndBattle", false);
        Assert.That(
            RuntimeProperty<bool>(store, "HasCheckpoint"),
            Is.False,
            "A defeated run must not remain resumable."
        );
    }

    [UnityTest]
    public IEnumerator TC_SAVE_005_CheckpointFailureIsVisibleAndCanBeRetried()
    {
        Call("RestartRun");
        object store = Get<object>("checkpointStore");
        object run = Get<object>("runManager");
        string checkpointPath = Property<string>(store, "CheckpointPath");
        string savedBeforeFailure = File.ReadAllText(checkpointPath);
        string blocked = Path.Combine(Application.temporaryCachePath,
            "syngrava-checkpoint-blocked-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(blocked, "test sentinel");
        try
        {
            Set("checkpointStore", NewRuntimeObject("ActiveRunCheckpointStore", blocked));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "^Active Run checkpoint gagal disimpan"));
            Call("SaveActiveRunCheckpoint", "test blocked path");
            Call("OpenRunMap");
            GameObject warning = Property<GameObject>(RunMapUI, "CheckpointWarning");
            Button retry = Property<Button>(RunMapUI, "RetryCheckpointButton");
            Assert.That(warning.activeInHierarchy, Is.True);
            Assert.That(retry.interactable, Is.True);
            Assert.That(File.ReadAllText(checkpointPath), Is.EqualTo(savedBeforeFailure));
            Assert.That(File.ReadAllText(blocked), Is.EqualTo("test sentinel"));
            yield return new WaitForSecondsRealtime(.4f);
            yield return CaptureCheckpointWarning(warning);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "^Active Run checkpoint gagal disimpan"));
            retry.onClick.Invoke();
            Assert.That(warning.activeSelf, Is.True, "A failed retry must retain its warning.");
            Assert.That(retry.interactable, Is.True);
            Assert.That(File.ReadAllText(checkpointPath), Is.EqualTo(savedBeforeFailure));

            Set("checkpointStore", store);
            retry.onClick.Invoke();
            Assert.That(warning.activeSelf, Is.False);
            Assert.That(Get<bool>("checkpointSaveFailed"), Is.False);
            object committed = InvokeRuntime(store, "LoadActiveRun");
            Assert.That(RuntimeField<string>(committed, "RunId"),
                Is.EqualTo(Property<string>(run, "RunId")));
            Assert.That(RuntimeField<int>(committed, "Depth"),
                Is.EqualTo(Property<int>(run, "Depth")));
            Assert.That(RuntimeField<bool>(committed, "NodeEntered"), Is.False);

            string savedAfterRetry = File.ReadAllText(checkpointPath);
            InvokeRuntime(run, "AddEmbers", 5);
            retry.onClick.Invoke();
            Assert.That(File.ReadAllText(checkpointPath), Is.EqualTo(savedAfterRetry),
                "A stale click on the hidden retry button must not write another checkpoint.");
        }
        finally
        {
            Set("checkpointStore", store);
            if (File.Exists(blocked)) File.Delete(blocked);
        }
    }

    private IEnumerator CaptureCheckpointWarning(GameObject warning)
    {
        Canvas canvas = RunMapUI.GetComponent<Canvas>();
        RenderMode oldMode = canvas.renderMode;
        Camera oldCamera = canvas.worldCamera;
        float oldDistance = canvas.planeDistance;
        var cameraObject = new GameObject("CheckpointWarningCapture", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 540f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.transform.position = new Vector3(0f, 0f, -100f);
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            foreach (Vector2Int size in new[] {
                new Vector2Int(1920, 1080), new Vector2Int(1280, 1024) })
            {
                target = new RenderTexture(size.x, size.y, 24);
                target.Create();
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                RectTransform warningRect = warning.GetComponent<RectTransform>();
                AssertRectInside(warningRect, warning.transform.parent.GetComponent<RectTransform>());
                foreach (TMP_Text label in warning.GetComponentsInChildren<TMP_Text>())
                {
                    label.ForceMeshUpdate();
                    Assert.That(label.isTextOverflowing, Is.False, label.name);
                    AssertRectInside(label.rectTransform, warningRect);
                }
                AssertRectInside(Property<Button>(RunMapUI, "RetryCheckpointButton")
                    .GetComponent<RectTransform>(), warningRect);
                camera.Render();
                RenderTexture.active = target;
                var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../checkpoint-warning-" + size.x + "x" + size.y + ".png")), pixels.EncodeToPNG());
                UnityEngine.Object.Destroy(pixels);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
                target = null;
            }
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = oldDistance;
            UnityEngine.Object.Destroy(cameraObject);
        }
    }

    [Test]
    public void TC_RECOVERY_001_BackupRestoresPreviousBoundaryAndSurvivesRecovery()
    {
        object store = Get<object>("checkpointStore");
        Call("RestartRun");
        object run = Get<object>("runManager");
        string first = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
        InvokeRuntime(run, "TryAdvanceDepth");
        InvokeRuntime(run, "AddEmbers", 15);
        InvokeRuntime(store, "SaveActiveRun", run);
        Assert.That(RuntimeProperty<bool>(store, "HasBackup"), Is.True);
        string path = RuntimeProperty<string>(store, "CheckpointPath");
        File.WriteAllText(path, "corrupted");
        object inspection = InvokeRuntime(store, "Inspect", 100, 10);
        Assert.That(RuntimeProperty<object>(inspection, "Status").ToString(),
            Is.EqualTo("RecoverableBackup"));
        Assert.That(Property<int>(run, "Depth"), Is.EqualTo(2),
            "Inspection must not change the live run.");
        InvokeRuntime(store, "RecoverBackup", run, 100, 10);
        Assert.That(JsonUtility.ToJson(InvokeRuntime(run, "CaptureState")), Is.EqualTo(first));
        Assert.That(JsonUtility.ToJson(InvokeRuntime(store, "LoadActiveRun")), Is.EqualTo(first));
        Assert.That(RuntimeProperty<bool>(store, "HasBackup"), Is.True);
        InvokeRuntime(store, "DeleteCheckpoint");
        Assert.That(RuntimeProperty<bool>(store, "HasBackup"), Is.False);
    }

    [UnityTest]
    public IEnumerator TC_RECOVERY_002_ContinueRestoresAfterSceneReload()
    {
        Call("RestartRun");
        EnterOrResolveCurrentMapNode();
        Call("EndBattle", true);
        Call("OpenRewardSelection");
        Call("ChooseReward", FindRewardChoice("MaxHPFlat"));
        object store = Get<object>("checkpointStore");
        string expected = JsonUtility.ToJson(InvokeRuntime(store, "LoadActiveRun"));
        var db = RuntimeType("GameDatabase").GetProperty("Instance").GetValue(null) as Component;
        UnityEngine.Object.Destroy(db.gameObject);
        yield return null;
        EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Project/Scenes/Battle.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        manager = UnityEngine.Object.FindAnyObjectByType(RuntimeType("BattleManager")) as Component;
        object ui = Get<object>("activeRunRecoveryUI");
        Assert.That(Property<bool>(ui, "IsOpen"), Is.True);
        Assert.That(Property<bool>(Get<object>("runManager"), "IsActive"), Is.False);
        Assert.That(Get<bool>("isPlayerTurn"), Is.False);
        Property<Button>(ui, "PrimaryButton").onClick.Invoke();
        Assert.That(Property<bool>(ui, "IsOpen"), Is.False);
        Assert.That(IsRunMapOpen, Is.True);
        Assert.That(JsonUtility.ToJson(InvokeRuntime(Get<object>("runManager"), "CaptureState")),
            Is.EqualTo(expected));
        Assert.That(Get<int>("runMaxHPBonus"), Is.GreaterThan(0));
        AdvanceMapUntilBattleStarts();
        Assert.That(Get<bool>("isPlayerTurn"), Is.True);
    }

    [Test]
    public void TC_RECOVERY_003_DamagedSaveRechecksAndDiscardStartsClean()
    {
        Call("RestartRun");
        object store = Get<object>("checkpointStore");
        string path = RuntimeProperty<string>(store, "CheckpointPath");
        File.WriteAllText(path, "corrupted");
        File.WriteAllText(RuntimeProperty<string>(store, "BackupPath"), "corrupted");
        Call("PresentStartupRunChoice");
        object ui = Get<object>("activeRunRecoveryUI");
        Assert.That(Property<TMP_Text>(ui, "TitleText").text, Is.EqualTo("SAVE DATA DAMAGED"));
        Property<Button>(ui, "PrimaryButton").onClick.Invoke();
        Assert.That(Property<bool>(ui, "IsOpen"), Is.True);
        Property<Button>(ui, "SecondaryButton").onClick.Invoke();
        Assert.That(Property<bool>(ui, "IsOpen"), Is.False);
        Assert.That(IsRunMapOpen, Is.True);
        Assert.That(RuntimeField<int>(InvokeRuntime(store, "LoadActiveRun"), "Depth"), Is.EqualTo(1));
        Assert.That(RuntimeProperty<bool>(store, "HasBackup"), Is.False);
    }

    [Test]
    public void TC_RECOVERY_004_RecoveryButtonPromotesBackup()
    {
        Call("RestartRun");
        object store = Get<object>("checkpointStore");
        object run = Get<object>("runManager");
        string expected = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
        InvokeRuntime(run, "TryAdvanceDepth");
        InvokeRuntime(store, "SaveActiveRun", run);
        File.WriteAllText(RuntimeProperty<string>(store, "CheckpointPath"), "corrupted");
        Call("PresentStartupRunChoice");
        object ui = Get<object>("activeRunRecoveryUI");
        Assert.That(Property<object>(ui, "VisibleStatus").ToString(), Is.EqualTo("RecoverableBackup"));
        Property<Button>(ui, "PrimaryButton").onClick.Invoke();
        Assert.That(IsRunMapOpen, Is.True);
        Assert.That(JsonUtility.ToJson(InvokeRuntime(store, "LoadActiveRun")), Is.EqualTo(expected));
    }

    [Test]
    public void TC_RECOVERY_004A_MainMenuContinueWithDamagedPrimaryOffersRecovery()
    {
        Call("RestartRun");
        object store = Get<object>("checkpointStore");
        object run = Get<object>("runManager");
        string expectedBackup = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
        InvokeRuntime(run, "TryAdvanceDepth");
        InvokeRuntime(store, "SaveActiveRun", run);
        string primaryPath = RuntimeProperty<string>(store, "CheckpointPath");
        File.WriteAllText(primaryPath, "corrupted");

        Type flow = RuntimeType("MainMenuFlow");
        PropertyInfo intent = flow.GetProperty("PendingIntent");
        intent.SetValue(null, Enum.Parse(RuntimeType("MainMenuIntent"), "ContinueRun"));
        try
        {
            Call("PresentStartupRunChoice");
            object ui = Get<object>("activeRunRecoveryUI");
            Assert.That(Property<bool>(ui, "IsOpen"), Is.True);
            Assert.That(Property<object>(ui, "VisibleStatus").ToString(),
                Is.EqualTo("RecoverableBackup"));
            Assert.That(Property<TMP_Text>(ui, "TitleText").text,
                Is.EqualTo("RECOVERY AVAILABLE"));
            Assert.That(File.ReadAllText(primaryPath), Is.EqualTo("corrupted"),
                "Opening Continue must not silently accept the backup.");

            Property<Button>(ui, "PrimaryButton").onClick.Invoke();
            Assert.That(IsRunMapOpen, Is.True);
            Assert.That(JsonUtility.ToJson(InvokeRuntime(store, "LoadActiveRun")),
                Is.EqualTo(expectedBackup));
        }
        finally
        {
            InvokeRuntimeStatic("MainMenuFlow", "Clear");
        }
    }

    [Test]
    public void TC_RECOVERY_005_StaleContinueCannotRestoreTwiceOrTrustChangedFile()
    {
        Call("RestartRun");
        Call("PresentStartupRunChoice");
        object store = Get<object>("checkpointStore");
        object ui = Get<object>("activeRunRecoveryUI");
        File.WriteAllText(RuntimeProperty<string>(store, "CheckpointPath"), "corrupted");
        Property<Button>(ui, "PrimaryButton").onClick.Invoke();
        Assert.That(Property<bool>(ui, "IsOpen"), Is.True);
        Assert.That(Property<object>(ui, "VisibleStatus").ToString(), Is.EqualTo("Invalid"));
        Property<Button>(ui, "SecondaryButton").onClick.Invoke();
        Call("PresentStartupRunChoice");
        Property<Button>(ui, "PrimaryButton").onClick.Invoke();
        EnterOrResolveCurrentMapNode();
        string entered = JsonUtility.ToJson(InvokeRuntime(Get<object>("runManager"), "CaptureState"));
        Property<Button>(ui, "PrimaryButton").onClick.Invoke();
        Assert.That(JsonUtility.ToJson(InvokeRuntime(Get<object>("runManager"), "CaptureState")),
            Is.EqualTo(entered));
    }

    [Test]
    public void TC_RECOVERY_006_PromotionFailurePreservesLiveStateAndBackup()
    {
        Call("RestartRun");
        object store = Get<object>("checkpointStore");
        object run = Get<object>("runManager");
        InvokeRuntime(run, "TryAdvanceDepth");
        InvokeRuntime(store, "SaveActiveRun", run);
        string path = RuntimeProperty<string>(store, "CheckpointPath");
        string backup = File.ReadAllText(RuntimeProperty<string>(store, "BackupPath"));
        string before = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
        // Holding the staging file open simulates a denied write on Windows.
        using (var locked = new FileStream(path + ".tmp", FileMode.Create,
            FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Throws<IOException>(() => InvokeRuntime(store, "RecoverBackup", run, 100, 10));
        }
        Assert.That(JsonUtility.ToJson(InvokeRuntime(run, "CaptureState")), Is.EqualTo(before));
        Assert.That(File.ReadAllText(RuntimeProperty<string>(store, "BackupPath")), Is.EqualTo(backup));
    }

    [UnityTest]
    public IEnumerator TC_RECOVERY_007_ModalRendersWithinThreeAspectRatios()
    {
        Call("RestartRun");
        Call("PresentStartupRunChoice");
        yield return new WaitForSecondsRealtime(0.3f);
        Component ui = Get<Component>("activeRunRecoveryUI");
        Canvas canvas = ui.GetComponent<Canvas>();
        RenderMode oldMode = canvas.renderMode;
        Camera oldCamera = canvas.worldCamera;
        float oldDistance = canvas.planeDistance;
        var captureObject = new GameObject("RecoveryLayoutCapture", typeof(Camera));
        Camera capture = captureObject.GetComponent<Camera>();
        capture.enabled = false;
        capture.orthographic = true;
        capture.orthographicSize = 540f;
        capture.clearFlags = CameraClearFlags.SolidColor;
        capture.backgroundColor = Color.black;
        capture.cullingMask = 1 << canvas.gameObject.layer;
        capture.transform.position = new Vector3(0, 0, -100);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = capture;
        canvas.planeDistance = 1f;
        RenderTexture target = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            foreach (Vector2Int resolution in new[] {
                new Vector2Int(1920, 1080), new Vector2Int(1280, 720),
                new Vector2Int(1280, 1024) })
            {
                target = new RenderTexture(resolution.x, resolution.y, 24);
                target.Create();
                capture.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                yield return null;
                RectTransform card = Property<RectTransform>(ui, "CardRect");
                AssertRectInside(card, Property<GameObject>(ui, "Overlay").GetComponent<RectTransform>());
                foreach (TMP_Text label in card.GetComponentsInChildren<TMP_Text>())
                {
                    AssertRectInside(label.rectTransform, card);
                    label.ForceMeshUpdate();
                    Assert.That(label.isTextOverflowing, Is.False, label.name);
                }
                AssertRectInside(Property<Button>(ui, "PrimaryButton").GetComponent<RectTransform>(), card);
                AssertRectInside(Property<Button>(ui, "SecondaryButton").GetComponent<RectTransform>(), card);
                capture.Render();
                RenderTexture.active = target;
                var pixels = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../recovery-layout-" + resolution.x + "x" + resolution.y + ".png")), pixels.EncodeToPNG());
                UnityEngine.Object.Destroy(pixels);
                RenderTexture.active = previous;
                capture.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
                target = null;
            }
        }
        finally
        {
            RenderTexture.active = previous;
            capture.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCamera;
            canvas.planeDistance = oldDistance;
            UnityEngine.Object.Destroy(captureObject);
        }
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
        Assert.That(buttons.Length, Is.EqualTo(Property<Array>(nodeMap, "AllNodes").Length));
        Assert.That(buttons.Count(button => button.interactable), Is.EqualTo(1));
        Assert.That(buttons[0].interactable, Is.True);
        Assert.That(buttons.Count(b => b.gameObject.activeSelf), Is.EqualTo(1));
        Assert.That(((IEnumerable)Property<object>(mapUI, "RouteLines")).Cast<Image>().Any(i => i.gameObject.activeSelf), Is.False);
        Assert.That(GameObject.Find("RunMapSpineText"), Is.Null);
        Assert.That(Property<int>(mapUI, "CurrentDepth"), Is.EqualTo(1));
        Assert.That(
            Property<string>(mapUI, "SelectedNodeCode"),
            Is.EqualTo(RuntimeField<string>(currentNode, "Code"))
        );
        Assert.That(Property<Button>(mapUI, "EnterNodeButton").interactable, Is.True);
        Assert.That(Property<TMP_Text>(mapUI, "DetailTitle").text, Is.EqualTo("BATTLE"));
        Assert.That(Property<TMP_Text>(mapUI, "DetailBody").text, Does.Not.Contain("BATTLE_"));
        Assert.That(Property<TMP_Text>(mapUI, "DetailBody").text, Does.Not.Contain("ROUTE"));
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

    [UnityTest]
    public IEnumerator TC_TUT_001_FirstRunGuidesRealMapAndCombatActions()
    {
        SetGuidedTutorialCompleted(false);
        Call("RestartRun");
        yield return null;

        Component tutorial = GuidedTutorial;
        Assert.That(tutorial, Is.Not.Null);
        Assert.That(TutorialIsRunning(tutorial), Is.True);
        Assert.That(IsRunMapOpen, Is.True);
        Assert.That(TutorialStep(tutorial), Is.EqualTo("EnterMapNode"));

        Canvas.ForceUpdateCanvases();
        RectTransform cardRect = Property<RectTransform>(tutorial, "CardRect");
        RectTransform overlayRect = Property<RectTransform>(tutorial, "OverlayRect");
        AssertRectInside(cardRect, overlayRect);
        foreach (TMP_Text label in cardRect.GetComponentsInChildren<TMP_Text>(true))
        {
            label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing, Is.False, label.name);
            AssertRectInside(label.rectTransform, cardRect);
        }

        Button enterNode = Property<Button>(RunMapUI, "EnterNodeButton");
        Button nonTargetNode = ((IEnumerable)Property<object>(RunMapUI, "NodeButtons"))
            .Cast<Button>().First(button => button != TutorialButton(tutorial, "HighlightedButton") &&
                button.gameObject.activeInHierarchy);
        Canvas canvas = tutorial.GetComponent<Canvas>();
        Vector2 targetPoint = RectTransformUtility.WorldToScreenPoint(
            canvas.worldCamera, TutorialButton(tutorial, "HighlightedButton").transform.position);
        var targetEvent = new PointerEventData(EventSystem.current) { position = targetPoint };
        var raycasts = new List<RaycastResult>();
        EventSystem.current.RaycastAll(targetEvent, raycasts);
        string raycastNames = string.Join(" | ", raycasts.Select(hit => hit.gameObject.name));
        string raycastableImages = string.Join(" | ",
            tutorial.GetComponentsInChildren<Image>(true)
                .Where(image => image.raycastTarget && image.enabled && image.gameObject.activeInHierarchy)
                .Select(image => image.name + ":" + RectTransformUtility.RectangleContainsScreenPoint(
                    image.rectTransform, targetPoint, canvas.worldCamera)));
        Image targetImage = TutorialButton(tutorial, "HighlightedButton").GetComponent<Image>();
        string targetDetails = $"targetDepth={targetImage.depth}, targetCull={targetImage.canvasRenderer.cull}, targetAlpha={targetImage.color.a}, targetCanvas={targetImage.canvas?.name}, targetCanvasActive={targetImage.canvas?.isActiveAndEnabled}";
        string hitHierarchy = raycasts.Count == 0 ? "none" : GetHierarchyPath(raycasts[0].gameObject.transform);
        Assert.That(raycasts.Any(hit => hit.gameObject == TutorialButton(tutorial, "HighlightedButton").gameObject ||
            hit.gameObject.transform.IsChildOf(TutorialButton(tutorial, "HighlightedButton").transform)), Is.True,
            $"The spotlight hole must pass input to the real target button. Target={TutorialButton(tutorial, "HighlightedButton").name}, screen={targetPoint}, hits={raycastNames}, hitHierarchy={hitHierarchy}, {targetDetails}, images={raycastableImages}");

        raycasts.Clear();
        Vector2 blockedPoint = RectTransformUtility.WorldToScreenPoint(
            canvas.worldCamera, nonTargetNode.transform.position);
        var blockedEvent = new PointerEventData(EventSystem.current) { position = blockedPoint };
        EventSystem.current.RaycastAll(blockedEvent, raycasts);
        Assert.That(raycasts.Count, Is.GreaterThan(0));
        Assert.That(raycasts[0].gameObject.transform.IsChildOf(overlayRect), Is.True,
            "A non-target route control must be blocked by the tutorial overlay.");

        if (TutorialButton(tutorial, "HighlightedButton") != enterNode)
        {
            TutorialButton(tutorial, "HighlightedButton").onClick.Invoke();
            yield return null;
        }
        Assert.That(TutorialButton(tutorial, "HighlightedButton"), Is.SameAs(enterNode));
        enterNode.onClick.Invoke();
        Assert.That(TutorialStep(tutorial), Is.EqualTo("BattleIntro"));
        TutorialButton(tutorial, "ContinueButton").onClick.Invoke();
        yield return AwaitGuidedTutorialStep("BasicAttack");

        Set("enemyHP", 500);
        Button attack = Get<Button>("basicAttackButton");
        Assert.That(TutorialButton(tutorial, "HighlightedButton"), Is.SameAs(attack));
        attack.onClick.Invoke();
        yield return AwaitEnemy();
        yield return AwaitGuidedTutorialStep("Skill");

        Button skill = Get<Button>("skillButton");
        Assert.That(skill.interactable, Is.True);
        Assert.That(TutorialButton(tutorial, "HighlightedButton"), Is.SameAs(skill));
        skill.onClick.Invoke();
        yield return AwaitEnemy();
        yield return AwaitGuidedTutorialStep("Guard");

        Button guard = Get<Button>("guardButton");
        Assert.That(TutorialButton(tutorial, "HighlightedButton"), Is.SameAs(guard));
        guard.onClick.Invoke();
        yield return AwaitEnemy();
        yield return AwaitGuidedTutorialStep("Analyze");

        object activeRun = Get<object>("runManager");
        Assert.That(Property<int>(activeRun, "ConvergencesTriggered"), Is.EqualTo(1));
        Assert.That(OrbitCount, Is.Zero);
        Button analyze = Get<Button>("analyzeButton");
        Assert.That(analyze.interactable, Is.True,
            "Attack, Skill, Guard, and the real enemy turn restore enough Mana for first-run Analyze.");
        Assert.That(TutorialButton(tutorial, "HighlightedButton"), Is.SameAs(analyze));
        int manaBeforeAnalyze = Get<int>("playerMana");
        int analyzeCost = (int)Call("GetAnalyzeManaCost");
        analyze.onClick.Invoke();

        Assert.That(Get<bool>("isPlayerTurn"), Is.True,
            "Analyze must leave the current player turn active.");
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.True);
        Assert.That(Get<int>("playerMana"), Is.EqualTo(manaBeforeAnalyze - analyzeCost));
        Assert.That(OrbitCount, Is.Zero, "Analyze must not enter Orbit.");
        Assert.That(TutorialStep(tutorial), Is.EqualTo("Complete"));
        TutorialButton(tutorial, "ContinueButton").onClick.Invoke();
        Assert.That(TutorialIsRunning(tutorial), Is.False);
        Assert.That(ReadGuidedTutorialCompleted(), Is.EqualTo("true"));
    }

    [UnityTest]
    public IEnumerator TC_TUT_002_SkipAndEscapePersistWithoutChangingRunProgress()
    {
        SetGuidedTutorialCompleted(false);
        Call("RestartRun");
        Component tutorial = GuidedTutorial;
        object run = Get<object>("runManager");
        int hp = Property<int>(run, "CurrentHP");
        int mana = Property<int>(run, "CurrentMana");
        string currentNodeCode = RuntimeField<string>(Property<object>(run, "CurrentNode"), "Code");

        TutorialButton(tutorial, "SkipButton").onClick.Invoke();
        Assert.That(TutorialIsRunning(tutorial), Is.False);
        Assert.That(IsRunMapOpen, Is.True);
        Assert.That(Property<int>(run, "Depth"), Is.EqualTo(1));
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(hp));
        Assert.That(Property<int>(run, "CurrentMana"), Is.EqualTo(mana));
        Assert.That(RuntimeField<string>(Property<object>(run, "CurrentNode"), "Code"), Is.EqualTo(currentNodeCode));
        Assert.That(Property<bool>(run, "NodeEntered"), Is.False);
        Assert.That(Property<string[]>(run, "VisitedNodeCodes"), Is.Empty);
        Assert.That(ReadGuidedTutorialCompleted(), Is.EqualTo("true"));

        SetGuidedTutorialCompleted(false);
        Call("RestartRun");
        Assert.That(TutorialIsRunning(tutorial), Is.True);
        yield return EnterGuidedTutorialBattle();
        TutorialAction(tutorial, "HandleEscape");
        Assert.That(TutorialIsRunning(tutorial), Is.False);
        Assert.That(Get<bool>("battleEnded"), Is.False);
        Assert.That(ReadGuidedTutorialCompleted(), Is.EqualTo("true"));
    }

    [UnityTest]
    public IEnumerator TC_TUT_003_EarlyVictoryClosesGuidanceAndKeepsAutomaticReward()
    {
        SetGuidedTutorialCompleted(false);
        Call("RestartRun");
        yield return EnterGuidedTutorialBattle();
        Component tutorial = GuidedTutorial;
        TutorialButton(tutorial, "ContinueButton").onClick.Invoke();
        yield return AwaitGuidedTutorialStep("BasicAttack");

        Set("enemyHP", 1);
        TutorialButton(tutorial, "HighlightedButton").onClick.Invoke();
        Assert.That(Get<bool>("battleEnded"), Is.True);
        Assert.That(TutorialIsRunning(tutorial), Is.False);
        Assert.That(ReadGuidedTutorialCompleted(), Is.EqualTo("true"));

        yield return AwaitRewardSelection();
        Assert.That(Get<bool>("rewardSelectionOpen"), Is.True);
    }

    [UnityTest]
    public IEnumerator TC_TUT_004_LowManaExplainsDisabledSkillAndAnalyzeWithoutSpendingTurn()
    {
        SetGuidedTutorialCompleted(false);
        Call("RestartRun");
        yield return null;
        Mana(0);
        yield return EnterGuidedTutorialBattle();

        Component tutorial = GuidedTutorial;
        TutorialButton(tutorial, "ContinueButton").onClick.Invoke();
        yield return AwaitGuidedTutorialStep("BasicAttack");
        Set("enemyHP", 500);
        TutorialButton(tutorial, "HighlightedButton").onClick.Invoke();
        yield return AwaitEnemy();
        yield return AwaitGuidedTutorialStep("SkillUnavailable");

        Assert.That(Get<Button>("skillButton").interactable, Is.False);
        int manaBeforeGuard = Get<int>("playerMana");
        TutorialButton(tutorial, "ContinueButton").onClick.Invoke();
        yield return AwaitGuidedTutorialStep("Guard");
        TutorialButton(tutorial, "HighlightedButton").onClick.Invoke();
        yield return AwaitEnemy();
        yield return AwaitGuidedTutorialStep("SkillAfterGuard");
        Assert.That(Get<Button>("skillButton").interactable, Is.True);
        Assert.That(Get<int>("playerMana"), Is.GreaterThan(manaBeforeGuard));

        TutorialButton(tutorial, "HighlightedButton").onClick.Invoke();
        yield return AwaitEnemy();
        yield return AwaitGuidedTutorialStep("AnalyzeUnavailable");
        Assert.That(Get<Button>("analyzeButton").interactable, Is.False);
        Assert.That(Get<bool>("isPlayerTurn"), Is.True);
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.False);

        int manaBeforeExplanation = Get<int>("playerMana");
        int orbitCountBeforeExplanation = OrbitCount;
        TutorialButton(tutorial, "ContinueButton").onClick.Invoke();
        Assert.That(TutorialStep(tutorial), Is.EqualTo("Complete"));
        Assert.That(Get<bool>("isPlayerTurn"), Is.True,
            "The unavailable Analyze explanation must not consume the player's turn.");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(manaBeforeExplanation));
        Assert.That(OrbitCount, Is.EqualTo(orbitCountBeforeExplanation));
        Assert.That(Get<bool>("analyzeUsedThisNode"), Is.False);
        TutorialButton(tutorial, "ContinueButton").onClick.Invoke();
        Assert.That(TutorialIsRunning(tutorial), Is.False);
        Assert.That(ReadGuidedTutorialCompleted(), Is.EqualTo("true"));
    }

    [UnityTest]
    public IEnumerator TC_TUT_005_EnemyFirstAnomalyWaitsForPlayerAndShowsRealOpening()
    {
        SetGuidedTutorialCompleted(false);
        Call("RestartRun");
        yield return null;
        object run = Get<object>("runManager");
        object modifiers = Property<object>(run, "Modifiers");
        object anomalies = InvokeRuntimeStatic("RunAnomalyCatalog", "GetDefinitions");
        object frozenOrbit = ((IEnumerable)anomalies).Cast<object>().Single(
            anomaly => Property<string>(anomaly, "Code") == "ANOMALY_FROZEN_ORBIT");
        InvokeRuntime(modifiers, "AddOrStack", frozenOrbit);
        yield return EnterGuidedTutorialBattle();

        Component tutorial = GuidedTutorial;
        Assert.That(TutorialStep(tutorial), Is.EqualTo("BattleIntro"));
        Button continueButton = TutorialButton(tutorial, "ContinueButton");
        Assert.That(continueButton.gameObject.activeSelf, Is.True);
        Assert.That(continueButton.GetComponentInChildren<TMP_Text>().text,
            Is.EqualTo("WATCH OPENING"));
        Assert.That(Get<bool>("enemyActsFirstThisRound"), Is.True);
        Assert.That(Get<bool>("isPlayerTurn"), Is.False);

        int hpBeforeOpening = Property<int>(run, "CurrentHP");
        continueButton.onClick.Invoke();
        Assert.That(TutorialStep(tutorial), Is.EqualTo("WaitingForPlayerTurn"));
        Assert.That(tutorial.gameObject.transform.Find("GuidedTutorialOverlay").gameObject.activeSelf,
            Is.False);
        yield return AwaitGuidedTutorialStep("BasicAttack");
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(hpBeforeOpening - 12),
            "The actual prepared opening action should resolve before the first player instruction.");
        Assert.That(Get<bool>("isPlayerTurn"), Is.True);

        TutorialAction(tutorial, "Skip");
        Assert.That(TutorialIsRunning(tutorial), Is.False);
        Assert.That(ReadGuidedTutorialCompleted(), Is.EqualTo("true"));
    }

    [UnityTest]
    public IEnumerator TC_TUT_006_DefeatDoesNotCompleteGuidedTutorial()
    {
        SetGuidedTutorialCompleted(false);
        Call("RestartRun");
        yield return EnterGuidedTutorialBattle();

        Component tutorial = GuidedTutorial;
        Set("playerHP", 1);
        Set("enemyHP", 500);
        TutorialButton(tutorial, "ContinueButton").onClick.Invoke();
        yield return AwaitGuidedTutorialStep("BasicAttack");

        TutorialButton(tutorial, "HighlightedButton").onClick.Invoke();
        yield return AwaitEnemy();

        Assert.That(Get<bool>("battleEnded"), Is.True);
        Assert.That(Get<bool>("isPlayerTurn"), Is.False);
        Assert.That(TutorialIsRunning(tutorial), Is.False);
        Assert.That(ReadGuidedTutorialCompleted(), Is.EqualTo("false"),
            "A defeated first attempt must remain eligible for guidance on the next fresh run.");
    }

    [Test]
    public void TC_MAP_003_EventAppliesOutcomeAndAdvancesOnce()
    {
        Call("RestartRun");
        Set("playerHP", 63);
        Mana(7);

        object activeRun = Get<object>("runManager");
        object node = Property<object>(activeRun, "CurrentNode");
        int safety = 0;

        while (RuntimeField<object>(node, "Type").ToString() != "RandomEvent")
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
        object encounter = InvokeRuntimeStatic("RunEventService", "GetEncounter", Property<int>(activeRun, "RunSeed"), nodeCode);
        string title = Property<string>(encounter, "Title");
        Call("HandleRunMapNodeSelected", nodeCode);

        Assert.That(Property<int>(activeRun, "Depth"), Is.EqualTo(startingDepth + 1));
        Assert.That(Property<int>(activeRun, "CurrentHP"), Is.EqualTo(title == "THE EMBER ALTAR" ? 53 : title == "THE SILENT WELL" ? 88 : 63));
        Assert.That(Property<int>(activeRun, "CurrentMana"), Is.EqualTo(title == "THE SILENT WELL" ? 4 : 7));
        Assert.That(Property<int>(activeRun, "TotalEmbers"), Is.EqualTo(title == "THE EMBER ALTAR" ? 15 : title == "THE ASHEN CACHE" ? 10 : 0));
        Assert.That(Property<object>(activeRun, "Modifiers"), Is.SameAs(Get<object>("runModifiers")));
        Assert.That(IsRunMapOpen, Is.True);

        LogAssert.Expect(
            LogType.Error,
            "Run Map menolak pilihan node yang stale atau tidak aktif."
        );
        Call("HandleRunMapNodeSelected", nodeCode);
        Assert.That(Property<int>(activeRun, "Depth"), Is.EqualTo(startingDepth + 1));
    }

    [Test]
    public void TC_MAP_004_AuthoredFracturedFateVisualsAreBound()
    {
        Call("RestartRun");

        Component mapUI = RunMapUI;
        object activeRun = Get<object>("runManager");
        object nodeMap = Property<object>(activeRun, "NodeMap");
        Array graphNodes = Property<Array>(nodeMap, "AllNodes");
        int nodeCount = graphNodes.Length;
        int edgeCount = graphNodes.Cast<object>().Sum(n => RuntimeField<string[]>(n, "NextNodeCodes").Length);
        Image[] plaques = ((IEnumerable)Property<object>(mapUI, "NodeImages"))
            .Cast<Image>()
            .ToArray();
        Image[] icons = ((IEnumerable)Property<object>(mapUI, "NodeIcons"))
            .Cast<Image>()
            .ToArray();
        Image[] lines = ((IEnumerable)Property<object>(mapUI, "RouteLines"))
            .Cast<Image>()
            .ToArray();

        Assert.That(Property<bool>(mapUI, "UsesAuthoredVisuals"), Is.True);
        Assert.That(GameObject.Find("RunMapOuterFrame"), Is.Not.Null);
        Assert.That(plaques.Length, Is.EqualTo(nodeCount));
        Assert.That(plaques.All(image => image.sprite != null), Is.True);
        Assert.That(icons.Length, Is.EqualTo(nodeCount));
        Assert.That(icons.All(image => image.sprite != null), Is.True);
        Assert.That(lines.Length, Is.EqualTo(edgeCount));
        Assert.That(lines.All(image => image.sprite != null), Is.True);
        Assert.That(Property<Image>(mapUI, "DetailIcon").sprite, Is.Not.Null);
    }

    [TestCase(false, 20, 1, 70, 1)]
    [TestCase(true, 20, 1, 20, 6)]
    [TestCase(false, 95, 9, 100, 9)]
    [TestCase(true, 95, 9, 95, 10)]
    public void TC_REST_001_ChooseOneRecoveryAndPersist(bool mana, int hp, int mp, int expectedHP, int expectedMana)
    {
        Call("RestartRun");
        object run = Get<object>("runManager");
        int safety = 0;
        while (RuntimeField<object>(Property<object>(run, "CurrentNode"), "Type").ToString() != "Rest")
        {
            Assert.That(safety++, Is.LessThan(9));
            InvokeRuntime(run, "TryAdvanceDepth");
        }
        InvokeRuntime(run, "SetResources", hp, mp, 100, 10);
        Call("SyncLegacyRunStateFields");
        Call("OpenRunMap");
        object node = Property<object>(run, "CurrentNode");
        string code = RuntimeField<string>(node, "Code");
        var all = Property<Array>(Property<object>(run, "NodeMap"), "AllNodes").Cast<object>().ToArray();
        int index = Array.FindIndex(all, n => RuntimeField<string>(n, "Code") == code);
        var buttons = ((IEnumerable)Property<object>(RunMapUI, "NodeButtons")).Cast<Button>().ToArray();
        buttons[index].onClick.Invoke();
        Assert.That(Property<Button>(RunMapUI, "RestManaButton").gameObject.activeSelf, Is.True);
        int depth = Property<int>(run, "Depth");
        Property<Button>(RunMapUI, mana ? "RestManaButton" : "EnterNodeButton").onClick.Invoke();
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(expectedHP));
        Assert.That(Property<int>(run, "CurrentMana"), Is.EqualTo(expectedMana));
        Assert.That(Property<int>(run, "Depth"), Is.EqualTo(depth + 1));
        Call("ResolveRestNode", code, !mana);
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(expectedHP));
        Assert.That(Property<int>(run, "CurrentMana"), Is.EqualTo(expectedMana));
        object saved = InvokeRuntime(Get<object>("checkpointStore"), "LoadActiveRun");
        Assert.That(RuntimeField<int>(saved, "CurrentHP"), Is.EqualTo(expectedHP));
        Assert.That(RuntimeField<int>(saved, "CurrentMana"), Is.EqualTo(expectedMana));
        Assert.That(((IEnumerable)Property<object>(RunMapUI, "NodeButtons")).Cast<Button>()
            .Count(b => b.gameObject.activeSelf), Is.EqualTo(Property<string[]>(run, "VisitedNodeCodes").Length + Property<Array>(run, "ReachableNodes").Length));
    }

    [UnityTest]
    public IEnumerator TC_MAP_005_SeparatedDecorationsStayBehindReadableContent()
    {
        Call("RestartRun");
        EnterOrResolveCurrentMapNode();
        Set("enemyHP", 1);
        Click("basicAttackButton");
        yield return AwaitRewardSelection();
        RewardButton(FindRewardChoice("AttackPercent")).onClick.Invoke();
        Assert.That(IsRunMapOpen, Is.True);
        Assert.That(Property<int>(RunMapUI, "CurrentDepth"), Is.EqualTo(2));
        Assert.That(((IEnumerable)Property<object>(RunMapUI, "NodeButtons"))
            .Cast<Button>().Count(b => b.interactable), Is.EqualTo(3));
        yield return new WaitForSecondsRealtime(0.4f);
        Component map = RunMapUI;
        Canvas canvas = map.GetComponent<Canvas>();
        RenderMode originalMode = canvas.renderMode;
        Camera originalCamera = canvas.worldCamera;
        float originalDistance = canvas.planeDistance;
        var captureObject = new GameObject("RunMapLayoutCapture", typeof(Camera));
        Camera capture = captureObject.GetComponent<Camera>();
        capture.enabled = false;
        capture.orthographic = true;
        capture.orthographicSize = 540f;
        capture.clearFlags = CameraClearFlags.SolidColor;
        capture.backgroundColor = Color.black;
        capture.cullingMask = 1 << canvas.gameObject.layer;
        capture.transform.position = new Vector3(0f, 0f, -100f);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = capture;
        canvas.planeDistance = 1f;
        RenderTexture target = null;
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            Transform frame = GameObject.Find("RunMapOuterFrame").transform;
            Transform title = GameObject.Find("RunMapTitle").transform;
            Assert.That(frame.GetSiblingIndex(), Is.LessThan(title.GetSiblingIndex()));
            Assert.That(GameObject.Find("RunMapOrnaments"), Is.Null,
                "The full ornament atlas must never be drawn over the route.");
            Image[] pieces = frame.GetComponentsInChildren<Image>();
            Assert.That(pieces.Length, Is.GreaterThanOrEqualTo(8));
            Assert.That(pieces.All(p => p.sprite != null && !p.raycastTarget), Is.True);
            Assert.That(pieces.All(p => p.sprite.rect.width < p.sprite.texture.width), Is.True);

            foreach (Vector2Int resolution in new[] {
                new Vector2Int(1920, 1080), new Vector2Int(1280, 720),
                new Vector2Int(1280, 1024) })
            {
                target = new RenderTexture(resolution.x, resolution.y, 24);
                target.Create();
                capture.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases();
                yield return null;
                RectTransform overlayRect = Property<GameObject>(map, "Overlay")
                    .GetComponent<RectTransform>();
                RectTransform card = GameObject.Find("StandardRunMapCard").GetComponent<RectTransform>();
                AssertRectInside(card, overlayRect);
                RectTransform route = GameObject.Find("RunMapRoutePanel").GetComponent<RectTransform>();
                RectTransform content = GameObject.Find("RunMapRouteContent").GetComponent<RectTransform>();
                RectTransform viewport = GameObject.Find("RunMapViewport").GetComponent<RectTransform>();
                AssertRectInside(viewport, route);
                foreach (Button node in (IEnumerable)Property<object>(map, "NodeButtons"))
                {
                    if (!node.gameObject.activeSelf) continue;
                    AssertRectInside(node.GetComponent<RectTransform>(), content);
                    if (node.interactable) AssertRectInside(node.GetComponent<RectTransform>(), viewport);
                }
                RectTransform details = GameObject.Find("RunMapDetailPanel").GetComponent<RectTransform>();
                AssertRectInside(Property<TMP_Text>(map, "DetailTitle").rectTransform, details);
                AssertRectInside(Property<TMP_Text>(map, "DetailBody").rectTransform, details);
                AssertRectInside(Property<Button>(map, "EnterNodeButton").GetComponent<RectTransform>(), details);
                capture.Render();
                RenderTexture.active = target;
                var pixels = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
                pixels.Apply();
                string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,
                    "../runmap-layout-" + resolution.x + "x" + resolution.y + ".png"));
                System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG());
                if (resolution.y == 1024)
                {
                    object run = Get<object>("runManager");
                    int safety = 0;
                    while (RuntimeField<object>(Property<object>(run, "CurrentNode"), "Type").ToString() != "Rest")
                    {
                        Assert.That(safety++, Is.LessThan(9));
                        InvokeRuntime(run, "TryAdvanceDepth");
                    }
                    Call("SyncLegacyRunStateFields");
                    Call("OpenRunMap");
                    string restCode = RuntimeField<string>(Property<object>(run, "CurrentNode"), "Code");
                    object[] all = Property<Array>(Property<object>(run, "NodeMap"), "AllNodes").Cast<object>().ToArray();
                    int restIndex = Array.FindIndex(all, n => RuntimeField<string>(n, "Code") == restCode);
                    ((IEnumerable)Property<object>(map, "NodeButtons")).Cast<Button>().ElementAt(restIndex).onClick.Invoke();
                    yield return new WaitForSecondsRealtime(.4f);
                    Canvas.ForceUpdateCanvases();
                    foreach (TMP_Text label in details.GetComponentsInChildren<TMP_Text>())
                    {
                        label.ForceMeshUpdate();
                        Assert.That(label.isTextOverflowing, Is.False, label.name);
                        AssertRectInside(label.rectTransform, details);
                    }
                    AssertRectInside(Property<Button>(map, "RestManaButton").GetComponent<RectTransform>(), details);
                    capture.Render();
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "../rest-choice-layout.png")), pixels.EncodeToPNG());
                }
                UnityEngine.Object.Destroy(pixels);
                RenderTexture.active = previousActive;
                capture.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
                target = null;
            }
        }
        finally
        {
            RenderTexture.active = previousActive;
            capture.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
            canvas.renderMode = originalMode;
            canvas.worldCamera = originalCamera;
            canvas.planeDistance = originalDistance;
            UnityEngine.Object.Destroy(captureObject);
        }
    }

    private static void AssertRectInside(RectTransform child, RectTransform parent)
    {
        var corners = new Vector3[4];
        child.GetWorldCorners(corners);
        foreach (Vector3 corner in corners)
        {
            Vector3 local = parent.InverseTransformPoint(corner);
            Assert.That(local.x, Is.InRange(parent.rect.xMin - 1f, parent.rect.xMax + 1f), child.name);
            Assert.That(local.y, Is.InRange(parent.rect.yMin - 1f, parent.rect.yMax + 1f), child.name);
        }
    }

    [Test]
    public void TC_EVENT_001_SeededContentRejectsUnaffordableAndStaleChoices()
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int seed = 1; seed <= 60; seed++)
        {
            object run = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
            InvokeRuntime(run, "StartNewRunWithNodeCount", seed, 10, 1, 0, 100, 10);
            int safety = 0;
            while (RuntimeField<object>(Property<object>(run, "CurrentNode"), "Type").ToString() != "RandomEvent")
            {
                Assert.That(safety++, Is.LessThan(9));
                InvokeRuntime(run, "TryAdvanceDepth");
            }
            string code = RuntimeField<string>(Property<object>(run, "CurrentNode"), "Code");
            object encounter = InvokeRuntimeStatic("RunEventService", "GetEncounter", seed, code);
            string title = Property<string>(encounter, "Title");
            seen.Add(title);
            Assert.That(Property<string>(InvokeRuntimeStatic("RunEventService", "GetEncounter", seed, code), "Title"), Is.EqualTo(title));
            string before = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
            Assert.That((bool)InvokeRuntimeStatic("RunEventService", "TryResolve", run, code, 2, 100, 10), Is.False);
            Assert.That(JsonUtility.ToJson(InvokeRuntime(run, "CaptureState")), Is.EqualTo(before));
            if (title != "THE ASHEN CACHE")
            {
                Assert.That((bool)InvokeRuntimeStatic("RunEventService", "TryResolve", run, code, 0, 100, 10), Is.False);
                Assert.That(JsonUtility.ToJson(InvokeRuntime(run, "CaptureState")), Is.EqualTo(before));
            }
            Assert.That((bool)InvokeRuntimeStatic("RunEventService", "TryResolve", run, code, 1, 100, 10), Is.True);
            string after = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
            Assert.That((bool)InvokeRuntimeStatic("RunEventService", "TryResolve", run, code, 1, 100, 10), Is.False);
            Assert.That(JsonUtility.ToJson(InvokeRuntime(run, "CaptureState")), Is.EqualTo(after));
            Assert.That(Property<int>(run, "CurrentHP"), Is.InRange(1, 100));
            Assert.That(Property<int>(run, "CurrentMana"), Is.InRange(0, 10));
        }
        Assert.That(seen.Count, Is.EqualTo(3));
    }

    [TestCase(0)]
    [TestCase(1)]
    public void TC_EVENT_002_UiChoiceSavesAndSurvivesContinue(int choice)
    {
        Call("RestartRun");
        object run = Get<object>("runManager");
        int safety = 0;
        while (RuntimeField<object>(Property<object>(run, "CurrentNode"), "Type").ToString() != "RandomEvent")
        {
            Assert.That(safety++, Is.LessThan(9));
            InvokeRuntime(run, "TryAdvanceDepth");
        }
        InvokeRuntime(run, "SetResources", 99, 9, 100, 10);
        Call("SyncLegacyRunStateFields");
        Call("OpenRunMap");
        string code = RuntimeField<string>(Property<object>(run, "CurrentNode"), "Code");
        object[] all = Property<Array>(Property<object>(run, "NodeMap"), "AllNodes").Cast<object>().ToArray();
        int index = Array.FindIndex(all, n => RuntimeField<string>(n, "Code") == code);
        ((IEnumerable)Property<object>(RunMapUI, "NodeButtons")).Cast<Button>().ElementAt(index).onClick.Invoke();
        Assert.That(Property<Button>(RunMapUI, "EventAlternativeButton").gameObject.activeSelf, Is.True);
        Property<Button>(RunMapUI, choice == 0 ? "EnterNodeButton" : "EventAlternativeButton").onClick.Invoke();
        Assert.That(Property<string[]>(run, "VisitedNodeCodes").Last(), Is.EqualTo(code));
        string expected = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
        Assert.That(JsonUtility.ToJson(InvokeRuntime(Get<object>("checkpointStore"), "LoadActiveRun")), Is.EqualTo(expected));
        Call("PresentStartupRunChoice");
        Call("ContinueSavedRun");
        Assert.That(JsonUtility.ToJson(InvokeRuntime(Get<object>("runManager"), "CaptureState")), Is.EqualTo(expected));
        Assert.That(Property<int>(run, "CurrentHP"), Is.InRange(1, 100));
        Assert.That(Property<int>(run, "CurrentMana"), Is.InRange(0, 10));
    }

    [Test]
    public void TC_ENCOUNTER_001_StablePatternsAndPhaseDamage()
    {
        Type nodeType = RuntimeType("StandardRunNodeType");
        object elite = Enum.Parse(nodeType, "EliteBattle");
        object boss = Enum.Parse(nodeType, "Boss");
        object first = InvokeRuntimeStatic("EnemyActionPlan", "CreateEncounterAction", elite, 2, 10, 0, false);
        object heavy = InvokeRuntimeStatic("EnemyActionPlan", "CreateEncounterAction", elite, 2, 10, 1, false);
        Assert.That(Property<int>(first, "TotalEstimatedDamage"), Is.EqualTo(10));
        Assert.That(Property<int>(heavy, "TotalEstimatedDamage"), Is.EqualTo(14));
        Assert.That(Property<int>(InvokeRuntimeStatic("EnemyActionPlan", "CreateEncounterAction", boss, 3, 10, 1, true), "TotalEstimatedDamage"), Is.Zero);
        Assert.That(Property<int>(InvokeRuntimeStatic("EnemyActionPlan", "CreateEncounterAction", boss, 3, 10, 2, false), "TotalEstimatedDamage"), Is.EqualTo(15));
        Assert.That(Property<int>(InvokeRuntimeStatic("EnemyActionPlan", "CreateEncounterAction", boss, 3, 10, 2, true), "TotalEstimatedDamage"), Is.EqualTo(18));
    }

    [UnityTest]
    public IEnumerator TC_ENCOUNTER_002_BossIntroBlocksInputAndPhaseKeepsPreparedAction()
    {
        Call("RestartRun");
        object run = Get<object>("runManager");
        while (RuntimeField<object>(Property<object>(run, "CurrentNode"), "Type").ToString() != "Boss")
            InvokeRuntime(run, "TryAdvanceDepth");
        Call("SyncLegacyRunStateFields");
        Call("OpenRunMap");
        Time.timeScale = 1;
        EnterOrResolveCurrentMapNode();
        object banner = Get<object>("encounterBanner");
        Assert.That(Property<bool>(banner, "IsPlaying"), Is.True);
        int before = Get<int>("enemyHP");
        Call("UseBasicAttack");
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(before));
        yield return new WaitForSeconds(.65f);
        Time.timeScale = 0;
        yield return CaptureEncounterBanner();
        Assert.That(Property<bool>(banner, "IsPlaying"), Is.True, "Pause must hold the cut-in.");
        Time.timeScale = 10;
        float deadline = Time.realtimeSinceStartup + 10;
        while (Property<bool>(banner, "IsPlaying"))
        {
            Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline));
            yield return null;
        }
        object prepared = Plan;
        Set("enemyHP", before / 2);
        Click("guardButton");
        yield return AwaitEnemy();
        Assert.That(Get<bool>("bossAwakened"), Is.True);
        Assert.That(Property<string>(prepared, "ActionCode"), Is.EqualTo("BOSS_STRIKE"));
        Assert.That(Property<string>(Plan, "ActionCode"), Is.EqualTo("BOSS_CHARGE"));
        int hp = Property<int>(run, "CurrentHP");
        Click("guardButton");
        yield return AwaitEnemy();
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(hp));
        Assert.That(Property<string>(Plan, "ActionCode"), Is.EqualTo("BOSS_RUPTURE"));
        Call("RestartRun");
        Assert.That(Property<bool>(banner, "IsPlaying"), Is.False);
    }

    [UnityTest]
    public IEnumerator TC_ENCOUNTER_003_OppositeSlidesReducedMotionAndCancellation()
    {
        Time.timeScale = 1;
        var banner = (MonoBehaviour)RunMapUI.gameObject.AddComponent(RuntimeType("EncounterBannerUI"));
        foreach (bool boss in new[] { false, true })
        {
            banner.StartCoroutine((IEnumerator)InvokeRuntime(banner, "Play", "TEST ENCOUNTER", boss, false));
            RectTransform band = GameObject.Find("SlidingThreatBanner").GetComponent<RectTransform>();
            Assert.That(band.anchoredPosition.x * (boss ? 1 : -1), Is.GreaterThan(0));
            yield return new WaitForSeconds(.65f);
            Assert.That(Mathf.Abs(band.anchoredPosition.x), Is.LessThan(.1f));
            yield return new WaitForSeconds(1.15f);
            Assert.That(band.anchoredPosition.x * (boss ? 1 : -1), Is.LessThan(0));
            yield return new WaitForSeconds(.4f);
            Assert.That(Property<bool>(banner, "IsPlaying"), Is.False);
        }
        Component settings = RunMapUI.GetComponent(RuntimeType("BattleSettingsUI"));
        var toggles = (System.Collections.Generic.Dictionary<string, bool>)settings.GetType()
            .GetField("toggleStates", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(settings);
        toggles["1:0"] = true;
        banner.StartCoroutine((IEnumerator)InvokeRuntime(banner, "Play", "REDUCED MOTION", true, false));
        Assert.That(GameObject.Find("SlidingThreatBanner").GetComponent<RectTransform>().anchoredPosition.x, Is.Zero);
        InvokeRuntime(banner, "Cancel");
        banner.StartCoroutine((IEnumerator)InvokeRuntime(banner, "Play", "NEW ANNOUNCEMENT", false, false));
        yield return null;
        Assert.That(Property<bool>(banner, "IsPlaying"), Is.True, "Cancelled iterator must not dismiss a newer banner.");
        InvokeRuntime(banner, "Cancel");
        toggles["1:0"] = false;
        UnityEngine.Object.Destroy(banner);
        Time.timeScale = 10;
    }

    [Test]
    public void TC_RELIC_001_CatalogOffersThreeUniqueSeededChoicesAndRejectsStacks()
    {
        object owned = NewRuntimeObject("RunModifierCollection");
        Array catalog = (Array)InvokeRuntimeStatic("RunRelicCatalog", "GetDefinitions");
        Assert.That(catalog.Length, Is.EqualTo(4));
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int seed = 1; seed <= 40; seed++)
        {
            Array choices = (Array)InvokeRuntimeStatic("RunRelicCatalog", "CreateChoices", seed, owned);
            Array same = (Array)InvokeRuntimeStatic("RunRelicCatalog", "CreateChoices", seed, owned);
            var codes = choices.Cast<object>().Select(r => Property<string>(r, "Code")).ToArray();
            Assert.That(codes.Distinct().Count(), Is.EqualTo(3));
            Assert.That(codes, Is.EqualTo(same.Cast<object>().Select(r => Property<string>(r, "Code"))));
            foreach (string code in codes) seen.Add(code);
        }
        Assert.That(seen.Count, Is.EqualTo(4));
        InvokeRuntime(owned, "AddOrStack", catalog.GetValue(0));
        Assert.Throws<InvalidOperationException>(() => InvokeRuntime(owned, "AddOrStack", catalog.GetValue(0)));
        Assert.That(Property<int>(owned, "Count"), Is.EqualTo(1));
    }

    [Test]
    public void TC_RELIC_002_OptionalBranchAndLegacyCheckpointStayCompatible()
    {
        object run = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(run, "StartNewRun", 2468, 77, 6, 100, 10);
        object graph = Property<object>(run, "NodeMap");
        object relic = InvokeRuntime(graph, "FindNode", "RELIC_03");
        Assert.That(relic, Is.Not.Null);
        Assert.That(Property<Array>(graph, "AllNodes").Cast<object>().Count(n => RuntimeField<int>(n, "Depth") == 3), Is.EqualTo(4));
        object state = InvokeRuntime(run, "CaptureState");
        Assert.That(RuntimeField<int>(state, "RouteVersion"), Is.EqualTo(1));
        SetRuntimeField(state, "RouteVersion", 0);
        InvokeRuntime(run, "RestoreState", state, 100, 10);
        Assert.That(InvokeRuntime(Property<object>(run, "NodeMap"), "FindNode", "RELIC_03"), Is.Null);
        SetRuntimeField(state, "RouteVersion", 99);
        Assert.Throws<InvalidOperationException>(() => InvokeRuntime(run, "RestoreState", state, 100, 10));
    }

    [UnityTest]
    public IEnumerator TC_RELIC_003_ShrineCardCommitsOnceAndSurvivesRestore()
    {
        Call("RestartRun");
        object run = Get<object>("runManager");
        object graph = Property<object>(run, "NodeMap");
        InvokeRuntime(run, "TryAdvanceDepth");
        object predecessor = Property<Array>(run, "ReachableNodes").Cast<object>().First(n =>
            RuntimeField<string[]>(n, "NextNodeCodes").Contains("RELIC_03"));
        string code = RuntimeField<string>(predecessor, "Code");
        Assert.That((bool)InvokeRuntime(run, "TryEnterNode", code), Is.True);
        InvokeRuntime(run, "TryResolveCurrentNode", RuntimeField<object>(predecessor, "Type"), code);
        Call("OpenRunMap");
        Call("HandleRunMapNodeSelected", "RELIC_03");
        yield return new WaitForSecondsRealtime(1.2f);
        Assert.That(RewardChoices.Length, Is.EqualTo(3));
        foreach (TMP_Text label in Get<GameObject>("rewardPanel").GetComponentsInChildren<TMP_Text>())
        {
            label.ForceMeshUpdate();
            Assert.That(label.isTextOverflowing, Is.False, label.text);
            Assert.That(label.color.grayscale, Is.GreaterThan(0.7f), "Card text must remain readable on dark panels.");
        }
        string picked = Property<string>(RewardChoices.GetValue(0), "Code");
        yield return CaptureEncounterBanner(true);
        Call("ChooseReward", 0);
        Call("ChooseReward", 1);
        Assert.That(Property<int>(Property<object>(run, "Modifiers"), "Count"), Is.EqualTo(1));
        Assert.That(Property<string[]>(run, "VisitedNodeCodes"), Does.Contain("RELIC_03"));
        object state = InvokeRuntime(run, "CaptureState");
        object restored = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(restored, "RestoreState", JsonUtility.FromJson(JsonUtility.ToJson(state), RuntimeType("SeededRunState")), 100, 10);
        Assert.That(JsonUtility.ToJson(InvokeRuntime(restored, "CaptureState")), Is.EqualTo(JsonUtility.ToJson(state)));
        Assert.That(JsonUtility.ToJson(state), Does.Contain(picked));
        object corrupt = InvokeRuntime(state, "Clone");
        Array savedModifiers = RuntimeField<Array>(corrupt, "Modifiers");
        SetRuntimeField(savedModifiers.GetValue(0), "StackCount", 2);
        Assert.Throws<InvalidOperationException>(() => InvokeRuntime(restored, "RestoreState", corrupt, 100, 10));
        Assert.That(JsonUtility.ToJson(InvokeRuntime(restored, "CaptureState")), Is.EqualTo(JsonUtility.ToJson(state)));
        Call("RestartRun");
        Assert.That(Property<int>(Property<object>(Get<object>("runManager"), "Modifiers"), "Count"), Is.Zero);
    }

    private void EquipRelic(int index)
    {
        Array catalog = (Array)InvokeRuntimeStatic("RunRelicCatalog", "GetDefinitions");
        InvokeRuntime(Property<object>(Get<object>("runManager"), "Modifiers"), "AddOrStack", catalog.GetValue(index));
    }

    private void EquipAnomaly(int index)
    {
        Array catalog = (Array)InvokeRuntimeStatic(
            "RunAnomalyCatalog",
            "GetDefinitions"
        );
        InvokeRuntime(
            Property<object>(Get<object>("runManager"), "Modifiers"),
            "AddOrStack",
            catalog.GetValue(index)
        );
    }

    private object StatusValue(string typeName)
    {
        return Enum.Parse(RuntimeType("BattleStatusEffectType"), typeName);
    }

    private object StatusTarget(string targetName)
    {
        return Enum.Parse(RuntimeType("BattleStatusTarget"), targetName);
    }

    [UnityTest]
    public IEnumerator TC_RELIC_004_GuardManaAndEntryHealingRespectCaps()
    {
        EquipRelic(0);
        Set("playerMana", 0);
        Call("UseGuard");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(3));
        yield return AwaitEnemy();
        Set("playerMana", 9);
        Call("UseGuard");
        Assert.That(Get<int>("playerMana"), Is.EqualTo(10));
        yield return AwaitEnemy();
        EquipRelic(2);
        Set("playerHP", 70);
        Call("StartBattle");
        Assert.That(Get<int>("playerHP"), Is.EqualTo(76));
        Set("playerHP", 99);
        Call("StartBattle");
        Assert.That(Get<int>("playerHP"), Is.EqualTo(100));
    }

    [UnityTest]
    public IEnumerator TC_RELIC_005_FirstSkillOnlyConsumesOnSuccess()
    {
        EquipRelic(3);
        object run = Get<object>("runManager");
        int seed = Property<int>(run, "RunSeed");
        int depth = Property<int>(run, "Depth");
        int nonCriticalPair = -1;
        for (int index = 0; index < 1000; index++)
        {
            bool first = (bool)InvokeRuntimeStatic(
                "BattleTurnRules", "IsCritical", seed, depth, index, 0.05f
            );
            bool second = (bool)InvokeRuntimeStatic(
                "BattleTurnRules", "IsCritical", seed, depth, index + 1, 0.05f
            );
            if (!first && !second)
            {
                nonCriticalPair = index;
                break;
            }
        }
        Assert.That(nonCriticalPair, Is.GreaterThanOrEqualTo(0));
        Set("playerStrikeIndex", nonCriticalPair);
        Set("enemyHP", 200);
        Set("playerMana", 0);
        Call("UseSkill");
        Assert.That(Get<bool>("relicFirstSkillUsed"), Is.False);
        Set("playerMana", 10);
        Call("UseSkill");
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(174));
        yield return AwaitEnemy();
        Call("UseSkill");
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(154));
        yield return AwaitEnemy();
        Call("StartBattle");
        Assert.That(Get<bool>("relicFirstSkillUsed"), Is.False);
    }

    [UnityTest]
    public IEnumerator TC_RELIC_006_BurnTicksExpiresCapsAndClearsBetweenBattles()
    {
        EquipRelic(1);
        Set("enemyHP", 200);
        object activeRun = Get<object>("runManager");
        int strikeIndex = Get<int>("playerStrikeIndex");
        float criticalChance = Property<float>(Get<object>("runStatController"),
            "EffectiveCriticalChance");
        int attackDamage = (int)Call("GetCurrentBasicAttackDamage");
        bool critical = (bool)InvokeRuntimeStatic("BattleTurnRules", "IsCritical",
            Property<int>(activeRun, "RunSeed"), Property<int>(activeRun, "Depth"),
            strikeIndex, criticalChance);
        if (critical)
            attackDamage = (int)InvokeRuntimeStatic("BattleTurnRules", "ApplyCritical", attackDamage);
        Call("UseBasicAttack");
        Assert.That(Get<int>("relicBurnStacks"), Is.EqualTo(1));
        yield return AwaitEnemy();
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(200 - attackDamage - 1));
        Assert.That(Get<int>("relicBurnTurns"), Is.EqualTo(1));
        Call("UseGuard");
        yield return AwaitEnemy();
        Assert.That(Get<int>("relicBurnStacks"), Is.Zero);
        object statuses = Get<object>("battleStatuses");
        for (int index = 0; index < 3; index++)
            InvokeRuntime(statuses, "Apply", StatusValue("Burn"), StatusTarget("Enemy"), 1, 1);
        Call("SyncLegacyBurnFields");
        Call("UseBasicAttack");
        Assert.That(Get<int>("relicBurnStacks"), Is.EqualTo(3));
        Assert.That(Get<int>("relicBurnTurns"), Is.EqualTo(2));
        yield return AwaitEnemy();
        Call("StartBattle");
        Assert.That(Get<int>("relicBurnStacks"), Is.Zero);
        Assert.That(Get<int>("relicBurnTurns"), Is.Zero);
    }

    [Test]
    public void TC_STATUS_001_ControllerCapsRefreshesTicksAndCalculatesDebuffs()
    {
        object statuses = NewRuntimeObject("BattleStatusController");
        object burn = StatusValue("Burn");
        object weakness = StatusValue("Weakness");
        object curse = StatusValue("Curse");
        object player = StatusTarget("Player");
        object enemy = StatusTarget("Enemy");

        for (int index = 0; index < 5; index++)
            InvokeRuntime(statuses, "Apply", burn, enemy, 1, 2);
        Assert.That((int)InvokeRuntime(statuses, "GetStacks", burn, enemy), Is.EqualTo(3));
        Assert.That((int)InvokeRuntime(statuses, "GetRemainingTurns", burn, enemy), Is.EqualTo(2));

        InvokeRuntime(statuses, "Apply", weakness, player, 1, 2);
        InvokeRuntime(statuses, "Apply", curse, enemy, 1, 2);
        Assert.That((int)InvokeRuntime(statuses, "ResolveOutgoingDamage", player, 100), Is.EqualTo(75));
        Assert.That((int)InvokeRuntime(statuses, "ResolveIncomingDamage", enemy, 100), Is.EqualTo(120));

        object firstTick = InvokeRuntime(statuses, "ResolveEndTurn", enemy);
        Assert.That(Property<int>(firstTick, "BurnDamage"), Is.EqualTo(3));
        Assert.That((int)InvokeRuntime(statuses, "GetRemainingTurns", burn, enemy), Is.EqualTo(1));
        InvokeRuntime(statuses, "ResolveEndTurn", enemy);
        Assert.That((int)InvokeRuntime(statuses, "GetStacks", burn, enemy), Is.Zero);
        Assert.That((int)InvokeRuntime(statuses, "GetRemainingTurns", curse, enemy), Is.Zero);
    }

    [UnityTest]
    public IEnumerator TC_STATUS_002_CurseAndWeaknessUseTheBattleDamagePipeline()
    {
        object statuses = Get<object>("battleStatuses");
        InvokeRuntime(
            statuses,
            "Apply",
            StatusValue("Curse"),
            StatusTarget("Enemy"),
            1,
            2
        );
        Set("enemyHP", 200);
        int criticalHitsBefore = Property<int>(Get<object>("runManager"), "CriticalHits");
        Call("UseBasicAttack");
        bool critical = Property<int>(Get<object>("runManager"), "CriticalHits") >
            criticalHitsBefore;
        int cursedDamage = critical
            ? (int)InvokeRuntimeStatic("BattleTurnRules", "ApplyCritical", 12)
            : 12;
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(200 - cursedDamage));
        yield return AwaitEnemy();

        InvokeRuntime(
            statuses,
            "Apply",
            StatusValue("Weakness"),
            StatusTarget("Enemy"),
            1,
            2
        );
        int hp = Get<int>("playerHP");
        int planned = Property<int>(Plan, "TotalEstimatedDamage");
        Call("UseGuard");
        yield return AwaitEnemy();
        int weakened = Mathf.RoundToInt(planned * 0.75f);
        int guarded = Mathf.CeilToInt(weakened * 0.5f);
        Assert.That(Get<int>("playerHP"), Is.EqualTo(hp - guarded));
    }

    [UnityTest]
    public IEnumerator TC_STATUS_003_EliteAndBossPlansApplyVisibleDebuffs()
    {
        Type nodeType = RuntimeType("StandardRunNodeType");
        object elitePlan = InvokeRuntimeStatic(
            "EnemyActionPlan",
            "CreateEncounterAction",
            Enum.Parse(nodeType, "EliteBattle"),
            1,
            12,
            1,
            false
        );
        Assert.That(Property<object>(elitePlan, "AppliedStatus").ToString(),
            Does.Contain("Weakness"));
        Assert.That(Property<string>(elitePlan, "EffectDescription"),
            Does.Contain("Weakness"));

        Set("enemyActionPlan", elitePlan);
        Call("UseGuard");
        yield return AwaitEnemy();
        object statuses = Get<object>("battleStatuses");
        Assert.That((int)InvokeRuntime(statuses, "GetRemainingTurns",
            StatusValue("Weakness"), StatusTarget("Player")), Is.EqualTo(2));

        object bossPlan = InvokeRuntimeStatic(
            "EnemyActionPlan",
            "CreateEncounterAction",
            Enum.Parse(nodeType, "Boss"),
            1,
            12,
            2,
            false
        );
        Assert.That(Property<object>(bossPlan, "AppliedStatus").ToString(),
            Does.Contain("Curse"));
        Assert.That(Property<string>(bossPlan, "EffectDescription"),
            Does.Contain("Curse"));

        Set("enemyActionPlan", bossPlan);
        Call("UseGuard");
        yield return AwaitEnemy();
        Assert.That((int)InvokeRuntime(statuses, "GetRemainingTurns",
            StatusValue("Curse"), StatusTarget("Player")), Is.EqualTo(2));
        Assert.That(Get<TMP_Text>("playerHPText").text, Does.Contain("CURSE"));
    }

    [Test]
    public void TC_ANO_001_CatalogIsSeededUniqueAndPersistsWithoutStacking()
    {
        object modifiers = NewRuntimeObject("RunModifierCollection");
        Array catalog = (Array)InvokeRuntimeStatic(
            "RunAnomalyCatalog",
            "GetDefinitions"
        );
        Assert.That(catalog.Length, Is.EqualTo(4));
        Array choices = (Array)InvokeRuntimeStatic(
            "RunAnomalyCatalog",
            "CreateChoices",
            7123,
            modifiers
        );
        Array same = (Array)InvokeRuntimeStatic(
            "RunAnomalyCatalog",
            "CreateChoices",
            7123,
            modifiers
        );
        Assert.That(choices.Length, Is.EqualTo(3));
        Assert.That(
            choices.Cast<object>().Select(value => Property<string>(value, "Code")),
            Is.EqualTo(same.Cast<object>().Select(value => Property<string>(value, "Code")))
        );
        Assert.That(
            choices.Cast<object>().Select(value => Property<string>(value, "Code")).Distinct().Count(),
            Is.EqualTo(3)
        );

        InvokeRuntime(modifiers, "AddOrStack", catalog.GetValue(1));
        Assert.Throws<InvalidOperationException>(() =>
            InvokeRuntime(modifiers, "AddOrStack", catalog.GetValue(1)));
        object stats = NewRuntimeObject(
            "RunStatController",
            100,
            10,
            10,
            0.5f,
            modifiers
        );
        Assert.That(Property<int>(stats, "EffectiveMaxHP"), Is.EqualTo(85));

        object run = NewRuntimeObject("RunManager", modifiers);
        InvokeRuntime(run, "StartNewRun", 7123, 85, 5, 85, 10);
        // StartNewRun intentionally clears prior modifiers; add the Anomaly as
        // a run reward before capturing its checkpoint state.
        InvokeRuntime(Property<object>(run, "Modifiers"), "AddOrStack", catalog.GetValue(1));
        object state = InvokeRuntime(run, "CaptureState");
        object restored = NewRuntimeObject(
            "RunManager",
            NewRuntimeObject("RunModifierCollection")
        );
        InvokeRuntime(restored, "RestoreState", state, 85, 10);
        Assert.That(
            Property<int>(Property<object>(restored, "Modifiers"), "Count"),
            Is.EqualTo(1)
        );
    }

    [UnityTest]
    public IEnumerator TC_ANO_001A_AllAnomalyCardGlyphsRenderWithTheSceneFont()
    {
        Array catalog = (Array)InvokeRuntimeStatic("RunAnomalyCatalog", "GetDefinitions");
        Component selection = Get<Component>("rewardSelectionUI");
        GameObject panel = Get<GameObject>("rewardPanel");
        Get<TMP_Text>("rewardTitleText").text = "CHOOSE ONE ANOMALY";
        for (int page = 0; page < 2; page++)
        {
            int count = page == 0 ? 3 : 1;
            InvokeRuntime(selection, "SetVisibleCardCount", count);
            for (int index = 0; index < count; index++)
                InvokeRuntime(selection, "SetCardDefinition", index, catalog.GetValue(page * 3 + index), 0);
            InvokeRuntime(selection, "ShowAnimated");
            yield return new WaitForSecondsRealtime(1.2f);
            TMP_Text[] glyphs = panel.GetComponentsInChildren<TMP_Text>()
                .Where(label => label.name == "RewardGlyph").ToArray();
            Assert.That(glyphs.Length, Is.EqualTo(count));
            foreach (TMP_Text glyph in glyphs)
            {
                glyph.ForceMeshUpdate();
                foreach (char character in glyph.text)
                    Assert.That(glyph.font.HasCharacter(character, true), Is.True,
                        "Anomaly glyph must be supported by the actual card font: " + glyph.text);
                Assert.That(glyph.textInfo.characterCount, Is.EqualTo(1));
                Assert.That(glyph.textInfo.characterInfo[0].isVisible, Is.True);
            }
            yield return CaptureEncounterBanner(true, "anomaly-cards-page" + (page + 1) + "-");
            InvokeRuntime(selection, "HideImmediate");
        }
    }

    [UnityTest]
    public IEnumerator TC_ANO_002_CollapsedStarAppliesBenefitAndConsequenceTogether()
    {
        EquipAnomaly(0);
        Set("enemyHP", 200);
        Mana(10);
        Call("UseSkill");
        object activeRun = Get<object>("runManager");
        float criticalChance = Property<float>(Get<object>("runStatController"),
            "EffectiveCriticalChance");
        bool critical = (bool)InvokeRuntimeStatic("BattleTurnRules", "IsCritical",
            Property<int>(activeRun, "RunSeed"), Property<int>(activeRun, "Depth"),
            0, criticalChance);
        int expectedDamage = critical
            ? (int)InvokeRuntimeStatic("BattleTurnRules", "ApplyCritical", 26)
            : 26;
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(200 - expectedDamage));
        Assert.That(Get<int>("playerMana"), Is.EqualTo(7));
        Assert.That(
            Get<Button>("skillButton").GetComponentInChildren<TMP_Text>(true).text,
            Does.Contain("3 Mana")
        );
        yield return AwaitEnemy();
    }

    [UnityTest]
    public IEnumerator TC_ANO_003_BlackSunUsesTwoActionsOnceAndReducesMaxHP()
    {
        EquipAnomaly(1);
        Set("playerHP", 100);
        Call("StartBattle");
        Assert.That(Get<int>("playerHP"), Is.EqualTo(85));
        Assert.That(Get<bool>("blackSunFirstConvergenceAvailable"), Is.True);
        Set("enemyHP", 200);

        object activeRun = Get<object>("runManager");
        float criticalChance = Property<float>(Get<object>("runStatController"),
            "EffectiveCriticalChance");
        bool firstCritical = (bool)InvokeRuntimeStatic(
            "BattleTurnRules", "IsCritical",
            Property<int>(activeRun, "RunSeed"),
            Property<int>(activeRun, "Depth"), 0, criticalChance
        );
        bool secondCritical = (bool)InvokeRuntimeStatic(
            "BattleTurnRules", "IsCritical",
            Property<int>(activeRun, "RunSeed"),
            Property<int>(activeRun, "Depth"), 1, criticalChance
        );
        int firstDamage = firstCritical ? 15 : 10;
        int skillDamage = secondCritical ? 30 : 20;
        int expectedEnemyHP = 200 - firstDamage - skillDamage -
            Mathf.RoundToInt(skillDamage * 0.30f);

        Call("UseBasicAttack");
        Assert.That(OrbitCount, Is.EqualTo(1));
        yield return AwaitEnemy();
        Call("UseSkill");
        Assert.That(OrbitCount, Is.Zero);
        Assert.That(Get<bool>("blackSunFirstConvergenceAvailable"), Is.False);
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(expectedEnemyHP));
        yield return AwaitEnemy();

        Call("UseBasicAttack");
        Assert.That(OrbitCount, Is.EqualTo(1));
        yield return AwaitEnemy();
        Call("UseBasicAttack");
        Assert.That(OrbitCount, Is.EqualTo(2), "Black Sun only shortens the first Convergence.");
        yield return AwaitEnemy();
    }

    [UnityTest]
    public IEnumerator TC_ANO_004_FrozenOrbitChangesInitiativeAndGuardsTwoAttacks()
    {
        EquipAnomaly(2);
        object stats = Get<object>("runStatController");
        Assert.That(Property<int>(stats, "EffectiveSpeed"), Is.EqualTo(8));
        Set("playerHP", 100);
        Call("StartBattle");
        Assert.That(Get<bool>("isPlayerTurn"), Is.False,
            "Speed 8 must let the Speed 9 enemy open the round.");
        int plannedDamage = Property<int>(Plan, "TotalEstimatedDamage");
        yield return AwaitEnemy();
        int expectedHP = 100 - plannedDamage;
        Assert.That(Get<int>("playerHP"), Is.EqualTo(expectedHP));

        Call("UseGuard");
        Assert.That(Get<int>("guardHitsRemaining"), Is.EqualTo(2));
        yield return AwaitEnemy();
        expectedHP -= Mathf.CeilToInt(plannedDamage * 0.5f);
        Assert.That(Get<int>("playerHP"), Is.EqualTo(expectedHP));
        Assert.That(Get<int>("guardHitsRemaining"), Is.EqualTo(1));

        Set("enemyHP", 200);
        Call("UseBasicAttack");
        yield return AwaitEnemy();
        expectedHP -= Mathf.CeilToInt(plannedDamage * 0.5f);
        Assert.That(Get<int>("playerHP"), Is.EqualTo(expectedHP));
        Assert.That(Get<int>("guardHitsRemaining"), Is.Zero);
        Assert.That(Get<bool>("isGuarding"), Is.False);
    }

    [UnityTest]
    public IEnumerator TC_ANO_005_InvertedPulseHealsButBasicAttackCannotCritical()
    {
        object run = Get<object>("runManager");
        int seed = Property<int>(run, "RunSeed");
        int depth = Property<int>(run, "Depth");
        int criticalStrike = -1;
        for (int index = 0; index < 1000; index++)
        {
            if ((bool)InvokeRuntimeStatic("BattleTurnRules", "IsCritical",
                seed, depth, index, 0.05f))
            {
                criticalStrike = index;
                break;
            }
        }
        Assert.That(criticalStrike, Is.GreaterThanOrEqualTo(0));

        Set("playerStrikeIndex", criticalStrike);
        Set("enemyHP", 200);
        Call("UseBasicAttack");
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(185),
            "The same seeded roll must produce a real baseline critical.");
        yield return AwaitEnemy();

        EquipAnomaly(3);
        Set("playerStrikeIndex", criticalStrike);
        Set("enemyHP", 200);
        Set("playerHP", 50);
        Call("UseBasicAttack");
        Assert.That(Get<int>("enemyHP"), Is.EqualTo(190));
        Assert.That(Get<int>("playerHP"), Is.EqualTo(52));
        Assert.That(Get<int>("playerStrikeIndex"), Is.EqualTo(criticalStrike + 1));
        yield return AwaitEnemy();

        object state = InvokeRuntime(run, "CaptureState");
        object restored = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(restored, "RestoreState", state, 100, 10);
        Assert.That((int)InvokeRuntime(Property<object>(restored, "Modifiers"),
            "GetStackCount", Enum.Parse(RuntimeType("RewardEffectType"),
                "AnomalyInvertedPulse")), Is.EqualTo(1));
    }

    private IEnumerator CaptureEncounterBanner(bool relicCards = false, string cardPrefix = "relic-cards-")
    {
        Canvas canvas = relicCards ? Get<GameObject>("rewardPanel").GetComponentInParent<Canvas>() : RunMapUI.GetComponent<Canvas>();
        RenderMode mode = canvas.renderMode;
        Camera previousCamera = canvas.worldCamera;
        float distance = canvas.planeDistance;
        var cameraObject = new GameObject("BossBannerCapture", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 540;
        camera.transform.position = new Vector3(0, 0, -100);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = null;
        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 1024) })
            {
                target = new RenderTexture(size.x, size.y, 24); target.Create(); camera.targetTexture = target;
                Canvas.ForceUpdateCanvases(); yield return null; Canvas.ForceUpdateCanvases();
                RectTransform band = relicCards ? Get<GameObject>("rewardPanel").GetComponent<RectTransform>() :
                    GameObject.Find("SlidingThreatBanner").GetComponent<RectTransform>();
                foreach (TMP_Text label in band.GetComponentsInChildren<TMP_Text>())
                {
                    label.ForceMeshUpdate(); Assert.That(label.isTextOverflowing, Is.False);
                    AssertRectInside(label.rectTransform, band);
                }
                camera.Render(); RenderTexture.active = target;
                var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); pixels.Apply();
                string prefix = relicCards ? "../" + cardPrefix : "../boss-banner-";
                File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, prefix + size.x + ".png")), pixels.EncodeToPNG());
                UnityEngine.Object.Destroy(pixels); RenderTexture.active = previous; camera.targetTexture = null;
                target.Release(); UnityEngine.Object.Destroy(target); target = null;
            }
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
            canvas.renderMode = mode; canvas.worldCamera = previousCamera; canvas.planeDistance = distance;
            UnityEngine.Object.Destroy(cameraObject);
        }
    }

    private string PrepareMerchantRun(object run, int hp, int mana)
    {
        for (int seed = 1; seed <= 100; seed++)
        {
            InvokeRuntime(run, "StartNewRunWithNodeCount", seed, 10, hp, mana, 100, 10);
            var nodes = Property<Array>(Property<object>(run, "NodeMap"), "AllNodes").Cast<object>();
            if (!nodes.Any(n => RuntimeField<object>(n, "Type").ToString() == "Merchant")) continue;
            for (int depth = 1; depth <= 10; depth++)
            {
                object node = Property<object>(run, "CurrentNode");
                if (RuntimeField<object>(node, "Type").ToString() == "Merchant")
                    return RuntimeField<string>(node, "Code");
                InvokeRuntime(run, "TryAdvanceDepth");
            }
        }
        Assert.Fail("No merchant found in bounded deterministic seeds.");
        return null;
    }

    [Test]
    public void TC_MERCHANT_001_RejectsInvalidCostsFullResourcesAndDuplicatePurchases()
    {
        object run = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
        string code = PrepareMerchantRun(run, 20, 1);
        string before = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
        foreach (int choice in new[] { -1, 0, 1, 3 })
            Assert.That((bool)InvokeRuntimeStatic("RunMerchantService", "TryResolve", run, code, choice, 100, 10), Is.False);
        Assert.That((bool)InvokeRuntime(run, "TrySpendEmbers", -1), Is.False);
        Assert.That(JsonUtility.ToJson(InvokeRuntime(run, "CaptureState")), Is.EqualTo(before));
        InvokeRuntime(run, "AddEmbers", 12);
        Assert.That((bool)InvokeRuntimeStatic("RunMerchantService", "TryResolve", run, "foreign", 0, 100, 10), Is.False);
        InvokeRuntime(run, "SetResources", 100, 10, 100, 10);
        Assert.That((bool)InvokeRuntimeStatic("RunMerchantService", "CanChoose", run, code, 0, 100, 10), Is.False);
        Assert.That((bool)InvokeRuntimeStatic("RunMerchantService", "CanChoose", run, code, 1, 100, 10), Is.False);
        InvokeRuntime(run, "SetResources", 80, 9, 101, 10);
        Assert.That((bool)InvokeRuntimeStatic("RunMerchantService", "TryResolve", run, code, 0, 101, 10), Is.True);
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(101));
        Assert.That(Property<int>(run, "TotalEmbers"), Is.Zero);
        string after = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
        Assert.That((bool)InvokeRuntimeStatic("RunMerchantService", "TryResolve", run, code, 0, 101, 10), Is.False);
        Assert.That(JsonUtility.ToJson(InvokeRuntime(run, "CaptureState")), Is.EqualTo(after));
        code = PrepareMerchantRun(run, 1, 0);
        Assert.That((bool)InvokeRuntimeStatic("RunMerchantService", "TryResolve", run, code, 2, 100, 10), Is.True);
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(1));
        Assert.That(Property<int>(run, "CurrentMana"), Is.Zero);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void TC_MERCHANT_002_UiPurchaseOrLeavePersistsThroughContinue(int choice)
    {
        Call("RestartRun");
        object run = Get<object>("runManager");
        string code = PrepareMerchantRun(run, 20, 1);
        InvokeRuntime(run, "AddEmbers", 30);
        Call("SyncLegacyRunStateFields");
        Call("OpenRunMap");
        object[] all = Property<Array>(Property<object>(run, "NodeMap"), "AllNodes").Cast<object>().ToArray();
        int index = Array.FindIndex(all, n => RuntimeField<string>(n, "Code") == code);
        ((IEnumerable)Property<object>(RunMapUI, "NodeButtons")).Cast<Button>().ElementAt(index).onClick.Invoke();
        Button button = Property<Button>(RunMapUI, choice == 0 ? "EnterNodeButton" : choice == 1 ? "EventAlternativeButton" : "LeaveMerchantButton");
        Assert.That(button.gameObject.activeSelf && button.interactable, Is.True);
        button.onClick.Invoke();
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(choice == 0 ? 70 : 20));
        Assert.That(Property<int>(run, "CurrentMana"), Is.EqualTo(choice == 1 ? 6 : 1));
        Assert.That(Property<int>(run, "TotalEmbers"), Is.EqualTo(choice == 0 ? 18 : choice == 1 ? 22 : 30));
        Assert.That(Property<string[]>(run, "VisitedNodeCodes").Last(), Is.EqualTo(code));
        string expected = JsonUtility.ToJson(InvokeRuntime(run, "CaptureState"));
        Assert.That((bool)InvokeRuntimeStatic("RunMerchantService", "TryResolve", run, code, choice, 100, 10), Is.False);
        Assert.That(JsonUtility.ToJson(InvokeRuntime(run, "CaptureState")), Is.EqualTo(expected));
        Assert.That(JsonUtility.ToJson(InvokeRuntime(Get<object>("checkpointStore"), "LoadActiveRun")), Is.EqualTo(expected));
        Call("PresentStartupRunChoice");
        Call("ContinueSavedRun");
        Assert.That(JsonUtility.ToJson(InvokeRuntime(Get<object>("runManager"), "CaptureState")), Is.EqualTo(expected));
    }

    [UnityTest]
    public IEnumerator TC_EVENT_003_EventAndMerchantPanelsFitWideAndNarrowScreens()
    {
        Call("RestartRun");
        object run = Get<object>("runManager");
        Canvas canvas = RunMapUI.GetComponent<Canvas>();
        RenderMode originalMode = canvas.renderMode;
        Camera originalCamera = canvas.worldCamera;
        float originalDistance = canvas.planeDistance;
        var cameraObject = new GameObject("EventCapture", typeof(Camera));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 540;
        camera.transform.position = new Vector3(0, 0, -100);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        RenderTexture oldTarget = RenderTexture.active;
        RenderTexture target = null;
        var seen = new System.Collections.Generic.HashSet<string>();
        try
        {
            for (int seed = 1; seed <= 61 && seen.Count < 4; seed++)
            {
                InvokeRuntime(run, "StartNewRunWithNodeCount", seed, 10, 70, 5, 100, 10);
                int safety = 0;
                while (RuntimeField<object>(Property<object>(run, "CurrentNode"), "Type").ToString() != "RandomEvent")
                {
                    Assert.That(safety++, Is.LessThan(9));
                    InvokeRuntime(run, "TryAdvanceDepth");
                }
                string code = RuntimeField<string>(Property<object>(run, "CurrentNode"), "Code");
                if (seed == 61) { code = PrepareMerchantRun(run, 70, 5); InvokeRuntime(run, "AddEmbers", 30); }
                string title = seed == 61 ? "MERCHANT" : Property<string>(InvokeRuntimeStatic("RunEventService", "GetEncounter", seed, code), "Title");
                if (!seen.Add(title)) continue;
                Call("SyncLegacyRunStateFields");
                Call("OpenRunMap");
                object[] all = Property<Array>(Property<object>(run, "NodeMap"), "AllNodes").Cast<object>().ToArray();
                int index = Array.FindIndex(all, n => RuntimeField<string>(n, "Code") == code);
                ((IEnumerable)Property<object>(RunMapUI, "NodeButtons")).Cast<Button>().ElementAt(index).onClick.Invoke();
                yield return new WaitForSecondsRealtime(.4f);
                foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 1024) })
                {
                    target = new RenderTexture(size.x, size.y, 24);
                    target.Create();
                    camera.targetTexture = target;
                    Canvas.ForceUpdateCanvases();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    RectTransform panel = GameObject.Find("RunMapDetailPanel").GetComponent<RectTransform>();
                    RectTransform route = GameObject.Find("RunMapRoutePanel").GetComponent<RectTransform>();
                    Assert.That(route.anchoredPosition.x + route.rect.width / 2,
                        Is.EqualTo(panel.anchoredPosition.x - panel.rect.width / 2).Within(0.01f), "No empty gutter at the shared seam.");
                    foreach (TMP_Text label in panel.GetComponentsInChildren<TMP_Text>())
                    {
                        label.ForceMeshUpdate();
                        Assert.That(label.isTextOverflowing, Is.False, title + " / " + label.name);
                        AssertRectInside(label.rectTransform, panel);
                    }
                    AssertRectInside(Property<Button>(RunMapUI, "EventAlternativeButton").GetComponent<RectTransform>(), panel);
                    if (seed == 61) AssertRectInside(Property<Button>(RunMapUI, "LeaveMerchantButton").GetComponent<RectTransform>(), panel);
                    camera.Render();
                    RenderTexture.active = target;
                    var pixels = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    pixels.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,
                        "../event-" + seen.Count + "-" + size.x + ".png")), pixels.EncodeToPNG());
                    UnityEngine.Object.Destroy(pixels);
                    RenderTexture.active = oldTarget;
                    camera.targetTexture = null;
                    target.Release();
                    UnityEngine.Object.Destroy(target);
                    target = null;
                }
            }
            Assert.That(seen.Count, Is.EqualTo(4));
        }
        finally
        {
            RenderTexture.active = oldTarget;
            camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.Destroy(target); }
            canvas.renderMode = originalMode;
            canvas.worldCamera = originalCamera;
            canvas.planeDistance = originalDistance;
            UnityEngine.Object.Destroy(cameraObject);
        }
    }

    [Test]
    public void TC_BRANCH_001_SeededWebAndAllReachableRoutesRemainValid()
    {
        for (int seed = 1; seed <= 100; seed++)
        for (int stages = 8; stages <= 10; stages++)
        {
            object run = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
            InvokeRuntime(run, "StartNewRunWithNodeCount", seed, stages, 73, 4, 100, 10);
            object graph = Property<object>(run, "NodeMap");
            InvokeRuntime(graph, "Validate"); // Includes all path quota combinations.
            object same = InvokeRuntimeStatic("StandardRunNodeMap", "Generate", seed, stages);
            Assert.That(JsonUtility.ToJson(graph), Is.EqualTo(JsonUtility.ToJson(same)));
            Assert.That(Property<Array>(graph, "AllNodes").Length, Is.GreaterThan(stages));
            Assert.That(RuntimeField<string[]>(Property<object>(run, "CurrentNode"), "NextNodeCodes").Length,
                Is.EqualTo(3));
            for (int depth = 1; depth <= stages; depth++)
            {
                Array choices = Property<Array>(run, "ReachableNodes");
                Assert.That(choices.Length, Is.GreaterThan(0));
                object choice = choices.GetValue((seed + depth) % choices.Length);
                string code = RuntimeField<string>(choice, "Code");
                Assert.That((bool)InvokeRuntime(run, "TryEnterNode", code), Is.True);
                Assert.That((bool)InvokeRuntime(run, "TryEnterNode", code), Is.False);
                object type = RuntimeField<object>(choice, "Type");
                Assert.That((bool)InvokeRuntime(run, "TryResolveCurrentNode", type, code), Is.True);
                Assert.That((bool)InvokeRuntime(run, "TryResolveCurrentNode", type, code), Is.False);
                Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(73));
                Assert.That(Property<int>(run, "CurrentMana"), Is.EqualTo(4));
            }
            Assert.That(Property<bool>(run, "IsCompleted"), Is.True);
            Assert.That(Property<string[]>(run, "VisitedNodeCodes").Length, Is.EqualTo(stages));
        }
    }

    [Test]
    public void TC_BRANCH_002_CheckpointKeepsChosenBranchAndRejectsForeignHistory()
    {
        object run = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(run, "StartNewRun", 2468, 77, 6, 100, 10);
        InvokeRuntime(run, "TryAdvanceDepth");
        object chosen = Property<Array>(run, "ReachableNodes").GetValue(2);
        string code = RuntimeField<string>(chosen, "Code");
        Assert.That((bool)InvokeRuntime(run, "TryEnterNode", code), Is.True);
        object snapshot = InvokeRuntime(run, "CaptureState");
        object restored = NewRuntimeObject("RunManager", NewRuntimeObject("RunModifierCollection"));
        InvokeRuntime(restored, "RestoreState", JsonUtility.FromJson(JsonUtility.ToJson(snapshot),
            RuntimeType("SeededRunState")), 100, 10);
        Assert.That(RuntimeField<string>(Property<object>(restored, "CurrentNode"), "Code"), Is.EqualTo(code));
        Assert.That(Property<bool>(restored, "NodeEntered"), Is.True);
        string before = JsonUtility.ToJson(InvokeRuntime(restored, "CaptureState"));
        object corrupt = InvokeRuntime(snapshot, "Clone");
        RuntimeField<string[]>(corrupt, "VisitedNodeCodes")[0] = code;
        Assert.Throws<InvalidOperationException>(() => InvokeRuntime(restored, "RestoreState", corrupt, 100, 10));
        Assert.That(JsonUtility.ToJson(InvokeRuntime(restored, "CaptureState")), Is.EqualTo(before));
        Assert.That(RuntimeField<string[]>(snapshot, "VisitedNodeCodes")[0], Is.Not.EqualTo(code));
        object wrongSelection = InvokeRuntime(snapshot, "Clone");
        SetRuntimeField(wrongSelection, "SelectedNodeCode", "BOSS_10");
        Assert.Throws<InvalidOperationException>(() => InvokeRuntime(restored, "RestoreState", wrongSelection, 100, 10));
        // A post-reward checkpoint has no entered selection; its exact future
        // choices must still be derived from the previously chosen branch.
        InvokeRuntime(run, "TryResolveCurrentNode", RuntimeField<object>(chosen, "Type"), code);
        object pending = InvokeRuntime(run, "CaptureState");
        InvokeRuntime(restored, "RestoreState", pending, 100, 10);
        Assert.That(Property<bool>(restored, "NodeEntered"), Is.False);
        Assert.That(Property<Array>(restored, "ReachableNodes").Cast<object>()
            .Select(n => RuntimeField<string>(n, "Code")),
            Is.EquivalentTo(RuntimeField<string[]>(chosen, "NextNodeCodes")));
    }

    [UnityTest]
    public IEnumerator TC_BRANCH_003_MapOffersConnectedChoicesAndCommitsSelectedNode()
    {
        Call("RestartRun");
        object run = Get<object>("runManager");
        InvokeRuntime(run, "TryAdvanceDepth");
        Call("SyncLegacyRunStateFields");
        Call("OpenRunMap");
        yield return null;
        Button[] buttons = ((IEnumerable)Property<object>(RunMapUI, "NodeButtons")).Cast<Button>().ToArray();
        Array all = Property<Array>(Property<object>(run, "NodeMap"), "AllNodes");
        Assert.That(buttons.Count(b => b.interactable), Is.EqualTo(3));
        Assert.That(Property<Button>(RunMapUI, "EnterNodeButton").interactable, Is.False);
        int selected = Array.FindLastIndex(buttons, b => b.interactable);
        object chosen = all.GetValue(selected);
        string code = RuntimeField<string>(chosen, "Code");
        int hp = Property<int>(run, "CurrentHP");
        buttons[selected].onClick.Invoke();
        Assert.That(Property<string>(RunMapUI, "SelectedNodeCode"), Is.EqualTo(code));
        Assert.That(Property<int>(run, "Depth"), Is.EqualTo(2), "Preview must not advance the run.");
        buttons[buttons.Length - 1].onClick.Invoke();
        Assert.That(Property<string>(RunMapUI, "SelectedNodeCode"), Is.EqualTo(code));
        // This test verifies branch navigation, not a purchase. A fresh run may
        // reach a merchant with no Embers or full resources; leaving is valid.
        bool isMerchant = RuntimeField<object>(chosen, "Type").ToString() == "Merchant";
        Button commit = Property<Button>(RunMapUI, isMerchant ? "LeaveMerchantButton" : "EnterNodeButton");
        Assert.That(commit.interactable, Is.True);
        commit.onClick.Invoke();
        if (Property<bool>(chosen, "IsBattle"))
        {
            Assert.That(IsRunMapOpen, Is.False);
            Assert.That(RuntimeField<string>(CurrentRunNode, "Code"), Is.EqualTo(code));
            Assert.That(Plan, Is.Not.Null);
        }
        else
        {
            Assert.That(IsRunMapOpen, Is.True);
            Assert.That(Property<string[]>(run, "VisitedNodeCodes").Last(), Is.EqualTo(code));
            string[] expected = RuntimeField<string[]>(chosen, "NextNodeCodes");
            Assert.That(Property<Array>(run, "ReachableNodes").Cast<object>()
                .Select(n => RuntimeField<string>(n, "Code")), Is.EquivalentTo(expected));
        }
        int expectedHP = hp;
        if (RuntimeField<object>(chosen, "Type").ToString() == "RandomEvent")
        {
            object encounter = InvokeRuntimeStatic("RunEventService", "GetEncounter", Property<int>(run, "RunSeed"), code);
            if (Property<string>(encounter, "Title") == "THE EMBER ALTAR") expectedHP -= 10;
        }
        Assert.That(Property<int>(run, "CurrentHP"), Is.EqualTo(expectedHP));
    }

    [Test]
    public void TC_BRANCH_004_GraphRejectsBrokenConnectionsAndInvalidPathQuotas()
    {
        object graph = InvokeRuntimeStatic("StandardRunNodeMap", "Generate", 123, 10);
        string json = JsonUtility.ToJson(graph);
        foreach (string corruption in new[] { "backward", "foreign", "duplicate", "deadend", "quota", "null" })
        {
            object broken = JsonUtility.FromJson(json, RuntimeType("StandardRunNodeMap"));
            Array all = Property<Array>(broken, "AllNodes");
            object first = all.GetValue(0);
            object second = all.GetValue(1);
            string firstCode = RuntimeField<string>(first, "Code");
            switch (corruption)
            {
                case "backward": SetRuntimeField(second, "NextNodeCodes", new[] { firstCode }); break;
                case "foreign": SetRuntimeField(second, "NextNodeCodes", new[] { "FOREIGN" }); break;
                case "duplicate":
                    string next = RuntimeField<string[]>(second, "NextNodeCodes")[0];
                    SetRuntimeField(second, "NextNodeCodes", new[] { next, next }); break;
                case "deadend": SetRuntimeField(second, "NextNodeCodes", new string[0]); break;
                case "quota": SetRuntimeField(second, "Type", Enum.Parse(RuntimeType("StandardRunNodeType"), "EliteBattle")); break;
                case "null": RuntimeField<Array>(broken, "Alternatives").SetValue(null, 0); break;
            }
            Assert.Throws<InvalidOperationException>(() => InvokeRuntime(broken, "Validate"), corruption);
        }
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
