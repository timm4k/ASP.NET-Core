namespace MinimalApiMiddleware.Profiles;

internal sealed record UserProfile(
    string Name,
    string Surname,
    int Age,
    string City,
    string Occupation,
    string Hobby,
    string FavoriteColor);
