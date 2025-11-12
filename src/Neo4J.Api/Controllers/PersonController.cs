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
MATCH p=shortestPath((:Person {name: $from})-[*]-(:Person {name: $to}))
RETURN [n in nodes(p) | n.name] as people
      ")
      .WithParameters(new {@from, @to})
      .WithMap(record => record["people"].As<IEnumerable<string>>())
      .ExecuteAsync();

      var path = new List<string>();
      foreach (var person in result.Result)
      {
        path.AddRange(person);
      }

      _logger.LogDebug("Found shortest path \"{Path}\"", string.Join("->", path));

      if (path.Any())
      {
        var pt = string.Join("->", path);
        return Ok(pt);
      }
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
RETURN p
        ")
        .WithParameters(new {oldName = request.OldName, newName = request.NewName})
        .ExecuteAsync(cancellationToken);

      return Ok();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "stuff in update");

      return Problem();
    }
  }

  [HttpPut("/update-when-they-know")]
  public async Task<ActionResult> UpdateKnownSinceAsync([FromBody] AddRelationRequest request, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await _driver.
        ExecutableQuery(@"
MATCH (:Person {name: $Name})-[rel:KNOWS]-(:Person {name: $Knows})
SET rel.since = $time
        ")
        .WithParameters(new {time = request.KnownSince, request.Name, request.Knows})
        .ExecuteAsync(cancellationToken);

      return Ok();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "stuff in update since");
      return Problem();
    }
  }

  [HttpDelete("/person")]
  public async Task<ActionResult> PersonIsNoMoreAsync([FromQuery] string name, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await _driver
        .ExecutableQuery(@"
MATCH (n:Person {name: $name})
DETACH DELETE n
        ")
        .WithParameters(new { name })
        .ExecuteAsync(cancellationToken);

      return Ok();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "stuff in kill person");
      return Problem();
    }
  }

  [HttpDelete("/no-longer-friends-with")]
  public async Task<ActionResult> PersonIsNoLongerFriendsWithAsync([FromQuery] string name, string noLongerFrinedsWith, CancellationToken cancellationToken = default)
  {
    try
    {
      var result = await _driver
        .ExecutableQuery(@"
MATCH (:Person {name: $name})-[rel:KNOWS]->(:Person {name: $knows})
DELETE rel
        ")
        .WithParameters(new { name, knows = noLongerFrinedsWith })
        .ExecuteAsync(cancellationToken);

      return Ok();
    }
    catch (Exception e)
    {
      _logger.LogError(e, "stuff in no longer friends with");
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
