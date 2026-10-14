using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Dtos;

public class ResultRequest
{
    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, 100000)]
    public int? Position { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int? Position { get; set; }
}