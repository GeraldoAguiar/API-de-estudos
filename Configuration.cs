namespace Blog;

public static class Configuration
{
    public static string JwtKey = "OCLAOBOobsckjoOihoh79qtGIut87GUKOUADU7";
    public static string ApiKeyName = "api_key";
    public static string ApiKey = "curso_api_MIVNIONnosdnJMO9U80/FE==";
    public static SmtpConfiguration Smtp = new();

    public class SmtpConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; } = 25;
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}