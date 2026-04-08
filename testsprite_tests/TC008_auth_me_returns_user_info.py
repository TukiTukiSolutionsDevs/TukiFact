import requests

BASE_URL = "http://localhost:5186"
LOGIN_ENDPOINT = "/v1/auth/login"
AUTH_ME_ENDPOINT = "/v1/auth/me"
TIMEOUT = 30


def test_auth_me_returns_user_info():
    login_url = f"{BASE_URL}{LOGIN_ENDPOINT}"
    auth_me_url = f"{BASE_URL}{AUTH_ME_ENDPOINT}"

    login_payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    headers = {"Content-Type": "application/json"}

    # Login to get the Bearer token
    try:
        login_response = requests.post(login_url, json=login_payload, headers=headers, timeout=TIMEOUT)
        login_response.raise_for_status()
    except requests.RequestException as e:
        raise AssertionError(f"Login request failed: {e}")

    login_data = login_response.json()
    access_token = login_data.get("accessToken")
    assert isinstance(access_token, str) and access_token.strip(), "Access token is missing or empty in login response"

    # Call /v1/auth/me endpoint with Bearer token
    auth_headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {access_token}"
    }
    try:
        auth_me_response = requests.get(auth_me_url, headers=auth_headers, timeout=TIMEOUT)
        auth_me_response.raise_for_status()
    except requests.RequestException as e:
        raise AssertionError(f"Auth /me request failed: {e}")

    assert auth_me_response.status_code == 200, f"Expected status code 200, got {auth_me_response.status_code}"
    auth_me_data = auth_me_response.json()

    user_id = auth_me_data.get("userId")
    tenant_id = auth_me_data.get("tenantId")
    email = auth_me_data.get("email")
    role = auth_me_data.get("role")

    assert user_id is not None, "userId should not be null"
    assert tenant_id == "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84", f"tenantId mismatch: expected '0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84', got '{tenant_id}'"
    assert email == "admin@tukitest.pe", f"email mismatch: expected 'admin@tukitest.pe', got '{email}'"
    assert role == "admin", f"role mismatch: expected 'admin', got '{role}'"


test_auth_me_returns_user_info()