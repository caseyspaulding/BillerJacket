#!/usr/bin/env node
/**
 * MCP Wrapper for BillerJacket.DbMCP HTTP Server
 *
 * This wraps the HTTP REST API as an MCP stdio server for Claude Code integration.
 *
 * Usage in .mcp.json:
 * {
 *   "mcpServers": {
 *     "billerjacket-db": {
 *       "type": "stdio",
 *       "command": "node",
 *       "args": ["/home/casey/projects/BillerJacket/src/BillerJacket.DbMCP/mcp-wrapper.js"]
 *     }
 *   }
 * }
 */

const http = require('http');
const readline = require('readline');

const DB_MCP_URL = 'http://localhost:7778';

// MCP Protocol implementation
class MCPServer {
  constructor() {
    this.rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout,
      terminal: false
    });
  }

  async start() {
    this.rl.on('line', async (line) => {
      try {
        const request = JSON.parse(line);
        const response = await this.handleRequest(request);
        console.log(JSON.stringify(response));
      } catch (err) {
        console.log(JSON.stringify({
          jsonrpc: '2.0',
          error: { code: -32700, message: 'Parse error', data: err.message },
          id: null
        }));
      }
    });
  }

  async handleRequest(request) {
    const { method, params, id } = request;

    switch (method) {
      case 'initialize':
        return this.initialize(id);
      case 'tools/list':
        return this.listTools(id);
      case 'tools/call':
        return this.callTool(params, id);
      default:
        return {
          jsonrpc: '2.0',
          error: { code: -32601, message: `Method not found: ${method}` },
          id
        };
    }
  }

  initialize(id) {
    return {
      jsonrpc: '2.0',
      result: {
        protocolVersion: '2024-11-05',
        capabilities: { tools: {} },
        serverInfo: {
          name: 'billerjacket-db',
          version: '1.0.0'
        }
      },
      id
    };
  }

  listTools(id) {
    return {
      jsonrpc: '2.0',
      result: {
        tools: [
          {
            name: 'db_query',
            description: 'Execute a SELECT query against the BillerJacket database. Use for: checking data, verifying records exist, debugging. Returns up to 100 rows by default. Only SELECT and EXEC statements allowed.',
            inputSchema: {
              type: 'object',
              properties: {
                sql: { type: 'string', description: 'SQL SELECT query (e.g., "SELECT * FROM Billers WHERE Name = \'test\'")' },
                limit: { type: 'number', description: 'Max rows to return', default: 100 }
              },
              required: ['sql']
            }
          },
          {
            name: 'db_execute',
            description: 'Execute DDL/DML (CREATE, UPDATE, DELETE) against the BillerJacket database. Use dryRun=true first to preview changes. Dangerous operations (DROP, TRUNCATE, UPDATE/DELETE without WHERE) require force=true.',
            inputSchema: {
              type: 'object',
              properties: {
                sql: { type: 'string', description: 'SQL statement' },
                dryRun: { type: 'boolean', description: 'Preview without executing', default: true }
              },
              required: ['sql']
            }
          },
          {
            name: 'db_context',
            description: 'Get database context - lists all tables with row counts. Use this first to understand what tables exist before writing queries or Entity classes.',
            inputSchema: { type: 'object', properties: {} }
          },
          {
            name: 'db_health',
            description: 'Check database connection health. Returns status, database name, and server info.',
            inputSchema: { type: 'object', properties: {} }
          },
          {
            name: 'db_schema',
            description: 'Get column details for a specific table. Returns column names, types, nullability, defaults, and primary key info. Use BEFORE writing Entity classes to ensure correct property types.',
            inputSchema: {
              type: 'object',
              properties: {
                table: { type: 'string', description: 'Table name (e.g., "Billers", "Invoices", "Organizations")' }
              },
              required: ['table']
            }
          }
        ]
      },
      id
    };
  }

  async callTool(params, id) {
    const { name, arguments: args } = params;

    try {
      let result;
      switch (name) {
        case 'db_query':
          result = await this.httpPost('/mcp/query', {
            sql: args.sql,
            limit: args.limit || 100
          });
          break;
        case 'db_execute':
          result = await this.httpPost('/mcp/execute', {
            sql: args.sql,
            dryRun: args.dryRun !== false
          });
          break;
        case 'db_context':
          result = await this.httpGet('/mcp/context');
          break;
        case 'db_health':
          result = await this.httpGet('/mcp/health');
          break;
        case 'db_schema':
          result = await this.httpGet(`/mcp/schema/${encodeURIComponent(args.table)}`);
          break;
        default:
          return {
            jsonrpc: '2.0',
            error: { code: -32602, message: `Unknown tool: ${name}` },
            id
          };
      }

      return {
        jsonrpc: '2.0',
        result: {
          content: [{ type: 'text', text: JSON.stringify(result, null, 2) }]
        },
        id
      };
    } catch (err) {
      return {
        jsonrpc: '2.0',
        result: {
          content: [{ type: 'text', text: `Error: ${err.message}` }],
          isError: true
        },
        id
      };
    }
  }

  httpGet(path) {
    return new Promise((resolve, reject) => {
      http.get(`${DB_MCP_URL}${path}`, (res) => {
        let data = '';
        res.on('data', chunk => data += chunk);
        res.on('end', () => {
          try {
            resolve(JSON.parse(data));
          } catch {
            resolve({ raw: data });
          }
        });
      }).on('error', reject);
    });
  }

  httpPost(path, body) {
    return new Promise((resolve, reject) => {
      const data = JSON.stringify(body);
      const req = http.request(`${DB_MCP_URL}${path}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(data)
        }
      }, (res) => {
        let responseData = '';
        res.on('data', chunk => responseData += chunk);
        res.on('end', () => {
          try {
            resolve(JSON.parse(responseData));
          } catch {
            resolve({ raw: responseData });
          }
        });
      });
      req.on('error', reject);
      req.write(data);
      req.end();
    });
  }
}

const server = new MCPServer();
server.start();
