using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityCommonEx;

/// <summary>
/// Text and selection presentation for reusable string list items.
/// </summary>
public class CommonSelectableListItemUIController : UISelectableListItemController<string>
{
    public TMP_Text NameText;
    public GameObject SelectedPanel;
    public Button InteractiveButton;
    public Material NormalMaterial;
    public Material SelectedMaterial;

    protected override void OnInit()
    {
        base.OnInit();
        if (InteractiveButton != null)
            InteractiveButton.onClick.AddListener(OnInteractiveButtonClicked);
    }

    protected override void OnRelease()
    {
        if (InteractiveButton != null)
            InteractiveButton.onClick.RemoveListener(OnInteractiveButtonClicked);
        base.OnRelease();
    }

    public override void SetItem(string item)
    {
        if (NameText != null)
            NameText.text = item ?? string.Empty;

        ApplyVisualState(isSelected: false);
    }

    public override void Unset()
    {
        if (NameText != null)
        {
            NameText.text = string.Empty;
            NameText.fontSharedMaterial = NormalMaterial;
        }

        if (SelectedPanel != null)
            SelectedPanel.SetActive(false);
    }

    public override void OnSelected()
    {
        base.OnSelected();
        ApplyVisualState(isSelected: true);
    }

    public override void OnDeselected()
    {
        base.OnDeselected();
        ApplyVisualState(isSelected: false);
    }

    void ApplyVisualState(bool isSelected)
    {
        if (SelectedPanel != null)
            SelectedPanel.SetActive(isSelected);

        if (NameText != null)
        {
            NameText.fontSharedMaterial = isSelected && SelectedMaterial != null
                ? SelectedMaterial
                : NormalMaterial;
        }
    }

    private void OnInteractiveButtonClicked()
    {
        OnClick();
    }
}
