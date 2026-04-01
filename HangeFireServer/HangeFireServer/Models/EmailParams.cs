namespace HangFireServer.Models;

public record EmailParams(
    string To,
    string? Subject,
    string? Message,
    string? Filename = null
);
