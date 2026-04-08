import requests

BASE_URL = "http://localhost:5186"
LOGIN_ENDPOINT = "/v1/auth/login"
API_KEYS_ENDPOINT = "/v1/api-keys"
LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84",
}
TIMEOUT = 30

def test_api_keys_create_and_revoke():
    # Login to get access token
    login_resp = requests.post(
        f"{BASE_URL}{LOGIN_ENDPOINT}",
        json=LOGIN_PAYLOAD,
        timeout=TIMEOUT
    )
    assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert access_token and isinstance(access_token, str) and access_token.strip(), "Invalid accessToken in login response"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json",
        "Accept": "application/json"
    }

    # Step 1: GET /v1/api-keys to get existing keys (expect 200 and JSON array)
    get_keys_resp = requests.get(f"{BASE_URL}{API_KEYS_ENDPOINT}", headers=headers, timeout=TIMEOUT)
    assert get_keys_resp.status_code == 200, f"GET /v1/api-keys failed: {get_keys_resp.text}"
    keys_json = get_keys_resp.json()
    assert isinstance(keys_json, list), f"Expected JSON array from GET /v1/api-keys, got: {type(keys_json)}"

    # Step 2: POST /v1/api-keys with name and permissions
    post_payload = {
        "name": "TestKey",
        "permissions": ["emit", "query"]
    }
    post_resp = requests.post(f"{BASE_URL}{API_KEYS_ENDPOINT}", headers=headers, json=post_payload, timeout=TIMEOUT)
    assert post_resp.status_code == 201, f"POST /v1/api-keys failed: {post_resp.text}"
    post_json = post_resp.json()
    key_id = post_json.get("id")
    plain_text_key = post_json.get("plainTextKey")

    assert key_id and isinstance(key_id, str) and key_id.strip(), "Missing or invalid 'id' in POST /v1/api-keys response"
    assert plain_text_key and isinstance(plain_text_key, str) and plain_text_key.strip(), "Missing or invalid 'plainTextKey' in POST /v1/api-keys response"

    # Step 3: DELETE /v1/api-keys/{id}
    try:
        delete_resp = requests.delete(f"{BASE_URL}{API_KEYS_ENDPOINT}/{key_id}", headers=headers, timeout=TIMEOUT)
        assert delete_resp.status_code == 204, f"DELETE /v1/api-keys/{key_id} failed: {delete_resp.text}"
    except Exception as e:
        # Attempt to cleanup but re-raise any assertion or request exceptions
        raise e

test_api_keys_create_and_revoke()