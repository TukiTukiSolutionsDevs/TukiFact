import requests
from datetime import datetime, timezone
import sys

BASE_URL = "http://localhost:80"
BACKOFFICE_LOGIN_URL = f"{BASE_URL}/v1/backoffice/auth/login"
IMPERSONATE_URL_TEMPLATE = f"{BASE_URL}/v1/backoffice/tenants/{{tenant_id}}/impersonate"

BACKOFFICE_EMAIL = "superadmin@tukifact.net.pe"
BACKOFFICE_PASSWORD = "SuperAdmin2026!"
TENANT_ID = "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
TIMEOUT = 30

def test_backoffice_impersonate_tenant():
    try:
        # 1. Authenticate backoffice user to get BACKOFFICE_TOKEN
        auth_payload = {
            "email": BACKOFFICE_EMAIL,
            "password": BACKOFFICE_PASSWORD
        }
        auth_resp = requests.post(BACKOFFICE_LOGIN_URL, json=auth_payload, timeout=TIMEOUT)
        assert auth_resp.status_code == 200, f"Backoffice auth failed with status {auth_resp.status_code}"
        auth_data = auth_resp.json()
        token = auth_data.get("accessToken")
        user = auth_data.get("user")
        assert token and isinstance(token, str) and token.startswith("eyJ"), "Invalid BACKOFFICE_TOKEN"
        assert user and user.get("role") == "superadmin", "User role is not superadmin"

        headers = {
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json"
        }

        # 2. POST /v1/backoffice/tenants/{tenantId}/impersonate
        impersonate_url = IMPERSONATE_URL_TEMPLATE.format(tenant_id=TENANT_ID)
        resp = requests.post(impersonate_url, headers=headers, timeout=TIMEOUT)
        
        # Validate response status
        assert resp.status_code == 200, f"Impersonate request failed with status {resp.status_code}"

        data = resp.json()
        # Validate keys present
        required_keys = ['tenantId', 'tenantName', 'tenantRuc', 'impersonating', 'expiresAt', 'redirectUrl']
        for key in required_keys:
            assert key in data, f"Response JSON missing key '{key}'"

        # Validate tenantId matches
        assert data['tenantId'] == TENANT_ID, f"tenantId mismatch: expected {TENANT_ID}, got {data['tenantId']}"

        # impersonating == True
        assert data['impersonating'] is True, "'impersonating' is not True"

        # expiresAt is a future date (ISO 8601)
        expires_at_str = data['expiresAt']
        try:
            expires_at = datetime.fromisoformat(expires_at_str.replace("Z", "+00:00"))
        except Exception as e:
            assert False, f"expiresAt is not a valid ISO8601 datetime: {expires_at_str}"
        now = datetime.now(timezone.utc)
        assert expires_at > now, f"expiresAt ({expires_at_str}) is not a future date"

        # redirectUrl is a non-empty string
        redirect_url = data['redirectUrl']
        assert isinstance(redirect_url, str) and redirect_url.strip() != "", "redirectUrl is empty or invalid"

    except (AssertionError, requests.RequestException) as e:
        print(f"TEST FAILED: {e}", file=sys.stderr)
        raise

test_backoffice_impersonate_tenant()