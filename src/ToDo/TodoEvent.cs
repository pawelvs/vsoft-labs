public class TodoEvent
{
    public string EventType { get; set; }
    public TodoItemDTO Todo { get; set; }
    public DateTime Timestamp { get; set; }

    public TodoEvent(string eventType, TodoItemDTO todo)
    {
        EventType = eventType;
        Todo = todo;
        Timestamp = DateTime.UtcNow;
    }
}