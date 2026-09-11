public static class MessageColoring
{
    public static string MessageWithColor(string message, string color)
    {
        string newMessage = "<" + color + ">" + message + "</color>";
        return newMessage;
    }
}