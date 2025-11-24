extern alias MainApp;
using MainApp::AmesaBackend.Models;
using Bogus;

namespace AmesaBackend.Tests.TestHelpers;

/// <summary>
/// Builder pattern for creating test data objects
/// </summary>
public class TestDataBuilder
{
    private static readonly Faker _faker = new();

    public static UserBuilder User() => new UserBuilder();
    public static HouseBuilder House() => new HouseBuilder();
    public static LotteryTicketBuilder LotteryTicket() => new LotteryTicketBuilder();
}

public class UserBuilder
{
    private readonly User _user;
    private static readonly Faker _faker = new();

    public UserBuilder()
    {
        _user = new User
        {
            Id = Guid.NewGuid(),
            Username = _faker.Internet.UserName(),
            Email = _faker.Internet.Email(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!@#"),
            Status = UserStatus.Active,
            AuthProvider = AuthProvider.Email,
            CreatedAt = DateTime.UtcNow,
            EmailVerified = true
        };
    }

    public UserBuilder WithId(Guid id)
    {
        _user.Id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }

    public UserBuilder WithUsername(string username)
    {
        _user.Username = username;
        return this;
    }

    public UserBuilder WithStatus(UserStatus status)
    {
        _user.Status = status;
        return this;
    }

    public UserBuilder WithPassword(string password)
    {
        _user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        return this;
    }

    public User Build() => _user;
}

public class HouseBuilder
{
    private readonly House _house;
    private static readonly Faker _faker = new();

    public HouseBuilder()
    {
        _house = new House
        {
            Id = Guid.NewGuid(),
            Title = _faker.Address.StreetAddress(),
            Description = _faker.Lorem.Paragraph(),
            Price = _faker.Random.Decimal(200000, 1000000),
            Location = _faker.Address.City(),
            Address = _faker.Address.FullAddress(),
            Bedrooms = _faker.Random.Int(2, 5),
            Bathrooms = _faker.Random.Int(1, 4),
            SquareFeet = _faker.Random.Int(1000, 3000),
            PropertyType = "House",
            Status = LotteryStatus.Active,
            TotalTickets = _faker.Random.Int(1000, 5000),
            TicketPrice = _faker.Random.Decimal(10, 100),
            LotteryEndDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };
    }

    public HouseBuilder WithId(Guid id)
    {
        _house.Id = id;
        return this;
    }

    public HouseBuilder WithTitle(string title)
    {
        _house.Title = title;
        return this;
    }

    public HouseBuilder WithPrice(decimal price)
    {
        _house.Price = price;
        return this;
    }

    public HouseBuilder WithStatus(LotteryStatus status)
    {
        _house.Status = status;
        return this;
    }

    public HouseBuilder WithTotalTickets(int totalTickets)
    {
        _house.TotalTickets = totalTickets;
        return this;
    }

    public House Build() => _house;
}

public class LotteryTicketBuilder
{
    private readonly LotteryTicket _ticket;
    private static readonly Faker _faker = new();

    public LotteryTicketBuilder()
    {
        _ticket = new LotteryTicket
        {
            Id = Guid.NewGuid(),
            TicketNumber = $"TICKET-{Guid.NewGuid().ToString()[..8]}",
            Status = TicketStatus.Active,
            PurchaseDate = DateTime.UtcNow,
            PurchasePrice = _faker.Random.Decimal(10, 100),
            CreatedAt = DateTime.UtcNow
        };
    }

    public LotteryTicketBuilder WithId(Guid id)
    {
        _ticket.Id = id;
        return this;
    }

    public LotteryTicketBuilder WithUserId(Guid userId)
    {
        _ticket.UserId = userId;
        return this;
    }

    public LotteryTicketBuilder WithHouseId(Guid houseId)
    {
        _ticket.HouseId = houseId;
        return this;
    }

    public LotteryTicketBuilder WithTicketNumber(string ticketNumber)
    {
        _ticket.TicketNumber = ticketNumber;
        return this;
    }

    public LotteryTicketBuilder WithStatus(TicketStatus status)
    {
        _ticket.Status = status;
        return this;
    }

    public LotteryTicket Build() => _ticket;
}

