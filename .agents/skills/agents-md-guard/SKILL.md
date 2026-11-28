---
name: agents-md-guard
description: Capture verified development methods in project skills and validate proposed rules against AGENTS.md topic routing. Use when development reveals a repeatable technique or the user wants to record a new convention.
---

# AGENTS.md Guard

## Record a proven method

During development, capture a method when it solves a recurring problem and its result has been verified. Add the symptom, working procedure, and a check to the closest project-owned skill in `.agents/skills/`. If the project has no skill for that procedure, create a focused one and link it from the relevant topic file. Scope the procedure to the cases where it works and keep project-specific decisions in the GDD or technical notes.

Use the validation and confirmation process below for new mandatory conventions.

## Validate the rule

Read the proposal and identify the closest topic from the routing table in the project’s `AGENTS.md`. Read that topic file if it exists, plus any directly relevant existing skill or rule needed for the duplication check. Ask the user to choose the topic when the boundaries in the table do not resolve it.

Apply all checks before proposing a write:

- The rule begins with an imperative such as “Use,” “Do not,” “Always,” or “Never,” or an equivalent imperative in the language of the project rules.
- The rule includes a one-sentence reason that explains when or why compliance matters.
- The rule is reusable across future projects where the topic applies. Keep one-off implementation details, asset specifications, and decisions about the current game in its design documentation.
- The rule adds something distinct from the existing project rules and applicable skills. Look for semantic duplication, including paraphrases of established architecture or Unity guidance.

If a check fails, reject the candidate, identify the failed check, and explain what would make the rule reusable or useful. Offer a revised candidate when the user’s intent is clear.

## Route and record

Follow the exact file path in the current repository's AGENTS.md routing table, including letter case. Choose one primary topic and use the matching section or add a short section.

Show the exact proposed wording and destination. Ask “Записать это правило в `<путь>`?” and wait for confirmation. Reuse an existing confirmation that covers both the wording and the destination.

Preview the insertion and count all resulting lines. If it would put the topic above 50 lines, propose a specific less relevant rule to archive in a separate Legacy file or remove, and wait for the user's decision. Archive or remove an existing rule only with authorization for that change.

After confirmation, append the approved rule in its topic file. Read the result back and verify that the rule is present once, the file has at most 50 lines, and the write matches the approved scope.
