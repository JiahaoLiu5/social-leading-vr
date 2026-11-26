using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Burst.Intrinsics;

public class DebateUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI _playerVal;

    public int playerValue;

    public GameObject debateCanvas1;
    public GameObject debateCanvas2;


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
    }
}