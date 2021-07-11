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

        public uint SampleSets = 3;

        //Internal Values
        private List<List<ValueTuple<float, float>>> loudAverage = new List<List<ValueTuple<float, float>>>();
        private List<ValueTuple<float, float>> activationLoudAverage = new List<ValueTuple<float, float>>();
        private float activationMin;
        private int lastSampleSet;
        private List<float> lastActivation = new List<float>();
        private List<float> min = new List<float>();
        private List<float> loudest = new List<float>();
        private List<float> lastLoudest = new List<float>();
        private List<float> lastTime = new List<float>();
        private List<float> lastLoudIndex = new List<float>();

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

            for (int i = 0; i < SampleSets; i++)
            {
                loudAverage.Add(new List<(float, float)>());
                min.Add(0);
                loudest.Add(0);
                lastLoudest.Add(0);
                lastTime.Add(0);
                lastLoudIndex.Add(0);
                lastActivation.Add(0);
            }
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

            hue = Remap((float)Math.Sin((Time * HueChangeMultiplier) + peakIndex), -1, 1, 0, 255);

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

                activationMin = averageMin * LoudAverageMultiplier;
                //min = currentSet[loudAverage.Count - 1].Item1 * LoudAverageMultiplier;
            }

            for (int i = 0; i < SampleSets; i++)
            {
                UpdateSampleSet(i);
            }

            List<int> activeSets = new List<int>();
            for (int i = 0; i < SampleSets; i++)
            {
                if (lastTime[i] == Time)
                {
                    activeSets.Add(i);
                }
            }

            float loudestSetValue = 0;
            int loudestSet = 0;
            bool found = false;
            foreach (int i in activeSets)
            {
                float _loudest = loudest[i] * (ActivationCooldown + (Time - lastActivation[i]));
                if (_loudest > loudestSetValue)
                {
                    //Debug.Log(_loudest + " | " + loudest[i] + " | Time: " + Time + " | Activating: " + activationMin);

                    loudestSetValue = _loudest;
                    loudestSet = i;
                    found = true;
                }
            }

            float loudestIndexAvrg = (float)lastLoudIndex[loudestSet] / (float)Samples;

            if (loudestSetValue > activationMin && found)
            {
                RGBToHSV(GetColorGradientData(loudestIndexAvrg), out float H, out float S, out float V);
                Color newColor = HSVToRGB(H + hue > 255 ? H + hue - 255 : H + hue, S, V);

                //Debug.Log("Activated: " + loudestSet + " | " + loudestSetValue + " | " + activationMin);

                LastColor = newColor;
                if (lastActivationFrame != frameCounter - 1) lastActivationDifference = Time - lastActivationTime;
                lastActivationFrame = frameCounter;
                //Debug.Log("Diff " + lastActivationDifference + " | Loudest: " + loudestSetValue + " | Multiplier " + (ActivationCooldown + (Time - lastActivation[loudestSet])) + " | Loudest Set: " + loudest[loudestSet] + " | Min: " + activationMin);
                lastActivationTime = Time;
                activationLoudAverage.Add((loudestSetValue, Time));
                toRetrun.lastActivationAmount = lastActivationLoudness;
                lastActivationLoudness = loudestSetValue;
                lastSampleSet = loudestSet;

                peakIndex += FadeAddMultiplier * DeltaTime;

                lastActivation[loudestSet] = Time;

                toRetrun.color = newColor;
                toRetrun.activated = true;
                toRetrun.activationBrightness = 1;
                toRetrun.activationAmount = loudest[loudestSet];
                toRetrun.expectedActivationDiff = lastActivationDifference * FadeMultiplier;

                return toRetrun;
            }
            else
            {
                //Debug.Log("Loudest " + loudestSetValue + " | Activation Min " + activationMin);
            }

            float lerpValue = (Time - lastActivationTime) / (lastActivationDifference * FadeMultiplier) + 0.05f;

            toRetrun.color = LerpColor(LastColor, BaseColor, lerpValue);
            toRetrun.activated = false;
            toRetrun.activationAmount = 0;
            toRetrun.activationBrightness = 1 - lerpValue;
            toRetrun.expectedActivationDiff = lastActivationDifference * FadeMultiplier;
            return toRetrun;
        }

        private void UpdateSampleSet(int sampleSet)
        {
            var currentSet = loudAverage[sampleSet];

            for (int i = 0; i < currentSet.Count; i++)
            {
                if (Time - currentSet[i].Item2 > DataGatherTime)
                {
                    currentSet.RemoveAt(i);
                }
            }

            if (currentSet.Count > 0)
            {
                float averageMin = 0;
                foreach (var f in currentSet)
                {
                    averageMin += f.Item1;
                }
                averageMin /= currentSet.Count;

                min[sampleSet] = averageMin * LoudAverageMultiplier;
                //min = currentSet[loudAverage.Count - 1].Item1 * LoudAverageMultiplier;
            }
            else
            {
                currentSet.Add((0, Time));
            }

            int _samples = (int)(Samples / SampleSets);

            int LoudestIndex = 0;
            float Loudest = 0;
            for (int i = _samples * sampleSet; i < _samples * (sampleSet + 1); i++)
            {
                float current = GetAudioData(i);
                if (current > Loudest)
                {
                    Loudest = current;
                    LoudestIndex = i;
                }
            }

            //float loudestIndexAvrg = (float)LoudestIndex / (float)Samples;

            float currentLoudest = Loudest;

            if (currentLoudest > min[sampleSet])
            {
                lastTime[sampleSet] = Time;
                lastLoudest[sampleSet] = loudest[sampleSet];
                loudest[sampleSet] = currentLoudest;

                lastLoudIndex[sampleSet] = LoudestIndex;

                loudAverage[sampleSet].Add((currentLoudest, Time));
            }
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
