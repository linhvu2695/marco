using CountryService.Models;

namespace CountryService.Data
{
    public interface IUserRepo
    {
        bool SaveChanges();

        User? GetUserById(int id);

        User? GetUserByEmail(string email);
        
        void CreateUser(User u);

        bool UserExists(int userId);

        bool EmailExists(string email);
    }
}