using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace everlaster
{
    sealed class Clock
    {
        float _interval;
        float _timePassed;

        public Clock(float interval = 0)
        {
            _interval = interval;
        }

        public void CalculateInterval(string frequencyOption) => CalculateInterval(StringOptionToFrequency(frequencyOption));

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public void CalculateInterval(float value)
        {
            _interval = 1 / (float) Mathf.RoundToInt(value);
            _timePassed = 0;
        }

        public bool AtInterval()
        {
            _timePassed += Time.unscaledDeltaTime;
            if(_timePassed >= _interval)
            {
                _timePassed = 0;
                return true;
            }

            return false;
        }

        static float StringOptionToFrequency(string option)
        {
            switch(option)
            {
                case "10 Hz": return 10;
                case "20 Hz": return 20;
                case "30 Hz": return 30;
                case "45 Hz": return 45;
                case "60 Hz": return 60;
                case "Use Physics Rate": return Mathf.RoundToInt(1 / Time.fixedDeltaTime);
                default: throw new ArgumentException($"Invalid option {option}");
            }
        }
    }
}
