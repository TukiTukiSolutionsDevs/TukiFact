import requests

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
REFRESH_URL = f"{BASE_URL}/v1/auth/refresh"
TIMEOUT = 30

def test_refresh_token_returns_new_tokens():
    login_payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    headers = {
        "Content-Type": "application/json"
    }

    # Step 1: Login to get refreshToken
    try:
        login_response = requests.post(LOGIN_URL, json=login_payload, headers=headers, timeout=TIMEOUT)
        assert login_response.status_code == 200, f"Login failed with status {login_response.status_code}"
        login_json = login_response.json()
        assert "refreshToken" in login_json and isinstance(login_json["refreshToken"], str) and login_json["refreshToken"], "refreshToken missing or empty in login response"
    except Exception as e:
        raise AssertionError(f"Login request failed: {e}")

    refresh_token = login_json["refreshToken"]

    # Step 2: Use refreshToken to get new tokens
    refresh_payload = {
        "refreshToken": refresh_token
    }
    try:
        refresh_response = requests.post(REFRESH_URL, json=refresh_payload, headers=headers, timeout=TIMEOUT)
        assert refresh_response.status_code == 200, f"Refresh token request failed with status {refresh_response.status_code}"
        refresh_json = refresh_response.json()
        assert "accessToken" in refresh_json and isinstance(refresh_json["accessToken"], str) and refresh_json["accessToken"], "accessToken missing or empty in refresh response"
        assert "refreshToken" in refresh_json and isinstance(refresh_json["refreshToken"], str) and refresh_json["refreshToken"], "refreshToken missing or empty in refresh response"
    except Exception as e:
        raise AssertionError(f"Refresh token request failed: {e}")

    # The new accessToken can be used for any subsequent tests if needed
    new_access_token = refresh_json["accessToken"]

test_refresh_token_returns_new_tokens()