import requests

BASE_URL = "http://localhost:5186"
LOGIN_ENDPOINT = "/v1/auth/login"
DOCUMENTS_ENDPOINT = "/v1/documents"
AUTH_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}
TIMEOUT = 30

def test_get_v1_documents_list_with_pagination():
    # Authenticate and obtain access token
    try:
        login_resp = requests.post(
            BASE_URL + LOGIN_ENDPOINT,
            json=AUTH_PAYLOAD,
            timeout=TIMEOUT
        )
        login_resp.raise_for_status()
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and isinstance(access_token, str), "No accessToken in login response"
    except Exception as e:
        raise AssertionError(f"Authentication failed: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}"
    }
    params = {
        "page": 1,
        "pageSize": 20
    }

    try:
        resp = requests.get(
            BASE_URL + DOCUMENTS_ENDPOINT,
            headers=headers,
            params=params,
            timeout=TIMEOUT
        )
        resp.raise_for_status()
        assert resp.status_code == 200, f"Expected status 200 but got {resp.status_code}"
        resp_json = resp.json()
        # Validate presence of paginated list and metadata
        assert isinstance(resp_json, dict), "Response JSON is not a dictionary"
        # Expecting at least keys like 'items' (list of documents), 'page', 'pageSize', 'totalItems', 'totalPages'
        assert "items" in resp_json, "Response JSON missing 'items'"
        assert isinstance(resp_json["items"], list), "'items' is not a list"
        # Metadata keys validation
        for meta_key in ["page", "pageSize", "totalItems", "totalPages"]:
            assert meta_key in resp_json, f"Response JSON missing '{meta_key}'"
            assert isinstance(resp_json[meta_key], int), f"'{meta_key}' is not an int"
    except Exception as e:
        raise AssertionError(f"Failed to get documents list with pagination: {e}")

test_get_v1_documents_list_with_pagination()