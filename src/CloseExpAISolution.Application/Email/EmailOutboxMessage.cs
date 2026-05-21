namespace CloseExpAISolution.Application.Email;

public sealed record EmailOutboxMessage(
    string ToEmail,
    string Subject,
    string HtmlBody);
