using System;

namespace Api.Facebook.Model;

public class FacebookUserDetails
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
}
