#nullable disable

namespace CountryService.Models
{
    public class FlagPuzzle : Puzzle
    {
        public FlagPuzzle(string question, string answer, string flagPermalink) : base(question, answer)
        {
            FlagPermalink = flagPermalink;
        }

        public string FlagPermalink { get; set; }
    }
}