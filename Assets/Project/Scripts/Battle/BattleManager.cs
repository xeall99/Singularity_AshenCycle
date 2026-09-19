using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    private const float RewardRevealDelay = 0.80f;

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
    private EnemyData currentEnemy;
    private EnemyActionPlan enemyActionPlan;
    private RewardDefinition[] currentRewardChoices;
    private BattleVisuals battleVisuals;
    private BattleCommandMenu battleCommandMenu;
    private OrbitUIAnimator orbitUIAnimator;
    private RewardSelectionUI rewardSelectionUI;
    private RewardManager rewardManager;
    private RunStatController runStatController;
    private RunManager runManager;
    private StandardRunMapUI standardRunMapUI;
    private Canvas battleCanvas;
    private GameObject commandPanelObject;
    private GameObject orbitPanelObject;
    private GameObject convergenceTooltipRoot;
    private RectTransform convergenceTooltipRect;
    private Coroutine rewardTransitionCoroutine;
    private bool hasReportedMissingOrbitUIAnimator;

    private readonly OrbitState orbitState = new OrbitState();
    private readonly OrbitResolver orbitResolver = new OrbitResolver();
    private readonly RunModifierCollection runModifiers =
        new RunModifierCollection();

    private void Awake()
    {
        InitializeRunStatController();
        InitializeRunManager();
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

        if (skillButton != null)
        {
            TMP_Text skillButtonText =
                skillButton.GetComponentInChildren<TMP_Text>();

            if (skillButtonText != null)
            {
                skillButtonText.text =
                    $"{shadowStrike.Name} " +
                    $"({shadowStrike.ManaCost} Mana)";
            }
        }

        ConfigureRewardButtonText();

        Debug.Log(
            $"BattleManager menerima skill: " +
            $"{shadowStrike.Name} | " +
            $"Mana: {shadowStrike.ManaCost} | " +
            $"Power: {shadowStrike.Power}"
        );

        RestartRun();
    }

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
        rewardTransitionCoroutine = null;
        rewardSelectionUI?.HideImmediate();
        standardRunMapUI?.HideImmediate();
        enemyActionPlan = null;
        currentRewardChoices = null;
        preparedRewardDepth = -1;
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
        SyncLegacyRunStateFields();
        enemyHP = currentEnemy.MaxHP;

        isPlayerTurn = true;
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
    }

    private void UseBasicAttack()
    {
        if (!CanPlayerAct())
        {
            return;
        }

        HideConvergencePreview();
        int currentAttackDamage = GetCurrentBasicAttackDamage();

        battleVisuals?.PlayPlayerAttack();
        battleVisuals?.PlayEnemyHit();

        enemyHP = Mathf.Max(0, enemyHP - currentAttackDamage);

        string actionLog =
            $"Player menggunakan Basic Attack. " +
            $"Damage: {currentAttackDamage}.";

        string orbitLog = RecordOrbitAction(
            PlayerActionType.Attack,
            currentAttackDamage
        );

        SetBattleLog(actionLog, orbitLog);
        FinishPlayerAction();
    }

    private void UseSkill()
    {
        if (!CanPlayerAct())
        {
            return;
        }

        if (shadowStrike == null)
        {
            SetBattleLog("Data Shadow Strike tidak ditemukan.");
            return;
        }

        RunManager activeRun = GetRunManager();

        if (activeRun.CurrentMana < shadowStrike.ManaCost)
        {
            SetBattleLog("Mana tidak cukup.");
            return;
        }

        HideConvergencePreview();
        battleVisuals?.PlayPlayerAttack();
        battleVisuals?.PlayEnemyHit();

        if (!activeRun.TrySpendMana(shadowStrike.ManaCost))
        {
            SetBattleLog("Mana tidak cukup.");
            return;
        }

        SyncLegacyRunStateFields();
        enemyHP = Mathf.Max(0, enemyHP - shadowStrike.Power);

        string actionLog =
            $"Player menggunakan {shadowStrike.Name}. " +
            $"Damage: {shadowStrike.Power}.";

        string orbitLog = RecordOrbitAction(
            PlayerActionType.Skill,
            shadowStrike.Power,
            string.IsNullOrWhiteSpace(shadowStrike.Code)
                ? shadowStrike.Name
                : shadowStrike.Code
        );

        SetBattleLog(actionLog, orbitLog);
        FinishPlayerAction();
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
        GetRunManager().RestoreMana(
            2,
            GetCurrentPlayerMaxMana()
        );
        SyncLegacyRunStateFields();

        int guardReductionPercent = Mathf.RoundToInt(
            GetCurrentGuardDamageReduction() * 100f
        );

        string actionLog =
            "Player menggunakan Guard. " +
            $"Damage berikutnya berkurang {guardReductionPercent}%.";

        string orbitLog = RecordOrbitAction(
            PlayerActionType.Guard,
            0
        );

        SetBattleLog(actionLog, orbitLog);
        FinishPlayerAction();
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

        RevealEnemyActionPlan();
        SetBattleLog(
            $"Analyze menggunakan {manaCost} Mana. Giliran Player berlanjut."
        );

        // Analyze is informational: it neither records an Orbit action nor ends the turn.
        UpdateUI();
        UpdateButtons();
    }

    private bool PrepareEnemyActionPlan()
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

        enemyActionPlan = EnemyActionPlan.CreateNormalAttack(
            currentEnemy.Id,
            currentEnemy.AttackDamage
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
        return isPlayerTurn && !battleEnded;
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

        if (!orbitState.IsFull)
        {
            UpdateOrbitUI();
            return string.Empty;
        }

        ConvergenceResult result = orbitResolver.Resolve(
            orbitState.Actions
        );

        string effectLog = ApplyConvergence(result, actionDamage);

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

        StartCoroutine(EnemyTurn());
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
        yield return new WaitForSeconds(0.8f);

        battleVisuals?.PlayEnemyAttack();

        yield return new WaitForSeconds(0.12f);

        battleVisuals?.PlayPlayerHit();

        int receivedDamage = executingPlan.TotalEstimatedDamage;

        if (isGuarding)
        {
            float remainingDamageMultiplier =
                1f - GetCurrentGuardDamageReduction();

            receivedDamage = Mathf.Max(
                0,
                Mathf.CeilToInt(
                    receivedDamage * remainingDamageMultiplier
                )
            );

            isGuarding = false;
        }

        RunManager activeRun = GetRunManager();
        activeRun.ApplyDamage(receivedDamage);
        SyncLegacyRunStateFields();

        string enemyTurnLog =
            $"{currentEnemy.Name} menggunakan {executingPlan.DisplayName}. " +
            $"Player menerima {receivedDamage} damage.";

        HideEnemyIntent();
        SetBattleLog(enemyTurnLog);
        UpdateUI();

        if (activeRun.CurrentHP <= 0)
        {
            EndBattle(false);
            yield break;
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
        isPlayerTurn = true;

        SetBattleLog("Giliran Player. Mana bertambah 1.");

        UpdateUI();
        UpdateButtons();
    }

    private void EndBattle(bool playerWon)
    {
        if (battleEnded)
        {
            return;
        }

        battleEnded = true;
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
                "Menyiapkan pilihan Anomaly..."
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
        SetText(rewardTitleText, "CHOOSE A RUN REWARD");

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
            !rewardNode.IsBattle ||
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
            !resolvedNode.IsBattle ||
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
            currentRewardChoices = rewardManager.GenerateChoices(
                activeRun.Depth,
                GetRewardSeedForDepth(activeRun.Depth)
            );
            preparedRewardDepth = activeRun.Depth;

            if (currentRewardChoices == null ||
                currentRewardChoices.Length != 3 ||
                HasDuplicateRewardCodes(currentRewardChoices))
            {
                currentRewardChoices = null;
                preparedRewardDepth = -1;
                Debug.LogError(
                    "RewardManager wajib menghasilkan tiga reward berbeda."
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
        if (rewardTransitionCoroutine != null)
        {
            StopCoroutine(rewardTransitionCoroutine);
            rewardTransitionCoroutine = null;
        }

        rewardSelectionOpen = false;
        rewardSelectionCommitted = false;

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
        SyncLegacyRunModifierFields();
        SyncLegacyRunStateFields();
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

    private void OpenRunMap()
    {
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
        standardRunMapUI.Show(activeRun.NodeMap, activeRun.Depth);
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

        if (!activeRun.IsActive ||
            currentNode == null ||
            !string.Equals(
                currentNode.Code,
                nodeCode,
                System.StringComparison.Ordinal
            ))
        {
            Debug.LogError(
                "Run Map menolak pilihan node yang stale atau tidak aktif."
            );
            OpenRunMap();
            return;
        }

        if (currentNode.IsBattle)
        {
            standardRunMapUI?.HideImmediate();
            StartBattle();
            return;
        }

        ResolvePlaceholderNode(currentNode);
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

        return 1 + ((System.Math.Max(1, normalBattleOrdinal) - 1) % 2);
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
        }
    }

    private void UpdateButtons()
    {
        RunManager activeRun = GetRunManager();
        bool canAct = isPlayerTurn && !battleEnded;

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

        if (skillButton != null)
        {
            skillButton.interactable =
                canAct &&
                shadowStrike != null &&
                activeRun.CurrentMana >= shadowStrike.ManaCost;
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
