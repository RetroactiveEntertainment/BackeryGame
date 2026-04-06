using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Bottom Bar Buttons")]
    public List<Button> buttons;

    [Header("Scale Settings")]
    public float selectedScale = 1.3f;
    public float deselectedScale = 1.0f;

    private int currentSelectedIndex = 2; // Default: middle button (index 2 out of 0-4)

    private void Start()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].transform.localScale = Vector3.one * deselectedScale;
        }

        // Select the middle button by default
        if (buttons.Count > 0 && currentSelectedIndex < buttons.Count)
        {
            buttons[currentSelectedIndex].transform.localScale = Vector3.one * selectedScale;
        }
    }

    /// <summary>
    /// Call this from each Button's OnClick event, passing the button reference.
    /// </summary>
    public void SwitchButton(Button selectedButton)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            float targetScale = (buttons[i] == selectedButton) ? selectedScale : deselectedScale;
            buttons[i].transform.localScale = Vector3.one * targetScale;

            if (buttons[i] == selectedButton)
                currentSelectedIndex = i;
        }
    }
}
