import requests
import re

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
AUTH_ME_URL = f"{BASE_URL}/v1/auth/me"
TENANT_ID = "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
EMAIL = "prdtest@test.pe"
PASSWORD = "PrdTest2026!"

def test_auth_me_returns_user_info():
    timeout = 30
    # Step 1: Login to get accessToken as per TC006 details
    login_payload = {
        "email": EMAIL,
        "password": PASSWORD,
        "tenantId": TENANT_ID
    }
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=timeout)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_data = login_resp.json()
        assert "accessToken" in login_data and isinstance(login_data["accessToken"], str) and login_data["accessToken"].startswith("eyJ"), "accessToken invalid or missing"
        access_token = login_data["accessToken"]
    except Exception as e:
        raise AssertionError(f"Failed to login and get accessToken: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # Step 2: Call GET /v1/auth/me with Bearer token
    try:
        me_resp = requests.get(AUTH_ME_URL, headers=headers, timeout=timeout)
        assert me_resp.status_code == 200, f"/v1/auth/me returned status {me_resp.status_code}"
        me_data = me_resp.json()
    except Exception as e:
        raise AssertionError(f"Failed to GET /v1/auth/me: {e}")

    # Validate response fields
    user_id = me_data.get("userId")
    tenant_id = me_data.get("tenantId")
    email = me_data.get("email")
    role = me_data.get("role")

    # Validate userId is non-null UUID string
    uuid_regex = re.compile(r"^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$", re.IGNORECASE)
    assert user_id is not None and isinstance(user_id, str) and uuid_regex.match(user_id), f"userId invalid UUID: {user_id}"
    # Validate tenantId matches expected UUID
    assert tenant_id == TENANT_ID, f"tenantId mismatch: expected '{TENANT_ID}', got '{tenant_id}'"
    # Validate email is correct
    assert email == EMAIL, f"email mismatch: expected '{EMAIL}', got '{email}'"
    # Validate role is admin
    assert role == "admin", f"role mismatch: expected 'admin', got '{role}'"

test_auth_me_returns_user_info()