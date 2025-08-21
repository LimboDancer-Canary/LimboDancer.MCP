#!/usr/bin/env bash
set -euo pipefail

# ===========
# Edit these
# ===========
TENANT_ID="00000000-0000-0000-0000-000000000000"

API_APP_NAME="LimboDancer.MCP.McpServer.Http"
CLIENT_APP_NAME="LimboDancer.MCP.BlazorConsole"

# Local dev URLs (from your launchSettings)
BLAZOR_CONSOLE_HTTPS="https://localhost:7127"
BLAZOR_CONSOLE_IIS_EXPRESS_HTTPS="https://localhost:44363"   # optional, only if you use IIS Express often
API_HTTPS_BASE="https://localhost:7056"

# Whether to also add the IIS Express redirect (true/false)
ADD_IIS_EXPRESS_REDIRECT=false

# =========================
# Pre-flight and login info
# =========================
command -v jq >/dev/null || { echo "jq is required"; exit 1; }
command -v uuidgen >/dev/null || { echo "uuidgen is required"; exit 1; }

echo "Using tenant: $TENANT_ID"
az account show >/dev/null || az login --tenant "$TENANT_ID" >/dev/null
az account tenant set --tenant "$TENANT_ID" >/dev/null

# ========================
# 1) Create the API (AAD)
# ========================
echo "Creating API app registration: $API_APP_NAME"
API_CREATE_JSON=$(az ad app create \
  --display-name "$API_APP_NAME" \
  --sign-in-audience AzureADMyOrg \
  -o json)

API_APP_ID=$(echo "$API_CREATE_JSON" | jq -r '.appId')
API_OBJECT_ID=$(echo "$API_CREATE_JSON" | jq -r '.id')

echo "API app created:"
echo "  appId:    $API_APP_ID"
echo "  objectId: $API_OBJECT_ID"

# Set Application ID URI: api://<API_CLIENT_ID>
echo "Setting Application ID URI"
az rest --method PATCH \
  --url "https://graph.microsoft.com/v1.0/applications/$API_OBJECT_ID" \
  --body "$(jq -n --arg uri "api://$API_APP_ID" '{identifierUris:[$uri]}')" >/dev/null

# Create delegated scope: Mcp.Access
MCP_ACCESS_SCOPE_ID=$(uuidgen)
echo "Adding scope Mcp.Access (id: $MCP_ACCESS_SCOPE_ID)"
az rest --method PATCH \
  --url "https://graph.microsoft.com/v1.0/applications/$API_OBJECT_ID" \
  --body "$(jq -n --arg id "$MCP_ACCESS_SCOPE_ID" \
    '{
       api:{
         oauth2PermissionScopes:[
           {
             adminConsentDescription:"Allow the app to access the MCP API on behalf of the signed-in user.",
             adminConsentDisplayName:"Access MCP API",
             id:$id,
             isEnabled:true,
             type:"User",
             userConsentDescription:"Allow the app to access the MCP API on your behalf.",
             userConsentDisplayName:"Access MCP API",
             value:"Mcp.Access"
           }
         ]
       }
     }')" >/dev/null

# Add app roles: ChatUser, Operator, Admin
CHATUSER_ROLE_ID=$(uuidgen)
OPERATOR_ROLE_ID=$(uuidgen)
ADMIN_ROLE_ID=$(uuidgen)
echo "Adding app roles (ChatUser:$CHATUSER_ROLE_ID, Operator:$OPERATOR_ROLE_ID, Admin:$ADMIN_ROLE_ID)"
az rest --method PATCH \
  --url "https://graph.microsoft.com/v1.0/applications/$API_OBJECT_ID" \
  --body "$(jq -n \
    --arg chatId "$CHATUSER_ROLE_ID" \
    --arg opId "$OPERATOR_ROLE_ID" \
    --arg admId "$ADMIN_ROLE_ID" \
    '{
      appRoles: [
        {
          allowedMemberTypes:["User"],
          description:"Regular chat user",
          displayName:"ChatUser",
          id:$chatId,
          isEnabled:true,
          value:"ChatUser"
        },
        {
          allowedMemberTypes:["User"],
          description:"Operator with elevated privileges",
          displayName:"Operator",
          id:$opId,
          isEnabled:true,
          value:"Operator"
        },
        {
          allowedMemberTypes:["User"],
          description:"Administrator",
          displayName:"Admin",
          id:$admId,
          isEnabled:true,
          value:"Admin"
        }
      ]
    }')" >/dev/null

# Ensure the service principal exists
echo "Creating API service principal (if not present)"
az ad sp create --id "$API_APP_ID" >/dev/null || true
API_SP_OBJECT_ID=$(az ad sp show --id "$API_APP_ID" --query id -o tsv)

# =================================
# 2) Create the Client (Blazor app)
# =================================
echo "Creating client app registration: $CLIENT_APP_NAME"
REDIRECTS=("$BLAZOR_CONSOLE_HTTPS/signin-oidc")
$ADD_IIS_EXPRESS_REDIRECT && REDIRECTS+=("$BLAZOR_CONSOLE_IIS_EXPRESS_HTTPS/signin-oidc")

CLIENT_CREATE_JSON=$(az ad app create \
  --display-name "$CLIENT_APP_NAME" \
  --sign-in-audience AzureADMyOrg \
  --web-redirect-uris "${REDIRECTS[@]}" \
  --web-logout-url "$BLAZOR_CONSOLE_HTTPS/signout-oidc" \
  -o json)

CLIENT_APP_ID=$(echo "$CLIENT_CREATE_JSON" | jq -r '.appId')
CLIENT_OBJECT_ID=$(echo "$CLIENT_CREATE_JSON" | jq -r '.id')

echo "Client app created:"
echo "  appId:    $CLIENT_APP_ID"
echo "  objectId: $CLIENT_OBJECT_ID"

# Create client secret
echo "Creating client secret (capture and store securely)"
CLIENT_SECRET_JSON=$(az ad app credential reset \
  --id "$CLIENT_APP_ID" \
  --display-name "dev-secret" \
  --years 1 -o json)
CLIENT_SECRET_VALUE=$(echo "$CLIENT_SECRET_JSON" | jq -r '.password')

# Ensure client service principal exists (optional)
az ad sp create --id "$CLIENT_APP_ID" >/dev/null || true

# ======================================
# 3) Wire client → API and grant consent
# ======================================
echo "Adding delegated permission (Mcp.Access) to the client"
az ad app permission add \
  --id "$CLIENT_APP_ID" \
  --api "$API_APP_ID" \
  --api-permissions "$MCP_ACCESS_SCOPE_ID=Scope" >/dev/null

echo "Granting admin consent for the client"
az ad app permission admin-consent --id "$CLIENT_APP_ID" >/dev/null

# (Optional) Pre-authorize the client on the API (lets it call without interactive consent)
echo "Pre-authorizing the client on the API"
az rest --method PATCH \
  --url "https://graph.microsoft.com/v1.0/applications/$API_OBJECT_ID" \
  --body "$(jq -n \
    --arg clientId "$CLIENT_APP_ID" \
    --arg scopeId "$MCP_ACCESS_SCOPE_ID" \
    '{ api: { preAuthorizedApplications: [ { appId: $clientId, delegatedPermissionIds: [ $scopeId ] } ] } }')" >/dev/null

# ==================
# 4) Output summary
# ==================
cat <<EOF

Done.

Values to copy into your code:

BlazorConsole appsettings.Development.json
-----------------------------------------
AzureAd:
  Instance: https://login.microsoftonline.com/
  TenantId: $TENANT_ID
  ClientId: $CLIENT_APP_ID
  ClientSecret: $CLIENT_SECRET_VALUE
  CallbackPath: /signin-oidc

DownstreamApi.McpApi:
  BaseUrl: $API_HTTPS_BASE
  Scopes:  ["api://$API_APP_ID/Mcp.Access"]

API (McpServer.Http) appsettings.Development.json
-------------------------------------------------
Authentication.Jwt:
  Authority: https://login.microsoftonline.com/$TENANT_ID/v2.0
  Audience:  $API_APP_ID

Role IDs (for automation, optional)
-----------------------------------
ChatUser: $CHATUSER_ROLE_ID
Operator: $OPERATOR_ROLE_ID
Admin:    $ADMIN_ROLE_ID

Notes:
- Redirect URIs set:
  - $BLAZOR_CONSOLE_HTTPS/signin-oidc
  $( $ADD_IIS_EXPRESS_REDIRECT && echo "  - $BLAZOR_CONSOLE_IIS_EXPRESS_HTTPS/signin-oidc" )
- Front-channel logout:
  - $BLAZOR_CONSOLE_HTTPS/signout-oidc
- If you assign roles to users, do it in Enterprise applications → $API_APP_NAME → Users and groups → Add user → pick role(s).

EOF