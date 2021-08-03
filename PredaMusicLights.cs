///  Author: Szilágyi Mátyas (Préda)
///  Contact: matyi44418@gmail.com | Préda#2026
///  Copyright: Szilágyi Mátyas

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
//using UnityEngine;
using Color = System.Drawing.Color;

namespace Preda
{
    public class PredaMusicLights
    {
        public struct ColorGradientData
        {
            public ColorGradientData(Color c, float t)
            {
                Color = c;
                time = t;
            }

            public Color Color;
            public float time;
        }

        public struct UpdateData
        {
            public Color color;
            public bool activated;
            public float activationAmount;
            public float lastActivationAmount;
            public float activationBrightness;
            public float expectedActivationDiff;
        }

        //Data
        public float Time;
        public float DeltaTime;
        public float[] AudioData;
        public ColorGradientData[] GradientData;

        //Settings
        private int Samples = 64;

        public Color BaseColor = Color.Black;
        public float DataGatherTime = 0.5f;
        public float LoudAverageMultiplier = 0.8f;
        public float FadeMultiplier = 4;
        public float HueChangeMultiplier = 0.1f;
        public float FadeAddMultiplier = 0.05f;
        public float ActivationCooldown = 0.9f;
        public float ActivationMinLimit = 0.05f;

        //Internal Values
        private List<ValueTuple<float, float>> activationLoudAverage = new List<ValueTuple<float, float>>();
        private float activationMin;
        private float lastActivation;
        private float loudest;
        private float lastLoudIndex;

        private float lastActivationLoudness;
        private float lastActivationTime;
        private float lastActivationDifference;
        private int lastActivationFrame;

        private Color LastColor = Color.Black;
        private float peakIndex = 0;
        private float hue = 0;
        private int frameCounter;

        public void Init(int SampleAmount, ColorGradientData[] Gradient)
        {
            GradientData = Gradient;
            DoInit(SampleAmount);
        }

        public void Init(int SampleAmount)
        {
            GradientData = new ColorGradientData[] {
                new ColorGradientData(Color.FromArgb(255,99,0,255),0),
                new ColorGradientData(Color.FromArgb(255,99,0,255),0.0264744f),
                new ColorGradientData(Color.FromArgb(255,0,12,255),0.06764325f),
                new ColorGradientData(Color.FromArgb(255,238,0,255),0.1705959f),
                new ColorGradientData(Color.FromArgb(255,255,0,0),0.3911803f),
                new ColorGradientData(Color.FromArgb(255,255,230,0),0.5764706f),
                new ColorGradientData(Color.FromArgb(255,0,255,36),0.7323567f),
                new ColorGradientData(Color.FromArgb(255,0,209,255),0.8500038f),
                new ColorGradientData(Color.FromArgb(255,0,209,255),1)
            };
            DoInit(SampleAmount);
        }

        private void DoInit(int SampleAmount)
        {
            Samples = SampleAmount;
        }

        public void SetAudioData(float[] newData)
        {
            AudioData = newData;
        }

        public UpdateData Update(float newTime, float[] AudioData)
        {
            frameCounter++;
            UpdateData toRetrun = new UpdateData();

            DeltaTime = newTime - Time;
            Time = newTime;
            SetAudioData(AudioData);

            hue = (float)Math.Sin((Time * HueChangeMultiplier) + peakIndex) * 255;

            for (int i = 0; i < activationLoudAverage.Count; i++)
            {
                if (Time - activationLoudAverage[i].Item2 > DataGatherTime)
                {
                    activationLoudAverage.RemoveAt(i);
                }
            }

            if (activationLoudAverage.Count > 0)
            {
                //Debug.Log("|||" + activationLoudAverage.Count);

                float averageMin = 0;
                foreach (var f in activationLoudAverage)
                {
                    averageMin += f.Item1;
                }
                averageMin /= activationLoudAverage.Count;

                activationLoudAverage.Sort((x1, x2) => x2.CompareTo(x1));

                float loudest = activationLoudAverage[0].Item1;

                activationMin = (((averageMin + loudest) / 2) * LoudAverageMultiplier) * (1 - (Time - lastActivationTime) * 0.5f);
                //Debug.Log((1 - (Time - lastActivationTime) * 0.5f));
                //min = currentSet[loudAverage.Count - 1].Item1 * LoudAverageMultiplier;
            }
            else
            {
                activationMin = 0;
            }

            UpdateSampleSet();

            //float _loudest = loudest * (ActivationCooldown + (Time - lastActivation));
            float _loudest = loudest;

            //float loudestIndexAvrg = (float)lastLoudIndex / (float)Samples;

            if (_loudest > activationMin && _loudest > ActivationMinLimit)
            {
                RGBToHSV(GetColorGradientData(lastLoudIndex / Samples), out float H, out float S, out float V);
                Color newColor = HSVToRGB(H + hue > 255 ? H + hue - 255 : H + hue, S, V);
                //Color newColor = GetColorGradientData(lastLoudIndex / Samples);

                //Debug.Log("Activated: " + loudestSet + " | " + loudestSetValue + " | " + activationMin);

                //Debug.Log("Loudest " + _loudest + " | Activation Min " + activationMin);

                LastColor = newColor;
                if (lastActivationFrame != frameCounter - 1) lastActivationDifference = Time - lastActivationTime;
                lastActivationFrame = frameCounter;
                //Debug.Log("Diff " + lastActivationDifference + " | Loudest: " + loudestSetValue + " | Multiplier " + (ActivationCooldown + (Time - lastActivation[loudestSet])) + " | Loudest Set: " + loudest[loudestSet] + " | Min: " + activationMin);
                lastActivationTime = Time;
                activationLoudAverage.Add((_loudest, Time));
                toRetrun.lastActivationAmount = lastActivationLoudness;
                lastActivationLoudness = _loudest;

                peakIndex += FadeAddMultiplier * DeltaTime;

                lastActivation = Time;

                toRetrun.color = newColor;
                toRetrun.activated = true;
                toRetrun.activationBrightness = 1;
                toRetrun.activationAmount = loudest;
                toRetrun.expectedActivationDiff = lastActivationDifference * FadeMultiplier;

                return toRetrun;
            }
            else
            {
                //Debug.Log("Loudest " + _loudest + " | Activation Min " + activationMin);
            }

            float lerpValue = (Time - lastActivationTime) / (lastActivationDifference * FadeMultiplier) + 0.05f;

            toRetrun.color = LerpColor(LastColor, BaseColor, lerpValue);
            toRetrun.activated = false;
            toRetrun.activationAmount = 0;
            toRetrun.activationBrightness = 1 - lerpValue;
            toRetrun.expectedActivationDiff = lastActivationDifference * FadeMultiplier;
            return toRetrun;
        }

        private void UpdateSampleSet()
        {


            float Loudest = 0;

            for (int i = 0; i < Samples; i++)
            {
                float current = GetAudioData(i);
                if (current > Loudest)
                {
                    Loudest = current;
                    lastLoudIndex = i;
                }
            }

            loudest = Loudest;
        }

        private void SetGradient(ColorGradientData[] newData)
        {
            GradientData = newData;
        }

        public float GetAudioData(int audioSampleIndex)
        {
            //Return audio data
            return AudioData[audioSampleIndex];
        }

        public Color GetColorGradientData(float time)
        {
            //Return Gradient Color

            if (GradientData.Length == 0)
            {
                return Color.Black;
            }

            ColorGradientData closestData = new ColorGradientData();
            float closestTime = 99;
            int index = 0;

            for (int i = 0; i < GradientData.Length; i++)
            {
                float currentTime = Math.Abs(time - GradientData[i].time);
                if (currentTime < closestTime)
                {
                    closestTime = currentTime;
                    closestData = GradientData[i];
                    index = i;
                }
            }

            if (closestData.time == time)
            {
                return closestData.Color;
            }
            else if (closestData.time > time)
            {
                ColorGradientData colorUnder = GradientData[index - 1];
                return LerpColor(colorUnder.Color, closestData.Color, (time - colorUnder.time) / closestData.time);
            }
            else
            {
                ColorGradientData colorAbove = GradientData[index + 1];
                return LerpColor(closestData.Color, colorAbove.Color, (time - closestData.time) / colorAbove.time);
            }
        }

        public static float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static Color LerpColor(Color s, Color t, float k)
        {
            k = Clamp(k, 0, 1);

            var bk = (1 - k);
            var a = s.A * bk + t.A * k;
            var r = s.R * bk + t.R * k;
            var g = s.G * bk + t.G * k;
            var b = s.B * bk + t.B * k;
            return Color.FromArgb((int)a, (int)r, (int)g, (int)b);
        }

        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static void RGBToHSV(Color color, out float H, out float S, out float V)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            H = color.GetHue();
            S = (max == 0) ? 0 : 1f - (1f * min / max);
            V = max / 255f;
        }

        public static Color HSVToRGB(float _hue, float _saturation, float _value)
        {
            int hi = Convert.ToInt32(Math.Floor(_hue / 60)) % 6;
            double f = _hue / 60 - Math.Floor(_hue / 60);

            _value = _value * 255;
            int v = Convert.ToInt32(_value);
            int p = Convert.ToInt32(_value * (1 - _saturation));
            int q = Convert.ToInt32(_value * (1 - f * _saturation));
            int t = Convert.ToInt32(_value * (1 - (1 - f) * _saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }
    }
}
