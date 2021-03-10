using BepInEx.Logging;
namespace ImmersivePortals
{
    public static class DebugUtil
    {
        private static void Log(string message, LogLevel level = LogLevel.None, params object[] args)
        {
            message = string.Format(message, args);
            ImmersivePortals.context.log.Log(level, message);
            if (ImmersivePortals.enableNotifications.Value && MessageHud.instance != null) {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, message);
            }
        }
        public static void Log(string message, params object[] args) => Log(message, LogLevel.None, args);
        public static void LogWarning(string message, params object[] args) => Log(message, LogLevel.Warning, args);
        public static void LogDebug(string message, params object[] args) => Log(message, LogLevel.Debug, args);
        public static void LogError(string message, params object[] args) => Log(message, LogLevel.Error, args);
        public static void LogInfo(string message, params object[] args) => Log(message, LogLevel.Info, args);
        public static void LogMessage(string message, params object[] args) => Log(message, LogLevel.Message, args);
        public static void LogFatal(string message, params object[] args) => Log(message, LogLevel.Fatal, args);
    }
}
