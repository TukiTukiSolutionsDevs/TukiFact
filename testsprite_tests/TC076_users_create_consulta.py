import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
USERS_URL = f"{BASE_URL}/v1/users"

def test_users_create_consulta():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_resp = requests.post(LOGIN_URL, json=login_payload, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid or missing accessToken in login response"
    except Exception as e:
        raise AssertionError(f"Login request failed: {e}")

    timestamp = int(time.time())
    user_email = f"consulta_{timestamp}@test.pe"
    user_payload = {
        "email": user_email,
        "password": "Consulta2026!",
        "fullName": "Consulta User PRD",
        "role": "consulta"
    }
    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    created_user_id = None
    try:
        user_resp = requests.post(USERS_URL, headers=headers, json=user_payload, timeout=30)
        assert user_resp.status_code == 201, f"User creation failed with status {user_resp.status_code}"
        user_data = user_resp.json()
        created_user_id = user_data.get("id")
        assert created_user_id, "Response JSON missing 'id'"
        assert user_data.get("email") == user_email, "Email in response does not match"
        assert user_data.get("role") == "consulta", "Role in response is not 'consulta'"
    finally:
        if created_user_id:
            try:
                # Cleanup: delete the created user
                del_resp = requests.delete(f"{USERS_URL}/{created_user_id}", headers=headers, timeout=30)
                assert del_resp.status_code in (200, 204), f"User deletion failed with status {del_resp.status_code}"
            except Exception as e:
                # Log cleanup failure but do not raise to prevent hiding previous test errors
                print(f"Warning: Failed to delete test user {created_user_id}: {e}")

test_users_create_consulta()