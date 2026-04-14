import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
USERS_URL = f"{BASE_URL}/v1/users"

def test_users_delete_soft():
    # Step 1: Login to get access token
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
        assert access_token and access_token.startswith("eyJ"), "Invalid access token"
    except Exception as e:
        raise AssertionError(f"Login step failed: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Create user to delete
    timestamp = int(time.time())
    user_payload = {
        "email": f"delete_{timestamp}@test.pe",
        "password": "Delete2026!",
        "fullName": "User To Delete",
        "role": "emisor"
    }

    user_id = None
    try:
        create_resp = requests.post(USERS_URL, json=user_payload, headers=headers, timeout=30)
        assert create_resp.status_code == 201, f"User creation failed with status {create_resp.status_code}"
        create_data = create_resp.json()
        user_id = create_data.get("id")
        assert user_id, "User ID not found in creation response"

        # Step 3: Delete the user (soft delete)
        delete_resp = requests.delete(f"{USERS_URL}/{user_id}", headers=headers, timeout=30)
        assert delete_resp.status_code in [200, 204], f"User delete failed with status {delete_resp.status_code}"

        # Step 4: Verify user is deleted or deactivated by fetching /v1/users and checking user is not active
        users_list_resp = requests.get(USERS_URL, headers=headers, timeout=30)
        assert users_list_resp.status_code == 200, f"Fetching users list failed with status {users_list_resp.status_code}"
        users_list = users_list_resp.json()
        # Adjusted to handle list or dict response
        if isinstance(users_list, dict):
            users = users_list.get("data", [])
        else:
            users = users_list
        # Find the user in list if exists
        found_user = None
        for u in users:
            if u.get("id") == user_id:
                found_user = u
                break
        # The user should not be active or might be missing from list
        # We consider user deactivated if isActive is False or user not found at all
        if found_user:
            is_active = found_user.get("isActive", True)
            assert is_active is False, "User still active after delete"
        # else user not found is also acceptable as deleted
    finally:
        # Cleanup: attempt to delete user forcibly if still exists
        if user_id:
            try:
                requests.delete(f"{USERS_URL}/{user_id}", headers=headers, timeout=10)
            except Exception:
                pass  # ignore cleanup errors

test_users_delete_soft()
