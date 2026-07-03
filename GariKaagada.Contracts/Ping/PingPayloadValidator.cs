using FluentValidation;

namespace GariKaagada.Contracts.Ping;

/// <summary>
/// Validates <see cref="PingPayload"/> — the single source of truth for this rule (constitution
/// Principle XI); the Angular reactive form validator is a UX-only replica of this.
/// </summary>
public class PingPayloadValidator : AbstractValidator<PingPayload>
{
    public PingPayloadValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(200);
    }
}
