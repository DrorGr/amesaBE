# Pieces MCP Integration Guide

## Overview

Pieces MCP (Model Context Protocol) integration provides Cursor agents with access to Long-Term Memory (LTM-2.7) engine, enabling powerful context retrieval from your workflow history, code snippets, notes, and activity tracking.

**Key Benefits:**
- **Context Continuity**: Agents understand recent work and decisions across sessions
- **Pattern Reuse**: Discover existing implementations and proven solutions
- **Decision Tracking**: Understand project evolution and documented challenges
- **Resource Discovery**: Access saved code snippets, bookmarks, and notes
- **Workflow Efficiency**: Faster problem-solving with historical context

## Prerequisites

### 1. PiecesOS Installation

PiecesOS must be installed and running for MCP integration to work.

**Installation Options:**
- **Desktop App**: Download from [Pieces Desktop App](https://docs.pieces.app/products/desktop/download) (includes PiecesOS)
- **Standalone**: [Manual Installation Guide](https://docs.pieces.app/products/core-dependencies/pieces-os/manual-installation#manual-download--installation)

**Verification:**
- Check system tray/processes for PiecesOS running
- Open PiecesOS Quick Menu (toolbar icon) to verify status

### 2. Long-Term Memory Engine

LTM-2.7 must be enabled for MCP to access workflow context.

**Enable via Desktop App:**
1. Open Pieces Desktop App
2. Navigate to **Settings**
3. Enable **Long-Term Memory Engine (LTM-2.7)**

**Enable via Quick Menu:**
1. Open PiecesOS Quick Menu (toolbar icon)
2. Enable **Long-Term Memory Engine (LTM-2.7)**

### 3. Get SSE Endpoint

The Server-Sent Events (SSE) endpoint is required for Cursor configuration.

**Method 1: PiecesOS Quick Menu**
1. Open PiecesOS Quick Menu
2. Expand **Model Context Protocol (MCP) Servers** tab
3. Click to copy the SSE endpoint
4. Format: `http://localhost:{port}/model_context_protocol/{version}/sse`
5. Default: `http://localhost:39300/model_context_protocol/2024-11-05/sse`

**Method 2: Desktop App**
1. Open Pieces Desktop App
2. Navigate to **Settings** → **Model Context Protocol (MCP)**
3. Copy the SSE endpoint URL

**Note**: Port number may vary - always use the active port from PiecesOS.

## Cursor Configuration

### Step 1: Open Cursor Settings

1. Open Cursor IDE
2. Navigate to **Cursor Settings** (or **Preferences**)
3. Go to **MCP** section

### Step 2: Add Pieces MCP Server

1. Click **"Add new global MCP server"**
2. Insert the following JSON configuration (adjust port if necessary):

```json
{
  "mcpServers": {
    "Pieces": {
      "url": "http://localhost:39300/model_context_protocol/2024-11-05/sse"
    }
  }
}
```

3. **Important**: Replace `39300` with your actual PiecesOS port if different
4. Save the configuration file

### Step 3: Verify Connection

1. Click the **refresh icon** in the MCP server window
2. Verify **green dot** indicates server is running
3. Status should show "running" (not "start" or "pause")

**Troubleshooting:**
- If status shows "start" or "pause", click to restart the server
- Check terminal outputs for error messages
- Verify PiecesOS is running and LTM is enabled

### Step 4: Enable Agent Mode

**Critical**: Cursor must be in **Agent Mode** (not Ask or Manual mode) to access `ask_pieces_ltm` tool.

1. Open Cursor chat panel (`⌘+i` on macOS, `ctrl+i` on Windows/Linux)
2. Ensure chat mode is set to **Agent** (not Ask or Manual)
3. If needed, turn off "auto-select" and manually select an agent (e.g., `claude-3.5-sonnet`)

## Usage Guidelines

### When to Use Pieces MCP

#### 1. Context Retrieval
Query previous work and decisions:
- "What was I working on yesterday?"
- "Show me recent authentication code changes"
- "What files did I modify last week?"
- "What was I doing with this file yesterday?"

#### 2. Code Pattern Discovery
Find existing implementations:
- "Show examples of React Context usage in this project"
- "What was my last implementation of API error handling?"
- "Have I previously optimized rendering performance in React components?"
- "Show me how I implemented authentication middleware"

#### 3. Decision Tracking
Understand project evolution:
- "Track the evolution of the dashboard feature"
- "Review documented challenges with the payment system"
- "Show the decisions made around UI updates for the onboarding flow"
- "What architectural decisions were made for the microservices?"

#### 4. Resource Discovery
Find saved resources:
- "Find recent bookmarks about Kubernetes"
- "What resources did I save recently related to Python decorators?"
- "Show notes taken about GraphQL in March"
- "What documentation did I bookmark about AWS ECS?"

#### 5. Code Review Context
Understand feedback and changes:
- "Show code review comments related to database indexing"
- "Did we finalize naming conventions for the latest API endpoints?"
- "What feedback did I leave on recent pull requests?"
- "What issues were identified in the security audit?"

### Effective Prompting Tips

#### Specify Timeframes
- Use specific dates: "April 2nd through April 6th"
- Use relative time: "yesterday", "last week", "this month"
- Combine with applications: "Stack Overflow pages I visited on Chrome yesterday"

#### Mention Applications
- "meeting notes from Notion"
- "code snippets from VS Code"
- "bookmarks from Chrome"
- "GitHub issues I commented on"

#### Include Technical Keywords
- "JavaScript code related to API authentication"
- ".NET 8.0 migration patterns"
- "PostgreSQL schema changes"
- "Docker deployment configurations"

#### Reference Open Files
- "What was I doing with this file yesterday?"
- "Show me changes related to the currently open file"
- "What context exists for this component?"

#### Combine Parameters
Mix timeframes, applications, and topics for precise queries:
- "What JavaScript code related to API authentication did I write in VS Code yesterday?"
- "Find notes on database changes between Monday and Wednesday"
- "What is the package version update that Mark asked me to make? Make the relevant update in my package manifest."

### Example Prompts

**Context Discovery:**
```
"What was I working on in this project yesterday?"
"What recent changes were made to the authentication system?"
"Show me recent work on the payment service"
```

**Pattern Finding:**
```
"Have I worked on similar features before?"
"What patterns or solutions did I use for rate limiting?"
"Show me examples of error handling I've implemented"
```

**Problem Solving:**
```
"How did I solve Redis connection issues before?"
"Show me examples of database migration patterns I've used"
"What was my approach to fixing SignalR connection problems?"
```

**Decision Understanding:**
```
"Why did we choose PostgreSQL over MySQL?"
"What were the considerations for the microservices architecture?"
"Show me documented decisions about authentication strategies"
```

## Agent Workflow Integration

### On Agent Creation

When a new agent session starts, the agent should:

1. **Check Pieces MCP Status**
   - Verify PiecesOS is running
   - Confirm LTM is enabled
   - Test connection with simple query

2. **Query Recent Context**
   - "What was I working on in this project yesterday?"
   - "What recent changes were made to the authentication system?"
   - "Show me recent work on the payment service"
   - "What issues or bugs were recently fixed?"

3. **Understand Current State**
   - "What is the current deployment status?"
   - "What features are in progress?"
   - "What technical debt or known issues exist?"

### During Task Execution

#### Before Starting Work
1. **Query Related Previous Work**
   - "Have I worked on similar features before?"
   - "What patterns or solutions did I use for [similar task]?"
   - "Show me previous implementations of [feature type]"

2. **Check for Existing Solutions**
   - "How did I solve [similar problem] before?"
   - "What approaches have been tried for [issue]?"
   - "Are there existing patterns I should follow?"

#### When Stuck
1. **Query LTM for Solutions**
   - "How did I solve [similar problem] before?"
   - "Show me examples of [pattern] I've used"
   - "What resources did I save about [topic]?"

2. **Find Related Context**
   - "What documentation exists about [topic]?"
   - "Show me code snippets related to [feature]"
   - "What decisions were made about [architectural choice]?"

#### After Completing Work
1. **Document Decisions** (if applicable)
   - Note: Agents should document important decisions and patterns
   - This helps future agent sessions understand context

## Troubleshooting

### MCP Server Not Running

**Symptoms:**
- Red status indicator in Cursor MCP settings
- "Sorry, I can't do this" messages
- Tool not available in chat

**Solutions:**
1. Verify PiecesOS is running (check system tray/processes)
2. Check MCP server status in Cursor settings (should show "running")
3. Restart MCP server if needed (click start/pause button)
4. Verify LTM engine is enabled in PiecesOS
5. Check terminal outputs for error messages

### Tool Not Available

**Symptoms:**
- `ask_pieces_ltm` tool not appearing in chat
- Agent says it can't access Pieces

**Solutions:**
1. **Enable Agent Mode**: Switch chat mode to Agent (not Ask or Manual)
2. **Turn Off Auto-Select**: Manually select an agent (e.g., `claude-3.5-sonnet`)
3. **Verify MCP Connection**: Check green dot in MCP settings
4. **Restart Cursor**: Sometimes requires IDE restart after MCP configuration

### No Results from Queries

**Symptoms:**
- Queries return empty or "no results found"
- LTM doesn't have context

**Solutions:**
1. **Verify LTM is Enabled**: Check PiecesOS settings
2. **Check PiecesOS Activity**: Ensure it's capturing your workflow
3. **Wait for Indexing**: LTM may need time to index recent activity
4. **Check Date Ranges**: Ensure queries reference timeframes with activity
5. **Verify Applications**: Ensure PiecesOS is tracking the applications you're querying

### Connection Issues

**Symptoms:**
- Connection errors in MCP settings
- SSE endpoint not responding

**Solutions:**
1. **Verify Port Number**: Check active PiecesOS port in Quick Menu
2. **Update Configuration**: Update MCP config with correct port
3. **Check Firewall**: Ensure localhost connections aren't blocked
4. **Restart PiecesOS**: Sometimes requires restart to fix connection issues
5. **Verify SSE Endpoint**: Use exact endpoint from PiecesOS Quick Menu

### JSON Blob Error in MCP Settings

**Symptoms:**
- Raw JSON payload displayed in MCP settings
- "Unknown message ID" error shown

**Note**: This is a **harmless visual artifact**. Cursor's Settings UI doesn't recognize the JSON-RPC success envelope and misclassifies it as an error.

**Solution:**
- **Ignore the red JSON blob** in MCP Settings view
- **Chat pane is the source of truth** - if queries return formatted summaries, integration is working correctly

## Configuration Reference

### MCP Server Configuration

```json
{
  "mcpServers": {
    "Pieces": {
      "url": "http://localhost:39300/model_context_protocol/2024-11-05/sse"
    }
  }
}
```

### Environment-Specific Ports

Port numbers may vary. Always use the active port from PiecesOS:
- Default: `39300`
- Check PiecesOS Quick Menu for current port
- Update MCP configuration if port differs

### SSE Endpoint Format

```
http://localhost:{port}/model_context_protocol/{version}/sse
```

- **Port**: Active PiecesOS port (check Quick Menu)
- **Version**: Protocol version (typically `2024-11-05` or `2025-03-26`)

## Best Practices

### For Developers

1. **Keep PiecesOS Running**: Ensure it's always running to capture workflow
2. **Enable LTM**: Keep Long-Term Memory engine enabled
3. **Save Important Context**: Use Pieces to save code snippets and notes
4. **Document Decisions**: Save architectural decisions and patterns
5. **Regular Queries**: Use LTM queries to understand project state

### For Agents

1. **Always Check Recent Context**: Query LTM at session start
2. **Query Before Starting Work**: Check for existing solutions
3. **Use Specific Prompts**: Include timeframes, applications, and keywords
4. **Combine Context Sources**: Use LTM alongside codebase search
5. **Document Important Work**: Note significant decisions and patterns

## Integration Status

**Status**: ✅ **Ready for Integration**

**Next Steps:**
1. Install/verify PiecesOS is running
2. Enable LTM-2.7 engine
3. Configure Cursor MCP settings
4. Test with simple query in Agent Mode
5. Start using LTM queries in agent workflow

## References

- [Pieces MCP Cursor Integration](https://docs.pieces.app/products/mcp/cursor)
- [Pieces MCP Prompting Guide](https://docs.pieces.app/products/mcp/prompting)
- [Pieces CLI Documentation](https://docs.pieces.app/products/cli)
- [PiecesOS Manual Installation](https://docs.pieces.app/products/core-dependencies/pieces-os/manual-installation)

---

**Last Updated**: 2025-01-25
**Integration Version**: 1.0.0
