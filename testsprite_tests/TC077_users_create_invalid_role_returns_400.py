import requests

def test_users_create_invalid_role_returns_400():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    users_url = f"{base_url}/v1/users"
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    user_payload = {
        "email": "bad@test.pe",
        "password": "Bad2026!",
        "fullName": "Bad",
        "role": "superadmin"
    }
    timeout = 30

    # Authenticate to get access token
    try:
        login_resp = requests.post(login_url, json=login_payload, timeout=timeout)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid access token format"
    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Authentication failed: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Attempt to create user with invalid role 'superadmin'
    try:
        response = requests.post(users_url, headers=headers, json=user_payload, timeout=timeout)
    except requests.RequestException as e:
        raise AssertionError(f"Request to create user failed: {e}")

    assert response.status_code == 400, (
        f"Expected 400 Bad Request, got {response.status_code}: {response.text}"
    )

test_users_create_invalid_role_returns_400()