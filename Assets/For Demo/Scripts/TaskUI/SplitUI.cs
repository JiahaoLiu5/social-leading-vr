using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Split panel: one slider that divides a total between two labels.</summary>
public class SplitUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI _playerVal;
    [SerializeField] private TextMeshProUGUI _leftVal;

    [Tooltip("Amount that is divided. The second label shows what remains.")]
    [SerializeField] private int total = 100;

    public int playerValue;

    // Not read by this script. Kept so that existing prefab references stay valid.
    public GameObject sliderCanvas1;
    public GameObject sliderCanvas2;
    public GameObject partnerCanvas1;
    public GameObject partnerCanvas2;

    public UIPlayerInput UIPlayerInput;

    private void Start()
    {
        if (slider == null) return;

        // Update the displayed values when the slider changes
        slider.onValueChanged.AddListener(UpdateValues);
        UpdateValues(slider.value);  // Initial update
    }

    private void OnDestroy()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(UpdateValues);
    }

    public void ChangeSliderValue(int amount)
    {
        if (slider == null) return;

        // Adjust the slider by the specified amount, ensuring it stays within bounds
        slider.value = Mathf.Clamp(slider.value + amount, slider.minValue, slider.maxValue);
        UpdateValues(slider.value);  // Refresh the displayed values
    }

    private void UpdateValues(float sliderValue)
    {
        playerValue = Mathf.RoundToInt(sliderValue);
        if (_playerVal != null) _playerVal.text = playerValue.ToString();
        if (_leftVal != null) _leftVal.text = (total - playerValue).ToString();
    }
}
