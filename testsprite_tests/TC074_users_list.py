import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
USERS_ENDPOINT = "/v1/users"
TIMEOUT = 30

def test_users_list():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        # Authenticate to get access token
        login_resp = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=login_payload,
            timeout=TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken in login response"
        headers = {
            "Authorization": f"Bearer {access_token}"
        }

        # GET /v1/users with Bearer token
        users_resp = requests.get(
            f"{BASE_URL}{USERS_ENDPOINT}",
            headers=headers,
            timeout=TIMEOUT
        )
        assert users_resp.status_code == 200, f"Users list failed with status {users_resp.status_code}"
        users_json = users_resp.json()
        assert isinstance(users_json, list), f"Expected JSON array, got {type(users_json)}"

        # Validate each element fields
        assert len(users_json) >= 1, "Users list is empty, expected at least one user"
        found_admin = False
        for user in users_json:
            assert "id" in user and isinstance(user["id"], str) and user["id"], "User missing valid 'id'"
            assert "email" in user and isinstance(user["email"], str) and user["email"], "User missing valid 'email'"
            assert "fullName" in user and isinstance(user["fullName"], str) and user["fullName"], "User missing valid 'fullName'"
            assert "role" in user and isinstance(user["role"], str) and user["role"], "User missing valid 'role'"
            assert "isActive" in user and isinstance(user["isActive"], bool), "User missing valid 'isActive'"
            if user["role"].lower() == "admin" and user["isActive"]:
                found_admin = True

        assert found_admin, "No active user with role 'admin' found in users list"

    except requests.RequestException as e:
        assert False, f"Request failed: {e}"

test_users_list()