namespace RaceDayMVC.ViewModels
{
    public class EventDetailsViewModel
    {
        public EventViewModel Event { get; set; } = new EventViewModel();
        public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
        public int SelectedCategoryId { get; set; }
    }
}