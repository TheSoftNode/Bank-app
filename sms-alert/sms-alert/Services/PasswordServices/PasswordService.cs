namespace sms_alert.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        return BC.HashPassword(password, BC.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BC.Verify(password, hashedPassword);
    }

    public string GenerateRandomPassword(int length = 12)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+-=";
        var random = new Random();
        var chars = new char[length];

        // Ensure at least one of each required character type
        chars[0] = validChars[random.Next(0, 26)]; // lowercase
        chars[1] = validChars[random.Next(26, 52)]; // uppercase
        chars[2] = validChars[random.Next(52, 62)]; // number
        chars[3] = validChars[random.Next(62, validChars.Length)]; // special char

        // Fill the rest randomly
        for (int i = 4; i < length; i++)
        {
            chars[i] = validChars[random.Next(validChars.Length)];
        }

        // Shuffle the array
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = chars[i];
            chars[i] = chars[j];
            chars[j] = temp;
        }

        return new string(chars);
    }
}