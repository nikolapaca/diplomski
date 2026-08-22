namespace FakeTrello.DTO
{
    public class CardImageDTO
    {
        public int Id { get; set; }

        public int CardId { get; set; }

        public string FileName { get; set; }

        public string FilePath { get; set; }

        public string UploadedByUsername { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}
