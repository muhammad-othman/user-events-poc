namespace UsersService.Data
{
    public class UserEvent
    {
        public EventType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum EventType
    {
        Created = 0,
        Updated = 1,
        Deleted = 2,
    }
}
