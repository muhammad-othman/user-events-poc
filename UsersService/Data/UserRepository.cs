using Microsoft.EntityFrameworkCore;

namespace UsersService.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDBContext context;

        public UserRepository(ApplicationDBContext context)
        {
            this.context = context;
        }
        public async Task<User> Add(User user)
        {
            context.Add<User>(user);
            await context.SaveChangesAsync();
            return user;
        }

        public async Task Delete(int id)
        {

            var userToDelete = await context.Users.FindAsync(id);
            if (userToDelete == null)
                throw new Exception("User not found");

            context.Users.Remove(userToDelete);
            await context.SaveChangesAsync();
        }

        public async Task<List<User>> GetAll()
        {
            return await context.Users.ToListAsync();
        }

        public async Task<User> Update(User user)
        {
            var userToUpdate = await context.Users.FindAsync(user.Id);
            if (userToUpdate == null)
                throw new Exception("User not found");

            userToUpdate.Name = user.Name;
            userToUpdate.Email = user.Email;
            userToUpdate.BirthDate = user.BirthDate;
            userToUpdate.Address = user.Address;

            await context.SaveChangesAsync();

            return userToUpdate;
        }
    }
}
