# Workflows

This section documents the PlantUML workflow definitions used by the FunWasHad application. Each workflow drives a guided, stateful user experience (e.g., location-based discovery, activity logging).

---

## Workflow Files

| Workflow | Source | Description |
|----------|--------|-------------|
| [Fun Was Had](workflow.md) | `workflow.puml` | Main flow: get nearby businesses, take a photo, record whether fun was had. |
| [New Location Detected](new-location.md) | `new-location.puml` | Triggered on `NewLocationAddress`: welcome, check businesses, optional activity/photo, save as landmark. |

---

## Overview

- **workflow.puml** — Loaded by the mobile app from the app bundle or working directory. Used for the primary “Fun Was Had” experience.
- **new-location.puml** — Loaded by `LocationWorkflowService` when a new address is detected. Instances are keyed by `location:{address_hash}` and can be resumed within a 24-hour window.

Each workflow’s doc includes a Mermaid diagram, the PlantUML source, states/transitions, actions, and any transitions to other workflows.
