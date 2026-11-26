using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Burst.Intrinsics;

public class SplitUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI _playerVal;
    [SerializeField] private TextMeshProUGUI _leftVal;

    public int playerValue;

    public GameObject sliderCanvas1;
    public GameObject sliderCanvas2;
    public GameObject partnerCanvas1;
    public GameObject partnerCanvas2;


    public UIPlayerInput UIPlayerInput;

    void Start()
    {
        // Update the displayed values when the slider changes
        slider.onValueChanged.AddListener(UpdateValues);
        UpdateValues(slider.value);  // Initial update

    }

    void Update()
    {
        //UpdateValues(slider.value);
    }

    public void ChangeSliderValue(int amount)
    {
        // Adjust the slider by the specified amount, ensuring it stays within bounds
        slider.value = Mathf.Clamp(slider.value + amount, slider.minValue, slider.maxValue);
        UpdateValues(slider.value);  // Refresh the displayed values
    }

    private void UpdateValues(float sliderValue)
    {
        playerValue = Mathf.RoundToInt(sliderValue);
        _playerVal.text = playerValue.ToString();
        _leftVal.text = (100 - playerValue).ToString();
    }


    //Original Working Slider

    /*
    void Start()
    {
        sliderCanvas1.enabled = true;
        partnerCanvas1.enabled = false;
        partnerCanvas2.enabled = false;
        //sliderCanvas2.enabled = false;

        // Set initial values
        UpdateValues(slider.value);

        // Update values when the slider changes
        slider.onValueChanged.AddListener((sliderValue) => {
            UpdateValues(sliderValue);
        });
    }

    private void UpdateValues(float sliderValue)
    {
        int playerValue = Mathf.RoundToInt(sliderValue);
        _playerVal.text = playerValue.ToString();
        _leftVal.text = (100 - playerValue).ToString();
    }
    */

    /* 
    void Start()
    {
        sliderCanvas1.enabled = true;
        partnerCanvas1.enabled = false;
        partnerCanvas2.enabled = false;

        // Set initial values
        UpdateValues(slider.value);

        // Update values when the slider changes
        slider.onValueChanged.AddListener(UpdateValues);
    }

    void Update()
    {
        if (UIPlayerInput == null)
        {
            UIPlayerInput = FindObjectOfType<UIPlayerInput>();
            if (UIPlayerInput == null) return;
        }

        // Read stick input and apply a dead zone to prevent drift.
        Vector2 stickInput = UIPlayerInput.rawStickVector;
        if (Mathf.Abs(stickInput.x) > 0.1f)  // Dead zone
        {
            float newSliderValue = slider.value + stickInput.x * thumbstickSensitivity * Time.deltaTime * thumbstickSpd;
            slider.value = Mathf.Clamp(newSliderValue, slider.minValue, slider.maxValue);
        }
    }

    private void UpdateValues(float sliderValue)
    {
        int playerValue = Mathf.RoundToInt(sliderValue);
        _playerVal.text = playerValue.ToString();
        _leftVal.text = (100 - playerValue).ToString();
    }
    */
}
