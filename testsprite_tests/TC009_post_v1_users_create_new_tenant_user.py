import requests
import time

BASE_URL = "http://localhost:5186"
LOGIN_ENDPOINT = "/v1/auth/login"
USERS_ENDPOINT = "/v1/users"
AUTH_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}
TIMEOUT = 30

def test_post_v1_users_create_new_tenant_user():
    session = requests.Session()
    try:
        # Authenticate as admin to get access token
        login_resp = session.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=AUTH_PAYLOAD,
            timeout=TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
        access_token = login_resp.json().get("accessToken")
        assert access_token, "No accessToken in login response"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Generate unique user email to avoid conflicts
        timestamp = int(time.time() * 1000)
        unique_email = f"newuser{timestamp}@tukitest.pe"

        new_user_payload = {
            "email": unique_email,
            "password": "User2026!",
            "fullName": "New User Test",
            "role": "emisor"
        }

        # Create the new tenant user
        create_resp = session.post(
            BASE_URL + USERS_ENDPOINT,
            headers=headers,
            json=new_user_payload,
            timeout=TIMEOUT
        )
        assert create_resp.status_code == 201, f"User creation failed: {create_resp.text}"
        json_data = create_resp.json()
        new_user_id = json_data.get("id")
        assert new_user_id, "Response missing id"

    finally:
        # Cleanup: delete the created user if it was created
        if 'new_user_id' in locals() and new_user_id:
            try:
                del_resp = session.delete(
                    f"{BASE_URL}{USERS_ENDPOINT}/{new_user_id}",
                    headers=headers,
                    timeout=TIMEOUT
                )
                assert del_resp.status_code == 204, f"User deletion failed: {del_resp.text}"
            except Exception as e:
                print(f"Cleanup deletion error: {e}")

test_post_v1_users_create_new_tenant_user()
