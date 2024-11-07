namespace Web.ContactDetails;

public sealed class ContactOptions
{
    public required string Email { get; set; }
    public required Uri EmailLink { get; set; }
    public required string LandLine { get; set; }
    public required Uri LandLineLink { get; set; }
    public required string Mobile { get; set; }
    public required Uri MobileLink { get; set; }
    public required string Address { get; set; }
    public required Uri AddressLink { get; set; }
    public required string FacebookPageName { get; set; }
    public required Uri FacebookPageLink { get; set; }
}