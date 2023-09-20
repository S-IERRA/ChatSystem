namespace ChatSystem.Logic.Constants.Configs;

public class SmtpSettings
{
    public string SmptServer { get; set; }
    public int SmptPort { get; set; }
    
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public string SenderPassword { get; set; }
}