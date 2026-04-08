import requests
import uuid

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
USERS_URL = f"{BASE_URL}/v1/users"
LOGIN_CREDENTIALS = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}
TIMEOUT = 30


def test_users_crud_and_role_validation():
    # Login as admin to get Bearer token
    try:
        response = requests.post(
            LOGIN_URL,
            json=LOGIN_CREDENTIALS,
            timeout=TIMEOUT
        )
        assert response.status_code == 200, f"Login failed with status {response.status_code}"
        token_data = response.json()
        access_token = token_data.get("accessToken")
        assert access_token and isinstance(access_token, str) and len(access_token) > 0, "Invalid accessToken in login response"
    except Exception as e:
        raise AssertionError(f"Login request failed: {str(e)}")

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # 1. GET /v1/users: Expect 200 OK with JSON array
    try:
        get_resp = requests.get(USERS_URL, headers=headers, timeout=TIMEOUT)
        assert get_resp.status_code == 200, f"GET /v1/users failed with status {get_resp.status_code}"
        users_list = get_resp.json()
        assert isinstance(users_list, list), "GET /v1/users response is not a JSON array"
    except Exception as e:
        raise AssertionError(f"GET /v1/users request failed: {str(e)}")

    user_id = None
    # To ensure unique emails, append a UUID suffix before @
    unique_suffix = uuid.uuid4().hex[:8]

    # 2. POST /v1/users with valid role "emisor": Expect 201 Created and save user ID
    new_user_payload = {
        "email": f"testcrud+{unique_suffix}@test.pe",
        "password": "Crud2026!",
        "fullName": "CRUD User",
        "role": "emisor"
    }
    try:
        post_resp = requests.post(USERS_URL, headers=headers, json=new_user_payload, timeout=TIMEOUT)
        assert post_resp.status_code == 201, f"POST /v1/users failed with status {post_resp.status_code}"
        post_data = post_resp.json()
        # The user ID is expected in response, typically as 'id'
        user_id = post_data.get("id")
        assert user_id and isinstance(user_id, str) and len(user_id) > 0, "User ID missing or invalid in POST response"
    except Exception as e:
        raise AssertionError(f"POST /v1/users request failed: {str(e)}")

    if not user_id:
        raise AssertionError("User ID not captured after creating user.")

    try:
        # 3. PUT /v1/users/{id} with body {"role": "consulta"}: Expect 204 No Content
        put_url = f"{USERS_URL}/{user_id}"
        put_payload = {"role": "consulta"}
        put_resp = requests.put(put_url, headers=headers, json=put_payload, timeout=TIMEOUT)
        assert put_resp.status_code == 204, f"PUT /v1/users/{user_id} failed with status {put_resp.status_code}"

        # 4. DELETE /v1/users/{id}: Expect 204 No Content
        del_resp = requests.delete(put_url, headers=headers, timeout=TIMEOUT)
        assert del_resp.status_code == 204, f"DELETE /v1/users/{user_id} failed with status {del_resp.status_code}"

    finally:
        # Cleanup: in case user was not deleted properly
        if user_id:
            try:
                requests.delete(f"{USERS_URL}/{user_id}", headers=headers, timeout=TIMEOUT)
            except Exception:
                pass

    # 5. POST /v1/users with invalid role "superadmin": Expect 400 Bad Request
    bad_user_payload = {
        "email": f"bad+{unique_suffix}@test.pe",
        "password": "B2026!",
        "fullName": "Bad",
        "role": "superadmin"
    }
    try:
        bad_post_resp = requests.post(USERS_URL, headers=headers, json=bad_user_payload, timeout=TIMEOUT)
        assert bad_post_resp.status_code == 400, f"POST /v1/users with invalid role did not return 400 Bad Request, got {bad_post_resp.status_code}"
    except Exception as e:
        raise AssertionError(f"POST /v1/users with invalid role request failed: {str(e)}")


test_users_crud_and_role_validation()