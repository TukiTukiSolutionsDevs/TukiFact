import requests

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
USERS_ENDPOINT = "/v1/users"
TIMEOUT = 30

def test_users_create_emisor():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    user_payload = {
        "email": "emisor_prd@test.pe",
        "password": "Emisor2026!",
        "fullName": "Emisor PRD",
        "role": "emisor"
    }

    # Authenticate and get access token
    login_url = BASE_URL + LOGIN_ENDPOINT
    try:
        login_response = requests.post(login_url, json=login_payload, timeout=TIMEOUT)
        login_response.raise_for_status()
        login_data = login_response.json()
        access_token = login_data.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        user_url = BASE_URL + USERS_ENDPOINT
        user_response = requests.post(user_url, json=user_payload, headers=headers, timeout=TIMEOUT)
        assert user_response.status_code == 201, f"Expected 201 Created, got {user_response.status_code}"
        user_data = user_response.json()
        user_id = user_data.get("id")
        assert isinstance(user_id, str) and len(user_id) > 0, "User ID missing or invalid"
    finally:
        # Cleanup: delete created user if created
        if 'user_id' in locals():
            try:
                delete_url = f"{BASE_URL}/v1/users/{user_id}"
                delete_resp = requests.delete(delete_url, headers=headers, timeout=TIMEOUT)
                # Accept 204 No Content or 200 OK on successful delete
                assert delete_resp.status_code in [200, 204], f"User deletion failed with status {delete_resp.status_code}"
            except Exception:
                # Ignore delete cleanup errors
                pass

test_users_create_emisor()