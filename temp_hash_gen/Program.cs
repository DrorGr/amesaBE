using BCrypt.Net;

var password = "Dro852!Fu";
var hash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
Console.WriteLine($"Password: {password}");
Console.WriteLine($"BCrypt Hash: {hash}");
Console.WriteLine($"Verification: {BCrypt.Net.BCrypt.Verify(password, hash)}");
