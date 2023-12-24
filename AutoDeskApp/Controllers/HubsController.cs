﻿using AutoDeskApp.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AutoDeskApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HubsController : Controller
{
    private readonly ILogger<HubsController> _logger;
    private readonly APS _aps;

    public HubsController(ILogger<HubsController> logger, APS aps)
    {
        _logger = logger;
        _aps = aps;
    }

    [HttpGet()]
    public async Task<ActionResult<string>> ListHubs()
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        var hubs = await _aps.GetHubs(tokens);
        return JsonConvert.SerializeObject(hubs);
    }

    [HttpGet("{hub}/projects")]
    public async Task<ActionResult<string>> ListProjects(string hub)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        var projects = await _aps.GetProjects(hub, tokens);
        return JsonConvert.SerializeObject(projects);
    }

    [HttpGet("{hub}/projects/{project}/contents")]
    public async Task<ActionResult<string>> ListItems(string hub, string project, [FromQuery] string? folder_id)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        var contents = await _aps.GetContents(hub, project, folder_id, tokens);
        return JsonConvert.SerializeObject(contents);
    }

    [HttpGet("{hub}/projects/{project}/contents/{item}/versions")]
    public async Task<ActionResult<string>> ListVersions(string hub, string project, string item)
    {
        var tokens = await AuthController.PrepareTokens(Request, Response, _aps);
        if (tokens == null)
        {
            return Unauthorized();
        }
        var versions = await _aps.GetVersions(hub, project, item, tokens);
        return JsonConvert.SerializeObject(versions);
    }
}
