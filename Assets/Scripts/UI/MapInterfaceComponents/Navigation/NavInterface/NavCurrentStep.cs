using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface
{
    public partial class Navigation
    {
        public partial class NavInterface
        {
            public class NavCurrentStep : NestedElement
            {
                private readonly Image _sprite;
                private readonly TextMeshProUGUI _text;

                public NavCurrentStep(RectTransform transform) : base(transform)
                {
                    _sprite = transform.GetElement<Image>("Sprite");
                    _text = transform.GetElement<TextMeshProUGUI>("Instruction");
                }

                public void SetInstruction(string text, Sprite sprite)
                {
                    _text.text = text;
                    _sprite.sprite = sprite;
                }
            }
        }
    }
}