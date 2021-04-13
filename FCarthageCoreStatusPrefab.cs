using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
namespace IFSCL.Programs {
    using VirtualWorld;
    public class FCarthageCoreStatusPrefab : FContentPrefab {
        public Animator moverAnimator;
        public Image shieldImage_0;
        public Image shieldImage_1;
        public Sprite blueShield_Sprite;
        public Sprite redShield_Sprite;
        public Image lifeBarTotal;
        public Text lifeBarTotalTextField;
        public Image lifeBarShield_0;
        public Image lifeBarShield_1;
        public Image lifeBarCore;
        public Image shield0_Bar;
        public Image shield1_Bar;
        public Image core_Bar;
        public Image total_Bar;
        public Image shield1_anim;
        public Image shield0_anim;
        public CanvasGroup damageGradients;
        public VideoPlayer video;
        [ReadOnly]
        public VirtualCore linkedCore;

        public Color colorGreen;
        public Color colorRed;
        public void OnInit() {
            video.playbackSpeed = 2;
        }
        void Update() { //TODO 43X: optim
            if (linkedCore == null)
                return;
            if (linkedCore.shield0_Hp == 0) {
                shieldImage_0.overrideSprite = redShield_Sprite;
            } else {
                shieldImage_0.overrideSprite = blueShield_Sprite;
            }
            if (linkedCore.shield1_Hp == 0) {
                shieldImage_1.overrideSprite = redShield_Sprite;
            } else {
                shieldImage_1.overrideSprite = blueShield_Sprite;
            }
            shield0_Bar.GetComponent<RectTransform>().anchoredPosition = new Vector2(Mathf.Lerp(45.8f, -46.2f, linkedCore.shield0_Hp / VirtualCore.shield_HpMAX), 0);
            shield1_Bar.GetComponent<RectTransform>().anchoredPosition = new Vector2(Mathf.Lerp(45.8f, -46.2f, linkedCore.shield1_Hp / VirtualCore.shield_HpMAX), 0);
            core_Bar.GetComponent<RectTransform>().anchoredPosition = new Vector2(Mathf.Lerp(45.8f, -46.2f, linkedCore.core_Hp / VirtualCore.core_HpMAX), 0);

            total_Bar.GetComponent<RectTransform>().anchoredPosition = new Vector2(Mathf.Lerp(63.7f, -63.9f, ((linkedCore.shield0_Hp + linkedCore.shield1_Hp + linkedCore.core_Hp) / 3) / 100), 0);

            lifeBarShield_0.color = Color.Lerp(colorRed, colorGreen, linkedCore.shield0_Hp / VirtualCore.shield_HpMAX);
            lifeBarShield_0.fillAmount = linkedCore.shield0_Hp / VirtualCore.shield_HpMAX;
            lifeBarShield_1.color = Color.Lerp(colorRed, colorGreen, linkedCore.shield1_Hp / VirtualCore.shield_HpMAX);
            lifeBarShield_1.fillAmount = linkedCore.shield1_Hp / VirtualCore.shield_HpMAX;
            lifeBarCore.color = Color.Lerp(colorRed, colorGreen, linkedCore.core_Hp / VirtualCore.core_HpMAX);
            if (Mathf.FloorToInt(linkedCore.shield0_Hp + linkedCore.shield1_Hp + linkedCore.core_Hp) / 3 == 0) {
                lifeBarCore.fillAmount = 0; //fix
            } else {
                lifeBarCore.fillAmount = linkedCore.core_Hp / VirtualCore.core_HpMAX;
            }
            shield0_anim.color = Color.Lerp(colorRed, colorGreen, linkedCore.shield0_Hp / VirtualCore.shield_HpMAX);
            shield1_anim.color = Color.Lerp(colorRed, colorGreen, linkedCore.shield1_Hp / VirtualCore.shield_HpMAX);

            lifeBarTotal.color = Color.Lerp(colorRed, colorGreen, ((linkedCore.shield0_Hp + linkedCore.shield1_Hp + linkedCore.core_Hp) / 3) / 100);
            lifeBarTotal.fillAmount = ((linkedCore.shield0_Hp + linkedCore.shield1_Hp + linkedCore.core_Hp) / 3) / 100;
            lifeBarTotalTextField.text = string.Format("{0:000} %", Mathf.FloorToInt(linkedCore.shield0_Hp + linkedCore.shield1_Hp + linkedCore.core_Hp) / 3);
        }
        //si on change le playbackSpeed on continue dans un update, la vidéo à des freezes  à chaque fois que la boucle se relance
        public void OnPauseEnabled() {
            video.playbackSpeed = 0;
        }
        public void OnPauseDisabled() {
            video.playbackSpeed = 2;
        }
        public float GetTotal() {
            if (!linkedCore) {
                return VirtualCore.shield_HpMAX + VirtualCore.shield_HpMAX + VirtualCore.core_HpMAX;
            } else {
                return linkedCore.shield0_Hp / VirtualCore.shield_HpMAX + linkedCore.shield1_Hp / VirtualCore.shield_HpMAX + linkedCore.core_Hp / VirtualCore.core_HpMAX;
            }
        }
    }
}