namespace ECommerce.Shared.Infrastructure.EventBus;

public record Event
{
    public Event()
    {
        Id = Guid.NewGuid();
        CreateDate = DateTime.UtcNow;
    }

    public Guid Id { get; set; }
    public DateTime CreateDate { get; set; }
}
