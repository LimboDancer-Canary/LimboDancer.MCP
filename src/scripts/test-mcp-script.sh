#!/bin/bash
# test-mcp-server.sh - Test script for LimboDancer MCP Server

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
TENANT_ID="${TENANT_ID:-00000000-0000-0000-0000-000000000000}"
MCP_BINARY="${MCP_BINARY:-ldm}"
TEST_SESSION_ID="$(uuidgen || echo 'test-session-123')"

echo -e "${YELLOW}Testing LimboDancer MCP Server${NC}"
echo "Tenant ID: $TENANT_ID"
echo "Test Session ID: $TEST_SESSION_ID"
echo ""

# Function to send request and get response
send_request() {
    local request=$1
    local description=$2
    
    echo -e "${YELLOW}Test: $description${NC}"
    echo "Request: $request"
    
    # Send request and capture response
    response=$(echo "$request" | $MCP_BINARY serve --stdio --tenant "$TENANT_ID" 2>/dev/null | head -n 1)
    
    if [ -z "$response" ]; then
        echo -e "${RED}✗ No response received${NC}"
        return 1
    fi
    
    # Check if response contains error
    if echo "$response" | grep -q '"error"'; then
        echo -e "${RED}✗ Error response: $response${NC}"
        return 1
    else
        echo -e "${GREEN}✓ Success${NC}"
        echo "Response: $response"
        echo ""
        return 0
    fi
}

# Test 1: Initialize
if send_request '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}' "Initialize protocol"; then
    echo -e "${GREEN}✓ Initialize test passed${NC}\n"
else
    echo -e "${RED}✗ Initialize test failed${NC}\n"
    exit 1
fi

# Test 2: List tools
if send_request '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' "List available tools"; then
    echo -e "${GREEN}✓ List tools test passed${NC}\n"
else
    echo -e "${RED}✗ List tools test failed${NC}\n"
fi

# Test 3: Execute history_get tool
history_request=$(cat <<EOF
{
    "jsonrpc": "2.0",
    "id": 3,
    "method": "tools/call",
    "params": {
        "name": "history_get",
        "arguments": {
            "sessionId": "$TEST_SESSION_ID",
            "limit": 5
        }
    }
}
EOF
)

if send_request "$history_request" "Execute history_get tool"; then
    echo -e "${GREEN}✓ Tool execution test passed${NC}\n"
else
    echo -e "${RED}✗ Tool execution test failed${NC}\n"
fi

# Test 4: Execute memory_search tool
search_request=$(cat <<EOF
{
    "jsonrpc": "2.0",
    "id": 4,
    "method": "tools/call",
    "params": {
        "name": "memory_search",
        "arguments": {
            "queryText": "test query",
            "k": 3
        }
    }
}
EOF
)

if send_request "$search_request" "Execute memory_search tool"; then
    echo -e "${GREEN}✓ Memory search test passed${NC}\n"
else
    echo -e "${RED}✗ Memory search test failed${NC}\n"
fi

# Test 5: Test error handling with invalid tool
error_request=$(cat <<EOF
{
    "jsonrpc": "2.0",
    "id": 5,
    "method": "tools/call",
    "params": {
        "name": "invalid_tool",
        "arguments": {}
    }
}
EOF
)

echo -e "${YELLOW}Test: Error handling with invalid tool${NC}"
echo "Request: $error_request"
response=$(echo "$error_request" | $MCP_BINARY serve --stdio --tenant "$TENANT_ID" 2>/dev/null | head -n 1)

if echo "$response" | grep -q '"error"'; then
    echo -e "${GREEN}✓ Error handling test passed (correctly returned error)${NC}"
    echo "Response: $response"
else
    echo -e "${RED}✗ Error handling test failed (should have returned error)${NC}"
fi

echo ""
echo -e "${YELLOW}Running HTTP mode tests...${NC}"

# Start server in background
$MCP_BINARY serve --tenant "$TENANT_ID" > /tmp/mcp-server.log 2>&1 &
SERVER_PID=$!

# Wait for server to start
echo "Waiting for server to start..."
sleep 3

# Function to test HTTP endpoint
test_http_endpoint() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4
    
    echo -e "${YELLOW}Test: $description${NC}"
    
    if [ -z "$data" ]; then
        response=$(curl -s -w "\nHTTP_STATUS:%{http_code}" "http://localhost:5179$endpoint")
    else
        response=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X "$method" \
            -H "Content-Type: application/json" \
            -H "X-Tenant-Id: $TENANT_ID" \
            -d "$data" \
            "http://localhost:5179$endpoint")
    fi
    
    http_status=$(echo "$response" | grep "HTTP_STATUS:" | cut -d: -f2)
    body=$(echo "$response" | sed '/HTTP_STATUS:/d')
    
    if [ "$http_status" = "200" ]; then
        echo -e "${GREEN}✓ Success (HTTP $http_status)${NC}"
        echo "Response: $body" | jq -r '.' 2>/dev/null || echo "$body"
    else
        echo -e "${RED}✗ Failed (HTTP $http_status)${NC}"
        echo "Response: $body"
    fi
    echo ""
}

# Test HTTP endpoints
test_http_endpoint "GET" "/health" "" "Health check"
test_http_endpoint "GET" "/api/mcp/manifest" "" "Get manifest"
test_http_endpoint "POST" "/api/mcp/initialize" "{}" "Initialize via HTTP"

# Cleanup
kill $SERVER_PID 2>/dev/null || true

echo ""
echo -e "${GREEN}All tests completed!${NC}"
echo "Check /tmp/mcp-server.log for server logs"

# Create a commands file for further testing
cat > mcp-test-commands.jsonl <<EOF
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}
{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"history_get","arguments":{"sessionId":"$TEST_SESSION_ID","limit":10}}}
{"jsonrpc":"2.0","method":"shutdown"}
EOF

echo ""
echo "Test commands saved to mcp-test-commands.jsonl"
echo "You can run: cat mcp-test-commands.jsonl | $MCP_BINARY serve --stdio"