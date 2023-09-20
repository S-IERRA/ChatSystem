namespace ChatSystem.Logic.Http.Email.Models;

public record NewsLetterTemplate
{
    public required string Username { get; set; }
    
    public required string PromotionalContent { get; set; }
    public required string RestUrl { get; set; }
}