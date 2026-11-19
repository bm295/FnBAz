using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace FnBManager.Controllers;

// A deliberately deterministic, non-executing GCEA vertical slice.
// Production deployments should replace this volatile store with durable persistence.
[ApiController]
[Route("api/governed-decisions")]
public sealed class GovernedDecisionsController(GovernedDecisionStore store) : ControllerBase
{
    [HttpPost("inventory-replenishment")]
    public ActionResult<GovernedDecision> Propose([FromBody] ReplenishmentSignal signal)
    {
        if (string.IsNullOrWhiteSpace(signal.ItemCode) || signal.CurrentStock < 0 ||
            signal.ReorderPoint < 0 || signal.TargetStock <= signal.ReorderPoint ||
            signal.UnitCost < 0 || signal.CurrentStock >= signal.ReorderPoint)
            return BadRequest("Supply an item, valid stock levels and a stock level below the reorder point.");

        var quantity = signal.TargetStock - signal.CurrentStock;
        var estimatedCost = quantity * signal.UnitCost;
        var requiresApproval = estimatedCost > 500_000m;
        var decision = new GovernedDecision(
            Guid.NewGuid(), signal.ItemCode.Trim(), signal.CurrentStock, signal.ReorderPoint,
            quantity, estimatedCost, "Stock below reorder point; replenish to target stock.",
            requiresApproval ? "PendingApproval" : "ApprovedForReview",
            requiresApproval, DateTimeOffset.UtcNow);
        store.Add(decision, "proposal.created", "system", decision.Rationale);
        return CreatedAtAction(nameof(Get), new { id = decision.Id }, decision);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GovernedDecision> Get(Guid id) =>
        store.Get(id) is { } value ? Ok(value) : NotFound();

    [HttpPost("{id:guid}/review")]
    public ActionResult<GovernedDecision> Review(Guid id, [FromBody] DecisionReview review)
    {
        if (string.IsNullOrWhiteSpace(review.Reviewer) || string.IsNullOrWhiteSpace(review.Reason))
            return BadRequest("Reviewer and reason are required.");
        var result = store.Review(id, review);
        return result.Outcome switch
        {
            ReviewOutcome.NotFound => NotFound(),
            ReviewOutcome.Conflict => Conflict("This decision has already been reviewed."),
            _ => Ok(result.Decision)
        };
    }

    [HttpGet("{id:guid}/audit")]
    public ActionResult<IReadOnlyList<DecisionAuditEntry>> Audit(Guid id) =>
        store.Get(id) is null ? NotFound() : Ok(store.Audit(id));
}

public sealed record ReplenishmentSignal(
    string ItemCode, int CurrentStock, int ReorderPoint, int TargetStock, decimal UnitCost);

public sealed record DecisionReview(string Reviewer, bool Approve, string Reason);

public sealed record GovernedDecision(
    Guid Id, string ItemCode, int CurrentStock, int ReorderPoint, int ProposedQuantity,
    decimal EstimatedCost, string Rationale, string Status, bool RequiresApproval,
    DateTimeOffset CreatedAtUtc);

public sealed record DecisionAuditEntry(
    DateTimeOffset Timestamp, string EventType, string Actor, string Details, string PreviousHash, string Hash);

public enum ReviewOutcome { Success, NotFound, Conflict }
public sealed record ReviewResult(ReviewOutcome Outcome, GovernedDecision? Decision);

public sealed class GovernedDecisionStore
{
    private sealed class State(GovernedDecision decision)
    {
        public GovernedDecision Decision = decision;
        public readonly List<DecisionAuditEntry> Events = [];
        public readonly object Gate = new();
    }

    private readonly ConcurrentDictionary<Guid, State> _states = new();

    public void Add(GovernedDecision decision, string eventType, string actor, string details)
    {
        var state = new State(decision);
        if (!_states.TryAdd(decision.Id, state))
            throw new InvalidOperationException("Duplicate decision ID.");
        Append(state, eventType, actor, details);
    }

    public GovernedDecision? Get(Guid id)
    {
        if (!_states.TryGetValue(id, out var state)) return null;
        lock (state.Gate) return state.Decision;
    }

    public ReviewResult Review(Guid id, DecisionReview review)
    {
        if (!_states.TryGetValue(id, out var state))
            return new(ReviewOutcome.NotFound, null);
        lock (state.Gate)
        {
            if (state.Decision.Status is "Approved" or "Rejected")
                return new(ReviewOutcome.Conflict, state.Decision);
            state.Decision = state.Decision with { Status = review.Approve ? "Approved" : "Rejected" };
            Append(state, review.Approve ? "decision.approved" : "decision.rejected",
                review.Reviewer.Trim(), review.Reason.Trim());
            return new(ReviewOutcome.Success, state.Decision);
        }
    }

    public IReadOnlyList<DecisionAuditEntry> Audit(Guid id)
    {
        if (!_states.TryGetValue(id, out var state)) return [];
        lock (state.Gate) return state.Events.ToArray();
    }

    private static void Append(State state, string eventType, string actor, string details)
    {
        var previousHash = state.Events.Count == 0 ? "" : state.Events[^1].Hash;
        var timestamp = DateTimeOffset.UtcNow;
        var payload = $"{timestamp:O}|{eventType}|{actor}|{details}|{previousHash}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        state.Events.Add(new(timestamp, eventType, actor, details, previousHash, hash));
    }
}
