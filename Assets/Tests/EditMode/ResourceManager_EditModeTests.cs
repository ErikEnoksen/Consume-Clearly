// filepath: c:\Users\ianko\Consume-Clearly\Assets\Tests\EditMode\ResourceManager_EditModeTests.cs
using System.Reflection;
using NUnit.Framework;
using PlayerResources;
using TMPro;
using System.Globalization;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Logging;

namespace Tests.EditMode
{
    [TestFixture]
    public class ResourceManager_EditModeTests
    {
        private GameObject go;
        private ResourceManager rm;

        [SetUp]
        public void SetUp()
        {
            // Create inactive GameObject so ResourceManager.Update() won't run until we have wired its UI fields
            go = new GameObject("ResourceManagerTest");
            go.SetActive(false);
            rm = go.AddComponent<ResourceManager>();

            // Create a TMP text object for TextMoney and assign
            var textObj = new GameObject("MoneyText");
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            rm.TextMoney = tmp;

            // Provide UI components used by ResourceManager to avoid null refs in AnimateMeter/UpdateRMS
            var sliderCSObj = new GameObject("SliderCS");
            var sliderCSImg = sliderCSObj.AddComponent<UnityEngine.UI.Image>();
            rm.SliderCS = sliderCSImg;

            var sliderRSObj = new GameObject("SliderRS");
            var sliderRSImg = sliderRSObj.AddComponent<UnityEngine.UI.Image>();
            rm.SliderRS = sliderRSImg;

            var sliderMSObj = new GameObject("SliderMS");
            var sliderMSImg = sliderMSObj.AddComponent<UnityEngine.UI.Image>();
            rm.SliderMS = sliderMSImg;

            var shaderObj = new GameObject("AddictiveShader");
            var raw = shaderObj.AddComponent<UnityEngine.UI.RawImage>();
            rm.AddictiveShader = raw;

            // Now that required fields are assigned, activate GameObject so Update/Awake run safely
            go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void UpdateMoney_FormatsText()
        {
            rm.Money = 0f;
            rm.UpdateMoney(12.345f);
            // Make assertion culture-agnostic: parse the text after the euro sign using current or invariant culture
            var text = rm.TextMoney.text;
            Assert.IsTrue(text.StartsWith("\u20ac"), "Money text should start with the euro sign");
            var numPart = text.Substring(1);
            float parsed;
            if (!float.TryParse(numPart, System.Globalization.NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
            {
                // try invariant as fallback
                Assert.IsTrue(float.TryParse(numPart, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out parsed), $"Failed to parse money text '{numPart}' with current and invariant cultures");
            }
            var expectedVal = (float)System.Math.Round(12.345f, 2);
            Assert.AreEqual(expectedVal, parsed);
        }

        [Test]
        public void UpdateRS_WithClampRMS_RespectsRSMax()
        {
            // Prepare MS so that RS_max = STotalLimit - MS = 200 - 50 = 150
            rm.MS = 50f;

            // invoke private UpdateTotalS to compute RS_max and related fields
            typeof(ResourceManager).GetMethod("UpdateTotalS", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(rm, null);

            // ensure clamp mode
            rm.ClampRMS = true;
            rm.RS = 0f;

            // Call UpdateRS with a very large amount which would exceed RS_max
            rm.UpdateRS(190f);

            // RS should be clamped to RS_max (200 - 50 = 150)
            float rs = rm.RS;
            // read private RS_max
            var rsMaxField = typeof(ResourceManager).GetField("RS_max", BindingFlags.NonPublic | BindingFlags.Instance);
            var rsMax = (float)rsMaxField.GetValue(rm);

            Assert.AreEqual(rsMax, rs);
            Assert.AreEqual(150f, rsMax);
        }

        [Test]
        public void ChangeCsOverTime_RespectsCapsAndTimer()
        {
            // Make satisfaction high enough to trigger an increase
            // satisfaction = RS - MS
            rm.RS = 200f;
            rm.MS = 0f;

            // ensure satisfaction/RS_max/etc are recalculated from RS/MS
            typeof(ResourceManager).GetMethod("UpdateTotalS", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rm, null);

            // set private csChangeInterval = 0 and CStimer >= csChangeInterval
            var csChangeIntervalField = typeof(ResourceManager).GetField("csChangeInterval", BindingFlags.NonPublic | BindingFlags.Instance);
            csChangeIntervalField.SetValue(rm, 0);

            var cstimerField = typeof(ResourceManager).GetField("CStimer", BindingFlags.NonPublic | BindingFlags.Instance);
            cstimerField.SetValue(rm, 1f);

            // ensure increase amount is known
            var csIncField = typeof(ResourceManager).GetField("CSIncreaseOverTimeAmount", BindingFlags.Public | BindingFlags.Instance);
            csIncField.SetValue(rm, 7);

            // diagnostics: read back private fields to verify setup
            var csChangeIntervalVal = (int)csChangeIntervalField.GetValue(rm);
            var cstimerVal = (float)cstimerField.GetValue(rm);
            var csIncVal = (int)csIncField.GetValue(rm);
            // ensure satisfaction is recalculated before calling ChangeCsOverTime
            typeof(ResourceManager).GetMethod("UpdateTotalS", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rm, null);
            var satisfactionField = typeof(ResourceManager).GetField("satisfaction", BindingFlags.NonPublic | BindingFlags.Instance);
            var satisfactionVal = (float)satisfactionField.GetValue(rm);

            Debug.Log($"[TestDiag] csChangeInterval={csChangeIntervalVal}, CStimer={cstimerVal}, CSInc={csIncVal}, satisfaction={satisfactionVal}");

            float before = rm.CS;

            // call private ChangeCsOverTime
            typeof(ResourceManager).GetMethod("ChangeCsOverTime", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(rm, null);

            var after = rm.CS;
            Debug.Log($"[TestDiag] CS before={before}, after={after}");

            Assert.AreEqual(before + csIncVal, after);
        }
    }
}
