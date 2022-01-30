using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LiveReload
{
    internal static class Utils
    {
        public static List<MVRScript> FindPluginsOnAtom(Atom atom)
        {
            var plugins = new List<MVRScript>();
            foreach(var storableId in atom.GetStorableIDs())
            {
                var storable = atom.GetStorableByID(storableId);
                try
                {
                    MVRScript plugin = (MVRScript) storable;
                    plugins.Add(plugin);
                }
                catch(Exception)
                {
                }
            }
            return plugins;
        }

        public static void LogError(string message, string name = "")
        {
            SuperController.LogError(Format(message, name));
        }

        public static void LogMessage(string message, string name = "")
        {
            SuperController.LogMessage(Format(message, name));
        }

        private static string Format(string message, string name)
        {
            return $"{nameof(LiveReload)} v{Script.version}: {message}{(string.IsNullOrEmpty(name) ? "" : $" [{name}]")}";
        }
    }

    internal static class UI
    {
        public static JSONStorableString NewTextField(
            this MVRScript script,
            string paramName,
            string initialValue,
            int fontSize,
            int height = 120,
            bool rightSide = false
        )
        {
            JSONStorableString storable = new JSONStorableString(paramName, initialValue);
            UIDynamicTextField textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        public static InputField NewInputField(UIDynamicTextField textField)
        {
            InputField inputField = textField.gameObject.AddComponent<InputField>();
            inputField.textComponent = textField.UItext;
            inputField.text = textField.text;
            inputField.textComponent.fontSize = textField.UItext.fontSize;

            var layoutElement = inputField.GetComponent<LayoutElement>();
            layoutElement.minHeight = 0f;
            layoutElement.preferredHeight = textField.height;

            return inputField;
        }

        public static JSONStorableBool NewToggle(
            this MVRScript script,
            string paramName,
            bool startingValue,
            bool rightSide = false
        )
        {
            JSONStorableBool storable = new JSONStorableBool(paramName, startingValue);
            script.CreateToggle(storable, rightSide);
            script.RegisterBool(storable);
            return storable;
        }

        public static JSONStorableFloat NewFloatSlider(
            this MVRScript script,
            string paramName,
            float startingValue,
            float minValue,
            float maxValue,
            string valueFormat,
            bool rightSide = false
        )
        {
            JSONStorableFloat storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Physical;
            script.RegisterFloat(storable);
            UIDynamicSlider slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = valueFormat;
            return storable;
        }

        public static UIDynamic NewSpacer(this MVRScript script, float height, bool rightSide = false)
        {
            UIDynamic spacer = script.CreateSpacer(rightSide);
            spacer.height = height;
            return spacer;
        }

        public static string Color(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
        }
    }
}
