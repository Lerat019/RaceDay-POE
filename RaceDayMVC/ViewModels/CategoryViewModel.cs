namespace RaceDayMVC.ViewModels
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Distance { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }
        public decimal EntryFee { get; set; }
    }
}