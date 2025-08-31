using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class UI_Button_MainMenu : MonoBehaviour
{

    [SerializeField] private Button _button;
    public Button Button => _button;

    [SerializeField] private Image _imgSelection;
    [SerializeField] private TextMeshProUGUI _text;

    private bool m_selected = true;

    void Awake()
    {
        Deselect();

        ChangeGlowSize();
    }

    public void Setup(UnityAction callback, List<Entry> entries = null)
    {
        _button.Setup(callback, entries);
    }

    private void ChangeGlowSize()
    {
        _imgSelection.rectTransform.sizeDelta = new(_text.text.Length * 22f, _imgSelection.rectTransform.sizeDelta.y);
    }

    public void Select()
    {
        if (m_selected)
            return;

        m_selected = true;
        _imgSelection.gameObject.SetActive(m_selected);
        _button.Select();
    }

    public void Deselect()
    {
        if (!m_selected)
            return;

        m_selected = false;
        _imgSelection.gameObject.SetActive(m_selected);
    }

}
