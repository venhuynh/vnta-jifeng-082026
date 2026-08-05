# MCP Servers

This repository currently standardizes DevExpress documentation lookup through solution-level MCP configuration for the HRM solution.

## Prerequisites

- Visual Studio 2022 `17.14+` or Visual Studio 2026.
- GitHub Copilot with `Agent` mode enabled.
- MCP tool usage allowed by organization policy if the account is managed.

## Active Configuration

The tracked MCP configuration is in `src/Vnta.HRM2026/.mcp.json`.

This location matches Visual Studio automatic discovery for the solution at `src/Vnta.HRM2026/Vnta.Hrm.slnx`.

Configured servers:

- `dxdocs`: DevExpress documentation MCP endpoint for the latest available docs.
- `dxdocs26_1`: DevExpress documentation MCP endpoint pinned to `v26.1`, aligned with the DevExpress packages used by `Vnta.Hrm.Web.Client`.
- Both endpoints use remote HTTP transport and do not require repository-local secrets in this repo configuration.

Current configuration:

```json
{
  "servers": {
    "dxdocs": {
      "type": "http",
      "url": "https://api.devexpress.com/mcp/docs"
    },
    "dxdocs26_1": {
      "type": "http",
      "url": "https://api.devexpress.com/mcp/docs?v=26.1"
    }
  }
}
```

## Copilot Instructions

Repository-level Copilot instructions are stored in `.github/copilot-instructions.md`.

The instructions tell GitHub Copilot or Visual Studio Agent mode to:

- use DevExpress MCP tools for DevExpress-related questions
- prefer `dxdocs26_1` for questions about the current HRM solution
- fall back to `dxdocs` for latest-version questions
- prefer the built-in DevExpress workflow prompt `mcp.dxdocs.devexpress_docs_query_workflow` when MCP prompts are available

## Prompt Workflow

DevExpress publishes a predefined MCP prompt workflow for documentation retrieval:

- workflow name: `mcp.dxdocs.devexpress_docs_query_workflow`
- in Visual Studio: open `Prompts` -> `MCP Prompts`, choose the DevExpress prompt, insert it into chat, then submit your question
- in VS Code: run `/mcp.dxdocs.devexpress_docs_query_workflow`

## Usage Notes

- Visual Studio requires Agent mode to access MCP tools.
- Visual Studio automatically discovers MCP configuration from `%USERPROFILE%\.mcp.json`, `<SOLUTIONDIR>\.vs\mcp.json`, `<SOLUTIONDIR>\.mcp.json`, `<SOLUTIONDIR>\.vscode\mcp.json`, and `<SOLUTIONDIR>\.cursor\mcp.json`.
- Some locations require `.mcp.json`, while `.vscode` and `.cursor` use `mcp.json`.
- When Visual Studio discovers a server, its tools are available to Agent mode but still need to be enabled manually in the tool picker.
- For this repository, the intended shared configuration is `src/Vnta.HRM2026/.mcp.json` so it cần stay in source control with the solution.

## Runtime Limitation

The solution-level `.mcp.json` is a discovery input for MCP-capable clients. It does **not** dynamically register a tool in an agent runtime that has already started. In particular, a Codex or other hosted-agent session can expose only the MCP servers provisioned by that host at session startup.

If `dxdocs26_1` is absent from the current runtime tool list:

1. Do not change the DevExpress endpoint merely to make a tool appear; the tracked endpoint is valid for `v=26.1`.
2. Reload the supported IDE/client session, then enable and trust `dxdocs26_1` in its tool picker.
3. If the host still does not expose it, use the official DevExpress documentation fallback in `doc/checklists/screen-implementation-principles.md` and report that MCP was unavailable in that runtime.

This distinction prevents repository instructions from incorrectly requiring a tool that the current host cannot provide.

## Verification Checklist

After opening `src/Vnta.HRM2026/Vnta.Hrm.slnx` in Visual Studio:

1. Switch Copilot chat to `Agent` mode.
2. Open the tool picker and confirm `dxdocs` or `dxdocs26_1` appears.
3. Enable the DevExpress tools you want to use.
4. If prompted, accept trust or authorization for the remote server.
5. Ask a small smoke-test question such as `Find the official docs for DxGrid filtering in DevExpress Blazor 26.1`.

## Current Scope

This repository does not currently include:

- `.vscode/mcp.json`
- `.cursor/mcp.json`
- a local `BootstrapBlazor` MCP server
- `tools/BootstrapBlazor.MCPServer`

If those are added later, update this document to keep the tracked configuration accurate.


