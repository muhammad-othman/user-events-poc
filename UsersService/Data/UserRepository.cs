using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace UsersService.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<UserEvent> userEventsCollection;
        private readonly IModel rabbitmqModel;

        private List<User> usersList = new List<User>();

        public UserRepository()
        {
            var dbClient = new MongoClient("mongodb://127.0.0.1:27017");
            var db = dbClient.GetDatabase("usersEventSourcingDB");

            userEventsCollection = db.GetCollection<UserEvent>("usersEvents");

            var factory = new ConnectionFactory();

            factory.UserName = "guest";
            factory.Password = "guest";
            factory.HostName = "45.63.116.153";
            factory.Port = 5672;

            var connection = factory.CreateConnection();

            rabbitmqModel = connection.CreateModel();
            rabbitmqModel.ExchangeDeclare("usersbus", ExchangeType.Fanout, true);

            ReprocessUserEvents();

        }


        public async Task<User> Add(User user)
        {
            var userEvent = new UserEvent
            {
                Type = EventType.Created,
                UserData = user,
            };
            await SaveAndProcessEvents(userEvent);
            return user;
        }

        public async Task Delete(Guid id)
        {
            var userToDelete = usersList.First(u => u.Id == id);
            var userEvent = new UserEvent
            {
                Type = EventType.Deleted,
                UserData = userToDelete,
            };
            await SaveAndProcessEvents(userEvent);
        }

        public async Task<List<User>> GetAll()
        {
            return usersList;
        }

        public async Task<User> Update(User user)
        {
            var userEvent = new UserEvent
            {
                Type = EventType.Updated,
                UserData = user,
            };
            await SaveAndProcessEvents(userEvent);

            return user;
        }

        private void PublishEvent(UserEvent userEvent) 
        {
            var jsonEvent = JsonSerializer.Serialize(userEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            var body = Encoding.UTF8.GetBytes(jsonEvent);

            rabbitmqModel.BasicPublish("usersbus", "", null, body);
        }

        private async Task SaveAndProcessEvents(UserEvent userEvent)
        {
            await userEventsCollection.InsertOneAsync(userEvent);
            ProcessUserEvent(userEvent);
            PublishEvent(userEvent);
        }

        private void ProcessUserEvent(UserEvent userEvent)
        {
            switch (userEvent.Type)
            {
                case EventType.Created:
                    usersList.Add(userEvent.UserData);
                    break;
                case EventType.Updated:
                    var oldUser = usersList.First(u => u.Id == userEvent.UserData.Id);
                    oldUser.Name = userEvent.UserData.Name;
                    oldUser.Email = userEvent.UserData.Email;
                    oldUser.BirthDate = userEvent.UserData.BirthDate;
                    oldUser.Address = userEvent.UserData.Address;
                    break;
                case EventType.Deleted:
                    usersList.RemoveAll(u => u.Id == userEvent.UserData.Id);
                    break;
            }
        }


        private async Task ReprocessUserEvents()
        {
            var eventsCollection = userEventsCollection.Find(_ => true).SortBy(e => e.CreatedAt);

            var eventsList = await eventsCollection.ToListAsync();
            usersList = new List<User>();

            foreach (var userEvent in eventsList)
            {
                ProcessUserEvent(userEvent);
            }
        }
    }
}
