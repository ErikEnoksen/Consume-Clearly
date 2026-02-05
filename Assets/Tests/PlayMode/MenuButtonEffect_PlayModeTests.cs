using MainMenu;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Tests.PlayMode
{
    public class MenuButtonEffect_PlayModeTests
    {
        [UnityTest]
        public System.Collections.IEnumerator MenuButtonEffect_DisabledStateRendersCorrectlyInRuntime()
        {
            var buttonObj = new GameObject("Button");
            var button = buttonObj.AddComponent<Button>();
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform);
        
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Default Text";
        
            var menuButtonEffect = textObj.AddComponent<MenuButtonEffect>();
        
            menuButtonEffect.SetEnabled(false);

            yield return null;

            Assert.AreEqual(new Color(0.5f, 0.5f, 0.5f, 1), text.color, "Disabled color wasn't correctly applied in runtime");
            Assert.IsFalse(button.interactable, "Button interactability wasn't disabled in runtime");
        }
    }
}
