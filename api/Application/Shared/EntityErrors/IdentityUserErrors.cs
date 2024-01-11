﻿namespace FeedbackAnalyzer.Application.Shared.EntityErrors;

public static class IdentityUserErrors
{
    public static Error NotFound(string email) => 
        Error.NotFound("User.NotFound", $"The user with Email = {email} was not found");
    public static Error NotValidCredential(string email) =>
        Error.Validation("User.NotValidCredential", $"The credentials for user with Email = {email} not valid");
    public static Error NotValidToken() => 
        Error.Validation("User.NotValidToken", "The provided token is not valid.");
    public static Error NotValidEmail() => 
        Error.Validation("User.Email", "The email is not a valid email.");
    public static Error NotValidUserName() => 
        Error.Validation("User.UserName", "The email is not a valid email.");
    public static Error Conflict() => 
        Error.Validation("User.EmailInUse", "The provided email is already taken.");
}