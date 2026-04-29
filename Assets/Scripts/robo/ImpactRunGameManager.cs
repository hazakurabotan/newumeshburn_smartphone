using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ImpactRunGameManager : MonoBehaviour
{
    public static ImpactRunGameManager Instance { get; private set; }

    [Header("Run Settings")]
    [Min(1f)] public float goalDistance = 150f;
    [Min(1f)] public float maxEnergy = 100f;
    public bool clearWhenEnergyFull = false;
    public bool startWithFullEnergy = true;

    [SerializeField] private float currentEnergy = 0f;
    [SerializeField] private float currentDistance = 0f;

    [Header("Energy Drain")]
    public bool drainEnergyOverTime = true;
    [Min(0f)] public float energyDrainPerSecond = 1f;
    public bool gameOverWhenEnergyZero = true;

    [Header("Scene Transition")]
    public string nextSceneName = "";
    public string retrySceneName = "";
    [Min(0f)] public float clearDelay = 1.2f;
    [Min(0f)] public float gameOverDelay = 1.2f;

    [Header("References")]
    public ImpactRunnerController player;
    public Camera targetCamera;

    [Header("Goal Visual")]
    public Transform goalMarker;
    public bool placeGoalMarkerAtStart = true;
    public float goalMarkerY = -1.15f;

    [Header("UI")]
    public Slider hpSlider;
    public TMP_Text hpText;

    public Slider energySlider;
    public TMP_Text energyText;

    public TMP_Text distanceText;
    public TMP_Text stateText;

    private float startX;
    private bool cleared;
    private bool gameOver;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public float CurrentDistance => currentDistance;
    public bool IsFinished => cleared || gameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (player == null)
            player = FindObjectOfType<ImpactRunnerController>();

        if (player != null)
        {
            RegisterPlayer(player);
        }
        else
        {
            RefreshAllUI();
        }

        if (stateText != null)
            stateText.text = "";
    }

    private void Update()
    {
        if (IsFinished || player == null)
            return;

        currentDistance = Mathf.Max(0f, player.transform.position.x - startX);

        if (drainEnergyOverTime)
        {
            currentEnergy = Mathf.Clamp(currentEnergy - energyDrainPerSecond * Time.deltaTime, 0f, maxEnergy);
            UpdateEnergyUI();

            if (gameOverWhenEnergyZero && currentEnergy <= 0f)
            {
                PlayerDied();
                return;
            }
        }

        UpdateDistanceUI();

        if (currentDistance >= goalDistance)
        {
            ClearRun();
        }
    }

    public void RegisterPlayer(ImpactRunnerController runner)
    {
        player = runner;
        startX = runner.transform.position.x;
        currentDistance = 0f;

        if (startWithFullEnergy)
            currentEnergy = maxEnergy;

        if (goalMarker != null && placeGoalMarkerAtStart)
        {
            Vector3 p = goalMarker.position;
            p.x = startX + goalDistance;
            p.y = goalMarkerY;
            goalMarker.position = p;
        }

        SetPlayerHP(runner.CurrentHP, runner.MaxHP);
        UpdateEnergyUI();
        UpdateDistanceUI();
    }

    public void SetPlayerHP(int currentHP, int maxHPValue)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHPValue;
            hpSlider.value = currentHP;
        }

        if (hpText != null)
        {
            hpText.text = $"HP {currentHP}/{maxHPValue}";
        }
    }

    public void AddEnergy(float amount)
    {
        if (IsFinished)
            return;

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
        UpdateEnergyUI();

        if (clearWhenEnergyFull && currentEnergy >= maxEnergy)
        {
            ClearRun();
        }
    }

    public void SetEnergy(float value)
    {
        if (IsFinished)
            return;

        currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
        UpdateEnergyUI();

        if (clearWhenEnergyFull && currentEnergy >= maxEnergy)
        {
            ClearRun();
        }
    }

    public void ClearRun()
    {
        if (IsFinished)
            return;

        cleared = true;

        if (stateText != null)
            stateText.text = "CLEAR";

        if (player != null)
        {
            ImpactRunToRoboBattleState.Save(
                player.CurrentHP,
                player.MaxHP,
                Mathf.RoundToInt(currentEnergy),
                Mathf.RoundToInt(maxEnergy)
            );

            player.StopRunner();
        }

        StartCoroutine(LoadNextSceneRoutine());
    }

    public void PlayerDied()
    {
        if (IsFinished)
            return;

        gameOver = true;

        if (stateText != null)
            stateText.text = "GAME OVER";

        if (player != null)
            player.StopRunner();

        StartCoroutine(RetrySceneRoutine());
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        yield return new WaitForSeconds(clearDelay);

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("ImpactRunGameManager: nextSceneName Ç™ñ¢ê›íËÇ≈Ç∑ÅB");
        }
    }

    private IEnumerator RetrySceneRoutine()
    {
        yield return new WaitForSeconds(gameOverDelay);

        if (!string.IsNullOrWhiteSpace(retrySceneName))
        {
            SceneManager.LoadScene(retrySceneName);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void RefreshAllUI()
    {
        UpdateEnergyUI();
        UpdateDistanceUI();

        if (hpSlider != null)
            hpSlider.value = 0f;

        if (hpText != null)
            hpText.text = "HP 0/0";
    }

    private void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value = currentEnergy;
        }

        if (energyText != null)
        {
            energyText.text = $"ENERGY {Mathf.CeilToInt(currentEnergy)}/{Mathf.RoundToInt(maxEnergy)}";
        }
    }

    private void UpdateDistanceUI()
    {
        float remain = Mathf.Max(0f, goalDistance - currentDistance);

        if (distanceText != null)
        {
            distanceText.text = $"GOAL écÇË {Mathf.CeilToInt(remain)}m";
        }
    }
}