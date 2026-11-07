using FluentValidation;
using MediatR;
using PixHook.Application.Behaviors;
using PixHook.Application.Common;
using PixHook.Application.Features.Dispatches;

namespace PixHook.Tests.Unit.Application;

public class ValidatorTests
{
    private readonly DispatchWebhookCommandValidator _dispatchValidator = new();

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-url")]
    [InlineData("ftp://localhost/hook")]
    [InlineData("/relativa/hook")]
    public void Validate_UrlDestinoInvalida_RetornaErroEmDestinationUrl(string url)
    {
        var result = _dispatchValidator.Validate(new DispatchWebhookCommand(Guid.NewGuid(), url));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DispatchWebhookCommand.DestinationUrl));
    }

    [Fact]
    public void Validate_TemplateIdVazio_RetornaErroEmTemplateId()
    {
        var result = _dispatchValidator.Validate(new DispatchWebhookCommand(Guid.Empty, "http://localhost:5000/hook"));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(DispatchWebhookCommand.TemplateId));
    }

    [Fact]
    public void Validate_ComandoValido_NaoRetornaErros()
    {
        var result = _dispatchValidator.Validate(new DispatchWebhookCommand(Guid.NewGuid(), "https://minhaapi.dev/webhooks/pix"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Validate_PaginacaoInvalida_RetornaErro(int page, int pageSize)
    {
        var result = new GetDispatchHistoryQueryValidator().Validate(new GetDispatchHistoryQuery(page, pageSize));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidationBehavior_RequisicaoInvalida_RetornaResultValidationSemChamarHandler()
    {
        var behavior = new ValidationBehavior<DispatchWebhookCommand, Result<DispatchDto>>(
            new IValidator<DispatchWebhookCommand>[] { _dispatchValidator });
        var nextCalled = false;
        RequestHandlerDelegate<Result<DispatchDto>> next = () =>
        {
            nextCalled = true;
            return Task.FromResult<Result<DispatchDto>>(Error.Failure("x", "x"));
        };

        var result = await behavior.Handle(new DispatchWebhookCommand(Guid.Empty, "invalida"), next, default);

        nextCalled.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Validation);
        result.Error.Details.Should().ContainKeys("TemplateId", "DestinationUrl");
    }
}
