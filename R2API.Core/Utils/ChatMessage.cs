using RoR2;

namespace R2API.Utils;

public static class ChatMessage
{

    /// <summary>
    /// Send a message to the chat
    /// </summary>
    /// <param name="message"></param>
    public static void Send(string? message)
    {
        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
        {
            baseToken = "{0}",
            paramTokens = new[] { message }
        });
    }

    /// <summary>
    /// Send a message to the chat in the format "messageFrom: message"
    /// </summary>
    /// <param name="message"></param>
    /// <param name="messageFrom"></param>
    public static void Send(string? message, string? messageFrom)
    {
        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
        {
            baseToken = "{0}: {1}",
            paramTokens = new[] { messageFrom, message }
        });
    }

    public static void SendColored(string? message, string? colorHex)
    {
        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
        {
            baseToken = $"<color={colorHex}>{{0}}</color>",
            paramTokens = new[] { message }
        });
    }

    public static void SendColored(string? message, ColorCatalog.ColorIndex color)
    {
        SendColored(message, ColorCatalog.GetColorHexString(color));
    }

    public static void SendColored(string? message, System.Drawing.Color color)
    {
        SendColored(message, ColorToHexString(color));
    }

    public static void SendColored(string? message, UnityEngine.Color color)
    {
        SendColored(message, ColorToHexString(color));
    }

    public static void SendColored(string? message, string? colorHex, string? messageFrom)
    {
        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
        {
            baseToken = $"<color={colorHex}>{{0}}: {{1}}</color>",
            paramTokens = new[] { messageFrom, message }
        });
    }

    public static void SendColored(string? message, ColorCatalog.ColorIndex color, string? messageFrom)
    {
        SendColored(message, ColorCatalog.GetColorHexString(color), messageFrom);
    }

    public static void SendColored(string? message, System.Drawing.Color color, string? messageFrom)
    {
        SendColored(message, ColorToHexString(color), messageFrom);
    }

    public static void SendColored(string? message, UnityEngine.Color color, string? messageFrom)
    {
        SendColored(message, ColorToHexString(color), messageFrom);
    }

    private static string ColorToHexString(System.Drawing.Color c)
    {
        return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
    }

    private static string ColorToHexString(UnityEngine.Color c)
    {
        return "#" + UnityEngine.ColorUtility.ToHtmlStringRGB(c);
    }
}
