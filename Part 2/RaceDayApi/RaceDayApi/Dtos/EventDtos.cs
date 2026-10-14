using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Dtos;

public class EventRequest
{
    [Required, StringLength(100)] public string EventName { get; set; } = string.Empty;
    [Required] public DateTime EventDate { get; set; }
    [Required, StringLength(150)] public string Location { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
}