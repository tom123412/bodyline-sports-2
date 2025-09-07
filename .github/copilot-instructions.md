# Bodyline Sports Project Instructions

## Azure Rules
- @azure Rule - Follow Azure Configuration Hierarchy: Always use the appropriate configuration source:
  1. User Secrets (Development)
  2. appsettings.json (Default)
  3. Azure App Configuration (Production)

## Project Structure Guidelines

### Web Project (Blazor WebAssembly)
- Use Tailwind CSS for all styling
- Follow component-based architecture:
  - Pages/: Routable pages
  - Components/: Reusable components
  - Layout/: Layout components
  - Facebook/: Facebook integration components
- Implement memory caching for Facebook API responses
- Use dependency injection consistently

### API Project
- Follow RESTful API design
- Use background services for Facebook token management
- Implement proper CORS for Web client
- Use Azure monitoring and logging
- Handle Facebook API rate limits appropriately

### Admin Project (Blazor Server)
- Use consistent styling with Web project
- Implement proper authorization
- Use Azure AD authentication

## Code Standards

### Facebook Integration
- Cache API responses using IMemoryCache
- Handle rate limits and errors gracefully
- Manage token refresh proactively
- Use proper DTOs for data transfer

### Security
- Use user secrets for local development
- Never commit sensitive information
- Follow HTTPS-only approach
- Implement proper CORS policies

### Performance
- Cache appropriate responses
- Optimize API calls
- Follow Blazor WebAssembly best practices
- Use Azure CDN where appropriate

### Azure Integration
- Use Azure Static Web Apps for frontend
- Configure proper monitoring
- Follow Azure security best practices
- Use managed identities where possible

## Testing and Maintenance
- Write unit tests for business logic
- Test Facebook integration thoroughly
- Keep dependencies updated
- Monitor Azure resources regularly
- Review and rotate secrets periodically