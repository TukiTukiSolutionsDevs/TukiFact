import requests

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
AUDIT_LOG_URL = f"{BASE_URL}/v1/audit-log"
LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}
TIMEOUT = 30


def test_audit_log_with_pagination_and_filter():
    # Step 1: Login to get Bearer token
    login_headers = {"Content-Type": "application/json"}
    resp = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, headers=login_headers, timeout=TIMEOUT)
    assert resp.status_code == 200, f"Login failed with status {resp.status_code}"
    login_data = resp.json()
    access_token = login_data.get("accessToken")
    assert access_token and isinstance(access_token, str), "No accessToken found in login response"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: GET /v1/audit-log?page=1&pageSize=10
    params_1 = {"page": 1, "pageSize": 10}
    resp1 = requests.get(AUDIT_LOG_URL, headers=headers, params=params_1, timeout=TIMEOUT)
    assert resp1.status_code == 200, f"Audit log first request failed with status {resp1.status_code}"
    json_1 = resp1.json()
    assert isinstance(json_1, dict), "Response is not a JSON object"
    assert "data" in json_1 and isinstance(json_1["data"], list), "'data' array missing or invalid"
    assert "pagination" in json_1 and isinstance(json_1["pagination"], dict), "'pagination' object missing or invalid"
    pagination_1 = json_1["pagination"]
    assert "totalCount" in pagination_1, "'totalCount' missing in pagination"
    assert isinstance(pagination_1["totalCount"], int) and pagination_1["totalCount"] > 0, "'totalCount' should be > 0"

    # Step 3: GET /v1/audit-log?entityType=Documents&page=1&pageSize=5
    params_2 = {"entityType": "Documents", "page": 1, "pageSize": 5}
    resp2 = requests.get(AUDIT_LOG_URL, headers=headers, params=params_2, timeout=TIMEOUT)
    assert resp2.status_code == 200, f"Audit log filtered request failed with status {resp2.status_code}"
    json_2 = resp2.json()
    assert isinstance(json_2, dict), "Filtered response is not a JSON object"
    assert "data" in json_2 and isinstance(json_2["data"], list), "Filtered 'data' array missing or invalid"
    assert "pagination" in json_2 and isinstance(json_2["pagination"], dict), "Filtered 'pagination' object missing or invalid"


test_audit_log_with_pagination_and_filter()