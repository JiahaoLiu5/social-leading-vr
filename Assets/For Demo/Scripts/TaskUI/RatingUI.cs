using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Rating panel: one slider and a label with its whole-number value.</summary>
public class RatingUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI _playerVal;

    public int playerValue;

    // Not read by this script. Kept so that existing prefab references stay valid.
    public GameObject ratingCanvas1;
    public GameObject ratingCanvas2;

    public UIPlayerInput UIPlayerInput;

    private void Start()
    {
        if (slider == null) return;

        // Update the displayed value when the slider changes
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
        UpdateValues(slider.value);  // Refresh the displayed value
    }

    private void UpdateValues(float sliderValue)
    {
        playerValue = Mathf.RoundToInt(sliderValue);
        if (_playerVal != null) _playerVal.text = playerValue.ToString();
    }
}
