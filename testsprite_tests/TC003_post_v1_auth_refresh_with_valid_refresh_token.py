import requests

BASE_URL = "http://localhost:5186"
LOGIN_ENDPOINT = "/v1/auth/login"
REFRESH_ENDPOINT = "/v1/auth/refresh"
LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}
TIMEOUT = 30


def test_post_v1_auth_refresh_with_valid_refresh_token():
    try:
        # Step 1: Login to get initial tokens
        response_login = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=LOGIN_PAYLOAD,
            timeout=TIMEOUT
        )
        assert response_login.status_code == 200, f"Login failed: {response_login.text}"
        login_data = response_login.json()
        assert "accessToken" in login_data, "No accessToken in login response"
        assert "refreshToken" in login_data, "No refreshToken in login response"
        refresh_token = login_data["refreshToken"]

        # Step 2: Use refreshToken to get new tokens
        refresh_payload = {"refreshToken": refresh_token}
        response_refresh = requests.post(
            f"{BASE_URL}{REFRESH_ENDPOINT}",
            json=refresh_payload,
            timeout=TIMEOUT
        )
        assert response_refresh.status_code == 200, f"Refresh token failed: {response_refresh.text}"
        refresh_data = response_refresh.json()
        assert "accessToken" in refresh_data, "No accessToken in refresh response"
        assert "refreshToken" in refresh_data, "No refreshToken in refresh response"
        # The new tokens should be different from the old ones to confirm rotation
        assert refresh_data["accessToken"] != login_data["accessToken"], "accessToken not refreshed"
        assert refresh_data["refreshToken"] != login_data["refreshToken"], "refreshToken not refreshed"
    except requests.RequestException as e:
        assert False, f"HTTP request failed: {e}"


test_post_v1_auth_refresh_with_valid_refresh_token()