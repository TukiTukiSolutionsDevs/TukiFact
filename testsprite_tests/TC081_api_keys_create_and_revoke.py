import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
API_KEYS_ENDPOINT = "/v1/api-keys"
TIMEOUT = 30

def test_api_keys_create_and_revoke():
    # Step 1: Authenticate and get accessToken
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_response = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=login_payload,
            timeout=TIMEOUT
        )
        login_response.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"

    login_json = login_response.json()
    access_token = login_json.get("accessToken")
    assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken in login response"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Create new API key
    api_key_name = f"PRD Test Key {int(time.time())}"
    create_payload = {
        "name": api_key_name,
        "permissions": ["emit", "query"]
    }
    try:
        create_response = requests.post(
            f"{BASE_URL}{API_KEYS_ENDPOINT}",
            headers=headers,
            json=create_payload,
            timeout=TIMEOUT
        )
        create_response.raise_for_status()
    except requests.RequestException as e:
        assert False, f"API Key creation request failed: {e}"

    assert create_response.status_code == 201, f"Expected status 201 Created, got {create_response.status_code}"
    create_json = create_response.json()
    key_id = create_json.get("id")
    plain_text_key = create_json.get("plainTextKey")

    assert isinstance(key_id, str) and len(key_id) > 0, "API Key 'id' missing or empty"
    assert isinstance(plain_text_key, str) and len(plain_text_key) > 0, "API Key 'plainTextKey' missing or empty"
    assert plain_text_key.startswith("tk_"), "plainTextKey does not start with 'tk_'"

    try:
        # Step 3: List API keys and check at least one exists
        try:
            list_response = requests.get(
                f"{BASE_URL}{API_KEYS_ENDPOINT}",
                headers=headers,
                timeout=TIMEOUT
            )
            list_response.raise_for_status()
        except requests.RequestException as e:
            assert False, f"API Keys list request failed: {e}"

        assert list_response.status_code == 200, f"Expected status 200 OK for list, got {list_response.status_code}"

        list_json = list_response.json()
        # The API might return {'data': [...]} or just an array. Check both.
        keys_list = None
        if isinstance(list_json, dict):
            # Check if 'data' key exists
            if "data" in list_json and isinstance(list_json["data"], list):
                keys_list = list_json["data"]
            elif isinstance(list_json.get("items"), list):
                keys_list = list_json["items"]
            else:
                # If no 'data', check if the root object itself is list of keys
                keys_list = [list_json] if list_json else []
        elif isinstance(list_json, list):
            keys_list = list_json
        else:
            keys_list = []

        assert isinstance(keys_list, list), "API keys list response is not a list"

        assert any(isinstance(k, dict) and "id" in k for k in keys_list), "No API keys found in list"

    finally:
        # Step 4: Delete the created API key
        try:
            del_response = requests.delete(
                f"{BASE_URL}{API_KEYS_ENDPOINT}/{key_id}",
                headers=headers,
                timeout=TIMEOUT
            )
            # Valid status is 204 No Content
            assert del_response.status_code == 204, f"Expected status 204 No Content on delete, got {del_response.status_code}"
        except requests.RequestException as e:
            assert False, f"API Key deletion request failed: {e}"

test_api_keys_create_and_revoke()
