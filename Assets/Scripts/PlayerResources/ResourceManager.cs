// =============================================================================
// ResourceManager.cs — Player Satisfaction & Economy UI Manager
// This is from Previous group we dont use the resourceManager Actively. 
//
// PURPOSE:
//   Tracks and displays three interconnected satisfaction meters and the player's
//   money. All meters decay or change over time and react to player actions.
//
// THE THREE METERS:
//   • RS (Relational Satisfaction) — yellow slider. Increased by social actions
//     (talking, gifting). Decays on a timer.
//   • MS (Material Satisfaction)   — red slider. Increased by buying/consuming.
//     Decays on a timer. When MS is high and RS is low, the addictive shader fires.
//   • CS (Community Spirit)        — blue slider. Rises passively when RS−MS is
//     above sCapIncreaseCS, falls when it's below sCapDecreaseCS.
//
// CLAMP MODE (ClampRMS):
//   When true, RS and MS cannot overlap — each is capped at STotalLimit minus
//   the other. This keeps the combined bar from exceeding 200.
//   When false, increasing one subtracts from the other if the total would overflow.
//
// SATISFACTION (RS − MS):
//   The difference between RS and MS drives Community Spirit over time.
//   High satisfaction (RS >> MS) grows CS; low satisfaction shrinks it.
//
// SLIDER ANIMATION:
//   All meters animate via AnimateMeterRoutine, which lerps the Image fillAmount
//   using one of four easing curves (Cubic, SmoothStep, SuperSoft, Bounce).
//   Each meter stores its coroutine reference so a new value cancels the in-progress animation.
//
// ADDICTIVE SHADER:
//   A fullscreen RawImage overlay becomes visible when MS ≥ 100 AND MS > RS.
//   This is a visual feedback mechanic for overconsumption.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.VersionControl;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace PlayerResources
{
    public class ResourceManager : MonoBehaviour
    {
        private static readonly int ShaderIntensity = Shader.PropertyToID("ShaderIntensity");

        // --- Inspector Fields ---
        [Header("Money Amount")]
        [SerializeField] public TMP_Text TextMoney;
        [Tooltip("Amount of money player has. Range between 0 - 999999")]
        [SerializeField] [Range(0, 999999)] public float Money = 1;

        // When true, RS and MS cap each other so they can't both be at max simultaneously.
        [Header("Clamp Settings")]
        [Tooltip("Can Material and Relational Satisfaction subtract each other when meter is filled or not?")]
        [SerializeField] public bool ClampRMS = true;

        [Header("Relational Satisfaction Settings")]
        [SerializeField] public Image SliderRS; // yellow fill image
        [Tooltip("Relational Satisfaction (yellow). Increases via social actions, decays over time.")]
        [SerializeField] [Range(0f, STotalLimit)] public float RS;
        [Tooltip("Seconds between each RS decay tick.")]
        [SerializeField] [Range(0, 60)] private int RSDecreaseInterval = 5;
        [Tooltip("How much RS loses each decay tick.")]
        [SerializeField] [Range(0, 50)] public int RSDecreaseOverTimeAmount = 5;

        [Header("Material Satisfaction Settings")]
        [SerializeField] public Image SliderMS; // red fill image
        [Tooltip("Material Satisfaction (red). Increases via purchases/consumption, decays over time.")]
        [SerializeField] [Range(0f, STotalLimit)] public float MS;
        [Tooltip("Seconds between each MS decay tick.")]
        [SerializeField] [Range(0, 60)] private int MSDecreaseInterval = 5;
        [Tooltip("How much MS loses each decay tick.")]
        [SerializeField] [Range(0, 50)] public int MSDecreaseOverTimeAmount = 5;

        [Header("Community Spirit Settings")]
        [SerializeField] public Image SliderCS; // blue fill image
        [Tooltip("Community Spirit (blue). Grows when RS >> MS, shrinks when RS << MS.")]
        [SerializeField] [Range(0, 100)] public float CS;
        [Tooltip("Seconds between each CS change tick.")]
        [SerializeField] [Range(0, 60)] private int csChangeInterval = 5;
        [Tooltip("How much CS decreases per tick when satisfaction is too low.")]
        [SerializeField] [Range(0, 25)] public int CSDecreaseOverTimeAmount = 5;
        [Tooltip("How much CS increases per tick when satisfaction is high enough.")]
        [SerializeField] [Range(0, 25)] public int CSIncreaseOverTimeAmount = 5;
        [Tooltip("RS−MS must exceed this to start growing CS over time.")]
        [SerializeField] [Range(0, 200)] private int sCapIncreaseCS = 90;
        [Tooltip("RS−MS must fall below this to start shrinking CS over time.")]
        [SerializeField] [Range(0, 200)] private int sCapDecreaseCS = 0;

        [Header("Slider Easing Settings")]
        [Tooltip("Duration of the meter fill animation in seconds.")]
        [SerializeField] [Range(0.01f, 2f)] float easeTime = 0.25f;
        public enum EasingType { Cubic, SmoothStep, SuperSoft, Bounce }
        [Tooltip("Easing curve applied to meter animations.")]
        [SerializeField] EasingType easingType;

        [Header("Shader Settings")]
        [Tooltip("Fullscreen overlay that fades in when MS is dominant (overconsumption visual).")]
        [SerializeField] public RawImage AddictiveShader;

        // --- Coroutine Handles ---
        // Stored per-meter so a new value update cancels the in-progress animation.
        private Coroutine rsAnimRoutine;
        private Coroutine msAnimRoutine;
        private Coroutine csAnimRoutine;

        // --- Constants & Derived State ---
        private const float STotalLimit = 200f; // max combined value the satisfaction bar can reach
        private float RS_max; // RS cap when clamped: STotalLimit - current MS
        private float MS_max; // MS cap when clamped: STotalLimit - current RS
        private float rms;    // RS + MS (clamped to STotalLimit) — total bar fill when unclipped
        private float satisfaction; // RS - MS — drives Community Spirit direction

        // Per-meter timers for decay/change intervals.
        private float CStimer;
        private float RStimer;
        private float MStimer;

        // --- Update ---
        // Ticks all timers and drives the full satisfaction + CS + money update each frame.
        void Update()
        {
            if (RS > 0f) RStimer += Time.deltaTime;
            if (MS > 0f) MStimer += Time.deltaTime;
            CStimer += Time.deltaTime;

            UpdateRMS();
            UpdateCS();
            DecreaseRSOverTime();
            DecreaseMSOverTime();
            ChangeCsOverTime();

            TextMoney.text = "€" + Money;
        }

        // =====================================================================
        // SATISFACTION LOGIC
        // =====================================================================

        // Converts RS and MS to fill ratios, animates the sliders, and toggles
        // the addictive shader when MS is dominant (≥ 100 and greater than RS).
        private void UpdateRMS()
        {
            float rsFillAmount = RS / STotalLimit;
            float msFillAmount = MS / STotalLimit;

            AnimateMeter(ref rsAnimRoutine, SliderRS, rsFillAmount);
            AnimateMeter(ref msAnimRoutine, SliderMS, msFillAmount);

            // Addictive shader — visible when material satisfaction overwhelms relational.
            bool addictiveActive = MS >= STotalLimit / 2 && MS > RS;
            Color shaderColor = AddictiveShader.color;
            shaderColor.a = addictiveActive ? 1f : 0f;
            AddictiveShader.color = shaderColor;
        }

        // Decays RS by RSDecreaseOverTimeAmount every RSDecreaseInterval seconds.
        private void DecreaseRSOverTime()
        {
            if (RStimer >= RSDecreaseInterval)
            {
                RS -= RSDecreaseOverTimeAmount;
                UpdateTotalS();
                RStimer = 0;
            }
        }

        // Called externally to increase RS (e.g. after a positive social interaction).
        // Clamp mode prevents RS from pushing the combined total over STotalLimit.
        public void UpdateRS(float amount)
        {
            if (RS < RS_max && ClampRMS)
            {
                UpdateTotalS();
                RS = Mathf.Min(RS + amount, RS_max);
            }

            if (ClampRMS == false)
            {
                RS = Mathf.Min(RS + amount, STotalLimit);
                UpdateTotalS();
                if (rms >= STotalLimit)
                {
                    MS = rms - RS; // push down MS so the total stays within bounds
                    UpdateTotalS();
                }
            }
            UpdateRMS();
        }

        // Decays MS by MSDecreaseOverTimeAmount every MSDecreaseInterval seconds.
        private void DecreaseMSOverTime()
        {
            if (MStimer >= MSDecreaseInterval)
            {
                MS -= MSDecreaseOverTimeAmount;
                UpdateTotalS();
                MStimer = 0;
            }
        }

        // Called externally to increase MS (e.g. after a purchase or consumption event).
        public void UpdateMS(float amount)
        {
            if (MS < MS_max && ClampRMS)
            {
                UpdateTotalS();
                MS = Mathf.Min(MS + amount, MS_max);
            }

            if (ClampRMS == false)
            {
                MS = Mathf.Min(MS + amount, STotalLimit);
                UpdateTotalS();
                if (rms >= STotalLimit)
                {
                    RS = rms - MS; // push down RS so the total stays within bounds
                    UpdateTotalS();
                }
            }
            UpdateRMS();
        }

        // Recalculates derived values after any RS or MS change.
        private void UpdateTotalS()
        {
            RS_max = STotalLimit - MS;
            MS_max = STotalLimit - RS;
            rms = Mathf.Clamp(RS + MS, 0, STotalLimit);
            satisfaction = Mathf.Clamp(RS - MS, -STotalLimit, STotalLimit);
        }

        // =====================================================================
        // COMMUNITY SPIRIT LOGIC
        // =====================================================================

        // Animates the CS bar to match the current CS value (0–100 mapped to 0–1 fill).
        private void UpdateCS()
        {
            AnimateMeter(ref csAnimRoutine, SliderCS, CS / 100f);
        }

        // Changes CS over time based on whether satisfaction is above or below the caps.
        // If RS−MS is in the neutral band (sCapDecreaseCS to sCapIncreaseCS), nothing happens.
        private void ChangeCsOverTime()
        {
            if (CStimer < csChangeInterval) return;
            if (satisfaction >= sCapDecreaseCS && satisfaction <= sCapIncreaseCS) return;

            CS += (satisfaction >= sCapIncreaseCS) ? CSIncreaseOverTimeAmount : -CSDecreaseOverTimeAmount;
            CStimer = 0;
            UpdateCS();
        }

        // Called externally to directly add to Community Spirit (e.g. workshop processing reward).
        public void UpdateCommunitySpirit(float amount)
        {
            CS = Mathf.Min(CS + amount, 100f);
            UpdateCS();
        }

        // =====================================================================
        // MONEY LOGIC
        // =====================================================================

        public void UpdateMoney(float amount)
        {
            Money += amount;
            UpdateMoneyText();
        }

        private void UpdateMoneyText()
        {
            TextMoney.text = "€" + Money.ToString("F2");
        }

        // =====================================================================
        // EASING ANIMATION
        // =====================================================================

        // Cancels any in-progress animation for this meter and starts a new one.
        // Storing the Coroutine reference prevents multiple routines running in parallel.
        private void AnimateMeter(ref Coroutine routine, Image image, float target)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(AnimateMeterRoutine(image, target));
        }

        // Lerps the image fillAmount from its current value to target over easeTime seconds,
        // applying the selected easing curve to the interpolation parameter t.
        private IEnumerator AnimateMeterRoutine(Image image, float target)
        {
            float start = image.fillAmount;
            float time = 0f;
            while (time < easeTime)
            {
                time += Time.deltaTime;
                float t = time / easeTime;

                switch (easingType)
                {
                    case EasingType.Cubic:     t = 1f - Mathf.Pow(1 - t, 3f);        break;
                    case EasingType.SmoothStep: t = t * t * (3f - 2f * t);            break;
                    case EasingType.SuperSoft: t = 1f - Mathf.Pow(1 - t, 7f);        break;
                    case EasingType.Bounce:    t = Mathf.Sin(t * Mathf.PI * 0.5f);   break;
                }

                image.fillAmount = Mathf.Lerp(start, target, t);
                yield return null;
            }
            image.fillAmount = target;
        }
    }
}
