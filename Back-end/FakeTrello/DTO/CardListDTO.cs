using System.ComponentModel.DataAnnotations;

namespace FakeTrello.DTO
{
    public class CardListDTO
    {
        public int Id { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "Name must have at least one character")]
        public string Name {  get; set; }
        public string BoardName { get; set; }
        public string BoardUsername { get; set; }
        public List<CardDTO> Cards { get; set; }
        public int Index { get; set; }
        public bool IsPinned { get; set; }
    }
}
