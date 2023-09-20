namespace ChatSystem.Logic.Constants;

public static class RestErrors
{
    public const string MissingFields = "Missing fields.";
    public const string EmailAlreadyExists = "Email is already registered.";
    public const string InvalidUserOrPass = "Invalid username or password.";

    public const string AccountNotConfirmed = "Please first confirm your account!";

    public const string InvalidUsernameLength = "Invalid username length, username must be 3-24 characters long.";
    public const string InvalidPasswordLength = "Invalid password length, password must be 6-27 characters long.";
    
    public const string InvalidPasswordCharacters = "Password must have atleast 1 upper-case letter and 1 special character in it.";
    public const string InvalidUsernameCharacters = "The username must be between 3 and 16 characters long and may only contain letters, digits, underscores, and hyphens";
    public const string InvalidEmail = "Invalid email address.";

    public const string InvalidResetToken = "Invalid password reset token.";
    public const string InvalidRegistrationToken = "Invalid registration token.";

    public const string FailedToFetchCachedValue = "Failed to fetch cached value, please contact an administrator.";
    public const string FailedToFetchTransaction = "Failed to find your transaction, please contact an administrator.";

    public const string QuantityMustBeAtleastOne = "Quantity must be at-least 1";
    public const string NotEnoughItemsInStock = "Not enough items in stock.";
    public const string ProductOutOfStockQueued =
        "The product you purchased is out of stock but we will send a key as soon as we receive it.";

    public const string InvalidRecaptchaResponse = "Invalid reCAPTCHA response.";

    public const string ListingNotFound = "Listing not found";
    public const string ListingTypeNotFound = "ListingType not found";
}