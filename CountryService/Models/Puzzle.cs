#nullable disable

namespace CountryService.Models
{
    public class Puzzle
    {
        public Puzzle(String question, String answer)
        {
            Question = question;
            Answer = answer;
        }
        
        public String Question { get; set; }

        public String Answer { get; set; }
    }
}