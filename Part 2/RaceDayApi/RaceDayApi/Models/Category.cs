namespace RaceDayApi.Models
{
    public class Category
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public int MaxParticipants { get; set; }
        public decimal EntryFee { get; set; }
    }
}
