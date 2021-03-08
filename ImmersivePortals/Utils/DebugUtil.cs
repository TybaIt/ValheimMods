using BepInEx.Logging;

namespace ImmersivePortals.Utils
{
    public static class DebugUtil
    {
        private static void Log(string message, LogLevel level = LogLevel.None, params string[] args)
        {
            for (int i = 0; i < args.Length; i++) {
                message = message.Replace($"{{{i}}}", args[i]);
            }
            ImmersivePortals.context.log.Log(level, message);
            if (ImmersivePortals.enableNotifications.Value) {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, message);
            }
        }
        public static void Log(string message, params string[] args) => Log(message, LogLevel.None, args);
        public static void LogWarning(string message, params string[] args) => Log(message, LogLevel.Warning, args);
        public static void LogDebug(string message, params string[] args) => Log(message, LogLevel.Debug, args);
        public static void LogError(string message, params string[] args) => Log(message, LogLevel.Error, args);
        public static void LogInfo(string message, params string[] args) => Log(message, LogLevel.Info, args);
        public static void LogMessage(string message, params string[] args) => Log(message, LogLevel.Message, args);
        public static void LogFatal(string message, params string[] args) => Log(message, LogLevel.Fatal, args);
    }
}
