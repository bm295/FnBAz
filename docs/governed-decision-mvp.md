# Governed inventory decision MVP (GCEA-inspired)

This vertical slice adapts the **knowledge → decision → bounded action → assurance → feedback** relationships of the Governed Cognitive Enterprise Architecture to FnB inventory. It is a **deterministic architectural experiment**, not an LLM agent and not a production purchasing integration.

## Demonstration

Run the application and submit:

```bash
curl -X POST http://localhost:5000/api/governed-decisions/inventory-replenishment \
  -H 'Content-Type: application/json' \
  -d '{"itemCode":"MILK-01","currentStock":2,"reorderPoint":5,"targetStock":20,"unitCost":40000}'
```

The response contains a decision ID, proposed quantity 18, estimated cost 720000, and `PendingApproval`. Replace `<id>` in the following commands:

```bash
curl -X POST http://localhost:5000/api/governed-decisions/<id>/review \
  -H 'Content-Type: application/json' \
  -d '{"reviewer":"inventory-manager","approve":true,"reason":"Supplier quote checked"}'

curl http://localhost:5000/api/governed-decisions/<id>/audit
```

The review does **not** purchase stock, change inventory, or invoke external tools. A repeated review returns HTTP 409. Each audit event contains a hash chained to the previous event to help detect modifications *within the returned event sequence*; this is not tamper-proof storage.

## Architecture mapping

| GCEA concern | MVP |
| --- | --- |
| Knowledge memory | Explicit inventory signal and decision rationale |
| Decision intelligence | Proposed quantity, cost, status, and reason |
| Governed agentic action | No automatic external side effects; approval gate |
| Human accountability | Named reviewer and recorded approval/rejection |
| AI assurance | Per-decision event trail with chained hashes |
| Operations/feedback | Audit endpoint provides evidence for later evaluation |

## Limits and production follow-up

- Decisions and audit entries live **in process memory** and disappear on restart; use database persistence and migrations before production.
- The `reviewer` field is **self-declared**, not an authenticated identity. Bind it to the authenticated principal and enforce a manager authorization policy.
- The approval threshold of 500000 is an illustrative fixed value, not a company policy. Move it to configurable, versioned policy.
- Inputs are supplied manually; integrate inventory read models and retain source/version identifiers for traceability.
- The approval endpoint only authorizes a proposed action. A future executor should verify authorization again, use idempotency/outbox, and record action outcomes.
- Add integration tests for access control, concurrency, persistence, policy changes, and audit-chain verification.
- No LLM, retrieval, vector memory, evaluation benchmark, or automated learning is included. The source paper is a conceptual architecture and reports no operational validation.

Reference: Agrawal & Tambe, *A reference architecture for governed cognitive enterprises combining knowledge memory and decision intelligence with agentic AI under continuous assurance* (19 Nov 2026), https://link.springer.com/article/10.1007/s44163-026-02719-6
