using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.ViewModels
{
    public class EventViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event type is required")]
        public string EventType { get; set; } = string.Empty;
    }
}