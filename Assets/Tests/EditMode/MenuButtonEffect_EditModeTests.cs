using MainMenu;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tests.EditMode
{
    [TestFixture]
    public class MenuButtonEffect_EditModeTests
    {
        private GameObject buttonObject;
        private TextMeshProUGUI textComponent;
        private Button button;
        private MenuButtonEffect menuButtonEffect;

        [SetUp]
        public void Setup()
        {
            Debug.Log("Setting up test environment...");
            buttonObject = new GameObject("TestButton");
            button = buttonObject.AddComponent<Button>();
        
            var textObject = new GameObject("ButtonText");
            textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textObject.transform.SetParent(buttonObject.transform);
        
            menuButtonEffect = textObject.AddComponent<MenuButtonEffect>();
        
            var menuButtonEffectType = typeof(MenuButtonEffect);
        
            var textMeshProField = menuButtonEffectType.GetField("textMeshPro", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            textMeshProField.SetValue(menuButtonEffect, textComponent);
        
            var buttonField = menuButtonEffectType.GetField("button", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            buttonField.SetValue(menuButtonEffect, button);
        
            var originalTextField = menuButtonEffectType.GetField("originalText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            textComponent.text = "Test";
            originalTextField.SetValue(menuButtonEffect, textComponent.text);

            Debug.Log($"Setup complete: ButtonObject = {buttonObject.name}, TextComponent = {textComponent.name}");
        }

        [TearDown]
        public void Teardown()
        {
            Debug.Log($"Tearing down test environment for ButtonObject: {buttonObject?.name}");
            Object.DestroyImmediate(buttonObject);
        }

        [Test]
        public void SetEnabled_DisablesButtonWhenFalse()
        {
            Debug.Log("Running SetEnabled_DisablesButtonWhenFalse...");
        
            menuButtonEffect.SetEnabled(false);
            Debug.Log($"Button interactable: {button.interactable}");
            Debug.Log($"Text color: {textComponent.color}");

            Assert.False(button.interactable, "Button should not be interactable when disabled");
            Assert.AreEqual(new Color(0.5f, 0.5f, 0.5f, 1f), textComponent.color, "Button color should change to disabled color when disabled");
        }

        [Test]
        public void SetEnabled_EnablesButtonWhenTrue()
        {
            Debug.Log("Running SetEnabled_EnablesButtonWhenTrue...");

            menuButtonEffect.SetEnabled(true);
            Debug.Log($"Button interactable: {button.interactable}");
            Debug.Log($"Text color: {textComponent.color}");

            Assert.True(button.interactable, "Button should be interactable when enabled");
            Assert.AreEqual(Color.white, textComponent.color, "Button color should be white when enabled");
        }

        [Test]
        public void OnPointerEnter_ChangesTextForEnabledButton()
        {
            Debug.Log("Running OnPointerEnter_ChangesTextForEnabledButton...");
    
            // Enable the button effect
            menuButtonEffect.SetEnabled(true);

            Debug.Log($"Initial text: {textComponent.text}");
    
            // Simulate pointer enter
            menuButtonEffect.OnPointerEnter(new PointerEventData(EventSystem.current));
            Debug.Log($"Text after OnPointerEnter: {textComponent.text}");

            // Assert the updated text matches expectations
            Assert.AreEqual(">Test", textComponent.text, "Text should have '>' prepended when hovered over");
        }

        [Test]
        public void OnPointerEnter_DoesNothingForDisabledButton()
        {
            Debug.Log("Running OnPointerEnter_DoesNothingForDisabledButton...");

            menuButtonEffect.SetEnabled(false);
            textComponent.text = "Test";
            Debug.Log($"Initial text: {textComponent.text}");

            menuButtonEffect.OnPointerEnter(new PointerEventData(EventSystem.current));
            Debug.Log($"Text after OnPointerEnter: {textComponent.text}");

            Assert.AreEqual("Test", textComponent.text, "Text should not change when button is disabled");
        }

        [Test]
        public void OnPointerExit_RevertsText()
        {
            Debug.Log("Running OnPointerExit_RevertsText...");

            menuButtonEffect.SetEnabled(true);
            textComponent.text = ">Test";
            Debug.Log($"Initial text: {textComponent.text}");
        
            menuButtonEffect.OnPointerExit(new PointerEventData(EventSystem.current));
            Debug.Log($"Text after OnPointerExit: {textComponent.text}");

            Assert.AreEqual("Test", textComponent.text, "Text should revert back to original text on pointer exit");
        }
    }
}
