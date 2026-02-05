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

        //This script uses 2 Images, 1 slider and 1 text mesh. 
        [Header("Money Amount")]
        [SerializeField] public TMP_Text TextMoney; //Simple TextMesh
        [Tooltip("Amount of money player has. Range between 0 - 9 99 999")]
        [SerializeField] [Range(0, 999999)] public float Money = 1;
        
        [Header("Clamp Settings")]
        [Tooltip("Can Material and Relational Satisfaction subtract each other when meter is filled or not?")]
        [SerializeField] public bool ClampRMS = true;
        
        [Header("Relational Satisfaction Settings")]
        [SerializeField] public Image SliderRS; //Yellow
        [Tooltip("Relational Satisfaction (yellow).")]
        [SerializeField] [Range(0f, STotalLimit)] public float RS; //Relational Satisfaction (yellow). Value is clamped in the Unity Editor.
        [Tooltip("Seconds it takes for RS to decrease. Interval Range between 0 - 60 seconds.")]
        [SerializeField] [Range(0, 60)] private int RSDecreaseInterval = 5; //Seconds it takes for RS to decrease. Value is clamped in the Unity Editor.
        [Tooltip("Amount of RS that decreases after interval. Decrease Amount range between 0 - 50")]
        [SerializeField] [Range(0, 50)] public int RSDecreaseOverTimeAmount = 5; //Amount of RS that decreases after interval.
        
        [Header("Material Satisfaction Settings")]
        [SerializeField] public Image SliderMS; //Red
        [Tooltip("Material Satisfaction (red).")]
        [SerializeField] [Range(0f, STotalLimit)] public float MS; //Material Satisfaction (red). Value is clamped in the Unity Editor.
        [Tooltip("Seconds it takes for MS to decrease. Interval Range between 0 - 60 seconds.")]
        [SerializeField] [Range(0, 60)] private int MSDecreaseInterval = 5; //Seconds it takes for MS to decrease. Value is clamped in the Unity Editor.
        [Tooltip("Amount of MS that decreases after interval. Decrease Amount range between 0 - 50")]
        [SerializeField] [Range(0, 50)] public int MSDecreaseOverTimeAmount = 5; //Amount of RS that decreases after interval.
        
        [Header("Community Spirit Settings")]
        [Tooltip("Fill Image of Community Spirit Meter")]
        [SerializeField] public Image SliderCS;
        [Tooltip("Community Spirit (blue). Value Range between 0 - 100")]
        [SerializeField] [Range(0, 100)] public float CS;
        [Tooltip("Seconds it takes for CS to increase or decrease. Interval Range between 0 - 60 seconds.")]
        [SerializeField] [Range(0, 60)] private int csChangeInterval = 5; //Seconds it takes for CS to decrease. Value is clamped in the Unity Editor.
        [Tooltip("Amount of CS that decreases after interval. Decrease Amount range between 0 - 25")]
        [SerializeField] [Range(0, 25)] public int CSDecreaseOverTimeAmount = 5; //Amount of CS that decreases after interval. Value is clamped in the Unity Editor.
        [Tooltip("Amount of CS that increases after interval. Decrease Amount range between 0 - 25")]
        [SerializeField] [Range(0, 25)] public int CSIncreaseOverTimeAmount = 5; //Amount of CS that increases after interval. Value is clamped in the Unity Editor.
        [Tooltip("The biggest amount overall satisfaction (RS - MS) can be before increasing Community Spirit over time.")]
        [SerializeField] [Range(0, 200)] private int sCapIncreaseCS = 90; //How much the float "satisfaction" (= RS - MS) can be before increasing CS over time. Value is clamped in the Unity Editor.
        [Tooltip("The smallest amount overall satisfaction (RS - MS) can be before decreasing Community Spirit over time.")]
        [SerializeField] [Range(0, 200)] private int sCapDecreaseCS = 0; //The smallest amount the float "satisfaction" (= RS - MS) can be before decreasing CS over time. Value is clamped in the Unity Editor.
        
        [Header("Slider Easing Settings")]
        [Tooltip("Time for slider anim to finish. Range between 0.01 - 2 seconds.")]
        [SerializeField] [Range(0.01f, 2f)] float easeTime = 0.25f; //Time for slider anim to finish. Value is clamped in the Unity Editor.
        public enum EasingType { Cubic, SmoothStep, SuperSoft, Bounce }
        [Tooltip("Type of animation for Satisfaction Slider")]
        [SerializeField] EasingType easingType;

        [Header("Shader Settings")]
        [Tooltip("The shader that activates when material satisfaction is too high.")]
        [SerializeField] public RawImage AddictiveShader;
        
        private Coroutine rsAnimRoutine;
        private Coroutine msAnimRoutine;
        private Coroutine csAnimRoutine;
        
        private const float STotalLimit = 200f; // "Satisfaction Total Limit" Total value of the satisfaction meter. E.g if RS is at 200 the meter is filled.
        private float RS_max; //"Relational Satisfaction Max" Relevant if Clamp = true. Subtract total satisfaction meter value with current MS so that RS and MS doesn't overlap.
        private float MS_max; //"Material Satisfaction Max" Relevant if Clamp = true. Subtract total satisfaction meter value with current RS so that RS and MS doesn't overlap.
        private float rms; //"Relational Material Satisfaction" RS+MS to calculate the total value filled in the satisfaction meter. Relevant if Clamp = false so that increasing RS subtract MS and vice versa.
        private float satisfaction; //Subtract MS from RS to determine if Community spirit should increase or decrease over time.
        private float CStimer;
        private float RStimer;
        private float MStimer;
        

        void Update()
        {
            if (RS > 0f)
            {
                RStimer += Time.deltaTime;
            }

            if (MS > 0f)
            {
                MStimer += Time.deltaTime;
            }
            
            CStimer += Time.deltaTime;
            
            UpdateRMS();
            UpdateCS();
            DecreaseRSOverTime();
            DecreaseMSOverTime();
            ChangeCsOverTime();
            
            TextMoney.text = "€" + Money;
        }
        
        
        //SATISFACTION LOGIC: START
        private void UpdateRMS() 
        {
            float rsFillAmount = RS / STotalLimit;
            float msFillAmount = MS / STotalLimit;
            
            AnimateMeter(ref rsAnimRoutine, SliderRS, rsFillAmount);
            AnimateMeter(ref msAnimRoutine, SliderMS, msFillAmount);

            if (MS >= STotalLimit / 2 && MS > RS)
            {
                float alpha = 1.0f;
                Color currColor = AddictiveShader.color;
                currColor.a = alpha;
                AddictiveShader.color = currColor;

            }
            else
            {
                float alpha = 0f;
                Color currColor = AddictiveShader.color;
                currColor.a = alpha;
                AddictiveShader.color = currColor;
            }
        }
        
        private void DecreaseRSOverTime()
        {
            if (RStimer >= RSDecreaseInterval)
            {
                RS -= RSDecreaseOverTimeAmount;
                UpdateTotalS();
                RStimer = 0;
            }
        }
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
                    MS = rms - RS;
                    UpdateTotalS();
                }
            }
            UpdateRMS();
        }
        
        private void DecreaseMSOverTime()
        {
            if (MStimer >= MSDecreaseInterval)
            {
                MS -= MSDecreaseOverTimeAmount;
                UpdateTotalS();
                MStimer = 0;
            }
        }
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
                    RS = rms - MS;
                    UpdateTotalS();
                }
            }
            UpdateRMS();
        }

        private void UpdateTotalS()
        {
            RS_max = STotalLimit - MS;
            MS_max = STotalLimit - RS;
            rms = Mathf.Clamp(RS + MS, 0, STotalLimit);
            satisfaction = Mathf.Clamp(RS - MS, -STotalLimit, STotalLimit);
        }
        //SATISFACTION LOGIC: END

        
        //COMMUNITY SPIRIT LOGIC: START
        private void UpdateCS()
        {
            float csFillAmount = CS / 100;
            AnimateMeter(ref csAnimRoutine, SliderCS, csFillAmount);
        }
        
        private void ChangeCsOverTime()
        {
            //Below says "If satisfaction is between 0 to 89 or the time interval for Community Spirit value change (5 sec) hasn't passed, DO NOTHING."
            if (CStimer < csChangeInterval) return;
            if (satisfaction >= sCapDecreaseCS && satisfaction <= sCapIncreaseCS) return;
            //Below says "If satisfaction is 90 or more, increase the value. If it's the other option (-1 or less) decrease the value."
            CS += (satisfaction >= sCapIncreaseCS) ? CSIncreaseOverTimeAmount : -CSDecreaseOverTimeAmount;
            CStimer = 0;
            UpdateCS();
        }
        public void UpdateCommunitySpirit(float amount)
        {
            CS = Mathf.Min(CS + amount, 100f);
            UpdateCS();
        }
        //COMMUNITY SPIRIT LOGIC: END
        
        
        //MONEY LOGIC: START
        public void UpdateMoney(float amount)
        {
            Money += amount;
            UpdateMoneyText();
        }
        private void UpdateMoneyText()
        {
            TextMoney.text = "€" + Money.ToString("F2");
        }
        //MONEY LOGIC: END
        
        
        //EASING ANIMATION LOGIC: START
        private void AnimateMeter(ref Coroutine routine, Image image, float target)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(AnimateMeterRoutine(image, target));
        }

        private IEnumerator AnimateMeterRoutine(Image image, float target)
        {
            float start = image.fillAmount;
            float time = 0f;
            while (time < easeTime)
            {
                time += Time.deltaTime;
                float t = time / easeTime;
                
                //anim style/effect
                switch (easingType)
                {
                    case EasingType.Cubic: t = 1f - Mathf.Pow(1 - t, 3f);  break;
                    case EasingType.SmoothStep: t = t * t * (3f - 2f * t);  break;
                    case EasingType.SuperSoft: t = 1f - Mathf.Pow(1 - t, 7f); break;
                    case EasingType.Bounce: t = Mathf.Sin(t * Mathf.PI * 0.5f);  break;
                }
                
                image.fillAmount = Mathf.Lerp(start, target, t);
                yield return null;
            }
            image.fillAmount = target;
        }
    }
}
