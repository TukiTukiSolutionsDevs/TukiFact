import requests

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DASHBOARD_ENDPOINT = "/v1/dashboard"
TIMEOUT = 30

def test_dashboard_returns_complete_kpis():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    # Authenticate and get token
    try:
        login_resp = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=login_payload,
            timeout=TIMEOUT
        )
        login_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Login request failed: {e}"
    login_json = login_resp.json()
    assert "accessToken" in login_json and isinstance(login_json["accessToken"], str) and login_json["accessToken"].startswith("eyJ"), \
        "accessToken missing or invalid in login response"
    token = login_json["accessToken"]

    headers = {"Authorization": f"Bearer {token}"}
    try:
        dashboard_resp = requests.get(
            f"{BASE_URL}{DASHBOARD_ENDPOINT}",
            headers=headers,
            timeout=TIMEOUT
        )
        dashboard_resp.raise_for_status()
    except requests.RequestException as e:
        assert False, f"Dashboard request failed: {e}"

    data = dashboard_resp.json()

    # Validate 'today' object
    assert "today" in data and isinstance(data["today"], dict), "'today' object missing"
    today = data["today"]
    for key in ["totalAmount", "totalDocuments"]:
        assert key in today, f"'{key}' missing in 'today'"
        assert isinstance(today[key], (int, float)) and today[key] >= 0, f"'{key}' in 'today' must be numeric >= 0"

    # Validate 'thisMonth' object
    assert "thisMonth" in data and isinstance(data["thisMonth"], dict), "'thisMonth' object missing"
    this_month = data["thisMonth"]
    for key in ["totalAmount", "totalDocuments"]:
        assert key in this_month, f"'{key}' missing in 'thisMonth'"
        assert isinstance(this_month[key], (int, float)) and this_month[key] >= 0, f"'{key}' in 'thisMonth' must be numeric >= 0"

    # Removed 'byType' assertions because 'byType' is not specified in PRD

    # Validate 'byStatus' array
    assert "byStatus" in data and isinstance(data["byStatus"], list), "'byStatus' array missing"
    for item in data["byStatus"]:
        assert isinstance(item, dict), "Each item in 'byStatus' must be an object"
        assert "status" in item and isinstance(item["status"], str), "'status' missing or not string in 'byStatus' item"
        assert "count" in item and isinstance(item["count"], (int, float)) and item["count"] >= 0, "'count' missing or invalid in 'byStatus' item"

    # Validate 'monthlySales' array
    assert "monthlySales" in data and isinstance(data["monthlySales"], list), "'monthlySales' array missing"
    for item in data["monthlySales"]:
        assert isinstance(item, dict), "Each item in 'monthlySales' must be an object"
        assert "month" in item and isinstance(item["month"], str), "'month' missing or invalid in 'monthlySales' item"
        assert "total" in item and isinstance(item["total"], (int, float)) and item["total"] >= 0, "'total' missing or invalid in 'monthlySales' item"


test_dashboard_returns_complete_kpis()