using Neo4j.Driver;
using Microsoft.AspNetCore.Mvc;

namespace Neo4J.Api.Controllers;

[ApiController]
[Route("/[controller]")]
public class PersonController: Controller
{
  private readonly IDriver _driver;
  private readonly ILogger<PersonController> _logger;

  public PersonController(IDriver driver, ILogger<PersonController> logger)
  {
    _driver = driver;
    _logger = logger;
  }

  [HttpPost("/add")]
  public async Task<ActionResult> AddPersonAsync([FromQuery] string name, CancellationToken cancellationToken = default)
  {
    var result = await _driver
      .ExecutableQuery(@"
CREATE (a:Person {name: $name})
      ")
      .WithParameters(new { name })
      .ExecuteAsync(cancellationToken);

    if (result.Summary.Counters.NodesCreated is not 1)
      return Problem();
    return Ok();
  }

  [HttpPost("/relation")]
  public async Task<ActionResult> AddRelationAsync([FromBody] AddRelationRequest request, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await _driver
        .ExecutableQuery(@"
MERGE (a:Person {name: $name})
MERGE (b:Person {name: $knows})
MERGE (a)-[:KNOWS {since: $time}]->(b)
        ")
        .WithParameters(new {name = request.Name, knows = request.Knows, time = request.KnownSince})
        .ExecuteAsync(cancellationToken);

      return Ok();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "Stuff happend");

      return Problem("Err");
    }
  }

  [HttpGet("/shortest-path")]
  public async Task<ActionResult> ShortestPathToPersonAsync([FromQuery] string @from, [FromQuery] string @to, CancellationToken cancellationToken = default)
  {
    try
    {
    var result = await _driver
      .ExecutableQuery(@"
MATCH p = SHROTEST 1 (p1:Person)-[:KNOWS]->(p2:Person)
WHERE p1.name = $name1 AND p2.name = $name2
RETURN [n in nodes(p) | n.name] as people
      ")
      .WithParameters(new {name1 = @from, name2 = @to})
      .WithMap(record => record["people"].As<string>())
      .ExecuteAsync();

      var path = new List<string>();
      foreach (var person in result.Result)
        path.Add(person);

      _logger.LogDebug("Found shortest path \"{Path}\"", string.Join("->", path));

      if (path.Any())
        return Ok(string.Join("->", path));
      return NoContent();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "stuff in shortest path");
      return Problem();
    }
  }

  [HttpPut("/update-person")]
  public async Task<ActionResult> UpdatePersonAsync([FromBody] UpdatePersonRequest request, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await _driver
        .ExecutableQuery(@"
MATCH (p:Person {name: $oldName})
SET p.name = $newName
RETURN p.name
        ")
        .WithParameters(new {request.OldName, request.NewName})
        .WithMap(record => record["name"].As<string>())
        .ExecuteAsync(cancellationToken);

      foreach (var newName in result.Result)
        _logger.LogDebug("New name is: " + newName);

      return Ok();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "stuff in update");

      return Problem();
    }
  }

  [HttpPut("/update-all-who-knows")]
  public async Task<ActionResult> UpdateKnownSinceAsync([FromBody] AddRelationRequest request, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await _driver.
        ExecutableQuery(@"
  MATCH (:Person {name: $name})-[rel:KNOWN]->(:Person {name: $knows})
  SET rel.since = $time
  RETURN rel.since
        ")
        .WithParameters(new {request.KnownSince, request.Name, request.Knows})
        .WithMap(record => record["since"].As<DateTime>())
        .ExecuteAsync(cancellationToken);

        foreach (var since in result.Result)
          _logger.LogDebug($"{request.Name} now knows {request.Knows} since: " + since);

      return Ok();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "stuff in update since");
      return Problem();
    }
  }
}

public class UpdateAllWhoKnowsRequest
{
  public required string Person { get; init; }
  public required string PeopelWhoKnowsNewName { get; init; }
}

public class UpdatePersonRequest
{
  public required string OldName { get; init; }
  public required string NewName { get; init; }
}

public class AddRelationRequest
{
  public required string Name { get; init; }
  public required string Knows { get; init; }
  public DateTime KnownSince { get; init; } = DateTime.Now;
}
