using System;
using System.Collections;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the in-VR task panels. Opens each panel when the monitor avatar reaches its signal position, turns
/// controller input into slider changes and decisions, and writes every response to CSV as soon as it is given.
/// </summary>
public class UIPlayerInput : MonoBehaviour
{
    private const string SummaryColumns = "R1 Split,R2 Split,Decision_1,Decision_2,Rating1,Rating2";
    private const string ResponseColumns = "Timestamp,Scene,Response,Value";
    private const float MonitorLookupInterval = 0.5f;
    private const float StickDeadZone = 0.1f;

    [Header("Button Action")]
    public InputActionReference pressA_Action;
    public InputActionReference pressX_Action;

    [Header("GameObjects")]
    public GameObject sliderCanvas1;
    public GameObject sliderCanvas2;
    public GameObject partnerCanvas1;
    public GameObject partnerCanvas2;
    public GameObject finalCanvas;

    [Header("Rating Panels")]
    public GameObject ratingCanvas1;
    public GameObject ratingCanvas2;

    [Header("Rating Transition Screens")]
    public GameObject question1Transition;
    public GameObject question2Transition;

    [Header("Information Boards")]
    public GameObject infoCanvas1;
    public GameObject infoCanvas2;

    [Header("Extra Text on the Boards")]
    public GameObject roleObject1;
    public GameObject roleObject2;

    [Header("Rating Values")]
    public int ratingVal1;
    public int ratingVal2;

    [Header("UI Canvas")]
    public Canvas waitingCanvas;
    public Canvas responseCanvas;

    [Header("Thumbstick Parameters")]
    public InputActionReference customThumbstick;
    public Vector2 rawStickVector;
    private float stickHoldDuration = 0f;
    private float intervalTimer = 0f;
    private int direction = 0;
    public float singleTimeCheck = 0.08f;

    [SerializeField] private int maxIncrement = 10;

    [Header("Saved Data")]
    public int selectedVal1;
    public int selectedVal2;
    public bool firstDecision;
    public bool secondDecision;

    [Header("Screen Last Time")]
    public float waitScreenTime;
    public float responseScreenTime;
    public float questionTransitionTime;

    [Header("UI TextMesh")]
    public TextMeshProUGUI partnerResponse;

    [Header("Scripted Partner Response")]
    [Tooltip("The scripted reply is a rejection when the first split is at or above this value.")]
    public int rejectThreshold = 80;
    public string acceptText = "accepts";
    public string rejectText = "rejects";

    [Header("Monitor Signal")]
    public GameObject monitor;

    [Tooltip("Name of the monitor avatar instance that Normcore creates on this client.")]
    public string monitorObjectName = "VR Player Complete Prefab 2(Clone)";

    [Tooltip("Monitor position that opens the information boards.")]
    public Vector3 taskInfoPosition = new Vector3(16, 0, 1);

    [Tooltip("Monitor position that shows the extra text on the boards.")]
    public Vector3 rolePosition = new Vector3(18, 0, 1);

    [Tooltip("Monitor position that opens the rating panels.")]
    public Vector3 ratingPosition = new Vector3(22, 0, 1);

    [Tooltip("Monitor position that opens the split panels.")]
    public Vector3 splitPosition = new Vector3(26, 0, 1);

    public bool SplitOn = false;

    [Header("Output")]
    [Tooltip("Folder inside the application's persistent data path. Empty writes to that path directly.")]
    [SerializeField] private string outputFolder = "";

    [Tooltip("Start of the summary file name: one row with all values, written after the last decision.")]
    [SerializeField] private string summaryFilePrefix = "SessionSummary_";

    [Tooltip("Start of the response file name: one row per response, written as soon as it is given.")]
    [SerializeField] private string responseFilePrefix = "ResponseLog_";

    private bool ratingOn = false;
    private bool taskInfoOn = false;
    private bool roleInfoOn = false;

    private string summaryPath;
    private string responsePath;
    private string sceneName;
    private InputAction thumbstick;
    private float nextMonitorLookupTime;

    private void Start()
    {
        thumbstick = customThumbstick != null ? customThumbstick.action : null;
        if (thumbstick != null)
        {
            thumbstick.started += ThumbStickStarted;
            thumbstick.performed += ThumbStickMoved;
            thumbstick.canceled += ThumbStickReleased;
        }

        Show(sliderCanvas1, false);
        Show(partnerCanvas1, false);
        Show(partnerCanvas2, false);
        Show(sliderCanvas2, false);
        Show(finalCanvas, false);

        Show(ratingCanvas1, false);
        Show(ratingCanvas2, false);

        Show(infoCanvas1, false);
        Show(infoCanvas2, false);

        Show(question1Transition, false);
        Show(question2Transition, false);

        Show(waitingCanvas, false);
        Show(responseCanvas, false);

        PrepareFiles();
    }

    private void OnDestroy()
    {
        // The action lives on a shared asset, so it would keep calling a destroyed object.
        if (thumbstick == null) return;
        thumbstick.started -= ThumbStickStarted;
        thumbstick.performed -= ThumbStickMoved;
        thumbstick.canceled -= ThumbStickReleased;
    }

    private void Update()
    {
        FindMonitor();

        if (WasPressed(pressA_Action))
        {
            HandleConfirm();
        }

        if (WasPressed(pressX_Action))
        {
            HandleReject();
        }
    }

    // ---------------------------------------------------------------- Input

    private void HandleConfirm()
    {
        if (IsOpen(ratingCanvas1))
        {
            if (!TryGetPanel(out RatingUI panel)) return;
            ratingVal1 = panel.playerValue;
            SaveResponse("Rating1", ratingVal1);
            TransitionToSecondRating();
        }
        else if (IsOpen(ratingCanvas2))
        {
            if (!TryGetPanel(out RatingUI panel)) return;
            ratingVal2 = panel.playerValue;
            SaveResponse("Rating2", ratingVal2);
            ExitFromSecondRating();
        }
        else if (IsOpen(sliderCanvas1))
        {
            if (!TryGetPanel(out SplitUI panel)) return;
            selectedVal1 = panel.playerValue;
            SaveResponse("R1 Split", selectedVal1);
            StartCoroutine(ConfirmFirstSplit());
        }
        else if (IsOpen(partnerCanvas1))
        {
            firstDecision = true;
            SaveResponse("Decision_1", firstDecision);
            StartCoroutine(ConfirmPartnerFirstSplit());
        }
        else if (IsOpen(sliderCanvas2))
        {
            if (!TryGetPanel(out SplitUI panel)) return;
            selectedVal2 = panel.playerValue;
            SaveResponse("R2 Split", selectedVal2);
            StartCoroutine(ConfirmSecondSplit());
        }
        else if (IsOpen(partnerCanvas2))
        {
            secondDecision = true;
            SaveResponse("Decision_2", secondDecision);
            StartCoroutine(ConfirmPartnerSecondSplit());
            SaveData();
        }
    }

    private void HandleReject()
    {
        if (IsOpen(partnerCanvas1))
        {
            firstDecision = false;
            SaveResponse("Decision_1", firstDecision);
            StartCoroutine(RejectPartnerFirstSplit());
        }
        else if (IsOpen(partnerCanvas2))
        {
            secondDecision = false;
            SaveResponse("Decision_2", secondDecision);
            StartCoroutine(RejectPartnerSecondSplit());
            SaveData();
        }
    }

    private void ThumbStickStarted(InputAction.CallbackContext context)
    {
        stickHoldDuration = 0f;
        intervalTimer = 0f;
        rawStickVector = context.ReadValue<Vector2>();

        direction = rawStickVector.x > StickDeadZone ? 1 : rawStickVector.x < -StickDeadZone ? -1 : 0;
    }

    private void ThumbStickMoved(InputAction.CallbackContext context)
    {
        rawStickVector = context.ReadValue<Vector2>();

        if (Mathf.Abs(rawStickVector.x) <= StickDeadZone) return;

        stickHoldDuration += Time.deltaTime;
        intervalTimer += Time.deltaTime;

        if (intervalTimer < singleTimeCheck) return;

        intervalTimer = 0f;

        // The step grows the longer the stick is held, up to maxIncrement.
        int increment = Mathf.FloorToInt(1f + stickHoldDuration * 2f);
        increment = Mathf.Min(increment, maxIncrement);

        SplitUI splitUI = FindPanel<SplitUI>();
        RatingUI ratingUI = FindPanel<RatingUI>();

        if (splitUI != null) splitUI.ChangeSliderValue(increment * direction);
        if (ratingUI != null) ratingUI.ChangeSliderValue(increment * direction);
    }

    private void ThumbStickReleased(InputAction.CallbackContext context)
    {
        rawStickVector = Vector2.zero;
        stickHoldDuration = 0f;
        intervalTimer = 0f;
        direction = 0;
    }

    // ------------------------------------------------------ Monitor signals

    public void FindMonitor()
    {
        if (monitor == null && Time.unscaledTime >= nextMonitorLookupTime)
        {
            nextMonitorLookupTime = Time.unscaledTime + MonitorLookupInterval;
            monitor = GameObject.Find(monitorObjectName);
        }

        // Check positions for triggering events
        MonitorReachTask();
        MonitorReachRole();
        MonitorLeaveTask();
        MonitorReachSplit();
    }

    public void MonitorReachSplit()
    {
        if (SplitOn || !IsMonitorAt(splitPosition)) return;
        Debug.Log("UIPlayerInput: split panels on.");
        StartCoroutine(SplitUIOn());
    }

    public void MonitorReachTask()
    {
        if (taskInfoOn || !IsMonitorAt(taskInfoPosition)) return;
        Debug.Log("UIPlayerInput: information boards on.");
        StartCoroutine(InfoUIOn());
    }

    public void MonitorReachRole()
    {
        if (roleInfoOn || !IsMonitorAt(rolePosition)) return;
        Debug.Log("UIPlayerInput: extra board text on.");
        StartCoroutine(RoleUIOn());
    }

    public void MonitorLeaveTask()
    {
        if (ratingOn || !IsMonitorAt(ratingPosition)) return;
        Debug.Log("UIPlayerInput: rating panels on.");
        StartCoroutine(RatingUIOn());
    }

    private bool IsMonitorAt(Vector3 position) => monitor != null && monitor.transform.position == position;

    // --------------------------------------------------------------- Panels

    public IEnumerator SplitUIOn()
    {
        if (!SplitOn)
        {
            Show(sliderCanvas1, true);
            Show(partnerCanvas1, false);
            Show(partnerCanvas2, false);
            Show(sliderCanvas2, false);

            Show(waitingCanvas, false);
            Show(responseCanvas, false);

            SplitOn = true;
        }
        yield return null;
    }

    public IEnumerator InfoUIOn()
    {
        if (!taskInfoOn)
        {
            Show(infoCanvas1, true);
            Show(infoCanvas2, true);
            Show(roleObject1, false);
            Show(roleObject2, false);
            taskInfoOn = true;
        }
        yield return null;
    }

    public IEnumerator RoleUIOn()
    {
        if (!roleInfoOn)
        {
            Show(infoCanvas1, true);
            Show(infoCanvas2, true);
            Show(roleObject1, true);
            Show(roleObject2, true);
            roleInfoOn = true;
        }
        yield return null;
    }

    public IEnumerator RatingUIOn()
    {
        if (!ratingOn)
        {
            StartCoroutine(QuestionTransition(question1Transition, ratingCanvas1));
            Show(ratingCanvas2, false);
            ratingOn = true;
        }
        yield return null;
    }

    public void ExitFromSecondRating()
    {
        Show(ratingCanvas2, false);
    }

    public void TransitionToSecondRating()
    {
        StartCoroutine(QuestionTransition(question2Transition, ratingCanvas2));
        Show(ratingCanvas1, false);
    }

    public IEnumerator ConfirmFirstSplit()
    {
        Show(sliderCanvas1, false);
        Show(waitingCanvas, true);
        yield return new WaitForSeconds(waitScreenTime);
        Show(waitingCanvas, false);
        Show(partnerCanvas1, true);
    }

    public IEnumerator ConfirmPartnerFirstSplit() => FinishFirstRound();

    public IEnumerator RejectPartnerFirstSplit() => FinishFirstRound();

    public IEnumerator ConfirmSecondSplit()
    {
        Show(sliderCanvas2, false);
        Show(waitingCanvas, true);
        yield return new WaitForSeconds(waitScreenTime);
        Show(waitingCanvas, false);
        Show(partnerCanvas2, true);
    }

    public IEnumerator ConfirmPartnerSecondSplit() => FinishSecondRound();

    public IEnumerator RejectPartnerSecondSplit() => FinishSecondRound();

    public IEnumerator QuestionTransition(GameObject TransitionScreen, GameObject ratingPanel)
    {
        Show(TransitionScreen, true);
        yield return new WaitForSeconds(questionTransitionTime);
        Show(TransitionScreen, false);
        Show(ratingPanel, true);
    }

    private IEnumerator FinishFirstRound()
    {
        Show(partnerCanvas1, false);
        Show(waitingCanvas, true);
        yield return new WaitForSeconds(waitScreenTime);
        Show(waitingCanvas, false);
        Show(responseCanvas, true);
        ShowPartnerResponse(selectedVal1);
        yield return new WaitForSeconds(responseScreenTime);
        Show(sliderCanvas2, true);
        Show(responseCanvas, false);
    }

    private IEnumerator FinishSecondRound()
    {
        Show(partnerCanvas2, false);
        Show(finalCanvas, true);
        yield return null;
    }

    private void ShowPartnerResponse(int split)
    {
        if (partnerResponse == null) return;
        partnerResponse.text = split >= rejectThreshold ? rejectText : acceptText;
    }

    // --------------------------------------------------------------- Output

    private void PrepareFiles()
    {
        sceneName = SceneManager.GetActiveScene().name;

        string folder = Path.Combine(Application.persistentDataPath, outputFolder);
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        summaryPath = Path.Combine(folder, summaryFilePrefix + stamp + ".csv");
        responsePath = Path.Combine(folder, responseFilePrefix + stamp + ".csv");

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogError($"UIPlayerInput: cannot create {folder}: {e.Message}");
            return;
        }

        AppendLine(summaryPath, SummaryColumns);
        AppendLine(responsePath, ResponseColumns);
    }

    /// <summary>Writes the summary row with all values. Called after the last decision.</summary>
    private void SaveData()
    {
        string newLine = selectedVal1 + "," + selectedVal2 + "," + firstDecision + "," + secondDecision + "," + ratingVal1 + "," + ratingVal2;
        AppendLine(summaryPath, newLine);
        Debug.Log("Saved To " + summaryPath);
    }

    private void SaveResponse(string response, int value) =>
        SaveResponse(response, value.ToString(CultureInfo.InvariantCulture));

    private void SaveResponse(string response, bool value) =>
        SaveResponse(response, value.ToString());

    private void SaveResponse(string response, string value)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        AppendLine(responsePath, timestamp + "," + CsvField(sceneName) + "," + CsvField(response) + "," + CsvField(value));
    }

    private static string CsvField(string text)
    {
        if (text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    // Opens and closes the file for each line, so that nothing is lost when the application stops.
    private static void AppendLine(string path, string line)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(line);
            }
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogError($"UIPlayerInput: cannot write {path}: {e.Message}");
        }
    }

    // -------------------------------------------------------------- Helpers

    private static bool WasPressed(InputActionReference reference) =>
        reference != null && reference.action != null && reference.action.WasPressedThisFrame();

    private static bool IsOpen(GameObject panel) => panel != null && panel.activeSelf;

    private static void Show(GameObject panel, bool visible)
    {
        if (panel != null) panel.SetActive(visible);
    }

    private static void Show(Canvas canvas, bool visible)
    {
        if (canvas != null) canvas.enabled = visible;
    }

    /// <summary>The panel of this type that is currently shown, or null.</summary>
    private static T FindPanel<T>() where T : MonoBehaviour
    {
        foreach (T panel in FindObjectsOfType<T>())
        {
            if (panel.isActiveAndEnabled) return panel;
        }
        return null;
    }

    private static bool TryGetPanel<T>(out T panel) where T : MonoBehaviour
    {
        panel = FindPanel<T>();
        if (panel != null) return true;
        Debug.LogWarning($"UIPlayerInput: no active {typeof(T).Name} found, the value was not read.");
        return false;
    }
}
