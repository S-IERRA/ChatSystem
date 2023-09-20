namespace ChatSystem.Logic.Email.Email.Models;

public record RequestRefundTemplate(string Email, string ProductName, string OneTimePassword);
public record RefundStatusTemplate(string Username, float Price, string OrderStatus, string OrderNumber);