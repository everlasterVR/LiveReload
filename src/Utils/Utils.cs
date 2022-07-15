using UnityEngine;
using UnityEngine.UI;

namespace LiveReload
{
    public static class Utils
    {
        public static void LogError(string message, string name = "") =>
            SuperController.LogError(Format(message, name));

        public static void LogMessage(string message, string name = "") =>
            SuperController.LogMessage(Format(message, name));

        private static string Format(string message, string name) =>
            $"{nameof(LiveReload)} v{Script.VERSION}: {message}{(string.IsNullOrEmpty(name) ? "" : $" [{name}]")}";
    }

    public static class UI
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
            var storable = new JSONStorableString(paramName, initialValue);
            var textField = script.CreateTextField(storable, rightSide);
            textField.UItext.fontSize = fontSize;
            textField.height = height;
            return storable;
        }

        public static InputField NewInputField(UIDynamicTextField textField)
        {
            var inputField = textField.gameObject.AddComponent<InputField>();
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
            var storable = new JSONStorableBool(paramName, startingValue);
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
            var storable = new JSONStorableFloat(paramName, startingValue, minValue, maxValue);
            storable.storeType = JSONStorableParam.StoreType.Physical;
            script.RegisterFloat(storable);
            var slider = script.CreateSlider(storable, rightSide);
            slider.valueFormat = valueFormat;
            return storable;
        }

        public static UIDynamic NewSpacer(this MVRScript script, float height, bool rightSide = false)
        {
            var spacer = script.CreateSpacer(rightSide);
            spacer.height = height;
            return spacer;
        }

        public static string Color(string text, Color color) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}
