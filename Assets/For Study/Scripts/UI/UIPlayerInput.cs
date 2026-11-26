/*
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class UIPlayerInput : MonoBehaviour
{

    [Header("Button Action")]
    public InputActionReference pressA_Action;
    public InputActionReference pressX_Action;
    
    [Header("GameObjects")]
    public GameObject sliderCanvas1;
    public GameObject sliderCanvas2;
    public GameObject partnerCanvas1;
    public GameObject partnerCanvas2;

    [Header("UI Canvas")]
    public Canvas waitingCanvas;
    public Canvas responseCanvas;

    [Header("Thumbstick Parameters")]
    public InputActionReference customThumbstick;
    public Vector2 rawStickVector;
    private float stickHoldDuration = 0f;  // Total time the stick is held
    private float intervalTimer = 0f;      
    private int direction = 0;    
    public float singleTimeCheck = 0.08f;   

    [SerializeField] private int maxIncrement = 10;  // Maximum increase per interval
    
    [Header("Saved Data")]
    public int selectedVal1;
    public int selectedVal2;
    public bool firstDecision;
    public bool secondDecision;

    [Header("Screen Last Time")]
    public float waitScreenTime;
    public float responseScreenTime;

    [Header("UI TextMesh")]
    public TextMeshProUGUI partnerResponse;


    private string filePath;

    public GameObject monitor;
    public bool SplitOn = false;

    void Start()
    {
        customThumbstick.action.started += ThumbStickStarted;    // When thumbstick is first pushed
        customThumbstick.action.performed += ThumbStickMoved;    // While thumbstick is being pushed
        customThumbstick.action.canceled += ThumbStickReleased;  // When thumbstick is released

        //sliderCanvas1.SetActive(true);
        sliderCanvas1.SetActive(false);
        partnerCanvas1.SetActive(false);
        partnerCanvas2.SetActive(false);
        sliderCanvas2.SetActive(false);

        waitingCanvas.enabled = false;
        responseCanvas.enabled = false;

        filePath = Path.Combine(Application.persistentDataPath, "SplitData.csv");

         
        if (!File.Exists(filePath))
        {
            WriteCSV("R1 Split,R2 Split, Decision_1, Decision_2");
        }
    }
    void WriteCSV(string line)
    {
        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine("R1 Split,R2 Split,Decision_1,Decision_2");
            writer.WriteLine(line);
        }
    }

    void SaveData()
    {
        string newLine = selectedVal1 + "," + selectedVal2 + "," + firstDecision + "," + secondDecision;;
        WriteCSV(newLine);
        Debug.Log("Saved To " + filePath);
    }

    void ThumbStickStarted(InputAction.CallbackContext context)
    {
        stickHoldDuration = 0f;     // Reset total hold timer
        intervalTimer = 0f;         // Reset interval timer
        rawStickVector = context.ReadValue<Vector2>();

        // Determine direction (right = 1, left = -1)
        if (rawStickVector.x > 0.1f)
        {
            direction = 1;  
        }
        else if (rawStickVector.x < -0.1f)
        {
            direction = -1;
        }
        else
        {
            direction = 0;
        }
    }

    void ThumbStickMoved(InputAction.CallbackContext context)
    {
        rawStickVector = context.ReadValue<Vector2>();

        if (Mathf.Abs(rawStickVector.x) > 0.1f)
        {
            stickHoldDuration += Time.deltaTime;    // Total hold time
            intervalTimer += Time.deltaTime;        // Timer for each interval

            // Every 0.2 seconds, increase the slider
            if (intervalTimer >= singleTimeCheck)
            {
                intervalTimer = 0f;  // Reset interval timer

                // Accelerate increase based on hold duration, but cap at maxIncrement
                int increment = Mathf.FloorToInt(1f + stickHoldDuration * 2f);
                increment = Mathf.Min(increment, maxIncrement);  // Cap the increase

                // Update slider value based on direction
                if (FindObjectOfType<SplitUI>() != null)
                {
                    FindObjectOfType<SplitUI>().ChangeSliderValue(increment * direction);
                }
                 
            }
        }
    }

    void ThumbStickReleased(InputAction.CallbackContext context)
    {
        // Reset timers and direction when the stick is released
        rawStickVector = Vector2.zero;
        stickHoldDuration = 0f;
        intervalTimer = 0f;
        direction = 0;
    }

    void Update()
    {
        FindMonitor();
        
        
        if(pressA_Action.action.WasPressedThisFrame())
        {
            if (!sliderCanvas1.activeSelf && !SplitOn)
            {
                MonitorReachMoneySplit();
            }

            else if(sliderCanvas1.activeSelf)
            {
                selectedVal1 = FindObjectOfType<SplitUI>().playerValue;
                Debug.Log("I selected " + selectedVal1);
                StartCoroutine(ConfirmFirstSplit());
            }

            else if(partnerCanvas1.activeSelf)
            {
                firstDecision = true;
                StartCoroutine(ConfirmPartnerFirstSplit());
            }

            else if(sliderCanvas2.activeSelf)
            {
                selectedVal2 = FindObjectOfType<SplitUI>().playerValue;
                Debug.Log("Then I selected " + selectedVal2);
                StartCoroutine(ConfirmSecondSplit()); 
            }

            else if(partnerCanvas2.activeSelf)
            {
                secondDecision = true;
                StartCoroutine(ConfirmPartnerSecondSplit());
                SaveData();
            }
        }

        if(pressX_Action.action.WasPressedThisFrame())
        {
            if(partnerCanvas1.activeSelf)
            {
                firstDecision = false;
                StartCoroutine(RejectPartnerFirstSplit());
            }

            else if(partnerCanvas2.activeSelf)
            {
                secondDecision = false;
                StartCoroutine(RejectPartnerSecondSplit());
                SaveData();
            }
        }
    }

    public void FindMonitor()
    {
        if(monitor == null)
        {
            monitor = GameObject.Find("VR Player Complete Prefab 2(Clone)");
        }
    } 

    public void MonitorReachMoneySplit()
    {
        if (monitor != null)
        {
            if(monitor.transform.position == new Vector3(26, 0, 1))
            {
                Debug.Log("Money Split On!");
                if (!SplitOn)
                {
                    StartCoroutine(SplitUIOn());
                }
            }
        }
    }

    public void MonitorReachDebate()
    {
        if (monitor != null)
        {        
            if(monitor.transform.position == new Vector3(16, 0, 1)) // Audio Clip 8
            {
                Debug.Log("Debate Info On!");
            }
        }
    }

    // LikeScale可以在这里
    public void MonitorLeaveDebate() 
    {
        if (monitor != null)
        {   
            if(monitor.transform.position == new Vector3(22, 0, 1)) // Audio Clip 11
            {
                Debug.Log("Debate Info On!");
            }
        }
    }

    public IEnumerator SplitUIOn()
    {
        while (!SplitOn)
        {
            sliderCanvas1.SetActive(true);
            partnerCanvas1.SetActive(false);
            partnerCanvas2.SetActive(false);
            sliderCanvas2.SetActive(false);

            waitingCanvas.enabled = false;
            responseCanvas.enabled = false;

            SplitOn = true;
        }
        yield return null;
    }

    //开启DebateUI协程
    public IEnumerator DebateUIOn()
    {
        //xxxxxxx
 
        yield return null;
    }
    
    //关闭DebateUI协程
    public IEnumerator DebateUIOff()
    {
        // xxxxxx
 
        yield return null;
    }


    public void RefusePartnerFirstSplit()
    {
        sliderCanvas2.SetActive(true);
        partnerCanvas1.SetActive(false);
    }

    public void RefusePartnerSecondSplit()
    {
        partnerCanvas2.SetActive(false);
        sliderCanvas2.SetActive(false);
    }

    IEnumerator ConfirmFirstSplit()
    {
        if (waitingCanvas != null)
        {
            sliderCanvas1.SetActive(false);
            waitingCanvas.enabled = true;
            yield return new WaitForSeconds(waitScreenTime);
            waitingCanvas.enabled = false;
            partnerCanvas1.SetActive(true);
        }
    }

    IEnumerator ConfirmPartnerFirstSplit()
    {
        if (waitingCanvas != null)
        {
            partnerCanvas1.SetActive(false);
            waitingCanvas.enabled = true;
            yield return new WaitForSeconds(waitScreenTime);
            waitingCanvas.enabled = false;
            
            responseCanvas.enabled = true;
       

            ResponseToReject1();
            yield return new WaitForSeconds(responseScreenTime);
            sliderCanvas2.SetActive(true);
            responseCanvas.enabled = false;
        }
    }

    IEnumerator ConfirmSecondSplit()
    {
        if (waitingCanvas != null)
        {
            sliderCanvas2.SetActive(false);
            waitingCanvas.enabled = true;
            yield return new WaitForSeconds(waitScreenTime);
            waitingCanvas.enabled = false;
            partnerCanvas2.SetActive(true);
        }
    }

    IEnumerator ConfirmPartnerSecondSplit()
    {
        if (waitingCanvas != null)
        {
            partnerCanvas2.SetActive(false);
            waitingCanvas.enabled = true;
            yield return new WaitForSeconds(waitScreenTime);
            waitingCanvas.enabled = false;
     
            ResponseToReject2();
            responseCanvas.enabled = true;
            yield return new WaitForSeconds(responseScreenTime);
            responseCanvas.enabled = false;
        }
    }

    IEnumerator RejectPartnerFirstSplit()
    {
        if (waitingCanvas != null)
        {
            partnerCanvas1.SetActive(false);
            waitingCanvas.enabled = true;
            yield return new WaitForSeconds(waitScreenTime);
            waitingCanvas.enabled = false;
            responseCanvas.enabled = true;
    
            ResponseToReject1();
             
            yield return new WaitForSeconds(responseScreenTime);
            sliderCanvas2.SetActive(true);
            responseCanvas.enabled = false;
        }
    }
    IEnumerator RejectPartnerSecondSplit()
    {
        if (waitingCanvas != null)
        {
            partnerCanvas2.SetActive(false);
            waitingCanvas.enabled = true;
            yield return new WaitForSeconds(waitScreenTime);
            waitingCanvas.enabled = false;
           
            ResponseToReject2();
            responseCanvas.enabled = true;
            yield return new WaitForSeconds(responseScreenTime);
            responseCanvas.enabled = false;
        }
    }

    void ResponseToReject1()
    {
        if (selectedVal1 >= 80)
        {
            partnerResponse.text = "rejects";
        }
        else
        {
            partnerResponse.text = "accepts";
        }         
    }

    void ResponseToReject2()
    {
        if (selectedVal2 >= 80)
        {
            partnerResponse.text = "rejects";
        }
        else
        {
            partnerResponse.text = "accepts";
        }   
    }
    */

    // Original Working
    /*
    void Start()
    {
        customThumbstick.action.performed += ThumbStickMoved;
        customThumbstick.action.canceled += ThumbStickReleased;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ThumbStickMoved(InputAction.CallbackContext context)
    {
        rawStickVector = context.ReadValue<Vector2>();
        Debug.Log("extractedVector:" + rawStickVector);
    }

    void ThumbStickReleased(InputAction.CallbackContext context)
    {
        rawStickVector = Vector2.zero; // Reset when released
        Debug.Log("Thumbstick released, reset vector.");
    }
    */

    /*
    void Update()
    {
     
        // Determine direction (right = 1, left = -1)
        if (rawStickVector.x > 0.1f)
        {
            direction = 1;  
        }
        else if (rawStickVector.x < -0.1f)
        {
            direction = -1;
        }
        else
        {
            direction = 0;
        }
        if (Mathf.Abs(rawStickVector.x) > 0.1f)
        {
            stickHoldDuration += Time.deltaTime;    // Total hold time
            intervalTimer += Time.deltaTime;        // Timer for each interval

            // Every 0.2 seconds, increase the slider
            if (intervalTimer >= 0.2f)
            {
                intervalTimer = 0f;  // Reset interval timer

                // Accelerate increase based on hold duration, but cap at maxIncrement
                int increment = Mathf.FloorToInt(1f + stickHoldDuration * 2f);
                increment = Mathf.Min(increment, maxIncrement);  // Cap the increase

                // Update slider value based on direction
                FindObjectOfType<SplitUI>().ChangeSliderValue(increment * direction);
                Debug.Log("pubIncrement:" + increment * direction);
                Debug.Log("stickHoldDuration:" + stickHoldDuration);
            }
        }

        else
        {
            rawStickVector = Vector2.zero;
            stickHoldDuration = 0f;
            intervalTimer = 0f;
            direction = 0;
        }
    }
    */


//}



using System.Collections;  
using System.Collections.Generic;  
using UnityEngine;  
using UnityEngine.InputSystem;  
using TMPro;  
using UnityEngine.UI;  
using System.IO;
using System.Threading;

public class UIPlayerInput : MonoBehaviour  
{  
    [Header("Button Action")]  
    public InputActionReference pressA_Action;  
    public InputActionReference pressX_Action;  

    [Header("GameObjects")]  
    public GameObject sliderCanvas1;  
    public GameObject sliderCanvas2;  
    public GameObject partnerCanvas1;  
    public GameObject partnerCanvas2;  
    public GameObject finalCanvas;

    [Header("Debate Canvases")]  
    public GameObject debateCanvas1;  
    public GameObject debateCanvas2;  

    [Header("Question Transition Canvases")]  
    public GameObject question1Transition;  
    public GameObject question2Transition;  

    [Header("Information Canvases")]
    public GameObject infoCanvas1;
    public GameObject infoCanvas2;

    [Header("Standpoint Objects")]
    public GameObject standpointObject1;
    public GameObject standpointObject2;

    [Header("Debate Saved Data")]  
    public int debateVal1;  
    public int debateVal2;  

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

    private string baseFolderPath;  
    private string filePath;  

    public GameObject monitor;  
    public bool SplitOn = false;  
    private bool debateScoringOn = false;  
    private bool debateInfoOn = false;
    private bool standpointInfoOn = false;

    void Start()  
    {  
        customThumbstick.action.started += ThumbStickStarted;  
        customThumbstick.action.performed += ThumbStickMoved;  
        customThumbstick.action.canceled += ThumbStickReleased;  

        sliderCanvas1.SetActive(false);  
        partnerCanvas1.SetActive(false);  
        partnerCanvas2.SetActive(false);  
        sliderCanvas2.SetActive(false);  
        finalCanvas.SetActive(false);

        // Initialize Debate Canvases  
        debateCanvas1.SetActive(false);  
        debateCanvas2.SetActive(false);  

        // Initialize Info Canvases
        infoCanvas1.SetActive(false);
        infoCanvas2.SetActive(false);

        question1Transition.SetActive(false);
        question2Transition.SetActive(false);

        waitingCanvas.enabled = false;  
        responseCanvas.enabled = false;  

        // CSV File Setup  
        baseFolderPath = Application.persistentDataPath;  
        string fileName = "ParticipantData_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";  
        filePath = Path.Combine(baseFolderPath, fileName);  

        WriteCSV("R1 Split,R2 Split,Decision_1,Decision_2,Debate1,Debate2");  
    }  

    void WriteCSV(string line)  
    {  
        using (StreamWriter writer = new StreamWriter(filePath, true))  
        {  
            writer.WriteLine(line);  
        }  
    }  

    void SaveData()  
    {  
        string newLine = selectedVal1 + "," + selectedVal2 + "," + firstDecision + "," + secondDecision + "," + debateVal1 + "," + debateVal2;  
        WriteCSV(newLine);  
        Debug.Log("Saved To " + filePath);  
    }  

    void ThumbStickStarted(InputAction.CallbackContext context)  
    {  
        stickHoldDuration = 0f;  
        intervalTimer = 0f;  
        rawStickVector = context.ReadValue<Vector2>();  

        direction = rawStickVector.x > 0.1f ? 1 : rawStickVector.x < -0.1f ? -1 : 0;  
    }  

    void ThumbStickMoved(InputAction.CallbackContext context)  
    {  
        rawStickVector = context.ReadValue<Vector2>();  

        if (Mathf.Abs(rawStickVector.x) > 0.1f)  
        {  
            stickHoldDuration += Time.deltaTime;  
            intervalTimer += Time.deltaTime;  

            if (intervalTimer >= singleTimeCheck)  
            {  
                intervalTimer = 0f;  
                int increment = Mathf.FloorToInt(1f + stickHoldDuration * 2f);  
                increment = Mathf.Min(increment, maxIncrement);  

                var splitUI = FindObjectOfType<SplitUI>();  
                var debateUI = FindObjectOfType<DebateUI>();  

                splitUI?.ChangeSliderValue(increment * direction);  
                debateUI?.ChangeSliderValue(increment * direction);  
            }  
        }  
    }  

    void ThumbStickReleased(InputAction.CallbackContext context)  
    {  
        rawStickVector = Vector2.zero;  
        stickHoldDuration = 0f;  
        intervalTimer = 0f;  
        direction = 0;  
    }  

    void Update()  
    {  
        FindMonitor();  

        if (pressA_Action.action.WasPressedThisFrame())  
        {  
            if (debateCanvas1.activeSelf)  
            {  
                debateVal1 = FindObjectOfType<DebateUI>().playerValue;  
                Debug.Log("Debate score 1 selected: " + debateVal1);  
                TransitionToSecondDebate();  
            }  
            else if (debateCanvas2.activeSelf)  
            {  
                debateVal2 = FindObjectOfType<DebateUI>().playerValue;  
                Debug.Log("Debate score 2 selected: " + debateVal2);  
                // SaveData(); // Save debate data upon completion  
                ExitFromSecondDebate();   
            }  
            else if (sliderCanvas1.activeSelf)  
            {  
                selectedVal1 = FindObjectOfType<SplitUI>().playerValue;  
                Debug.Log("I selected " + selectedVal1);  
                StartCoroutine(ConfirmFirstSplit());  
            }  
            else if (partnerCanvas1.activeSelf)  
            {  
                firstDecision = true;  
                StartCoroutine(ConfirmPartnerFirstSplit());  
            }  
            else if (sliderCanvas2.activeSelf)  
            {  
                selectedVal2 = FindObjectOfType<SplitUI>().playerValue;  
                Debug.Log("Then I selected " + selectedVal2);  
                StartCoroutine(ConfirmSecondSplit());  
            }  
            else if (partnerCanvas2.activeSelf)  
            {  
                secondDecision = true;  
                StartCoroutine(ConfirmPartnerSecondSplit());  
                SaveData(); // Save final decision data  
            }  
        }  

        if (pressX_Action.action.WasPressedThisFrame())  
        {  
            if (partnerCanvas1.activeSelf)  
            {  
                firstDecision = false;  
                StartCoroutine(RejectPartnerFirstSplit());  
            }  
            else if (partnerCanvas2.activeSelf)  
            {  
                secondDecision = false;  
                StartCoroutine(RejectPartnerSecondSplit());  
                SaveData();  
            }  
        }  
    }  

    public void FindMonitor()  
    {  
        if (monitor == null)  
        {  
            monitor = GameObject.Find("VR Player Complete Prefab 2(Clone)");  
        }  

        // Check positions for triggering events  
        MonitorReachDebate(); 
        MonitorReachStandpoint(); 
        MonitorLeaveDebate();  
        MonitorReachMoneySplit();  
    }  

    public void MonitorReachMoneySplit()  
    {  
        if (monitor != null && monitor.transform.position == new Vector3(26, 0, 1))  
        {  
            Debug.Log("Money Split On!");  
            if (!SplitOn)  
            {  
                StartCoroutine(SplitUIOn());  
            }  
        }  
    }  

    public void MonitorReachDebate()  
    {  
        if (monitor != null && monitor.transform.position == new Vector3(16, 0, 1)) // Audio Clip 8  
        {  
            Debug.Log("Debate Info On!");  
            if (!debateInfoOn)
            {
                StartCoroutine(InfoUIOn());
            }
        }  
    }  

    public void MonitorReachStandpoint()
    {
        if (monitor != null && monitor.transform.position == new Vector3(18, 0, 1)) // Audio Clip 9
        {
            Debug.Log("Standpoint Info On!");
            if (!standpointInfoOn)
            {
                StartCoroutine(StandpointUIOn());
            }
        }
    }

    public void MonitorLeaveDebate()  
    {  
        if (monitor != null && monitor.transform.position == new Vector3(22, 0, 1)) // Audio Clip 11  
        {  
            Debug.Log("Debate ended, showing scoring interface!");  
            if (!debateScoringOn)  
            {  
                StartCoroutine(DebateUIOn());  
            }  
        }  
    }  

    public IEnumerator SplitUIOn()  
    {  
        while (!SplitOn)  
        {  
            sliderCanvas1.SetActive(true);  
            partnerCanvas1.SetActive(false);  
            partnerCanvas2.SetActive(false);  
            sliderCanvas2.SetActive(false);  

            waitingCanvas.enabled = false;  
            responseCanvas.enabled = false;  

            SplitOn = true;  
        }  
        yield return null;  
    }  

    public IEnumerator InfoUIOn()
    {
        while (!debateInfoOn)
        {
            infoCanvas1.SetActive(true);
            infoCanvas2.SetActive(true);
            standpointObject1.SetActive(false);
            standpointObject2.SetActive(false);
            debateInfoOn = true;
        }
        yield return null;
    }

    public IEnumerator StandpointUIOn()
    {
        while (!standpointInfoOn)
        {
            infoCanvas1.SetActive(true);
            infoCanvas2.SetActive(true);
            standpointObject1.SetActive(true);
            standpointObject2.SetActive(true);
            standpointInfoOn = true;
        }
        yield return null;
    }

    public IEnumerator DebateUIOn()  
    {  
        while (!debateScoringOn)  
        {
            StartCoroutine(QuestionTransition(question1Transition, debateCanvas1));
            // debateCanvas1.SetActive(true);  
            debateCanvas2.SetActive(false);  
            debateScoringOn = true;  
        }  
        yield return null;  
    }  

    public void ExitFromSecondDebate()
    {
        debateCanvas2.SetActive(false);
    }

    public void TransitionToSecondDebate()  
    {  
        StartCoroutine(QuestionTransition(question2Transition, debateCanvas2));
        debateCanvas1.SetActive(false);  
        //debateCanvas2.SetActive(true);  
    }  

    public IEnumerator ConfirmFirstSplit()  
    {  
        if (waitingCanvas != null)  
        {  
            sliderCanvas1.SetActive(false);  
            waitingCanvas.enabled = true;  
            yield return new WaitForSeconds(waitScreenTime);  
            waitingCanvas.enabled = false;  
            partnerCanvas1.SetActive(true);  
        }  
    }  

    public IEnumerator ConfirmPartnerFirstSplit()  
    {  
        if (waitingCanvas != null)  
        {  
            partnerCanvas1.SetActive(false);  
            waitingCanvas.enabled = true;  
            yield return new WaitForSeconds(waitScreenTime);  
            waitingCanvas.enabled = false;  
            responseCanvas.enabled = true;  
            ResponseToReject1();  
            yield return new WaitForSeconds(responseScreenTime);  
            sliderCanvas2.SetActive(true);  
            responseCanvas.enabled = false;  
        }  
    }  

    public IEnumerator ConfirmSecondSplit()  
    {  
        if (waitingCanvas != null)  
        {  
            sliderCanvas2.SetActive(false);  
            waitingCanvas.enabled = true;  
            yield return new WaitForSeconds(waitScreenTime);  
            waitingCanvas.enabled = false;  
            partnerCanvas2.SetActive(true);  
        }  
    }  

    public IEnumerator ConfirmPartnerSecondSplit()  
    {  
        if (waitingCanvas != null)  
        {  
            partnerCanvas2.SetActive(false);  
            finalCanvas.SetActive(true);
            //waitingCanvas.enabled = true;  
            //yield return new WaitForSeconds(waitScreenTime);  
            //waitingCanvas.enabled = false;  
            //responseCanvas.enabled = true;  
            //ResponseToReject2();  
            //yield return new WaitForSeconds(responseScreenTime);  
            //responseCanvas.enabled = false;  
        }  
        yield return null;
    }  

    public IEnumerator RejectPartnerFirstSplit()  
    {  
        if (waitingCanvas != null)  
        {  
            partnerCanvas1.SetActive(false);  
            waitingCanvas.enabled = true;  
            yield return new WaitForSeconds(waitScreenTime);  
            waitingCanvas.enabled = false;  
            responseCanvas.enabled = true;  
            ResponseToReject1();  
            yield return new WaitForSeconds(responseScreenTime);  
            sliderCanvas2.SetActive(true);  
            responseCanvas.enabled = false;  
        }  
    }  

    public IEnumerator RejectPartnerSecondSplit()  
    {  
        if (waitingCanvas != null)  
        {  
            partnerCanvas2.SetActive(false);  
            finalCanvas.SetActive(true);
            //waitingCanvas.enabled = true;  
            //yield return new WaitForSeconds(waitScreenTime);  
            //waitingCanvas.enabled = false;  
            //responseCanvas.enabled = true;  
            //ResponseToReject2();  
            //yield return new WaitForSeconds(responseScreenTime);  
            //responseCanvas.enabled = false;  
        }  
        yield return null;
    }  

    public IEnumerator QuestionTransition(GameObject TransitionScreen, GameObject DebateJudges)  
    {  
        if (waitingCanvas != null)  
        {  
            TransitionScreen.SetActive(true);  
        }  
        yield return new WaitForSeconds(questionTransitionTime);
        TransitionScreen.SetActive(false); 
        DebateJudges.SetActive(true);
    }  

    void ResponseToReject1()  
    {  
        partnerResponse.text = selectedVal1 >= 80 ? "rejects" : "accepts";  
    }  

    void ResponseToReject2()  
    {  
        partnerResponse.text = selectedVal2 >= 80 ? "rejects" : "accepts";  
    }  
}  
