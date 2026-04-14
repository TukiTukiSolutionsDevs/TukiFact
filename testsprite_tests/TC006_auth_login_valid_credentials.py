import requests

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
TIMEOUT = 30

# Global variables to save tokens for subsequent tests
ACCESS_TOKEN = None
REFRESH_TOKEN = None

def test_auth_login_valid_credentials():
    global ACCESS_TOKEN
    global REFRESH_TOKEN

    url = f"{BASE_URL}{LOGIN_ENDPOINT}"
    payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    headers = {
        "Content-Type": "application/json"
    }

    try:
        response = requests.post(url, json=payload, headers=headers, timeout=TIMEOUT)
        response.raise_for_status()
    except requests.exceptions.RequestException as e:
        assert False, f"Request failed: {e}"

    assert response.status_code == 200, f"Expected status 200, got {response.status_code}"
    try:
        data = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    # Validate accessToken
    assert "accessToken" in data, "'accessToken' not in response"
    ACCESS_TOKEN = data["accessToken"]
    assert isinstance(ACCESS_TOKEN, str) and ACCESS_TOKEN.startswith("eyJ") and len(ACCESS_TOKEN) > 0, \
        "accessToken must be non-empty string starting with 'eyJ'"

    # Validate refreshToken
    assert "refreshToken" in data, "'refreshToken' not in response"
    REFRESH_TOKEN = data["refreshToken"]
    assert isinstance(REFRESH_TOKEN, str) and len(REFRESH_TOKEN) > 0, "refreshToken must be non-empty string"

    # Validate user object
    assert "user" in data, "'user' not in response"
    user = data["user"]
    assert isinstance(user, dict), "'user' should be an object"

    assert user.get("email") == "prdtest@test.pe", f"User email expected 'prdtest@test.pe', got '{user.get('email')}'"
    assert user.get("role") == "admin", f"User role expected 'admin', got '{user.get('role')}'"


test_auth_login_valid_credentials()