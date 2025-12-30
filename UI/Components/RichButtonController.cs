using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCommonEx
{

    public class RichButtonController : UIListItemController<RichButtonController.RichButtonData>
    {

        public struct RichButtonData
        {
            public string MainText;
            public Action OnClick;
            public Sprite Icon;
            public string SideText;
            public bool Interactable;
        }

        public TMP_Text MainText;
        public GameObject MainTextPadding;
        public Image IconImage;
        public TMP_Text SideText;
        public Button MainButton;

        Action onClickCallback;

        protected override void OnInit()
        {
            base.OnInit();
            
            // 给 MainButton 绑定 OnClick
            if (MainButton != null)
            {
                MainButton.onClick.AddListener(OnClick);
            }
        }

        protected override void OnRelease()
        {
            if (MainButton != null)
            {
                MainButton.onClick.RemoveAllListeners();
            }
            
            base.OnRelease();
        }

        public override void SetItem(RichButtonData item)
        {
            Set(item.MainText, item.OnClick, item.Icon, item.SideText, item.Interactable);
        }

        public void Set(string mainText, Action onClick, Sprite icon = null, string sideText = null, bool interactable = true)
        {
            MainText.text = mainText;
            onClickCallback = onClick;
            if (MainTextPadding != null)
            {
                MainTextPadding.SetActive(icon != null || sideText != null);
            }
            if (IconImage != null)
            {
                IconImage.gameObject.SetActive(icon != null);
                IconImage.sprite = icon;    
            }
            if (SideText != null)
            {
                SideText.gameObject.SetActive(sideText != null);
                SideText.text = sideText;
            }
            MainButton.interactable = interactable;
        }

        public override void OnClick()
        {
            base.OnClick();
            onClickCallback?.Invoke();
        }

    }

}