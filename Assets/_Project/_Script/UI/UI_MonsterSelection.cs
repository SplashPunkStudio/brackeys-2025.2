using UnityEngine;
using UnityEngine.UI;

public class UI_MonsterSelection : MonoBehaviour
{

    [SerializeField] private InspectorScene _mainMenuScene;
    [SerializeField] private InspectorScene _playScene;

    [SerializeField] private Sprite _spriteParty1;
    [SerializeField] private Sprite _spriteParty2;

    [SerializeField] private Image _imgP1;
    [SerializeField] private Image _imgP2;

    [SerializeField] private Button _btnBack;
    [SerializeField] private Button _btnSwitch;
    [SerializeField] private Button _btnConfirm;

    private bool m_p1SpriteParty1 = true;

    void Awake()
    {
        m_p1SpriteParty1 = true;

        _btnBack.Setup(BTN_Back);
        _btnSwitch.Setup(BTN_Switch);
        _btnConfirm.Setup(BTN_Confirm);
    }

    private void BTN_Back()
    {
        Manager_Events.Sound.SFX.OnSfxPress.Notify();

        Manager_Events.Scene.Transition.Notify(_mainMenuScene);
    }

    private void BTN_Switch()
    {
        m_p1SpriteParty1 = !m_p1SpriteParty1;

        _imgP1.sprite = m_p1SpriteParty1 ? _spriteParty1 : _spriteParty2;
        _imgP2.sprite = m_p1SpriteParty1 ? _spriteParty2 : _spriteParty1;

        Manager_Events.Sound.SFX.OnSfxPress.Notify();
    }

    private void BTN_Confirm()
    {
        Manager_Events.Sound.SFX.OnSfxPress.Notify();
        
        InstanceInfo.SetPlayer1Party1(m_p1SpriteParty1);

        Manager_Events.Scene.TransitionWithLoading.Notify(_playScene);
    }

}
