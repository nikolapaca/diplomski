using Pivot.Model;
using Pivot.Model.Enum;

namespace Pivot.DTO
{
    public class BoardMemberDTO
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public UserRole Role { get; set; }
    }
}
