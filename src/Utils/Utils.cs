using System.ComponentModel;

namespace LiveReload
{
    public static class Utils
    {
        public static void LogError(string message, string name = "") =>
            SuperController.LogError(Format(message, name));

        public static void LogMessage(string message, string name = "") =>
            SuperController.LogMessage(Format(message, name));

        private static string Format(string message, string name) =>
            $"{nameof(LiveReload)} {Script.VERSION}: {message}{(string.IsNullOrEmpty(name) ? "" : $" [{name}]")}";

        // ReSharper disable once UnusedMember.Global
        public static string ObjectPropertiesString(object obj)
        {
            string result = "";
            foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                result += $"{descriptor.Name} = {descriptor.GetValue(obj)}\n";
            }

            return result;
        }
    }
}
