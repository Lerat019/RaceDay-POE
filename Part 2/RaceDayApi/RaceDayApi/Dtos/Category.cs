using System.ComponentModel.DataAnnotations;

namespace RaceDayApi.Dtos;

public class CategoryRequest
{
    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99)]
    public decimal Distance { get; set; }

    [Range(1, 100000)]
    public int MaxParticipants { get; set; }

    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
}
