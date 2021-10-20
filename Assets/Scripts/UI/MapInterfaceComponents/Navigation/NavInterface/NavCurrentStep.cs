using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MRK.UI.MapInterface {
    public partial class EGRMapInterfaceComponentNavigation {
        partial class NavInterface {
            public class NavCurrentStep : EGRUINestedElement {
                readonly Image m_Sprite;
                readonly TextMeshProUGUI m_Text;

                public NavCurrentStep(RectTransform transform) : base(transform) {
                    m_Sprite = transform.GetElement<Image>("Sprite");
                    m_Text = transform.GetElement<TextMeshProUGUI>("Instruction");
                }

                public void SetInstruction(string text, Sprite sprite) {
                    m_Text.text = text;
                    m_Sprite.sprite = sprite;
                }
            }
        }
    }
}