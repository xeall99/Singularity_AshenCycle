using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    private const float RewardRevealDelay = 0.80f;

    private enum RewardSelectionKind
    {
        Standard,
        Relic,
        Anomaly
    }

    [Header("UI Text")]
    [SerializeField] private TMP_Text playerHPText;
    [SerializeField] private TMP_Text playerManaText;
    [SerializeField] private TMP_Text enemyHPText;
    [SerializeField] private TMP_Text enemyIntentText;
    [SerializeField] private TMP_Text battleLogText;

    [Header("Battle Buttons")]
    [SerializeField] private Button basicAttackButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button guardButton;
    [SerializeField] private Button analyzeButton;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultDescriptionText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextBattleButton;

    [Header("Reward Panel")]
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private TMP_Text rewardTitleText;
    [SerializeField] private Button rewardButton1;
    [SerializeField] private Button rewardButton2;
    [SerializeField] private Button rewardButton3;

    [Header("Singularity Orbit UI")]
    [SerializeField] private TMP_Text orbitSlot1Text;
    [SerializeField] private TMP_Text orbitSlot2Text;
    [SerializeField] private TMP_Text orbitSlot3Text;
    [SerializeField] private TMP_Text convergencePreviewText;

    [Header("Locked Player Stats")]
    [SerializeField] private int playerMaxHP = 100;
    [SerializeField] private int playerMaxMana = 10;
    [SerializeField] private int playerStartingMana = 5;
    [SerializeField] private int basicAttackDamage = 10;

    [SerializeField]
    [Range(0f, 1f)]
    private float baseGuardDamageReduction = 0.50f;

    [Header("Temporary Reward Values")]
    [SerializeField] private int maxHPRewardAmount = 15;

    [SerializeField]
    [Range(0f, 1f)]
    private float attackRewardPercent = 0.10f;

    [SerializeField]
    [Range(0f, 1f)]
    private float guardRewardPercent = 0.20f;

    [Header("Convergence Settings")]
    [SerializeField]
    [Range(0f, 1f)]
    private float eventHorizonBonus = 0.30f;

    [SerializeField]
    [Range(0f, 1f)]
    private float reversalCounterMultiplier = 0.50f;

    private int playerHP;
    private int playerMana;
    private int enemyHP;

    private int battleNumber = 1;
    private int totalEmbers;

    private int runMaxHPBonus;
    private float runAttackBonusPercent;
    private float runGuardStrengthBonusPercent;
    private int runAttackRewardStacks;
    private int runMaxHPRewardStacks;
    private int runGuardRewardStacks;

    private int rewardRunSeed;
    private int preparedRewardDepth = -1;

    private bool isPlayerTurn;
    private bool isGuarding;
    private bool battleEnded;
    private bool runCompleted;
    private bool reversalCounterReady;
    private bool rewardSelectionOpen;
    private bool rewardSelectionCommitted;
    private bool analyzeUsedThisNode;

    private string convergenceNotice = string.Empty;

    private SkillData shadowStrike;
    private SkillProgressStore skillProgressStore;
    private Button[] equippedSkillButtons = new Button[SkillProgressStore.LoadoutSlotCount];
    private SkillDefinition[] equippedSkills = new SkillDefinition[SkillProgressStore.LoadoutSlotCount];
    private readonly HashSet<string> usedSkillPassives = new HashSet<string>(System.StringComparer.Ordinal);
    private int skillWardRemaining;
    private bool skillParryReady;
    private int nextBasicAttackBonus;
    private int lastBasicAttackDamage;
    private bool markedEnemy;
    private bool analyzeProtectionReady;
    private bool orbitalSeverConvergenceReady;
    private PlayerActionType? previousPlayerAction;
    private int lastSkillLayoutKey = -1;
    private EnemyData currentEnemy;
    private EnemyActionPlan enemyActionPlan;
    private EncounterBannerUI encounterBanner;
    private int encounterTurn;
    private bool bossAwakened;
    private RewardDefinition[] currentRewardChoices;
    private BattleVisuals battleVisuals;
    private BattleCommandMenu battleCommandMenu;
    private OrbitUIAnimator orbitUIAnimator;
    private RewardSelectionUI rewardSelectionUI;
    private RewardManager rewardManager;
    private RunStatController runStatController;
    private RunManager runManager;
    private ActiveRunCheckpointStore checkpointStore;
    private bool checkpointSaveFailed;
    private ActiveRunCheckpointInspection pendingCheckpointInspection;
    private ActiveRunRecoveryUI activeRunRecoveryUI;
    private StandardRunMapUI standardRunMapUI;
    private GuidedTutorialController guidedTutorialController;
    private RunSummaryUI runSummaryUI;
    private SkillRewardSelectionUI skillRewardSelectionUI;
    private EchoFragmentWallet echoWallet;
    private EchoFragmentAward echoAward;
    private EchoFragmentReceipt echoReceipt;
    private bool echoSaveFailed;
    private RunHistoryStore runHistoryStore;
    private bool historySaveFailed;
    private bool historyCommitted;
    private bool terminalSummaryPending;
    private RunSummaryOutcome terminalOutcome;
    private string terminalEndingReason;
    private string terminalEnemyName;
    private Canvas battleCanvas;
    private GameObject commandPanelObject;
    private GameObject orbitPanelObject;
    private GameObject convergenceTooltipRoot;
    private RectTransform convergenceTooltipRect;
    private Coroutine rewardTransitionCoroutine;
    private bool hasReportedMissingOrbitUIAnimator;
    private RewardSelectionKind rewardSelectionKind;
    private bool blackSunFirstConvergenceAvailable;
    private int guardHitsRemaining;
    private int playerStrikeIndex;
    private bool enemyActsFirstThisRound;
    private bool enemyActedThisRound;
    private bool guidedTutorialOpeningTurnStarted;

    public Button GuidedTutorialBasicAttackButton => basicAttackButton;
    public Button GuidedTutorialSkillButton => skillButton;
    public Button GuidedTutorialGuardButton => guardButton;
    public Button GuidedTutorialAnalyzeButton => analyzeButton;
    public bool GuidedTutorialCanPlayerAct => CanPlayerAct();
    public bool GuidedTutorialBattleEnded => battleEnded;
    public bool GuidedTutorialEnemyActsFirst => enemyActsFirstThisRound;

    public void BeginGuidedTutorialEnemyOpeningTurn()
    {
        if (!enemyActsFirstThisRound || battleEnded || guidedTutorialOpeningTurnStarted)
            return;
        guidedTutorialOpeningTurnStarted = true;
        StartCoroutine(EnemyOpeningTurn());
    }

    private readonly OrbitState orbitState = new OrbitState();
    private readonly OrbitResolver orbitResolver = new OrbitResolver();
    private readonly BattleStatusController battleStatuses =
        new BattleStatusController();
    private readonly RunModifierCollection runModifiers =
        new RunModifierCollection();

    private void Awake()
    {
        InitializeRunStatController();
        InitializeRunManager();
        InitializeCheckpointStore();
        battleVisuals = GetComponent<BattleVisuals>();
        CacheBattlePresentation();
        SubscribeCommandPreview();
        HideEnemyIntent();
        HideConvergencePreview();

        if (analyzeButton != null)
        {
            analyzeButton.onClick.AddListener(UseAnalyze);
        }

        if (basicAttackButton != null)
        {
            basicAttackButton.onClick.AddListener(UseBasicAttack);
        }

        if (skillButton != null)
        {
            skillButton.onClick.AddListener(UseSkill);
        }

        if (guardButton != null)
        {
            guardButton.onClick.AddListener(UseGuard);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RestartRun);
        }

        if (nextBattleButton != null)
        {
            nextBattleButton.onClick.AddListener(OpenRewardSelection);
        }

        if (rewardButton1 != null)
        {
            rewardButton1.onClick.AddListener(ChooseAttackReward);
        }

        if (rewardButton2 != null)
        {
            rewardButton2.onClick.AddListener(ChooseMaxHPReward);
        }

        if (rewardButton3 != null)
        {
            rewardButton3.onClick.AddListener(ChooseGuardReward);
        }
    }

    private void Start()
    {
        SetPanelActive(resultPanel, false);
        SetPanelActive(rewardPanel, false);
        PrepareConvergenceTooltip();
        EnsureRewardSelectionUI();
        EnsureStandardRunMapUI();
        EnsureGuidedTutorialController();

        if (GameDatabase.Instance == null ||
            !GameDatabase.Instance.IsReady)
        {
            ShowDatabaseError("GameDatabase belum siap.");
            return;
        }

        shadowStrike = GameDatabase.Instance.GetSkillByCode(
            "SKL_SHADOW_STRIKE"
        );

        if (shadowStrike == null)
        {
            ShowDatabaseError(
                "Shadow Strike tidak ditemukan di database."
            );

            return;
        }

        try
        {
            skillProgressStore = SkillProgressStore.CreateDefault();
            InitializeSkillButtons();
            string[] profileLoadout = skillProgressStore.GetLoadout();
            for (int slot = 0; slot < profileLoadout.Length; slot++)
                equippedSkills[slot] = SkillCatalog.Get(profileLoadout[slot]);
            RefreshSkillButtonPresentation();
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Skill Archive belum tersedia: " + exception.Message);
        }

        if (skillProgressStore == null && equippedSkills[0] == null)
            equippedSkills[0] = SkillCatalog.Get("SKL_SHADOW_STRIKE");
        RefreshSkillButtonPresentation();

        ConfigureRewardButtonText();

        Debug.Log(
            $"BattleManager menerima skill: " +
            $"{SkillCatalog.Get("SKL_SHADOW_STRIKE").Name} | " +
            $"Mana: {SkillCatalog.Get("SKL_SHADOW_STRIKE").ManaCost} | " +
            $"Power: {SkillCatalog.Get("SKL_SHADOW_STRIKE").Power}"
        );

        PresentStartupRunChoice();
    }

    private void Update()
    {
        if (runManager != null && runManager.IsActive && Time.timeScale > 0f)
        {
            runManager.AdvanceElapsedTime(Time.unscaledDeltaTime);
        }
    }

    private void InitializeSkillButtons()
    {
        if (equippedSkillButtons[0] == null) equippedSkillButtons[0] = skillButton;
        if (skillButton == null) return;

        var additional = new List<Button>();
        for (int slot = 1; slot < SkillProgressStore.LoadoutSlotCount; slot++)
        {
            GameObject clone = Instantiate(skillButton.gameObject, skillButton.transform.parent);
            clone.name = "SkillButtonSlot" + (slot + 1);
            UnityEngine.EventSystems.EventTrigger inheritedTrigger =
                clone.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (inheritedTrigger != null) inheritedTrigger.triggers.Clear();

            Button button = clone.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            int capturedSlot = slot;
            button.onClick.AddListener(() => UseSkillSlot(capturedSlot));
            equippedSkillButtons[slot] = button;
            additional.Add(button);
        }

        if (battleCommandMenu == null)
            battleCommandMenu = GetComponent<BattleCommandMenu>();
        if (battleCommandMenu != null)
            battleCommandMenu.RegisterAdditionalButtons(additional.ToArray());
    }

    private void RefreshSkillButtonPresentation()
    {
        if (equippedSkillButtons[0] == null && skillButton != null)
            equippedSkillButtons[0] = skillButton;

        string[] codes = runManager != null && runManager.IsActive
            ? runManager.EquippedSkillCodes
            : skillProgressStore == null ? null : skillProgressStore.GetLoadout();
        if (codes != null)
        {
            for (int slot = 0; slot < equippedSkills.Length; slot++)
                equippedSkills[slot] = slot < codes.Length
                    ? SkillCatalog.Get(codes[slot]) : null;
        }
        if (codes == null)
            equippedSkills[0] = SkillCatalog.Get("SKL_SHADOW_STRIKE");
        shadowStrike = equippedSkills[0]?.Data;

        int activeSkillCount = 0;
        for (int slot = 0; slot < equippedSkillButtons.Length; slot++)
        {
            Button button = equippedSkillButtons[slot];
            if (button == null) continue;
            SkillDefinition definition = equippedSkills[slot];
            bool equipped = definition != null;
            button.gameObject.SetActive(equipped);
            if (!equipped) continue;
            activeSkillCount++;
            SetButtonText(button, $"{definition.Name} ({definition.ManaCost} Mana)");
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.fontSizeMax = 22f;
                label.textWrappingMode = TMPro.TextWrappingModes.Normal;
            }
        }

        var row = new List<RectTransform>();
        if (basicAttackButton != null) row.Add(basicAttackButton.GetComponent<RectTransform>());
        for (int slot = 0; slot < equippedSkillButtons.Length; slot++)
            if (equippedSkillButtons[slot] != null && equippedSkillButtons[slot].gameObject.activeSelf)
                row.Add(equippedSkillButtons[slot].GetComponent<RectTransform>());
        if (guardButton != null) row.Add(guardButton.GetComponent<RectTransform>());
        if (analyzeButton != null) row.Add(analyzeButton.GetComponent<RectTransform>());

        const float commandSpacing = 210f;
        if (lastSkillLayoutKey != activeSkillCount)
        {
            for (int index = 0; index < row.Count; index++)
            {
                row[index].anchoredPosition = new Vector2(
                    (index - (row.Count - 1) * 0.5f) * commandSpacing,
                    -390f
                );
                row[index].sizeDelta = new Vector2(190f, row[index].sizeDelta.y);
            }
            lastSkillLayoutKey = activeSkillCount;
            battleCommandMenu?.RefreshRestingLayout();
        }
    }

    private int relicBurnStacks;
    private int relicBurnTurns;
    private bool relicFirstSkillUsed;
    private bool relicFirstAttackUsed;

    private bool HasRelic(RewardEffectType effect) =>
        GetRunManager().IsActive && RunRelicCatalog.Has(GetRunManager().Modifiers, effect);

    private bool HasAnomaly(RewardEffectType effect) =>
        GetRunManager().IsActive && RunAnomalyCatalog.Has(
            GetRunManager().Modifiers,
            effect
        );

    private void StartBattle()
    {
        RunManager activeRun = GetRunManager();
        StandardRunNode currentNode = activeRun.CurrentNode;

        if (!activeRun.IsActive ||
            currentNode == null ||
            !currentNode.IsBattle)
        {
            ShowDatabaseError(
                "Node aktif bukan encounter battle yang valid. " +
                "Buka ulang Run Map atau Restart Run."
            );
            return;
        }

        StopAllCoroutines();
        encounterBanner?.Cancel();
        rewardTransitionCoroutine = null;
        rewardSelectionUI?.HideImmediate();
        standardRunMapUI?.HideImmediate();
        enemyActionPlan = null;
        currentRewardChoices = null;
        preparedRewardDepth = -1;
        rewardSelectionKind = RewardSelectionKind.Standard;
        HideEnemyIntent();
        HideConvergencePreview();

        int encounterIndex = GetEncounterDatabaseIndex(
            activeRun.NodeMap,
            currentNode
        );
        currentEnemy = GameDatabase.Instance.GetEnemyByBattleIndex(
            encounterIndex
        );

        if (currentEnemy == null)
        {
            ShowDatabaseError(
                $"Enemy encounter index {encounterIndex} " +
                "tidak ditemukan di database."
            );

            return;
        }

        // Node transitions preserve spent resources; only a new run refills them.
        activeRun.ClampResources(
            GetCurrentPlayerMaxHP(),
            GetCurrentPlayerMaxMana()
        );
        if (HasRelic(RewardEffectType.RelicEntryHeal))
            activeRun.RestoreHP(6, GetCurrentPlayerMaxHP());
        battleStatuses.Clear();
        SyncLegacyBurnFields();
        relicFirstSkillUsed = false;
        relicFirstAttackUsed = false;
        blackSunFirstConvergenceAvailable = HasAnomaly(
            RewardEffectType.AnomalyBlackSun
        );
        SyncLegacyRunStateFields();
        enemyHP = currentEnemy.MaxHP;
        encounterTurn = 0;
        bossAwakened = false;
        guardHitsRemaining = 0;
        playerStrikeIndex = 0;
        enemyActsFirstThisRound = !BattleTurnRules.PlayerActsFirst(
            GetRunStatController().EffectiveSpeed
        );
        guidedTutorialOpeningTurnStarted = false;
        enemyActedThisRound = false;

        isPlayerTurn = !enemyActsFirstThisRound;
        isGuarding = false;
        battleEnded = false;
        rewardSelectionOpen = false;
        rewardSelectionCommitted = false;

        ResetOrbitForBattle();
        analyzeUsedThisNode = false;

        if (!PrepareEnemyActionPlan())
        {
            return;
        }

        SetPanelActive(resultPanel, false);
        SetPanelActive(rewardPanel, false);
        SetBattleChromeVisible(true);

        if (EnsureOrbitUIAnimator(true))
        {
            orbitUIAnimator.SynchronizeSlots(orbitState.Actions, false);
            orbitUIAnimator.PlayStageEntrance();
        }

        SetButtonText(retryButton, "Restart Run");
        SetButtonText(nextBattleButton, "Choose Reward");

        string battleType = GetBattleNodeLabel(currentNode);

        if (currentNode.Type == StandardRunNodeType.EliteBattle || currentNode.Type == StandardRunNodeType.Boss)
            StartCoroutine(PlayEncounterIntroduction(currentNode.Type == StandardRunNodeType.Boss));

        SetBattleLog(
            $"{battleType}: {currentEnemy.Name} - " +
            "Giliran Player."
        );

        Debug.Log(
            $"Memulai node {currentNode.Depth} " +
            $"({currentNode.Type}): " +
            $"{currentEnemy.Name} | " +
            $"Encounter DB: {encounterIndex} | " +
            $"HP: {currentEnemy.MaxHP} | " +
            $"Damage: {currentEnemy.AttackDamage} | " +
            $"Reward: {currentEnemy.RewardEmbers} | " +
            GetRunStatsSummary()
        );

        UpdateUI();
        UpdateButtons();
        bool guidedTutorialHoldsOpeningTurn = guidedTutorialController != null &&
            guidedTutorialController.BeginFirstBattle();
        if (enemyActsFirstThisRound && !guidedTutorialHoldsOpeningTurn)
        {
            guidedTutorialOpeningTurnStarted = true;
            StartCoroutine(EnemyOpeningTurn());
        }
    }

    private void UseBasicAttack()
    {
        if (!CanPlayerAct())
        {
            return;
        }

        HideConvergencePreview();
        int enemyHPBeforeAction = enemyHP;
        bool enemyWasBurning = battleStatuses.GetStacks(
            BattleStatusEffectType.Burn, BattleStatusTarget.Enemy) > 0;
        bool enemyWasWeakened = battleStatuses.GetStacks(
            BattleStatusEffectType.Weakness, BattleStatusTarget.Enemy) > 0;
        bool enemyWasCursed = battleStatuses.GetStacks(
            BattleStatusEffectType.Curse, BattleStatusTarget.Enemy) > 0;
        bool enemyInExecuteRange = currentEnemy != null &&
            enemyHP <= currentEnemy.MaxHP * 0.35f;
        int baseAttack = GetCurrentBasicAttackDamage();
        bool consumedMark = markedEnemy;
        if (consumedMark)
        {
            baseAttack += 8;
            markedEnemy = false;
        }
        baseAttack += nextBasicAttackBonus;
        baseAttack = ApplyHitDamagePassives(baseAttack, enemyWasBurning);
        nextBasicAttackBonus = 0;
        if (!relicFirstAttackUsed && HasRelic(RewardEffectType.RelicFirstAttack))
        {
            baseAttack += 4;
            relicFirstAttackUsed = true;
        }
        int currentAttackDamage = ResolvePlayerDamage(baseAttack);
        bool critical = RollPlayerCritical(
            !HasAnomaly(RewardEffectType.AnomalyInvertedPulse)
        );
        if (critical)
            currentAttackDamage = BattleTurnRules.ApplyCritical(currentAttackDamage);

        battleVisuals?.PlayPlayerAttack();
        battleVisuals?.PlayEnemyHit();

        enemyHP = Mathf.Max(0, enemyHP - currentAttackDamage);
        lastBasicAttackDamage = currentAttackDamage;
        int restoredHP = HasAnomaly(RewardEffectType.AnomalyInvertedPulse)
            ? GetRunManager().RestoreHP(2, GetCurrentPlayerMaxHP())
            : 0;
        if (restoredHP > 0) SyncLegacyRunStateFields();
        if (enemyHP > 0 && HasRelic(RewardEffectType.RelicAttackBurn))
        {
            battleStatuses.Apply(
                BattleStatusEffectType.Burn,
                BattleStatusTarget.Enemy
            );
            SyncLegacyBurnFields();
        }
        if (consumedMark && enemyHP > 0 && TryUsePassive("SKL_BLACKSTAR_BRAND"))
            battleStatuses.Apply(BattleStatusEffectType.Weakness, BattleStatusTarget.Enemy, 1, 1);
        ApplyHitPassives(enemyWasWeakened, enemyWasCursed, enemyInExecuteRange, enemyHP > 0);
        if (enemyHP <= 0) ApplyKillPassives(enemyWasCursed, false);

        string actionLog =
            $"Player menggunakan Basic Attack. " +
            $"Damage: {currentAttackDamage}" +
            (critical ? " (CRITICAL)" : string.Empty) +
            (restoredHP > 0 ? $". HP +{restoredHP}." : ".");

        string orbitLog = RecordOrbitAction(
            PlayerActionType.Attack,
            currentAttackDamage
        );
        GetRunManager().RecordPlayerAction(
            enemyHPBeforeAction - enemyHP,
            critical
        );
        SyncLegacyBurnFields();
        SyncLegacyRunStateFields();
        previousPlayerAction = PlayerActionType.Attack;

        SetBattleLog(actionLog, orbitLog);
        FinishPlayerAction();
        guidedTutorialController?.OnCombatActionResolved(PlayerActionType.Attack);
    }

    private void UseSkill()
    {
        UseSkillSlot(0);
    }

    private void UseSkillSlot(int slot)
    {
        if (!CanPlayerAct()) return;
        RefreshSkillButtonPresentation();
        if (slot < 0 || slot >= equippedSkills.Length || equippedSkills[slot] == null)
        {
            SetBattleLog("Tidak ada skill yang dipasang pada slot ini.");
            return;
        }

        SkillDefinition definition = equippedSkills[slot];
        RunManager activeRun = GetRunManager();
        int manaCost = GetCurrentSkillManaCost(slot);
        if (activeRun.CurrentMana < manaCost)
        {
            SetBattleLog("Mana tidak cukup.");
            return;
        }
        if (definition.ActiveEffect == SkillActiveEffect.SelfSacrifice && activeRun.CurrentHP <= 3 ||
            definition.ActiveEffect == SkillActiveEffect.NightfallVow && activeRun.CurrentHP <= 5)
        {
            SetBattleLog("HP terlalu rendah untuk menggunakan skill ini.");
            return;
        }

        HideConvergencePreview();
        int enemyHPBeforeAction = enemyHP;
        int playerHPBeforeAction = activeRun.CurrentHP;
        bool enemyWasBurning = battleStatuses.GetStacks(
            BattleStatusEffectType.Burn, BattleStatusTarget.Enemy) > 0;
        bool enemyWasWeakened = battleStatuses.GetStacks(
            BattleStatusEffectType.Weakness, BattleStatusTarget.Enemy) > 0;
        bool enemyWasCursed = battleStatuses.GetStacks(
            BattleStatusEffectType.Curse, BattleStatusTarget.Enemy) > 0;
        bool enemyInExecuteRange = enemyHP <= currentEnemy.MaxHP * 0.35f;
        int consumedBurn = 0;
        int damage = definition.Power;
        bool dealsDamage = true;
        bool critical = false;
        bool cleansePlayer = false;
        int healAmount = 0;
        int manaRestore = 0;

        switch (definition.ActiveEffect)
        {
            case SkillActiveEffect.Damage:
                if (definition.Code == "SKL_SHADOW_STRIKE" && analyzeUsedThisNode &&
                    TryUsePassive(definition.Code)) damage += 4;
                break;
            case SkillActiveEffect.Burn:
                break;
            case SkillActiveEffect.Weakness:
                break;
            case SkillActiveEffect.HealAndCleanse:
                dealsDamage = false;
                healAmount = definition.Power;
                cleansePlayer = true;
                break;
            case SkillActiveEffect.HealAndWard:
                dealsDamage = false;
                healAmount = definition.Power;
                skillWardRemaining += 5;
                break;
            case SkillActiveEffect.Curse:
                break;
            case SkillActiveEffect.DamageAndParry:
                skillParryReady = true;
                break;
            case SkillActiveEffect.RestoreMana:
                dealsDamage = false;
                manaRestore = definition.Power;
                break;
            case SkillActiveEffect.BurnBonus:
                if (enemyWasBurning) damage += 4;
                break;
            case SkillActiveEffect.Siphon:
                if (enemyWasCursed) healAmount += 4;
                break;
            case SkillActiveEffect.Mark:
                markedEnemy = true;
                break;
            case SkillActiveEffect.SelfSacrifice:
                break;
            case SkillActiveEffect.Execute30:
                if (enemyHP <= currentEnemy.MaxHP * 0.30f) damage += 11;
                break;
            case SkillActiveEffect.DamageAndWard:
                skillWardRemaining = 5;
                break;
            case SkillActiveEffect.EchoDamage:
                damage += Mathf.Min(5, lastBasicAttackDamage / 2);
                break;
            case SkillActiveEffect.BurnVerdict:
                consumedBurn = battleStatuses.ConsumeStacks(
                    BattleStatusEffectType.Burn, BattleStatusTarget.Enemy, 3);
                damage += consumedBurn * 4;
                break;
            case SkillActiveEffect.WeaknessBonus:
                if (enemyWasWeakened)
                {
                    damage += 4;
                }
                break;
            case SkillActiveEffect.BurnSustain:
                break;
            case SkillActiveEffect.BurnAndSustain:
                healAmount = 2;
                break;
            case SkillActiveEffect.WardOnly:
                dealsDamage = false;
                skillWardRemaining = definition.Power;
                break;
            case SkillActiveEffect.DamageAndCleanse:
                cleansePlayer = true;
                break;
            case SkillActiveEffect.OrbitSever:
                if (orbitState.Count == 2) damage += 4;
                orbitalSeverConvergenceReady = true;
                break;
            case SkillActiveEffect.AnalyzeDamage:
                if (analyzeUsedThisNode) damage += 4;
                break;
            case SkillActiveEffect.BurnConsume:
                consumedBurn = battleStatuses.ConsumeStacks(
                    BattleStatusEffectType.Burn, BattleStatusTarget.Enemy, 1);
                if (consumedBurn > 0) damage += 6;
                break;
            case SkillActiveEffect.OrbitEcho:
                damage += GetDistinctRecentActionCount() * 2;
                damage = System.Math.Min(definition.Power + 4, damage);
                break;
            case SkillActiveEffect.GuardEcho:
                if (previousPlayerAction == PlayerActionType.Guard) damage += 4;
                break;
            case SkillActiveEffect.NightfallVow:
                break;
            case SkillActiveEffect.GravityThread:
                break;
            case SkillActiveEffect.CurseBrand:
                if (enemyWasCursed) damage += 6;
                break;
            case SkillActiveEffect.MourningBell:
                if (HasRecentAttackAndGuard()) damage += 6;
                break;
            case SkillActiveEffect.UmbralExecution:
                if (enemyHP <= currentEnemy.MaxHP * 0.35f) damage += 9;
                break;
        }

        if (!activeRun.TrySpendMana(manaCost))
        {
            SetBattleLog("Mana tidak cukup.");
            return;
        }
        if (dealsDamage) damage = ApplyHitDamagePassives(damage, enemyWasBurning);
        if (definition.Code == "SKL_VESPER_STITCH" &&
            playerHPBeforeAction <= GetCurrentPlayerMaxHP() * 0.40f)
            TryUsePassive(definition.Code);

        if (dealsDamage)
        {
            if (!relicFirstSkillUsed && HasRelic(RewardEffectType.RelicFirstSkill))
                damage = (int)System.Math.Min(int.MaxValue, (long)damage + 6);
            if (HasAnomaly(RewardEffectType.AnomalyCollapsedStar))
                damage = Mathf.RoundToInt(damage * 1.30f);
            damage = ResolvePlayerDamage(damage);
            critical = RollPlayerCritical(true);
            if (critical) damage = BattleTurnRules.ApplyCritical(damage);
            enemyHP = Mathf.Max(0, enemyHP - damage);
            relicFirstSkillUsed = true;
            battleVisuals?.PlayPlayerAttack();
            if (damage > 0) battleVisuals?.PlayEnemyHit();
        }
        else
        {
            relicFirstSkillUsed = true;
        }

        switch (definition.ActiveEffect)
        {
            case SkillActiveEffect.Burn:
            case SkillActiveEffect.BurnAndSustain:
                if (enemyHP > 0) battleStatuses.Apply(BattleStatusEffectType.Burn, BattleStatusTarget.Enemy, 1, 2);
                break;
            case SkillActiveEffect.Weakness:
                if (enemyHP > 0) battleStatuses.Apply(BattleStatusEffectType.Weakness, BattleStatusTarget.Enemy, 1, 2);
                break;
            case SkillActiveEffect.Curse:
                if (enemyHP > 0) battleStatuses.Apply(BattleStatusEffectType.Curse, BattleStatusTarget.Enemy, 1, 2);
                break;
            case SkillActiveEffect.GravityThread:
                if (enemyHP > 0)
                {
                    int weaknessDuration = enemyWasWeakened ? 3 : 2;
                    if (TryUsePassive("SKL_GRAVITY_THREAD")) weaknessDuration++;
                    battleStatuses.Apply(BattleStatusEffectType.Weakness,
                        BattleStatusTarget.Enemy, 1, weaknessDuration);
                }
                break;
            case SkillActiveEffect.CurseBrand:
                if (!enemyWasCursed && enemyHP > 0)
                    battleStatuses.Apply(BattleStatusEffectType.Curse, BattleStatusTarget.Enemy, 1, 2);
                break;
        }
        if (cleansePlayer)
        {
            bool removed = battleStatuses.RemoveOne(BattleStatusEffectType.Weakness, BattleStatusTarget.Player);
            if (!removed) removed = battleStatuses.RemoveOne(BattleStatusEffectType.Curse, BattleStatusTarget.Player);
            if (removed && definition.Code == "SKL_REQUIEM_OF_GLASS" && enemyHP > 0 &&
                TryUsePassive(definition.Code))
                battleStatuses.Apply(BattleStatusEffectType.Weakness, BattleStatusTarget.Enemy, 1, 1);
        }
        if (healAmount > 0) activeRun.RestoreHP(healAmount, GetCurrentPlayerMaxHP());
        if (manaRestore > 0) activeRun.RestoreMana(manaRestore, GetCurrentPlayerMaxMana());
        if (definition.ActiveEffect == SkillActiveEffect.SelfSacrifice)
        {
            activeRun.ApplyDamage(3);
            if (TryUsePassive("SKL_VOW_OF_ASH")) skillWardRemaining += 3;
        }
        else if (definition.ActiveEffect == SkillActiveEffect.NightfallVow)
        {
            activeRun.ApplyDamage(5);
            if (TryUsePassive("SKL_NIGHTFALL_VOW"))
                activeRun.RestoreMana(2, GetCurrentPlayerMaxMana());
        }
        if (consumedBurn > 0 && definition.Code == "SKL_CINDER_KEY" &&
            TryUsePassive("SKL_CINDER_KEY"))
            activeRun.AddEmbers(1);
        if (definition.Code == "SKL_ECHO_BRAID" && TryUsePassive(definition.Code))
            nextBasicAttackBonus += 3;
        if (definition.Code == "SKL_SABLE_SUTURE")
            SetPassiveReady(definition.Code);

        SyncLegacyBurnFields();
        SyncLegacyRunStateFields();
        if (definition.ActiveEffect == SkillActiveEffect.BurnVerdict &&
            consumedBurn >= 2 && enemyHP > 0 && TryUsePassive(definition.Code))
            battleStatuses.Apply(BattleStatusEffectType.Curse, BattleStatusTarget.Enemy, 1, 1);
        string actionLog = $"Player menggunakan {definition.Name}.";
        if (dealsDamage) actionLog += $" Damage: {damage}" + (critical ? " (CRITICAL)." : ".");
        if (healAmount > 0) actionLog += $" HP +{healAmount}.";
        if (manaRestore > 0) actionLog += $" Mana +{manaRestore}.";
        if (definition.ActiveEffect == SkillActiveEffect.SelfSacrifice) actionLog += " HP -3.";
        if (definition.ActiveEffect == SkillActiveEffect.NightfallVow) actionLog += " HP -5.";

        int orbitDamage = dealsDamage ? damage : 0;
        string orbitLog = RecordOrbitAction(PlayerActionType.Skill, orbitDamage,
            definition.Code);
        int appliedDamage = enemyHPBeforeAction - enemyHP;
        activeRun.RecordPlayerAction(appliedDamage, critical);
        ApplyHitPassives(enemyWasWeakened, enemyWasCursed, enemyInExecuteRange, enemyHP > 0);
        if (enemyHP <= 0) ApplyKillPassives(enemyWasCursed, true);
        if (previousPlayerAction == PlayerActionType.Guard &&
            TryUsePassive("SKL_EMBER_REPRISE"))
        {
            activeRun.RestoreMana(1, GetCurrentPlayerMaxMana());
            SyncLegacyRunStateFields();
        }
        previousPlayerAction = PlayerActionType.Skill;
        SetBattleLog(actionLog + orbitLog);
        FinishPlayerAction();
        guidedTutorialController?.OnCombatActionResolved(PlayerActionType.Skill);
    }

    private void SetPassiveReady(string skillCode)
    {
        // The one-use marker is claimed when the corresponding follow-up resolves.
        if (!usedSkillPassives.Contains(skillCode)) passiveReadySkills.Add(skillCode);
    }

    private readonly HashSet<string> passiveReadySkills =
        new HashSet<string>(System.StringComparer.Ordinal);

    private bool TryUsePassive(string skillCode)
    {
        if (string.IsNullOrWhiteSpace(skillCode) || !HasEquippedSkill(skillCode)) return false;
        passiveReadySkills.Remove(skillCode);
        return usedSkillPassives.Add(skillCode);
    }

    private bool HasEquippedSkill(string skillCode)
    {
        foreach (SkillDefinition definition in equippedSkills)
            if (definition != null && definition.Code == skillCode) return true;
        return false;
    }

    private int GetDistinctRecentActionCount()
    {
        if (orbitState.Count == 0) return 0;
        var actions = new HashSet<PlayerActionType>();
        int start = Mathf.Max(0, orbitState.Count - 2);
        for (int i = start; i < orbitState.Count; i++) actions.Add(orbitState.Actions[i]);
        return actions.Count;
    }

    private bool HasRecentAttackAndGuard()
    {
        if (orbitState.Count < 2) return false;
        bool attack = false;
        bool guard = false;
        for (int i = orbitState.Count - 2; i < orbitState.Count; i++)
        {
            attack |= orbitState.Actions[i] == PlayerActionType.Attack;
            guard |= orbitState.Actions[i] == PlayerActionType.Guard;
        }
        return attack && guard;
    }

    private int ApplyHitDamagePassives(int damage, bool enemyWasBurning)
    {
        if (enemyWasBurning && TryUsePassive("SKL_CINDER_REFRAIN")) damage += 2;
        return damage;
    }

    private void ApplyHitPassives(
        bool enemyWasWeakened,
        bool enemyWasCursed,
        bool enemyInExecuteRange,
        bool targetSurvived
    )
    {
        RunManager activeRun = GetRunManager();
        if (enemyWasWeakened && TryUsePassive("SKL_FRACTURE_LANCE"))
            activeRun.RestoreMana(1, GetCurrentPlayerMaxMana());
        if (enemyInExecuteRange && TryUsePassive("SKL_UMBRAL_EXECUTION"))
            skillWardRemaining += 4;
        if (enemyWasCursed && targetSurvived && TryUsePassive("SKL_PAPER_CROWN"))
            battleStatuses.ExtendDuration(BattleStatusEffectType.Curse,
                BattleStatusTarget.Enemy, 1);
    }

    private void ApplyKillPassives(bool enemyWasCursed, bool skillKill)
    {
        if (enemyWasCursed && TryUsePassive("SKL_RIFT_SIPHON"))
            GetRunManager().RestoreHP(4, GetCurrentPlayerMaxHP());
        if (skillKill && TryUsePassive("SKL_GALLOWS_BLOOM"))
            GetRunManager().RestoreMana(1, GetCurrentPlayerMaxMana());
        SyncLegacyRunStateFields();
    }

    private void UseGuard()
    {
        if (!CanPlayerAct())
        {
            return;
        }

        HideConvergencePreview();
        battleVisuals?.PlayGuard();

        isGuarding = true;
        guardHitsRemaining = HasAnomaly(RewardEffectType.AnomalyFrozenOrbit)
            ? 2 : 1;
        GetRunManager().RestoreMana(
            HasRelic(RewardEffectType.RelicGuardMana) ? 3 : 2,
            GetCurrentPlayerMaxMana()
        );
        SyncLegacyRunStateFields();

        if (passiveReadySkills.Contains("SKL_GLOAMING_BULWARK") &&
            TryUsePassive("SKL_GLOAMING_BULWARK"))
            guardHitsRemaining = Mathf.Min(guardHitsRemaining + 1,
                HasAnomaly(RewardEffectType.AnomalyFrozenOrbit) ? 3 : 2);

        int guardReductionPercent = Mathf.RoundToInt(
            GetCurrentGuardDamageReduction() * 100f
        );

        string actionLog =
            "Player menggunakan Guard. " +
            $"{guardHitsRemaining} serangan berikutnya berkurang " +
            $"{guardReductionPercent}%.";

        string orbitLog = RecordOrbitAction(
            PlayerActionType.Guard,
            0
        );
        GetRunManager().RecordPlayerAction(0, false);
        previousPlayerAction = PlayerActionType.Guard;

        RunManager activeRun = GetRunManager();
        if (passiveReadySkills.Contains("SKL_SABLE_SUTURE") && TryUsePassive("SKL_SABLE_SUTURE"))
            activeRun.RestoreHP(2, GetCurrentPlayerMaxHP());
        SyncLegacyRunStateFields();

        SetBattleLog(actionLog, orbitLog);
        FinishPlayerAction();
        guidedTutorialController?.OnCombatActionResolved(PlayerActionType.Guard);
    }

    private int GetAnalyzeManaCost()
    {
        return GetRunStatController().AnalyzeManaCost;
    }

    private void UseAnalyze()
    {
        if (!CanPlayerAct() || currentEnemy == null)
        {
            return;
        }

        if (analyzeUsedThisNode)
        {
            SetBattleLog("Analyze sudah digunakan di node ini.");
            return;
        }

        int manaCost = GetAnalyzeManaCost();
        RunManager activeRun = GetRunManager();

        if (activeRun.CurrentMana < manaCost)
        {
            SetBattleLog("Mana tidak cukup untuk Analyze");
            return;
        }

        if (enemyActionPlan == null)
        {
            ShowEnemyPlanError();
            return;
        }

        HideConvergencePreview();

        if (!activeRun.TrySpendMana(manaCost))
        {
            SetBattleLog("Mana tidak cukup untuk Analyze");
            return;
        }

        SyncLegacyRunStateFields();
        analyzeUsedThisNode = true;
        analyzeProtectionReady = HasEquippedSkill("SKL_ASTERION_GAZE");

        RevealEnemyActionPlan();
        SetBattleLog(
            $"Analyze menggunakan {manaCost} Mana. Giliran Player berlanjut."
        );

        // Analyze is informational: it neither records an Orbit action nor ends the turn.
        UpdateUI();
        UpdateButtons();
        guidedTutorialController?.OnAnalyzeResolved();
    }

    private IEnumerator PlayEncounterIntroduction(bool boss, bool awakened = false)
    {
        if (battleCanvas == null && playerHPText != null)
            battleCanvas = playerHPText.GetComponentInParent<Canvas>(true);
        if (battleCanvas == null) yield break;
        if (encounterBanner == null)
            encounterBanner = battleCanvas.GetComponent<EncounterBannerUI>() ?? battleCanvas.gameObject.AddComponent<EncounterBannerUI>();
        IEnumerator animation = encounterBanner.Play(currentEnemy.Name, boss, awakened);
        // Start the iterator before updating controls so input is blocked immediately.
        if (animation.MoveNext())
        {
            UpdateButtons();
            do { yield return animation.Current; } while (animation.MoveNext());
        }
        UpdateButtons();
    }

    private bool PrepareEnemyActionPlan(int? plannedTurn = null)
    {
        if (enemyActionPlan != null)
        {
            return true;
        }

        if (currentEnemy == null || currentEnemy.AttackDamage < 0)
        {
            ShowEnemyPlanError();
            return false;
        }

        enemyActionPlan = EnemyActionPlan.CreateEncounterAction(
            GetRunManager().CurrentNode.Type, currentEnemy.Id,
            currentEnemy.AttackDamage, plannedTurn ?? encounterTurn, bossAwakened
        );
        return true;
    }

    private void RevealEnemyActionPlan()
    {
        if (enemyIntentText == null)
        {
            return;
        }

        enemyIntentText.text =
            "ANALYZE RESULT\n" +
            $"Action: {enemyActionPlan.DisplayName}\n" +
            $"Target: {enemyActionPlan.Target}\n" +
            $"Hits: {enemyActionPlan.HitCount}\n" +
            $"Estimated Damage: {enemyActionPlan.TotalEstimatedDamage}\n" +
            $"Effect: {enemyActionPlan.EffectDescription}";

        // The existing top-right text area was sized for one line of intent.
        // Grow downwards at runtime without changing its Inspector reference or scene.
        enemyIntentText.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            enemyIntentText.GetPreferredValues(
                enemyIntentText.text, enemyIntentText.rectTransform.rect.width, 0f
            ).y
        );
        enemyIntentText.gameObject.SetActive(true);
    }

    private void HideEnemyIntent()
    {
        if (enemyIntentText != null)
        {
            enemyIntentText.text = string.Empty;
            enemyIntentText.gameObject.SetActive(false);
        }
    }

    private void ShowEnemyPlanError()
    {
        const string message = "EnemyActionPlan tidak tersedia atau tidak valid. Restart Run diperlukan.";
        Debug.LogError(message);
        battleEnded = true;
        isPlayerTurn = false;
        HideEnemyIntent();
        HideConvergencePreview();
        DisableAllBattleButtons();
        SetPanelActive(rewardPanel, false);
        SetText(resultTitleText, "BATTLE ERROR");
        SetText(resultDescriptionText, message);
        SetButtonText(retryButton, "Restart Run");
        SetObjectActive(retryButton, true);
        SetObjectActive(nextBattleButton, false);
        SetPanelActive(resultPanel, true);
    }

    private bool CanPlayerAct()
    {
        return isPlayerTurn && !battleEnded && (encounterBanner == null || !encounterBanner.IsPlaying);
    }

    private string RecordOrbitAction(
        PlayerActionType actionType,
        int actionDamage,
        string skillCode = null
    )
    {
        convergenceNotice = string.Empty;
        int recordedSlotIndex = orbitState.Count;
        orbitState.AddAction(actionType);

        if (EnsureOrbitUIAnimator(true))
        {
            orbitUIAnimator.SetSlotAction(
                recordedSlotIndex,
                actionType,
                skillCode
            );
        }

        bool resolveBlackSunEarly =
            blackSunFirstConvergenceAvailable && orbitState.Count == 2;

        if (!orbitState.IsFull && !resolveBlackSunEarly)
        {
            UpdateOrbitUI();
            return string.Empty;
        }

        ConvergenceResult result = resolveBlackSunEarly
            ? orbitResolver.ResolveWithWildcardPrefix(orbitState.Actions)
            : orbitResolver.Resolve(orbitState.Actions);

        if (resolveBlackSunEarly)
        {
            blackSunFirstConvergenceAvailable = false;
        }

        string effectLog = ApplyConvergence(result, actionDamage);
        GetRunManager().RecordConvergence(result.Type);
        ApplySkillConvergencePassives(result.Type);

        Debug.Log(
            $"Convergence aktif: " +
            $"{result.DisplayName} | " +
            $"{result.Description}"
        );

        convergenceNotice = $"CONVERGENCE: {result.DisplayName}";

        if (EnsureOrbitUIAnimator(true))
        {
            orbitUIAnimator.PlayConvergencePulse();
        }

        orbitState.Clear();

        if (EnsureOrbitUIAnimator(true))
        {
            orbitUIAnimator.ClearSlotIcons();
        }

        UpdateOrbitUI();

        return $"\n{result.DisplayName}: {effectLog}";
    }

    private void ApplySkillConvergencePassives(ConvergenceType type)
    {
        RunManager run = GetRunManager();
        if (type == ConvergenceType.EventHorizon &&
            battleStatuses.GetStacks(BattleStatusEffectType.Curse, BattleStatusTarget.Enemy) > 0 &&
            HasEquippedSkill("SKL_PALE_LEDGER") && TryUsePassive("SKL_PALE_LEDGER"))
            run.RestoreMana(1, GetCurrentPlayerMaxMana());
        if (type == ConvergenceType.ReversalOrbit &&
            HasEquippedSkill("SKL_BLACKGLASS_PARRY") && TryUsePassive("SKL_BLACKGLASS_PARRY"))
            battleStatuses.Apply(BattleStatusEffectType.Weakness, BattleStatusTarget.Enemy, 1, 1);
        if (type == ConvergenceType.UnstablePulse &&
            HasEquippedSkill("SKL_HOLLOW_CONDUIT") && TryUsePassive("SKL_HOLLOW_CONDUIT"))
            run.RestoreMana(1, GetCurrentPlayerMaxMana());
        if (orbitalSeverConvergenceReady && HasEquippedSkill("SKL_ORBITAL_SEVER") &&
            TryUsePassive("SKL_ORBITAL_SEVER"))
        {
            run.RestoreHP(2, GetCurrentPlayerMaxHP());
            orbitalSeverConvergenceReady = false;
        }
        if (HasEquippedSkill("SKL_MOURNING_BELL") &&
            TryUsePassive("SKL_MOURNING_BELL"))
        {
            run.RestoreMana(2, GetCurrentPlayerMaxMana());
        }
        SyncLegacyRunStateFields();
    }

    private string ApplyConvergence(
        ConvergenceResult result,
        int actionDamage
    )
    {
        switch (result.Type)
        {
            case ConvergenceType.EventHorizon:
            {
                int bonusDamage = Mathf.RoundToInt(
                    actionDamage * eventHorizonBonus
                );

                enemyHP = Mathf.Max(0, enemyHP - bonusDamage);

                return
                    $"Skill mendapatkan tambahan " +
                    $"{bonusDamage} damage.";
            }

            case ConvergenceType.ReversalOrbit:
            {
                reversalCounterReady = true;

                return
                    "Counter disiapkan untuk " +
                    "serangan enemy berikutnya.";
            }

            case ConvergenceType.UnstablePulse:
            {
                int restoredMana = GetRunManager().RestoreMana(
                    1,
                    GetCurrentPlayerMaxMana()
                );
                SyncLegacyRunStateFields();

                return restoredMana > 0
                    ? "Memulihkan 1 Mana."
                    : "Mana sudah penuh sehingga tidak ada Mana yang dipulihkan.";
            }

            default:
                return string.Empty;
        }
    }

    private void FinishPlayerAction()
    {
        HideConvergencePreview();
        isPlayerTurn = false;

        UpdateUI();
        UpdateButtons();

        if (enemyHP <= 0)
        {
            EndBattle(true);
            return;
        }

        BattleStatusTickResult playerStatusTick =
            battleStatuses.ResolveEndTurn(BattleStatusTarget.Player);
        if (playerStatusTick.BurnDamage > 0)
        {
            RunManager activeRun = GetRunManager();
            activeRun.ApplyDamage(playerStatusTick.BurnDamage);
            SyncLegacyRunStateFields();
            SetBattleLog(
                $"Player menerima {playerStatusTick.BurnDamage} Burn damage."
            );
            UpdateUI();

            if (activeRun.CurrentHP <= 0)
            {
                EndBattle(false);
                return;
            }
        }

        if (enemyActedThisRound)
        {
            enemyActedThisRound = false;
            if (IsSpecialEncounter())
                encounterTurn++;
            if (IsBossAwakening())
            {
                bossAwakened = true;
                StartCoroutine(PlayEncounterIntroduction(true, true));
            }
            StartCoroutine(EnemyOpeningTurn());
        }
        else
        {
            StartCoroutine(EnemyTurn());
        }
    }

    private IEnumerator EnemyOpeningTurn()
    {
        // The opening enemy action waits for the Elite/Boss cut-in. This also
        // handles phase-II cut-ins between two consecutive enemy-first rounds.
        while (encounterBanner != null && encounterBanner.IsPlaying)
            yield return null;
        if (!battleEnded) yield return EnemyTurn();
    }

    private bool IsSpecialEncounter()
    {
        StandardRunNode current = GetRunManager().CurrentNode;
        return current != null && (current.Type == StandardRunNodeType.EliteBattle ||
            current.Type == StandardRunNodeType.Boss);
    }

    private bool IsBossAwakening()
    {
        StandardRunNode current = GetRunManager().CurrentNode;
        return current != null && current.Type == StandardRunNodeType.Boss &&
            !bossAwakened && currentEnemy != null &&
            enemyHP <= currentEnemy.MaxHP / 2;
    }

    private IEnumerator EnemyTurn()
    {
        HideConvergencePreview();

        // Never prepare or reroll here: Analyze and execution share this node's plan.
        if (enemyActionPlan == null)
        {
            ShowEnemyPlanError();
            yield break;
        }

        EnemyActionPlan executingPlan = enemyActionPlan;
        bool enemyWasCursedAtTurnStart = battleStatuses.GetStacks(
            BattleStatusEffectType.Curse, BattleStatusTarget.Enemy) > 0;
        yield return new WaitForSeconds(0.8f);

        if (executingPlan.TotalEstimatedDamage > 0) battleVisuals?.PlayEnemyAttack();

        yield return new WaitForSeconds(0.12f);

        if (executingPlan.TotalEstimatedDamage > 0) battleVisuals?.PlayPlayerHit();

        int receivedDamage = battleStatuses.ResolveOutgoingDamage(
            BattleStatusTarget.Enemy,
            executingPlan.TotalEstimatedDamage
        );
        receivedDamage = battleStatuses.ResolveIncomingDamage(
            BattleStatusTarget.Player,
            receivedDamage
        );

        if (executingPlan.TotalEstimatedDamage > 0 && receivedDamage > 0)
        {
            if (analyzeProtectionReady)
            {
                analyzeProtectionReady = false;
                if (TryUsePassive("SKL_ASTERION_GAZE"))
                    receivedDamage = Mathf.Max(0, receivedDamage - 3);
            }
            if (battleStatuses.GetStacks(BattleStatusEffectType.Weakness, BattleStatusTarget.Enemy) > 0)
            {
                if (HasEquippedSkill("SKL_GRAVEMUTE_SEAL") && TryUsePassive("SKL_GRAVEMUTE_SEAL"))
                    receivedDamage = Mathf.Max(0, receivedDamage - 3);
            }

            int wardPrevented = Mathf.Min(skillWardRemaining, receivedDamage);
            if (wardPrevented > 0)
            {
                skillWardRemaining -= wardPrevented;
                receivedDamage -= wardPrevented;
                if (HasEquippedSkill("SKL_MIRROR_WARDEN") && TryUsePassive("SKL_MIRROR_WARDEN"))
                    nextBasicAttackBonus += 4;
                if (HasEquippedSkill("SKL_GLOAMING_BULWARK"))
                    SetPassiveReady("SKL_GLOAMING_BULWARK");
            }

            if (skillParryReady)
            {
                int prevented = Mathf.CeilToInt(receivedDamage * 0.5f);
                receivedDamage = Mathf.Max(0, receivedDamage - prevented);
                int reflected = prevented / 2;
                enemyHP = Mathf.Max(0, enemyHP - reflected);
                skillParryReady = false;
            }
        }

        if (isGuarding && executingPlan.TotalEstimatedDamage > 0)
        {
            float remainingDamageMultiplier =
                1f - GetCurrentGuardDamageReduction();

            receivedDamage = Mathf.Max(
                0,
                Mathf.CeilToInt(
                    receivedDamage * remainingDamageMultiplier
                )
            );

            guardHitsRemaining = Mathf.Max(0, guardHitsRemaining - 1);
            isGuarding = guardHitsRemaining > 0;
        }

        RunManager activeRun = GetRunManager();
        activeRun.ApplyDamage(receivedDamage);
        SyncLegacyRunStateFields();
        if (enemyHP <= 0)
            ApplyKillPassives(enemyWasCursedAtTurnStart, false);

        string appliedStatusLog = string.Empty;
        if (executingPlan.AppliedStatus.HasValue && activeRun.CurrentHP > 0)
        {
            BattleStatusState applied = battleStatuses.Apply(
                executingPlan.AppliedStatus.Value,
                BattleStatusTarget.Player,
                1,
                executingPlan.StatusDuration
            );
            appliedStatusLog =
                $" {applied.EffectType} aktif selama " +
                $"{applied.RemainingTurns} turn.";
        }

        string enemyTurnLog =
            $"{currentEnemy.Name} menggunakan {executingPlan.DisplayName}. " +
            $"Player menerima {receivedDamage} damage." + appliedStatusLog;

        HideEnemyIntent();
        SetBattleLog(enemyTurnLog);
        UpdateUI();

        if (activeRun.CurrentHP <= 0)
        {
            EndBattle(false);
            yield break;
        }
        if (enemyHP <= 0)
        {
            EndBattle(true);
            yield break;
        }

        BattleStatusTickResult enemyStatusTick =
            battleStatuses.ResolveEndTurn(BattleStatusTarget.Enemy);
        SyncLegacyBurnFields();

        if (enemyStatusTick.BurnDamage > 0)
        {
            enemyHP = Mathf.Max(
                0,
                enemyHP - enemyStatusTick.BurnDamage
            );
            enemyTurnLog +=
                $" Burn deals {enemyStatusTick.BurnDamage} damage.";
            if (HasEquippedSkill("SKL_ASHWAKE_CANTICLE") && TryUsePassive("SKL_ASHWAKE_CANTICLE"))
                activeRun.RestoreMana(1, GetCurrentPlayerMaxMana());
            if (HasEquippedSkill("SKL_WRAITH_CANDLE") && TryUsePassive("SKL_WRAITH_CANDLE"))
                activeRun.RestoreHP(2, GetCurrentPlayerMaxHP());
            SyncLegacyRunStateFields();
            SetBattleLog(enemyTurnLog);
            UpdateUI();
            if (enemyHP <= 0)
            {
                ApplyKillPassives(enemyWasCursedAtTurnStart, false);
                EndBattle(true);
                yield break;
            }
        }

        if (reversalCounterReady)
        {
            reversalCounterReady = false;

            yield return new WaitForSeconds(0.25f);

            battleVisuals?.PlayPlayerAttack();
            battleVisuals?.PlayEnemyHit();

            int counterDamage = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    GetCurrentBasicAttackDamage() *
                    reversalCounterMultiplier
                )
            );

            enemyHP = Mathf.Max(0, enemyHP - counterDamage);

            SetBattleLog(
                enemyTurnLog,
                $"\nREVERSAL ORBIT: Player membalas " +
                $"dengan {counterDamage} damage."
            );

            UpdateUI();

            if (enemyHP <= 0)
            {
                ApplyKillPassives(enemyWasCursedAtTurnStart, false);
                EndBattle(true);
                yield break;
            }
        }

        yield return new WaitForSeconds(0.8f);

        activeRun.RestoreMana(
            1,
            GetCurrentPlayerMaxMana()
        );
        SyncLegacyRunStateFields();
        if (enemyActsFirstThisRound)
        {
            enemyActedThisRound = true;
            enemyActionPlan = null;
            if (!PrepareEnemyActionPlan(encounterTurn + 1)) yield break;
        }
        else if (IsSpecialEncounter())
        {
            encounterTurn++;
            if (IsBossAwakening())
            {
                bossAwakened = true;
                yield return PlayEncounterIntroduction(true, true);
            }
            enemyActionPlan = null;
            if (!PrepareEnemyActionPlan()) yield break;
        }
        isPlayerTurn = true;

        SetBattleLog(executingPlan.ActionCode == "BOSS_CHARGE"
            ? "Boss mengumpulkan tenaga. Serangan berat menyusul—bersiaplah!\nGiliran Player. Mana bertambah 1."
            : "Giliran Player. Mana bertambah 1.");

        UpdateUI();
        UpdateButtons();
    }

    private void EndBattle(bool playerWon)
    {
        if (battleEnded)
        {
            return;
        }

        guidedTutorialController?.OnBattleEnded(playerWon);
        battleEnded = true;
        encounterBanner?.Cancel();
        isPlayerTurn = false;
        rewardSelectionOpen = false;
        rewardSelectionCommitted = false;
        HideEnemyIntent();
        HideConvergencePreview();

        UpdateUI();
        UpdateButtons();

        RunManager activeRun = GetRunManager();

        if (!playerWon)
        {
            activeRun.MarkDefeated();
            SyncLegacyRunStateFields();
            FinalizeEchoFragments();

            SetText(resultTitleText, "DEFEAT");
            SetText(
                resultDescriptionText,
                $"Dikalahkan oleh {currentEnemy.Name}.\n" +
                $"Total Embers: {activeRun.TotalEmbers}\n" +
                "Temporary Run Stats akan direset."
            );

            SetButtonText(retryButton, "Restart Run");
            SetObjectActive(retryButton, true);
            SetObjectActive(nextBattleButton, false);
            SetPanelActive(rewardPanel, false);
            SetPanelActive(resultPanel, true);
            ShowRunSummary(
                RunSummaryOutcome.Defeated,
                $"{currentEnemy.Name} severed this thread at node {activeRun.Depth}.",
                currentEnemy.Name
            );
            return;
        }

        StandardRunNode resolvedNode = activeRun.CurrentNode;

        if (resolvedNode == null || !resolvedNode.IsBattle)
        {
            ShowRewardError(
                "Node battle aktif tidak tersedia saat kemenangan diproses."
            );
            return;
        }

        activeRun.AddEmbers(currentEnemy.RewardEmbers);
        activeRun.RecordBattleWon();
        SyncLegacyRunStateFields();

        if (!resolvedNode.IsFinal)
        {
            if (!PrepareRewardChoices())
            {
                ShowRewardError(
                    "Pilihan reward tidak dapat dibuat untuk node ini."
                );
                return;
            }

            SetText(resultTitleText, "VICTORY");
            SetText(
                resultDescriptionText,
                $"{currentEnemy.Name} dikalahkan.\n" +
                $"Mendapatkan {currentEnemy.RewardEmbers} Embers.\n" +
                $"Total Embers: {activeRun.TotalEmbers}\n" +
                (rewardSelectionKind == RewardSelectionKind.Anomaly
                    ? "Menyiapkan pilihan Anomaly..."
                    : "Menyiapkan pilihan Run Reward...")
            );

            SetObjectActive(retryButton, false);
            SetObjectActive(nextBattleButton, false);

            rewardTransitionCoroutine = StartCoroutine(
                OpenRewardSelectionAfterDelay()
            );
        }
        else
        {
            if (!activeRun.TryResolveCurrentNode(
                resolvedNode.Type,
                resolvedNode.Code
            ))
            {
                ShowRewardError(
                    "Boss final tidak dapat diselesaikan oleh RunManager."
                );
                return;
            }

            SyncLegacyRunStateFields();
            FinalizeEchoFragments();

            SetText(resultTitleText, "RUN COMPLETE");
            SetText(
                resultDescriptionText,
                $"{currentEnemy.Name} telah dikalahkan.\n" +
                $"Total Embers: {activeRun.TotalEmbers}\n" +
                "Seluruh Standard Run berhasil diselesaikan."
            );

            SetButtonText(retryButton, "Restart Run");
            SetObjectActive(retryButton, true);
            SetObjectActive(nextBattleButton, false);
        }

        SetPanelActive(resultPanel, true);
        if (activeRun.IsCompleted)
        {
            ShowRunSummary(
                RunSummaryOutcome.Cleared,
                $"{currentEnemy.Name} has fallen. One path survived the fracture.",
                currentEnemy.Name
            );
        }
    }

    private void OpenRewardSelection()
    {
        if (!battleEnded ||
            GetRunManager().IsCompleted ||
            rewardSelectionOpen ||
            rewardSelectionCommitted)
        {
            return;
        }

        if (rewardPanel == null)
        {
            Debug.LogError(
                "RewardPanel belum dihubungkan ke BattleManager."
            );

            return;
        }

        if (rewardTransitionCoroutine != null)
        {
            StopCoroutine(rewardTransitionCoroutine);
            rewardTransitionCoroutine = null;
        }

        EnsureRewardSelectionUI();

        if (!PrepareRewardChoices())
        {
            ShowRewardError(
                "Pilihan reward tidak dapat dibuat untuk node ini."
            );
            return;
        }

        rewardSelectionOpen = true;
        rewardSelectionCommitted = false;

        ConfigureRewardButtonText();
        SetText(
            rewardTitleText,
            rewardSelectionKind == RewardSelectionKind.Anomaly
                ? "CHOOSE ONE ANOMALY"
                : "CHOOSE A RUN REWARD"
        );

        HideConvergencePreview();
        SetBattleChromeVisible(false);
        SetPanelActive(resultPanel, false);

        if (rewardSelectionUI != null && rewardSelectionUI.IsInitialized)
        {
            rewardSelectionUI.ShowAnimated();
        }
        else
        {
            SetPanelActive(rewardPanel, true);
        }
    }

    private IEnumerator OpenRewardSelectionAfterDelay()
    {
        yield return new WaitForSecondsRealtime(RewardRevealDelay);
        rewardTransitionCoroutine = null;
        OpenRewardSelection();
    }

    private void ChooseAttackReward()
    {
        ChooseReward(0);
    }

    private void ChooseMaxHPReward()
    {
        ChooseReward(1);
    }

    private void ChooseGuardReward()
    {
        ChooseReward(2);
    }

    private void ChooseReward(int choiceIndex)
    {
        if (!rewardSelectionOpen || rewardSelectionCommitted)
        {
            return;
        }

        StandardRunNode rewardNode = GetRunManager().CurrentNode;

        if (rewardNode == null ||
            (!rewardNode.IsBattle && rewardNode.Type != StandardRunNodeType.Relic) ||
            rewardNode.IsFinal)
        {
            ShowRewardError(
                "Reward hanya dapat dipilih setelah battle non-final aktif."
            );
            return;
        }

        if (currentRewardChoices == null ||
            choiceIndex < 0 ||
            choiceIndex >= currentRewardChoices.Length)
        {
            Debug.LogError(
                "Snapshot reward tidak tersedia. Pilihan tidak diterapkan."
            );
            return;
        }

        if (!TryCommitRewardSelection())
        {
            return;
        }

        RewardDefinition reward = currentRewardChoices[choiceIndex];
        ApplyReward(reward);

        CompleteRewardSelection(
            $"{reward.DisplayName} Tier {reward.Tier} dipilih. " +
            $"{reward.ExactEffectText}."
        );
    }

    private void ApplyReward(RewardDefinition reward)
    {
        if (reward == null)
        {
            Debug.LogError("Reward null tidak dapat diterapkan.");
            return;
        }

        switch (reward.EffectType)
        {
            case RewardEffectType.AttackPercent:
                break;

            case RewardEffectType.MaxHPFlat:
                break;

            case RewardEffectType.GuardStrengthPercent:
            case RewardEffectType.RelicGuardMana:
            case RewardEffectType.RelicAttackBurn:
            case RewardEffectType.RelicEntryHeal:
            case RewardEffectType.RelicFirstSkill:
            case RewardEffectType.RelicFirstAttack:
            case RewardEffectType.AnomalyCollapsedStar:
            case RewardEffectType.AnomalyBlackSun:
            case RewardEffectType.AnomalyFrozenOrbit:
            case RewardEffectType.AnomalyInvertedPulse:
                break;

            default:
                Debug.LogError(
                    $"Reward effect tidak dikenali: {reward.EffectType}."
                );
                return;
        }

        GetRunManager().Modifiers.AddOrStack(reward);
        SyncLegacyRunModifierFields();

        if (reward.EffectType == RewardEffectType.MaxHPFlat)
        {
            GetRunManager().RestoreHP(
                reward.FlatValue,
                GetCurrentPlayerMaxHP()
            );
            SyncLegacyRunStateFields();
        }

        if (reward.EffectType == RewardEffectType.AnomalyBlackSun)
        {
            GetRunManager().ClampResources(
                GetCurrentPlayerMaxHP(),
                GetCurrentPlayerMaxMana()
            );
            SyncLegacyRunStateFields();
        }
    }

    private void CompleteRewardSelection(string rewardLog)
    {
        rewardSelectionOpen = false;
        rewardSelectionUI?.HideImmediate();
        SetPanelActive(rewardPanel, false);

        Debug.Log(rewardLog);
        Debug.Log(GetRunStatsSummary());

        RunManager activeRun = GetRunManager();
        StandardRunNode resolvedNode = activeRun.CurrentNode;

        if (resolvedNode == null ||
            (!resolvedNode.IsBattle && resolvedNode.Type != StandardRunNodeType.Relic) ||
            resolvedNode.IsFinal ||
            !activeRun.TryResolveCurrentNode(
                resolvedNode.Type,
                resolvedNode.Code
            ))
        {
            ShowRewardError(
                "RunManager menolak perpindahan node. Restart Run diperlukan."
            );
            return;
        }

        SyncLegacyRunStateFields();
        SaveActiveRunCheckpoint("reward committed");
        OpenRunMap();
    }

    private bool TryCommitRewardSelection()
    {
        if (!rewardSelectionOpen || rewardSelectionCommitted)
        {
            return false;
        }

        rewardSelectionCommitted = true;
        rewardSelectionOpen = false;
        rewardSelectionUI?.LockSelection();
        return true;
    }

    private void ConfigureRewardButtonText()
    {
        if (currentRewardChoices == null)
        {
            return;
        }

        Button[] buttons =
        {
            rewardButton1,
            rewardButton2,
            rewardButton3
        };

        int cardCount = Mathf.Min(
            currentRewardChoices.Length,
            buttons.Length
        );

        rewardSelectionUI?.SetVisibleCardCount(cardCount);

        for (int index = 0; index < buttons.Length; index++)
        {
            SetObjectActive(buttons[index], index < cardCount);
        }

        for (int index = 0; index < cardCount; index++)
        {
            RewardDefinition reward = currentRewardChoices[index];
            int currentStack = GetRewardStack(reward.EffectType);

            if (rewardSelectionUI != null && rewardSelectionUI.IsInitialized)
            {
                rewardSelectionUI.SetCardDefinition(
                    index,
                    reward,
                    currentStack
                );
            }
            else
            {
                SetButtonText(
                    buttons[index],
                    $"TIER {reward.Tier}\n" +
                    $"{reward.DisplayName}\n" +
                    $"{reward.ExactEffectText}\n" +
                    $"STACK {currentStack} → {currentStack + 1}"
                );
            }
        }
    }

    private bool PrepareRewardChoices()
    {
        RunManager activeRun = GetRunManager();

        if (currentRewardChoices != null &&
            preparedRewardDepth == activeRun.Depth)
        {
            return true;
        }

        if (rewardManager == null)
        {
            InitializeRewardSystem(false);
        }

        try
        {
            StandardRunNode rewardNode = activeRun.CurrentNode;
            bool isEliteReward = rewardNode != null &&
                rewardNode.Type == StandardRunNodeType.EliteBattle;

            if (isEliteReward)
            {
                RewardDefinition[] anomalyChoices =
                    RunAnomalyCatalog.CreateChoices(
                        GetRewardSeedForDepth(activeRun.Depth),
                        activeRun.Modifiers
                    );

                if (anomalyChoices.Length > 0)
                {
                    currentRewardChoices = anomalyChoices;
                    rewardSelectionKind = RewardSelectionKind.Anomaly;
                }
            }

            if (currentRewardChoices == null)
            {
                currentRewardChoices = rewardManager.GenerateChoices(
                    activeRun.Depth,
                    GetRewardSeedForDepth(activeRun.Depth)
                );
                rewardSelectionKind = RewardSelectionKind.Standard;
            }

            preparedRewardDepth = activeRun.Depth;

            if (currentRewardChoices == null ||
                currentRewardChoices.Length < 1 ||
                currentRewardChoices.Length > 3 ||
                HasDuplicateRewardCodes(currentRewardChoices))
            {
                currentRewardChoices = null;
                preparedRewardDepth = -1;
                Debug.LogError(
                    "Reward selection wajib menghasilkan 1-3 pilihan berbeda."
                );
                return false;
            }

            return true;
        }
        catch (System.Exception exception)
        {
            currentRewardChoices = null;
            preparedRewardDepth = -1;
            Debug.LogError(
                $"RewardManager gagal membuat pilihan: {exception.Message}"
            );
            return false;
        }
    }

    private void RestartRun()
    {
        // A failed permanent-currency write leaves the last safe checkpoint
        // available and makes the CTA retry the write before discarding it.
        if (echoSaveFailed && !FinalizeEchoFragments()) return;
        if (terminalSummaryPending && !historyCommitted && !TryRecordTerminalHistory())
        {
            runSummaryUI?.Show(BuildTerminalSummary());
            return;
        }
        StopAllCoroutines();
        encounterBanner?.Cancel();
        runSummaryUI?.HideImmediate();
        if (rewardTransitionCoroutine != null)
        {
            StopCoroutine(rewardTransitionCoroutine);
            rewardTransitionCoroutine = null;
        }

        rewardSelectionOpen = false;
        rewardSelectionCommitted = false;
        echoAward = null;
        echoReceipt = null;
        echoSaveFailed = false;
        historySaveFailed = false;
        historyCommitted = false;
        terminalSummaryPending = false;
        terminalEndingReason = null;
        terminalEnemyName = null;

        InitializeRunStatController();
        InitializeRewardSystem(true);

        RunStatController stats = GetRunStatController();
        GetRunManager().StartNewRun(
            rewardRunSeed,
            stats.BaseMaxHP,
            playerStartingMana,
            stats.BaseMaxHP,
            stats.BaseMaxMana
        );
        try
        {
            if (skillProgressStore == null) skillProgressStore = SkillProgressStore.CreateDefault();
            GetRunManager().SetEquippedSkillCodes(skillProgressStore.GetLoadout());
        }
        catch (System.Exception error)
        {
            Debug.LogError("Loadout pemain tidak dapat dibaca; Shadow Strike digunakan. " + error.Message);
            GetRunManager().SetEquippedSkillCodes(new[] { "SKL_SHADOW_STRIKE", null, null });
        }
        RefreshSkillButtonPresentation();
        SyncLegacyRunModifierFields();
        SyncLegacyRunStateFields();
        ClearActiveRunCheckpoint();
        SaveActiveRunCheckpoint("new run");
        analyzeUsedThisNode = false;
        isGuarding = false;
        enemyActionPlan = null;
        ResetOrbitForBattle();
        OpenRunMap();
    }

    private void ResetTemporaryRunStats()
    {
        GetRunManager().ClearModifiers();
        SyncLegacyRunModifierFields();

        Debug.Log("Temporary Run Stats telah direset.");
    }

    private void SyncLegacyRunModifierFields()
    {
        // Keep the existing private field contract stable while the collection
        // becomes the single owner of active-run modifier records.
        RunStatController stats = GetRunStatController();
        RunModifierCollection modifiers = GetRunManager().Modifiers;
        runMaxHPBonus = stats.MaxHPBonus;
        runAttackBonusPercent = stats.BasicAttackBonusPercent;
        runGuardStrengthBonusPercent = stats.GuardStrengthBonusPercent;
        runAttackRewardStacks = modifiers.GetStackCount(
            RewardEffectType.AttackPercent
        );
        runMaxHPRewardStacks = modifiers.GetStackCount(
            RewardEffectType.MaxHPFlat
        );
        runGuardRewardStacks = modifiers.GetStackCount(
            RewardEffectType.GuardStrengthPercent
        );
    }

    private void InitializeRewardSystem(bool generateNewSeed)
    {
        rewardManager = new RewardManager(
            maxHPRewardAmount,
            attackRewardPercent,
            guardRewardPercent
        );

        if (generateNewSeed || rewardRunSeed == 0)
        {
            rewardRunSeed = System.Guid.NewGuid().GetHashCode();

            if (rewardRunSeed == 0)
            {
                rewardRunSeed = 1;
            }
        }

        currentRewardChoices = null;
        preparedRewardDepth = -1;
    }

    private int GetRewardSeedForDepth(int depth)
    {
        unchecked
        {
            int seed = GetRunManager().RunSeed;

            if (seed == 0)
            {
                seed = rewardRunSeed;
            }

            seed = (seed * 397) ^ depth;
            seed = (seed * 397) ^ 0x53A71;
            return seed;
        }
    }

    private int GetRewardStack(RewardEffectType effectType)
    {
        return GetRunManager().Modifiers.GetStackCount(effectType);
    }

    private static bool HasDuplicateRewardCodes(
        RewardDefinition[] rewards
    )
    {
        for (int left = 0; left < rewards.Length; left++)
        {
            if (rewards[left] == null)
            {
                return true;
            }

            for (int right = left + 1; right < rewards.Length; right++)
            {
                if (rewards[right] == null ||
                    string.Equals(
                        rewards[left].Code,
                        rewards[right].Code,
                        System.StringComparison.Ordinal
                    ))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ShowRewardError(string message)
    {
        Debug.LogError(message);
        rewardSelectionOpen = false;
        rewardSelectionCommitted = false;
        rewardSelectionUI?.HideImmediate();
        SetPanelActive(rewardPanel, false);
        SetText(resultTitleText, "REWARD ERROR");
        SetText(resultDescriptionText, message);
        SetButtonText(retryButton, "Restart Run");
        SetObjectActive(retryButton, true);
        SetObjectActive(nextBattleButton, false);
        SetPanelActive(resultPanel, true);
    }

    private int GetCurrentPlayerMaxHP()
    {
        return GetRunStatController().EffectiveMaxHP;
    }

    private int GetCurrentPlayerMaxMana()
    {
        return GetRunStatController().EffectiveMaxMana;
    }

    private int GetCurrentSkillManaCost()
    {
        return GetCurrentSkillManaCost(0);
    }

    private int GetCurrentSkillManaCost(int slot)
    {
        if (slot < 0 || slot >= equippedSkills.Length || equippedSkills[slot] == null)
        {
            return 0;
        }

        int anomalyCost = HasAnomaly(
            RewardEffectType.AnomalyCollapsedStar
        ) ? 1 : 0;
        int cost = checked(equippedSkills[slot].ManaCost + anomalyCost);
        if (equippedSkills[slot].Code == "SKL_VESPER_STITCH" &&
            GetRunManager().CurrentHP <= GetCurrentPlayerMaxHP() * 0.40f &&
            !usedSkillPassives.Contains(equippedSkills[slot].Code))
            cost = Mathf.Max(0, cost - 1);
        if (equippedSkills[slot].Code == "SKL_ECHO_SCRIPT" &&
            previousPlayerAction == PlayerActionType.Guard &&
            !usedSkillPassives.Contains(equippedSkills[slot].Code))
            cost = Mathf.Max(0, cost - 1);
        return cost;
    }

    private int ResolvePlayerDamage(int baseDamage)
    {
        int outgoing = battleStatuses.ResolveOutgoingDamage(
            BattleStatusTarget.Player,
            baseDamage
        );
        return battleStatuses.ResolveIncomingDamage(
            BattleStatusTarget.Enemy,
            outgoing
        );
    }

    private bool RollPlayerCritical(bool allowed)
    {
        RunManager activeRun = GetRunManager();
        int strike = playerStrikeIndex++;
        return allowed && BattleTurnRules.IsCritical(
            activeRun.RunSeed,
            activeRun.Depth,
            strike,
            GetRunStatController().EffectiveCriticalChance
        );
    }

    private void SyncLegacyBurnFields()
    {
        relicBurnStacks = battleStatuses.GetStacks(
            BattleStatusEffectType.Burn,
            BattleStatusTarget.Enemy
        );
        relicBurnTurns = battleStatuses.GetRemainingTurns(
            BattleStatusEffectType.Burn,
            BattleStatusTarget.Enemy
        );
    }

    private string FormatStatusSummary(BattleStatusTarget target)
    {
        BattleStatusState[] states = battleStatuses.CreateSnapshot(target);
        if (states.Length == 0)
        {
            return string.Empty;
        }

        var labels = new System.Text.StringBuilder();
        for (int index = 0; index < states.Length; index++)
        {
            if (index > 0)
            {
                labels.Append("  •  ");
            }

            BattleStatusState state = states[index];
            labels.Append(state.EffectType.ToString().ToUpperInvariant());
            if (state.StackCount > 1)
            {
                labels.Append(" x");
                labels.Append(state.StackCount);
            }
            labels.Append(" · ");
            labels.Append(state.RemainingTurns);
            labels.Append(state.RemainingTurns == 1 ? " TURN" : " TURNS");
        }

        return labels.ToString();
    }

    private int GetCurrentBasicAttackDamage()
    {
        return GetRunStatController().EffectiveBasicAttackDamage;
    }

    private float GetCurrentGuardDamageReduction()
    {
        return GetRunStatController().EffectiveGuardDamageReduction;
    }

    private string GetRunStatsSummary()
    {
        RunStatController stats = GetRunStatController();
        int maxHPBonus = stats.MaxHPBonus;
        int attackBonusPercent = Mathf.RoundToInt(
            stats.BasicAttackBonusPercent * 100f
        );

        int guardBonusPercent = Mathf.RoundToInt(
            stats.GuardStrengthBonusPercent * 100f
        );

        return
            $"Run Stats: Max HP +{maxHPBonus} | " +
            $"Basic Attack +{attackBonusPercent}% | " +
            $"Guard Strength +{guardBonusPercent}%";
    }

    private void InitializeRunStatController()
    {
        runStatController = new RunStatController(
            playerMaxHP,
            playerMaxMana,
            basicAttackDamage,
            baseGuardDamageReduction,
            runModifiers
        );
    }

    private RunStatController GetRunStatController()
    {
        if (runStatController == null)
        {
            InitializeRunStatController();
        }

        return runStatController;
    }

    private void InitializeRunManager()
    {
        if (runManager == null)
        {
            runManager = new RunManager(runModifiers);
        }
    }

    private void InitializeCheckpointStore()
    {
        if (checkpointStore == null)
        {
            checkpointStore = ActiveRunCheckpointStore.CreateDefault();
        }
    }

    private bool EnsureActiveRunRecoveryUI()
    {
        if (activeRunRecoveryUI != null)
        {
            activeRunRecoveryUI.Initialize(
                ContinueSavedRun,
                RecoverSavedRun,
                StartNewRunFromStartup,
                PresentStartupRunChoice
            );
            return true;
        }

        if (battleCanvas == null && playerHPText != null)
        {
            battleCanvas = playerHPText.GetComponentInParent<Canvas>(true);
        }

        if (battleCanvas == null)
        {
            battleCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                FindObjectsInactive.Include
            );
        }

        if (battleCanvas == null)
        {
            Debug.LogError(
                "Canvas Battle tidak tersedia untuk Continue/Recovery UI."
            );
            return false;
        }

        activeRunRecoveryUI = battleCanvas.GetComponent<ActiveRunRecoveryUI>();
        if (activeRunRecoveryUI == null)
        {
            activeRunRecoveryUI =
                battleCanvas.gameObject.AddComponent<ActiveRunRecoveryUI>();
        }
        activeRunRecoveryUI.Initialize(
            ContinueSavedRun,
            RecoverSavedRun,
            StartNewRunFromStartup,
            PresentStartupRunChoice
        );
        return true;
    }

    private void PresentStartupRunChoice()
    {
        InitializeCheckpointStore();
        InitializeRunStatController();
        RunStatController stats = GetRunStatController();
        pendingCheckpointInspection = checkpointStore.Inspect(
            stats.BaseMaxHP,
            stats.BaseMaxMana
        );

        if (MainMenuFlow.PendingIntent == MainMenuIntent.ContinueRun)
        {
            MainMenuFlow.Clear();
            // A usable backup still needs an explicit recovery choice so it
            // can be promoted to the primary checkpoint before play resumes.
            if (pendingCheckpointInspection != null && pendingCheckpointInspection.CanContinue)
            {
                activeRunRecoveryUI?.HideImmediate();
                try
                {
                    SeededRunState state = pendingCheckpointInspection.CreateStateCopy();
                    RestoreRunAtMapBoundary(state);
                    return;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Gagal melanjutkan run dari MainMenu: " + ex.Message);
                }
            }
        }
        else if (MainMenuFlow.PendingIntent == MainMenuIntent.NewRun)
        {
            MainMenuFlow.Clear();
            pendingCheckpointInspection = null;
            activeRunRecoveryUI?.HideImmediate();
            ClearActiveRunCheckpoint();
            RestartRun();
            return;
        }

        if (pendingCheckpointInspection.Status ==
            ActiveRunCheckpointStatus.None)
        {
            activeRunRecoveryUI?.HideImmediate();
            RestartRun();
            return;
        }

        if (!EnsureActiveRunRecoveryUI())
        {
            ShowDatabaseError(
                "Continue/Recovery UI tidak dapat dibuat pada Canvas Battle."
            );
            return;
        }

        standardRunMapUI?.HideImmediate();
        rewardSelectionUI?.HideImmediate();
        SetPanelActive(rewardPanel, false);
        SetPanelActive(resultPanel, false);
        isPlayerTurn = false;
        battleEnded = true;
        DisableAllBattleButtons();
        activeRunRecoveryUI.Show(pendingCheckpointInspection);
    }

    private void ContinueSavedRun()
    {
        if (activeRunRecoveryUI == null || !activeRunRecoveryUI.IsOpen) return;
        RunStatController stats = GetRunStatController();
        pendingCheckpointInspection = checkpointStore.Inspect(stats.BaseMaxHP, stats.BaseMaxMana);
        if (pendingCheckpointInspection == null ||
            !pendingCheckpointInspection.CanContinue)
        {
            PresentStartupRunChoice();
            return;
        }

        try
        {
            SeededRunState state =
                pendingCheckpointInspection.CreateStateCopy();
            RestoreRunAtMapBoundary(state);
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "Active Run gagal dilanjutkan: " + exception.Message
            );
            PresentStartupRunChoice();
        }
    }

    private void RecoverSavedRun()
    {
        if (activeRunRecoveryUI == null || !activeRunRecoveryUI.IsOpen) return;
        if (pendingCheckpointInspection == null ||
            !pendingCheckpointInspection.CanRecover)
        {
            PresentStartupRunChoice();
            return;
        }

        try
        {
            RunStatController stats = GetRunStatController();
            checkpointStore.RecoverBackup(
                GetRunManager(),
                stats.BaseMaxHP,
                stats.BaseMaxMana
            );
            FinishRestoredRunSetup();
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "Backup Active Run gagal dipulihkan: " + exception.Message
            );
            PresentStartupRunChoice();
        }
    }

    private void StartNewRunFromStartup()
    {
        if (activeRunRecoveryUI == null || !activeRunRecoveryUI.IsOpen) return;
        pendingCheckpointInspection = null;
        activeRunRecoveryUI?.HideImmediate();
        ClearActiveRunCheckpoint();
        RestartRun();
    }

    private void RestoreRunAtMapBoundary(SeededRunState state)
    {
        if (state == null)
        {
            throw new System.InvalidOperationException(
                "Checkpoint terverifikasi tidak memiliki state."
            );
        }

        RunStatController baseStats = GetRunStatController();
        ActiveRunCheckpointStore.GetEffectiveCaps(
            state,
            baseStats.BaseMaxHP,
            baseStats.BaseMaxMana,
            out int effectiveMaxHP,
            out int effectiveMaxMana
        );
        GetRunManager().RestoreState(
            state,
            effectiveMaxHP,
            effectiveMaxMana
        );
        FinishRestoredRunSetup();
    }

    private void FinishRestoredRunSetup()
    {
        RunManager activeRun = GetRunManager();
        rewardRunSeed = activeRun.RunSeed;
        RefreshSkillButtonPresentation();
        InitializeRewardSystem(false);
        SyncLegacyRunModifierFields();
        SyncLegacyRunStateFields();
        pendingCheckpointInspection = null;
        rewardSelectionOpen = false;
        rewardSelectionCommitted = false;
        analyzeUsedThisNode = false;
        isGuarding = false;
        enemyActionPlan = null;
        ResetOrbitForBattle();
        activeRunRecoveryUI?.HideImmediate();
        OpenRunMap();
    }

    private void SaveActiveRunCheckpoint(string reason)
    {
        InitializeCheckpointStore();

        try
        {
            checkpointStore.SaveActiveRun(GetRunManager());
            checkpointSaveFailed = false;
        }
        catch (System.Exception exception)
        {
            checkpointSaveFailed = true;
            Debug.LogError(
                $"Active Run checkpoint gagal disimpan ({reason}): " +
                exception.Message
            );
        }
    }

    public bool CanSaveAtMapBoundary => runManager != null &&
        runManager.IsActive && !runManager.IsCompleted &&
        !runManager.NodeEntered && standardRunMapUI != null &&
        standardRunMapUI.IsOpen;
    public bool HasFinishedRunSummary => terminalSummaryPending &&
        runManager != null && runManager.IsCompleted;

    public bool TrySaveForMainMenu(out string feedback)
    {
        if (CanSaveAtMapBoundary)
        {
            SaveActiveRunCheckpoint("return to Main Menu");
            if (!checkpointSaveFailed)
            {
                try
                {
                    RunStatController stats = GetRunStatController();
                    ActiveRunCheckpointInspection inspection = checkpointStore.Inspect(
                        stats.BaseMaxHP, stats.BaseMaxMana);
                    SeededRunState saved = inspection.CanContinue
                        ? inspection.CreateStateCopy() : null;
                    if (saved == null || saved.RunId != runManager.RunId ||
                        saved.Depth != runManager.Depth)
                        checkpointSaveFailed = true;
                }
                catch (System.Exception exception)
                {
                    checkpointSaveFailed = true;
                    Debug.LogError("Checkpoint keluar ke Main Menu gagal diverifikasi: " +
                        exception.Message);
                }
            }

            standardRunMapUI.SetCheckpointSaveStatus(
                checkpointSaveFailed, RetryActiveRunCheckpoint);
            feedback = checkpointSaveFailed
                ? "SAVE FAILED. RETRY ON THE MAP BEFORE LEAVING."
                : "PROGRESS SAVED";
            return !checkpointSaveFailed;
        }

        if (runManager != null && runManager.IsActive)
        {
            feedback = "FINISH THIS NODE AND REWARD BEFORE LEAVING.";
            return false;
        }

        if (runManager != null && runManager.IsCompleted && !terminalSummaryPending)
        {
            feedback = "WAIT FOR THE RUN SUMMARY BEFORE LEAVING.";
            return false;
        }

        if (terminalSummaryPending)
        {
            if (echoSaveFailed && !FinalizeEchoFragments())
            {
                feedback = "ECHO SAVE FAILED. RETRY BEFORE LEAVING.";
                return false;
            }
            if (!historyCommitted && !TryRecordTerminalHistory())
            {
                feedback = "HISTORY SAVE FAILED. RETRY BEFORE LEAVING.";
                return false;
            }
            ClearActiveRunCheckpoint();
            if (checkpointStore != null &&
                (checkpointStore.HasCheckpoint || checkpointStore.HasBackup))
            {
                feedback = "RUN CLOSE FAILED. RETRY BEFORE LEAVING.";
                return false;
            }
        }

        feedback = "RUN COMPLETE";
        return true;
    }

    private void RetryActiveRunCheckpoint()
    {
        RunManager run = GetRunManager();
        if (standardRunMapUI == null || !standardRunMapUI.IsOpen ||
            !run.IsActive || run.IsCompleted || run.NodeEntered) return;

        SaveActiveRunCheckpoint("player retry");
        standardRunMapUI.SetCheckpointSaveStatus(
            checkpointSaveFailed, RetryActiveRunCheckpoint
        );
    }

    private void ClearActiveRunCheckpoint()
    {
        InitializeCheckpointStore();

        try
        {
            checkpointStore.DeleteCheckpoint();
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                "Active Run checkpoint gagal dihapus: " + exception.Message
            );
        }
    }

    private RunManager GetRunManager()
    {
        InitializeRunManager();
        return runManager;
    }

    private void SyncLegacyRunStateFields()
    {
        // These fields predate RunManager and remain as compatibility mirrors.
        // Runtime decisions read the manager; no Inspector or reflection contract
        // needs to be removed while later run systems migrate incrementally.
        RunManager activeRun = GetRunManager();
        playerHP = activeRun.CurrentHP;
        playerMana = activeRun.CurrentMana;
        battleNumber = activeRun.Depth;
        totalEmbers = activeRun.TotalEmbers;
        runCompleted = activeRun.IsCompleted;

        if (activeRun.RunSeed != 0)
        {
            rewardRunSeed = activeRun.RunSeed;
        }
    }

    private void CacheBattlePresentation()
    {
        if (basicAttackButton != null)
        {
            battleCommandMenu =
                basicAttackButton.GetComponentInParent<BattleCommandMenu>(true);
        }

        if (battleCommandMenu != null)
        {
            commandPanelObject = battleCommandMenu.gameObject;
        }

        EnsureOrbitUIAnimator(false);
    }

    private bool EnsureOrbitUIAnimator(bool reportMissing)
    {
        if (orbitUIAnimator == null && orbitSlot1Text != null)
        {
            orbitUIAnimator =
                orbitSlot1Text.GetComponentInParent<OrbitUIAnimator>(true);
        }

        if (orbitUIAnimator == null)
        {
            orbitUIAnimator = UnityEngine.Object.FindFirstObjectByType<OrbitUIAnimator>(
                FindObjectsInactive.Include
            );
        }

        if (orbitUIAnimator != null)
        {
            orbitPanelObject = orbitUIAnimator.gameObject;
            hasReportedMissingOrbitUIAnimator = false;
            return true;
        }

        if (reportMissing && !hasReportedMissingOrbitUIAnimator)
        {
            Debug.LogError(
                "OrbitUIAnimator tidak ditemukan. " +
                "Periksa komponen OrbitPanel dan reference slot di Battle scene."
            );
            hasReportedMissingOrbitUIAnimator = true;
        }

        return false;
    }

    private void SubscribeCommandPreview()
    {
        if (battleCommandMenu == null)
        {
            return;
        }

        battleCommandMenu.PreviewRequested += ShowConvergencePreview;
        battleCommandMenu.PreviewHidden += HideConvergencePreview;
    }

    private void PrepareConvergenceTooltip()
    {
        if (convergenceTooltipRoot != null || convergencePreviewText == null)
        {
            return;
        }

        battleCanvas = convergencePreviewText.GetComponentInParent<Canvas>();

        if (battleCanvas == null)
        {
            Debug.LogError(
                "Canvas untuk Convergence hover preview tidak ditemukan."
            );
            convergencePreviewText.text = string.Empty;
            convergencePreviewText.gameObject.SetActive(false);
            return;
        }

        convergenceTooltipRoot = new GameObject(
            "ConvergenceHoverTooltip",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline)
        );
        convergenceTooltipRoot.transform.SetParent(
            battleCanvas.transform,
            false
        );

        convergenceTooltipRect =
            convergenceTooltipRoot.GetComponent<RectTransform>();
        convergenceTooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        convergenceTooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        convergenceTooltipRect.pivot = new Vector2(0.5f, 0f);
        convergenceTooltipRect.sizeDelta = new Vector2(360f, 96f);

        Image tooltipBackground =
            convergenceTooltipRoot.GetComponent<Image>();
        tooltipBackground.color = new Color(0.035f, 0.02f, 0.07f, 0.96f);
        tooltipBackground.raycastTarget = false;

        Outline tooltipBorder =
            convergenceTooltipRoot.GetComponent<Outline>();
        tooltipBorder.effectColor = new Color(0.72f, 0.38f, 1f, 0.85f);
        tooltipBorder.effectDistance = new Vector2(2f, -2f);
        tooltipBorder.useGraphicAlpha = true;

        RectTransform previewRect = convergencePreviewText.rectTransform;
        previewRect.SetParent(convergenceTooltipRect, false);
        previewRect.anchorMin = Vector2.zero;
        previewRect.anchorMax = Vector2.one;
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.offsetMin = new Vector2(14f, 10f);
        previewRect.offsetMax = new Vector2(-14f, -10f);
        previewRect.localScale = Vector3.one;
        previewRect.localRotation = Quaternion.identity;

        convergencePreviewText.text = string.Empty;
        convergencePreviewText.fontSize = 20f;
        convergencePreviewText.enableAutoSizing = true;
        convergencePreviewText.fontSizeMin = 14f;
        convergencePreviewText.fontSizeMax = 20f;
        convergencePreviewText.alignment = TextAlignmentOptions.Center;
        convergencePreviewText.richText = true;
        convergencePreviewText.raycastTarget = false;
        convergencePreviewText.gameObject.SetActive(true);

        convergenceTooltipRoot.SetActive(false);
    }

    private void ShowConvergencePreview(int commandIndex)
    {
        if (!CanPlayerAct() ||
            rewardSelectionOpen ||
            orbitState.Count != 2)
        {
            HideConvergencePreview();
            return;
        }

        if (!TryGetPreviewCommand(
            commandIndex,
            out PlayerActionType actionType,
            out Button targetButton,
            out string actionLabel
        ))
        {
            HideConvergencePreview();
            return;
        }

        if (targetButton == null ||
            !targetButton.gameObject.activeInHierarchy ||
            !targetButton.interactable)
        {
            HideConvergencePreview();
            return;
        }

        PrepareConvergenceTooltip();

        if (convergenceTooltipRoot == null || convergencePreviewText == null)
        {
            return;
        }

        ConvergenceResult result = orbitResolver.Preview(
            orbitState.Actions,
            actionType
        );

        if (!result.HasConvergence)
        {
            HideConvergencePreview();
            return;
        }

        convergencePreviewText.text =
            $"<size=16>{actionLabel} →</size> " +
            $"<b>{result.DisplayName}</b>\n" +
            $"<size=16>{result.Description}</size>";

        PositionTooltipAbove(targetButton);
        convergenceTooltipRoot.transform.SetAsLastSibling();
        convergenceTooltipRoot.SetActive(true);
    }

    private bool TryGetPreviewCommand(
        int commandIndex,
        out PlayerActionType actionType,
        out Button targetButton,
        out string actionLabel
    )
    {
        switch (commandIndex)
        {
            case 0:
                actionType = PlayerActionType.Attack;
                targetButton = basicAttackButton;
                actionLabel = "ATTACK";
                return true;

            case 1:
                actionType = PlayerActionType.Skill;
                targetButton = skillButton;
                actionLabel = "SKILL";
                return true;

            case 2:
                actionType = PlayerActionType.Guard;
                targetButton = guardButton;
                actionLabel = "GUARD";
                return true;

            case 3:
            case 4:
                int skillSlot = commandIndex - 2;
                actionType = PlayerActionType.Skill;
                targetButton = skillSlot < equippedSkillButtons.Length
                    ? equippedSkillButtons[skillSlot] : null;
                actionLabel = skillSlot < equippedSkills.Length && equippedSkills[skillSlot] != null
                    ? equippedSkills[skillSlot].Name.ToUpperInvariant() : "SKILL";
                return targetButton != null;

            default:
                actionType = default;
                targetButton = null;
                actionLabel = string.Empty;
                return false;
        }
    }

    private void PositionTooltipAbove(Button targetButton)
    {
        if (battleCanvas == null ||
            convergenceTooltipRect == null ||
            targetButton == null)
        {
            return;
        }

        RectTransform targetRect = targetButton.GetComponent<RectTransform>();
        RectTransform canvasRect = battleCanvas.transform as RectTransform;

        if (targetRect == null || canvasRect == null)
        {
            return;
        }

        Vector3[] worldCorners = new Vector3[4];
        targetRect.GetWorldCorners(worldCorners);
        Vector3 topCentre = (worldCorners[1] + worldCorners[2]) * 0.5f;
        Camera eventCamera = battleCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : battleCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            eventCamera,
            topCentre
        );

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            eventCamera,
            out Vector2 localPoint
        ))
        {
            return;
        }

        localPoint.y += 18f;
        Rect available = canvasRect.rect;
        float halfWidth = convergenceTooltipRect.sizeDelta.x * 0.5f;
        float tooltipHeight = convergenceTooltipRect.sizeDelta.y;
        localPoint.x = Mathf.Clamp(
            localPoint.x,
            available.xMin + halfWidth,
            available.xMax - halfWidth
        );
        localPoint.y = Mathf.Clamp(
            localPoint.y,
            available.yMin,
            available.yMax - tooltipHeight
        );

        convergenceTooltipRect.anchoredPosition = localPoint;
    }

    private void HideConvergencePreview()
    {
        if (convergencePreviewText != null)
        {
            convergencePreviewText.text = string.Empty;
        }

        if (convergenceTooltipRoot != null)
        {
            convergenceTooltipRoot.SetActive(false);
        }
        else if (convergencePreviewText != null)
        {
            convergencePreviewText.gameObject.SetActive(false);
        }
    }

    private void EnsureRewardSelectionUI()
    {
        if (rewardPanel == null)
        {
            return;
        }

        rewardSelectionUI = rewardPanel.GetComponent<RewardSelectionUI>();

        if (rewardSelectionUI == null)
        {
            rewardSelectionUI = rewardPanel.AddComponent<RewardSelectionUI>();
        }

        rewardSelectionUI.Initialize(
            rewardTitleText,
            rewardButton1,
            rewardButton2,
            rewardButton3
        );
    }

    private bool EnsureStandardRunMapUI()
    {
        if (standardRunMapUI != null)
        {
            standardRunMapUI.Initialize(HandleRunMapNodeSelected);
            return true;
        }

        if (battleCanvas == null && playerHPText != null)
        {
            battleCanvas = playerHPText.GetComponentInParent<Canvas>(true);
        }

        if (battleCanvas == null)
        {
            battleCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                FindObjectsInactive.Include
            );
        }

        if (battleCanvas == null)
        {
            Debug.LogError(
                "Canvas Battle tidak tersedia untuk Standard Run Map."
            );
            return false;
        }

        standardRunMapUI = battleCanvas.GetComponent<StandardRunMapUI>();

        if (standardRunMapUI == null)
        {
            standardRunMapUI =
                battleCanvas.gameObject.AddComponent<StandardRunMapUI>();
        }

        standardRunMapUI.Initialize(HandleRunMapNodeSelected);
        return true;
    }

    private void EnsureGuidedTutorialController()
    {
        if (battleCanvas == null && playerHPText != null)
            battleCanvas = playerHPText.GetComponentInParent<Canvas>(true);
        if (battleCanvas == null) return;

        guidedTutorialController = battleCanvas.GetComponent<GuidedTutorialController>();
        if (guidedTutorialController == null)
            guidedTutorialController = battleCanvas.gameObject.AddComponent<GuidedTutorialController>();
        guidedTutorialController.Initialize(this, standardRunMapUI);
    }

    private bool EnsureRunSummaryUI()
    {
        if (battleCanvas == null && playerHPText != null)
            battleCanvas = playerHPText.GetComponentInParent<Canvas>(true);
        if (battleCanvas == null)
        {
            battleCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>(
                FindObjectsInactive.Include
            );
        }
        if (battleCanvas == null)
        {
            Debug.LogError("Canvas Battle tidak tersedia untuk Run Summary.");
            return false;
        }

        runSummaryUI = battleCanvas.GetComponent<RunSummaryUI>();
        if (runSummaryUI == null)
            runSummaryUI = battleCanvas.gameObject.AddComponent<RunSummaryUI>();
        runSummaryUI.Initialize(RestartRun);
        return true;
    }

    private void ShowRunSummary(
        RunSummaryOutcome outcome,
        string endingReason,
        string finalEnemyName
    )
    {
        terminalOutcome = outcome;
        terminalEndingReason = endingReason;
        terminalEnemyName = finalEnemyName;
        terminalSummaryPending = true;
        if (!echoSaveFailed && !historyCommitted) TryRecordTerminalHistory();
        if (!echoSaveFailed && historyCommitted) ClearActiveRunCheckpoint();
        if (!EnsureRunSummaryUI()) return;

        standardRunMapUI?.HideImmediate();
        rewardSelectionUI?.HideImmediate();
        activeRunRecoveryUI?.HideImmediate();
        SetPanelActive(rewardPanel, false);
        HideEnemyIntent();
        HideConvergencePreview();
        SetBattleChromeVisible(false);
        runSummaryUI.Show(BuildTerminalSummary());
        TryShowSkillMemoryReward();
    }

    private void TryShowSkillMemoryReward()
    {
        RunManager run = GetRunManager();
        if (run.VisitedNodeCodes == null || run.VisitedNodeCodes.Length == 0) return;
        try
        {
            if (skillProgressStore == null) skillProgressStore = SkillProgressStore.CreateDefault();
            SkillRewardClaimRow claim = skillProgressStore.EnsureStandardRunOffer(
                run.RunId, true, run.RunSeed);
            if (claim == null || claim.Status != "Pending") return;
            if (battleCanvas == null && playerHPText != null)
                battleCanvas = playerHPText.GetComponentInParent<Canvas>(true);
            if (battleCanvas == null) return;
            skillRewardSelectionUI = battleCanvas.GetComponent<SkillRewardSelectionUI>();
            if (skillRewardSelectionUI == null)
                skillRewardSelectionUI = battleCanvas.gameObject.AddComponent<SkillRewardSelectionUI>();
            skillRewardSelectionUI.Show(claim.OfferedSkills, code =>
            {
                try
                {
                    if (skillProgressStore.TryClaim(claim.ClaimId, code))
                        skillRewardSelectionUI.HideImmediate();
                    else
                        skillRewardSelectionUI.ShowMessage("This skill could not be saved. Your offer remains available in Skill Archive.");
                }
                catch (System.Exception error)
                {
                    Debug.LogError("Skill reward could not be saved: " + error.Message);
                    skillRewardSelectionUI.ShowMessage("Save failed. Your offer remains available in Skill Archive.");
                }
            });
        }
        catch (System.Exception error)
        {
            Debug.LogError("Skill Memory offer could not be prepared: " + error.Message);
        }
    }

    private RunSummarySnapshot BuildTerminalSummary()
    {
        return RunSummarySnapshot.Create(
            terminalOutcome,
            terminalEndingReason,
            terminalEnemyName,
            GetRunManager(),
            GetRunStatController(),
            echoAward,
            echoReceipt,
            echoSaveFailed,
            historySaveFailed
        );
    }

    private bool TryRecordTerminalHistory()
    {
        try
        {
            if (runHistoryStore == null) runHistoryStore = RunHistoryStore.CreateDefault();
            runHistoryStore.RecordFinishedRun(BuildTerminalSummary());
            historyCommitted = true;
            historySaveFailed = false;
            return true;
        }
        catch (System.Exception error)
        {
            historySaveFailed = true;
            Debug.LogError("Run History gagal disimpan: " + error.Message);
            return false;
        }
    }

    private bool FinalizeEchoFragments()
    {
        try
        {
            RunManager run = GetRunManager();
            echoAward = EchoFragmentAward.FromFinishedRun(run);
            if (echoWallet == null) echoWallet = EchoFragmentWallet.CreateDefault();
            echoReceipt = echoWallet.ClaimFinishedRun(run);
            echoSaveFailed = false;
            return true;
        }
        catch (System.Exception error)
        {
            echoSaveFailed = true;
            Debug.LogError("Echo Fragment gagal disimpan: " + error.Message);
            return false;
        }
    }

    private void OpenRunMap()
    {
        runSummaryUI?.HideImmediate();
        RunManager activeRun = GetRunManager();

        if (!activeRun.IsActive ||
            activeRun.IsCompleted ||
            activeRun.NodeMap == null ||
            activeRun.CurrentNode == null)
        {
            ShowDatabaseError(
                "Standard Run Map tidak memiliki node aktif yang valid."
            );
            return;
        }

        if (!EnsureStandardRunMapUI())
        {
            ShowDatabaseError(
                "Standard Run Map tidak dapat dibuat pada Canvas Battle."
            );
            return;
        }

        if (rewardTransitionCoroutine != null)
        {
            StopCoroutine(rewardTransitionCoroutine);
            rewardTransitionCoroutine = null;
        }

        rewardSelectionOpen = false;
        rewardSelectionCommitted = false;
        rewardSelectionUI?.HideImmediate();
        SetPanelActive(rewardPanel, false);
        SetPanelActive(resultPanel, false);
        HideEnemyIntent();
        HideConvergencePreview();
        SetBattleChromeVisible(false);

        isPlayerTurn = false;
        battleEnded = true;
        currentEnemy = null;
        enemyActionPlan = null;
        DisableAllBattleButtons();

        StandardRunNode currentNode = activeRun.CurrentNode;
        SetBattleLog(
            $"RUN MAP - Node {currentNode.Depth}/{activeRun.NodeMap.Count}: " +
            $"{currentNode.Type}."
        );
        UpdateUI();
        RunStatController mapStats = GetRunStatController();
        standardRunMapUI.SetResourceCaps(mapStats.EffectiveMaxHP, mapStats.EffectiveMaxMana);
        standardRunMapUI.ShowRun(activeRun);
        standardRunMapUI.SetCheckpointSaveStatus(
            checkpointSaveFailed, RetryActiveRunCheckpoint
        );
        EnsureGuidedTutorialController();
        guidedTutorialController?.TryBeginAtMap(activeRun, standardRunMapUI);
    }

    private void HandleRunMapNodeSelected(string nodeCode)
    {
        RunManager activeRun = GetRunManager();
        StandardRunNode currentNode = activeRun.CurrentNode;

        if (standardRunMapUI == null || !standardRunMapUI.IsOpen)
        {
            Debug.LogError(
                "Run Map menolak callback karena peta tidak sedang terbuka."
            );
            return;
        }

        StandardRunNode requestedNode = activeRun.NodeMap.FindNode(nodeCode);
        if (requestedNode != null && requestedNode.Type == StandardRunNodeType.Merchant && activeRun.CanEnterNode(nodeCode))
        {
            RunStatController stats = GetRunStatController();
            if (RunMerchantService.TryResolve(activeRun, nodeCode, standardRunMapUI.MerchantChoiceIndex,
                stats.EffectiveMaxHP, stats.EffectiveMaxMana))
            {
                SyncLegacyRunStateFields();
                SaveActiveRunCheckpoint("merchant choice resolved");
            }
            OpenRunMap();
            return;
        }
        if (requestedNode != null && requestedNode.Type == StandardRunNodeType.RandomEvent && activeRun.CanEnterNode(nodeCode))
        {
            RunStatController stats = GetRunStatController();
            if (RunEventService.TryResolve(activeRun, nodeCode, standardRunMapUI.EventChoiceIndex,
                stats.EffectiveMaxHP, stats.EffectiveMaxMana))
            {
                SyncLegacyRunStateFields();
                SaveActiveRunCheckpoint("event choice resolved");
            }
            OpenRunMap();
            return;
        }

        if (!activeRun.TryEnterNode(nodeCode))
        {
            Debug.LogError(
                "Run Map menolak pilihan node yang stale atau tidak aktif."
            );
            OpenRunMap();
            return;
        }

        currentNode = activeRun.CurrentNode;

        if (currentNode.IsBattle)
        {
            standardRunMapUI?.HideImmediate();
            StartBattle();
            return;
        }

        if (currentNode.Type == StandardRunNodeType.Relic)
        {
            standardRunMapUI.HideImmediate();
            EnsureRewardSelectionUI();
            currentRewardChoices = RunRelicCatalog.CreateChoices(activeRun.RunSeed, activeRun.Modifiers);
            rewardSelectionKind = RewardSelectionKind.Relic;
            rewardSelectionOpen = true;
            rewardSelectionCommitted = false;
            ConfigureRewardButtonText();
            SetText(rewardTitleText, "CHOOSE ONE RELIC");
            SetBattleLog("Relic Shrine — choose one relic for this journey.");
            SetBattleChromeVisible(false);
            SetPanelActive(resultPanel, false);
            if (rewardSelectionUI != null && rewardSelectionUI.IsInitialized) rewardSelectionUI.ShowAnimated();
            else SetPanelActive(rewardPanel, true);
            return;
        }

        if (currentNode.Type == StandardRunNodeType.Rest)
        {
            ResolveRestNode(currentNode.Code, standardRunMapUI.RestoresMana);
            return;
        }
        ResolvePlaceholderNode(currentNode);
    }

    private void ResolveRestNode(string nodeCode, bool restoreMana)
    {
        RunManager run = GetRunManager();
        if (!run.NodeEntered || run.CurrentNode == null ||
            run.CurrentNode.Type != StandardRunNodeType.Rest || run.CurrentNode.Code != nodeCode) return;
        RunStatController stats = GetRunStatController();
        if (!run.TryResolveCurrentNode(StandardRunNodeType.Rest, nodeCode)) return;
        if (restoreMana)
            run.RestoreMana(Mathf.CeilToInt(stats.EffectiveMaxMana * 0.5f), stats.EffectiveMaxMana);
        else
            run.RestoreHP(Mathf.CeilToInt(stats.EffectiveMaxHP * 0.5f), stats.EffectiveMaxHP);
        SyncLegacyRunStateFields();
        SaveActiveRunCheckpoint("rest recovery chosen");
        OpenRunMap();
    }

    private void ResolvePlaceholderNode(StandardRunNode node)
    {
        RunManager activeRun = GetRunManager();

        if (node == null ||
            node.IsBattle ||
            !activeRun.TryResolveCurrentNode(node.Type, node.Code))
        {
            Debug.LogError(
                "RunManager menolak penyelesaian placeholder node."
            );
            OpenRunMap();
            return;
        }

        SyncLegacyRunStateFields();
        SaveActiveRunCheckpoint("placeholder node resolved");
        Debug.Log(
            $"Node {node.Depth} {node.Type} diselesaikan sebagai " +
            "placeholder tanpa mengubah HP, Mana, Embers, atau modifier."
        );
        OpenRunMap();
    }

    private static int GetEncounterDatabaseIndex(
        StandardRunNodeMap map,
        StandardRunNode node
    )
    {
        if (map == null || node == null || !node.IsBattle)
        {
            throw new System.ArgumentException(
                "Encounter membutuhkan node battle dari map yang valid."
            );
        }

        if (node.Type == StandardRunNodeType.Boss)
        {
            return 3;
        }

        if (node.Type == StandardRunNodeType.EliteBattle)
        {
            return 2;
        }

        int normalBattleOrdinal = 0;

        for (int index = 0; index < map.Nodes.Length; index++)
        {
            StandardRunNode candidate = map.Nodes[index];

            if (candidate.Type == StandardRunNodeType.NormalBattle)
            {
                normalBattleOrdinal++;
            }

            if (candidate.Depth == node.Depth)
            {
                break;
            }
        }

        return 1 + ((System.Math.Max(1, normalBattleOrdinal) - 1 +
            (node.Lane == 1 ? 0 : 1)) % 2);
    }

    private static string GetBattleNodeLabel(StandardRunNode node)
    {
        switch (node.Type)
        {
            case StandardRunNodeType.EliteBattle:
                return $"ELITE NODE {node.Depth}";
            case StandardRunNodeType.Boss:
                return "FINAL BOSS";
            default:
                return $"BATTLE NODE {node.Depth}";
        }
    }

    private void SetBattleChromeVisible(bool visible)
    {
        if (commandPanelObject != null)
        {
            commandPanelObject.SetActive(visible);
        }

        if (orbitPanelObject != null)
        {
            orbitPanelObject.SetActive(visible);
        }
    }

    private void ResetOrbitForBattle()
    {
        orbitState.Clear();
        usedSkillPassives.Clear();
        passiveReadySkills.Clear();
        skillWardRemaining = 0;
        skillParryReady = false;
        nextBasicAttackBonus = 0;
        lastBasicAttackDamage = 0;
        markedEnemy = false;
        analyzeProtectionReady = false;
        orbitalSeverConvergenceReady = false;
        previousPlayerAction = null;

        if (EnsureOrbitUIAnimator(true))
        {
            orbitUIAnimator.ClearSlotIcons();
        }

        reversalCounterReady = false;
        convergenceNotice = string.Empty;
        UpdateOrbitUI();
    }

    private void UpdateOrbitUI()
    {
        SetOrbitSlot(orbitSlot1Text, 0);
        SetOrbitSlot(orbitSlot2Text, 1);
        SetOrbitSlot(orbitSlot3Text, 2);

        if (EnsureOrbitUIAnimator(true))
        {
            orbitUIAnimator.SynchronizeSlots(orbitState.Actions, true);
        }

        HideConvergencePreview();
    }

    private void SetOrbitSlot(TMP_Text targetText, int actionIndex)
    {
        if (targetText == null)
        {
            return;
        }

        if (orbitState.TryGetAction(
            actionIndex,
            out PlayerActionType action
        ))
        {
            targetText.text = GetActionLabel(action);
            return;
        }

        targetText.text = "—";
    }

    private string GetActionLabel(PlayerActionType action)
    {
        switch (action)
        {
            case PlayerActionType.Attack:
                return "A";

            case PlayerActionType.Skill:
                return "S";

            case PlayerActionType.Guard:
                return "G";

            default:
                return "?";
        }
    }

    private void UpdateUI()
    {
        RunManager activeRun = GetRunManager();

        if (playerHPText != null)
        {
            playerHPText.text =
                $"Player HP: {activeRun.CurrentHP}/{GetCurrentPlayerMaxHP()}";
            string playerStatuses = FormatStatusSummary(
                BattleStatusTarget.Player
            );
            if (!battleEnded && playerStatuses.Length > 0)
            {
                playerHPText.text += $"\n{playerStatuses}";
            }
        }

        if (playerManaText != null)
        {
            playerManaText.text =
                $"Mana: {activeRun.CurrentMana}/{GetCurrentPlayerMaxMana()}";
        }

        if (enemyHPText != null)
        {
            enemyHPText.text = currentEnemy != null
                ? $"{currentEnemy.Name} HP: " +
                  $"{enemyHP}/{currentEnemy.MaxHP}"
                : "Enemy tidak tersedia";
            string enemyStatuses = FormatStatusSummary(
                BattleStatusTarget.Enemy
            );
            if (!battleEnded && enemyStatuses.Length > 0)
            {
                enemyHPText.text += $"\n{enemyStatuses}";
            }
        }
    }

    private void UpdateButtons()
    {
        RunManager activeRun = GetRunManager();
        bool canAct = CanPlayerAct();
        RefreshSkillButtonPresentation();

        if (analyzeButton != null)
        {
            analyzeButton.interactable =
                canAct && currentEnemy != null && enemyActionPlan != null &&
                !analyzeUsedThisNode &&
                activeRun.CurrentMana >= GetAnalyzeManaCost();
            SetButtonText(
                analyzeButton,
                analyzeUsedThisNode
                    ? "Analyze (Used)"
                    : $"Analyze ({GetAnalyzeManaCost()} Mana)"
            );
        }

        if (basicAttackButton != null)
        {
            basicAttackButton.interactable = canAct;
        }

        if (guardButton != null)
        {
            guardButton.interactable = canAct;
        }

        for (int slot = 0; slot < equippedSkillButtons.Length; slot++)
        {
            Button button = equippedSkillButtons[slot];
            SkillDefinition definition = equippedSkills[slot];
            if (button == null) continue;
            button.gameObject.SetActive(definition != null);
            if (definition == null) continue;
            int manaCost = GetCurrentSkillManaCost(slot);
            button.interactable = canAct && activeRun.CurrentMana >= manaCost;
            SetButtonText(button, $"{definition.Name} ({manaCost} Mana)");
        }
    }

    private void DisableAllBattleButtons()
    {
        if (analyzeButton != null)
        {
            analyzeButton.interactable = false;
        }

        if (basicAttackButton != null)
        {
            basicAttackButton.interactable = false;
        }

        if (skillButton != null)
        {
            skillButton.interactable = false;
        }
        for (int slot = 1; slot < equippedSkillButtons.Length; slot++)
            if (equippedSkillButtons[slot] != null)
                equippedSkillButtons[slot].interactable = false;

        if (guardButton != null)
        {
            guardButton.interactable = false;
        }
    }

    private void ShowDatabaseError(string message)
    {
        Debug.LogError(message);
        HideEnemyIntent();
        HideConvergencePreview();

        battleEnded = true;
        isPlayerTurn = false;

        DisableAllBattleButtons();
        SetPanelActive(rewardPanel, false);

        if (resultPanel == null)
        {
            return;
        }

        SetText(resultTitleText, "DATABASE ERROR");
        SetText(resultDescriptionText, message);
        SetObjectActive(retryButton, false);
        SetObjectActive(nextBattleButton, false);
        SetPanelActive(resultPanel, true);
    }

    private void SetBattleLog(
        string mainMessage,
        string additionalMessage = ""
    )
    {
        if (battleLogText != null)
        {
            battleLogText.text = mainMessage + additionalMessage;
        }
    }

    private void SetButtonText(Button targetButton, string newText)
    {
        if (targetButton == null)
        {
            return;
        }

        TMP_Text buttonText =
            targetButton.GetComponentInChildren<TMP_Text>(true);

        if (buttonText != null)
        {
            buttonText.text = newText;
        }
    }

    private void SetText(TMP_Text targetText, string newText)
    {
        if (targetText != null)
        {
            targetText.text = newText;
        }
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    private void SetObjectActive(Button targetButton, bool isActive)
    {
        if (targetButton != null)
        {
            targetButton.gameObject.SetActive(isActive);
        }
    }

    private void OnDestroy()
    {
        encounterBanner?.Cancel();
        if (battleCommandMenu != null)
        {
            battleCommandMenu.PreviewRequested -= ShowConvergencePreview;
            battleCommandMenu.PreviewHidden -= HideConvergencePreview;
        }

        if (analyzeButton != null)
        {
            analyzeButton.onClick.RemoveListener(UseAnalyze);
        }

        if (basicAttackButton != null)
        {
            basicAttackButton.onClick.RemoveListener(UseBasicAttack);
        }

        if (skillButton != null)
        {
            skillButton.onClick.RemoveListener(UseSkill);
        }
        for (int slot = 1; slot < equippedSkillButtons.Length; slot++)
            if (equippedSkillButtons[slot] != null)
                equippedSkillButtons[slot].onClick.RemoveAllListeners();

        if (guardButton != null)
        {
            guardButton.onClick.RemoveListener(UseGuard);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RestartRun);
        }

        if (nextBattleButton != null)
        {
            nextBattleButton.onClick.RemoveListener(OpenRewardSelection);
        }

        skillProgressStore?.Dispose();

        if (rewardButton1 != null)
        {
            rewardButton1.onClick.RemoveListener(ChooseAttackReward);
        }

        if (rewardButton2 != null)
        {
            rewardButton2.onClick.RemoveListener(ChooseMaxHPReward);
        }

        if (rewardButton3 != null)
        {
            rewardButton3.onClick.RemoveListener(ChooseGuardReward);
        }
    }
}
