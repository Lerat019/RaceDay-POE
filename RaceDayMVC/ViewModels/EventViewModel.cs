using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.ViewModels
{
    public class EventViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Distance is required")]
        public string Distance { get; set; }

        [Required(ErrorMessage = "Event type is required")]
        public string EventType { get; set; }

        public string? BannerImageUrl { get; set; }
    }
}