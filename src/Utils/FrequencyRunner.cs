// ReSharper disable UnusedMember.Global
using System;
using UnityEngine;

namespace everlaster
{
    public class FrequencyRunner
    {
        readonly float _frequency;
        float _timeSinceLastCheck;

        public FrequencyRunner(float frequency)
        {
            _frequency = frequency;
        }

        public T Run<T>(Func<T> action)
        {
            _timeSinceLastCheck += Time.unscaledDeltaTime;
            if(_timeSinceLastCheck >= _frequency)
            {
                _timeSinceLastCheck = 0;
                return action();
            }

            return default(T);
        }

        public void Run(Action action)
        {
            _timeSinceLastCheck += Time.unscaledDeltaTime;
            if(_timeSinceLastCheck >= _frequency)
            {
                _timeSinceLastCheck = 0;
                action();
            }
        }
    }
}
