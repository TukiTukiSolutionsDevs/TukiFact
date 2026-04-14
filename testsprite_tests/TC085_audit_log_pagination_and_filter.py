import requests

BASE_URL = "http://localhost:80"
LOGIN_PATH = "/v1/auth/login"
AUDIT_LOG_PATH = "/v1/audit-log"
LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}
TIMEOUT = 30

def test_audit_log_pagination_and_filter():
    # Step 1: Authenticate to get access token
    try:
        login_resp = requests.post(
            f"{BASE_URL}{LOGIN_PATH}", json=LOGIN_PAYLOAD, timeout=TIMEOUT
        )
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"
    assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"

    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert access_token and isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken"

    headers = {"Authorization": f"Bearer {access_token}"}

    # Step 2: GET /v1/audit-log?page=1&pageSize=10
    params_page = {"page": 1, "pageSize": 10}
    try:
        resp_page = requests.get(
            f"{BASE_URL}{AUDIT_LOG_PATH}", headers=headers, params=params_page, timeout=TIMEOUT
        )
    except requests.RequestException as e:
        assert False, f"Audit log pagination request failed: {e}"
    assert resp_page.status_code == 200, f"Audit log pagination failed with status {resp_page.status_code}"

    json_page = resp_page.json()
    data_page = json_page.get("data")
    pagination = json_page.get("pagination")

    assert isinstance(data_page, list), "'data' is not a list"
    assert isinstance(pagination, dict), "'pagination' is not a dict"
    total_count = pagination.get("totalCount")
    assert isinstance(total_count, int) and total_count >= 1, f"Invalid totalCount: {total_count}"

    # Step 3: GET /v1/audit-log?entityType=Documents&page=1&pageSize=5
    params_filtered = {"entityType": "Documents", "page": 1, "pageSize": 5}
    try:
        resp_filtered = requests.get(
            f"{BASE_URL}{AUDIT_LOG_PATH}", headers=headers, params=params_filtered, timeout=TIMEOUT
        )
    except requests.RequestException as e:
        assert False, f"Audit log filtered request failed: {e}"
    assert resp_filtered.status_code == 200, f"Audit log filtered failed with status {resp_filtered.status_code}"

    json_filtered = resp_filtered.json()
    data_filtered = json_filtered.get("data")
    pagination_filtered = json_filtered.get("pagination")

    assert isinstance(data_filtered, list), "'data' filtered response is not a list"
    assert isinstance(pagination_filtered, dict), "'pagination' filtered response is not a dict"
    # It is valid that filtered results count can be zero, so no minimum count assertion here

def run_test():
    test_audit_log_pagination_and_filter()

run_test()