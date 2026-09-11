using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
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

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultDescriptionText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextBattleButton;

    [Header("Singularity Orbit UI")]
    [SerializeField] private TMP_Text orbitSlot1Text;
    [SerializeField] private TMP_Text orbitSlot2Text;
    [SerializeField] private TMP_Text orbitSlot3Text;
    [SerializeField] private TMP_Text convergencePreviewText;

    [Header("Player Stats")]
    [SerializeField] private int playerMaxHP = 100;
    [SerializeField] private int playerMaxMana = 10;
    [SerializeField] private int playerStartingMana = 5;
    [SerializeField] private int basicAttackDamage = 10;

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

    private bool isPlayerTurn;
    private bool isGuarding;
    private bool battleEnded;
    private bool runCompleted;
    private bool reversalCounterReady;

    private string convergenceNotice = string.Empty;

    private SkillData shadowStrike;
    private EnemyData currentEnemy;
    private BattleVisuals battleVisuals;

    private readonly OrbitState orbitState =
        new OrbitState();

    private readonly OrbitResolver orbitResolver =
        new OrbitResolver();

    private void Awake()
    {
        battleVisuals = GetComponent<BattleVisuals>();

        if (basicAttackButton != null)
        {
            basicAttackButton.onClick.AddListener(
                UseBasicAttack
            );
        }

        if (skillButton != null)
        {
            skillButton.onClick.AddListener(
                UseSkill
            );
        }

        if (guardButton != null)
        {
            guardButton.onClick.AddListener(
                UseGuard
            );
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(
                RetryBattle
            );
        }

        if (nextBattleButton != null)
        {
            nextBattleButton.onClick.AddListener(
                NextBattle
            );
        }
    }

    private void Start()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (GameDatabase.Instance == null ||
            !GameDatabase.Instance.IsReady)
        {
            ShowDatabaseError(
                "GameDatabase belum siap."
            );

            return;
        }

        shadowStrike =
            GameDatabase.Instance.GetSkillByCode(
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

        Debug.Log(
            $"BattleManager menerima skill: " +
            $"{shadowStrike.Name} | " +
            $"Mana: {shadowStrike.ManaCost} | " +
            $"Power: {shadowStrike.Power}"
        );

        StartBattle();
    }

    private void StartBattle()
    {
        StopAllCoroutines();

        currentEnemy =
            GameDatabase.Instance.GetEnemyByBattleIndex(
                battleNumber
            );

        if (currentEnemy == null)
        {
            ShowDatabaseError(
                $"Enemy Battle {battleNumber} " +
                "tidak ditemukan di database."
            );

            return;
        }

        playerHP = playerMaxHP;
        playerMana = playerStartingMana;
        enemyHP = currentEnemy.MaxHP;

        isPlayerTurn = true;
        isGuarding = false;
        battleEnded = false;

        ResetOrbitForBattle();

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        SetButtonText(retryButton, "Retry");
        SetButtonText(
            nextBattleButton,
            "Next Battle"
        );

        string battleType = currentEnemy.IsBoss
            ? "BOSS"
            : $"BATTLE {battleNumber}";

        if (battleLogText != null)
        {
            battleLogText.text =
                $"{battleType}: {currentEnemy.Name} - " +
                "Giliran Player.";
        }

        if (enemyIntentText != null)
        {
            enemyIntentText.text =
                $"Intent: Attack " +
                $"({currentEnemy.AttackDamage})";
        }

        Debug.Log(
            $"Memulai Battle {battleNumber}: " +
            $"{currentEnemy.Name} | " +
            $"HP: {currentEnemy.MaxHP} | " +
            $"Damage: {currentEnemy.AttackDamage} | " +
            $"Reward: {currentEnemy.RewardEmbers}"
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

        battleVisuals?.PlayPlayerAttack();
        battleVisuals?.PlayEnemyHit();

        enemyHP = Mathf.Max(
            0,
            enemyHP - basicAttackDamage
        );

        string actionLog =
            $"Player menggunakan Basic Attack. " +
            $"Damage: {basicAttackDamage}.";

        string orbitLog = RecordOrbitAction(
            PlayerActionType.Attack,
            basicAttackDamage
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
            SetBattleLog(
                "Data Shadow Strike tidak ditemukan."
            );

            return;
        }

        if (playerMana < shadowStrike.ManaCost)
        {
            SetBattleLog("Mana tidak cukup.");

            // Tidak memanggil RecordOrbitAction.
            // Artinya aksi gagal tidak masuk Orbit.
            return;
        }

        battleVisuals?.PlayPlayerAttack();
        battleVisuals?.PlayEnemyHit();

        playerMana -= shadowStrike.ManaCost;

        enemyHP = Mathf.Max(
            0,
            enemyHP - shadowStrike.Power
        );

        string actionLog =
            $"Player menggunakan {shadowStrike.Name}. " +
            $"Damage: {shadowStrike.Power}.";

        string orbitLog = RecordOrbitAction(
            PlayerActionType.Skill,
            shadowStrike.Power
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

        battleVisuals?.PlayGuard();

        isGuarding = true;

        playerMana = Mathf.Min(
            playerMaxMana,
            playerMana + 2
        );

        string actionLog =
            "Player menggunakan Guard. " +
            "Damage berikutnya berkurang 50%.";

        string orbitLog = RecordOrbitAction(
            PlayerActionType.Guard,
            0
        );

        SetBattleLog(actionLog, orbitLog);

        FinishPlayerAction();
    }

    private bool CanPlayerAct()
    {
        return isPlayerTurn && !battleEnded;
    }

    private string RecordOrbitAction(
        PlayerActionType actionType,
        int actionDamage
    )
    {
        convergenceNotice = string.Empty;

        orbitState.AddAction(actionType);

        if (!orbitState.IsFull)
        {
            UpdateOrbitUI();
            return string.Empty;
        }

        ConvergenceResult result =
            orbitResolver.Resolve(
                orbitState.Actions
            );

        string effectLog =
            ApplyConvergence(
                result,
                actionDamage
            );

        Debug.Log(
            $"Convergence aktif: " +
            $"{result.DisplayName} | " +
            $"{result.Description}"
        );

        convergenceNotice =
            $"CONVERGENCE: {result.DisplayName}";

        orbitState.Clear();
        UpdateOrbitUI();

        return
            $"\n{result.DisplayName}: {effectLog}";
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
                    actionDamage *
                    eventHorizonBonus
                );

                enemyHP = Mathf.Max(
                    0,
                    enemyHP - bonusDamage
                );

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
                int manaBefore = playerMana;

                playerMana = Mathf.Min(
                    playerMaxMana,
                    playerMana + 1
                );

                int restoredMana =
                    playerMana - manaBefore;

                if (restoredMana > 0)
                {
                    return "Memulihkan 1 Mana.";
                }

                return
                    "Mana sudah penuh sehingga " +
                    "tidak ada Mana yang dipulihkan.";
            }

            default:
                return string.Empty;
        }
    }

    private void FinishPlayerAction()
    {
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
        yield return new WaitForSeconds(0.8f);

        battleVisuals?.PlayEnemyAttack();

        yield return new WaitForSeconds(0.12f);

        battleVisuals?.PlayPlayerHit();

        int receivedDamage =
            currentEnemy.AttackDamage;

        if (isGuarding)
        {
            receivedDamage = Mathf.CeilToInt(
                receivedDamage * 0.5f
            );

            isGuarding = false;
        }

        playerHP = Mathf.Max(
            0,
            playerHP - receivedDamage
        );

        string enemyTurnLog =
            $"{currentEnemy.Name} menyerang. " +
            $"Player menerima {receivedDamage} damage.";

        SetBattleLog(enemyTurnLog);

        UpdateUI();

        if (playerHP <= 0)
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
                    basicAttackDamage *
                    reversalCounterMultiplier
                )
            );

            enemyHP = Mathf.Max(
                0,
                enemyHP - counterDamage
            );

            SetBattleLog(
                enemyTurnLog,
                $"\nREVERSAL ORBIT: " +
                $"Player membalas dengan " +
                $"{counterDamage} damage."
            );

            UpdateUI();

            if (enemyHP <= 0)
            {
                EndBattle(true);
                yield break;
            }
        }

        yield return new WaitForSeconds(0.8f);

        playerMana = Mathf.Min(
            playerMaxMana,
            playerMana + 1
        );

        isPlayerTurn = true;

        SetBattleLog(
            "Giliran Player. Mana bertambah 1."
        );

        UpdateUI();
        UpdateButtons();
    }

    private void ResetOrbitForBattle()
    {
        orbitState.Clear();

        reversalCounterReady = false;
        convergenceNotice = string.Empty;

        UpdateOrbitUI();
    }

    private void UpdateOrbitUI()
    {
        SetOrbitSlot(
            orbitSlot1Text,
            0
        );

        SetOrbitSlot(
            orbitSlot2Text,
            1
        );

        SetOrbitSlot(
            orbitSlot3Text,
            2
        );

        if (convergencePreviewText == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(
            convergenceNotice
        ))
        {
            convergencePreviewText.text =
                convergenceNotice;

            return;
        }

        if (orbitState.Count != 2)
        {
            convergencePreviewText.text =
                $"ORBIT {orbitState.Count}/3";

            return;
        }

        ConvergenceResult attackPreview =
            orbitResolver.Preview(
                orbitState.Actions,
                PlayerActionType.Attack
            );

        ConvergenceResult skillPreview =
            orbitResolver.Preview(
                orbitState.Actions,
                PlayerActionType.Skill
            );

        ConvergenceResult guardPreview =
            orbitResolver.Preview(
                orbitState.Actions,
                PlayerActionType.Guard
            );

        convergencePreviewText.text =
            $"ATTACK → {attackPreview.DisplayName}\n" +
            $"SKILL → {skillPreview.DisplayName}\n" +
            $"GUARD → {guardPreview.DisplayName}";
    }

    private void SetOrbitSlot(
        TMP_Text targetText,
        int actionIndex
    )
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
            targetText.text =
                GetActionLabel(action);

            return;
        }

        targetText.text = "—";
    }

    private string GetActionLabel(
        PlayerActionType action
    )
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
        if (playerHPText != null)
        {
            playerHPText.text =
                $"Player HP: " +
                $"{playerHP}/{playerMaxHP}";
        }

        if (playerManaText != null)
        {
            playerManaText.text =
                $"Mana: " +
                $"{playerMana}/{playerMaxMana}";
        }

        if (enemyHPText != null)
        {
            if (currentEnemy != null)
            {
                enemyHPText.text =
                    $"{currentEnemy.Name} HP: " +
                    $"{enemyHP}/{currentEnemy.MaxHP}";
            }
            else
            {
                enemyHPText.text =
                    "Enemy tidak tersedia";
            }
        }
    }

    private void UpdateButtons()
    {
        bool canAct =
            isPlayerTurn &&
            !battleEnded;

        if (basicAttackButton != null)
        {
            basicAttackButton.interactable =
                canAct;
        }

        if (guardButton != null)
        {
            guardButton.interactable =
                canAct;
        }

        if (skillButton != null)
        {
            skillButton.interactable =
                canAct &&
                shadowStrike != null &&
                playerMana >=
                shadowStrike.ManaCost;
        }
    }

    private void EndBattle(bool playerWon)
    {
        battleEnded = true;
        isPlayerTurn = false;

        UpdateUI();
        UpdateButtons();

        if (!playerWon)
        {
            runCompleted = false;

            if (resultTitleText != null)
            {
                resultTitleText.text =
                    "DEFEAT";
            }

            if (resultDescriptionText != null)
            {
                resultDescriptionText.text =
                    $"Dikalahkan oleh " +
                    $"{currentEnemy.Name}.\n" +
                    $"Total Embers: {totalEmbers}";
            }

            SetButtonText(
                retryButton,
                "Retry"
            );

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(
                    true
                );
            }

            if (nextBattleButton != null)
            {
                nextBattleButton.gameObject.SetActive(
                    false
                );
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
            }

            return;
        }

        totalEmbers +=
            currentEnemy.RewardEmbers;

        EnemyData nextEnemy =
            GameDatabase.Instance
                .GetEnemyByBattleIndex(
                    battleNumber + 1
                );

        if (nextEnemy != null)
        {
            runCompleted = false;

            if (resultTitleText != null)
            {
                resultTitleText.text =
                    "VICTORY";
            }

            if (resultDescriptionText != null)
            {
                resultDescriptionText.text =
                    $"{currentEnemy.Name} dikalahkan.\n" +
                    $"Mendapatkan " +
                    $"{currentEnemy.RewardEmbers} Embers.\n" +
                    $"Total Embers: {totalEmbers}";
            }

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(
                    false
                );
            }

            if (nextBattleButton != null)
            {
                nextBattleButton.gameObject.SetActive(
                    true
                );
            }
        }
        else
        {
            runCompleted = true;

            if (resultTitleText != null)
            {
                resultTitleText.text =
                    "RUN COMPLETE";
            }

            if (resultDescriptionText != null)
            {
                resultDescriptionText.text =
                    $"{currentEnemy.Name} " +
                    $"telah dikalahkan.\n" +
                    $"Total Embers: {totalEmbers}\n" +
                    "Seluruh battle berhasil " +
                    "diselesaikan.";
            }

            SetButtonText(
                retryButton,
                "Restart Run"
            );

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(
                    true
                );
            }

            if (nextBattleButton != null)
            {
                nextBattleButton.gameObject.SetActive(
                    false
                );
            }
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }
    }

    private void RetryBattle()
    {
        if (runCompleted)
        {
            battleNumber = 1;
            totalEmbers = 0;
            runCompleted = false;
        }

        StartBattle();
    }

    private void NextBattle()
    {
        battleNumber++;
        StartBattle();
    }

    private void DisableAllBattleButtons()
    {
        if (basicAttackButton != null)
        {
            basicAttackButton.interactable =
                false;
        }

        if (skillButton != null)
        {
            skillButton.interactable =
                false;
        }

        if (guardButton != null)
        {
            guardButton.interactable =
                false;
        }
    }

    private void ShowDatabaseError(string message)
    {
        Debug.LogError(message);

        battleEnded = true;
        isPlayerTurn = false;

        DisableAllBattleButtons();

        if (resultPanel == null)
        {
            return;
        }

        if (resultTitleText != null)
        {
            resultTitleText.text =
                "DATABASE ERROR";
        }

        if (resultDescriptionText != null)
        {
            resultDescriptionText.text =
                message;
        }

        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(
                false
            );
        }

        if (nextBattleButton != null)
        {
            nextBattleButton.gameObject.SetActive(
                false
            );
        }

        resultPanel.SetActive(true);
    }

    private void SetBattleLog(
        string mainMessage,
        string additionalMessage = ""
    )
    {
        if (battleLogText == null)
        {
            return;
        }

        battleLogText.text =
            mainMessage + additionalMessage;
    }

    private void SetButtonText(
        Button targetButton,
        string newText
    )
    {
        if (targetButton == null)
        {
            return;
        }

        TMP_Text buttonText =
            targetButton
                .GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            buttonText.text = newText;
        }
    }

    private void OnDestroy()
    {
        if (basicAttackButton != null)
        {
            basicAttackButton.onClick.RemoveListener(
                UseBasicAttack
            );
        }

        if (skillButton != null)
        {
            skillButton.onClick.RemoveListener(
                UseSkill
            );
        }

        if (guardButton != null)
        {
            guardButton.onClick.RemoveListener(
                UseGuard
            );
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(
                RetryBattle
            );
        }

        if (nextBattleButton != null)
        {
            nextBattleButton.onClick.RemoveListener(
                NextBattle
            );
        }
    }
}