import requests

BASE_URL = "http://localhost:5186"
LOGIN_ENDPOINT = "/v1/auth/login"
TIMEOUT = 30

# Global storage for tokens to be used by subsequent tests
tokens = {
    "accessToken": None,
    "refreshToken": None
}

def test_TC005_login_valid_credentials():
    url = BASE_URL + LOGIN_ENDPOINT
    headers = {
        "Content-Type": "application/json"
    }
    payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }

    try:
        response = requests.post(url, json=payload, headers=headers, timeout=TIMEOUT)
    except requests.RequestException as e:
        assert False, f"Request to {url} failed with exception: {e}"

    assert response.status_code == 200, f"Expected status code 200, got {response.status_code}"

    try:
        data = response.json()
    except ValueError:
        assert False, "Response is not valid JSON"

    assert "accessToken" in data and isinstance(data["accessToken"], str) and data["accessToken"].strip() != "", "accessToken missing or empty"
    assert "refreshToken" in data and isinstance(data["refreshToken"], str) and data["refreshToken"].strip() != "", "refreshToken missing or empty"
    assert "user" in data and isinstance(data["user"], dict), "User object missing in response"
    user = data["user"]
    assert user.get("email") == "admin@tukitest.pe", f"User email expected 'admin@tukitest.pe', got {user.get('email')}"
    assert user.get("role") == "admin", f"User role expected 'admin', got {user.get('role')}"

    # Save tokens globally for use in subsequent tests
    tokens["accessToken"] = data["accessToken"]
    tokens["refreshToken"] = data["refreshToken"]

test_TC005_login_valid_credentials()
