using NotificationService.Application.Templates.Commands.CreateTemplate;
using NotificationService.Application.Templates.Queries.GetTemplate;
using NotificationService.Application.Templates.Queries.ListTemplates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Templates;
namespace NotificationService.Api.Controllers;

[ApiController, Authorize]
[Route("api/templates")]
public sealed class TemplatesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TemplateResponse>> Create(CreateTemplateCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TemplateResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTemplateQuery(id), cancellationToken));
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TemplateResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ListTemplatesQuery(), cancellationToken));
}
