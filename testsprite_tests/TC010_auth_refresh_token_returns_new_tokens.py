import requests

def test_auth_refresh_token_returns_new_tokens():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    refresh_url = f"{base_url}/v1/auth/refresh"
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572",
    }
    headers = {"Content-Type": "application/json"}

    try:
        # Step 1: Login to get original tokens
        login_resp = requests.post(login_url, json=login_payload, headers=headers, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_data = login_resp.json()

        orig_access_token = login_data.get("accessToken")
        orig_refresh_token = login_data.get("refreshToken")

        assert isinstance(orig_access_token, str) and orig_access_token.startswith("eyJ") and orig_access_token.strip() != "", "Invalid original accessToken"
        assert isinstance(orig_refresh_token, str) and orig_refresh_token.strip() != "", "Invalid original refreshToken"

        # Step 2: Refresh tokens using original refreshToken
        refresh_payload = {"refreshToken": orig_refresh_token}
        refresh_resp = requests.post(refresh_url, json=refresh_payload, headers=headers, timeout=30)
        assert refresh_resp.status_code == 200, f"Refresh failed with status {refresh_resp.status_code}"
        refresh_data = refresh_resp.json()

        new_access_token = refresh_data.get("accessToken")
        new_refresh_token = refresh_data.get("refreshToken")

        assert isinstance(new_access_token, str) and new_access_token.startswith("eyJ") and new_access_token.strip() != "", "Invalid new accessToken"
        assert isinstance(new_refresh_token, str) and new_refresh_token.strip() != "", "Invalid new refreshToken"

        # Tokens must be different from originals
        assert new_access_token != orig_access_token, "New accessToken matches original"
        assert new_refresh_token != orig_refresh_token, "New refreshToken matches original"

    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Test failed: {e}")

test_auth_refresh_token_returns_new_tokens()