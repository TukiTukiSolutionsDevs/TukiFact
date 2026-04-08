import requests

def test_tc004_get_v1_auth_me_with_valid_access_token():
    base_url = "http://localhost:5186"
    login_url = f"{base_url}/v1/auth/login"
    auth_me_url = f"{base_url}/v1/auth/me"
    login_payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    timeout = 30

    # Step 1: Login to get accessToken
    try:
        login_response = requests.post(login_url, json=login_payload, timeout=timeout)
    except requests.RequestException as e:
        raise AssertionError(f"Login request failed: {e}")
    assert login_response.status_code == 200, f"Expected 200 from login, got {login_response.status_code}"
    login_data = login_response.json()
    access_token = login_data.get("accessToken")
    assert access_token, "accessToken not found in login response"

    headers = {
        "Authorization": f"Bearer {access_token}"
    }

    # Step 2: Call GET /v1/auth/me with valid token
    try:
        auth_me_response = requests.get(auth_me_url, headers=headers, timeout=timeout)
    except requests.RequestException as e:
        raise AssertionError(f"GET /v1/auth/me request failed: {e}")
    assert auth_me_response.status_code == 200, f"Expected 200 from /v1/auth/me, got {auth_me_response.status_code}"

    auth_me_data = auth_me_response.json()
    # Validate required fields are present and not null
    for field in ["userId", "tenantId", "email", "role"]:
        assert field in auth_me_data, f"Field '{field}' missing in /v1/auth/me response"
        assert auth_me_data[field] is not None, f"Field '{field}' is null in /v1/auth/me response"

test_tc004_get_v1_auth_me_with_valid_access_token()