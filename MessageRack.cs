public class MessageRack
{
    public MessageRack()
    {
        Messages = new List<Message>();
    }

    public List<Message> Messages { get; set; }
    public string? Topic { get; set; }
}