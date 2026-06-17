# Copilot Instructions

## Project Guidelines
- Use proper dependency layering: Core/foundational classes should not depend on extension/utility classes. If shared logic is needed, extract it to a shared internal helper that both core and extensions can depend on. This maintains clean architecture and prevents circular dependencies.
