using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    [Header("Bottom Bar Buttons")]
    public List<RectTransform> buttons;

    [Header("Panels")]
    public List<GameObject> panels;

    [Header("Size Settings")]
    public float selectedSize = 80f;
    public float deselectedSize = 60f;
    public float animationDuration = 0.3f;

    private int currentSelectedIndex = 2;

    private void Start()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            SetButtonSize(buttons[i], deselectedSize, 0f);

            Button btn = buttons[i].GetComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => SwitchButton(index));
        }

        for (int i = 0; i < panels.Count; i++)
        {
            panels[i].SetActive(false);
        }

        if (buttons.Count > 0 && currentSelectedIndex < buttons.Count)
        {
            SetButtonSize(buttons[currentSelectedIndex], selectedSize, 0f);
            if (currentSelectedIndex < panels.Count)
            {
                panels[currentSelectedIndex].SetActive(true);
            }
        }
    }

    public void SwitchButton(int selectedIndex)
    {
        if (selectedIndex == currentSelectedIndex)
            return;

        SetButtonSize(buttons[currentSelectedIndex], deselectedSize, animationDuration);


        if (currentSelectedIndex < panels.Count)
        {
            panels[currentSelectedIndex].SetActive(false);
        }

        SetButtonSize(buttons[selectedIndex], selectedSize, animationDuration);

        if (selectedIndex < panels.Count)
        {
            panels[selectedIndex].SetActive(true);
        }

        currentSelectedIndex = selectedIndex;
    }

    private void SetButtonSize(RectTransform button, float size, float duration)
    {
        if (duration > 0)
        {
            button.DOSizeDelta(new Vector2(size, size), duration).SetEase(Ease.OutBack);
        }
        else
        {
            button.sizeDelta = new Vector2(size, size);
        }
    }
}