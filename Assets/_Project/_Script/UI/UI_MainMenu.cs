using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class UI_MainMenu : MonoBehaviour
{

    [SerializeField] private InspectorScene _playScene;

    [SerializeField] private SO_Sound _music;

    [SerializeField] private UI_FadeEffect _ctnMainMenu;
    [SerializeField] private UI_FadeEffect _ctnCredits;

    [SerializeField] private UI_Button_MainMenu _btnNewGame;
    [SerializeField] private UI_Button_MainMenu _btnCredits;
    [SerializeField] private UI_Button_MainMenu _btnOptions;
    [SerializeField] private UI_Button_MainMenu _btnQuit;

    [SerializeField] private Button _btnCloseCredits;

    private GameObject m_lastButton = null;

    void Awake()
    {
        _music.Play();

        _ctnCredits.HideForced();

        _btnNewGame.Setup(BTN_NewGame, Select(_btnNewGame));
        _btnCredits.Setup(BTN_Credits, Select(_btnCredits));
        _btnOptions.Setup(BTN_Options, Select(_btnOptions));
        _btnQuit.Setup(BTN_Quit, Select(_btnQuit));

        _btnCloseCredits.Setup(BTN_CloseCredits);

        List<Entry> Select(UI_Button_MainMenu button)
        {
            List<Entry> entries = new();

            Entry entrySelect = new() { eventID = EventTriggerType.Select };
            entrySelect.callback.RemoveAllListeners();
            entrySelect.callback.AddListener(_ => SelectButton(button));

            Entry entryPointerEnter = new() { eventID = EventTriggerType.PointerEnter };
            entryPointerEnter.callback.RemoveAllListeners();
            entryPointerEnter.callback.AddListener(_ => SelectButton(button));

            void SelectButton(UI_Button_MainMenu button)
            {
                DeselectAll();
                button.Select();
            }

            entries.Add(entrySelect);
            entries.Add(entryPointerEnter);

            return entries;
        }
    }

    void Start()
    {
        _ctnMainMenu.ShowForced();

        DeselectAll();
        _btnNewGame.Select();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_ctnCredits.IsShowing) BTN_CloseCredits();
        }
    }

    private void DeselectAll()
    {
        _btnNewGame.Deselect();
        _btnCredits.Deselect();
        _btnOptions.Deselect();
        _btnQuit.Deselect();
    }

    private void BTN_NewGame()
    {
        Manager_Events.Sound.SFX.OnSfxPress.Notify();

        Manager_Events.Scene.Transition.Notify(_playScene);
    }

    private void BTN_Credits()
    {
        Manager_Events.Sound.SFX.OnSfxPress.Notify();

        _ctnMainMenu.FadeOut(() =>
        {
            _ctnCredits.FadeIn(() =>
            {
                m_lastButton = EventSystem.current.currentSelectedGameObject;
                _btnCloseCredits.Select();
            });
        });
    }

    private void BTN_Options()
    {
        Manager_Events.Sound.SFX.OnSfxPress.Notify();

        // m_lastButton = EventSystem.current.currentSelectedGameObject;
        // _ctnMainMenu.FadeOut(() => _ctnSettings.Setup(() => _ctnMainMenu.FadeIn(SelectLastButton)));
    }

    private void BTN_CloseCredits()
    {
        Manager_Events.Sound.SFX.OnSfxPress.Notify();

        _ctnCredits.FadeOut(() => _ctnMainMenu.FadeIn(SelectLastButton));
    }

    private void SelectLastButton()
    {
        EventSystem.current.SetSelectedGameObject(m_lastButton);
    }

    private void BTN_Quit()
    {
        Manager_Events.Sound.SFX.OnSfxPress.Notify();
        
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#endif

        Application.Quit();
    }

}
