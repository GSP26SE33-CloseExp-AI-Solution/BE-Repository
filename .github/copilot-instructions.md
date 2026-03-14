# Copilot Repository Instructions (CloseExpAI)

## Project Context
This repository is backend for an AI-powered near-expiry food trading platform.

Core business domains:
- User and role management (Admin, Supermarket Staff, Internal Packaging Staff, Delivery Staff, Marketing Staff, Food Vendor)
- Near-expiry product lifecycle and OCR extraction
- Rule-based pricing recommendation by remaining shelf-life
- Order batching, packing, delivery, and status transitions

Tech and architecture constraints:
- .NET 8, ASP.NET Core Web API, EF Core, SQL Server, JWT
- Clean Architecture with four layers:
  - API (presentation)
  - Application (use cases/business logic)
  - Domain (entities/contracts, no outside dependencies)
  - Infrastructure (db/external integrations)

## How To Respond
- Prefer Vietnamese in explanations unless user asks for English.
- Be concrete and code-first: provide runnable edits over abstract advice.
- When requirements are ambiguous, infer from project context first, then ask at most 1-2 clarifying questions.
- Keep naming and examples aligned with this domain (expiry, OCR, pricing, orders, delivery, role-based auth).

## Diagram Standard
- When the user asks for diagrams, flows, graphs, architecture visualizations, or model relationships, use UML 2.0 conventions by default.
- Prefer UML diagram types that match intent: Use Case, Class, Sequence, Activity, State Machine, Component, and Deployment.
- Keep domain terms from this project in diagrams (near-expiry batch, OCR extraction, pricing rule, order status transitions, delivery assignment).
- If a rendered format is required, provide PlantUML first (UML 2.0 syntax). If PlantUML is not possible, provide an explicitly labeled UML 2.0-equivalent representation.

## Coding Rules
- Preserve Clean Architecture boundaries; avoid leaking Infrastructure concerns into Domain.
- For API changes:
  - Define request/response DTOs in Application layer.
  - Validate input and authorization roles.
  - Keep controller thin; business logic belongs in services/use cases.
- For business logic:
  - Express rules explicitly (e.g., shelf-life thresholds, status transitions).
  - Handle edge cases around date parsing/time zones/invalid expiry data.
- For persistence:
  - Use EF Core with clear entity configuration and migration-safe changes.
  - Prefer transactional integrity for order creation and status updates.

## Delivery Expectations
When implementing features or fixes, try to include:
- Minimal code changes with clear layer placement
- Error handling and useful log messages
- Tests or at least test cases to validate behavior
- Notes about migration impact if schema changes are introduced
